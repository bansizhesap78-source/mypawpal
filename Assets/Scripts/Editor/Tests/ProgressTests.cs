using System;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    /// <summary>Seri, görevler, rozetler, Pass, kulüp, şov, yavru, tema ve etkinlikler (M5).</summary>
    public class ProgressTests
    {
        private static readonly DateTime Day1 = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Local);

        private static SaveData NewSave() => SaveData.CreateNew(DateTime.UtcNow);

        private static PetState Pet(SaveData save, string species = "cat", string name = "Pamuk")
        {
            PetState pet = PetState.Adopt(SpeciesCatalog.Get(species), name, 0, SpeedMode.Real, DateTime.UtcNow);
            pet.lifespanYears = SpeciesCatalog.Get(species).lifespanYears;
            save.pets.Add(pet);
            save.adoptions++;
            return pet;
        }

        // ---------- Seri ve görevler ----------

        [Test]
        public void Streak_CountsConsecutiveDays_ResetsAfterGap()
        {
            SaveData save = NewSave();
            int gold = save.gold;
            Assert.IsTrue(Progress.RecordCare(save, Day1));
            Assert.IsFalse(Progress.RecordCare(save, Day1.AddHours(3)), "aynı gün bir kez");
            Assert.AreEqual(1, save.streakDays);
            Assert.AreEqual(gold + Progress.StreakGold, save.gold);
            for (int d = 1; d < 7; d++) Progress.RecordCare(save, Day1.AddDays(d));
            Assert.AreEqual(7, save.streakDays);
            Assert.AreEqual(SaveData.StartingDiamonds + 1, save.diamonds, "7. günde 1 elmas");
            Progress.RecordCare(save, Day1.AddDays(9));
            Assert.AreEqual(1, save.streakDays, "gün atlanınca seri baştan");
        }

        [Test]
        public void DailyTasks_ThreeDistinct_SameForSameDay()
        {
            SaveData a = NewSave(), b = NewSave();
            Progress.EnsureDay(a, Day1);
            Progress.EnsureDay(b, Day1.AddHours(5));
            Assert.AreEqual(Progress.TasksPerDay, a.taskIds.Count);
            CollectionAssert.AllItemsAreUnique(a.taskIds);
            CollectionAssert.AreEqual(a.taskIds, b.taskIds);
        }

        [Test]
        public void Task_CompletesAndPaysOnce()
        {
            SaveData save = NewSave();
            Progress.EnsureDay(save, Day1);
            string id = save.taskIds[0];
            int target = Progress.Task(id).target;
            Assert.IsFalse(Progress.Claim(save, 0, Day1), "bitmeden alınmaz");
            for (int i = 0; i < target; i++) Progress.Count(save, id, Day1);
            int gold = save.gold;
            Assert.IsTrue(Progress.Claim(save, 0, Day1));
            Assert.AreEqual(gold + Progress.TaskGold, save.gold);
            Assert.IsFalse(Progress.Claim(save, 0, Day1));
            Assert.Greater(save.passXp, 0);
        }

        [Test]
        public void Badges_GivenOnceWithReward()
        {
            SaveData save = NewSave();
            Pet(save);
            int gold = save.gold;
            var earned = Progress.CheckBadges(save);
            Assert.IsTrue(earned.Exists(b => b.id == "first_pal"));
            Assert.Greater(save.gold, gold);
            Assert.IsEmpty(Progress.CheckBadges(save), "ikinci kez verilmez");
        }

        // ---------- Pati Pass ----------

        [Test]
        public void Pass_LevelsUp_PremiumNeedsPurchase_ResetsNextMonth()
        {
            SaveData save = NewSave();
            PatiPass.AddXp(save, PatiPass.XpPerLevel * 2, Day1);
            Assert.AreEqual(2, PatiPass.Level(save));
            Assert.IsTrue(PatiPass.Claim(save, 1, false, Day1));
            Assert.IsFalse(PatiPass.Claim(save, 1, false, Day1), "bir kez");
            Assert.IsFalse(PatiPass.Claim(save, 3, false, Day1), "basamağa ulaşılmadı");
            Assert.IsFalse(PatiPass.Claim(save, 2, true, Day1), "premium alınmadı");

            Shop.Grant(save, ProductCatalog.Get(ProductCatalog.Pass), "p", Day1.ToUniversalTime());
            Assert.IsTrue(PatiPass.Claim(save, 2, true, Day1));
            Assert.Contains("flower_crown", save.ownedItems, "2. premium basamak çiçek taç");

            PatiPass.AddXp(save, 10, Day1.AddMonths(1));
            Assert.AreEqual(0, PatiPass.Level(save), "yeni sezon");
            Assert.IsFalse(PatiPass.HasPremium(save, Day1.AddMonths(1)));
        }

        // ---------- Pati Kulübü ----------

        [Test]
        public void Club_SubscriptionActivates_DailyDiamond_GoldBonus_Lapses()
        {
            SaveData save = NewSave();
            DateTime utc = Day1.ToUniversalTime();
            Assert.IsTrue(Shop.Grant(save, ProductCatalog.Get(ProductCatalog.ClubMonthly), "sub", utc));
            Assert.IsTrue(Club.Active(save, utc));
            Assert.IsTrue(Shop.Grant(save, ProductCatalog.Get(ProductCatalog.ClubMonthly), "sub", utc) == false, "yeniden doğrulama yeni hak değil");
            Assert.IsTrue(Club.Active(save, utc.AddDays(30)), "doğrulama uzadı");
            Assert.Contains(Club.Item, save.ownedItems);

            int diamonds = save.diamonds;
            Assert.IsTrue(Club.ClaimDiamond(save, Day1));
            Assert.IsFalse(Club.ClaimDiamond(save, Day1.AddHours(2)));
            Assert.AreEqual(diamonds + 1, save.diamonds);

            PetState pet = Pet(save);
            pet.energy = 100;
            int gold = save.gold;
            Economy.FinishGame(save, pet, MiniGame.Catch, 20, Day1);
            Assert.AreEqual(gold + 30, save.gold, "+%50");

            Assert.IsFalse(Club.Active(save, utc.AddDays(Club.VerifyDays + 1)), "doğrulanmazsa kapanır");
            Club.Lapse(save);
            Assert.IsFalse(Club.Active(save, utc));
        }

        // ---------- Pet Şov ----------

        [Test]
        public void Show_RivalsVisibleBeforehand_OncePerWeek()
        {
            SaveData save = NewSave();
            PetState pet = Pet(save);
            int[] before = PetShow.RivalScores(Day1, false);
            CollectionAssert.AreEqual(before, PetShow.RivalScores(Day1.AddHours(5), false), "hafta boyunca sabit");

            ShowResult result = PetShow.Enter(save, pet, false, Day1);
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(before, result.scores, "katılmadan önce görülen puanlar");
            int expectedPlace = 1;
            foreach (int s in before) if (s > PetShow.Score(pet)) expectedPlace++;
            Assert.AreEqual(expectedPlace, result.place);
            Assert.IsNull(PetShow.Enter(save, pet, false, Day1), "haftada bir");
            Assert.IsNotNull(PetShow.Enter(save, pet, false, Day1.AddDays(7)));
        }

        [Test]
        public void StarShow_CostsDiamonds_FreeForClub()
        {
            SaveData save = NewSave();
            PetState pet = Pet(save);
            save.diamonds = 2;
            Assert.IsFalse(PetShow.CanEnter(save, true, Day1));
            save.diamonds = 3;
            int beforeDiamonds = save.diamonds;
            ShowResult result = PetShow.Enter(save, pet, true, Day1);
            Assert.AreEqual(beforeDiamonds - PetShow.StarEntryDiamonds + result.diamonds, save.diamonds);

            SaveData club = NewSave();
            club.diamonds = 0;
            Club.Confirm(club, Day1.ToUniversalTime());
            Assert.IsTrue(PetShow.CanEnter(club, true, Day1));
        }

        [Test]
        public void ShowScore_RewardsCareTricksAndStyle()
        {
            SaveData save = NewSave();
            PetState plain = Pet(save);
            PetState star = Pet(save, "cat", "Yıldız");
            star.love = 100;
            star.hat = "crown";
            star.neck = "bow_tie";
            star.commands.Add(new CommandProgress { id = "sit", progress = 100 });
            Assert.Greater(PetShow.Score(star), PetShow.Score(plain));
        }

        // ---------- Yavru ----------

        [Test]
        public void Breeding_NeedsAdultLovedPair_AndFreeSlot()
        {
            SaveData save = NewSave();
            save.ownedProducts.Add(ProductCatalog.Slot2);
            save.ownedProducts.Add(ProductCatalog.Slot3);
            PetState a = Pet(save, "cat", "Pamuk");
            PetState b = Pet(save, "cat", "Boncuk");
            Assert.AreEqual(BreedResult.NotAdult, Breeding.Check(save, a, b));
            a.ageSeconds = b.ageSeconds = 2 * 365.25 * 86400;
            a.love = b.love = 50;
            Assert.AreEqual(BreedResult.NotLoved, Breeding.Check(save, a, b));
            a.love = b.love = 90;
            Assert.AreEqual(BreedResult.Done, Breeding.Check(save, a, b));
            Assert.AreEqual(BreedResult.DifferentSpecies, Breeding.Check(save, a, Pet(save, "dog", "Karabaş")));
        }

        [Test]
        public void Breeding_MakesBabyWithParents_CostsAndCoolsDown()
        {
            SaveData save = NewSave();
            save.ownedProducts.Add(ProductCatalog.Slot2);
            save.ownedProducts.Add(ProductCatalog.Slot3);
            PetState a = Pet(save, "cat", "Pamuk");
            PetState b = Pet(save, "cat", "Boncuk");
            a.ageSeconds = b.ageSeconds = 2 * 365.25 * 86400;
            a.love = b.love = 90;
            a.color = 1;
            b.color = 2;
            int diamonds = save.diamonds;
            PetState baby = Breeding.Breed(save, a, b, "Minik", true, DateTime.UtcNow, out BreedResult result);
            Assert.AreEqual(BreedResult.Done, result);
            Assert.AreEqual("Pamuk & Boncuk", baby.parents);
            Assert.AreEqual(2, baby.generation);
            Assert.That(baby.color, Is.EqualTo(1).Or.EqualTo(2));
            Assert.AreEqual(diamonds - Breeding.DiamondCost, save.diamonds);
            Assert.AreEqual(3, save.pets.Count);
            Assert.AreEqual(BreedResult.NoSlot, Breeding.Check(save, a, b), "slot doldu");
            save.pets.Remove(baby);
            Assert.AreEqual(BreedResult.TooSoon, Breeding.Check(save, a, b));
        }

        // ---------- Tema, etkinlik, kıyafet ----------

        [Test]
        public void Themes_GoldOrPack()
        {
            SaveData save = NewSave();
            save.gold = 250;
            Assert.AreEqual(DecorResult.Bought, Decor.Tap(save, Decor.Get("forest")));
            Assert.AreEqual("forest", save.theme);
            Assert.AreEqual(50, save.gold);
            Assert.AreEqual(DecorResult.NoMoney, Decor.Tap(save, Decor.Get("ocean")));
            Assert.AreEqual(DecorResult.NeedPack, Decor.Tap(save, Decor.Get("space")));
            Shop.Grant(save, ProductCatalog.Get(ProductCatalog.DecorPack), "d");
            Assert.AreEqual(DecorResult.Selected, Decor.Tap(save, Decor.Get("space")));
        }

        [TestCase(10, 26, "halloween")]
        [TestCase(12, 31, "newyear")]
        [TestCase(1, 3, "newyear")]
        [TestCase(4, 23, "children")]
        [TestCase(7, 10, "summer")]
        [TestCase(3, 10, null)]
        public void SeasonEvents_ByDate(int month, int day, string expected)
        {
            SeasonEvent e = SeasonEvents.Current(new DateTime(2026, month, day));
            Assert.AreEqual(expected, e?.id);
        }

        [Test]
        public void EventItem_OnlyDuringEvent_KeptAfter()
        {
            SaveData save = NewSave();
            PetState pet = Pet(save);
            ClothingItem witch = Wardrobe.Get("witch_hat");
            DateTime march = new DateTime(2026, 3, 10);
            Assert.IsFalse(Wardrobe.Visible(save, witch, march));
            Assert.AreEqual(WardrobeResult.Locked, Wardrobe.Tap(save, pet, witch, march));
            Assert.AreEqual(WardrobeResult.Bought, Wardrobe.Tap(save, pet, witch, Day1));
            Assert.IsTrue(Wardrobe.Visible(save, witch, march), "alınan kalır");
            Assert.AreEqual(WardrobeResult.Locked, Wardrobe.Tap(save, pet, Wardrobe.Get("tiara"), Day1), "Pass parçası satılmaz");
        }

        [Test]
        public void PremiumColors_UnlockTwoMore()
        {
            SaveData save = NewSave();
            Assert.AreEqual(4, Shop.ColorChoices(save));
            Shop.Grant(save, ProductCatalog.Get(ProductCatalog.PremiumColors), "c");
            Assert.AreEqual(6, Shop.ColorChoices(save));
        }
    }
}
