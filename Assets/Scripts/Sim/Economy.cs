using System;

namespace PatiDostum
{
    public enum MiniGame { Fetch, Catch, Memory, Hide, Bubble }

    /// <summary>
    /// Altın ekonomisi (saf C#; testleri Editor/Tests/EconomyTests). Altın şimdilik mini oyunlardan gelir
    /// (M5'te bakım serisi, günlük görevler ve Pet Şov da verecek). Harcandığı yerler: lezzetli yiyecekler,
    /// veteriner, aşı, kıyafetler. Temel yem ve su bedavadır.
    /// Oynamak bol bol kazandırır: bir günün ilk 15 oyunu tam, sonraki 15'i yarım, daha sonrakiler çeyrek altın verir.
    /// Seviye arttıkça ödül de artar (GameLevels).
    /// </summary>
    public static class Economy
    {
        public const int FullPlaysPerDay = 15;
        public const int HalfPlaysPerDay = 30;
        public const double MinPlayEnergy = 15.0;
        /// <summary>En kötü oyunda bile verilen altın: oynamak hep kazandırır.</summary>
        public const int MinPlayGold = 5;

        /// <summary>Oyunun skorundan (oyuna göre) taban altın.</summary>
        public static int BaseGold(MiniGame game, int score, int level = 1)
        {
            score = Math.Max(0, score);
            // Üst sınır ve altın seviyeyle büyür: 2. seviye %15 fazla, tavan %50 yüksek
            double room = 1.0 + (level - 1) * 0.5, worth = 1.0 + (level - 1) * 0.15;
            int Cap(int limit) => Math.Min(score, (int)(limit * room));
            double gold;
            switch (game)
            {
                case MiniGame.Fetch: gold = Cap(15) * 2; break;     // atış × 0–3 yıldız
                case MiniGame.Catch: gold = Cap(30); break;         // yakalanan yiyecek
                case MiniGame.Memory: gold = Cap(20); break;        // hatasız en yüksek
                case MiniGame.Hide: gold = Math.Min(score, MiniGameRules.HideRounds) * 5; break;   // 5 turda doğru tahmin
                case MiniGame.Bubble: gold = Cap(30); break;        // patlatılan kabarcık
                default: return 0;
            }
            return (int)(gold * worth);
        }

        /// <summary>Bugünkü kaçıncı oyun olduğuna göre verilecek altın.</summary>
        public static int Reward(MiniGame game, int score, int playsBeforeToday, int level = 1)
        {
            int gold = BaseGold(game, score, level);
            if (playsBeforeToday >= HalfPlaysPerDay) return gold / 4;
            if (playsBeforeToday >= FullPlaysPerDay) return gold / 2;
            return gold;
        }

        public static bool CanPlay(PetState p) => !p.asleep && p.energy >= MinPlayEnergy;

        /// <summary>Oyun bitti: altını verir, dosta etkisini uygular; verilen altını döndürür.</summary>
        public static int FinishGame(SaveData save, PetState p, MiniGame game, int score, DateTime nowLocal)
        {
            long day = Vet.DayNumber(nowLocal);
            if (save.playDay != day)
            {
                save.playDay = day;
                save.playsToday = 0;
            }
            int gold = Reward(game, score, save.playsToday, GameLevels.Get(save, game));
            // Pati Kulübü üyelerine +%50
            if (Club.Active(save, nowLocal.ToUniversalTime())) gold = (int)Math.Round(gold * Club.GoldBonus);
            gold = Math.Max(gold, MinPlayGold);
            save.playsToday++;
            save.gold += gold;

            p.fun += 15.0;
            p.energy -= 10.0;
            p.thirst -= 5.0;
            p.love += 2.0;
            p.weight -= 1.0;
            PetSimulator.Clamp(p);
            return gold;
        }

        /// <summary>Gezintide toplanan altın: gezinti başına en çok bu kadar.</summary>
        public const int WalkCoinCap = 60;

        /// <summary>Altınla alım: yetiyorsa düşer ve true döner.</summary>
        public static bool Spend(SaveData save, int price)
        {
            if (price <= 0) return true;
            if (save.gold < price) return false;
            save.gold -= price;
            return true;
        }
    }
}
