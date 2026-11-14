using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    public class NotificationPlannerTests
    {
        private static readonly DateTime Morning = new DateTime(2026, 10, 26, 10, 0, 0, DateTimeKind.Local);

        private static PetState NewCat()
        {
            PetState pet = PetState.Adopt(SpeciesCatalog.Get("cat"), "Pamuk", 0, SpeedMode.Real, new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc));
            pet.fullness = pet.thirst = pet.energy = pet.hygiene = pet.toilet = pet.fun = 100;
            pet.lifespanYears = 15;
            pet.rng = 777;
            return pet;
        }

        [Test]
        public void HungrySoon_NotifiesWhenFoodRunsLow()
        {
            PetState pet = NewCat();
            pet.fullness = 60;   // saatte 6 düşer: 5 saat sonra 30'un altı
            List<PlannedNotification> plan = NotificationPlanner.Plan(new[] { pet }, Morning, false);
            PlannedNotification hungry = plan.Find(n => n.bodyKey == "notify_hungry");
            Assert.AreEqual("Pamuk", hungry.petName);
            Assert.That((hungry.at - Morning).TotalHours, Is.InRange(5.0, 5.3));
            Assert.AreEqual(60, pet.fullness, 0.001, "planlama asıl dostu değiştirmez");
        }

        [Test]
        public void QuietHours_MoveToMorning()
        {
            Assert.AreEqual(new DateTime(2026, 10, 27, 8, 0, 0), NotificationPlanner.OutOfQuiet(new DateTime(2026, 10, 26, 23, 10, 0)));
            Assert.AreEqual(new DateTime(2026, 10, 27, 8, 0, 0), NotificationPlanner.OutOfQuiet(new DateTime(2026, 10, 27, 3, 0, 0)));
            Assert.AreEqual(new DateTime(2026, 10, 26, 15, 0, 0), NotificationPlanner.OutOfQuiet(new DateTime(2026, 10, 26, 15, 0, 0)));
        }

        [Test]
        public void NothingDuringQuietHours()
        {
            PetState pet = NewCat();
            pet.fullness = 45;
            pet.toilet = 40;
            foreach (PlannedNotification n in NotificationPlanner.Plan(new[] { pet }, new DateTime(2026, 10, 26, 20, 0, 0), false))
                if (!n.critical) Assert.IsFalse(NotificationPlanner.IsQuiet(n.at), n.bodyKey + " " + n.at);
        }

        [Test]
        public void CriticalPet_GetsThreeWarningsBeforeDeath()
        {
            PetState pet = NewCat();
            pet.health = 10;
            pet.disease = DiseaseCatalog.Cold;
            List<PlannedNotification> plan = NotificationPlanner.Plan(new[] { pet }, Morning, false);
            List<PlannedNotification> critical = plan.FindAll(n => n.critical);
            Assert.AreEqual(3, critical.Count);
            DateTime death = Morning.AddHours(PetSimulator.DeathAfterCriticalHours);
            foreach (PlannedNotification n in critical)
            {
                Assert.Less(n.at, death);
                Assert.Greater(n.at, Morning);
            }
        }

        [Test]
        public void CriticalPet_NoDeathSetting_SingleReminder()
        {
            PetState pet = NewCat();
            pet.health = 10;
            List<PlannedNotification> plan = NotificationPlanner.Plan(new[] { pet }, Morning, true);
            Assert.AreEqual(1, plan.FindAll(n => n.critical).Count);
        }

        [Test]
        public void AtMostThreePerDay()
        {
            var pets = new List<PetState>();
            for (int i = 0; i < 3; i++)
            {
                PetState pet = NewCat();
                pet.name = "Dost" + i;
                pet.fullness = 40;
                pet.toilet = 35;
                pets.Add(pet);
            }
            var perDay = new Dictionary<DateTime, int>();
            foreach (PlannedNotification n in NotificationPlanner.Plan(pets, Morning, false))
            {
                perDay.TryGetValue(n.at.Date, out int c);
                perDay[n.at.Date] = c + 1;
            }
            foreach (int count in perDay.Values) Assert.LessOrEqual(count, NotificationPlanner.MaxPerDay);
        }

        [Test]
        public void DeadPets_AreSkipped()
        {
            PetState pet = NewCat();
            pet.dead = true;
            pet.fullness = 10;
            Assert.IsEmpty(NotificationPlanner.Plan(new[] { pet }, Morning, false));
        }
    }
}
