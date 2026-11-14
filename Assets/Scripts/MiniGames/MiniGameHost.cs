using System;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>Mini oyunların ortak arayüzü: host başlatır, oyun bitince puanı bildirir.</summary>
    public abstract class MiniGameBase : MonoBehaviour
    {
        protected MiniGameHost Host { get; private set; }

        /// <summary>Rehber el: oyunlar nereyi göstereceğini söyler, ne zaman çıkacağına el karar verir.</summary>
        protected GameCoach Coach => Host.Coach;

        public void Begin(MiniGameHost host)
        {
            Host = host;
            gameObject.SetActive(true);
            StartGame();
        }

        protected abstract void StartGame();

        /// <summary>
        /// Dostun çizimi varsa yer tutucu patiyi gizleyip onun mutlu pozunu gösterir (patinin "Art" çocuğu).
        /// </summary>
        protected static void ShowPetArt(RectTransform pet)
        {
            Transform artChild = pet.Find("Art");
            Sprite sprite = GameSession.CurrentPet != null ? PetArt.ForPet(GameSession.CurrentPet, "happy") : null;
            if (artChild == null || sprite == null) return;
            foreach (Transform child in pet) child.gameObject.SetActive(child == artChild);
            artChild.GetComponent<Image>().sprite = sprite;
        }
    }

    /// <summary>
    /// MiniGame sahnesi: seçili oyunu (Selected) açar; üstte adı, puanı ve geri butonu. Oyun bitince altını
    /// Economy.FinishGame ile verir, sonuç kartını gösterir: "Tekrar" ya da "Odaya dön".
    /// Geri butonuyla çıkan altın almaz (dosta da etkisi olmaz).
    /// </summary>
    public class MiniGameHost : MonoBehaviour
    {
        public const string MiniGameScene = "MiniGame";
        public static MiniGame Selected = MiniGame.Fetch;

        public static readonly string[] TitleKeys = { "game_fetch", "game_catch", "game_memory", "game_hide", "game_bubble" };

        [SerializeField] private MiniGameBase[] games;
        [SerializeField] private Text title;
        [SerializeField] private Text scoreText;
        [SerializeField] private GameObject result;
        [SerializeField] private Text resultText;
        [SerializeField] private GameObject againButton;
        [Tooltip("Oyunun arka planı (MiniGame sırasıyla: park, piknik, masa, salon); çizim yoksa düz renk")]
        [SerializeField] private Image background;
        [SerializeField] private Sprite[] backgrounds;
        [SerializeField] private GameCoach coach;
        [Tooltip("Sonuç kartındaki 3 yıldız (puanın oyunun en iyi puanına oranına göre dolar)")]
        [SerializeField] private Image[] resultStars;
        [Tooltip("Sonuç kartında seviye atlama yazısı")]
        [SerializeField] private Text levelText;

        private int level;
        private string scoreLine = "";

        public GameCoach Coach => coach;

        private void Start()
        {
            if (GameSession.CurrentPet == null)
            {
                LoadingScreen.Go(HomeController.HomeScene);
                return;
            }
            // Türe uymayan oyun açılmaz (balıkta top getir)
            Species species = SpeciesCatalog.Get(GameSession.CurrentPet.species);
            if (!GameLevels.Available(species, Selected)) Selected = MiniGame.Bubble;
            level = GameLevels.Get(GameSession.Data, Selected);
            int index = (int)Selected;
            // Suda yaşayan her oyunu su altında oynar
            Sprite habitat = species.moves == MoveKind.Swim ? PetArt.RoomOf(species, "play") : null;
            if (habitat != null && background != null)
            {
                background.sprite = habitat;
                background.color = Color.white;
            }
            else
            if (background != null && backgrounds != null && index < backgrounds.Length && backgrounds[index] != null)
            {
                background.sprite = backgrounds[index];
                background.color = Color.white;
            }
            result.SetActive(false);
            title.text = Loc.Get(TitleKeys[(int)Selected]);
            SetScore("");
            if (levelText != null) levelText.text = "";
            for (int i = 0; i < games.Length; i++) games[i].gameObject.SetActive(false);
            if (coach != null) coach.Begin(Selected);
            games[(int)Selected].Begin(this);
        }

        /// <summary>Üst satır her zaman seviyeyi de söyler.</summary>
        public void SetScore(string text)
        {
            scoreLine = text;
            string prefix = Loc.Format("game_level", level);
            scoreText.text = string.IsNullOrEmpty(scoreLine) ? prefix : prefix + " · " + scoreLine;
        }

        public void Finish(int score)
        {
            PetState pet = GameSession.CurrentPet;
            int gold = Economy.FinishGame(GameSession.Data, pet, Selected, score, DateTime.Now);
            LevelOutcome outcome = GameLevels.Complete(GameSession.Data, Selected, score);
            Progress.Count(GameSession.Data, "minigame", DateTime.Now);
            Progress.CheckBadges(GameSession.Data);
            GameSession.Save();
            // Puan 0 ise övgü ve kutlama yok; katılım altını (Economy.MinPlayGold) yine verilir
            bool scored = score > 0;
            resultText.text = Loc.Format(scored ? "game_result" : "game_result_zero", gold);
            if (levelText != null)
            {
                if (!outcome.leveledUp) levelText.text = Loc.Format("game_level_again", level);
                else if (outcome.bonusDiamonds > 0) levelText.text = Loc.Format("game_level_up_gem", outcome.level, outcome.bonusGold, outcome.bonusDiamonds);
                else levelText.text = Loc.Format("game_level_up", outcome.level, outcome.bonusGold);
            }
            againButton.SetActive(Economy.CanPlay(pet));
            result.SetActive(true);
            // 0-3 yıldız: okuyamayan çocuk da ne kadar iyi oynadığını görür (puan varsa en az 1: oynadı, aferin)
            if (resultStars != null && resultStars.Length == 3)
            {
                StartCoroutine(PopStars(outcome.stars));
            }
            // Kazanınca kutlama: konfeti ve sonuç yazısından altın saçılır
            if (scored && gold > 0)
            {
                Sound.Play("fanfare");
                Fx.Confetti(resultText.transform, resultText.transform.position, 70);
                Fx.Burst(resultText.transform, "icon_fx_coin", resultText.transform.position, Mathf.Clamp(gold / 5, 4, 12), 80f);
            }
        }

        /// <summary>Yıldızlar sırayla "pıt" diye dolar; kazanılmayanlar soluk kalır.</summary>
        private System.Collections.IEnumerator PopStars(int earned)
        {
            foreach (Image star in resultStars)
            {
                star.color = new Color(1f, 1f, 1f, 0.25f);
                star.transform.localScale = Vector3.one;
            }
            for (int i = 0; i < earned; i++)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                resultStars[i].color = Color.white;
                Sound.Play("sparkle", 0.8f, 1f + i * 0.15f);
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime * 5f)
                {
                    float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.45f;
                    resultStars[i].transform.localScale = new Vector3(s, s, 1f);
                    yield return null;
                }
                resultStars[i].transform.localScale = Vector3.one;
            }
        }

        public void Again() => LoadingScreen.Go(MiniGameScene);
        public void Back() => LoadingScreen.Go(PetController.PetScene);
    }
}
