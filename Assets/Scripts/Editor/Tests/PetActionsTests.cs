using System;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    public class PetActionsTests
    {
        private static PetState NewCat()
        {
            return PetState.Adopt(SpeciesCatalog.Get("cat"), "Pamuk", 0, SpeedMode.Real, new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void Walk_IsFun_ButNeedsEnergy()
        {
            PetState pet = NewCat();
            pet.fun = 40;
            pet.energy = 80;
            pet.hygiene = 80;
            Assert.AreEqual(ActionResult.Done, PetActions.Walk(pet));
            Assert.Greater(pet.fun, 60.0);
            Assert.Less(pet.energy, 80.0);
            Assert.Less(pet.hygiene, 80.0);

            pet.energy = PetActions.WalkMinEnergy - 1;
            Assert.AreEqual(ActionResult.Tired, PetActions.Walk(pet));
            pet.energy = 80;
            pet.asleep = true;
            Assert.AreEqual(ActionResult.Asleep, PetActions.Walk(pet));
        }

        [Test]
        public void Closet_RemoveAll_KeepsItemsOwned()
        {
            PetState pet = NewCat();
            var save = new SaveData { gold = 1000 };
            Wardrobe.Tap(save, pet, Wardrobe.Get("crown"));
            Wardrobe.Tap(save, pet, Wardrobe.Get("sweater"));
            Assert.AreEqual("crown", pet.hat);
            Assert.AreEqual("sweater", pet.outfit);
            Assert.AreEqual(2, Wardrobe.OwnedCount(save));

            Wardrobe.RemoveAll(pet);
            Assert.AreEqual("", pet.hat);
            Assert.AreEqual("", pet.outfit);
            Assert.AreEqual(2, Wardrobe.OwnedCount(save), "çıkarılan parça dolapta kalır");
            Assert.AreEqual(WardrobeResult.Equipped, Wardrobe.Tap(save, pet, Wardrobe.Get("crown")), "yeniden giymek bedava");
        }

        [Test]
        public void Feed_FillsAndMakesToiletNeed()
        {
            PetState pet = NewCat();
            pet.fullness = 40;
            pet.toilet = 80;
            Food food = FoodCatalog.Get("pet_food");
            Assert.AreEqual(ActionResult.Done, PetActions.Feed(pet, food));
            Assert.AreEqual(40 + food.fullness, pet.fullness, 0.001);
            Assert.AreEqual(80 - food.toilet, pet.toilet, 0.001);
        }

        [Test]
        public void Feed_WhenFull_AddsWeight()
        {
            PetState pet = NewCat();
            pet.fullness = 98;
            double weight = pet.weight;
            Assert.AreEqual(ActionResult.Overfed, PetActions.Feed(pet, FoodCatalog.Get("pet_food")));
            Assert.AreEqual(100, pet.fullness, 0.001);
            Assert.AreEqual(weight + PetActions.OverfedWeight, pet.weight, 0.001);
        }

        [Test]
        public void SleepingPet_CannotEatOrPlay()
        {
            PetState pet = NewCat();
            pet.energy = 40;
            Assert.AreEqual(ActionResult.Done, PetActions.Sleep(pet));
            Assert.AreEqual(ActionResult.Asleep, PetActions.Feed(pet, FoodCatalog.Get("fish")));
            Assert.AreEqual(ActionResult.Asleep, PetActions.Play(pet));
            Assert.AreEqual(ActionResult.Done, PetActions.Wake(pet));
            Assert.IsFalse(pet.asleep);
        }

        [Test]
        public void RestedPet_WontSleep()
        {
            PetState pet = NewCat();
            pet.energy = 100;
            Assert.AreEqual(ActionResult.NotNeeded, PetActions.Sleep(pet));
            Assert.IsFalse(pet.asleep);
        }

        [Test]
        public void Toilet_OnlyWhenNeeded()
        {
            PetState pet = NewCat();
            pet.toilet = 95;
            Assert.AreEqual(ActionResult.NotNeeded, PetActions.UseToilet(pet));
            pet.toilet = 30;
            Assert.AreEqual(ActionResult.Done, PetActions.UseToilet(pet));
            Assert.AreEqual(100, pet.toilet, 0.001);
        }

        [Test]
        public void CleanPoop_RemovesIt()
        {
            PetState pet = NewCat();
            Assert.AreEqual(ActionResult.NotNeeded, PetActions.CleanPoop(pet));
            pet.poopOnFloor = true;
            Assert.AreEqual(ActionResult.Done, PetActions.CleanPoop(pet));
            Assert.IsFalse(pet.poopOnFloor);
        }

        [Test]
        public void Play_NeedsEnergy_AndIsFun()
        {
            PetState pet = NewCat();
            pet.energy = 10;
            Assert.AreEqual(ActionResult.Tired, PetActions.Play(pet));
            pet.energy = 60;
            pet.fun = 30;
            Assert.AreEqual(ActionResult.Done, PetActions.Play(pet));
            Assert.AreEqual(50, pet.fun, 0.001);
            Assert.AreEqual(50, pet.energy, 0.001);
        }

        [Test]
        public void Values_StayInRange()
        {
            PetState pet = NewCat();
            for (int i = 0; i < 20; i++)
            {
                PetActions.Feed(pet, FoodCatalog.Get("treat"));
                PetActions.Stroke(pet);
                PetActions.Brush(pet);
            }
            Assert.AreEqual(100, pet.love, 0.001);
            Assert.AreEqual(100, pet.fun, 0.001);
            Assert.That(pet.weight, Is.InRange(0.0, 100.0));
        }

        [Test]
        public void Mood_ShowsMostUrgentNeed()
        {
            PetState pet = NewCat();
            pet.fullness = pet.thirst = pet.energy = pet.hygiene = pet.toilet = pet.fun = pet.health = 90;
            pet.love = 80;
            Assert.AreEqual(MoodKind.Funny, Mood.Of(pet));
            pet.fun = 60;
            Assert.AreEqual(MoodKind.Happy, Mood.Of(pet));
            pet.fullness = 20;
            Assert.AreEqual(MoodKind.Hungry, Mood.Of(pet));
            pet.toilet = 10;
            Assert.AreEqual(MoodKind.NeedsToilet, Mood.Of(pet), "tuvalet açlıktan acildir");
            pet.health = 20;
            Assert.AreEqual(MoodKind.Sick, Mood.Of(pet));
            pet.asleep = true;
            Assert.AreEqual(MoodKind.Sleeping, Mood.Of(pet));
        }

        [Test]
        public void PoopOnFloor_LowersHappiness()
        {
            PetState pet = NewCat();
            double before = Mood.Happiness(pet);
            pet.poopOnFloor = true;
            Assert.AreEqual(before - 15, Mood.Happiness(pet), 0.001);
        }
    }
}
