using System;
using System.Collections.Generic;

namespace PatiDostum
{
    /// <summary>Anı Bahçesi'ndeki bir dost: ömrünü tamamlayan ya da hastalıktan ölen hayvan.</summary>
    [Serializable]
    public class MemorialEntry
    {
        public string name;
        public string species;
        public int color;
        public int mode;
        public double ageDays;
        public long adoptedUtcTicks;
        public long diedUtcTicks;
        /// <summary>"natural", "neglect" ya da "shelter" (barınak ona yeni bir aile buldu).</summary>
        public string cause;
        public int rescues;
    }

    /// <summary>
    /// Oyunun tek kayıt dosyası (JsonUtility ile). Alan eklerken varsayılanı ver; eski kayıtta olmayan alan
    /// varsayılanla gelir. Alanın anlamı değişirse CurrentVersion artırılır ve SaveStore.Migrate'e adım eklenir.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>
        /// 2: hayvanlara ömür ve sağlık alanları geldi (M2). 3: kıyafet, komut, mini oyun sayacı (M3).
        /// 4: satın almalar, slotlar, biletler, harcama limiti (M4).
        /// 5: seri, görevler, rozetler, şov, pass, kulüp, temalar, soy ağacı (M5).
        /// </summary>
        public const int CurrentVersion = 5;

        /// <summary>Yeni oyuncunun başlangıç parası (plan §6: kurtarma için 5 elmas).</summary>
        public const int StartingGold = 100;
        public const int StartingDiamonds = 5;
        /// <summary>İlkinden sonraki her yeni dostla gelen elmas hediyesi.</summary>
        public const int NewPetDiamonds = 3;

        public int version = CurrentVersion;
        public long createdUtcTicks;
        public ClockState clock = new ClockState();

        public int gold;
        public int diamonds;

        /// <summary>Sahiplenilen hayvanlar (en fazla 3; şimdilik 1).</summary>
        public List<PetState> pets = new List<PetState>();
        /// <summary>Anı Bahçesi.</summary>
        public List<MemorialEntry> memorial = new List<MemorialEntry>();
        /// <summary>Bugüne kadar sahiplenilen dost sayısı.</summary>
        public int adoptions;

        /// <summary>Alınmış kıyafetler (bütün dostlar giyebilir).</summary>
        public List<string> ownedItems = new List<string>();
        /// <summary>Mini oyun günlük sayacı: hangi gün (Vet.DayNumber) ve o gün kaç oyun.</summary>
        public long playDay;
        public int playsToday;

        // Satın almalar (M4)
        /// <summary>Alınmış kalıcı ürünler (ProductCatalog id; Play'den geri yüklenir).</summary>
        public List<string> ownedProducts = new List<string>();
        /// <summary>Hakkı verilmiş son satın alma jetonları: aynı alım iki kez verilmesin.</summary>
        public List<string> grantedTokens = new List<string>();
        public int modeChangers;
        public int shelterTickets;
        public int boardingTickets;
        /// <summary>Pansiyon bu ana kadar sürer (UTC tick); bu sürede ihtiyaçlar donar.</summary>
        public long boardingUntilUtcTicks;
        /// <summary>Odada gösterilen dost (PetState.id).</summary>
        public string selectedPet = "";

        // Ebeveyn: aylık harcama limiti (TL; 0 = limitsiz)
        public int monthlyLimit;
        public int spendMonth;
        public double spentThisMonth;

        // Bağlılık (M5)
        public int streakDays;
        public long lastCareDay;
        public long taskDay;
        public List<string> taskIds = new List<string>();
        public List<int> taskCounts = new List<int>();
        public List<bool> taskClaimed = new List<bool>();
        public List<string> badges = new List<string>();
        public int totalGames;
        public int totalLessons;
        public int totalTreatments;
        public int showWins;
        public int starWins;
        public int trophies;
        public int lastShowWeek;
        public int lastStarShowWeek;

        /// <summary>Mini oyun seviyeleri (MiniGame sırası); eksikse 1.</summary>
        public List<int> gameLevels = new List<int>();

        // Pati Pass (sezon = yıl*100 + ay)
        public int passSeason;
        public int passXp;
        public int passPremiumSeason;
        public List<int> passClaimedFree = new List<int>();
        public List<int> passClaimedPremium = new List<int>();

        // Pati Kulübü (abonelik): bu ana kadar etkin sayılır (Play her açılışta yeniden doğrular)
        public long clubUntilUtcTicks;
        public long clubDiamondDay;
        public int clubBoardingMonth;

        // Ev dekorasyonu
        public List<string> ownedThemes = new List<string>();
        public string theme = "classic";

        // Ayarlar
        public bool soundOn = true;
        public bool notificationsOn = true;
        /// <summary>Ebeveyn ayarı "Hayvanlar ölmesin": ihmal edilen hayvan kritik kalır ama ölmez.</summary>
        public bool noDeath;

        public static SaveData CreateNew(DateTime nowUtc)
        {
            return new SaveData
            {
                createdUtcTicks = nowUtc.Ticks,
                gold = StartingGold,
                diamonds = StartingDiamonds
            };
        }
    }
}
