using System;
using System.IO;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    public class SaveStoreTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "PatiDostumTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [Test]
        public void NoFile_StartsNewGameWithStartingMoney()
        {
            SaveData data = new SaveStore(dir).Load(Now);
            Assert.AreEqual(SaveData.StartingGold, data.gold);
            Assert.AreEqual(SaveData.StartingDiamonds, data.diamonds);
            Assert.AreEqual(Now.Ticks, data.createdUtcTicks);
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
        }

        [Test]
        public void SaveThenLoad_KeepsEverything()
        {
            var store = new SaveStore(dir);
            SaveData data = store.Load(Now);
            data.gold = 1234;
            data.diamonds = 7;
            data.noDeath = true;
            data.clock.trustedUtcTicks = 42;
            data.clock.elapsedMs = 99;
            store.Save(data);

            SaveData loaded = new SaveStore(dir).Load(Now.AddDays(1));
            Assert.AreEqual(1234, loaded.gold);
            Assert.AreEqual(7, loaded.diamonds);
            Assert.IsTrue(loaded.noDeath);
            Assert.AreEqual(42, loaded.clock.trustedUtcTicks);
            Assert.AreEqual(99, loaded.clock.elapsedMs);
            Assert.AreEqual(Now.Ticks, loaded.createdUtcTicks);
        }

        [Test]
        public void CorruptMain_FallsBackToPreviousSave()
        {
            var store = new SaveStore(dir);
            SaveData data = store.Load(Now);
            data.gold = 1;
            store.Save(data);
            data.gold = 2;
            store.Save(data);

            File.WriteAllText(Path.Combine(dir, SaveStore.FileName), "{bozuk");
            Assert.AreEqual(1, new SaveStore(dir).Load(Now).gold);
        }

        [Test]
        public void Version1Save_GetsLifespanAndHealthFields()
        {
            Directory.CreateDirectory(dir);
            const string v1 = "{\"version\":1,\"gold\":100,\"diamonds\":5,\"pets\":[{\"id\":\"abc\",\"species\":\"cat\",\"name\":\"Karamel\",\"mode\":5,\"ageSeconds\":4838400.0,\"fullness\":80.0,\"health\":100.0}]}";
            File.WriteAllText(Path.Combine(dir, SaveStore.FileName), v1);
            SaveData data = new SaveStore(dir).Load(Now);
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            PetState pet = data.pets[0];
            Assert.AreEqual(15.0, pet.lifespanYears, 0.001);
            Assert.AreEqual("", pet.disease);
            Assert.AreNotEqual(0u, pet.rng);
            Assert.AreEqual(1, data.adoptions);
            Assert.IsNotNull(data.memorial);

            // Ömrü 0 sanılıp hemen ölmemeli
            PetSimulator.Simulate(pet, Now, 60);
            Assert.IsFalse(pet.dead);
        }

        [Test]
        public void EverythingCorrupt_StartsNewGame()
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, SaveStore.FileName), "");
            File.WriteAllText(Path.Combine(dir, SaveStore.BackupName), "not json");
            SaveData data = new SaveStore(dir).Load(Now);
            Assert.AreEqual(SaveData.StartingDiamonds, data.diamonds);
        }
    }
}
