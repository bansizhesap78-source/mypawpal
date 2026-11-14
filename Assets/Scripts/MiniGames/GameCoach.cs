using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Mini oyunlardaki rehber el (okuma bilmeyen çocuk için). Ne zaman çıkar:
    /// - öğrenirken: o oyunda toplam LearnedAfter başarılı hamle yapılana kadar (cihazda saklanır),
    /// - zorlanınca: art arda FailsForHelp başarısız hamlede (bir başarı sayacı sıfırlar),
    /// - bekleyince: IdleSeconds boyunca ekrana dokunulmazsa.
    /// Oyunlar her karede elin nereye gideceğini söyler (Point / Drag); el gerekmiyorsa gizlenir.
    /// </summary>
    public class GameCoach : MonoBehaviour
    {
        public const int LearnedAfter = 4;
        public const int FailsForHelp = 2;
        public const float IdleSeconds = 5f;

        /// <summary>El çizimlerinde parmak ucunun yeri (PetController ile aynı ölçüm).</summary>
        private static readonly Vector2 PointTip = new Vector2(0.41f, 0.97f), TapTip = new Vector2(0.48f, 0.19f);

        [SerializeField] private Image hand;

        private Sprite point, tap;
        private string key;
        private int failStreak;
        private float idle;
        private bool shownThisFrame;

        public bool Learning => PlayerPrefs.GetInt(key, 0) < LearnedAfter;

        /// <summary>El şu an gerekli mi (öğrenirken, zorlanınca ya da uzun süre dokunulmayınca).</summary>
        public bool Wanted => Learning || failStreak >= FailsForHelp || idle > IdleSeconds;

        /// <summary>Çocuk zorlanıyor: el cevabı da gösterebilir (ör. saklambaçta doğru kutu).</summary>
        public bool Struggling => failStreak >= FailsForHelp;

        public void Begin(MiniGame game) => Begin(game.ToString());

        /// <summary>Okul dersi gibi mini oyun dışı yerler için: id cihazdaki öğrenme sayacının adıdır.</summary>
        public void Begin(string id)
        {
            key = "coach_" + id;
            point = PetArt.Get("icon_hand_point") ?? PetArt.Get("icon_ui_paw");
            tap = PetArt.Get("icon_hand_tap");
            failStreak = 0;
            idle = 0f;
            hand.gameObject.SetActive(false);
        }

        public void Success()
        {
            failStreak = 0;
            if (Learning) PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
        }

        public void Fail() => failStreak++;

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed) idle = 0f;
            else idle += Time.deltaTime;
        }

        private void LateUpdate()
        {
            // Bu kare hiçbir oyun eli istemediyse gizle
            if (!shownThisFrame && hand.gameObject.activeSelf) hand.gameObject.SetActive(false);
            shownThisFrame = false;
        }

        /// <summary>
        /// Eli hedefin üstüne getirir; pressed true iken basar (basan el çizimi), false iken üstünde bekler.
        /// pressed null ise kendiliğinden basıp kalkar.
        /// </summary>
        public void Point(Vector3 target, bool? pressed = null)
        {
            float cycle = Mathf.Repeat(Time.unscaledTime * 1.2f, 1f);
            float press = pressed.HasValue
                ? (pressed.Value ? 1f : 0f)
                : cycle < 0.5f ? Mathf.SmoothStep(0f, 1f, cycle * 2f) : Mathf.SmoothStep(1f, 0f, (cycle - 0.5f) * 2f);
            Place(target + new Vector3(0f, (10f + 60f * (1f - press)) * Unit, 0f), press > 0.85f);
        }

        /// <summary>Parmak from'dan to'ya kayar (sürükleme hareketi), sonra baştan.</summary>
        public void Drag(Vector3 from, Vector3 to)
        {
            float t = Mathf.Repeat(Time.unscaledTime * 0.7f, 1f);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.1f) / 0.7f));
            Place(Vector3.Lerp(from, to, k), t > 0.1f && t < 0.8f);
        }

        private float Unit => hand.canvas != null ? hand.canvas.scaleFactor : 1f;

        private void Place(Vector3 tip, bool tapping)
        {
            shownThisFrame = true;
            if (!hand.gameObject.activeSelf) hand.gameObject.SetActive(true);
            tapping &= tap != null;
            hand.sprite = tapping ? tap : point;
            hand.rectTransform.pivot = tapping ? TapTip : PointTip;
            // İşaret eli yukarıyı gösterir: dikey çevrilir (parmak aşağı); basan el zaten aşağı bakar
            hand.rectTransform.localScale = new Vector3(1f, tapping ? 1f : -1f, 1f);
            hand.transform.position = tip;
        }
    }
}
