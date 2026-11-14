namespace PatiDostum
{
    /// <summary>Bir hastalık: tedavi edilene kadar her saat ölçümlere etkisi ve veterinerde tedavi bedeli (altın).</summary>
    public sealed class Disease
    {
        public string id;
        public double healthPerHour;
        public double funPerHour;
        public double energyPerHour;
        public double hygienePerHour;
        public int cost;

        public string NameKey => "disease_" + id;
        /// <summary>Balonda görünen belirti ("Hapşu!").</summary>
        public string SymptomKey => "symptom_" + id;
    }

    /// <summary>
    /// 5 hastalık (plan §6). Nasıl bulaşır:
    ///  - cold (soğuk algınlığı): kirli ya da bitkinken; sağlık düşükken az da olsa. Aşı riski dörtte bire indirir.
    ///  - fleas (pire): çok kirliyken.
    ///  - stomach (mide bozukluğu): tok karnına yemek yiyince (PetActions.Feed).
    ///  - toothache (diş ağrısı): şeker yükü yüksekken ödül yiyince (PetActions.Feed).
    ///  - obesity (obezite): kilo 80'i geçince.
    /// </summary>
    public static class DiseaseCatalog
    {
        public const string Cold = "cold";
        public const string Fleas = "fleas";
        public const string Stomach = "stomach";
        public const string Toothache = "toothache";
        public const string Obesity = "obesity";

        public static readonly Disease[] All =
        {
            new Disease { id = Cold, healthPerHour = 3, energyPerHour = 2, cost = 30 },
            new Disease { id = Fleas, healthPerHour = 1.5, funPerHour = 4, hygienePerHour = 2, cost = 20 },
            new Disease { id = Stomach, healthPerHour = 4, funPerHour = 3, cost = 25 },
            new Disease { id = Toothache, healthPerHour = 2, funPerHour = 3, cost = 40 },
            new Disease { id = Obesity, healthPerHour = 1, energyPerHour = 3, cost = 30 },
        };

        public static Disease Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (Disease disease in All)
                if (disease.id == id) return disease;
            return null;
        }
    }
}
