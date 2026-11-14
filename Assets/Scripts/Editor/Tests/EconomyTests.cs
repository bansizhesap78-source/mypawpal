using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    /// <summary>Altın ekonomisi, gardırop, mini oyun puanları ve 10 tür (M3).</summary>
    public class EconomyTests
    {
        private static readonly DateTime Noon = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Local);

        private static PetState NewPet(string species = "cat")
        {
            PetState pet = PetState.Adopt(SpeciesCatalog.Get(species), "Pamuk", 0, SpeedMode.Real, new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc));
            pet.energy = 80;
            return pet;
        }

        [Test]
        public void EverySpecies_HasFreeBasicFood_AndThreeCommands()
        {
            foreach (Species s in SpeciesCatalog.All)
            {
                Assert.IsTrue(s.available, s.id + " M3'te açık olmalı");
                Assert.AreEqual(0, FoodCatalog.Get(s.foods[0]).price, s.id + ": ilk yiyecek bedava");
                Assert.AreEqual(3, s.commands.Length, s.id);
            }
        }

        [Test]
        public void Goldfish_NeverThirstyNorNeedsToilet()
        {
            PetState fish = NewPet("goldfish");
            Assert.AreEqual(100, fish.thirst, 0.001);
            PetSimulator.Simulate(fish, Noon, 24 * 3600);
            Assert.AreEqual(100, fish.thirst, 0.001);
            Assert.AreEqual(100, fish.toilet, 0.001);
            Assert.IsFalse(fish.poopOnFloor);
            fish.asleep = false;
            Assert.AreEqual(ActionResult.Done, PetActions.Feed(fish, FoodCatalog.Get("worm")));
            Assert.AreEqual(100, fish.toilet, 0.001);
        }

        [Test]
        public void Turtle_NeedsDropSlower()
        {
            PetState turtle = NewPet("turtle");
            PetState cat = NewPet("cat");
            turtle.fullness = cat.fullness = 100;
            PetSimulator.Simulate(turtle, Noon, 3600);
            PetSimulator.Simulate(cat, Noon, 3600);
            Assert.Greater(turtle.fullness, cat.fullness);
        }

        [Test]
        public void MiniGameReward_DiminishesAfterManyPlays()
        {
            Assert.AreEqual(30, Economy.Reward(MiniGame.Fetch, 15, 0));
            Assert.AreEqual(30, Economy.Reward(MiniGame.Fetch, 99, 4), "skor üst sınırı");
            Assert.AreEqual(30, Economy.Reward(MiniGame.Fetch, 15, Economy.FullPlaysPerDay - 1));
            Assert.AreEqual(15, Economy.Reward(MiniGame.Fetch, 15, Economy.FullPlaysPerDay));
            Assert.AreEqual(7, Economy.Reward(MiniGame.Fetch, 15, Economy.HalfPlaysPerDay));
            Assert.AreEqual(25, Economy.Reward(MiniGame.Hide, 5, 0));
            Assert.AreEqual(0, Economy.Reward(MiniGame.Catch, -3, 0));
        }

        [Test]
        public void FinishGame_PaysGold_CountsDailyPlays_AndTiresPet()
        {
            var save = SaveData.CreateNew(DateTime.UtcNow);
            PetState pet = NewPet();
            int start = save.gold;
            int plays = Economy.FullPlaysPerDay + 1;
            for (int i = 0; i < plays; i++) Economy.FinishGame(save, pet, MiniGame.Catch, 20, Noon);
            Assert.AreEqual(start + Economy.FullPlaysPerDay * 20 + 10, save.gold);
            Assert.AreEqual(plays, save.playsToday);
            Economy.FinishGame(save, pet, MiniGame.Catch, 20, Noon.AddDays(1));
            Assert.AreEqual(1, save.playsToday, "yeni gün sayaç sıfırlanır");
            Assert.Less(pet.energy, 80);
        }

        [Test]
        public void Wardrobe_BuyOnce_ThenEquipAndRemoveFree()
        {
            var save = SaveData.CreateNew(DateTime.UtcNow);
            PetState pet = NewPet();
            ClothingItem hat = Wardrobe.Get("party_hat");
            Assert.AreEqual(WardrobeResult.Bought, Wardrobe.Tap(save, pet, hat));
            Assert.AreEqual(SaveData.StartingGold - hat.price, save.gold);
            Assert.AreEqual("party_hat", pet.hat);
            Assert.AreEqual(WardrobeResult.Removed, Wardrobe.Tap(save, pet, hat));
            Assert.AreEqual("", pet.hat);
            Assert.AreEqual(WardrobeResult.Equipped, Wardrobe.Tap(save, pet, hat));
            Assert.AreEqual(SaveData.StartingGold - hat.price, save.gold, "ikinci kez para alınmaz");

            // Aynı yuvaya başka parça önceki parçanın yerine geçer
            save.gold = 1000;
            Wardrobe.Tap(save, pet, Wardrobe.Get("cap"));
            Assert.AreEqual("cap", pet.hat);
        }

        [Test]
        public void Wardrobe_NoMoney()
        {
            var save = SaveData.CreateNew(DateTime.UtcNow);
            save.gold = 10;
            PetState pet = NewPet();
            Assert.AreEqual(WardrobeResult.NoMoney, Wardrobe.Tap(save, pet, Wardrobe.Get("crown")));
            Assert.AreEqual("", pet.hat);
            Assert.AreEqual(10, save.gold);
        }

        [Test]
        public void Spend_OnlyWhenEnough()
        {
            var save = new SaveData { gold = 5 };
            Assert.IsTrue(Economy.Spend(save, 0));
            Assert.IsFalse(Economy.Spend(save, 6));
            Assert.IsTrue(Economy.Spend(save, 5));
            Assert.AreEqual(0, save.gold);
        }

        [TestCase(0.75f, 3)]
        [TestCase(0.85f, 2)]
        [TestCase(0.5f, 1)]
        [TestCase(0.1f, 0)]
        public void FetchStars_ByDistanceToTarget(float power, int stars)
        {
            Assert.AreEqual(stars, MiniGameRules.FetchStars(power));
        }

        [Test]
        public void MemoryDeck_HasEachPairTwice()
        {
            int[] deck = MiniGameRules.MemoryDeck(MiniGameRules.MemoryPairs, new Random(5));
            var counts = new Dictionary<int, int>();
            foreach (int card in deck)
            {
                counts.TryGetValue(card, out int c);
                counts[card] = c + 1;
            }
            Assert.AreEqual(MiniGameRules.MemoryPairs, counts.Count);
            foreach (int c in counts.Values) Assert.AreEqual(2, c);
            Assert.AreEqual(20, MiniGameRules.MemoryScore(6));
            Assert.AreEqual(5, MiniGameRules.MemoryScore(40));
        }
    }

    /// <summary>Okul: komut eğitimi ve kitap (M3).</summary>
    public class SchoolTests
    {
        private static Species Cat => SpeciesCatalog.Get("cat");

        private static PetState NewCat()
        {
            PetState pet = PetState.Adopt(Cat, "Pamuk", 0, SpeedMode.Real, new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc));
            pet.energy = 100;
            pet.lifespanYears = 15;
            return pet;
        }

        [Test]
        public void LovedPet_LearnsFaster()
        {
            PetState loved = NewCat();
            PetState shy = NewCat();
            loved.love = 100;
            shy.love = 0;
            Assert.Greater(Training.Gain(loved, Cat), Training.Gain(shy, Cat));
        }

        [Test]
        public void Practice_UntilLearned_ThenPerform()
        {
            PetState pet = NewCat();
            pet.love = 50;
            Assert.AreEqual(ActionResult.NotNeeded, Training.Perform(pet, "sit"));
            int lessons = 0;
            while (!Training.Learned(pet, "sit") && lessons < 20)
            {
                pet.energy = 100;
                Assert.AreEqual(ActionResult.Done, Training.Practice(pet, Cat, "sit", true));
                lessons++;
            }
            Assert.IsTrue(Training.Learned(pet, "sit"));
            Assert.That(lessons, Is.InRange(3, 6));
            Assert.AreEqual(1, Training.LearnedCount(pet));
            Assert.AreEqual(ActionResult.Done, Training.Perform(pet, "sit"));
        }

        [Test]
        public void FailedLesson_StillHelpsALittle()
        {
            PetState pet = NewCat();
            Training.Practice(pet, Cat, "paw", false);
            Assert.AreEqual(Training.FailGain, Training.Progress(pet, "paw"), 0.001);
        }

        [Test]
        public void TiredOrSleeping_CannotLearn()
        {
            PetState pet = NewCat();
            pet.energy = 5;
            Assert.AreEqual(ActionResult.Tired, Training.Practice(pet, Cat, "sit", true));
            pet.energy = 50;
            pet.asleep = true;
            Assert.AreEqual(ActionResult.Asleep, Training.Practice(pet, Cat, "sit", true));
        }

        [Test]
        public void Reading_HelpsKittensMore()
        {
            PetState kitten = NewCat();
            PetState adult = NewCat();
            adult.ageSeconds = 3 * 365.25 * 86400;
            kitten.fun = adult.fun = 50;
            Training.Read(kitten, Cat);
            Training.Read(adult, Cat);
            Assert.Greater(kitten.fun, adult.fun);
        }
    }
}
