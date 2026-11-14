using System;

namespace PatiDostum
{
    public enum VetResult
    {
        Done,
        NotSick,
        NoMoney,
        AlreadyVaccinated,
        /// <summary>Kritik durumda normal tedavi yetmez: Mucize Tedavi gerekir.</summary>
        Critical,
        NotCritical,
        /// <summary>Bedava acil müdahale bugün kullanıldı.</summary>
        UsedToday
    }

    /// <summary>
    /// Veteriner (saf C#; testleri Editor/Tests/VetTests). Plan §6:
    ///  - Hastalık altınla tedavi edilir; altını yetmeyen için günde bir bedava acil müdahale vardır.
    ///  - Aşı soğuk algınlığı riskini dörtte bire indirir, bir oyun yılı geçerlidir.
    ///  - Kritik (ölüm döşeği) hayvanı yalnızca Mucize Tedavi kurtarır: mavi elmasla, hayvan başına ilki bedava.
    /// </summary>
    public static class Vet
    {
        public const int VaccineCost = 40;
        public const double VaccineGameDays = 365.0;
        public const double TreatHealthBonus = 10.0;
        public const double ObesityWeightLoss = 20.0;
        /// <summary>Mucize Tedavi bedeli (elmas): ilk kurtarma bedava, sonra 3, 5, 8.</summary>
        public static readonly int[] MiracleCosts = { 0, 3, 5, 8 };
        public const int SeniorMiracleExtra = 2;

        public static int TreatCost(PetState p)
        {
            Disease disease = DiseaseCatalog.Get(p.disease);
            return disease != null ? disease.cost : 0;
        }

        public static bool IsVaccinated(PetState p) => p.ageSeconds < p.vaccineUntilAge;

        public static int MiracleCost(PetState p, Species s)
        {
            int cost = MiracleCosts[Math.Min(p.rescues, MiracleCosts.Length - 1)];
            if (cost > 0 && Mood.Stage(p, s) == LifeStage.Senior) cost += SeniorMiracleExtra;
            return cost;
        }

        public static VetResult Treat(PetState p, SaveData wallet)
        {
            VetResult check = CanTreat(p);
            if (check != VetResult.Done) return check;
            int cost = TreatCost(p);
            if (wallet.gold < cost) return VetResult.NoMoney;
            wallet.gold -= cost;
            Cure(p);
            return VetResult.Done;
        }

        public static long DayNumber(DateTime local) => local.Date.Ticks / TimeSpan.TicksPerDay;

        public static bool FreeTreatAvailable(PetState p, DateTime nowLocal) => p.freeTreatDay != DayNumber(nowLocal);

        /// <summary>Altını yetmeyen için günde bir bedava tedavi (çocuk parasızlıktan hayvanını kaybetmesin).</summary>
        public static VetResult FreeTreat(PetState p, DateTime nowLocal)
        {
            VetResult check = CanTreat(p);
            if (check != VetResult.Done) return check;
            if (!FreeTreatAvailable(p, nowLocal)) return VetResult.UsedToday;
            p.freeTreatDay = DayNumber(nowLocal);
            Cure(p);
            return VetResult.Done;
        }

        public static VetResult Vaccinate(PetState p, SaveData wallet)
        {
            if (IsVaccinated(p)) return VetResult.AlreadyVaccinated;
            if (wallet.gold < VaccineCost) return VetResult.NoMoney;
            wallet.gold -= VaccineCost;
            p.vaccineUntilAge = p.ageSeconds + VaccineGameDays * 86400.0;
            return VetResult.Done;
        }

        public static VetResult Miracle(PetState p, Species s, SaveData wallet)
        {
            if (!PetSimulator.IsCritical(p)) return VetResult.NotCritical;
            int cost = MiracleCost(p, s);
            if (wallet.diamonds < cost) return VetResult.NoMoney;
            wallet.diamonds -= cost;
            p.rescues++;
            p.disease = "";
            p.criticalSeconds = 0.0;
            p.health = 60.0;
            p.fullness = Math.Max(p.fullness, 50.0);
            p.thirst = Math.Max(p.thirst, 50.0);
            p.energy = Math.Max(p.energy, 50.0);
            p.hygiene = Math.Max(p.hygiene, 50.0);
            p.fun = Math.Max(p.fun, 50.0);
            p.asleep = false;
            return VetResult.Done;
        }

        private static VetResult CanTreat(PetState p)
        {
            if (!p.IsSick) return VetResult.NotSick;
            if (PetSimulator.IsCritical(p)) return VetResult.Critical;
            return VetResult.Done;
        }

        private static void Cure(PetState p)
        {
            if (p.disease == DiseaseCatalog.Obesity) p.weight -= ObesityWeightLoss;
            if (p.disease == DiseaseCatalog.Toothache) p.candy = 0.0;
            p.disease = "";
            p.health += TreatHealthBonus;
            PetSimulator.Clamp(p);
        }
    }
}
