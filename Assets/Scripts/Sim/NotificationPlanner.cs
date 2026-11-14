using System;
using System.Collections.Generic;

namespace PatiDostum
{
    /// <summary>Planlanmış yerel bildirim: zamanı, metin anahtarları ve hangi dost için olduğu.</summary>
    public struct PlannedNotification
    {
        public DateTime at;
        public string bodyKey;
        public string petName;
        /// <summary>Kritik uyarılar önceliklidir ve günlük sınırda önce bunlar kalır.</summary>
        public bool critical;
    }

    /// <summary>
    /// Uygulama kapanırken hangi bildirimlerin ne zaman gideceğini hesaplar (saf C#; testleri
    /// Editor/Tests/NotificationPlannerTests). Plan §9:
    ///  - Dostun bir kopyası 72 saat ileri işletilir; ilk acıkma, tuvalet, hastalık ve kritik an bulunur.
    ///  - Kritik dosta ölümden önce 3 uyarı gider (36, 12 ve 2 saat kala).
    ///  - Sessiz saatlerde (22:00–08:00) bildirim yok: sabah 08:00'e kayar; kritik uyarı akşam 21:30'a öne alınır.
    ///  - Günde en fazla 3 bildirim; kritikler önce.
    /// </summary>
    public static class NotificationPlanner
    {
        public const int QuietStartHour = 22;
        public const int QuietEndHour = 8;
        public const int MaxPerDay = 3;
        public const double HorizonHours = 72.0;
        public const double StepMinutes = 15.0;
        public static readonly double[] CriticalWarningHours = { 36.0, 12.0, 2.0 };

        public static List<PlannedNotification> Plan(IList<PetState> pets, DateTime nowLocal, bool noDeath)
        {
            var all = new List<PlannedNotification>();
            foreach (PetState pet in pets)
                if (!pet.dead) PlanPet(pet, nowLocal, noDeath, all);

            for (int i = 0; i < all.Count; i++)
            {
                PlannedNotification n = all[i];
                n.at = n.critical ? CriticalOutOfQuiet(n.at, nowLocal) : OutOfQuiet(n.at);
                all[i] = n;
            }

            // Günde en fazla 3: önce kritikler, sonra en erken olanlar
            all.Sort((a, b) => a.critical != b.critical ? (a.critical ? -1 : 1) : a.at.CompareTo(b.at));
            var perDay = new Dictionary<DateTime, int>();
            var result = new List<PlannedNotification>();
            foreach (PlannedNotification n in all)
            {
                perDay.TryGetValue(n.at.Date, out int count);
                if (count >= MaxPerDay) continue;
                perDay[n.at.Date] = count + 1;
                result.Add(n);
            }
            result.Sort((a, b) => a.at.CompareTo(b.at));
            return result;
        }

        private static void PlanPet(PetState pet, DateTime now, bool noDeath, List<PlannedNotification> list)
        {
            if (PetSimulator.IsCritical(pet))
            {
                if (noDeath)
                {
                    Add(list, pet, now.AddHours(3), "notify_critical", true);
                    return;
                }
                DateTime death = now.AddHours(PetSimulator.HoursLeft(pet));
                foreach (double hours in CriticalWarningHours)
                {
                    DateTime at = death.AddHours(-hours);
                    if (at > now.AddMinutes(30)) Add(list, pet, at, "notify_critical", true);
                }
                if (death > now.AddMinutes(30) && !list.Exists(n => n.critical && n.petName == pet.name))
                    Add(list, pet, now.AddMinutes(30), "notify_critical", true);
                return;
            }

            // Kopyayı ileri işlet: her durumun ilk anını bul
            PetState future = pet.Clone();
            bool hungry = false, toilet = false, sick = pet.IsSick, critical = false;
            double step = StepMinutes * 60.0;
            for (double t = step; t <= HorizonHours * 3600.0 && !future.dead; t += step)
            {
                PetSimulator.Simulate(future, now.AddSeconds(t - step), step, noDeath);
                DateTime at = now.AddSeconds(t);
                if (!hungry && future.fullness < 30.0) { hungry = true; Add(list, pet, at, "notify_hungry", false); }
                if (!toilet && future.toilet < 25.0) { toilet = true; Add(list, pet, at, "notify_toilet", false); }
                if (!sick && future.IsSick) { sick = true; Add(list, pet, at, "notify_sick", false); }
                if (!critical && PetSimulator.IsCritical(future)) { critical = true; Add(list, pet, at, "notify_critical", true); }
            }
        }

        private static void Add(List<PlannedNotification> list, PetState pet, DateTime at, string key, bool critical)
        {
            list.Add(new PlannedNotification { at = at, bodyKey = key, petName = pet.name, critical = critical });
        }

        public static bool IsQuiet(DateTime t) => t.Hour >= QuietStartHour || t.Hour < QuietEndHour;

        /// <summary>Sessiz saatteki bildirimi sabah 08:00'e kaydırır.</summary>
        public static DateTime OutOfQuiet(DateTime t)
        {
            if (!IsQuiet(t)) return t;
            DateTime morning = t.Date.AddHours(QuietEndHour);
            return t.Hour >= QuietStartHour ? morning.AddDays(1) : morning;
        }

        /// <summary>Kritik uyarı sabahı beklemesin: mümkünse bir önceki akşam 21:30'a alınır.</summary>
        public static DateTime CriticalOutOfQuiet(DateTime t, DateTime now)
        {
            if (!IsQuiet(t)) return t;
            DateTime evening = (t.Hour >= QuietStartHour ? t.Date : t.Date.AddDays(-1)).AddHours(QuietStartHour).AddMinutes(-30);
            return evening > now ? evening : OutOfQuiet(t);
        }
    }
}
