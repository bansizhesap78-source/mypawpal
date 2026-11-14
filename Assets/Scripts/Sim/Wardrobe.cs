using System;

namespace PatiDostum
{
    /// <summary>
    /// Yuvalar. Feet artık kullanılmaz (eski kayıtlarla uyum için duruyor, katalogda parça yok). Outfit: her türün
    /// her pozu için ayrı çizilmiş katman (PetArt: outfit_&lt;id&gt;_&lt;tür&gt;[_adult]_&lt;poz&gt;); çizimi olmayan türde gizlenir.
    /// </summary>
    public enum ClothingSlot { Hat, Glasses, Neck, Outfit, Feet }

    public sealed class ClothingItem
    {
        public string id;
        public ClothingSlot slot;
        public int price;
        /// <summary>
        /// Nereden gelir: "shop" (altınla), "event:&lt;id&gt;" (yalnızca etkinlikte altınla), "pass" (Pati Pass),
        /// "club" (Pati Kulübü). Alınan her parça kalıcıdır.
        /// </summary>
        public string source = "shop";

        public string NameKey => "item_" + id;
    }

    public enum WardrobeResult { Bought, Equipped, Removed, NoMoney, Locked }

    /// <summary>
    /// Gardırop (saf C#; testleri Editor/Tests/EconomyTests). Kıyafetler altınla bir kez alınır, bütün dostlar
    /// giyebilir (SaveData.ownedItems); her dostun her yuvasında bir parça olur (PetState.hat ...).
    /// </summary>
    public static class Wardrobe
    {
        public static readonly ClothingItem[] All =
        {
            new ClothingItem { id = "party_hat", slot = ClothingSlot.Hat, price = 40 },
            new ClothingItem { id = "cap", slot = ClothingSlot.Hat, price = 60 },
            new ClothingItem { id = "crown", slot = ClothingSlot.Hat, price = 150 },
            new ClothingItem { id = "round_glasses", slot = ClothingSlot.Glasses, price = 50 },
            new ClothingItem { id = "sunglasses", slot = ClothingSlot.Glasses, price = 70 },
            new ClothingItem { id = "bow_tie", slot = ClothingSlot.Neck, price = 40 },
            new ClothingItem { id = "scarf", slot = ClothingSlot.Neck, price = 50 },
            // Tur 4 (K3 sayfası): yalnızca başa, yüze ve boyna takılanlar
            new ClothingItem { id = "bunny_ears", slot = ClothingSlot.Hat, price = 50 },
            new ClothingItem { id = "bear_ears", slot = ClothingSlot.Hat, price = 50 },
            new ClothingItem { id = "beret", slot = ClothingSlot.Hat, price = 60 },
            new ClothingItem { id = "chef_hat", slot = ClothingSlot.Hat, price = 70 },
            new ClothingItem { id = "cowboy_hat", slot = ClothingSlot.Hat, price = 90 },
            new ClothingItem { id = "pirate_hat", slot = ClothingSlot.Hat, price = 100 },
            new ClothingItem { id = "top_hat", slot = ClothingSlot.Hat, price = 120 },
            new ClothingItem { id = "heart_glasses", slot = ClothingSlot.Glasses, price = 60 },
            new ClothingItem { id = "rainbow_glasses", slot = ClothingSlot.Glasses, price = 80 },
            new ClothingItem { id = "party_mask", slot = ClothingSlot.Glasses, price = 90 },
            new ClothingItem { id = "bell_collar", slot = ClothingSlot.Neck, price = 40 },
            new ClothingItem { id = "bandana", slot = ClothingSlot.Neck, price = 50 },
            new ClothingItem { id = "flower_lei", slot = ClothingSlot.Neck, price = 70 },
            new ClothingItem { id = "pearl_necklace", slot = ClothingSlot.Neck, price = 110 },
            new ClothingItem { id = "medal", slot = ClothingSlot.Neck, price = 80 },
            // Elbiseler (dışarı gezdirirken de üstündedir)
            new ClothingItem { id = "sweater", slot = ClothingSlot.Outfit, price = 120 },
            new ClothingItem { id = "raincoat", slot = ClothingSlot.Outfit, price = 140 },
            new ClothingItem { id = "cape", slot = ClothingSlot.Outfit, price = 160 },
            // Sezon etkinlikleri (M5)
            new ClothingItem { id = "witch_hat", slot = ClothingSlot.Hat, price = 80, source = "event:halloween" },
            new ClothingItem { id = "santa_hat", slot = ClothingSlot.Hat, price = 80, source = "event:newyear" },
            new ClothingItem { id = "sun_hat", slot = ClothingSlot.Hat, price = 80, source = "event:summer" },
            new ClothingItem { id = "star_bow", slot = ClothingSlot.Neck, price = 80, source = "event:children" },
            // Pati Pass ve Pati Kulübü
            new ClothingItem { id = "flower_crown", slot = ClothingSlot.Hat, source = "pass" },
            new ClothingItem { id = "star_glasses", slot = ClothingSlot.Glasses, source = "pass" },
            new ClothingItem { id = "tiara", slot = ClothingSlot.Hat, source = "pass" },
            new ClothingItem { id = "club_bow", slot = ClothingSlot.Neck, source = "club" },
        };

        /// <summary>Dolapta görünür mü: mağaza parçaları hep, etkinlik parçası etkinlikte, diğerleri alındıysa.</summary>
        public static bool Visible(SaveData save, ClothingItem item, DateTime nowLocal)
        {
            if (save.ownedItems.Contains(item.id) || item.source == "shop") return true;
            return item.source.StartsWith("event:") && SeasonEvents.IsActive(item.source.Substring(6), nowLocal);
        }

        private static bool Buyable(ClothingItem item, DateTime nowLocal)
        {
            if (item.source == "shop") return true;
            return item.source.StartsWith("event:") && SeasonEvents.IsActive(item.source.Substring(6), nowLocal);
        }

        public static ClothingItem Get(string id)
        {
            foreach (ClothingItem item in All)
                if (item.id == id) return item;
            return null;
        }

        public static string Worn(PetState p, ClothingSlot slot)
        {
            switch (slot)
            {
                case ClothingSlot.Hat: return p.hat;
                case ClothingSlot.Glasses: return p.glasses;
                case ClothingSlot.Neck: return p.neck;
                case ClothingSlot.Outfit: return p.outfit;
                case ClothingSlot.Feet: return p.feet;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public static bool IsWorn(PetState p, ClothingItem item) => Worn(p, item.slot) == item.id;

        /// <summary>Karttaki dokunuş: alınmamışsa alır ve giydirir; giyiliyse çıkarır; değilse giydirir.</summary>
        public static WardrobeResult Tap(SaveData save, PetState p, ClothingItem item) => Tap(save, p, item, DateTime.Now);

        public static WardrobeResult Tap(SaveData save, PetState p, ClothingItem item, DateTime nowLocal)
        {
            if (!save.ownedItems.Contains(item.id))
            {
                if (!Buyable(item, nowLocal)) return WardrobeResult.Locked;
                if (save.gold < item.price) return WardrobeResult.NoMoney;
                save.gold -= item.price;
                save.ownedItems.Add(item.id);
                Wear(p, item.slot, item.id);
                p.fun += 5.0;
                PetSimulator.Clamp(p);
                return WardrobeResult.Bought;
            }
            if (IsWorn(p, item))
            {
                Wear(p, item.slot, "");
                return WardrobeResult.Removed;
            }
            Wear(p, item.slot, item.id);
            return WardrobeResult.Equipped;
        }

        /// <summary>Dostun üstündeki her şeyi çıkarır (parçalar dolapta kalır).</summary>
        public static void RemoveAll(PetState p)
        {
            foreach (ClothingSlot slot in Enum.GetValues(typeof(ClothingSlot))) Wear(p, slot, "");
        }

        /// <summary>Dolap koleksiyonu: katalogdaki parçalardan kaçı alındı.</summary>
        public static int OwnedCount(SaveData save)
        {
            int count = 0;
            foreach (ClothingItem item in All)
                if (save.ownedItems.Contains(item.id)) count++;
            return count;
        }

        private static void Wear(PetState p, ClothingSlot slot, string id)
        {
            switch (slot)
            {
                case ClothingSlot.Hat: p.hat = id; break;
                case ClothingSlot.Glasses: p.glasses = id; break;
                case ClothingSlot.Neck: p.neck = id; break;
                case ClothingSlot.Outfit: p.outfit = id; break;
                case ClothingSlot.Feet: p.feet = id; break;
            }
        }
    }
}
