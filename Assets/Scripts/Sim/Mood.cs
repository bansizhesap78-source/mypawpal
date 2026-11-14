namespace PatiDostum
{
    public enum MoodKind { Critical, Sleeping, Sick, NeedsToilet, Hungry, Thirsty, Sleepy, Dirty, Bored, Sulky, Funny, Happy, Okay }

    public enum LifeStage { Baby, Young, Teen, Adult, Senior }

    /// <summary>Ölçümlerden türetilenler: mutluluk, ruh hali etiketi, kilo durumu, büyüme aşaması.</summary>
    public static class Mood
    {
        /// <summary>Zihinsel durum (0–100): ihtiyaçların ağırlıklı ortalaması, sevgi bonusu, yerdeki kaka cezası.</summary>
        public static double Happiness(PetState p)
        {
            double h = p.fullness * 0.2 + p.thirst * 0.15 + p.energy * 0.1 + p.hygiene * 0.1
                       + p.toilet * 0.1 + p.fun * 0.2 + p.health * 0.15;
            h += (p.love - 50.0) * 0.1;
            if (p.poopOnFloor) h -= 15.0;
            return h < 0.0 ? 0.0 : h > 100.0 ? 100.0 : h;
        }

        /// <summary>Balonda gösterilen ruh hali: en acil ihtiyaç önce gelir.</summary>
        public static MoodKind Of(PetState p)
        {
            if (PetSimulator.IsCritical(p)) return MoodKind.Critical;
            if (p.asleep) return MoodKind.Sleeping;
            if (p.IsSick || p.health < 30.0) return MoodKind.Sick;
            if (p.toilet < 25.0) return MoodKind.NeedsToilet;
            if (p.fullness < 30.0) return MoodKind.Hungry;
            if (p.thirst < 30.0) return MoodKind.Thirsty;
            if (p.energy < 25.0) return MoodKind.Sleepy;
            if (p.hygiene < 30.0 || p.poopOnFloor) return MoodKind.Dirty;
            if (p.fun < 30.0) return MoodKind.Bored;
            if (p.love < 15.0) return MoodKind.Sulky;
            double happiness = Happiness(p);
            if (happiness >= 80.0 && p.fun >= 85.0) return MoodKind.Funny;
            if (happiness >= 60.0) return MoodKind.Happy;
            return MoodKind.Okay;
        }

        public static string Key(MoodKind mood) => "mood_" + mood.ToString().ToLowerInvariant();

        /// <summary>Balon yazısı: hastaysa hastalığın belirtisi ("Hapşu!"), değilse ruh hali.</summary>
        public static string BubbleKey(PetState p)
        {
            MoodKind mood = Of(p);
            Disease disease = DiseaseCatalog.Get(p.disease);
            return mood == MoodKind.Sick && disease != null ? disease.SymptomKey : Key(mood);
        }

        public static string WeightKey(PetState p)
        {
            if (p.weight < 35.0) return "weight_thin";
            if (p.weight > 65.0) return "weight_heavy";
            return "weight_ideal";
        }

        public static double AgeYears(PetState p) => p.ageSeconds / (365.25 * 86400.0);

        public static LifeStage Stage(PetState p, Species s)
        {
            double years = AgeYears(p);
            double lifespan = p.lifespanYears > 0.0 ? p.lifespanYears : s.lifespanYears;
            if (years >= lifespan * 0.8) return LifeStage.Senior;
            if (years >= s.adultYears) return LifeStage.Adult;
            double grown = years / s.adultYears;
            if (grown < 0.25) return LifeStage.Baby;
            if (grown < 0.6) return LifeStage.Young;
            return LifeStage.Teen;
        }

        /// <summary>Ekrandaki boyut: yetişkinliğe kadar 0,45'ten 1'e büyür.</summary>
        public static float Scale(PetState p, Species s)
        {
            double grown = AgeYears(p) / s.adultYears;
            if (grown > 1.0) grown = 1.0;
            return (float)(0.45 + 0.55 * grown);
        }
    }
}
