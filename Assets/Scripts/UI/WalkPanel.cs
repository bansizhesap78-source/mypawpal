using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Dışarıda gezinti (balık yüzmeye, kuş uçmaya çıkar; dokununca yükselir): manzara akar, dost (üstündeki kıyafetlerle) yürür. Yerdeki altınları kendi toplar; havadaki
    /// yıldız ve kalplere dokununca zıplar. Ara sıra tökezleyip yuvarlanır, gülerek kalkar. Eve dönünce eğlence ve sevgi
    /// artar, toplanan altın (en çok Economy.WalkCoinCap) cüzdana geçer. Kaybetme yok: yalnızca dostla dışarı çıkma keyfi.
    /// </summary>
    public class WalkPanel : MonoBehaviour
    {
        private const float GroundSpeed = 250f, Duration = 45f;
        private const float JumpSpeed = 1500f, Gravity = 3200f;
        private const float SwimLift = 720f, SwimSink = 900f, SwimTop = 1000f, SwimBottom = 60f;

        [SerializeField] private GameObject root;
        [SerializeField] private PetController owner;
        [SerializeField] private RectTransform[] tiles;
        [SerializeField] private PetView view;
        [SerializeField] private RectTransform petRoot;
        [SerializeField] private RectTransform petShadow;
        [SerializeField] private RectTransform itemLayer;
        [SerializeField] private Text coinText;
        [SerializeField] private float floorY = -520f;

        private sealed class Item
        {
            public RectTransform rt;
            public float height;
            public int coins;
        }

        private readonly List<Item> items = new List<Item>();
        private float scroll, age, spawnTimer, petH, petV, tripTimer, pace = 1f;
        private bool tumbling;
        private int flipsPaid;
        /// <summary>Takla başına altın: bir gezintide en çok bu kadar kez.</summary>
        private const int MaxPaidFlips = 5;
        private MoveKind moves;
        private Sprite groundArt;
        private int coins;

        public bool IsOpen => root.activeSelf;

        private void Awake()
        {
            root.SetActive(false);
        }

        public void Open()
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            root.SetActive(true);
            moves = SpeciesCatalog.Get(pet.species).moves;
            if (groundArt == null) groundArt = tiles[0].GetComponent<Image>().sprite;
            Sprite scene = PetArt.WalkScene(SpeciesCatalog.Get(pet.species));
            foreach (RectTransform tile in tiles) tile.GetComponent<Image>().sprite = scene != null ? scene : groundArt;
            petShadow.gameObject.SetActive(moves == MoveKind.Walk);
            view.HoldPose("happy", float.MaxValue, true);
            view.Apply(pet, SpeciesCatalog.Get(pet.species));
            view.SetFacing(1);
            StopAllCoroutines();
            tumbling = false;
            flipsPaid = 0;
            pace = 1f;
            view.ResetGesture();
            tripTimer = Random.Range(6f, 10f);
            coins = 0;
            age = scroll = petV = 0f;
            petH = moves == MoveKind.Walk ? 0f : 380f;
            spawnTimer = 0.6f;
            coinText.text = "0";
            foreach (Item item in items)
                if (item.rt != null) Destroy(item.rt.gameObject);
            items.Clear();
            Sound.Music("music_game");
            Sound.PetVoice(pet.species, 0.8f);
        }

        /// <summary>Eve dön: gezinti dostun durumuna işlenir, altın cüzdana geçer.</summary>
        public void Close()
        {
            root.SetActive(false);
            owner.FinishWalk(Mathf.Min(coins, Economy.WalkCoinCap));
        }

        /// <summary>Ekrana dokununca zıplar.</summary>
        public void Jump()
        {
            if (moves != MoveKind.Walk)
            {
                petV = SwimLift;
                Sound.Play(moves == MoveKind.Swim ? "bubble" : "whoosh", 0.5f);
                if (moves == MoveKind.Swim) Fx.Burst(transform, "icon_fx_bubble", petRoot.position, 2, 46f, true, 0.7f);
                return;
            }
            if (petH > 1f || tumbling) return;
            petV = JumpSpeed;
            Sound.Play("bounce", 0.7f);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            age += dt;

            // Manzara: aynalı karolar birleşince ek yeri görünmez
            scroll += GroundSpeed * pace * dt;
            float width = tiles[0].rect.width;
            float offset = scroll % (width * 2f);
            for (int i = 0; i < tiles.Length; i++)
                tiles[i].anchoredPosition = new Vector2((i - 1) * width - offset, 0f);

            // Yürüyüş ve zıplama
            if (moves == MoveKind.Walk)
            {
                petV -= Gravity * dt;
                petH = Mathf.Max(0f, petH + petV * dt);
                if (petH <= 0f) petV = 0f;
            }
            else
            {
                // Yüzer ya da uçar: dokunuşla yükselir, yavaşça alçalır
                petV = Mathf.Max(petV - SwimSink * dt, -240f);
                petH = Mathf.Clamp(petH + petV * dt, SwimBottom, SwimTop);
            }
            float swing = Mathf.Sin(age * 9f);
            if (moves != MoveKind.Walk)
            {
                view.StepLift = 0f;
                view.Lean(petV > 0f ? -6f : 4f);
            }
            else if (!tumbling)
            {
                view.StepLift = petH > 0f ? 0f : Mathf.Abs(swing) * 28f;
                view.Lean(petH > 0f ? 6f : swing * 3f);
                tripTimer -= dt;
                if (tripTimer <= 0f && petH <= 0f)
                {
                    tripTimer = Random.Range(9f, 16f);
                    StartCoroutine(Tumble());
                }
            }
            petRoot.anchoredPosition = new Vector2(-120f, floorY + petH);
            float air = Mathf.Clamp01(1f - petH / 500f);
            petShadow.localScale = new Vector3(0.6f + air * 0.4f, 0.6f + air * 0.4f, 1f);

            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                spawnTimer = Random.Range(0.9f, 1.6f);
                Spawn();
            }
            MoveItems(dt);
        }

        /// <summary>Takla tuşu: dost havada takla atar (balık suda döner, kuş havada); ilk birkaçı altın kazandırır.</summary>
        public void Flip()
        {
            if (tumbling) return;
            StartCoroutine(FlipRoutine());
        }

        private IEnumerator FlipRoutine()
        {
            tumbling = true;
            view.StepLift = 0f;
            view.Lean(0f);
            Sound.Play("whoosh", 0.8f);
            string fx = moves == MoveKind.Swim ? "icon_fx_bubble" : moves == MoveKind.Fly ? "icon_fx_petal" : "icon_fx_star";
            float lift = moves == MoveKind.Walk ? 300f : 120f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
            {
                view.Spin = -360f * Mathf.SmoothStep(0f, 1f, t);
                view.Offset = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * lift);
                yield return null;
            }
            view.Spin = 0f;
            view.Offset = Vector2.zero;
            Vector3 at = petRoot.position + Vector3.up * 220f * transform.lossyScale.y;
            Fx.Burst(transform, fx, at, 6, 60f, true, 0.9f);
            if (flipsPaid < MaxPaidFlips)
            {
                flipsPaid++;
                coins++;
                coinText.text = Mathf.Min(coins, Economy.WalkCoinCap).ToString();
                Sound.Play("sparkle", 0.6f);
            }
            if (moves == MoveKind.Walk)
            {
                Sound.Play("drop", 0.5f);
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.18f)
                {
                    view.Squish = Mathf.Sin(t * Mathf.PI) * 0.18f;
                    yield return null;
                }
                view.Squish = 0f;
            }
            PetState pet = GameSession.CurrentPet;
            if (pet != null) Sound.PetVoice(pet.species, 0.6f);
            tumbling = false;
        }

        /// <summary>Takılıp yuvarlanır: toz kalkar, yere oturur, gülerek silkelenip yoluna devam eder.</summary>
        private IEnumerator Tumble()
        {
            tumbling = true;
            view.StepLift = 0f;
            view.Lean(0f);
            Sound.Play("whoosh", 0.7f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.16f)
            {
                view.Tilt = -40f * t;
                yield return null;
            }
            view.Tilt = 0f;
            // İki tur yuvarlanır (yol hızlanır)
            pace = 1.8f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.8f)
            {
                view.Spin = -40f - 680f * t;
                view.Offset = new Vector2(0f, Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 60f);
                if (Random.value < 0.12f)
                    Fx.Burst(transform, "icon_fx_dust", petRoot.position, 1, 60f, true, 0.6f);
                yield return null;
            }
            view.Spin = 0f;
            view.Offset = Vector2.zero;
            Sound.Play("drop", 0.8f);
            pace = 0f;
            Fx.Burst(transform, "icon_fx_star", petRoot.position + Vector3.up * 200f * transform.lossyScale.y, 4, 56f, true, 0.9f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.24f)
            {
                view.Squish = Mathf.Sin(t * Mathf.PI) * 0.26f;
                yield return null;
            }
            view.Squish = 0f;
            view.Wiggle(0.4f);
            PetState pet = GameSession.CurrentPet;
            if (pet != null) Sound.PetVoice(pet.species, 0.7f);
            yield return new WaitForSecondsRealtime(0.45f);
            pace = 1f;
            tumbling = false;
        }

        private void Spawn()
        {
            bool high = Random.value < 0.45f;
            string art = high ? (Random.value < 0.5f ? "icon_fx_star" : "icon_fx_heart") : "icon_fx_coin";
            var go = new GameObject("Item", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(itemLayer, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 120f);
            var image = go.GetComponent<Image>();
            image.sprite = PetArt.Get(art);
            image.preserveAspect = true;
            image.raycastTarget = false;
            // Yüzen ve uçan için her yükseklikte; yürüyen için yerde ve zıplama boyunda
            float height = moves != MoveKind.Walk ? Random.Range(100f, 1050f) : high ? Random.Range(380f, 520f) : 80f;
            rt.anchoredPosition = new Vector2(760f, floorY + height);
            items.Add(new Item { rt = rt, height = height, coins = art == "icon_fx_star" ? 5 : art == "icon_fx_coin" ? 2 : 0 });
        }

        private void MoveItems(float dt)
        {
            float petTop = petH + 420f * view.VisualScale;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item item = items[i];
                Vector2 p = item.rt.anchoredPosition;
                p.x -= GroundSpeed * pace * dt;
                p.y = floorY + item.height + Mathf.Sin(age * 3f + i) * 12f;
                item.rt.anchoredPosition = p;
                if (Mathf.Abs(p.x - petRoot.anchoredPosition.x) < 150f && item.height > petH - 40f && item.height < petTop + 60f)
                {
                    coins += item.coins;
                    coinText.text = Mathf.Min(coins, Economy.WalkCoinCap).ToString();
                    Fx.Burst(transform, item.coins > 0 ? "icon_fx_sparkle" : "icon_fx_heart", item.rt.position, 3, 56f, true, 0.8f);
                    Sound.Play(item.coins > 0 ? "sparkle" : "heart", 0.6f);
                    Destroy(item.rt.gameObject);
                    items.RemoveAt(i);
                }
                else if (p.x < -760f)
                {
                    Destroy(item.rt.gameObject);
                    items.RemoveAt(i);
                }
            }
        }
    }
}
