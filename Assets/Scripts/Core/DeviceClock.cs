using System.Diagnostics;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace PatiDostum
{
    /// <summary>
    /// Telefonun açılışından beri geçen süre (ms). Kullanıcı bu saati değiştiremez, bu yüzden aynı açılış
    /// içinde geçen süre duvar saatinden değil buradan ölçülür (bkz. GameClock.Measure).
    /// Android'de uyku dahil sayan SystemClock.elapsedRealtime, iOS'ta mach_continuous_time kullanılır; editörde ve masaüstünde
    /// Stopwatch (Windows'ta o da açılıştan beri sayar).
    /// </summary>
    public static class DeviceClock
    {
#if UNITY_IOS && !UNITY_EDITOR
        // Assets/Plugins/iOS/PatiClock.c: mach_continuous_time (uyku dahil, kullanıcı değiştiremez)
        [DllImport("__Internal")]
        private static extern long PatiClock_ElapsedMs();
#endif

        public static long ElapsedMs()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var clock = new UnityEngine.AndroidJavaClass("android.os.SystemClock"))
                    return clock.CallStatic<long>("elapsedRealtime");
            }
            catch (System.Exception error)
            {
                UnityEngine.Debug.LogWarning("elapsedRealtime okunamadı: " + error.Message);
            }
#endif
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                return PatiClock_ElapsedMs();
            }
            catch (System.Exception error)
            {
                UnityEngine.Debug.LogWarning("Açılıştan beri geçen süre okunamadı: " + error.Message);
            }
#endif
            return (long)(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency * 1000.0);
        }
    }
}
