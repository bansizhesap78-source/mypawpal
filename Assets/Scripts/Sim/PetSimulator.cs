using System;

namespace PatiDostum
{
    /// <summary>
    /// Zamanın hayvana etkisi (saf C#; testleri Editor/Tests/PetSimulatorTests). Oyun açıkken her saniye,
    /// uygulama kapalıyken açılışta (en fazla 72 saat) aynı kurallarla işletilir.
    /// Uzun süreler 5 dakikalık adımlara bölünür ki gece/gündüz ve uyku doğru hesaplansın.
    /// </summary>
    public static class PetSimulator
    {
        public const double StepSeconds = 300.0;

        /// <summary>Bu değerin altındaki ölçüm "kırmızı"dır: sağlık düşmeye başlar.</summary>
        public const double RedLine = 20.0;
        public const double HealthLossPerRedNeed = 2.0;   // puan / saat, her kırmızı ihtiyaç için
        public const double HealthRegen = 1.0;            // puan / saat, bakım iyiyken
        public const double AccidentHygieneLoss = 25.0;
        public const double AccidentFunLoss = 10.0;
        public const double PoopHygieneLoss = 4.0;        // puan / saat, kaka yerde durdukça
        public const double StarveWeightLoss = 0.5;       // puan / saat, açken
        public const double SulkLoveLoss = 1.0;           // puan / saat, mutsuzken

        /// <summary>Gece bu enerjinin altındaysa kendiliğinden uyur; gündüz SleepyFaint'in altında uyuyakalır.</summary>
        public const double NightSleepEnergy = 70.0;
        public const double SleepyFaint = 5.0;
        /// <summary>Gündüz enerjisi tamamen dolunca kendiliğinden uyanır.</summary>
        public const double WakeEnergy = 100.0;
        /// <summary>Çocuk gece uyandırınca dost bu kadar süre uyanık kalır (sonra yorgunsa yine uyur).</summary>
        public const double WokenAwakeSeconds = 2 * 3600.0;

        // Sağlık (M2, plan §6)
        /// <summary>Sağlık bunun altındaysa "kritik": yalnızca Mucize Tedavi ya da iyi bakım kurtarır.</summary>
        public const double CriticalLine = 20.0;
        /// <summary>Kritik durumda bu kadar gerçek süre geçerse (moddan bağımsız) ölür; "Hayvanlar ölmesin" açıksa ölmez.</summary>
        public const double DeathAfterCriticalHours = 48.0;
        public const double ColdRiskPerHour = 0.04;       // kirli ya da bitkinken
        public const double ColdRiskWeakPerHour = 0.01;   // sağlık 60'ın altındayken
        public const double VaccineRiskFactor = 0.25;
        public const double FleaRiskPerHour = 0.05;       // çok kirliyken
        public const double ObesityWeight = 80.0;
        public const double CandyDecayPerHour = 1.0 / 6.0;

        /// <summary><paramref name="realSeconds"/> kadar gerçek süreyi, <paramref name="startLocal"/> yerel saatinden başlayarak işletir.</summary>
        public static void Simulate(PetState pet, DateTime startLocal, double realSeconds, bool noDeath = false)
        {
            Species species = SpeciesCatalog.Get(pet.species);
            double done = 0.0;
            while (done < realSeconds - 1e-9 && !pet.dead)
            {
                double step = Math.Min(StepSeconds, realSeconds - done);
                Step(pet, species, startLocal.AddSeconds(done).Hour, step, noDeath);
                done += step;
            }
        }

        /// <summary>Pansiyondayken: dost yalnızca yaşlanır, ihtiyaçları donar (ömrü dolarsa yine huzurla uyur).</summary>
        public static void AgeOnly(PetState pet, double realSeconds)
        {
            if (pet.dead || realSeconds <= 0.0) return;
            pet.ageSeconds += GameClock.GameSeconds(realSeconds, pet.Mode);
            if (pet.lifespanYears > 0.0 && Mood.AgeYears(pet) >= pet.lifespanYears) Die(pet, "natural");
        }

        public static bool IsCritical(PetState pet) => pet.health < CriticalLine;

        /// <summary>Ölüme kalan gerçek süre (sa); kritik değilse ya da ölüm kapalıysa sonsuz.</summary>
        public static double HoursLeft(PetState pet)
        {
            if (!IsCritical(pet)) return double.PositiveInfinity;
            return Math.Max(0.0, DeathAfterCriticalHours - pet.criticalSeconds / 3600.0);
        }

        public static void Step(PetState pet, Species species, int localHour, double realSeconds, bool noDeath = false)
        {
            if (pet.dead) return;
            double hours = realSeconds / 3600.0;
            bool night = GameClock.IsNight(localHour);
            pet.ageSeconds += GameClock.GameSeconds(realSeconds, pet.Mode);

            // Ömrünü tamamladı: huzurla uyur ("Hayvanlar ölmesin" yalnızca ihmali kapsar)
            if (pet.lifespanYears > 0.0 && Mood.AgeYears(pet) >= pet.lifespanYears)
            {
                Die(pet, "natural");
                return;
            }

            // Uyku: gece yorgunsa kendiliğinden uyur (çocuk az önce uyandırdıysa uyumaz), bitkin düşerse
            // uyuyakalır, gündüz dinlenince uyanır
            bool wokenByChild = pet.stayAwakeSeconds > 0.0;
            if (wokenByChild) pet.stayAwakeSeconds = Math.Max(0.0, pet.stayAwakeSeconds - realSeconds);
            if (!pet.asleep && ((night && !wokenByChild && pet.energy < NightSleepEnergy) || pet.energy <= SleepyFaint))
                pet.asleep = true;
            else if (pet.asleep && !night && pet.energy >= WakeEnergy)
                pet.asleep = false;

            // Gece ya da uyurken ihtiyaçlar yavaş düşer
            double factor = pet.asleep ? GameClock.NightNeedsFactor : GameClock.NeedsFactor(localHour);
            NeedRates r = species.rates;
            pet.fullness -= r.fullness * hours * factor;
            pet.thirst -= r.thirst * hours * factor;
            pet.hygiene -= r.hygiene * hours * factor;
            pet.toilet -= r.toilet * hours * factor;
            pet.fun -= r.fun * hours * factor;
            pet.energy += (pet.asleep ? r.energyAsleep : -r.energyAwake) * hours;
            if (pet.poopOnFloor) pet.hygiene -= PoopHygieneLoss * hours;

            // Tuvalete götürülmezse kaza olur
            if (pet.toilet <= 0.0)
            {
                pet.toilet = 100.0;
                pet.poopOnFloor = true;
                pet.hygiene -= AccidentHygieneLoss;
                pet.fun -= AccidentFunLoss;
            }

            pet.candy = Math.Max(0.0, pet.candy - CandyDecayPerHour * hours);
            if (!pet.IsSick) CatchDisease(pet, hours);

            Disease disease = DiseaseCatalog.Get(pet.disease);
            if (disease != null)
            {
                pet.health -= disease.healthPerHour * hours;
                pet.fun -= disease.funPerHour * hours;
                pet.energy -= disease.energyPerHour * hours;
                pet.hygiene -= disease.hygienePerHour * hours;
            }

            int red = (pet.fullness < RedLine ? 1 : 0) + (pet.thirst < RedLine ? 1 : 0) + (pet.hygiene < RedLine ? 1 : 0);
            if (red > 0) pet.health -= HealthLossPerRedNeed * red * hours;
            else if (disease == null && pet.fullness > 50.0 && pet.thirst > 50.0 && pet.hygiene > 50.0) pet.health += HealthRegen * hours;

            if (pet.fullness < RedLine) pet.weight -= StarveWeightLoss * hours;
            if (Mood.Happiness(pet) < 30.0) pet.love -= SulkLoveLoss * hours;

            Clamp(pet);

            // Kritik sayaç: sağlık düzelince sıfırlanır; 48 saat dolarsa ölür
            if (IsCritical(pet)) pet.criticalSeconds += realSeconds;
            else pet.criticalSeconds = 0.0;
            if (!noDeath && pet.criticalSeconds >= DeathAfterCriticalHours * 3600.0) Die(pet, "neglect");
        }

        private static void CatchDisease(PetState pet, double hours)
        {
            if (pet.weight >= ObesityWeight)
            {
                pet.disease = DiseaseCatalog.Obesity;
                return;
            }

            bool vaccinated = pet.ageSeconds < pet.vaccineUntilAge;
            double coldRisk = pet.hygiene < 30.0 || pet.energy < 15.0 ? ColdRiskPerHour
                : pet.health < 60.0 ? ColdRiskWeakPerHour : 0.0;
            if (vaccinated) coldRisk *= VaccineRiskFactor;
            double fleaRisk = pet.hygiene < 25.0 ? FleaRiskPerHour : 0.0;

            if (coldRisk > 0.0 && pet.NextRandom() < coldRisk * hours) pet.disease = DiseaseCatalog.Cold;
            else if (fleaRisk > 0.0 && pet.NextRandom() < fleaRisk * hours) pet.disease = DiseaseCatalog.Fleas;
        }

        private static void Die(PetState pet, string cause)
        {
            pet.dead = true;
            pet.deathCause = cause;
            pet.asleep = true;
        }

        public static void Clamp(PetState pet)
        {
            pet.fullness = Clamp01(pet.fullness);
            pet.thirst = Clamp01(pet.thirst);
            pet.energy = Clamp01(pet.energy);
            pet.hygiene = Clamp01(pet.hygiene);
            pet.toilet = Clamp01(pet.toilet);
            pet.fun = Clamp01(pet.fun);
            pet.health = Clamp01(pet.health);
            pet.love = Clamp01(pet.love);
            pet.weight = Clamp01(pet.weight);
        }

        private static double Clamp01(double value) => value < 0.0 ? 0.0 : value > 100.0 ? 100.0 : value;
    }
}
