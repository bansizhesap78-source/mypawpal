using System;

namespace PatiDostum
{
    /// <summary>Mini oyunların puanlama kuralları (saf C#; arayüzleri MiniGames/ altında, testleri EconomyTests).</summary>
    public static class MiniGameRules
    {
        // Top getir: gidip gelen güç çubuğunda hedef bölgeye ne kadar yakın durdurulduğu
        public const int FetchThrows = 5;
        public const float FetchTarget = 0.75f;

        public static int FetchStars(float power)
        {
            float distance = Math.Abs(power - FetchTarget);
            if (distance <= 0.06f) return 3;
            if (distance <= 0.15f) return 2;
            if (distance <= 0.3f) return 1;
            return 0;
        }

        // Yiyecek yakala
        public const float CatchSeconds = 30f;

        // Hafıza kartları: 6 çift; her fazla hamle 1 puan götürür, en az 5
        public const int MemoryPairs = 6;
        public const int MemoryMaxPairs = 10;

        public static int MemoryScore(int moves)
        {
            return Math.Max(5, 20 - Math.Max(0, moves - MemoryPairs));
        }

        /// <summary>Çiftleri karıştırılmış deste: her değer iki kez (0..pairs-1).</summary>
        public static int[] MemoryDeck(int pairs, Random random)
        {
            var deck = new int[pairs * 2];
            for (int i = 0; i < deck.Length; i++) deck[i] = i / 2;
            for (int i = deck.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            return deck;
        }

        // Saklambaç: 3 kutu, 5 tur
        public const int HideRounds = 5;
        public const int HideBoxes = 3;
        public const int HideMaxBoxes = 5;

        // Okul: sıra hafızası (3 renkli düğme, dizi 3 adım)
        public const int SimonPads = 4;
        public const int SimonLength = 3;
    }
}
