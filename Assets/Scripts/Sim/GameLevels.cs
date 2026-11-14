using System;
using System.Collections.Generic;

namespace PatiDostum
{
    /// <summary>Oyun bitince seviye sonucu: yıldız, seviye atladı mı, bonus ödül.</summary>
    public struct LevelOutcome
    {
        public int level, stars, bonusGold, bonusDiamonds;
        public bool leveledUp;
    }

    /// <summary>
    /// Mini oyun seviyeleri (saf C#; testleri Editor/Tests/GameLevelTests). Her oyunun seviyesi kayıtta durur, bütün
    /// dostlar için ortaktır. 2 yıldız alan bir üst seviyeye geçer ve bedava altın alır; her 5 seviyede 1 mavi elmas.
    /// Seviye geri düşmez: çocuk zorlanırsa aynı seviyeyi yine oynar, oynadıkça altını yine kazanır.
    /// </summary>
    public static class GameLevels
    {
        public static int Get(SaveData save, MiniGame game)
        {
            int index = (int)game;
            return index < save.gameLevels.Count ? Math.Max(1, save.gameLevels[index]) : 1;
        }

        private static void Set(SaveData save, MiniGame game, int level)
        {
            while (save.gameLevels.Count <= (int)game) save.gameLevels.Add(1);
            save.gameLevels[(int)game] = level;
        }

        // ---------- Oyun başına zorluk ----------

        /// <summary>Top getir: atış sayısı 5'ten 8'e çıkar.</summary>
        public static int FetchThrows(int level) => Math.Min(MiniGameRules.FetchThrows + (level - 1) / 3, 8);

        /// <summary>Top getir: işaretin hızı.</summary>
        public static float FetchSpeed(int level) => Math.Min(0.9f + (level - 1) * 0.12f, 2.6f);

        /// <summary>Top getir: 3 yıldız bölgesinin yarı genişliği; 2 ve 1 yıldız bunun katları.</summary>
        public static float FetchZone(int level) => Math.Max(0.03f, 0.06f - (level - 1) * 0.004f);

        /// <summary>Top getir: 3. seviyeden sonra hedef her atışta başka yerde.</summary>
        public static bool FetchMovingTarget(int level) => level >= 3;

        public static int FetchStars(float power, float target, int level)
        {
            float distance = Math.Abs(power - target), zone = FetchZone(level);
            if (distance <= zone) return 3;
            if (distance <= zone * 2.5f) return 2;
            if (distance <= zone * 5f) return 1;
            return 0;
        }

        /// <summary>Yiyecek yakala: düşme hızı çarpanı.</summary>
        public static float CatchSpeed(int level) => Math.Min(1f + (level - 1) * 0.08f, 2.2f);

        /// <summary>Yiyecek yakala: çöp oranı.</summary>
        public static float CatchJunk(int level) => Math.Min(0.2f + (level - 1) * 0.015f, 0.4f);

        /// <summary>Hafıza: kart çifti 4'ten 10'a çıkar (8 karttan 20 karta).</summary>
        public static int MemoryPairs(int level) => Math.Min(3 + level, MiniGameRules.MemoryMaxPairs);

        /// <summary>Hafıza puanı: hatasız en yüksek, her fazla hamle 1 puan götürür.</summary>
        public static int MemoryScore(int moves, int pairs) => Math.Max(pairs, pairs * 3 + 2 - Math.Max(0, moves - pairs));

        /// <summary>Saklambaç: kutu sayısı 3'ten 5'e çıkar.</summary>
        public static int HideBoxes(int level) => level >= 8 ? 5 : level >= 4 ? 4 : MiniGameRules.HideBoxes;

        /// <summary>Saklambaç: bir turdaki karışma sayısı.</summary>
        public static int HideSwaps(int level, int round) => 4 + round + (level - 1) / 2;

        /// <summary>Kabarcık: yükselme hızı çarpanı.</summary>
        public static float BubbleSpeed(int level) => Math.Min(1f + (level - 1) * 0.08f, 2.2f);

        // ---------- Puan ve seviye ----------

        /// <summary>3 yıldız için gereken puan (bunun %80'i 3, %45'i 2 yıldız).</summary>
        public static int GreatScore(MiniGame game, int level)
        {
            switch (game)
            {
                case MiniGame.Fetch: return FetchThrows(level) * 3 - 3;
                case MiniGame.Catch: return Math.Min(16 + (level - 1) * 3, 60);
                case MiniGame.Memory: return MemoryPairs(level) * 3;
                case MiniGame.Hide: return MiniGameRules.HideRounds;
                case MiniGame.Bubble: return Math.Min(20 + (level - 1) * 3, 60);
                default: return 1;
            }
        }

        /// <summary>0-3 yıldız: puan varsa en az 1 (oynadı, aferin); puan 0 ise yıldız yok.</summary>
        public static int Stars(MiniGame game, int level, int score)
        {
            if (score <= 0) return 0;
            float ratio = score / (float)GreatScore(game, level);
            return ratio >= 0.8f ? 3 : ratio >= 0.45f ? 2 : 1;
        }

        /// <summary>Seviye atlama ödülü: seviye başına artan altın, her 5 seviyede 1 mavi elmas.</summary>
        public static int LevelGold(int newLevel) => 20 + newLevel * 10;

        public static int LevelDiamonds(int newLevel) => newLevel % 5 == 0 ? 1 : 0;

        /// <summary>Oyun bitti: yıldızı hesaplar, 2 yıldız ve üstü bir üst seviyeyi açar, ödülü verir.</summary>
        public static LevelOutcome Complete(SaveData save, MiniGame game, int score)
        {
            int level = Get(save, game);
            var outcome = new LevelOutcome { level = level, stars = Stars(game, level, score) };
            if (outcome.stars < 2) return outcome;
            outcome.leveledUp = true;
            outcome.level = level + 1;
            outcome.bonusGold = LevelGold(outcome.level);
            outcome.bonusDiamonds = LevelDiamonds(outcome.level);
            save.gold += outcome.bonusGold;
            save.diamonds += outcome.bonusDiamonds;
            Set(save, game, outcome.level);
            return outcome;
        }

        /// <summary>Tür için uygun oyunlar: balık top getiremez.</summary>
        public static bool Available(Species species, MiniGame game)
        {
            return game != MiniGame.Fetch || species.moves != MoveKind.Swim;
        }

        public static MiniGame[] AllGames => (MiniGame[])Enum.GetValues(typeof(MiniGame));
    }
}
