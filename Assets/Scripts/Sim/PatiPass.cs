using System;

namespace PatiDostum
{
    /// <summary>Bir Pass basamağının ödülü (sabit; şans yok).</summary>
    public sealed class PassReward
    {
        public int gold;
        public int diamonds;
        /// <summary>Yalnızca Pass ile alınabilen kıyafet (Wardrobe source "pass").</summary>
        public string item = "";
    }

    /// <summary>
    /// Pati Pass (saf C#; testleri ProgressTests). Her ay yeni sezon: görevler, seri ve şovlar puan verir,
    /// her 300 puanda bir basamak (10 basamak). Bedava yolu herkes alır; premium yol "pati_pass" ürünüyle o sezon
    /// için açılır. Ödüller önceden görünür ve sabittir (plan §7b: şans kutusu yok).
    /// </summary>
    public static class PatiPass
    {
        public const int Levels = 10;
        public const int XpPerLevel = 300;

        public static readonly PassReward[] Free =
        {
            new PassReward { gold = 20 }, new PassReward { gold = 30 }, new PassReward { gold = 40 },
            new PassReward { gold = 50 }, new PassReward { gold = 60 }, new PassReward { gold = 70 },
            new PassReward { gold = 80 }, new PassReward { gold = 90 }, new PassReward { gold = 100 },
            new PassReward { gold = 150, diamonds = 1 },
        };

        public static readonly PassReward[] Premium =
        {
            new PassReward { diamonds = 2 }, new PassReward { item = "flower_crown" }, new PassReward { gold = 200 },
            new PassReward { diamonds = 2 }, new PassReward { item = "star_glasses" }, new PassReward { gold = 300 },
            new PassReward { diamonds = 3 }, new PassReward { gold = 400 }, new PassReward { diamonds = 3 },
            new PassReward { item = "tiara" },
        };

        public static int Season(DateTime local) => local.Year * 100 + local.Month;

        public static int DaysLeft(DateTime local)
        {
            DateTime next = new DateTime(local.Year, local.Month, 1).AddMonths(1);
            return Math.Max(1, (int)Math.Ceiling((next - local).TotalDays));
        }

        /// <summary>Ay değişince puan ve alınan ödüller sıfırlanır.</summary>
        public static void EnsureSeason(SaveData save, DateTime nowLocal)
        {
            int season = Season(nowLocal);
            if (save.passSeason == season) return;
            save.passSeason = season;
            save.passXp = 0;
            save.passClaimedFree.Clear();
            save.passClaimedPremium.Clear();
        }

        public static void AddXp(SaveData save, int xp, DateTime nowLocal)
        {
            EnsureSeason(save, nowLocal);
            save.passXp = Math.Min(save.passXp + xp, Levels * XpPerLevel);
        }

        /// <summary>Ulaşılan basamak (0–10).</summary>
        public static int Level(SaveData save) => save.passXp / XpPerLevel;

        public static bool HasPremium(SaveData save, DateTime nowLocal) => save.passPremiumSeason == Season(nowLocal);

        /// <summary><paramref name="level"/> 1'den başlar.</summary>
        public static bool CanClaim(SaveData save, int level, bool premium, DateTime nowLocal)
        {
            EnsureSeason(save, nowLocal);
            if (level < 1 || level > Levels || Level(save) < level) return false;
            if (premium && !HasPremium(save, nowLocal)) return false;
            return !(premium ? save.passClaimedPremium : save.passClaimedFree).Contains(level);
        }

        public static bool Claim(SaveData save, int level, bool premium, DateTime nowLocal)
        {
            if (!CanClaim(save, level, premium, nowLocal)) return false;
            PassReward reward = (premium ? Premium : Free)[level - 1];
            (premium ? save.passClaimedPremium : save.passClaimedFree).Add(level);
            save.gold += reward.gold;
            save.diamonds += reward.diamonds;
            if (!string.IsNullOrEmpty(reward.item) && !save.ownedItems.Contains(reward.item)) save.ownedItems.Add(reward.item);
            return true;
        }
    }

    /// <summary>
    /// Pati Kulübü aboneliği (plan §7): her gün 1 elmas, mini oyunlarda +%50 altın, ayda bir pansiyon bileti,
    /// Yıldızlar Şovu'na bedava giriş ve kulübe özel papyon. Google Play her açılışta aboneliği doğrular;
    /// telefon uzun süre çevrimdışıysa son doğrulamadan 35 gün sonra kulüp kapanır.
    /// </summary>
    public static class Club
    {
        public const double VerifyDays = 35.0;
        public const double GoldBonus = 1.5;
        public const string Item = "club_bow";

        public static bool Active(SaveData save, DateTime nowUtc) => save.clubUntilUtcTicks > nowUtc.Ticks;

        public static void Confirm(SaveData save, DateTime nowUtc)
        {
            save.clubUntilUtcTicks = nowUtc.AddDays(VerifyDays).Ticks;
            if (!save.ownedItems.Contains(Item)) save.ownedItems.Add(Item);
        }

        public static void Lapse(SaveData save) => save.clubUntilUtcTicks = 0;

        public static bool CanClaimDiamond(SaveData save, DateTime nowLocal)
        {
            return Active(save, nowLocal.ToUniversalTime()) && save.clubDiamondDay != Vet.DayNumber(nowLocal);
        }

        public static bool ClaimDiamond(SaveData save, DateTime nowLocal)
        {
            if (!CanClaimDiamond(save, nowLocal)) return false;
            save.clubDiamondDay = Vet.DayNumber(nowLocal);
            save.diamonds++;
            return true;
        }

        public static bool CanClaimBoarding(SaveData save, DateTime nowLocal)
        {
            return Active(save, nowLocal.ToUniversalTime()) && save.clubBoardingMonth != PatiPass.Season(nowLocal);
        }

        public static bool ClaimBoarding(SaveData save, DateTime nowLocal)
        {
            if (!CanClaimBoarding(save, nowLocal)) return false;
            save.clubBoardingMonth = PatiPass.Season(nowLocal);
            save.boardingTickets++;
            return true;
        }
    }
}
