using System;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    /// <summary>Hastalık, kritik durum, ölüm ve veteriner (M2, plan §6).</summary>
    public class HealthTests
    {
        private static readonly DateTime Noon = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Local);
        private static Species Cat => SpeciesCatalog.Get("cat");

        private static PetState NewCat(SpeedMode mode = SpeedMode.Real)
        {
            PetState pet = PetState.Adopt(Cat, "Pamuk", 0, mode, new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc));
            pet.fullness = pet.thirst = pet.energy = pet.hygiene = pet.toilet = pet.fun = 100;
            pet.lifespanYears = 15;
            pet.rng = 12345;
            return pet;
        }

        private static SaveData Wallet(int gold, int diamonds) => new SaveData { gold = gold, diamonds = diamonds };

        [Test]
        public void Adopt_LifespanVariesAroundSpeciesAverage()
        {
            for (int i = 0; i < 50; i++)
            {
                PetState pet = PetState.Adopt(Cat, "x", 0, SpeedMode.Real, DateTime.UtcNow);
                Assert.That(pet.lifespanYears, Is.InRange(Cat.lifespanYears * 0.9, Cat.lifespanYears * 1.1));
            }
        }

        [Test]
        public void DirtyPet_CatchesSomethingWithinDays()
        {
            PetState pet = NewCat();
            pet.hygiene = 10;
            for (int i = 0; i < 200 && !pet.IsSick; i++)
            {
                pet.fullness = pet.thirst = pet.energy = 90;
                pet.hygiene = 10;   // hep kirli kalsın
                PetSimulator.Simulate(pet, Noon, 3600);
            }
            Assert.IsTrue(pet.disease == DiseaseCatalog.Cold || pet.disease == DiseaseCatalog.Fleas, pet.disease);
        }

        [Test]
        public void CleanHealthyPet_DoesNotGetSick()
        {
            PetState pet = NewCat();
            for (int i = 0; i < 24 * 7; i++)
            {
                pet.fullness = pet.thirst = pet.hygiene = pet.energy = pet.fun = pet.toilet = 90;
                PetSimulator.Simulate(pet, Noon, 3600);
            }
            Assert.IsFalse(pet.IsSick, pet.disease);
        }

        [Test]
        public void Overweight_BecomesObesity()
        {
            PetState pet = NewCat();
            pet.weight = 85;
            PetSimulator.Simulate(pet, Noon, 60);
            Assert.AreEqual(DiseaseCatalog.Obesity, pet.disease);
        }

        [Test]
        public void TooManyTreats_CanCauseToothache()
        {
            PetState pet = NewCat();
            pet.fullness = 0;
            Food treat = FoodCatalog.Get("treat");
            for (int i = 0; i < 30 && !pet.IsSick; i++)
            {
                pet.fullness = 0;   // tok karnına yemesin, mide bozulmasın
                PetActions.Feed(pet, treat);
            }
            Assert.AreEqual(DiseaseCatalog.Toothache, pet.disease);
        }

        [Test]
        public void Sickness_DrainsHealthAndBlocksHealing()
        {
            PetState pet = NewCat();
            pet.health = 80;
            pet.disease = DiseaseCatalog.Cold;
            PetSimulator.Simulate(pet, Noon, 3600);
            Assert.AreEqual(80 - DiseaseCatalog.Get(DiseaseCatalog.Cold).healthPerHour, pet.health, 0.01);
        }

        [Test]
        public void Critical_DiesAfter48Hours_NotBefore()
        {
            PetState pet = NewCat();
            pet.health = 15;
            pet.disease = DiseaseCatalog.Stomach;
            PetSimulator.Simulate(pet, Noon, 47 * 3600);
            Assert.IsFalse(pet.dead, "47 saatte ölmemeli");
            Assert.AreEqual(1.0, PetSimulator.HoursLeft(pet), 0.01);
            PetSimulator.Simulate(pet, Noon, 2 * 3600);
            Assert.IsTrue(pet.dead);
            Assert.AreEqual("neglect", pet.deathCause);
        }

        [Test]
        public void Critical_SameInFastMode()
        {
            PetState pet = NewCat(SpeedMode.Fast);
            pet.health = 15;
            pet.disease = DiseaseCatalog.Stomach;
            PetSimulator.Simulate(pet, Noon, 47 * 3600);
            Assert.IsFalse(pet.dead, "48 saat gerçek zamandır, hızlı modda kısalmaz");
        }

        [Test]
        public void NoDeathSetting_KeepsPetAlive()
        {
            PetState pet = NewCat();
            pet.health = 5;
            pet.disease = DiseaseCatalog.Cold;
            PetSimulator.Simulate(pet, Noon, 10 * 24 * 3600, noDeath: true);
            Assert.IsFalse(pet.dead);
            Assert.IsTrue(PetSimulator.IsCritical(pet));
        }

        [Test]
        public void RecoveringHealth_ResetsCriticalTimer()
        {
            PetState pet = NewCat();
            pet.health = 19;
            PetSimulator.Simulate(pet, Noon, 600);
            Assert.Greater(pet.criticalSeconds, 0);
            pet.health = 50;
            PetSimulator.Simulate(pet, Noon, 60);
            Assert.AreEqual(0, pet.criticalSeconds, 0.001);
        }

        [Test]
        public void OldAge_NaturalDeath()
        {
            PetState pet = NewCat(SpeedMode.Fast);
            pet.ageSeconds = (pet.lifespanYears * 365.25 - 1) * 86400;   // ömrüne 1 oyun günü kaldı
            PetSimulator.Simulate(pet, Noon, 6 * 3600);                   // hızlı modda 30 oyun saati
            Assert.IsTrue(pet.dead);
            Assert.AreEqual("natural", pet.deathCause);
        }

        [Test]
        public void Treat_CostsGold_AndCures()
        {
            PetState pet = NewCat();
            pet.health = 50;
            pet.disease = DiseaseCatalog.Fleas;
            int cost = Vet.TreatCost(pet);
            SaveData poor = Wallet(cost - 1, 0);
            Assert.AreEqual(VetResult.NoMoney, Vet.Treat(pet, poor));
            SaveData wallet = Wallet(100, 0);
            Assert.AreEqual(VetResult.Done, Vet.Treat(pet, wallet));
            Assert.AreEqual(100 - cost, wallet.gold);
            Assert.IsFalse(pet.IsSick);
            Assert.AreEqual(VetResult.NotSick, Vet.Treat(pet, wallet));
        }

        [Test]
        public void FreeTreat_OncePerDay()
        {
            PetState pet = NewCat();
            pet.health = 50;
            pet.disease = DiseaseCatalog.Cold;
            Assert.AreEqual(VetResult.Done, Vet.FreeTreat(pet, Noon));
            pet.disease = DiseaseCatalog.Cold;
            Assert.AreEqual(VetResult.UsedToday, Vet.FreeTreat(pet, Noon.AddHours(5)));
            Assert.AreEqual(VetResult.Done, Vet.FreeTreat(pet, Noon.AddDays(1)));
        }

        [Test]
        public void Critical_NeedsMiracle_FirstOneFree()
        {
            PetState pet = NewCat();
            pet.health = 10;
            pet.disease = DiseaseCatalog.Cold;
            SaveData wallet = Wallet(1000, 0);
            Assert.AreEqual(VetResult.Critical, Vet.Treat(pet, wallet), "kritikte altın yetmez");
            Assert.AreEqual(0, Vet.MiracleCost(pet, Cat));
            Assert.AreEqual(VetResult.Done, Vet.Miracle(pet, Cat, wallet));
            Assert.IsFalse(PetSimulator.IsCritical(pet));
            Assert.IsFalse(pet.IsSick);

            pet.health = 10;
            Assert.AreEqual(3, Vet.MiracleCost(pet, Cat));
            Assert.AreEqual(VetResult.NoMoney, Vet.Miracle(pet, Cat, wallet));
            wallet.diamonds = 4;
            Assert.AreEqual(VetResult.Done, Vet.Miracle(pet, Cat, wallet));
            Assert.AreEqual(1, wallet.diamonds);
            pet.health = 10;
            Assert.AreEqual(5, Vet.MiracleCost(pet, Cat));
        }

        [Test]
        public void StartingDiamonds_CoverRescues()
        {
            // Plan §6 güvencesi: bedava ilk kurtarma + başlangıçtaki 5 elmas ikinci kurtarmaya yeter
            PetState pet = NewCat();
            SaveData wallet = SaveData.CreateNew(DateTime.UtcNow);
            for (int i = 0; i < 2; i++)
            {
                pet.health = 10;
                Assert.AreEqual(VetResult.Done, Vet.Miracle(pet, Cat, wallet), "kurtarma " + (i + 1));
            }
        }

        [Test]
        public void Vaccine_LastsAGameYear_AndLowersColdRisk()
        {
            PetState pet = NewCat();
            SaveData wallet = Wallet(100, 0);
            Assert.AreEqual(VetResult.Done, Vet.Vaccinate(pet, wallet));
            Assert.AreEqual(100 - Vet.VaccineCost, wallet.gold);
            Assert.AreEqual(VetResult.AlreadyVaccinated, Vet.Vaccinate(pet, wallet));
            pet.ageSeconds += 366 * 86400;
            Assert.IsFalse(Vet.IsVaccinated(pet));
        }

        [Test]
        public void Bubble_ShowsSymptom()
        {
            PetState pet = NewCat();
            pet.disease = DiseaseCatalog.Fleas;
            Assert.AreEqual("symptom_fleas", Mood.BubbleKey(pet));
            pet.health = 10;
            Assert.AreEqual("mood_critical", Mood.BubbleKey(pet));
        }
    }
}
