using System;

namespace PatiDostum
{
    /// <summary>
    /// Satın alma kuralları (saf C#; testleri Editor/Tests/ShopTests). Mağazadan (Google Play ya da test mağazası)
    /// gelen her başarılı alım Grant ile verilir; aynı alım jetonu ikinci kez hak vermez.
    /// Plan §7 ve §7b: şans kutusu yok, her ürünün içeriği önceden belli; ebeveyn kapısı ve isteğe bağlı aylık limit.
    /// </summary>
    public static class Shop
    {
        public const int MaxPets = 5;
        public const int StarterDays = 7;
        public const int ShelterDiamonds = 10;
        public const int DiamondToGold = 100;
        public const double BoardingDays = 7.0;
        public const int TokenMemory = 100;
        public static readonly string[] StarterItems = { "bell_collar", "scarf" };
        /// <summary>Ebeveynin seçebileceği aylık limitler (TL; 0 = limitsiz).</summary>
        public static readonly int[] LimitOptions = { 0, 50, 100, 250, 500 };

        public static bool Owns(SaveData save, string productId) => save.ownedProducts.Contains(productId);

        /// <summary>Aynı anda bakılabilecek dost sayısı: 1 bedava; başlangıç paketi ya da 2. slot, sonra 3., 4., 5. slot.</summary>
        public static int Slots(SaveData save)
        {
            if (Owns(save, ProductCatalog.FamilyPack)) return MaxPets;
            int slots = 1;
            if (Owns(save, ProductCatalog.Slot2) || Owns(save, ProductCatalog.StarterPack)) slots++;
            if (Owns(save, ProductCatalog.Slot3)) slots++;
            if (Owns(save, ProductCatalog.Slot4)) slots++;
            if (Owns(save, ProductCatalog.Slot5)) slots++;
            return Math.Min(slots, MaxPets);
        }

        public static bool CanAdopt(SaveData save) => save.pets.Count < Slots(save);

        public static bool DinoUnlocked(SaveData save) => Owns(save, ProductCatalog.DinoPack);

        /// <summary>Sahiplenirken seçilebilen renk sayısı: 4 bedava, Özel Renkler paketiyle 6.</summary>
        public static int ColorChoices(SaveData save) => Owns(save, ProductCatalog.PremiumColors) ? 6 : 4;

        /// <summary>Tür sahiplenilebilir mi (paketli türler paket alınınca açılır).</summary>
        public static bool SpeciesUnlocked(SaveData save, Species species)
        {
            return string.IsNullOrEmpty(species.pack) || (species.pack == "dino" && DinoUnlocked(save));
        }

        /// <summary>Sıradaki yeri açan ürün (başlangıç paketi daha avantajlı: varsa o); hepsi açıksa null.</summary>
        public static string NextSlotProduct(SaveData save, DateTime nowUtc)
        {
            if (StarterAvailable(save, nowUtc)) return ProductCatalog.StarterPack;
            string[] order = { ProductCatalog.Slot2, ProductCatalog.Slot3, ProductCatalog.Slot4, ProductCatalog.Slot5 };
            int slots = Slots(save);
            return slots - 1 < order.Length ? order[slots - 1] : null;
        }

        private static readonly string[] SingleSlots = { ProductCatalog.Slot2, ProductCatalog.Slot3, ProductCatalog.Slot4, ProductCatalog.Slot5 };

        /// <summary>Henüz açılmamış yerleri tek tek almanın toplam fiyatı (TL).</summary>
        private static double RemainingSlotsPrice(SaveData save)
        {
            double sum = 0.0;
            for (int i = Slots(save) - 1; i < SingleSlots.Length; i++) sum += ProductCatalog.Get(SingleSlots[i]).tlPrice;
            return sum;
        }

        /// <summary>
        /// Paketin gerçek indirimi (%): aynı şeyleri mağazadaki fiyatlarla tek tek almaya göre. Sahte "eski fiyat" ya da
        /// geri sayım yok (plan §7b); indirim yoksa 0.
        /// </summary>
        public static int BundleSaving(SaveData save, Product product)
        {
            double single;
            if (product.id == ProductCatalog.FamilyPack) single = RemainingSlotsPrice(save);
            else if (product.id == ProductCatalog.StarterPack)
                single = ProductCatalog.Get(ProductCatalog.Slot2).tlPrice + product.diamonds * ProductCatalog.Get(ProductCatalog.Gems10).tlPrice / 10.0;
            else return 0;
            return single > product.tlPrice ? (int)((1.0 - product.tlPrice / single) * 100.0) : 0;
        }

        /// <summary>Ürün zaten elinde mi ("Alındı"): 2. slotu başlangıç paketi de verir, aile paketi hepsini.</summary>
        public static bool Owned(SaveData save, Product product, DateTime nowUtc)
        {
            if (Array.IndexOf(SingleSlots, product.id) >= 0 && Owns(save, ProductCatalog.FamilyPack)) return true;
            if (product.kind == ProductKind.Subscription) return Club.Active(save, nowUtc);
            if (product.id == ProductCatalog.Pass) return PatiPass.HasPremium(save, nowUtc.ToLocalTime());
            if (product.id == ProductCatalog.Slot2 && Owns(save, ProductCatalog.StarterPack)) return true;
            return product.kind == ProductKind.Permanent && Owns(save, product.id);
        }

        /// <summary>Başlangıç paketi: ilk 7 gün, bir kez, henüz ikinci slotu olmayana.</summary>
        public static bool StarterAvailable(SaveData save, DateTime nowUtc)
        {
            if (Owns(save, ProductCatalog.StarterPack) || Owns(save, ProductCatalog.Slot2) || Owns(save, ProductCatalog.FamilyPack)) return false;
            return (nowUtc - new DateTime(save.createdUtcTicks, DateTimeKind.Utc)).TotalDays < StarterDays;
        }

        /// <summary>Mağazada satın alma butonu açık mı.</summary>
        public static bool CanBuy(SaveData save, Product product, DateTime nowUtc)
        {
            if (product.kind == ProductKind.Permanent && Owns(save, product.id)) return false;
            if (product.kind == ProductKind.Subscription) return !Club.Active(save, nowUtc);
            switch (product.id)
            {
                case ProductCatalog.Pass: return !PatiPass.HasPremium(save, nowUtc.ToLocalTime());
                case ProductCatalog.StarterPack: return StarterAvailable(save, nowUtc);
                case ProductCatalog.FamilyPack: return Slots(save) < MaxPets && BundleSaving(save, product) > 0;
                case ProductCatalog.Slot2: return !Owns(save, ProductCatalog.StarterPack) && !Owns(save, ProductCatalog.FamilyPack);
                // Slotlar sırayla açılır
                case ProductCatalog.Slot3: return Slots(save) >= 2 && !Owns(save, ProductCatalog.FamilyPack);
                case ProductCatalog.Slot4: return Slots(save) >= 3 && !Owns(save, ProductCatalog.FamilyPack);
                case ProductCatalog.Slot5: return Slots(save) >= 4 && !Owns(save, ProductCatalog.FamilyPack);
                default: return true;
            }
        }

        /// <summary>
        /// Başarılı alımın hakkını verir. <paramref name="token"/> mağazanın alım jetonu; daha önce verildiyse
        /// (yeniden yükleme, yarım kalan alım) tekrar vermez. Verildiyse true.
        /// </summary>
        public static bool Grant(SaveData save, Product product, string token) => Grant(save, product, token, DateTime.UtcNow);

        public static bool Grant(SaveData save, Product product, string token, DateTime nowUtc)
        {
            // Abonelik her doğrulamada uzar (aynı jeton her açılışta yeniden gelir)
            if (product.kind == ProductKind.Subscription)
            {
                bool wasActive = Club.Active(save, nowUtc);
                Club.Confirm(save, nowUtc);
                Remember(save, token);
                return !wasActive;
            }
            if (!string.IsNullOrEmpty(token) && save.grantedTokens.Contains(token)) return false;
            if (product.kind == ProductKind.Permanent && Owns(save, product.id))
            {
                Remember(save, token);
                return false;
            }

            save.diamonds += product.diamonds;
            save.gold += product.gold;
            switch (product.grants)
            {
                case "mode": save.modeChangers++; break;
                case "shelter": save.shelterTickets++; break;
                case "boarding": save.boardingTickets++; break;
                case "starter":
                    foreach (string item in StarterItems)
                        if (!save.ownedItems.Contains(item)) save.ownedItems.Add(item);
                    break;
                case "pass": save.passPremiumSeason = PatiPass.Season(nowUtc.ToLocalTime()); break;
            }
            if (product.kind == ProductKind.Permanent) save.ownedProducts.Add(product.id);
            Remember(save, token);
            return true;
        }

        private static void Remember(SaveData save, string token)
        {
            if (string.IsNullOrEmpty(token)) return;
            save.grantedTokens.Add(token);
            while (save.grantedTokens.Count > TokenMemory) save.grantedTokens.RemoveAt(0);
        }

        // ---------- Aylık harcama limiti ----------

        private static int MonthKey(DateTime local) => local.Year * 100 + local.Month;

        public static double SpentThisMonth(SaveData save, DateTime nowLocal)
        {
            return save.spendMonth == MonthKey(nowLocal) ? save.spentThisMonth : 0.0;
        }

        public static bool WithinLimit(SaveData save, Product product, DateTime nowLocal)
        {
            if (save.monthlyLimit <= 0) return true;
            return SpentThisMonth(save, nowLocal) + product.tlPrice <= save.monthlyLimit + 0.001;
        }

        public static void RecordSpend(SaveData save, Product product, DateTime nowLocal)
        {
            int month = MonthKey(nowLocal);
            if (save.spendMonth != month)
            {
                save.spendMonth = month;
                save.spentThisMonth = 0.0;
            }
            save.spentThisMonth += product.tlPrice;
        }

        public static void NextLimit(SaveData save)
        {
            int index = Array.IndexOf(LimitOptions, save.monthlyLimit);
            save.monthlyLimit = LimitOptions[(index + 1) % LimitOptions.Length];
        }

        // ---------- Elmas ve biletlerin kullanımı ----------

        /// <summary>1 mavi elmas = 100 altın (elması olan altınsız kalmasın).</summary>
        public static bool ExchangeDiamond(SaveData save)
        {
            if (save.diamonds < 1) return false;
            save.diamonds--;
            save.gold += DiamondToGold;
            return true;
        }

        /// <summary>Mod Değiştirici: dostun zaman modunu 1x ↔ 5x çevirir.</summary>
        public static bool ChangeMode(SaveData save, PetState pet)
        {
            if (save.modeChangers < 1) return false;
            save.modeChangers--;
            pet.mode = pet.Mode == SpeedMode.Real ? (int)SpeedMode.Fast : (int)SpeedMode.Real;
            return true;
        }

        public static bool CanSendToShelter(SaveData save) => save.shelterTickets > 0 || save.diamonds >= ShelterDiamonds;

        /// <summary>
        /// Barınağa gönder: dost "atılmaz", barınak ona yeni bir aile bulur (Anı Bahçesi'nde öyle yazar) ve slot boşalır.
        /// Bilet varsa bilet, yoksa 10 elmas.
        /// </summary>
        public static MemorialEntry SendToShelter(SaveData save, PetState pet, DateTime nowUtc)
        {
            if (save.shelterTickets > 0) save.shelterTickets--;
            else if (save.diamonds >= ShelterDiamonds) save.diamonds -= ShelterDiamonds;
            else return null;

            var entry = new MemorialEntry
            {
                name = pet.name, species = pet.species, color = pet.color, mode = pet.mode, ageDays = pet.AgeDays,
                adoptedUtcTicks = pet.adoptedUtcTicks, diedUtcTicks = nowUtc.Ticks, cause = "shelter", rescues = pet.rescues
            };
            save.memorial.Add(entry);
            save.pets.Remove(pet);
            return entry;
        }

        public static bool IsBoarding(SaveData save, DateTime nowUtc) => save.boardingUntilUtcTicks > nowUtc.Ticks;

        /// <summary>Pansiyon bileti: 7 gün boyunca bütün dostların ihtiyaçları donar (süre üst üste eklenir).</summary>
        public static bool StartBoarding(SaveData save, DateTime nowUtc)
        {
            if (save.boardingTickets < 1) return false;
            save.boardingTickets--;
            long start = Math.Max(nowUtc.Ticks, save.boardingUntilUtcTicks);
            save.boardingUntilUtcTicks = start + TimeSpan.FromDays(BoardingDays).Ticks;
            return true;
        }
    }
}
