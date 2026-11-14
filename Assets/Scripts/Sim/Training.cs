using System;

namespace PatiDostum
{
    /// <summary>Bir komutun öğrenilme ilerlemesi (0–100; 100'de öğrenildi).</summary>
    [Serializable]
    public class CommandProgress
    {
        public string id;
        public double progress;
    }

    /// <summary>
    /// Okul (saf C#; testleri Editor/Tests/SchoolTests). Plan §5:
    ///  - Komut eğitimi: sıra hafızası mini oyunu oynanır; başarı ilerlemeyi bağa (sevgi) göre artırır,
    ///    yavruyken daha hızlı öğrenir. 100'e ulaşan komut öğrenilmiştir ve "göster" ile yapılır.
    ///  - Kitap okumak zihinsel durumu (eğlence, sevgi) yükseltir; bebek ve yavruyken daha etkilidir.
    /// </summary>
    public static class Training
    {
        public const double MinEnergy = 10.0;
        public const double BaseGain = 20.0;
        public const double FailGain = 5.0;
        public const double YoungBonus = 1.2;

        public static double Progress(PetState p, string command)
        {
            CommandProgress entry = Find(p, command);
            return entry != null ? entry.progress : 0.0;
        }

        public static bool Learned(PetState p, string command) => Progress(p, command) >= 100.0;

        public static int LearnedCount(PetState p)
        {
            int count = 0;
            foreach (CommandProgress c in p.commands)
                if (c.progress >= 100.0) count++;
            return count;
        }

        /// <summary>Başarılı derste kazanılan ilerleme: sevgi 0'da 12, 100'de 28; yavruyken %20 fazla.</summary>
        public static double Gain(PetState p, Species s)
        {
            double gain = BaseGain * (0.6 + 0.8 * p.love / 100.0);
            LifeStage stage = Mood.Stage(p, s);
            if (stage == LifeStage.Baby || stage == LifeStage.Young) gain *= YoungBonus;
            return gain;
        }

        /// <summary>Bir ders: sıra hafızası oyunu başarılıysa <paramref name="success"/>.</summary>
        public static ActionResult Practice(PetState p, Species s, string command, bool success)
        {
            if (p.asleep) return ActionResult.Asleep;
            if (p.energy < MinEnergy) return ActionResult.Tired;
            CommandProgress entry = Find(p, command);
            if (entry == null)
            {
                entry = new CommandProgress { id = command };
                p.commands.Add(entry);
            }
            entry.progress = Math.Min(100.0, entry.progress + (success ? Gain(p, s) : FailGain));
            p.energy -= 5.0;
            p.fun += success ? 3.0 : 1.0;
            p.love += success ? 2.0 : 1.0;
            PetSimulator.Clamp(p);
            return ActionResult.Done;
        }

        /// <summary>Öğrenilmiş komutu gösterir (numara yapar).</summary>
        public static ActionResult Perform(PetState p, string command)
        {
            if (p.asleep) return ActionResult.Asleep;
            if (!Learned(p, command)) return ActionResult.NotNeeded;
            if (p.energy < MinEnergy) return ActionResult.Tired;
            p.fun += 6.0;
            p.love += 2.0;
            p.energy -= 3.0;
            PetSimulator.Clamp(p);
            return ActionResult.Done;
        }

        public static ActionResult Read(PetState p, Species s)
        {
            if (p.asleep) return ActionResult.Asleep;
            LifeStage stage = Mood.Stage(p, s);
            double bonus = stage == LifeStage.Baby || stage == LifeStage.Young ? 1.5 : 1.0;
            p.fun += 6.0 * bonus;
            p.love += 3.0 * bonus;
            p.energy -= 2.0;
            PetSimulator.Clamp(p);
            return ActionResult.Done;
        }

        private static CommandProgress Find(PetState p, string command)
        {
            if (p.commands == null) p.commands = new System.Collections.Generic.List<CommandProgress>();
            foreach (CommandProgress c in p.commands)
                if (c.id == command) return c;
            return null;
        }
    }
}
