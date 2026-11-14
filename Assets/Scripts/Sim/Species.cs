using System;

namespace PatiDostum
{
    /// <summary>
    /// İhtiyaçların gerçek saatte düşme hızları (puan / gerçek saat, gündüz). Gece GameClock.NeedsFactor ile yavaşlar.
    /// Zaman modu bu hızları değiştirmez: hızlı modda yalnızca yaşlanma 5 kat hızlıdır; ihtiyaçlar da 5 kat
    /// hızlansaydı hızlı modda saatte bir beslemek gerekirdi.
    /// </summary>
    public sealed class NeedRates
    {
        public double fullness = 6.0;
        public double thirst = 7.0;
        public double energyAwake = 5.0;
        public double energyAsleep = 50.0;   // uyurken kazanılan: 2 saatte boştan doluya (çocuk dostu beklemesin)
        public double hygiene = 3.0;
        public double toilet = 8.0;
        public double fun = 5.0;

        /// <summary>Bütün düşüşleri aynı oranda yavaşlatır/hızlandırır (kaplumbağa yavaş yaşar).</summary>
        public NeedRates Scaled(double factor)
        {
            return new NeedRates
            {
                fullness = fullness * factor, thirst = thirst * factor, energyAwake = energyAwake * factor,
                energyAsleep = energyAsleep, hygiene = hygiene * factor, toilet = toilet * factor, fun = fun * factor
            };
        }
    }

    /// <summary>Nasıl gezer: yürür, yüzer (suda süzülür, dışarı gezmeye değil yüzmeye çıkar), uçar.</summary>
    public enum MoveKind { Walk, Swim, Fly }

    /// <summary>Hayvan türü. Ömürler ortalama gerçek değerlerdir (plan §3).</summary>
    public sealed class Species
    {
        public string id;
        public double lifespanYears;
        public double adultYears;
        /// <summary>Sahiplenildiği yaş (gün). Yavru kedi ve köpek 8 haftalıkken sahiplenilir.</summary>
        public double adoptAgeDays;
        public NeedRates rates = new NeedRates();
        /// <summary>Mutfakta gösterilen yiyecekler (FoodCatalog id). İlki her zaman bedava temel yemdir.</summary>
        public string[] foods;
        /// <summary>Okulda öğrenebileceği komutlar (Training.Commands id).</summary>
        public string[] commands;
        /// <summary>Tuvalet ihtiyacı var mı (balıkta yok; akvaryum temizliği "Yıka" ile yapılır).</summary>
        public bool hasToilet = true;
        /// <summary>Su kabından içer mi (balık suda yaşar).</summary>
        public bool drinks = true;
        /// <summary>Taranabilir mi (tüysüzlerde yok).</summary>
        public bool brushable = true;
        public bool available = true;
        /// <summary>Paketle açılan tür ("dino"); boşsa herkese açık. Shop.SpeciesUnlocked.</summary>
        public string pack = "";
        /// <summary>Gezinti şekli; balık odasında da süzülür.</summary>
        public MoveKind moves = MoveKind.Walk;
        /// <summary>Yaşadığı ortam (Resources/Rooms/&lt;ortam&gt;_&lt;oda&gt;); çizimi yoksa ev odaları.</summary>
        public string habitat = "home";

        public string NameKey => "species_" + id;
    }

    public static class SpeciesCatalog
    {
        public static readonly Species[] All =
        {
            new Species
            {
                id = "hamster", lifespanYears = 2.5, adultYears = 2.0 / 12, adoptAgeDays = 28,
                rates = new NeedRates { fullness = 8, thirst = 8, energyAwake = 6 },
                foods = new[] { "sunflower", "carrot", "strawberry" }, commands = new[] { "come", "spin", "stand" }, habitat = "burrow"
            },
            new Species
            {
                id = "hedgehog", lifespanYears = 5, adultYears = 0.5, adoptAgeDays = 42,
                foods = new[] { "cricket", "worm", "fruit" }, commands = new[] { "come", "ball", "spin" }, brushable = false, habitat = "burrow"
            },
            new Species
            {
                id = "guinea_pig", lifespanYears = 6, adultYears = 4.0 / 12, adoptAgeDays = 28,
                rates = new NeedRates { fullness = 7 },
                foods = new[] { "hay", "cucumber", "carrot" }, commands = new[] { "come", "stand", "spin" }, habitat = "burrow"
            },
            new Species
            {
                id = "budgie", lifespanYears = 8, adultYears = 0.5, adoptAgeDays = 42,
                rates = new NeedRates { fun = 6 },
                foods = new[] { "millet", "seeds", "fruit" }, commands = new[] { "speak", "come", "spin" }, habitat = "aviary", moves = MoveKind.Fly
            },
            new Species
            {
                id = "rabbit", lifespanYears = 9, adultYears = 0.5, adoptAgeDays = 56,
                foods = new[] { "hay", "carrot", "greens" }, commands = new[] { "jump", "come", "spin" }, habitat = "garden"
            },
            new Species
            {
                id = "duck", lifespanYears = 9, adultYears = 0.5, adoptAgeDays = 28,
                rates = new NeedRates { thirst = 8, hygiene = 4 },
                foods = new[] { "corn", "worm", "greens" }, commands = new[] { "come", "flap", "spin" }, habitat = "pond"
            },
            new Species
            {
                id = "goldfish", lifespanYears = 10, adultYears = 1, adoptAgeDays = 60,
                // Suda yaşar: susamaz, tuvaleti yok; akvaryum kirlenir (temizlik), "Yıka" akvaryumu temizler
                rates = new NeedRates { fullness = 4, thirst = 0, hygiene = 5, toilet = 0, fun = 3 },
                foods = new[] { "flakes", "krill", "worm" }, commands = new[] { "come", "hoop", "spin" },
                hasToilet = false, drinks = false, brushable = false, habitat = "aquarium", moves = MoveKind.Swim
            },
            new Species
            {
                id = "dog", lifespanYears = 12, adultYears = 1.5, adoptAgeDays = 56,
                rates = new NeedRates { energyAwake = 6, toilet = 9, fun = 6 },
                foods = new[] { "pet_food", "bone", "steak" }, commands = new[] { "sit", "paw", "fetch" }
            },
            new Species
            {
                id = "cat", lifespanYears = 15, adultYears = 1, adoptAgeDays = 56,
                foods = new[] { "pet_food", "fish", "treat" }, commands = new[] { "sit", "paw", "roll" }
            },
            new Species
            {
                id = "turtle", lifespanYears = 40, adultYears = 10, adoptAgeDays = 90,
                // Yavaş yaşar: ihtiyaçları da yavaş azalır
                rates = new NeedRates().Scaled(0.6),
                foods = new[] { "greens", "worm", "strawberry" }, commands = new[] { "come", "peek", "spin" }, brushable = false, habitat = "pond"
            },
            new Species
            {
                id = "penguin", lifespanYears = 20, adultYears = 1, adoptAgeDays = 60,
                foods = new[] { "krill", "fish", "treat" }, commands = new[] { "jump", "come", "spin" }, brushable = false, habitat = "ice"
            },
            new Species
            {
                id = "lion", lifespanYears = 15, adultYears = 3, adoptAgeDays = 60,
                rates = new NeedRates { fullness = 7 },
                foods = new[] { "meat", "steak", "bone" }, commands = new[] { "roar", "come", "jump" }, habitat = "savanna"
            },
            new Species
            {
                id = "giraffe", lifespanYears = 25, adultYears = 4, adoptAgeDays = 60,
                foods = new[] { "acacia", "fruit", "carrot" }, commands = new[] { "come", "stand", "spin" }, habitat = "savanna"
            },
            new Species
            {
                id = "panda", lifespanYears = 20, adultYears = 5, adoptAgeDays = 60,
                rates = new NeedRates { fullness = 7 },
                foods = new[] { "bamboo", "fruit", "honey" }, commands = new[] { "roll", "come", "spin" }, habitat = "bamboo"
            },
            new Species
            {
                id = "crocodile", lifespanYears = 40, adultYears = 10, adoptAgeDays = 60,
                rates = new NeedRates().Scaled(0.7),
                foods = new[] { "meat", "fish", "steak" }, commands = new[] { "roar", "come", "spin" }, brushable = false, habitat = "pond"
            },

            // Dino Paketi (ücretli, plan §3): ömürleri biz belirledik (20 yıl), bebek dinozor olarak gelirler
            new Species
            {
                id = "t_rex", lifespanYears = 20, adultYears = 2, adoptAgeDays = 30, pack = "dino",
                rates = new NeedRates { fullness = 7, fun = 6 },
                foods = new[] { "meat", "steak", "bone" }, commands = new[] { "roar", "come", "spin" }, brushable = false, habitat = "jungle"
            },
            new Species
            {
                id = "triceratops", lifespanYears = 20, adultYears = 2, adoptAgeDays = 30, pack = "dino",
                foods = new[] { "fern", "greens", "fruit" }, commands = new[] { "come", "roar", "spin" }, brushable = false, habitat = "jungle"
            },
            new Species
            {
                id = "brachiosaurus", lifespanYears = 20, adultYears = 2, adoptAgeDays = 30, pack = "dino",
                rates = new NeedRates { fullness = 8 },
                foods = new[] { "acacia", "fern", "fruit" }, commands = new[] { "come", "stand", "roar" }, brushable = false, habitat = "jungle"
            },
        };

        public static Species Get(string id)
        {
            foreach (Species species in All)
                if (species.id == id) return species;
            throw new ArgumentException("Bilinmeyen tür: " + id);
        }
    }
}
