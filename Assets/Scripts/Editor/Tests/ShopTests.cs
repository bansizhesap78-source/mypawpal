using System;
using NUnit.Framework;

namespace PatiDostum.Tests
{
    /// <summary>Satın almalar, slotlar, biletler ve harcama limiti (M4).</summary>
    public class ShopTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

        private static SaveData NewSave() => SaveData.CreateNew(Now);
        private static Product P(string id) => ProductCatalog.Get(id);

        private static PetState Adopt(SaveData save, string species = "cat")
        {
            PetState pet = PetState.Adopt(SpeciesCatalog.Get(species), "Pamuk", 0, SpeedMode.Real, Now);
            save.pets.Add(pet);
            return pet;
        }

        [Test]
        public void Catalog_IdsAreValidForGooglePlay()
        {
            foreach (Product product in ProductCatalog.All)
            {
                Assert.That(product.id, Does.Match("^[a-z0-9][a-z0-9_.]*$"), product.id);
                Assert.Greater(product.tlPrice, 0, product.id);
            }
        }

        [Test]
        public void NextSlot_FollowsOrder_StarterFirst()
        {
            SaveData save = NewSave();
            Assert.AreEqual(ProductCatalog.StarterPack, Shop.NextSlotProduct(save, Now), "ilk hafta daha avantajlı paket");
            Assert.AreEqual(ProductCatalog.Slot2, Shop.NextSlotProduct(save, Now.AddDays(Shop.StarterDays + 1)));
            Shop.Grant(save, P(ProductCatalog.Slot2), "a");
            Assert.AreEqual(ProductCatalog.Slot3, Shop.NextSlotProduct(save, Now));
            Shop.Grant(save, P(ProductCatalog.Slot3), "b");
            Assert.AreEqual(ProductCatalog.Slot4, Shop.NextSlotProduct(save, Now));
            Shop.Grant(save, P(ProductCatalog.Slot4), "c");
            Assert.AreEqual(ProductCatalog.Slot5, Shop.NextSlotProduct(save, Now));
            Shop.Grant(save, P(ProductCatalog.Slot5), "d");
            Assert.IsNull(Shop.NextSlotProduct(save, Now), "en çok 5 yer");
        }

        [Test]
        public void Owned_CountsStarterAsSecondSlot_AndConsumablesNever()
        {
            SaveData save = NewSave();
            Assert.IsFalse(Shop.Owned(save, P(ProductCatalog.Slot2), Now));
            Shop.Grant(save, P(ProductCatalog.StarterPack), "a", Now);
            Assert.IsTrue(Shop.Owned(save, P(ProductCatalog.Slot2), Now), "başlangıç paketi 2. yeri verdi");
            Assert.IsFalse(Shop.CanBuy(save, P(ProductCatalog.Slot2), Now));
            Assert.IsFalse(Shop.Owned(save, P(ProductCatalog.Gems10), Now), "tüketilir ürün tekrar alınır");
            Assert.IsTrue(Shop.CanBuy(save, P(ProductCatalog.Gems10), Now));
        }

        [Test]
        public void EveryProduct_GrantsExactlyWhatItPromises()
        {
            foreach (Product product in ProductCatalog.All)
            {
                SaveData save = NewSave();
                int gold = save.gold, diamonds = save.diamonds;
                Assert.IsTrue(Shop.Grant(save, product, "tok-" + product.id, Now), product.id);
                Assert.AreEqual(gold + product.gold, save.gold, product.id);
                Assert.AreEqual(diamonds + product.diamonds, save.diamonds, product.id);
                switch (product.grants)
                {
                    case "mode": Assert.AreEqual(1, save.modeChangers, product.id); break;
                    case "shelter": Assert.AreEqual(1, save.shelterTickets, product.id); break;
                    case "boarding": Assert.AreEqual(1, save.boardingTickets, product.id); break;
                    case "starter":
                        foreach (string item in Shop.StarterItems) Assert.Contains(item, save.ownedItems, product.id);
                        Assert.AreEqual(2, Shop.Slots(save), product.id);
                        break;
                    case "club":
                        Assert.IsTrue(Club.Active(save, Now), product.id);
                        Assert.Contains(Club.Item, save.ownedItems, product.id);
                        break;
                    case "pass": Assert.IsTrue(PatiPass.HasPremium(save, Now.ToLocalTime()), product.id); break;
                    case "slot":
                    case "dino":
                    case "colors":
                    case "photo":
                    case "decor":
                        Assert.IsTrue(Shop.Owns(save, product.id), product.id);
                        break;
                }
                // Mağazada adı ve açıklaması olmalı
                Assert.AreNotEqual(product.NameKey, Loc.Get(product.NameKey), product.id);
                Assert.AreNotEqual(product.DescKey, Loc.Get(product.DescKey), product.id);
            }
        }

        [Test]
        public void Diamonds_AddUp_AndSameTokenGrantsOnce()
        {
            SaveData save = NewSave();
            Assert.IsTrue(Shop.Grant(save, P(ProductCatalog.Gems10), "tok1"));
            Assert.AreEqual(SaveData.StartingDiamonds + 10, save.diamonds);
            Assert.IsFalse(Shop.Grant(save, P(ProductCatalog.Gems10), "tok1"), "yeniden yüklemede ikinci kez verilmez");
            Assert.AreEqual(SaveData.StartingDiamonds + 10, save.diamonds);
            Assert.IsTrue(Shop.Grant(save, P(ProductCatalog.Gems10), "tok2"));
            Assert.AreEqual(SaveData.StartingDiamonds + 20, save.diamonds);
        }

        [Test]
        public void Slots_OneFree_UpToFive()
        {
            SaveData save = NewSave();
            Adopt(save);
            Assert.AreEqual(1, Shop.Slots(save));
            Assert.IsFalse(Shop.CanAdopt(save));
            Assert.IsFalse(Shop.CanBuy(save, P(ProductCatalog.Slot3), Now), "3. slot 2.'den sonra");
            Shop.Grant(save, P(ProductCatalog.Slot2), "a");
            Assert.IsTrue(Shop.CanAdopt(save));
            Assert.IsFalse(Shop.CanBuy(save, P(ProductCatalog.Slot2), Now), "kalıcı ürün bir kez");
            Assert.IsTrue(Shop.CanBuy(save, P(ProductCatalog.Slot3), Now));
            Shop.Grant(save, P(ProductCatalog.Slot3), "b");
            Assert.AreEqual(3, Shop.Slots(save));
            Assert.IsFalse(Shop.CanBuy(save, P(ProductCatalog.Slot5), Now), "5. slot 4.'den sonra");
            Assert.IsTrue(Shop.CanBuy(save, P(ProductCatalog.Slot4), Now));
            Shop.Grant(save, P(ProductCatalog.Slot4), "c");
            Assert.IsTrue(Shop.CanBuy(save, P(ProductCatalog.Slot5), Now));
            Shop.Grant(save, P(ProductCatalog.Slot5), "d");
            Assert.AreEqual(5, Shop.MaxPets);
            Assert.AreEqual(Shop.MaxPets, Shop.Slots(save));
        }

        [Test]
        public void StarterPack_FirstWeekOnly_GivesSlotDiamondsAndClothes()
        {
            SaveData save = NewSave();
            Assert.IsTrue(Shop.StarterAvailable(save, Now.AddDays(6)));
            Assert.IsFalse(Shop.StarterAvailable(save, Now.AddDays(8)));
            Shop.Grant(save, P(ProductCatalog.StarterPack), "s");
            Assert.AreEqual(2, Shop.Slots(save));
            Assert.AreEqual(SaveData.StartingDiamonds + 20, save.diamonds);
            Assert.Contains("bell_collar", save.ownedItems);
            Assert.IsFalse(Shop.StarterAvailable(save, Now));
            Assert.IsFalse(Shop.CanBuy(save, P(ProductCatalog.Slot2), Now), "başlangıç paketi 2. slotu zaten verdi");
        }

        [Test]
        public void DinoPack_UnlocksDinosaurs()
        {
            SaveData save = NewSave();
            Species rex = SpeciesCatalog.Get("t_rex");
            Assert.IsFalse(Shop.SpeciesUnlocked(save, rex));
            Assert.IsTrue(Shop.SpeciesUnlocked(save, SpeciesCatalog.Get("cat")));
            Shop.Grant(save, P(ProductCatalog.DinoPack), "d");
            Assert.IsTrue(Shop.SpeciesUnlocked(save, rex));
        }

        [Test]
        public void ModeChanger_FlipsModeOnce()
        {
            SaveData save = NewSave();
            PetState pet = Adopt(save);
            Assert.IsFalse(Shop.ChangeMode(save, pet));
            Shop.Grant(save, P(ProductCatalog.ModeChanger), "m");
            Assert.IsTrue(Shop.ChangeMode(save, pet));
            Assert.AreEqual(SpeedMode.Fast, pet.Mode);
            Assert.AreEqual(0, save.modeChangers);
        }

        [Test]
        public void Shelter_TicketOrTenDiamonds_FreesSlot()
        {
            SaveData save = NewSave();
            PetState pet = Adopt(save);
            save.diamonds = 9;
            Assert.IsFalse(Shop.CanSendToShelter(save));
            Assert.IsNull(Shop.SendToShelter(save, pet, Now));
            save.diamonds = 12;
            MemorialEntry entry = Shop.SendToShelter(save, pet, Now);
            Assert.IsNotNull(entry);
            Assert.AreEqual("shelter", entry.cause);
            Assert.AreEqual(2, save.diamonds);
            Assert.IsTrue(Shop.CanAdopt(save));
            Assert.AreEqual(1, save.memorial.Count);
        }

        [Test]
        public void Boarding_FreezesNeedsForAWeek()
        {
            SaveData save = NewSave();
            Assert.IsFalse(Shop.StartBoarding(save, Now));
            Shop.Grant(save, P(ProductCatalog.Boarding), "b");
            Assert.IsTrue(Shop.StartBoarding(save, Now));
            Assert.IsTrue(Shop.IsBoarding(save, Now.AddDays(6.9)));
            Assert.IsFalse(Shop.IsBoarding(save, Now.AddDays(7.1)));

            PetState pet = Adopt(save);
            double fullness = pet.fullness, age = pet.ageSeconds;
            PetSimulator.AgeOnly(pet, 3 * 86400);
            Assert.AreEqual(fullness, pet.fullness, 0.001, "pansiyonda acıkmaz");
            Assert.AreEqual(age + 3 * 86400, pet.ageSeconds, 0.001, "ama yaşlanır");
        }

        [Test]
        public void MonthlyLimit_BlocksOverspend_ResetsNextMonth()
        {
            SaveData save = NewSave();
            DateTime local = new DateTime(2026, 10, 26, 15, 0, 0);
            Assert.IsTrue(Shop.WithinLimit(save, P(ProductCatalog.Gems80), local), "limit yoksa serbest");
            save.monthlyLimit = 50;
            Assert.IsTrue(Shop.WithinLimit(save, P(ProductCatalog.Gems10), local));
            Shop.RecordSpend(save, P(ProductCatalog.Gems10), local);
            Assert.IsFalse(Shop.WithinLimit(save, P(ProductCatalog.Gems10), local), "29,99 + 29,99 > 50");
            Assert.IsTrue(Shop.WithinLimit(save, P(ProductCatalog.Gold500), local), "29,99 + 19,99 <= 50");
            Assert.IsTrue(Shop.WithinLimit(save, P(ProductCatalog.Gems10), local.AddMonths(1)), "yeni ay");
        }

        [Test]
        public void LimitOptions_Cycle()
        {
            SaveData save = NewSave();
            for (int i = 1; i < Shop.LimitOptions.Length; i++)
            {
                Shop.NextLimit(save);
                Assert.AreEqual(Shop.LimitOptions[i], save.monthlyLimit);
            }
            Shop.NextLimit(save);
            Assert.AreEqual(0, save.monthlyLimit);
        }

        [Test]
        public void DiamondExchange_GivesGold()
        {
            SaveData save = NewSave();
            int gold = save.gold;
            Assert.IsTrue(Shop.ExchangeDiamond(save));
            Assert.AreEqual(gold + Shop.DiamondToGold, save.gold);
            Assert.AreEqual(SaveData.StartingDiamonds - 1, save.diamonds);
            save.diamonds = 0;
            Assert.IsFalse(Shop.ExchangeDiamond(save));
        }

        [Test]
        public void NoLootBoxes_EveryProductContentIsFixed()
        {
            // Plan §7b: paralı rastgele ödül yok; aynı ürün her alımda aynı şeyi verir
            foreach (Product product in ProductCatalog.All)
            {
                if (product.kind == ProductKind.Permanent) continue;
                SaveData a = NewSave(), b = NewSave();
                Shop.Grant(a, product, "x");
                Shop.Grant(b, product, "y");
                Assert.AreEqual(a.diamonds, b.diamonds, product.id);
                Assert.AreEqual(a.gold, b.gold, product.id);
            }
        }
    }
}
