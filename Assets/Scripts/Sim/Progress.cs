using System;
using System.Collections.Generic;

namespace PatiDostum
{
    /// <summary>Günlük görev tanımı: hangi aksiyon kaç kez.</summary>
    public sealed class DailyTask
    {
        public string id;
        public int target;

        public string NameKey => "task_" + id;
    }

    /// <summary>Rozet: koşulu ve ödülü.</summary>
    public sealed class Badge
    {
        public string id;
        public Func<SaveData, bool> earned;
        public int gold;
        public int diamonds;

        public string NameKey => "badge_" + id;
        public string DescKey => "badge_" + id + "_desc";
    }

    /// <summary>
    /// Bağlılık (saf C#; testleri Editor/Tests/ProgressTests). Plan §8:
    ///  - Bakım serisi: dosta bakılan her gün seri bir artar (bir gün atlanırsa 1'e döner); her gün 10 altın,
    ///    her 7 günde 1 mavi elmas.
    ///  - Günlük görevler: her gün 3 görev (güne göre seçilir); biten görev 15 altın ve 100 Pass puanı.
    ///  - Rozetler: koşul sağlanınca bir kez verilir.
    /// Oyundaki aksiyonlar Count ile sayılır (PetController, mini oyunlar).
    /// </summary>
    public static class Progress
    {
        public const int TasksPerDay = 3;
        public const int TaskGold = 15;
        public const int TaskXp = 100;
        public const int StreakGold = 10;
        public const int StreakXp = 50;

        public static readonly DailyTask[] TaskPool =
        {
            new DailyTask { id = "feed", target = 3 },
            new DailyTask { id = "drink", target = 2 },
            new DailyTask { id = "bathe", target = 1 },
            new DailyTask { id = "play", target = 2 },
            new DailyTask { id = "stroke", target = 5 },
            new DailyTask { id = "minigame", target = 2 },
            new DailyTask { id = "lesson", target = 1 },
            new DailyTask { id = "read", target = 1 },
            new DailyTask { id = "wear", target = 1 },
        };

        public static readonly Badge[] Badges =
        {
            new Badge { id = "first_pal", gold = 50, earned = s => s.adoptions >= 1 },
            new Badge { id = "three_pals", gold = 50, diamonds = 1, earned = s => s.pets.Count >= 3 },
            new Badge { id = "streak_7", gold = 50, diamonds = 1, earned = s => s.streakDays >= 7 },
            new Badge { id = "streak_30", gold = 100, diamonds = 3, earned = s => s.streakDays >= 30 },
            new Badge { id = "first_trick", gold = 50, earned = s => s.pets.Exists(p => Training.LearnedCount(p) >= 1) },
            new Badge { id = "smart_pal", gold = 80, diamonds = 1, earned = s => s.pets.Exists(p => Training.LearnedCount(p) >= 3) },
            new Badge { id = "gamer", gold = 50, earned = s => s.totalGames >= 20 },
            new Badge { id = "show_winner", gold = 80, earned = s => s.showWins >= 1 },
            new Badge { id = "star_winner", gold = 100, diamonds = 3, earned = s => s.starWins >= 1 },
            new Badge { id = "fashion", gold = 50, earned = s => s.ownedItems.Count >= 5 },
            new Badge { id = "healer", gold = 50, earned = s => s.totalTreatments >= 5 },
            new Badge { id = "lifelong", gold = 100, diamonds = 3, earned = s => s.memorial.Exists(m => m.cause == "natural") },
            new Badge { id = "family", gold = 80, diamonds = 1, earned = s => s.pets.Exists(p => p.generation >= 2) },
        };

        public static DailyTask Task(string id)
        {
            foreach (DailyTask task in TaskPool)
                if (task.id == id) return task;
            return null;
        }

        /// <summary>Yeni günde 3 görevi güne göre (herkese aynı, tekrar etmeden) seçer.</summary>
        public static void EnsureDay(SaveData save, DateTime nowLocal)
        {
            long day = Vet.DayNumber(nowLocal);
            if (save.taskDay == day && save.taskIds.Count == TasksPerDay) return;
            save.taskDay = day;
            save.taskIds.Clear();
            save.taskCounts.Clear();
            save.taskClaimed.Clear();
            var random = new Random((int)(day % int.MaxValue));
            var pool = new List<DailyTask>(TaskPool);
            for (int i = 0; i < TasksPerDay; i++)
            {
                int pick = random.Next(pool.Count);
                save.taskIds.Add(pool[pick].id);
                save.taskCounts.Add(0);
                save.taskClaimed.Add(false);
                pool.RemoveAt(pick);
            }
        }

        /// <summary>
        /// Bir bakım aksiyonu yapıldı: günün görevlerini ilerletir ve bakım serisini sayar.
        /// Serinin o günkü ödülü verildiyse true (ekranda "Seri: 5 gün! +10 altın").
        /// </summary>
        public static bool Count(SaveData save, string action, DateTime nowLocal)
        {
            EnsureDay(save, nowLocal);
            for (int i = 0; i < save.taskIds.Count; i++)
                if (save.taskIds[i] == action) save.taskCounts[i]++;
            if (action == "minigame") save.totalGames++;
            if (action == "lesson") save.totalLessons++;
            return RecordCare(save, nowLocal);
        }

        public static bool RecordCare(SaveData save, DateTime nowLocal)
        {
            long day = Vet.DayNumber(nowLocal);
            if (save.lastCareDay == day) return false;
            save.streakDays = save.lastCareDay == day - 1 ? save.streakDays + 1 : 1;
            save.lastCareDay = day;
            save.gold += StreakGold;
            if (save.streakDays % 7 == 0) save.diamonds++;
            PatiPass.AddXp(save, StreakXp, nowLocal);
            return true;
        }

        public static bool TaskDone(SaveData save, int index)
        {
            DailyTask task = Task(save.taskIds[index]);
            return task != null && save.taskCounts[index] >= task.target;
        }

        public static bool Claim(SaveData save, int index, DateTime nowLocal)
        {
            EnsureDay(save, nowLocal);
            if (index >= save.taskIds.Count || save.taskClaimed[index] || !TaskDone(save, index)) return false;
            save.taskClaimed[index] = true;
            save.gold += TaskGold;
            PatiPass.AddXp(save, TaskXp, nowLocal);
            return true;
        }

        /// <summary>Yeni kazanılan rozetleri verir (ödülleriyle) ve döndürür.</summary>
        public static List<Badge> CheckBadges(SaveData save)
        {
            var earned = new List<Badge>();
            foreach (Badge badge in Badges)
            {
                if (save.badges.Contains(badge.id) || !badge.earned(save)) continue;
                save.badges.Add(badge.id);
                save.gold += badge.gold;
                save.diamonds += badge.diamonds;
                earned.Add(badge);
            }
            return earned;
        }
    }
}
