namespace PatiDostum
{
    public enum ProductKind
    {
        /// <summary>Tüketilir: her alışta tekrar alınabilir (elmas, altın, bilet).</summary>
        Consumable,
        /// <summary>Kalıcı: bir kez alınır, Play'den geri yüklenir (slot, paket).</summary>
        Permanent,
        /// <summary>Abonelik (Pati Kulübü): Play her açılışta etkin olup olmadığını bildirir.</summary>
        Subscription
    }

    /// <summary>
    /// Gerçek parayla satılan ürün (Google Play'de aynı id ile tanımlanır; liste PLAY_URUNLER.md'de).
    /// Fiyat mağazadan yerel para biriminde gelir; tlPrice yalnızca mağaza cevap vermezken gösterilir ve
    /// aylık harcama limiti bununla sayılır.
    /// </summary>
    public sealed class Product
    {
        public string id;
        public ProductKind kind;
        public double tlPrice;
        public int diamonds;
        public int gold;
        /// <summary>
        /// Verdiği hak (Shop.Grant'ta yorumlanır): "slot", "dino", "mode", "shelter", "boarding", "starter",
        /// "club", "pass", "colors", "photo", "decor".
        /// </summary>
        public string grants = "";

        public string NameKey => "product_" + id;
        public string DescKey => "product_" + id + "_desc";
    }

    /// <summary>Ürün kataloğu (plan §7).</summary>
    public static class ProductCatalog
    {
        public const string ClubMonthly = "pati_club_monthly";
        public const string ClubYearly = "pati_club_yearly";
        public const string Pass = "pati_pass";
        public const string PremiumColors = "premium_colors";
        public const string PhotoPack = "photo_pack";
        public const string DecorPack = "decor_pack";
        public const string Gems10 = "gems_10";
        public const string Gems30 = "gems_30";
        public const string Gems80 = "gems_80";
        public const string Gold500 = "gold_500";
        public const string Gold1500 = "gold_1500";
        public const string Gold4000 = "gold_4000";
        public const string Slot2 = "slot_2";
        public const string Slot3 = "slot_3";
        public const string Slot4 = "slot_4";
        public const string Slot5 = "slot_5";
        public const string FamilyPack = "family_pack";
        public const string DinoPack = "dino_pack";
        public const string ModeChanger = "mode_changer";
        public const string Shelter = "shelter_ticket";
        public const string Boarding = "boarding_ticket";
        public const string StarterPack = "starter_pack";

        public static readonly Product[] All =
        {
            new Product { id = StarterPack, kind = ProductKind.Permanent, tlPrice = 39.99, diamonds = 20, grants = "starter" },
            new Product { id = Gems10, kind = ProductKind.Consumable, tlPrice = 29.99, diamonds = 10 },
            new Product { id = Gems30, kind = ProductKind.Consumable, tlPrice = 79.99, diamonds = 30 },
            new Product { id = Gems80, kind = ProductKind.Consumable, tlPrice = 179.99, diamonds = 80 },
            new Product { id = Gold500, kind = ProductKind.Consumable, tlPrice = 19.99, gold = 500 },
            new Product { id = Gold1500, kind = ProductKind.Consumable, tlPrice = 49.99, gold = 1500 },
            new Product { id = Gold4000, kind = ProductKind.Consumable, tlPrice = 99.99, gold = 4000 },
            new Product { id = Slot2, kind = ProductKind.Permanent, tlPrice = 49.99, grants = "slot" },
            new Product { id = Slot3, kind = ProductKind.Permanent, tlPrice = 79.99, grants = "slot" },
            new Product { id = Slot4, kind = ProductKind.Permanent, tlPrice = 99.99, grants = "slot" },
            new Product { id = Slot5, kind = ProductKind.Permanent, tlPrice = 119.99, grants = "slot" },
            new Product { id = FamilyPack, kind = ProductKind.Permanent, tlPrice = 249.99, grants = "slot" },
            new Product { id = DinoPack, kind = ProductKind.Permanent, tlPrice = 59.99, grants = "dino" },
            new Product { id = ModeChanger, kind = ProductKind.Consumable, tlPrice = 29.99, grants = "mode" },
            new Product { id = Shelter, kind = ProductKind.Consumable, tlPrice = 19.99, grants = "shelter" },
            new Product { id = Boarding, kind = ProductKind.Consumable, tlPrice = 24.99, grants = "boarding" },
            // M5
            new Product { id = ClubMonthly, kind = ProductKind.Subscription, tlPrice = 59.99, grants = "club" },
            new Product { id = ClubYearly, kind = ProductKind.Subscription, tlPrice = 449.99, grants = "club" },
            new Product { id = Pass, kind = ProductKind.Consumable, tlPrice = 49.99, grants = "pass" },
            new Product { id = PremiumColors, kind = ProductKind.Permanent, tlPrice = 39.99, grants = "colors" },
            new Product { id = PhotoPack, kind = ProductKind.Permanent, tlPrice = 19.99, grants = "photo" },
            new Product { id = DecorPack, kind = ProductKind.Permanent, tlPrice = 29.99, grants = "decor" },
        };

        public static bool IsSubscription(string id) => Get(id)?.kind == ProductKind.Subscription;

        public static Product Get(string id)
        {
            foreach (Product product in All)
                if (product.id == id) return product;
            return null;
        }
    }
}
