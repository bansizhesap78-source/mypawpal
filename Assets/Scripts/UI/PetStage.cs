using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Dostu canlı gösteren sahne: yemek kabı yere düşer ya da yemek havadan gelir, dost zıplayıp kapar (bazen kaçırır);
    /// top düşer, dost peşinden koşar, bazen kafasına çarpar ve başı döner; banyoda köpük yapışır, duş akar, bazen sabuna
    /// basıp kayar; çok sevilince gıdıklanıp yerde yuvarlanır; boşta gezinir, tökezler, sevinçle zıplar.
    /// Şakalar rastgeledir (Gag) ve yalnızca görüntüdür: can yakmaz, ölçümlere dokunmaz, hasta dostta çıkmaz.
    /// Kuş (MoveKind.Fly) odada kanat çırpıp uçar, topu havada kovalar, yemeğe ve uykuya iner.
    /// Balık (MoveKind.Swim) yerde yürümez, suda süzülür; yemi yüzeyden düşer, topu yok kabarcık kovalar, banyosu akvaryum
    /// temizliğidir, düşüp kaymaz. Simülasyona dokunmaz, kurallar PetController'da. Konumlar tuval merkezine göre tasarım birimidir.
    /// </summary>
    public class PetStage : MonoBehaviour
    {
        /// <summary>Dostun ayaklarının yeri (SceneBuilder.Critter ile aynı).</summary>
        private const float FloorY = -430f;
        private const float MoveSpeed = 340f, ChaseSpeed = 640f;
        private const float BallLife = 11f, Gravity = 2600f;
        private const int MaxFoam = 16;

        [SerializeField] private PetView view;
        [SerializeField] private RectTransform petRoot;
        [SerializeField] private RectTransform shadow;
        [SerializeField] private RectTransform strokeArea;
        [Tooltip("Dostun önünde çizilen katman (kap, top, köpük)")]
        [SerializeField] private RectTransform front;

        /// <summary>Sıradaki yemeğin resmi (PetController besleme öncesi koyar).</summary>
        public Sprite NextFood { get; set; }

        /// <summary>Yemek, banyo gibi bir gösteri sürüyor: yeni aksiyon beklesin.</summary>
        public bool Busy => routine != null && !idleGag;
        public bool BallActive => ball != null;

        private Coroutine routine, gagRoutine;
        /// <summary>Süren gösteri kendiliğinden başladı (tökezleme, zıplama): çocuk bir şeye basınca yarıda kalır.</summary>
        private bool idleGag;
        /// <summary>Hangi şaka en son ne zaman çıktı: şans uzun süre tutmazsa çocuk yine de görsün.</summary>
        private static readonly Dictionary<string, int> sinceGag = new Dictionary<string, int>();
        private float stunned, lastStroke;
        private int strokeStreak;
        private bool ballChecked;
        private readonly List<GameObject> props = new List<GameObject>();
        private readonly List<RectTransform> foam = new List<RectTransform>();
        private float petX, goalX, speed = MoveSpeed;
        private float idleTimer = 5f, stepTime, rubTimer;
        private PetState Pet => GameSession.CurrentPet;
        private bool Swims => Pet != null && SpeciesCatalog.Get(Pet.species).moves == MoveKind.Swim;
        private bool Flies => Pet != null && SpeciesCatalog.Get(Pet.species).moves == MoveKind.Fly;
        /// <summary>Kuş havada mı (yerdeyken yürür, şaka yapar).</summary>
        private bool Airborne => Flies && hoverNow > 4f;
        private float hoverNow = 260f, hoverGoal = 260f;
        private bool flapping;
        private readonly List<RectTransform> playBubbles = new List<RectTransform>();

        // Top
        private RectTransform ball, ballShadow;
        private float ballX, ballH, ballVX, ballVH, ballAge, kickCooldown;
        private Action ballDone;

        private void Start()
        {
            if (Swims) return;
            hoverNow = hoverGoal = 0f;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (ball != null) UpdateBall(dt);
            else Idle(dt);
            MovePet(dt);
            if (Swims || Flies) MoveHover(dt);
            if (rubTimer > 0f)
            {
                rubTimer -= dt;
                if (rubTimer <= 0f) view.Lean(0f);
            }
        }

        /// <summary>Boşken ara sıra odada birkaç adım gezinir; panel açıkken ortada bekler.</summary>
        private void Idle(float dt)
        {
            if (routine != null || Pet == null) return;
            if (PanelPop.AnyOpen)
            {
                goalX = 0f;
                hoverGoal = Swims ? 260f : 0f;
                return;
            }
            if (Pet.asleep)
            {
                hoverGoal = Swims ? 90f : 0f;   // uyurken dibe iner, kuş yere konar
                return;
            }
            idleTimer -= dt;
            if (idleTimer > 0f) return;
            if (Swims)
            {
                idleTimer = UnityEngine.Random.Range(1.5f, 4f);
                speed = 170f;
                goalX = UnityEngine.Random.Range(-300f, 300f);
                hoverGoal = UnityEngine.Random.Range(140f, 500f);
                return;
            }
            if (Flies)
            {
                // Odada dolaşır: çoğu zaman havada bir yere uçar, arada konup yürür
                float flight = UnityEngine.Random.value;
                if (flight < 0.5f || Airborne)
                {
                    idleTimer = UnityEngine.Random.Range(2f, 4.5f);
                    speed = 300f;
                    goalX = UnityEngine.Random.Range(-260f, 260f);
                    hoverGoal = flight < 0.75f ? UnityEngine.Random.Range(180f, 520f) : 0f;
                    return;
                }
            }
            idleTimer = UnityEngine.Random.Range(4f, 8f);
            speed = MoveSpeed;
            float roll = UnityEngine.Random.value;
            if (CanGag && Gag("trip", 0.07f, 12))
            {
                Begin(TripRoutine());
                idleGag = true;
            }
            else if (CanGag && roll < 0.14f)
            {
                Begin(HopsRoutine());
                idleGag = true;
            }
            else if (CanGag && roll < 0.26f) view.Trick();
            else goalX = UnityEngine.Random.Range(-210f, 210f);
        }

        /// <summary>Hasta ya da kritik dost şaka yapmaz (düşmesi komik değil, üzücü olur).</summary>
        private bool CanGag => Pet != null && !Pet.asleep && !Pet.IsSick && !PetSimulator.IsCritical(Pet) && !Swims && !Airborne;

        /// <summary>Şans ya da uzun süredir çıkmadıysa true: oyun her seferinde başka bir şey yapar.</summary>
        private static bool Gag(string key, float chance, int pity)
        {
            sinceGag.TryGetValue(key, out int since);
            since++;
            bool hit = UnityEngine.Random.value < chance || since >= pity;
            sinceGag[key] = hit ? 0 : since;
            return hit;
        }

        private void MovePet(float dt)
        {
            // Dost sağdaki butonların (Merkez, Foto, Yavru) altına girmesin, soldan da ekrandan taşmasın
            float half = (Swims ? 300f : 200f) * view.VisualScale;
            goalX = Mathf.Clamp(goalX, -Mathf.Max(60f, 530f - half), Mathf.Max(40f, 340f - half));
            float before = petX;
            petX = Mathf.MoveTowards(petX, goalX, speed * dt);
            bool walking = !Mathf.Approximately(before, petX);
            if (walking)
            {
                view.SetFacing(petX > before ? 1 : -1);
                stepTime += dt;
                view.StepLift = Swims || Airborne ? 0f : Mathf.Abs(Mathf.Sin(stepTime * 9f)) * 30f;
            }
            else if (view.StepLift != 0f) view.StepLift = 0f;

            Vector2 at = petRoot.anchoredPosition;
            if (!Mathf.Approximately(at.x, petX))
            {
                petRoot.anchoredPosition = new Vector2(petX, at.y);
                Vector2 s = shadow.anchoredPosition;
                shadow.anchoredPosition = new Vector2(petX, s.y);
                Vector2 hit = strokeArea.anchoredPosition;
                strokeArea.anchoredPosition = new Vector2(petX, hit.y);
            }
        }

        /// <summary>Suda süzülme: yüksekliğe yavaşça gider, hafifçe aşağı yukarı dalgalanır; dokunma alanı da peşinden gelir.</summary>
        private void MoveHover(float dt)
        {
            hoverNow = Mathf.MoveTowards(hoverNow, hoverGoal, Mathf.Max(speed * 0.7f, 120f) * dt);
            float bob = Swims ? Mathf.Sin(Time.unscaledTime * 2f) * 14f : Airborne ? Mathf.Sin(Time.unscaledTime * 9f) * 12f : 0f;
            view.Hover = hoverNow + bob;
            strokeArea.anchoredPosition = new Vector2(strokeArea.anchoredPosition.x, -170f + view.Hover);
            if (!Flies) return;
            // Kanat çırpma: uçarken hızlı ezilip uzar; gölgesi yerde kalır, yükseldikçe küçülür
            if (Airborne && routine == null) view.Squish = Mathf.Sin(Time.unscaledTime * 26f) * 0.045f;
            else if (flapping) view.Squish = 0f;
            flapping = Airborne && routine == null;
            float small = 1f - Mathf.Clamp01(hoverNow / 800f) * 0.55f;
            shadow.localScale = new Vector3(small, small, 1f);
        }

        /// <summary>Oda değişti ya da başka bir şey başladı: gösteri biter, sahne temizlenir.</summary>
        public void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (gagRoutine != null) StopCoroutine(gagRoutine);
            gagRoutine = null;
            stunned = 0f;
            view.ResetGesture();
            foreach (GameObject prop in props)
                if (prop != null) Destroy(prop);
            props.Clear();
            foam.Clear();
            playBubbles.Clear();
            ball = ballShadow = null;
            ballDone = null;
            view.Lean(0f);
            goalX = 0f;
            speed = MoveSpeed;
            flapping = false;
            if (Flies) hoverGoal = 0f;   // yeni iş yerde başlar
        }

        /// <summary>Aksiyonun canlı gösterisini başlatır; sonunda done çağrılır. Gösterisi yoksa false.</summary>
        public bool Perform(string track, Action done)
        {
            switch (track)
            {
                case "feed" when Swims:
                    Begin(FishFeedRoutine(NextFood, done));
                    return true;
                case "play" when Swims:
                    Begin(BubblePlayRoutine(done));
                    return true;
                case "bathe" when Swims:
                    Begin(TankCleanRoutine(done));
                    return true;
                case "feed":
                    // Her seferinde aynı olmasın: kapta, havada kapma ya da kaçırıp yerden yeme
                    float pick = UnityEngine.Random.value;
                    if (NextFood == null || !CanGag || pick < 0.4f) Begin(EatRoutine(NextFood, PetArt.Get("icon_mg_bowl"), "eat", "icon_fx_sparkle", done));
                    else Begin(CatchRoutine(NextFood, pick > 0.72f, done));
                    return true;
                case "drink":
                    Begin(EatRoutine(null, PetArt.Get("icon_food_water"), "drink", "icon_fx_drop", done));
                    return true;
                case "play":
                    DropBall(done);
                    return true;
                case "bathe":
                    Begin(BathRoutine(done));
                    return true;
                case "brush":
                    Begin(BrushRoutine(done));
                    return true;
                case "toilet":
                    Begin(ToiletRoutine(done));
                    return true;
                case "stroke":
                    // Üst üste sevince (ya da bazen) gıdıklanıp yerde yuvarlanır
                    strokeStreak = Time.unscaledTime - lastStroke < 2.5f ? strokeStreak + 1 : 1;
                    lastStroke = Time.unscaledTime;
                    if (routine == null && ball == null && CanGag && (strokeStreak >= 4 || Gag("tickle", 0.15f, 8)))
                    {
                        strokeStreak = 0;
                        Begin(TickleRoutine(done));
                        return true;
                    }
                    // Dokununca sevinir; kalpleri denetleyici başının üstünde saçar
                    view.HoldPose("happy", 1.2f);
                    view.Bounce();
                    return false;
                default:
                    return false;
            }
        }

        private void Begin(IEnumerator body)
        {
            Cancel();
            idleGag = false;
            routine = StartCoroutine(Run(body));
        }

        private IEnumerator Run(IEnumerator body)
        {
            yield return body;
            routine = null;
            idleGag = false;
            idleTimer = UnityEngine.Random.Range(3f, 6f);
        }

        // ---------- Yemek ve su ----------

        private IEnumerator EatRoutine(Sprite food, Sprite bowlArt, string sound, string fx, Action done)
        {
            // Kap dostun yanına düşer, dost yürüyüp yanına gelir (ekranın ortasına doğru)
            int dir = petX > 40f ? -1 : petX < -40f ? 1 : (UnityEngine.Random.value < 0.5f ? -1 : 1);
            float gap = 170f + 150f * view.VisualScale;
            float bowlX = dir * gap * 0.55f;
            goalX = -dir * gap * 0.45f;
            speed = MoveSpeed * 1.4f;
            Image bowl = Prop(bowlArt, new Vector2(bowlX, FloorY + 520f), 220f);
            yield return Fall(bowl.rectTransform, FloorY - 26f, 0.32f);
            Sound.Play("drop", 0.7f);
            yield return Squash(bowl.rectTransform, 0.14f);
            while (!Mathf.Approximately(petX, goalX)) yield return null;
            view.SetFacing(dir);

            Image bite = null;
            if (food != null)
            {
                // Yemek havadan kaba düşer
                bite = Prop(food, new Vector2(bowlX, FloorY + 420f), 140f);
                yield return Fall(bite.rectTransform, FloorY + 34f, 0.28f);
                Sound.Play("pickup", 0.6f);
            }

            view.Lean(13f);
            yield return Wait(0.14f);
            for (int i = 0; i < 3; i++)
            {
                view.Chew(0.42f);
                Sound.Play(sound, 0.75f, UnityEngine.Random.Range(0.95f, 1.15f));
                Vector3 at = (bite != null ? bite.rectTransform : bowl.rectTransform).position;
                Fx.Burst(transform, food != null ? "icon_fx_sparkle" : fx, at, 2, 40f, true, 0.7f);
                if (bite != null)
                {
                    float s = 1f - (i + 1) / 3f;
                    bite.rectTransform.localScale = new Vector3(s, s, 1f);
                }
                yield return Wait(0.46f);
            }
            view.Lean(0f);
            view.HoldPose("happy", 1.8f);
            view.Bounce();
            done?.Invoke();
            yield return Wait(0.5f);
            yield return Vanish(bowl.rectTransform);
            goalX = 0f;
            speed = MoveSpeed;
            yield return BurpIfFull();
        }

        /// <summary>Yemek havadan gelir, dost zıplayıp ağzıyla kapar. miss: erken zıplar, kaçırır, yerden yer.</summary>
        private IEnumerator CatchRoutine(Sprite food, bool miss, Action done)
        {
            goalX = Mathf.Clamp(petX, -150f, 150f);
            speed = MoveSpeed * 1.4f;
            while (!Mathf.Approximately(petX, goalX)) yield return null;
            int dir = petX > 0f ? -1 : 1;
            view.SetFacing(dir);
            yield return Wait(0.12f);
            Sound.Play("whoosh", 0.6f);

            const float jump = 190f;
            float arrive = miss ? 0.9f : 0.72f;       // yemeğin yolu
            float jumpAt = miss ? 0.2f : 0.47f;       // zıplayışın başı: kaçırırken çok erken
            const float air = 0.5f;
            Vector2 mouth = view.Mouth;
            Vector2 target = miss
                ? new Vector2(petX + dir * 230f, FloorY + 30f)
                : new Vector2(petX + mouth.x, FloorY + mouth.y + jump);
            Vector2 start = new Vector2(target.x + dir * 300f, FloorY + 1300f);
            Image bite = Prop(food, start, 150f);
            for (float t = 0f; t < arrive; t += Time.unscaledDeltaTime)
            {
                float k = t / arrive;
                bite.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(start.x, target.x, k), Mathf.Lerp(start.y, target.y, k * k));
                bite.rectTransform.Rotate(0f, 0f, 260f * Time.unscaledDeltaTime);
                float u = Mathf.Clamp01((t - jumpAt) / air);
                view.Offset = new Vector2(0f, Mathf.Sin(u * Mathf.PI) * jump);
                view.Squish = u <= 0f && t > jumpAt - 0.14f ? 0.14f : u > 0f && u < 1f ? -0.1f : 0f;   // çömelir, uzar
                yield return null;
            }

            if (!miss)
            {
                // Hap! Havada kapar, çiğneyerek iner
                Sound.Play("eat");
                Fx.Burst(transform, "icon_fx_sparkle", bite.rectTransform.position, 4, 50f, false, 0.8f);
                Destroy(bite.gameObject);
                view.HoldPose("happy", 2.4f);
                view.Chew(0.9f);
                yield return Land(jump, arrive - jumpAt, air);
                yield return Wait(0.5f);
                view.Bounce();
                done?.Invoke();
                yield return BurpIfFull();
                goalX = 0f;
                speed = MoveSpeed;
                yield break;
            }

            // Kaçırdı: yemek yere çarpıp seker, dost şaşkın bakar, koşup yerden yer
            view.Offset = Vector2.zero;
            Sound.Play("drop", 0.8f);
            Squashes(bite.rectTransform);
            float groundY = target.y;
            for (float t = 0f; t < 0.34f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.34f;
                bite.rectTransform.anchoredPosition = new Vector2(target.x + dir * 24f * k, groundY + Mathf.Sin(k * Mathf.PI) * 70f);
                yield return null;
            }
            view.Squish = 0f;
            view.Wiggle(0.3f);
            yield return Wait(0.4f);
            goalX = Mathf.Clamp(target.x - dir * (120f + 90f * view.VisualScale), -340f, 340f);
            speed = MoveSpeed * 1.8f;
            while (!Mathf.Approximately(petX, goalX)) yield return null;
            view.SetFacing(dir);
            view.Lean(13f);
            yield return Wait(0.14f);
            for (int i = 0; i < 3; i++)
            {
                view.Chew(0.38f);
                Sound.Play("eat", 0.75f, UnityEngine.Random.Range(0.95f, 1.15f));
                float s = 1f - (i + 1) / 3f;
                bite.rectTransform.localScale = new Vector3(s, s, 1f);
                yield return Wait(0.4f);
            }
            view.Lean(0f);
            view.HoldPose("happy", 1.8f);
            view.Bounce();
            done?.Invoke();
            yield return BurpIfFull();
            goalX = 0f;
            speed = MoveSpeed;
        }

        /// <summary>Havadan iner: yere değince ezilir, biraz sallanır.</summary>
        private IEnumerator Land(float jump, float passed, float air)
        {
            for (float t = Mathf.Min(passed, air); t < air; t += Time.unscaledDeltaTime)
            {
                view.Offset = new Vector2(0f, Mathf.Sin(t / air * Mathf.PI) * jump);
                view.Squish = -0.08f;
                yield return null;
            }
            view.Offset = Vector2.zero;
            Sound.Play("drop", 0.5f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.22f)
            {
                view.Squish = Mathf.Sin(t * Mathf.PI) * 0.2f;
                yield return null;
            }
            view.Squish = 0f;
        }

        /// <summary>Karnı tokken yiyince şişer ve geğirir.</summary>
        private IEnumerator BurpIfFull()
        {
            if (Pet == null || Pet.fullness < 90.0) yield break;
            yield return Wait(0.35f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.55f)
            {
                view.Squish = t * 0.24f;
                yield return null;
            }
            yield return Wait(0.16f);
            Sound.Play("bubble", 1f, 0.5f);
            Vector2 mouth = view.Mouth;
            Vector3 at = petRoot.position + new Vector3(mouth.x, mouth.y, 0f) * transform.lossyScale.y;
            Fx.Burst(transform, "icon_fx_bubble", at, 3, 76f, true, 0.6f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
            {
                view.Squish = Mathf.Lerp(0.24f, 0f, t) - Mathf.Sin(t * Mathf.PI) * 0.12f;
                yield return null;
            }
            view.Squish = 0f;
        }

        // ---------- Top ----------

        private void DropBall(Action done)
        {
            Cancel();
            ballDone = done;
            Image ballShadowImage = Prop(shadow.GetComponent<Image>().sprite, new Vector2(petX, FloorY - 60f), 150f);
            ballShadowImage.preserveAspect = false;
            ballShadowImage.color = new Color(0f, 0f, 0f, 0.16f);
            ballShadowImage.rectTransform.sizeDelta = new Vector2(130f, 34f);
            ballShadow = ballShadowImage.rectTransform;

            Image image = Prop(PetArt.Get("icon_act_play"), Vector2.zero, 170f);
            image.raycastTarget = true;
            image.gameObject.AddComponent<BallTap>().Init(this);
            ball = image.rectTransform;
            int side = petX > 0f ? -1 : 1;
            ballX = petX + side * 210f;
            ballH = 1250f;
            ballVX = -side * 90f;
            ballVH = 0f;
            ballAge = 0f;
            kickCooldown = 0.2f;
            ballChecked = false;
            Sound.Play("pickup", 0.6f);
        }

        /// <summary>Top düğmesi: top yeniden sekip uçar.</summary>
        public void KickBall()
        {
            if (ball == null) return;
            ballVH = UnityEngine.Random.Range(1300f, 1600f);
            ballVX = UnityEngine.Random.Range(-620f, 620f);
            ballAge = 0f;
            Sound.Play("bounce", 0.8f);
        }

        /// <summary>Çocuk topa dokundu: parmağın ters yönüne sıçrar.</summary>
        public void TapBall(Vector2 screenPos)
        {
            if (ball == null) return;
            float away = ball.position.x >= screenPos.x ? 1f : -1f;
            ballVH = UnityEngine.Random.Range(1400f, 1750f);
            ballVX = away * UnityEngine.Random.Range(320f, 760f);
            ballAge = 0f;
            Fx.Burst(transform, "icon_fx_star", ball.position, 2, 46f, false, 0.7f);
            Sound.Play("bounce");
        }

        private void UpdateBall(float dt)
        {
            ballAge += dt;
            kickCooldown -= dt;
            ballVH -= Gravity * dt;
            ballH += ballVH * dt;
            ballX += ballVX * dt;
            if (ballH <= 0f)
            {
                ballH = 0f;
                if (ballVH < -320f) Sound.Play("bounce", Mathf.Clamp01(-ballVH / 1600f) * 0.8f);
                ballVH = ballVH < -140f ? -ballVH * 0.66f : 0f;
                ballVX *= 0.86f;
            }
            if (ballH <= 0f) ballVX *= Mathf.Exp(-1.3f * dt);
            if (Mathf.Abs(ballX) > 450f)
            {
                ballX = Mathf.Sign(ballX) * 450f;
                ballVX = -ballVX * 0.8f;
            }
            ball.anchoredPosition = new Vector2(ballX, FloorY + 60f + ballH);
            ball.Rotate(0f, 0f, -ballVX * dt * 0.7f);
            float air = Mathf.Clamp01(1f - ballH / 1100f);
            ballShadow.anchoredPosition = new Vector2(ballX, FloorY - 26f);
            ballShadow.localScale = new Vector3(0.45f + air * 0.75f, 0.45f + air * 0.75f, 1f);

            // Dost topun peşinde (başı dönüyorsa bekler)
            if (ballVH > 0f) ballChecked = false;
            if (stunned > 0f)
            {
                stunned -= dt;
                goalX = petX;
                FinishBall();
                return;
            }
            speed = ChaseSpeed;
            goalX = Mathf.Clamp(ballX, -340f, 340f);
            if (Flies) hoverGoal = Mathf.Clamp(ballH * 0.7f, 0f, 560f);   // kuş topu havada kovalar

            // Top kafasına düşerse bazen çarpar: pat! Başı döner
            Vector2 head = view.HeadTop;
            if (!ballChecked && CanGag && kickCooldown <= 0f && ballVH < 0f && ballH < head.y + 30f && ballH > head.y - 130f
                && Mathf.Abs(ballX - (petX + head.x)) < 120f)
            {
                ballChecked = true;
                if (Gag("bonk", 0.5f, 2))
                {
                    Bonk();
                    FinishBall();
                    return;
                }
            }

            // Yetişince burnuyla vurur (bazen takla atarak)
            float reach = 110f + 120f * view.VisualScale;
            if (kickCooldown <= 0f && ballH < 300f + hoverNow && ballH > hoverNow - 80f && Mathf.Abs(ballX - petX) < reach)
            {
                kickCooldown = 0.75f;
                float dir = Mathf.Abs(ballX) > 300f ? -Mathf.Sign(ballX) : (UnityEngine.Random.value < 0.5f ? -1f : 1f);
                ballVH = UnityEngine.Random.Range(1050f, 1450f);
                ballVX = dir * UnityEngine.Random.Range(240f, 620f);
                view.Bounce();
                view.HoldPose("happy", 1f);
                view.SetFacing(ballX >= petX ? 1 : -1);
                Fx.Burst(transform, "icon_fx_star", ball.position, 2, 46f, false, 0.7f);
                Sound.Play("bounce");
                if (gagRoutine == null && CanGag && Gag("flip", 0.3f, 4)) gagRoutine = StartCoroutine(FlipRoutine());
                if (ballDone != null)
                {
                    ballDone();
                    ballDone = null;
                }
            }
            FinishBall();
        }

        private void Bonk()
        {
            ballVH = UnityEngine.Random.Range(1250f, 1500f);
            ballVX = (ballX >= petX ? 1f : -1f) * UnityEngine.Random.Range(420f, 720f);
            kickCooldown = 2f;
            stunned = 1.5f;
            Sound.Play("bounce");
            if (ballDone != null)
            {
                ballDone();
                ballDone = null;
            }
            if (gagRoutine != null) StopCoroutine(gagRoutine);
            gagRoutine = StartCoroutine(DizzyRoutine(1.5f));
        }

        /// <summary>Başının üstünde dönen yıldızlar; sendeler, sonra kendine gelip silkelenir.</summary>
        private IEnumerator DizzyRoutine(float seconds)
        {
            view.ResetGesture();
            var stars = new Image[3];
            for (int i = 0; i < stars.Length; i++) stars[i] = Prop(PetArt.Get("icon_fx_star"), Vector2.zero, 74f);
            view.HoldPose("normal", seconds + 0.3f);
            Sound.Play("sparkle", 0.6f);
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float hit = Mathf.Clamp01(1f - t / 0.24f);
                view.Squish = hit * 0.28f;
                view.Tilt = Mathf.Sin(t * 6f) * 9f * Mathf.Clamp01(t * 3f);
                Vector2 head = view.HeadTop;
                for (int i = 0; i < stars.Length; i++)
                {
                    float angle = t * 7f + i * Mathf.PI * 2f / stars.Length;
                    stars[i].rectTransform.anchoredPosition = new Vector2(petX + head.x + Mathf.Cos(angle) * 130f * Mathf.Max(0.6f, view.VisualScale),
                        FloorY + head.y + 26f + Mathf.Sin(angle) * 34f);
                    float depth = 0.75f + Mathf.Sin(angle) * 0.25f;
                    stars[i].rectTransform.localScale = new Vector3(depth, depth, 1f);
                }
                yield return null;
            }
            foreach (Image star in stars)
                if (star != null) StartCoroutine(Vanish(star.rectTransform));
            view.ResetGesture();
            view.Wiggle(0.35f);
            gagRoutine = null;
        }

        /// <summary>Topa vururken havada takla.</summary>
        private IEnumerator FlipRoutine()
        {
            float direction = view.HeadTop.x >= 0f ? -1f : 1f;
            Sound.Play("whoosh", 0.6f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.62f)
            {
                view.Spin = direction * 360f * Mathf.SmoothStep(0f, 1f, t);
                view.Offset = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 210f);
                yield return null;
            }
            view.ResetGesture();
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.18f)
            {
                view.Squish = Mathf.Sin(t * Mathf.PI) * 0.18f;
                yield return null;
            }
            view.Squish = 0f;
            gagRoutine = null;
        }

        /// <summary>Topun süresi doldu mu: oyun biter, top küçülüp kaybolur.</summary>
        private void FinishBall()
        {
            if (ballAge < BallLife) return;
            // Oyun bitti: top küçülüp kaybolur, dost ortaya döner
            ballDone?.Invoke();
            StartCoroutine(Vanish(ball));
            if (ballShadow != null) Destroy(ballShadow.gameObject);
            ball = ballShadow = null;
            ballDone = null;
            speed = MoveSpeed;
            goalX = 0f;
            if (Flies) hoverGoal = 0f;
        }

        // ---------- Banyo ----------

        /// <summary>Parmak dostun üstünde gezerken: banyoda köpük yapışır, diğer odalarda keyiften gözü kapanır.</summary>
        public void Rubbing(Vector3 screenPos, bool bath)
        {
            if (bath)
            {
                if (foam.Count < MaxFoam && routine == null) AddFoam(screenPos);
                return;
            }
            view.HoldPose("happy", 0.7f);
            view.Lean((screenPos.x - petRoot.position.x) / Mathf.Max(1f, Screen.width) * 22f);
            rubTimer = 0.25f;
        }

        private void AddFoam(Vector3 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(front, screenPos, null, out Vector2 local);
            foreach (RectTransform existing in foam)
                if (existing != null && (existing.anchoredPosition - local).sqrMagnitude < 3600f) return;
            Image bubble = Prop(PetArt.Get("icon_fx_bubble"), local, UnityEngine.Random.Range(90f, 150f));
            foam.Add(bubble.rectTransform);
            StartCoroutine(Pop(bubble.rectTransform));
            Sound.Play("bubble", 0.4f, UnityEngine.Random.Range(0.9f, 1.3f));
        }

        private IEnumerator BathRoutine(Action done)
        {
            // Dostu köpük kaplar
            float height = 480f * view.VisualScale;
            for (int i = 0; i < 9; i++)
            {
                Vector2 at = new Vector2(petX + UnityEngine.Random.Range(-170f, 170f) * view.VisualScale,
                    FloorY + UnityEngine.Random.Range(40f, height));
                Image bubble = Prop(PetArt.Get("icon_fx_bubble"), at, UnityEngine.Random.Range(90f, 150f));
                foam.Add(bubble.rectTransform);
                StartCoroutine(Pop(bubble.rectTransform));
                Sound.Play("bubble", 0.4f, UnityEngine.Random.Range(0.9f, 1.3f));
                yield return Wait(0.07f);
            }
            yield return Wait(0.25f);

            // Duş: yukarıdan su akar, köpükler teker teker söner
            Sound.Play("splash");
            Vector3 top = petRoot.position + Vector3.up * (height + 120f) * transform.lossyScale.y;
            float width = 200f * view.VisualScale * transform.lossyScale.x;
            for (float t = 0f; t < 1.1f; t += 0.07f)
            {
                Vector3 at = top + Vector3.right * UnityEngine.Random.Range(-width, width);
                Fx.Burst(transform, "icon_fx_drop", at, 1, 52f, false, 0.9f);
                if (foam.Count > 0 && UnityEngine.Random.value < 0.8f)
                {
                    RectTransform last = foam[foam.Count - 1];
                    foam.RemoveAt(foam.Count - 1);
                    if (last != null) StartCoroutine(Vanish(last));
                }
                yield return Wait(0.07f);
            }
            foreach (RectTransform rest in foam)
                if (rest != null) StartCoroutine(Vanish(rest));
            foam.Clear();

            // Silkelenir, damlalar saçılır, pırıl pırıl
            view.Wiggle(0.6f);
            Vector3 middle = petRoot.position + Vector3.up * (height * 0.5f) * transform.lossyScale.y;
            Fx.Burst(transform, "icon_fx_drop", middle, 10, 50f, false, 1f);
            yield return Wait(0.6f);
            if (CanGag && Gag("slip", 0.5f, 2)) yield return SoapSlip();
            view.HoldPose("happy", 1.8f);
            view.Bounce();
            done?.Invoke();
        }

        /// <summary>Yerdeki sabuna basar, kayıp sırtüstü düşer; sabun fırlar. Gülerek kalkar.</summary>
        private IEnumerator SoapSlip()
        {
            int dir = petX > 0f ? -1 : 1;
            float soapX = petX + dir * 170f;
            Image soap = Prop(PetArt.Get("icon_act_bathe"), new Vector2(soapX, FloorY + 300f), 130f);
            yield return Fall(soap.rectTransform, FloorY + 10f, 0.2f);
            Sound.Play("drop", 0.5f);
            yield return Wait(0.2f);
            goalX = soapX;
            speed = MoveSpeed;
            while (!Mathf.Approximately(petX, goalX)) yield return null;

            // Kayma: dost ileri fırlar, geriye devrilir, sabun ters yöne uçar
            Sound.Play("whoosh");
            view.HoldPose("happy", 2.6f);
            goalX = Mathf.Clamp(petX + dir * 250f, -340f, 340f);
            speed = 900f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
            {
                view.Tilt = dir * 88f * t * t;
                Vector2 s = soap.rectTransform.anchoredPosition;
                soap.rectTransform.anchoredPosition = new Vector2(s.x - dir * 1800f * Time.unscaledDeltaTime, s.y + Mathf.Cos(t * Mathf.PI) * 500f * Time.unscaledDeltaTime);
                soap.rectTransform.Rotate(0f, 0f, 900f * Time.unscaledDeltaTime);
                yield return null;
            }
            // Pat! Yere çarpar: damlalar ve yıldızlar; sabun gözden kaybolur
            StartCoroutine(Vanish(soap.rectTransform));
            view.Tilt = dir * 88f;
            Sound.Play("drop");
            Sound.Play("splash", 0.5f);
            Vector3 impact = petRoot.position + Vector3.up * 60f * transform.lossyScale.y;
            Fx.Burst(transform, "icon_fx_drop", impact, 8, 50f, true, 0.9f);
            Fx.Burst(transform, "icon_fx_star", impact, 4, 56f, true, 0.9f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.24f)
            {
                view.Tilt = dir * (88f - Mathf.Sin(t * Mathf.PI) * 12f);
                yield return null;
            }
            yield return Wait(0.5f);

            // Silkinip ayağa kalkar
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
            {
                view.Tilt = dir * 88f * (1f - t * t);
                view.Offset = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 70f);
                yield return null;
            }
            view.ResetGesture();
            view.Wiggle(0.3f);
            Sound.PetVoice(Pet.species, 0.7f);
            yield return Wait(0.3f);
            goalX = 0f;
            speed = MoveSpeed;
        }

        // ---------- Balık ----------

        /// <summary>Yem yüzeyden serpilir, yavaşça batar; balık tek tek yetişip yer.</summary>
        private IEnumerator FishFeedRoutine(Sprite food, Action done)
        {
            speed = 420f;
            var bits = new List<RectTransform>();
            for (int i = 0; i < 5; i++)
            {
                Image bit = Prop(food, new Vector2(UnityEngine.Random.Range(-330f, 330f), UnityEngine.Random.Range(520f, 680f)), 80f);
                bits.Add(bit.rectTransform);
            }
            Sound.Play("drop", 0.4f, 1.4f);
            for (float t = 0f; t < 9f && bits.Count > 0; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                RectTransform nearest = null;
                float best = float.MaxValue;
                foreach (RectTransform bit in bits)
                {
                    Vector2 p = bit.anchoredPosition;
                    p.y = Mathf.Max(FloorY + 30f, p.y - 150f * dt);
                    p.x += Mathf.Sin(Time.unscaledTime * 2f + p.y * 0.02f) * 20f * dt;
                    bit.anchoredPosition = p;
                    float d = Mathf.Abs(p.x - petX) + Mathf.Abs(p.y - (FloorY + hoverNow + 220f * view.VisualScale));
                    if (d < best)
                    {
                        best = d;
                        nearest = bit;
                    }
                }
                goalX = Mathf.Clamp(nearest.anchoredPosition.x, -360f, 360f);
                hoverGoal = Mathf.Clamp(nearest.anchoredPosition.y - FloorY - 220f * view.VisualScale, 60f, 640f);
                if (best < 150f)
                {
                    Sound.Play("eat", 0.75f, UnityEngine.Random.Range(0.95f, 1.2f));
                    Fx.Burst(transform, "icon_fx_bubble", nearest.position, 2, 40f, true, 0.7f);
                    view.Chew(0.3f);
                    bits.Remove(nearest);
                    props.Remove(nearest.gameObject);
                    Destroy(nearest.gameObject);
                }
                yield return null;
            }
            foreach (RectTransform bit in bits)
                if (bit != null) StartCoroutine(Vanish(bit));
            view.HoldPose("happy", 1.8f);
            view.Trick();
            done?.Invoke();
            yield return BurpIfFull();
            speed = 170f;
        }

        /// <summary>Kabarcıklar yükselir; balık kovalar, çocuk da dokunup patlatabilir.</summary>
        private IEnumerator BubblePlayRoutine(Action done)
        {
            speed = 460f;
            Action first = done;
            Sound.Play("bubble");
            for (int i = 0; i < 9; i++)
            {
                Image bubble = Prop(PetArt.Get("icon_fx_bubble"), new Vector2(UnityEngine.Random.Range(-380f, 380f), FloorY - 40f - i * 130f), UnityEngine.Random.Range(110f, 170f));
                bubble.raycastTarget = true;
                bubble.gameObject.AddComponent<BubbleTap>().Init(this);
                playBubbles.Add(bubble.rectTransform);
            }
            for (float t = 0f; t < 10f && playBubbles.Count > 0; t += Time.unscaledDeltaTime)
            {
                float dt = Time.unscaledDeltaTime;
                RectTransform nearest = null;
                float best = float.MaxValue;
                for (int i = playBubbles.Count - 1; i >= 0; i--)
                {
                    RectTransform b = playBubbles[i];
                    Vector2 p = b.anchoredPosition;
                    p.y += 150f * dt;
                    p.x += Mathf.Sin(Time.unscaledTime * 2.4f + i) * 36f * dt;
                    b.anchoredPosition = p;
                    if (p.y > 640f)
                    {
                        playBubbles.RemoveAt(i);
                        props.Remove(b.gameObject);
                        Destroy(b.gameObject);
                        continue;
                    }
                    if (p.y < FloorY) continue;
                    float d = Mathf.Abs(p.x - petX) + Mathf.Abs(p.y - (FloorY + hoverNow + 220f * view.VisualScale));
                    if (d < best)
                    {
                        best = d;
                        nearest = b;
                    }
                }
                if (nearest != null)
                {
                    goalX = Mathf.Clamp(nearest.anchoredPosition.x, -360f, 360f);
                    hoverGoal = Mathf.Clamp(nearest.anchoredPosition.y - FloorY - 220f * view.VisualScale, 60f, 640f);
                    if (best < 150f)
                    {
                        PopBubble(nearest);
                        view.Bounce();
                        view.HoldPose("happy", 1f);
                        first?.Invoke();
                        first = null;
                    }
                }
                yield return null;
            }
            first?.Invoke();
            view.HoldPose("happy", 1.4f);
            speed = 170f;
        }

        /// <summary>Kabarcık patlar (balık ya da çocuk).</summary>
        public void PopBubble(RectTransform bubble)
        {
            if (bubble == null || !playBubbles.Remove(bubble)) return;
            Sound.Play("bubble", 0.8f, UnityEngine.Random.Range(1f, 1.4f));
            Fx.Burst(transform, "icon_fx_sparkle", bubble.position, 3, 50f, false, 0.8f);
            props.Remove(bubble.gameObject);
            Destroy(bubble.gameObject);
        }

        /// <summary>Akvaryum temizliği: sünger camı siler, kabarcıklar çıkar, su berraklaşır.</summary>
        private IEnumerator TankCleanRoutine(Action done)
        {
            goalX = 0f;
            hoverGoal = 120f;
            Image sponge = Prop(PetArt.Get("icon_act_clean"), new Vector2(-400f, 500f), 210f);
            for (int stroke = 0; stroke < 3; stroke++)
            {
                float y = 500f - stroke * 280f;
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.42f)
                {
                    float x = Mathf.Lerp(-400f, 400f, stroke % 2 == 0 ? t : 1f - t);
                    sponge.rectTransform.anchoredPosition = new Vector2(x, y + Mathf.Sin(t * Mathf.PI * 3f) * 30f);
                    yield return null;
                }
                Fx.Burst(transform, "icon_fx_bubble", sponge.rectTransform.position, 6, 60f, true, 0.6f);
                Sound.Play("sweep", 0.6f);
            }
            yield return Vanish(sponge.rectTransform);
            Sound.Play("sparkle");
            view.HoldPose("happy", 1.8f);
            view.Trick();
            done?.Invoke();
            speed = 170f;
        }

        // ---------- Şakalar ----------

        /// <summary>Gıdıklanır: yana devrilir, debelenir, gülerek yuvarlanıp kalkar.</summary>
        private IEnumerator TickleRoutine(Action done)
        {
            goalX = petX;
            int dir = Mathf.Abs(petX) > 60f ? (petX > 0f ? 1 : -1) : (UnityEngine.Random.value < 0.5f ? -1 : 1);
            view.HoldPose("happy", 2.6f);
            Sound.PetVoice(Pet.species);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.16f)
            {
                view.Tilt = dir * 86f * t;
                yield return null;
            }
            view.Tilt = dir * 86f;
            Sound.Play("drop", 0.5f);
            done?.Invoke();
            view.Wiggle(1.1f);
            for (float t = 0f; t < 1.1f; t += 0.28f)
            {
                Vector3 at = petRoot.position + new Vector3(UnityEngine.Random.Range(-120f, 120f), 120f, 0f) * transform.lossyScale.y;
                Fx.Burst(transform, t % 0.56f < 0.28f ? "icon_fx_heart" : "icon_fx_note", at, 1, 66f, true, 0.8f);
                yield return Wait(0.28f);
            }
            // Yerde bir tur yuvarlanıp ayağa kalkar
            float target = Mathf.Clamp(petX + dir * -170f, -300f, 300f);
            goalX = target;
            speed = 620f;
            Sound.Play("whoosh", 0.6f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
            {
                view.Tilt = dir * 86f * (1f - t);
                view.Spin = dir * 360f * t;
                view.Offset = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 40f);
                yield return null;
            }
            view.ResetGesture();
            view.Bounce();
            speed = MoveSpeed;
            goalX = 0f;
        }

        /// <summary>Koşarken tökezler: yüzüstü kayar, toz kalkar, gülerek kalkar.</summary>
        private IEnumerator TripRoutine()
        {
            int dir = petX > 0f ? -1 : 1;
            speed = 560f;
            // Ekranın bir kenarından ortaya doğru koşar: başı yere değdiğinde ekranın içinde kalır
            goalX = -dir * 300f;
            while (!Mathf.Approximately(petX, goalX)) yield return null;
            goalX = -dir * 60f;
            yield return Wait(0.28f);
            Sound.Play("whoosh", 0.7f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.2f)
            {
                view.Tilt = -dir * 84f * t * t;
                yield return null;
            }
            view.Tilt = -dir * 84f;
            Sound.Play("drop", 0.7f);
            Vector3 impact = petRoot.position + Vector3.up * 40f * transform.lossyScale.y;
            Fx.Burst(transform, "icon_fx_dust", impact, 5, 70f, true, 0.7f);
            Fx.Burst(transform, "icon_fx_star", impact, 3, 50f, true, 0.8f);
            goalX = petX;
            yield return Wait(0.5f);
            view.HoldPose("happy", 1.6f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
            {
                view.Tilt = -dir * 84f * (1f - t * t);
                view.Offset = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 60f);
                yield return null;
            }
            view.ResetGesture();
            view.Wiggle(0.3f);
            Sound.PetVoice(Pet.species, 0.7f);
            yield return Wait(0.3f);
            goalX = 0f;
            speed = MoveSpeed;
        }

        /// <summary>Keyfinden üst üste zıplar.</summary>
        private IEnumerator HopsRoutine()
        {
            goalX = petX;
            view.HoldPose("happy", 1.6f);
            for (int i = 0; i < 3; i++)
            {
                Sound.Play("bounce", 0.4f, 1.2f);
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.34f)
                {
                    view.Offset = new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 110f);
                    view.Squish = t < 0.12f || t > 0.88f ? 0.12f : -0.06f;
                    yield return null;
                }
            }
            view.ResetGesture();
        }

        private IEnumerator BrushRoutine(Action done)
        {
            Image brush = Prop(PetArt.Get("icon_act_brush"), new Vector2(petX - 220f, FloorY + 300f), 200f);
            for (int stroke = 0; stroke < 3; stroke++)
            {
                float y = FloorY + 400f * view.VisualScale - stroke * 90f * view.VisualScale;
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.34f)
                {
                    float x = Mathf.Lerp(-190f, 190f, stroke % 2 == 0 ? t : 1f - t);
                    brush.rectTransform.anchoredPosition = new Vector2(petX + x, y + Mathf.Sin(t * Mathf.PI) * 26f);
                    yield return null;
                }
                Fx.Burst(transform, "icon_fx_sparkle", brush.rectTransform.position, 3, 54f, false, 0.7f);
                Sound.Play("bubble", 0.4f, 1.4f);
            }
            yield return Vanish(brush.rectTransform);
            view.HoldPose("happy", 1.6f);
            view.Bounce();
            done?.Invoke();
        }

        private IEnumerator ToiletRoutine(Action done)
        {
            // Kum kabı önüne gelir, dost üstüne çıkar, işi bitince kap gider
            Image box = Prop(PetArt.Get("icon_act_toilet"), new Vector2(petX, FloorY + 480f), 300f);
            yield return Fall(box.rectTransform, FloorY - 8f, 0.3f);
            Sound.Play("drop", 0.7f);
            view.Chew(0.9f);
            yield return Wait(0.95f);
            Fx.Burst(transform, "icon_fx_sparkle", box.rectTransform.position, 5, 56f, false, 0.7f);
            yield return Vanish(box.rectTransform);
            view.HoldPose("happy", 1.4f);
            view.Bounce();
            done?.Invoke();
        }

        // ---------- Parçalar ----------

        private Image Prop(Sprite sprite, Vector2 at, float size)
        {
            var go = new GameObject("Prop", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(front, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = at;
            rt.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            props.Add(go);
            return image;
        }

        private static IEnumerator Fall(RectTransform rt, float groundY, float seconds)
        {
            float startY = rt.anchoredPosition.y;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                Vector2 p = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(p.x, Mathf.Lerp(startY, groundY, t * t));
                yield return null;
            }
            Vector2 end = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(end.x, groundY);
        }

        private void Squashes(RectTransform rt) => StartCoroutine(Squash(rt, 0.14f));

        private static IEnumerator Squash(RectTransform rt, float seconds)
        {
            for (float t = 0f; t < 1f && rt != null; t += Time.unscaledDeltaTime / seconds)
            {
                float k = Mathf.Sin(t * Mathf.PI) * 0.22f;
                rt.localScale = new Vector3(1f + k, 1f - k, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private static IEnumerator Pop(RectTransform rt)
        {
            for (float t = 0f; t < 1f && rt != null; t += Time.unscaledDeltaTime / 0.16f)
            {
                float s = Mathf.Sin(t * Mathf.PI * 0.75f) / 0.707f;
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private static IEnumerator Vanish(RectTransform rt)
        {
            for (float t = 0f; t < 1f && rt != null; t += Time.unscaledDeltaTime / 0.2f)
            {
                float s = 1f - t;
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }
    }

    /// <summary>Oyun kabarcığına dokunuşu sahneye iletir.</summary>
    public class BubbleTap : MonoBehaviour, IPointerDownHandler
    {
        private PetStage stage;

        public void Init(PetStage owner) => stage = owner;

        public void OnPointerDown(PointerEventData eventData) => stage.PopBubble((RectTransform)transform);
    }

    /// <summary>Topa dokunuşu sahneye iletir.</summary>
    public class BallTap : MonoBehaviour, IPointerDownHandler
    {
        private PetStage stage;

        public void Init(PetStage owner) => stage = owner;

        public void OnPointerDown(PointerEventData eventData) => stage.TapBall(eventData.position);
    }
}
