using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// "Gizlilik Politikası" ve "Kullanım Koşulları" bağlantıları (Apple 3.1.2: abonelik ekranında görünür olmalı).
    /// Çocuk oyunu olduğu için dış bağlantı ebeveyn kapısının arkasında açılır. Kullanım Koşulları (Apple standart EULA)
    /// yalnızca iOS'ta gösterilir; Android'de yalnızca gizlilik politikası kalır ve ortalanır.
    /// </summary>
    public class LegalLinks : MonoBehaviour
    {
        public const string PrivacyUrl = "https://alpisgames.com/index.php?page=privacy";
        public const string TermsUrl = "https://www.apple.com/legal/internet-services/itunes/dev/stdeula/";

        [SerializeField] private ParentalGate gate;
        [SerializeField] private RectTransform privacyButton;
        [SerializeField] private RectTransform termsButton;

        private void Awake()
        {
            if (ShowTerms()) return;
            // Android: yalnızca gizlilik politikası, ortada
            if (termsButton != null) termsButton.gameObject.SetActive(false);
            if (privacyButton != null)
            {
                Vector2 pos = privacyButton.anchoredPosition;
                pos.x = 0f;
                privacyButton.anchoredPosition = pos;
            }
        }

        private static bool ShowTerms()
        {
#if UNITY_EDITOR
            return true;
#else
            return Application.platform == RuntimePlatform.IPhonePlayer;
#endif
        }

        public void OpenPrivacy() => Open(PrivacyUrl);

        public void OpenTerms() => Open(TermsUrl);

        private void Open(string url)
        {
            if (gate != null) gate.Ask(() => Application.OpenURL(url));
            else Application.OpenURL(url);
        }
    }
}
