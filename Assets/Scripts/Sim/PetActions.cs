namespace PatiDostum
{
    public enum ActionResult
    {
        Done,
        /// <summary>Uyuyor: önce uyandırılmalı.</summary>
        Asleep,
        /// <summary>Gerek yok (tuvaleti yok, kaka yok, zaten uyuyor...).</summary>
        NotNeeded,
        /// <summary>Tok karnına yedi: kilo aldı.</summary>
        Overfed,
        /// <summary>Oynayacak enerjisi yok.</summary>
        Tired
    }

    /// <summary>Odalardaki bakım aksiyonları (saf C#; testleri Editor/Tests/PetActionsTests).</summary>
    public static class PetActions
    {
        public const double OverfedLine = 95.0;
        public const double OverfedWeight = 3.0;
        public const double PlayMinEnergy = 15.0;
        public const double WalkMinEnergy = 20.0;
        public const double SleepMaxEnergy = 95.0;
        public const double StomachChance = 0.3;
        public const double ToothacheCandy = 4.0;
        public const double ToothacheChance = 0.5;

        /// <summary>Dışarıda gezinti: çok eğlendirir, sevgi ve zayıflama; enerji, su ve temizlik gider.</summary>
        public static ActionResult Walk(PetState p)
        {
            if (p.asleep) return ActionResult.Asleep;
            if (p.energy < WalkMinEnergy) return ActionResult.Tired;
            p.fun += 25.0;
            p.love += 3.0;
            p.weight -= 1.5;
            p.energy -= 12.0;
            p.thirst -= 8.0;
            p.hygiene -= 8.0;
            PetSimulator.Clamp(p);
            return ActionResult.Done;
        }

        public static ActionResult Feed(PetState p, Food food)
        {
            if (p.asleep) return ActionResult.Asleep;
            bool overfed = p.fullness >= OverfedLine;
            p.fullness += food.fullness;
            p.fun += food.fun;
            p.weight += food.weight + (overfed ? OverfedWeight : 0.0);
            p.love += food.love;
            if (SpeciesCatalog.Get(p.species).hasToilet) p.toilet -= food.toilet;

            // Tok karnına yemek mideyi, çok şeker dişi bozabilir (plan §6)
            if (!p.IsSick && overfed && p.NextRandom() < StomachChance) p.disease = DiseaseCatalog.Stomach;
            if (food.sugar > 0.0)
            {
                p.candy += food.sugar;
                if (!p.IsSick && p.candy > ToothacheCandy && p.NextRandom() < ToothacheChance) p.disease = DiseaseCatalog.Toothache;
            }
            return Finish(p, overfed ? ActionResult.Overfed : ActionResult.Done);
        }

        public static ActionResult Drink(PetState p)
        {
            if (p.asleep) return ActionResult.Asleep;
            p.thirst += 40.0;
            p.toilet -= 6.0;
            return Finish(p, ActionResult.Done);
        }

        public static ActionResult Bathe(PetState p)
        {
            if (p.asleep) return ActionResult.Asleep;
            p.hygiene = 100.0;
            p.fun -= 5.0;   // kediler banyoyu pek sevmez
            p.energy -= 5.0;
            p.love += 1.0;
            return Finish(p, ActionResult.Done);
        }

        public static ActionResult Brush(PetState p)
        {
            if (p.asleep) return ActionResult.Asleep;
            p.hygiene += 15.0;
            p.love += 3.0;
            p.fun += 3.0;
            return Finish(p, ActionResult.Done);
        }

        public static ActionResult UseToilet(PetState p)
        {
            if (p.asleep) return ActionResult.Asleep;
            if (p.toilet >= 90.0) return ActionResult.NotNeeded;
            p.toilet = 100.0;
            p.hygiene -= 2.0;
            p.love += 1.0;
            return Finish(p, ActionResult.Done);
        }

        public static ActionResult CleanPoop(PetState p)
        {
            if (!p.poopOnFloor) return ActionResult.NotNeeded;
            p.poopOnFloor = false;
            return Finish(p, ActionResult.Done);
        }

        /// <summary>Enerjisi doluyken uyumaz (gündüz hemen uyanırdı).</summary>
        public static ActionResult Sleep(PetState p)
        {
            if (p.asleep || p.energy >= SleepMaxEnergy) return ActionResult.NotNeeded;
            p.asleep = true;
            p.stayAwakeSeconds = 0.0;
            return ActionResult.Done;
        }

        public static ActionResult Wake(PetState p)
        {
            if (!p.asleep) return ActionResult.NotNeeded;
            p.asleep = false;
            // Çocuk uyandırdı: bir süre gece de uyanık kalır, oyun engellenmez
            p.stayAwakeSeconds = PetSimulator.WokenAwakeSeconds;
            return Finish(p, ActionResult.Done);
        }

        /// <summary>Sevmek (hayvana dokununca).</summary>
        public static ActionResult Stroke(PetState p)
        {
            if (p.asleep) return ActionResult.Asleep;
            p.love += 3.0;
            p.fun += 4.0;
            return Finish(p, ActionResult.Done);
        }

        public static ActionResult Play(PetState p)
        {
            if (p.asleep) return ActionResult.Asleep;
            if (p.energy < PlayMinEnergy) return ActionResult.Tired;
            p.fun += 20.0;
            p.energy -= 10.0;
            p.thirst -= 5.0;
            p.hygiene -= 3.0;
            p.weight -= 1.0;
            p.love += 2.0;
            return Finish(p, ActionResult.Done);
        }

        private static ActionResult Finish(PetState p, ActionResult result)
        {
            PetSimulator.Clamp(p);
            return result;
        }
    }
}
