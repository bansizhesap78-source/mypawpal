using System;
using System.Globalization;

namespace PatiDostum
{
    public sealed class ShowResult
    {
        public string[] names;
        public int[] scores;
        /// <summary>Dostun sırası (1 = birinci).</summary>
        public int place;
        public int gold;
        public int diamonds;
    }

    /// <summary>
    /// Pet Şov (saf C#; testleri ProgressTests). Haftada bir yerel yarışma: 4 yapay rakip (puanları haftaya göre
    /// sabit ve katılmadan ÖNCE görünür, şans yok) ve dost. Dostun puanı bakımından gelir: mutluluk, sevgi, sağlık,
    /// öğrendiği komutlar ve kıyafetleri.
    ///  - Pet Şov: bedava, haftada bir.
    ///  - Yıldızlar Şovu: rakipler daha güçlü, ödüller büyük; giriş 3 mavi elmas (kulüp üyelerine bedava).
    ///    Rakip puanları önceden görüldüğü için sonuç bellidir: parayla şans satılmaz.
    /// </summary>
    public static class PetShow
    {
        public const int StarEntryDiamonds = 3;
        public static readonly string[] Rivals = { "Boncuk", "Zeytin", "Fıstık", "Duman" };

        private static readonly int[] Gold = { 100, 60, 30, 15, 10 };
        private static readonly int[] Diamonds = { 1, 0, 0, 0, 0 };
        private static readonly int[] StarGold = { 300, 150, 80, 40, 30 };
        private static readonly int[] StarDiamonds = { 5, 2, 1, 0, 0 };

        public static int Week(DateTime local)
        {
            int week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(local, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return local.Year * 100 + week;
        }

        /// <summary>Dostun şov puanı (0–109).</summary>
        public static int Score(PetState p)
        {
            int outfit = 0;
            foreach (string worn in new[] { p.hat, p.glasses, p.neck, p.outfit, p.feet })
                if (!string.IsNullOrEmpty(worn)) outfit++;
            double score = Mood.Happiness(p) * 0.3 + p.love * 0.25 + p.health * 0.15
                           + Math.Min(Training.LearnedCount(p), 3) * 8 + Math.Min(outfit, 5) * 3;
            return (int)Math.Round(score);
        }

        /// <summary>Bu haftanın rakip puanları (herkes için aynı; katılmadan görünür).</summary>
        public static int[] RivalScores(DateTime nowLocal, bool star)
        {
            var random = new Random(Week(nowLocal) * 7 + (star ? 1 : 0));
            var scores = new int[Rivals.Length];
            for (int i = 0; i < scores.Length; i++) scores[i] = star ? random.Next(60, 96) : random.Next(40, 81);
            return scores;
        }

        public static bool CanEnter(SaveData save, bool star, DateTime nowLocal)
        {
            int week = Week(nowLocal);
            if ((star ? save.lastStarShowWeek : save.lastShowWeek) == week) return false;
            return !star || Club.Active(save, nowLocal.ToUniversalTime()) || save.diamonds >= StarEntryDiamonds;
        }

        public static ShowResult Enter(SaveData save, PetState pet, bool star, DateTime nowLocal)
        {
            if (!CanEnter(save, star, nowLocal)) return null;
            if (star && !Club.Active(save, nowLocal.ToUniversalTime())) save.diamonds -= StarEntryDiamonds;
            int week = Week(nowLocal);
            if (star) save.lastStarShowWeek = week;
            else save.lastShowWeek = week;

            int[] rivals = RivalScores(nowLocal, star);
            int mine = Score(pet);
            int place = 1;
            foreach (int rival in rivals)
                if (rival > mine) place++;

            var result = new ShowResult
            {
                names = Rivals,
                scores = rivals,
                place = place,
                gold = (star ? StarGold : Gold)[place - 1],
                diamonds = (star ? StarDiamonds : Diamonds)[place - 1]
            };
            save.gold += result.gold;
            save.diamonds += result.diamonds;
            if (place == 1)
            {
                if (star) save.starWins++;
                else save.showWins++;
            }
            if (place <= 3) save.trophies++;
            PatiPass.AddXp(save, 150, nowLocal);
            return result;
        }
    }

    public enum BreedResult { Done, NotAdult, NotLoved, Sick, DifferentSpecies, NoSlot, TooSoon, NoMoney, Same }

    /// <summary>
    /// Yavru ve soy ağacı (saf C#; testleri ProgressTests). Aynı türden iki yetişkin, sağlıklı ve sevgisi 70'in
    /// üstünde dostun yavrusu olabilir; yavru boş bir slot ister. Bedeli 5 mavi elmas ya da 300 altın.
    /// Yavru anne-babasından birinin rengini alır; kuşağı bir artar. Aynı dost 14 oyun günü bekler.
    /// </summary>
    public static class Breeding
    {
        public const double MinLove = 70.0;
        public const double CooldownGameDays = 14.0;
        public const int DiamondCost = 5;
        public const int GoldCost = 300;

        public static BreedResult Check(SaveData save, PetState a, PetState b)
        {
            if (a == b) return BreedResult.Same;
            if (a.species != b.species) return BreedResult.DifferentSpecies;
            Species species = SpeciesCatalog.Get(a.species);
            if (!Adult(a, species) || !Adult(b, species)) return BreedResult.NotAdult;
            if (a.love < MinLove || b.love < MinLove) return BreedResult.NotLoved;
            if (a.IsSick || b.IsSick || PetSimulator.IsCritical(a) || PetSimulator.IsCritical(b)) return BreedResult.Sick;
            if (!Shop.CanAdopt(save)) return BreedResult.NoSlot;
            if (TooSoon(a) || TooSoon(b)) return BreedResult.TooSoon;
            return BreedResult.Done;
        }

        private static bool Adult(PetState p, Species s)
        {
            LifeStage stage = Mood.Stage(p, s);
            return stage == LifeStage.Adult || stage == LifeStage.Senior;
        }

        private static bool TooSoon(PetState p) => p.bredAtAge > 0.0 && p.ageSeconds - p.bredAtAge < CooldownGameDays * 86400.0;

        public static PetState Breed(SaveData save, PetState a, PetState b, string name, bool useDiamonds, DateTime nowUtc, out BreedResult result)
        {
            result = Check(save, a, b);
            if (result != BreedResult.Done) return null;
            if (useDiamonds ? save.diamonds < DiamondCost : save.gold < GoldCost)
            {
                result = BreedResult.NoMoney;
                return null;
            }
            if (useDiamonds) save.diamonds -= DiamondCost;
            else save.gold -= GoldCost;

            Species species = SpeciesCatalog.Get(a.species);
            int color = a.NextRandom() < 0.5 ? a.color : b.color;
            PetState baby = PetState.Adopt(species, name, color, a.Mode, nowUtc);
            baby.ageSeconds = 86400.0;   // yeni doğmuş
            baby.parents = a.name + " & " + b.name;
            baby.generation = Math.Max(a.generation, b.generation) + 1;
            baby.love = 60.0;
            a.bredAtAge = a.ageSeconds;
            b.bredAtAge = b.ageSeconds;
            save.pets.Add(baby);
            save.adoptions++;
            return baby;
        }
    }
}
