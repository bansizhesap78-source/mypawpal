using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Oyunun açılış ve sahne geçişi ekranı (Unity açılış logosunun yerine). Boot sahnesinde durur,
    /// sahneler arasında yaşamaya devam eder ve her geçişte yeniden görünür: arka plan, oyunun adı,
    /// kısa bir ipucu ve üstünde pati zıplayan dolum çubuğu.
    /// Boot sahnesi olmadan açılan sahnelerde (editörde doğrudan oynatma) geçişler ekransız olur.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        public static LoadingScreen Instance { get; private set; }

        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image fill;
        [SerializeField] private RectTransform runner;
        [SerializeField] private float barWidth = 760f;
        [SerializeField] private Text tip;

        [Tooltip("İlk açılışta ekran en az bu kadar kalır (logo görülsün)")]
        [SerializeField] private float firstMinTime = 2.2f;
        [Tooltip("Sahne geçişlerinde ekran en az bu kadar kalır")]
        [SerializeField] private float switchMinTime = 0.9f;
        [SerializeField] private float fadeTime = 0.25f;

        private bool busy;

        /// <summary>Geldiği ekranlar: geri butonu çocuğu kaldığı yere döndürür. Ana sayfaya gelince sıfırlanır.</summary>
        private static readonly Stack<string> history = new Stack<string>();

        /// <summary>Yükleme ekranıyla sahne değiştirir; ekran yoksa doğrudan yükler.</summary>
        public static void Go(string scene)
        {
            string current = SceneManager.GetActiveScene().name;
            if (scene == HomeController.HomeScene) history.Clear();
            else if (current != scene && current != "Boot") history.Push(current);
            Open(scene);
        }

        /// <summary>Bir önceki ekrana döner; geçmiş yoksa ana sayfaya.</summary>
        public static void Back()
        {
            string current = SceneManager.GetActiveScene().name;
            while (history.Count > 0)
            {
                string previous = history.Pop();
                if (previous == current) continue;
                if (previous == HomeController.HomeScene) history.Clear();
                Open(previous);
                return;
            }
            Go(HomeController.HomeScene);
        }

        private static void Open(string scene)
        {
            if (Instance != null) Instance.Load(scene, false);
            else SceneManager.LoadScene(scene);
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            group.alpha = 1f;
            // Mağaza açılışta bağlansın: fiyatlar gelsin, yarım kalan alımlar tamamlansın
            _ = StoreService.Instance;
            // Uzaktayken ölen dost varsa Anı Bahçesi'ne; yoksa her açılışta ana sayfa (dostlar orada)
            string first = GameSession.LastDeath != null ? MemorialController.MemorialScene : HomeController.HomeScene;
            Load(first, true);
        }

        private void Load(string scene, bool first)
        {
            if (busy) return;
            StartCoroutine(LoadRoutine(scene, first));
        }

        private IEnumerator LoadRoutine(string scene, bool first)
        {
            busy = true;
            canvas.enabled = true;
            group.blocksRaycasts = true;
            tip.text = Loc.Get("tip_" + Random.Range(1, Loc.TipCount + 1));
            SetProgress(0f);

            if (!first) yield return Fade(0f, 1f);

            AsyncOperation op = SceneManager.LoadSceneAsync(scene);
            op.allowSceneActivation = false;
            float minTime = first ? firstMinTime : switchMinTime;
            float shown = 0f;
            for (float t = 0f; ; t += Time.unscaledDeltaTime)
            {
                // Çubuk hem gerçek yüklemeyi hem en kısa süreyi bekler; hiç geri gitmez
                float target = Mathf.Min(op.progress / 0.9f, t / minTime);
                shown = Mathf.Max(shown, Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 2.5f));
                SetProgress(shown);
                if (shown >= 0.999f && op.progress >= 0.9f) break;
                yield return null;
            }

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            yield return null;

            yield return Fade(1f, 0f);
            group.blocksRaycasts = false;
            canvas.enabled = false;
            busy = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            group.alpha = to;
        }

        private void Update()
        {
            if (!canvas.enabled) return;
            // Pati çubuğun ucunda sekerek ilerler
            float hop = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7f)) * 26f;
            runner.anchoredPosition = new Vector2(-barWidth * 0.5f + barWidth * fill.fillAmount, 20f + hop);
            runner.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 7f) * 8f);
        }

        private void SetProgress(float value)
        {
            fill.fillAmount = value;
        }
    }
}
