using System.Collections.Generic;
using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// Çizimler gelene kadar her türün şekillerden yer tutucu görünüşü: hangi parçalar açık (kulak tipi, gaga,
    /// kanat, kuyruk, kabuk...) ve sahiplenirken seçilebilen 4 renk. Parçalar SceneBuilder.Critter'da "Look_*"
    /// adıyla kurulur; PetView burada adı geçenleri açar.
    /// </summary>
    public static class PetLooks
    {
        public const string Paws = "Look_Paws";
        public const string Nose = "Look_Nose";

        /// <summary>Her türde açılan parçalar (listede olmayan Look_* parçaları kapalı).</summary>
        private static readonly Dictionary<string, string[]> Parts = new Dictionary<string, string[]>
        {
            { "cat", new[] { "Look_EarsPointy", "Look_TailLong", Paws, Nose } },
            { "dog", new[] { "Look_EarsFloppy", "Look_TailShort", Paws, Nose } },
            { "rabbit", new[] { "Look_EarsLong", "Look_TailPuff", Paws, Nose } },
            { "hamster", new[] { "Look_EarsRound", "Look_Cheeks", Paws, Nose } },
            { "guinea_pig", new[] { "Look_EarsRound", Paws, Nose } },
            { "hedgehog", new[] { "Look_Spikes", "Look_EarsRound", Paws, Nose } },
            { "budgie", new[] { "Look_Beak", "Look_Wings", "Look_TailFeathers", "Look_BirdFeet" } },
            { "duck", new[] { "Look_Bill", "Look_Wings", "Look_BirdFeet" } },
            { "goldfish", new[] { "Look_FishTail", "Look_Fins" } },
            { "turtle", new[] { "Look_Shell", Paws } },
            { "t_rex", new[] { "Look_DinoTail", "Look_Plates", Paws } },
            { "triceratops", new[] { "Look_DinoTail", "Look_Frill", "Look_Horns", Paws } },
            { "brachiosaurus", new[] { "Look_DinoTail", "Look_Spots", Paws } },
            { "penguin", new[] { "Look_Bill", "Look_Wings", "Look_BirdFeet" } },
            { "lion", new[] { "Look_EarsPointy", "Look_TailLong", Paws, Nose } },
            { "giraffe", new[] { "Look_EarsPointy", "Look_TailLong", Paws, Nose } },
            { "panda", new[] { "Look_EarsPointy", Paws, Nose } },
            { "crocodile", new[] { "Look_DinoTail", Paws } },
        };

        private static readonly Dictionary<string, Color32[]> Colors = new Dictionary<string, Color32[]>
        {
            { "cat", new Color32[] { new Color32(0xF2, 0xA6, 0x5A, 255), new Color32(0x9A, 0xA0, 0xA8, 255), new Color32(0x4E, 0x4A, 0x58, 255), new Color32(0xF5, 0xEF, 0xE6, 255) } },
            { "dog", new Color32[] { new Color32(0xE0, 0xB0, 0x60, 255), new Color32(0x9A, 0x6A, 0x44, 255), new Color32(0x4E, 0x4A, 0x58, 255), new Color32(0xF2, 0xEE, 0xE8, 255) } },
            { "rabbit", new Color32[] { new Color32(0xF5, 0xF2, 0xEC, 255), new Color32(0xA8, 0xA8, 0xB0, 255), new Color32(0xB0, 0x80, 0x60, 255), new Color32(0x6B, 0x64, 0x70, 255) } },
            { "hamster", new Color32[] { new Color32(0xE8, 0xB0, 0x70, 255), new Color32(0xF3, 0xE2, 0xC4, 255), new Color32(0xB0, 0xA8, 0xA0, 255), new Color32(0xF7, 0xF3, 0xEE, 255) } },
            { "guinea_pig", new Color32[] { new Color32(0xD8, 0x9A, 0x5E, 255), new Color32(0xF1, 0xE6, 0xD4, 255), new Color32(0x7A, 0x5A, 0x44, 255), new Color32(0xC8, 0xB8, 0xA8, 255) } },
            { "hedgehog", new Color32[] { new Color32(0xD8, 0xC0, 0xA0, 255), new Color32(0xC0, 0xA0, 0x80, 255), new Color32(0xE8, 0xDC, 0xC8, 255), new Color32(0xA8, 0x90, 0x78, 255) } },
            { "budgie", new Color32[] { new Color32(0x7B, 0xD1, 0x5A, 255), new Color32(0x5A, 0xB0, 0xF0, 255), new Color32(0xF2, 0xD9, 0x4E, 255), new Color32(0xED, 0xED, 0xF5, 255) } },
            { "duck", new Color32[] { new Color32(0xF6, 0xD8, 0x5A, 255), new Color32(0xF5, 0xF5, 0xF0, 255), new Color32(0xA8, 0x86, 0x60, 255), new Color32(0xB8, 0xB8, 0xB8, 255) } },
            { "goldfish", new Color32[] { new Color32(0xFF, 0x9A, 0x3C, 255), new Color32(0xF0, 0x60, 0x3C, 255), new Color32(0xF5, 0xF0, 0xE6, 255), new Color32(0x4A, 0x4A, 0x55, 255) } },
            { "turtle", new Color32[] { new Color32(0x7E, 0xA8, 0x60, 255), new Color32(0x9A, 0xA0, 0x5A, 255), new Color32(0x5F, 0x7A, 0x50, 255), new Color32(0x5E, 0xA0, 0x90, 255) } },
            { "t_rex", new Color32[] { new Color32(0x6E, 0xC0, 0x5A, 255), new Color32(0xF0, 0x8A, 0x4A, 255), new Color32(0x9C, 0x7B, 0xE0, 255), new Color32(0x4F, 0x9F, 0xD8, 255) } },
            { "triceratops", new Color32[] { new Color32(0x5E, 0xB0, 0xA8, 255), new Color32(0xE0, 0x9A, 0x5A, 255), new Color32(0xB0, 0x8A, 0xD8, 255), new Color32(0x8A, 0xB8, 0x5A, 255) } },
            { "brachiosaurus", new Color32[] { new Color32(0x8A, 0xC8, 0xE8, 255), new Color32(0xA8, 0xD0, 0x6A, 255), new Color32(0xF2, 0xB8, 0x6A, 255), new Color32(0xC8, 0x9A, 0xD8, 255) } },
            { "penguin", new Color32[] { new Color32(0x4A, 0x4A, 0x55, 255), new Color32(0x3E, 0x5C, 0x8A, 255), new Color32(0x7A, 0x5A, 0x44, 255), new Color32(0xB8, 0xBC, 0xC4, 255) } },
            { "lion", new Color32[] { new Color32(0xF0, 0xB8, 0x50, 255), new Color32(0xF3, 0xE2, 0xC4, 255), new Color32(0xD8, 0xB0, 0x80, 255), new Color32(0xA8, 0xA8, 0xB0, 255) } },
            { "giraffe", new Color32[] { new Color32(0xF2, 0xC8, 0x4E, 255), new Color32(0xF3, 0xE6, 0xC8, 255), new Color32(0xF0, 0x9A, 0x3E, 255), new Color32(0xB8, 0xB8, 0xB8, 255) } },
            { "panda", new Color32[] { new Color32(0x3A, 0x3A, 0x44, 255), new Color32(0x9A, 0x6A, 0x44, 255), new Color32(0xA8, 0xA8, 0xB0, 255), new Color32(0x7A, 0x5A, 0x44, 255) } },
            { "crocodile", new Color32[] { new Color32(0x6E, 0xC0, 0x5A, 255), new Color32(0x5F, 0x7A, 0x40, 255), new Color32(0xA8, 0xE0, 0x5A, 255), new Color32(0xC8, 0xA8, 0x78, 255) } },
        };

        /// <summary>
        /// Özel Renkler paketiyle açılan iki ek renk (4 ve 5): kedide Van kedisi beyazı ve Ankara kedisi kreması,
        /// köpekte Husky grisi ve Golden, tavşanda albino, balıkta gökkuşağı moru gibi.
        /// </summary>
        private static readonly Dictionary<string, Color32[]> PremiumColors = new Dictionary<string, Color32[]>
        {
            { "cat", new Color32[] { new Color32(0xFF, 0xFF, 0xFF, 255), new Color32(0xF3, 0xE3, 0xC3, 255) } },
            { "dog", new Color32[] { new Color32(0x8C, 0x96, 0xA8, 255), new Color32(0xF0, 0xC0, 0x60, 255) } },
            { "rabbit", new Color32[] { new Color32(0xFF, 0xF8, 0xF8, 255), new Color32(0xD8, 0xB0, 0x90, 255) } },
            { "goldfish", new Color32[] { new Color32(0xB0, 0x7A, 0xF0, 255), new Color32(0x4F, 0xD0, 0xE0, 255) } },
            { "t_rex", new Color32[] { new Color32(0xF5, 0x9E, 0xC8, 255), new Color32(0xF2, 0xC9, 0x4E, 255) } },
            { "lion", new Color32[] { new Color32(0xFF, 0xFF, 0xFF, 255), new Color32(0xC9, 0xA7, 0xF2, 255) } },
            { "crocodile", new Color32[] { new Color32(0xF8, 0xEE, 0xEE, 255), new Color32(0xB0, 0x7A, 0xF0, 255) } },
            { "triceratops", new Color32[] { new Color32(0xF5, 0x9E, 0xC8, 255), new Color32(0x6A, 0xB8, 0xF0, 255) } },
            { "brachiosaurus", new Color32[] { new Color32(0xF5, 0x9E, 0xC8, 255), new Color32(0x6A, 0xD0, 0xC0, 255) } },
        };

        private static readonly Color32[] DefaultPremium = { new Color32(0xB0, 0x7A, 0xF0, 255), new Color32(0x4F, 0xD0, 0xE0, 255) };

        public const int ColorCount = 6;
        public const int FreeColors = 4;

        public static Color ColorOf(string species, int index)
        {
            if (index >= FreeColors)
            {
                Color32[] premium = PremiumColors.TryGetValue(species ?? "", out Color32[] p) ? p : DefaultPremium;
                return premium[Mathf.Clamp(index - FreeColors, 0, premium.Length - 1)];
            }
            Color32[] colors = Colors.TryGetValue(species ?? "", out Color32[] list) ? list : Colors["cat"];
            return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
        }

        /// <summary>Oda temasının rengi: odanın kendi rengiyle yarı yarıya karışır.</summary>
        public static Color ThemeTint(string theme)
        {
            switch (theme)
            {
                case "forest": return new Color32(0xB8, 0xE0, 0xA8, 255);
                case "ocean": return new Color32(0xA8, 0xD8, 0xF0, 255);
                case "space": return new Color32(0x8C, 0x84, 0xC8, 255);
                case "princess": return new Color32(0xF8, 0xC8, 0xE0, 255);
                default: return Color.clear;
            }
        }

        /// <summary>Kıyafetlerin yer tutucu rengi (gardırop kartındaki önizleme ve dostun üstündeki parça).</summary>
        public static readonly Dictionary<string, Color32> ItemColors = new Dictionary<string, Color32>
        {
            { "party_hat", new Color32(0xFF, 0x6F, 0xA8, 255) },
            { "cap", new Color32(0x4F, 0x8F, 0xE8, 255) },
            { "crown", new Color32(0xFF, 0xC9, 0x3C, 255) },
            { "round_glasses", new Color32(0x4A, 0x33, 0x55, 255) },
            { "sunglasses", new Color32(0x22, 0x22, 0x2A, 255) },
            { "bow_tie", new Color32(0xE5, 0x48, 0x48, 255) },
            { "scarf", new Color32(0x4C, 0xAF, 0x8A, 255) },
            { "sweater", new Color32(0x9C, 0x7B, 0xE0, 255) },
            { "raincoat", new Color32(0xFF, 0xD2, 0x3F, 255) },
            { "booties", new Color32(0x5A, 0xB0, 0xF0, 255) },
            { "witch_hat", new Color32(0x6A, 0x4A, 0x9C, 255) },
            { "santa_hat", new Color32(0xE5, 0x48, 0x48, 255) },
            { "sun_hat", new Color32(0xF2, 0xD0, 0x7A, 255) },
            { "star_bow", new Color32(0x4F, 0x8F, 0xE8, 255) },
            { "flower_crown", new Color32(0xFF, 0x9E, 0xC8, 255) },
            { "star_glasses", new Color32(0xFF, 0xC9, 0x3C, 255) },
            { "cape", new Color32(0xC0, 0x3A, 0x5A, 255) },
            { "club_bow", new Color32(0xFF, 0xC9, 0x3C, 255) },
        };

        public static bool Shows(string species, string part)
        {
            return Parts.TryGetValue(species ?? "", out string[] parts) && System.Array.IndexOf(parts, part) >= 0;
        }
    }
}
