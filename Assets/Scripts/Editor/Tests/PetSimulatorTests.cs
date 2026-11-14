using System;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    public class PetSimulatorTests
    {
        private static readonly DateTime Noon = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Local);
        private static readonly DateTime Midnight = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Local);
        private static Species Cat => SpeciesCatalog.Get("cat");

        private static PetState NewCat(SpeedMode mode = SpeedMode.Real)
        {
            PetState pet = PetState.Adopt(Cat, "Pamuk", 0, mode, new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc));
            pet.fullness = pet.thirst = pet.energy = pet.hygiene = pet.toilet = pet.fun = 100;
            return pet;
        }

        [Test]
        public void DayHour_DropsNeedsAtDayRates()
        {
            PetState pet = NewCat();
            PetSimulator.Simulate(pet, Noon, 3600);
            NeedRates r = Cat.rates;
            Assert.AreEqual(100 - r.fullness, pet.fullness, 0.001);
            Assert.AreEqual(100 - r.thirst, pet.thirst, 0.001);
            Assert.AreEqual(100 - r.fun, pet.fun, 0.001);
            Assert.AreEqual(100 - r.energyAwake, pet.energy, 0.001);
            Assert.IsFalse(pet.asleep);
        }

        [Test]
        public void Night_SleepingPetNeedsLess()
        {
            PetState pet = NewCat();
            pet.energy = 50;
            PetSimulator.Simulate(pet, Midnight, 3600);
            Assert.IsTrue(pet.asleep, "gece yorgun kedi kendiliğinden uyur");
            Assert.AreEqual(100 - Cat.rates.fullness * GameClock.NightNeedsFactor, pet.fullness, 0.01);
            Assert.Greater(pet.energy, 50);
        }

        [Test]
        public void Morning_WakesUpWhenRested()
        {
            PetState pet = NewCat();
            pet.energy = 20;   // 15'in altı soğuk algınlığı riski getirir; bu test yalnızca uykuya bakar
            // 22:00'den 12:00'ye kadar: gece uyur, enerji dolar, sabah 7'de uyanır ve 5 saat uyanık kalır
            PetSimulator.Simulate(pet, new DateTime(2026, 10, 26, 22, 0, 0, DateTimeKind.Local), 14 * 3600);
            Assert.IsFalse(pet.asleep);
            Assert.AreEqual(100 - 5 * Cat.rates.energyAwake, pet.energy, 0.5);
        }

        [Test]
        public void FastMode_AgesFiveTimesButNeedsStayRealTime()
        {
            PetState real = NewCat(SpeedMode.Real);
            PetState fast = NewCat(SpeedMode.Fast);
            double startAge = real.ageSeconds;
            PetSimulator.Simulate(real, Noon, 3600);
            PetSimulator.Simulate(fast, Noon, 3600);
            Assert.AreEqual(3600, real.ageSeconds - startAge, 0.001);
            Assert.AreEqual(5 * 3600, fast.ageSeconds - startAge, 0.001);
            Assert.AreEqual(real.fullness, fast.fullness, 0.001);
        }

        [Test]
        public void ToiletAccident_LeavesPoopAndDirt()
        {
            PetState pet = NewCat();
            pet.toilet = 1;
            PetSimulator.Simulate(pet, Noon, 600);
            Assert.IsTrue(pet.poopOnFloor);
            Assert.AreEqual(100, pet.toilet, 2.0);
            Assert.Less(pet.hygiene, 100 - PetSimulator.AccidentHygieneLoss + 1);
        }

        [Test]
        public void Neglect_HurtsHealth_GoodCareHeals()
        {
            PetState pet = NewCat();
            pet.fullness = 0;
            pet.thirst = 0;
            PetSimulator.Simulate(pet, Noon, 3600);
            Assert.AreEqual(100 - 2 * PetSimulator.HealthLossPerRedNeed, pet.health, 0.01);

            // 60 ve üstünde soğuk algınlığı zarı atılmaz (zayıf dost risklidir; o zar rastgele tohumla test kırılırdı)
            PetState healthy = NewCat();
            healthy.health = 60;
            PetSimulator.Simulate(healthy, Noon, 3600);
            Assert.AreEqual(60 + PetSimulator.HealthRegen, healthy.health, 0.01);
        }

        [Test]
        public void Night_WokenByChild_StaysAwakeThenSleepsAgain()
        {
            PetState pet = NewCat();
            pet.energy = 50;
            PetSimulator.Simulate(pet, Midnight, 600);
            Assert.IsTrue(pet.asleep, "gece yorgun kedi kendiliğinden uyur");

            Assert.AreEqual(ActionResult.Done, PetActions.Wake(pet));
            PetSimulator.Simulate(pet, Midnight, 3600);
            Assert.IsFalse(pet.asleep, "çocuk uyandırdı: bir süre gece de uyanık kalır, oyun engellenmez");

            PetSimulator.Simulate(pet, Midnight.AddHours(1), PetSimulator.WokenAwakeSeconds);
            Assert.IsTrue(pet.asleep, "süre dolunca yorgun dost yine uyur");
        }

        [Test]
        public void ThreeDaysAlone_StaysInRange()
        {
            PetState pet = NewCat();
            PetSimulator.Simulate(pet, Noon, GameClock.CatchUpSeconds(10 * 24 * 3600));
            foreach (double v in new[] { pet.fullness, pet.thirst, pet.energy, pet.hygiene, pet.toilet, pet.fun, pet.health, pet.love, pet.weight })
            {
                Assert.IsFalse(double.IsNaN(v));
                Assert.That(v, Is.InRange(0.0, 100.0));
            }
            Assert.AreEqual(0, pet.fullness, 0.001, "3 gün yalnız kalan kedi acıkır");
            Assert.Less(pet.health, 100);
        }

        [Test]
        public void ResultDoesNotDependOnStepSize()
        {
            PetState once = NewCat();
            PetState minutes = NewCat();
            PetSimulator.Simulate(once, Noon, 4 * 3600);
            for (int i = 0; i < 240; i++) PetSimulator.Simulate(minutes, Noon.AddMinutes(i), 60);
            Assert.AreEqual(once.fullness, minutes.fullness, 0.01);
            Assert.AreEqual(once.energy, minutes.energy, 0.01);
            Assert.AreEqual(once.ageSeconds, minutes.ageSeconds, 0.01);
        }

        [Test]
        public void KittenGrowsIntoAdult()
        {
            PetState pet = NewCat();
            pet.lifespanYears = 15;
            Assert.AreEqual(LifeStage.Baby, Mood.Stage(pet, Cat), "8 haftalık yavru");
            Assert.Less(Mood.Scale(pet, Cat), 0.6f);
            pet.ageSeconds = 0.5 * 365.25 * 86400;
            Assert.AreEqual(LifeStage.Young, Mood.Stage(pet, Cat));
            pet.ageSeconds = 1.2 * 365.25 * 86400;
            Assert.AreEqual(LifeStage.Adult, Mood.Stage(pet, Cat));
            Assert.AreEqual(1f, Mood.Scale(pet, Cat), 0.0001f);
            pet.ageSeconds = 13 * 365.25 * 86400;
            Assert.AreEqual(LifeStage.Senior, Mood.Stage(pet, Cat));
        }

        [Test]
        public void EverySpeciesHasSaneData()
        {
            foreach (Species s in SpeciesCatalog.All)
            {
                Assert.Greater(s.lifespanYears, s.adultYears, s.id);
                Assert.Less(s.adoptAgeDays / 365.25, s.adultYears, s.id);
                foreach (string food in s.foods) Assert.IsNotNull(FoodCatalog.Get(food), s.id + ": " + food);
            }
        }
    }
}
