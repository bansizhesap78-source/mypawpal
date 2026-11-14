using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Yiyecek yakala: kase parmağı takip eder; yukarıdan yiyecek (renkli) ve çöp (gri) düşer.
    /// Yiyecek +1, çöp −2. 30 saniye; puan yakalanan yiyecek. Seviye arttıkça hızlanır, çöp çoğalır. Balıkta kase yok:
    /// yemi balığın kendisi kapar, düşenler de türün yemleridir.
    /// </summary>
    public class CatchGame : MiniGameBase, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private RectTransform area;
        [SerializeField] private RectTransform bowl;
        [SerializeField] private Image[] drops;
        [SerializeField] private Color[] foodColors;
        [SerializeField] private Color junkColor;
        [Tooltip("Çizimler varsa renkli daireler yerine bunlar düşer")]
        [SerializeField] private Sprite[] foodSprites;
        [SerializeField] private Sprite junkSprite;
        [Tooltip("Arada bir düşen bonus yıldız (+3)")]
        [SerializeField] private Sprite starSprite;

        private const int StarPoints = 3;
        private readonly bool[] junk = new bool[16];
        private readonly bool[] star = new bool[16];
        private readonly float[] speeds = new float[16];
        private float timeLeft, spawnTimer, targetX;
        private int caught, level;
        private float bowlHalf;
        private bool running;

        protected override void StartGame()
        {
            timeLeft = MiniGameRules.CatchSeconds;
            caught = 0;
            level = GameLevels.Get(GameSession.Data, MiniGame.Catch);
            UseSpeciesLook();
            bowlHalf = bowl.sizeDelta.x * 0.5f;
            foreach (Image drop in drops) drop.gameObject.SetActive(false);
            targetX = bowl.anchoredPosition.x;
            running = true;
        }

        private void UseSpeciesLook()
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            Species species = SpeciesCatalog.Get(pet.species);
            if (species.moves == MoveKind.Swim)
            {
                var image = bowl.GetComponent<Image>();
                image.sprite = PetArt.ForPet(pet, "happy");
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                bowl.sizeDelta = new Vector2(260f, 260f);
            }
            // Düşenler türün kendi yemekleri: panda bambu, aslan et yakalar
            var foods = new System.Collections.Generic.List<Sprite>();
            foreach (string food in species.foods)
            {
                Sprite sprite = PetArt.Get("icon_food_" + food);
                if (sprite != null) foods.Add(sprite);
            }
            if (foods.Count > 0) foodSprites = foods.ToArray();
        }

        public void OnPointerDown(PointerEventData eventData) => Aim(eventData);
        public void OnDrag(PointerEventData eventData) => Aim(eventData);

        private void Aim(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 local))
                targetX = Mathf.Clamp(local.x, -area.rect.width * 0.5f + 100f, area.rect.width * 0.5f - 100f);
        }

        private void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            timeLeft -= dt;
            Host.SetScore(Loc.Format("catch_score", caught, Mathf.CeilToInt(Mathf.Max(0f, timeLeft))));
            if (timeLeft <= 0f)
            {
                running = false;
                foreach (Image drop in drops) drop.gameObject.SetActive(false);
                Host.Finish(caught);
                return;
            }

            Vector2 bowlPos = bowl.anchoredPosition;
            bowlPos.x = Mathf.MoveTowards(bowlPos.x, targetX, 2200f * dt);
            bowl.anchoredPosition = bowlPos;
            // Kase yakalayınca zıplar
            bowlBounce = Mathf.MoveTowards(bowlBounce, 0f, dt * 4f);
            float squash = Mathf.Sin(bowlBounce * Mathf.PI) * 0.2f;
            bowl.localScale = new Vector3(1f + squash, 1f - squash, 1f);

            // Zaman geçtikçe daha sık ve daha hızlı düşer; öğrenirken ya da zorlanırken daha yavaş
            float elapsed = MiniGameRules.CatchSeconds - timeLeft;
            bool easy = Coach != null && (Coach.Learning || Coach.Struggling);
            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                float pace = GameLevels.CatchSpeed(level);
                spawnTimer = Mathf.Lerp(0.8f, 0.45f, elapsed / MiniGameRules.CatchSeconds) / Mathf.Sqrt(pace) * (easy ? 1.3f : 1f);
                Spawn(Mathf.Lerp(420f, 760f, elapsed / MiniGameRules.CatchSeconds) * pace * (easy ? 0.75f : 1f));
            }

            float half = area.rect.height * 0.5f;
            RectTransform guide = null;   // rehber elin göstereceği: kaseye en yakın (en alçak) yiyecek
            for (int i = 0; i < drops.Length; i++)
            {
                if (!drops[i].gameObject.activeSelf) continue;
                RectTransform rt = drops[i].rectTransform;
                Vector2 p = rt.anchoredPosition;
                p.y -= speeds[i] * dt;
                rt.anchoredPosition = p;
                if (star[i]) rt.Rotate(0f, 0f, 200f * dt);

                if (Mathf.Abs(p.y - bowlPos.y) < 60f && Mathf.Abs(p.x - bowlPos.x) < bowlHalf + 20f)
                {
                    if (junk[i])
                    {
                        caught = Mathf.Max(0, caught - 2);
                        Sound.Play("error", 0.6f);
                        Coach?.Fail();
                    }
                    else
                    {
                        caught += star[i] ? StarPoints : 1;
                        Sound.Play(star[i] ? "gem" : "bubble", 0.7f, 1f + Mathf.Min(caught, 20) * 0.02f);
                        Fx.Burst(bowl, star[i] ? "icon_fx_star" : "icon_fx_sparkle", bowl.position, star[i] ? 6 : 2, 60f);
                        bowlBounce = 1f;
                        Coach?.Success();
                    }
                    drops[i].gameObject.SetActive(false);
                }
                else if (p.y < -half - 80f)
                {
                    // Yiyecek kaçtı (çöp kaçarsa sorun değil)
                    if (!junk[i]) Coach?.Fail();
                    drops[i].gameObject.SetActive(false);
                }
                else if (!junk[i] && p.y > bowlPos.y && (guide == null || p.y < guide.anchoredPosition.y)) guide = rt;
            }

            // Rehber el kaseden yiyeceğin altına doğru sürükler
            if (Coach != null && Coach.Wanted && guide != null)
            {
                Vector3 from = bowl.position;
                Vector3 to = new Vector3(guide.position.x, bowl.position.y, bowl.position.z);
                Coach.Drag(from, to);
            }
        }

        private float bowlBounce;

        private void Spawn(float speed)
        {
            for (int i = 0; i < drops.Length; i++)
            {
                if (drops[i].gameObject.activeSelf) continue;
                junk[i] = Random.value < GameLevels.CatchJunk(level);
                // Arada bir bonus yıldız (çöp değilse)
                star[i] = !junk[i] && starSprite != null && Random.value < 0.08f;
                speeds[i] = speed * Random.Range(0.9f, 1.15f) * (star[i] ? 1.2f : 1f);
                drops[i].rectTransform.localRotation = Quaternion.identity;
                if (junkSprite != null && foodSprites != null && foodSprites.Length > 0)
                {
                    drops[i].sprite = junk[i] ? junkSprite : star[i] ? starSprite : foodSprites[Random.Range(0, foodSprites.Length)];
                    drops[i].color = Color.white;
                }
                else drops[i].color = junk[i] ? junkColor : foodColors[Random.Range(0, foodColors.Length)];
                drops[i].rectTransform.anchoredPosition = new Vector2(
                    Random.Range(-area.rect.width * 0.5f + 80f, area.rect.width * 0.5f - 80f), area.rect.height * 0.5f + 60f);
                drops[i].gameObject.SetActive(true);
                return;
            }
        }
    }
}
