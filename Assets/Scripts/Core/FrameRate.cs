using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatiDostum
{
    /// <summary>
    /// Kare hızı: Unity Android'de varsayılan olarak saniyede 30 kare çizer; animasyonlar takılıyormuş gibi görünür.
    /// Oyun açılırken ve her sahne yüklenince 60'a çıkarılır (ekran 60'tan azsa ekranın hızı). Dikey eşitleme kapalı,
    /// hızı bu belirler.
    /// </summary>
    public static class FrameRate
    {
        public const int Target = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Apply();
            SceneManager.sceneLoaded += (scene, mode) => Apply();
            // Test sürümündeki Unity konsolu (internet izni olmayınca hata basar) alt sekmeleri örtmesin
            Debug.developerConsoleEnabled = false;
        }

        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            double screen = Screen.currentResolution.refreshRateRatio.value;
            int target = screen >= 59.0 || screen <= 0.0 ? Target : Mathf.RoundToInt((float)screen);
            Application.targetFrameRate = target;
            Debug.Log($"[Pati Dostum] Kare hızı hedefi {target} (ekran {screen:0.#} Hz, kalite {QualitySettings.names[QualitySettings.GetQualityLevel()]})");
        }
    }
}
