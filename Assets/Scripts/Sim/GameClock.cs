using System;

namespace PatiDostum
{
    /// <summary>Zaman modu: gerçek dünyayla birebir ya da 5 kat hızlı. Hayvan sahiplenirken seçilir.</summary>
    public enum SpeedMode { Real = 1, Fast = 5 }

    /// <summary>Saat ölçümünün kaydedilen durumu.</summary>
    [Serializable]
    public class ClockState
    {
        /// <summary>Oyunun güvendiği şimdiki zaman (UTC tick); 0 ise henüz ölçülmedi.</summary>
        public long trustedUtcTicks;
        /// <summary>Son ölçümde telefonun açılışından beri geçen süre (ms).</summary>
        public long elapsedMs;
    }

    /// <summary>
    /// Oyunun saati (saf C#, Unity'ye bağlı değil; testleri Editor/Tests altında).
    /// Gerçek geçen süreyi kullanıcının değiştiremeyeceği açılış sayacıyla ölçer:
    ///  - Telefon kapanıp açılmadıysa duvar saati hiç kullanılmaz; saati ileri/geri almak bir şey değiştirmez.
    ///  - Telefon yeniden başlamışsa duvar saatine bakılır ama süre en az açılıştan beri geçen kadar sayılır
    ///    (saat geri alındıysa ilerleme durmaz ama geri de gitmez). İleri alınan saati 72 saat sınırı keser.
    /// Uptime'ı öncekinden uzun bir yeniden başlatma "aynı açılış" sanılır; o durumda süre eksik sayılır ki
    /// oyuncunun aleyhine değil.
    /// </summary>
    public static class GameClock
    {
        /// <summary>Uygulama kapalıyken en fazla bu kadar süre işletilir (pansiyon yoksa).</summary>
        public const double MaxCatchUpHours = 72.0;

        public const int NightStartHour = 22;
        public const int NightEndHour = 7;

        /// <summary>Gece (hayvan uyurken) ihtiyaçlar bu oranda düşer (%70 daha yavaş).</summary>
        public const double NightNeedsFactor = 0.3;

        /// <summary>
        /// Son ölçümden beri geçen gerçek süreyi (sn, sınırsız) döndürür ve durumu günceller.
        /// İlk ölçümde 0 döner.
        /// </summary>
        public static double Measure(ClockState state, DateTime nowUtc, long nowElapsedMs)
        {
            if (state.trustedUtcTicks == 0)
            {
                state.trustedUtcTicks = nowUtc.Ticks;
                state.elapsedMs = nowElapsedMs;
                return 0.0;
            }

            double seconds;
            if (nowElapsedMs >= state.elapsedMs)
            {
                // Aynı açılış: yalnızca açılış sayacı
                seconds = (nowElapsedMs - state.elapsedMs) / 1000.0;
            }
            else
            {
                // Telefon yeniden başlamış: en az yeni açılıştan beri geçen süre kadar zaman geçti
                double wall = (nowUtc.Ticks - state.trustedUtcTicks) / (double)TimeSpan.TicksPerSecond;
                seconds = Math.Max(wall, nowElapsedMs / 1000.0);
            }

            state.trustedUtcTicks += (long)(seconds * TimeSpan.TicksPerSecond);
            state.elapsedMs = nowElapsedMs;
            return seconds;
        }

        /// <summary>Uzakta geçen süreden işletilecek kısmı (72 saatle sınırlı).</summary>
        public static double CatchUpSeconds(double realSeconds)
        {
            return Math.Max(0.0, Math.Min(realSeconds, MaxCatchUpHours * 3600.0));
        }

        /// <summary>Gerçek süreyi hayvanın yaşadığı oyun süresine çevirir.</summary>
        public static double GameSeconds(double realSeconds, SpeedMode mode)
        {
            return realSeconds * (int)mode;
        }

        public static bool IsNight(int localHour)
        {
            return localHour >= NightStartHour || localHour < NightEndHour;
        }

        /// <summary>İhtiyaçların düşme hızı çarpanı: gündüz 1, gece 0,3. Yaşlanmayı etkilemez.</summary>
        public static double NeedsFactor(int localHour)
        {
            return IsNight(localHour) ? NightNeedsFactor : 1.0;
        }
    }
}
