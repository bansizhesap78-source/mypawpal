using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Cüzdandaki altın/elmas sayısı. Artınca altınlar son dokunulan yerden (yoksa ekranın ortasından) uçarak
    /// ikona gelir, ikon zıplar, sayı tıkır tıkır artar. Azalınca sayı hemen düşer, ikon sallanır.
    /// Denetleyiciler yazıyı her yenilemede yazar; bu bileşen LateUpdate'te kendi gösterdiği değeri üstüne yazar.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class CurrencyCounter : MonoBehaviour
    {
        [SerializeField] private bool diamond;
        [SerializeField] private RectTransform icon;

        private Text text;
        private bool ready;
        private int shown, countFrom, countTo, written = int.MinValue;
        private string shownText;
        private float countTimer, bump;
        private Vector3 lastTap;
        private float lastTapTime = -10f;
        private Vector3 iconScale = Vector3.one;

        private int Actual
        {
            get
            {
                SaveData data = GameSession.Data;
                return data == null ? 0 : diamond ? data.diamonds : data.gold;
            }
        }

        private void Awake()
        {
            text = GetComponent<Text>();
            if (icon != null) iconScale = icon.localScale;
        }

        private void OnEnable()
        {
            // Sahne açılırken uçuş yok: değer olduğu gibi gösterilir
            ready = false;
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed)
            {
                lastTap = pointer.position.ReadValue();
                lastTapTime = Time.unscaledTime;
            }
        }

        private void LateUpdate()
        {
            int actual = Actual;
            if (!ready)
            {
                shown = countFrom = countTo = actual;
                ready = true;
            }
            else if (actual > countTo) Earn(actual);
            else if (actual < countTo)
            {
                // Harcama: sayı hemen düşer, ikon sallanır
                shown = countFrom = countTo = actual;
                countTimer = 0f;
                bump = -1f;
            }

            if (countTimer > 0f)
            {
                countTimer -= Time.unscaledDeltaTime;
                // İlk 0.45 sn altınlar yolda; sonra 0.6 sn'de sayılır
                float t = Mathf.Clamp01((1.05f - countTimer - 0.45f) / 0.6f);
                shown = Mathf.RoundToInt(Mathf.Lerp(countFrom, countTo, t));
                if (countTimer <= 0f) shown = countTo;
            }
            // Sayı değişince yazı bir kez üretilir; denetleyici araya kendi yazısını yazdıysa bizimki geri konur
            if (shown != written)
            {
                written = shown;
                shownText = shown.ToString();
            }
            if (!ReferenceEquals(text.text, shownText)) text.text = shownText;

            if (icon != null)
            {
                if (bump > 0f)
                {
                    bump = Mathf.Max(0f, bump - Time.unscaledDeltaTime * 5f);
                    icon.localScale = iconScale * (1f + bump * 0.35f);
                    icon.localRotation = Quaternion.identity;
                }
                else if (bump < 0f)
                {
                    bump = Mathf.Min(0f, bump + Time.unscaledDeltaTime * 3f);
                    icon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(bump * 40f) * 14f * -bump);
                }
            }
        }

        private void Earn(int actual)
        {
            int gained = actual - countTo;
            countFrom = shown;
            countTo = actual;
            countTimer = 1.05f;
            Vector3 from = Time.unscaledTime - lastTapTime < 2f ? lastTap : Fx.ScreenCenter;
            int coins = Mathf.Clamp(gained / (diamond ? 1 : 10) + 2, 3, 10);
            // Her para varınca "çın" sesi biraz daha tizleşir (biriken ödül hissi)
            int arrived = 0;
            Fx.Fly(transform, diamond ? "icon_fx_gem" : "icon_fx_coin", from, icon != null ? icon : (RectTransform)transform,
                coins, 80f, () =>
                {
                    bump = 1f;
                    Sound.Play(diamond ? "gem" : "coin", 0.55f, 1f + 0.05f * arrived++);
                });
        }
    }
}
