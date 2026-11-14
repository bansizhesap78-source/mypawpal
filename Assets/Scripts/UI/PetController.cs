using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Dostun odası: üstte adı, yaşı ve 9 ölçümün çubukları; ortada dostun kendisi, ruh hali balonu ve yerdeki
    /// kaka; altta oda sekmeleri (Mutfak, Banyo, Yatak, Oyun) ve seçili odanın aksiyon butonları.
    /// Zamanı GameSession işletir; burası yalnızca gösterir ve aksiyonları uygular.
    /// </summary>
    public class PetController : MonoBehaviour
    {
        public const string PetScene = "Pet";

        public enum Room { Kitchen, Bath, Bed, Play, School, Closet, Vet }

        /// <summary>Oda çizimlerinin anahtarı: Assets/Art/Rooms/room_&lt;anahtar&gt;.png.</summary>
        public static readonly string[] RoomKeys = { "kitchen", "bath", "bed", "play", "school", "closet", "vet" };

        /// <summary>Durum panelindeki ölçümler (statFills sırası).</summary>
        public static readonly string[] StatKeys =
        {
            "stat_fullness", "stat_thirst", "stat_energy", "stat_hygiene", "stat_toilet",
            "stat_fun", "stat_health", "stat_love", "stat_happiness"
        };

        /// <summary>
        /// Ekrandaki ikon halkaları (StatKeys sırasındaki indeksler): tokluk, su, enerji, temizlik, eğlence, sağlık.
        /// Tuvalet balondan, sevgi ve mutluluk dostun hâlinden anlaşılır.
        /// </summary>
        public static readonly int[] RingStats = { 0, 1, 2, 3, 5, 6 };

        /// <summary>RingStats sırasıyla halkaların ikonları (Assets/Art/Icons).</summary>
        public static readonly string[] RingIcons = { "stat_hunger", "stat_thirst", "stat_energy", "stat_clean", "stat_fun", "stat_health" };

        private const float RefreshInterval = 0.25f;
        /// <summary>Yetişkin dostun yerden başının üstüne kadar boyu (px); balon bunun üstünde durur.</summary>
        private const float PetHeight = 600f;

        [SerializeField] private PetView view;
        [SerializeField] private Text nameText;
        [SerializeField] private Text infoText;
        [SerializeField] private Text weightText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text diamondText;
        [SerializeField] private Image[] statFills;
        [Tooltip("İkon halkaları (azalınca nabız gibi atar); boşsa eski çubuklar")]
        [SerializeField] private RectTransform[] statRings;

        [SerializeField] private RectTransform bubble;
        [SerializeField] private Text bubbleText;
        [SerializeField] private Image bubbleIcon;
        [Tooltip("Uyurken odayı karartan katman")]
        [SerializeField] private Image night;
        [Tooltip("Fiyat rozetlerinin yazıları (rozet = yazının ebeveyni)")]
        [SerializeField] private Text[] foodPrices;
        [SerializeField] private Text treatPrice;
        [SerializeField] private Text vaccinePrice;
        [SerializeField] private Text miraclePrice;

        [Header("Yazısız ekran")]
        [Tooltip("Geri bildirim yazı yerine resimle (ikon çizimleri varken)")]
        [SerializeField] private bool iconFeedback;
        [Tooltip("Durum satırı yerine küçük ikonlar: aşılı, hasta, pansiyonda, etkinlik")]
        [SerializeField] private GameObject[] statusIcons;
        [Tooltip("Rehber el ve gösterdiği butonlar")]
        [SerializeField] private Image guideHand;
        [Tooltip("Rehber elin gösterdiği butonun etrafında nabız gibi atan halka")]
        [SerializeField] private Image guideRing;
        [SerializeField] private RectTransform batheButton;
        [SerializeField] private RectTransform playButton;
        [SerializeField] private RectTransform petArea;
        [SerializeField] private Text feedbackText;
        [SerializeField] private GameObject poop;

        [SerializeField] private Image roomBackground;
        [SerializeField] private AspectRatioFitter roomFitter;
        [SerializeField] private Image floor;
        [SerializeField] private Color[] roomColors;
        [SerializeField] private GameObject[] roomPanels;
        [SerializeField] private Image[] tabFaces;
        [SerializeField] private Text sleepLabel;
        [SerializeField] private Image sleepIcon;
        [SerializeField] private GameObject cleanButton;
        [SerializeField] private Image vetPanda;

        [Header("Türe göre butonlar")]
        [SerializeField] private GameObject[] foodButtons;
        [SerializeField] private Text[] foodLabels;
        [SerializeField] private Image[] foodIcons;
        [SerializeField] private GameObject waterButton;
        [SerializeField] private GameObject brushButton;
        [SerializeField] private GameObject toiletButton;
        [SerializeField] private Text[] commandLabels;
        [Tooltip("Yazısız okul: komutun öğrenme çubuğu ve öğrenince yıldız")]
        [SerializeField] private Image[] commandBars;
        [SerializeField] private GameObject[] commandStars;
        [Tooltip("Okul düğmelerinin ikonları: her komutun kendi resmi (yazısız ayırt edilsin)")]
        [SerializeField] private Image[] commandIcons;

        [Header("Paneller")]
        [SerializeField] private WardrobePanel wardrobe;
        [SerializeField] private WalkPanel walk;
        [SerializeField] private GameObject breedButton;
        [SerializeField] private GameObject walkButton;
        [Tooltip("Dostun canlı hareketleri: yemek, top, banyo, gezinme")]
        [SerializeField] private PetStage stage;
        [SerializeField] private MiniGameMenu games;
        [SerializeField] private SimonPanel simon;
        [SerializeField] private PhotoPanel photo;
        [SerializeField] private BreedPanel breed;
        [SerializeField] private Text hubBadge;

        [Header("Veteriner ve kritik uyarı")]
        [SerializeField] private Text treatLabel;
        [SerializeField] private Text vaccineLabel;
        [SerializeField] private Text miracleLabel;
        [SerializeField] private GameObject miracleButton;
        [SerializeField] private GameObject criticalBanner;
        [SerializeField] private Text criticalText;

        private PetState pet;
        private Species species;
        private Room room;
        // Merkezden, mağazadan ya da mini oyundan dönünce çocuk kaldığı odaya döner (uygulama açılışında mutfak)
        private static Room lastRoom = Room.Kitchen;
        private float refreshTimer;
        private float vetPoseTimer;
        private float[] ringValues = new float[0];
        private float ambientTimer, rubDistance, rubSpawn;
        private Vector2 bubbleHome;
        /// <summary>Dokunulmadan geçen bu süreden sonra rehber el çıkar (sn).</summary>
        private const float GuideDelay = 2.5f;
        private float idleTime;
        private Sprite guidePoint, guideTap;
        /// <summary>El çizimlerinde parmak ucunun yeri (icon_hand_point / icon_hand_tap; tools ile ölçüldü).</summary>
        private static readonly Vector2 PointTip = new Vector2(0.41f, 0.97f), TapTip = new Vector2(0.48f, 0.19f);
        private Coroutine feedbackRoutine;
        private Vector2 feedbackHome;

        private void Awake()
        {
            pet = GameSession.CurrentPet;
            if (pet == null)
            {
                // Dostu olmayan oyuncu bu sahneye gelmemeli; editörde doğrudan açıldıysa sahiplenmeye gönder
                enabled = false;
                LoadingScreen.Go(HomeController.HomeScene);
                return;
            }
            species = SpeciesCatalog.Get(pet.species);
            feedbackText.text = "";
            // Rehber elin çizimi (U10) gelene kadar pati gösterir
            guidePoint = PetArt.Get("icon_hand_point") ?? PetArt.Get("icon_ui_paw");
            guideTap = PetArt.Get("icon_hand_tap");
            if (guideHand != null)
            {
                guideHand.sprite = guidePoint;
                guideHand.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            Loc.Changed += Refresh;
            GameSession.Advanced += Refresh;
            GameSession.PetDied += OnPetDied;
        }

        private void OnDisable()
        {
            Loc.Changed -= Refresh;
            GameSession.Advanced -= Refresh;
            GameSession.PetDied -= OnPetDied;
        }

        private void OnPetDied()
        {
            pet = null;
            LoadingScreen.Go(MemorialController.MemorialScene);
        }

        private void Start()
        {
            if (pet == null) return;
            ApplySpeciesButtons();
            ShowRoom((int)lastRoom);
            // Dost "merhaba" der (uyuyorsa sessiz)
            if (!pet.asleep) Invoke(nameof(Greet), 0.8f);
        }

        private void Greet() => Sound.PetVoice(pet.species, 0.8f);

        private void Update()
        {
            if (pet == null) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f) Refresh();
            if (vetPoseTimer > 0f)
            {
                vetPoseTimer -= Time.unscaledDeltaTime;
                if (vetPoseTimer <= 0f) SetVetPose("vet_wave");
            }
            Animate();
            UpdateGuide();
        }

        /// <summary>
        /// Her karede: azalan halkalar nabız gibi atar, balon süzülür, uyurken oda kararır ve Zzz yükselir,
        /// yerdeki kakadan koku bulutu çıkar.
        /// </summary>
        private void Animate()
        {
            float t = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            // Uyurken ninni, uyanıkken gündüz müziği (aynısı çalıyorsa bir şey yapmaz)
            if (walk == null || !walk.IsOpen) Sound.Music(pet.asleep ? "music_night" : "music_day");
            // Yalnızca değişen şey yazılır: aynı değeri her karede yazmak da tuvali yeniden hesaplatır
            if (statRings != null)
                for (int i = 0; i < statRings.Length && i < ringValues.Length; i++)
                {
                    float s = ringValues[i] < 0.3f ? 1f + Mathf.Abs(Mathf.Sin(t * 5f)) * 0.12f : 1f;
                    if (!Mathf.Approximately(statRings[i].localScale.x, s)) statRings[i].localScale = new Vector3(s, s, 1f);
                }
            if (bubble != null && bubble.gameObject.activeSelf)
            {
                // Dost gezinirken balon da peşinden gelir; dost sağa gidince sol yanına geçer (sağdaki butonlara binmesin)
                float offset = string.IsNullOrEmpty(pet.hat) ? 150f : 230f;
                float petX = ((RectTransform)view.transform).anchoredPosition.x;
                float side = petX > 40f ? -1f : 1f;
                // Süzülen dostla birlikte yükselir, durum halkalarının altında kalır
                float y = Mathf.Min(bubbleHome.y + view.Hover, 400f);
                bubble.anchoredPosition = new Vector2(petX + side * offset, y + Mathf.Sin(t * 2.2f) * 10f);
                if (!Mathf.Approximately(bubble.localScale.x, side))
                {
                    bubble.localScale = new Vector3(side, 1f, 1f);
                    if (bubbleIcon != null) bubbleIcon.rectTransform.localScale = new Vector3(side, 1f, 1f);
                }
            }
            if (night != null)
            {
                Color c = night.color;
                float target = pet.asleep ? 0.5f : 0f;
                if (!Mathf.Approximately(c.a, target))
                {
                    c.a = Mathf.MoveTowards(c.a, target, dt * 1.2f);
                    night.color = c;
                }
                // Gündüz tam ekran saydam katman hiç çizilmesin
                bool show = c.a > 0.001f;
                if (night.enabled != show) night.enabled = show;
            }

            ambientTimer -= dt;
            if (ambientTimer > 0f) return;
            // Panel ya da gezinti açıkken odanın Zzz'si ve kokusu önüne çıkmasın
            if ((walk != null && walk.IsOpen) || PanelPop.AnyOpen) return;
            ambientTimer = 1.4f;
            if (pet.asleep) Fx.Burst(transform, "icon_fx_zzz", PetCenter + Vector3.up * 120f * transform.lossyScale.y, 1, 90f, true, 0.25f);
            if (pet.poopOnFloor && poop.activeInHierarchy) Fx.Burst(transform, "icon_fx_stink", poop.transform.position + Vector3.up * 60f * transform.lossyScale.y, 1, 70f, true, 0.2f);
        }

        /// <summary>
        /// Dostun üstünde parmağı gezdirme: banyoda köpük çıkar ve yeterince ovalayınca yıkanır, diğer odalarda
        /// kalpler çıkar ve sevilir.
        /// </summary>
        public void Rub(Vector3 screenPos, float distance)
        {
            if (pet == null) return;
            bool bath = RoomKeys[(int)room] == "bath";
            rubSpawn += distance;
            rubDistance += distance;
            if (stage != null) stage.Rubbing(screenPos, bath);
            if (rubSpawn > 130f)
            {
                rubSpawn = 0f;
                // Kalpler parmaktan yukarı uçup dağılır (dostun yüzünde birikmesin); banyoda köpük üstüne yapışır
                if (!bath) Fx.Burst(transform, "icon_fx_heart", screenPos, 1, 62f, true, 1f);
                Sound.Play("bubble", 0.35f, bath ? UnityEngine.Random.Range(0.9f, 1.3f) : UnityEngine.Random.Range(1.3f, 1.6f));
            }
            if (rubDistance < 1500f) return;
            rubDistance = 0f;
            if (bath) Bathe();
            else Stroke();
        }

        // ---------- Odalar ----------

        public void ShowRoom(int index)
        {
            room = lastRoom = (Room)index;
            if (stage != null) stage.Cancel();
            for (int i = 0; i < roomPanels.Length; i++)
            {
                roomPanels[i].SetActive(i == index);
                tabFaces[i].color = i == index ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }
            // Veteriner panda yalnızca veteriner odasında
            if (vetPanda != null)
            {
                vetPanda.gameObject.SetActive(RoomKeys[index] == "vet");
                SetVetPose("vet_wave");
                vetPoseTimer = 0f;
            }
            // Oda teması (Merkez > Ev) odanın rengine yarı yarıya karışır
            Color tint = PetLooks.ThemeTint(GameSession.Data.theme);
            // Türün kendi ortamı: balık fanusta, ördek gölette...
            Sprite drawing = PetArt.RoomOf(species, RoomKeys[index]);
            roomBackground.sprite = drawing;
            if (drawing != null)
            {
                // Çizilmiş oda: kendi renginde (tema hafif tonlar); zemindeki beyaz oval yerine dostun gölgesi
                roomBackground.color = tint.a > 0f ? Color.Lerp(Color.white, tint, 0.25f) : Color.white;
                roomFitter.aspectRatio = drawing.rect.width / drawing.rect.height;
                floor.color = new Color(0f, 0f, 0f, species.moves == MoveKind.Swim ? 0f : 0.14f);   // süzülenin yerde gölgesi yok
                floor.rectTransform.sizeDelta = new Vector2(430f, 90f);
                floor.rectTransform.anchoredPosition = new Vector2(floor.rectTransform.anchoredPosition.x, -425f);
            }
            else
            {
                roomBackground.color = tint.a > 0f ? Color.Lerp(roomColors[index], tint, 0.5f) : roomColors[index];
                floor.color = new Color(1f, 1f, 1f, 0.45f);
                floor.rectTransform.sizeDelta = new Vector2(1000f, 260f);
                floor.rectTransform.anchoredPosition = new Vector2(0f, -330f);
            }
            Refresh();
        }

        /// <summary>Satırdaki görünen butonları eşit aralıkla ortalar (balıkta su, penguende tarama yok...).</summary>
        private static void CenterActions(Transform row)
        {
            const float step = 264f;
            int count = 0;
            for (int i = 0; i < row.childCount; i++)
                if (row.GetChild(i).gameObject.activeSelf && row.GetChild(i).GetComponent<Button>() != null) count++;
            int index = 0;
            for (int i = 0; i < row.childCount; i++)
            {
                Transform child = row.GetChild(i);
                if (!child.gameObject.activeSelf || child.GetComponent<Button>() == null) continue;
                var rect = (RectTransform)child;
                rect.anchoredPosition = new Vector2((index - (count - 1) * 0.5f) * step, rect.anchoredPosition.y);
                index++;
            }
        }

        // ---------- Aksiyonlar ----------

        /// <summary>Türün yiyecek listesindeki <paramref name="index"/>. yiyeceği verir; lezzetliler altınla.</summary>
        public void FeedFood(int index)
        {
            if (index >= species.foods.Length || (stage != null && stage.Busy)) return;
            Food food = FoodCatalog.Get(species.foods[index]);
            if (GameSession.Data.gold < food.price)
            {
                Feedback("vet_nomoney");
                return;
            }
            if (stage != null) stage.NextFood = PetArt.Get("icon_food_" + food.id);
            ActionResult result = PetActions.Feed(WokenPet(), food);
            if (result == ActionResult.Done || result == ActionResult.Overfed) Economy.Spend(GameSession.Data, food.price);
            Show(result, "fx_eat", "feed");
        }

        // ---------- Okul, dolap, oyunlar ----------

        /// <summary>Komut butonu: öğrenilmişse gösterir (numara), değilse ders açılır (sıra hafızası).</summary>
        public void Command(int index)
        {
            string command = species.commands[index];
            if (Training.Learned(pet, command))
            {
                ActionResult result = Training.Perform(WokenPet(), command);
                if (result == ActionResult.Done)
                {
                    view.Trick();
                    Fx.Burst(transform, "icon_mg_star", PetCenter, 8, 80f, false);
                    Sound.Play("fanfare", 0.7f);
                    Sound.PetVoice(pet.species);
                }
                Show(result, "fx_trick");
                return;
            }
            WokenPet();
            if (pet.energy < Training.MinEnergy) { Feedback("res_tired"); return; }
            simon.Open(command);
        }

        /// <summary>SimonPanel dersi bitirince çağırır.</summary>
        public void FinishLesson(string command, bool success)
        {
            ActionResult result = Training.Practice(pet, species, command, success);
            if (result != ActionResult.Done)
            {
                Show(result, "");
                return;
            }
            string progress = TrackAction("lesson");
            GameSession.Save();
            bool learned = Training.Learned(pet, command);
            if (learned)
            {
                view.Trick();
                Fx.Confetti(transform, PetCenter, 70);
                Sound.Play("fanfare");
                Sound.PetVoice(pet.species);
            }
            if (iconFeedback)
            {
                if (!learned) FeedbackIcon(success ? "" : "school_again");
                if (success && !learned) Fx.Burst(transform, "icon_fx_star", PetCenter, 5, 70f);
                Refresh();
                return;
            }
            if (learned) FeedbackText(progress ?? Loc.Get("school_learned"));
            else FeedbackText(progress ?? Loc.Get(success ? "school_good" : "school_again"));
        }

        public void ReadBook() => Show(Training.Read(WokenPet(), species), "fx_read", "read");
        public void OpenShop() => LoadingScreen.Go(ShopController.ShopScene);
        public void GoHome() => LoadingScreen.Go(HomeController.HomeScene);
        public void OpenHub() => LoadingScreen.Go(HubController.HubScene);
        public void OpenPhoto()
        {
            if (stage != null) stage.Cancel();
            photo.Open();
        }

        public void OpenBreed() => breed.Open();
        public void OpenCloset() => wardrobe.Open();

        /// <summary>Dolap odasındaki düğme: üstündeki her şeyi çıkarır (parçalar dolapta kalır).</summary>
        public void RemoveAllClothes()
        {
            Wardrobe.RemoveAll(pet);
            GameSession.Save();
            view.Bounce();
            Sound.Play("sparkle", 0.7f);
            Refresh();
        }

        /// <summary>Durum halkasına dokunuş: o ihtiyacın odasına gider.</summary>
        public void RingTap(int index)
        {
            int[] rooms = { (int)Room.Kitchen, (int)Room.Kitchen, (int)Room.Bed, (int)Room.Bath, (int)Room.Play, (int)Room.Vet };
            if (index >= 0 && index < rooms.Length) ShowRoom(rooms[index]);
        }

        /// <summary>Düşünce balonuna dokunuş: dostun istediği yere gider.</summary>
        public void GoToNeed()
        {
            if (GuideTarget(out int roomIndex, out RectTransform _)) ShowRoom(roomIndex);
        }

        /// <summary>Gezintiden döndü: dostun durumuna işlenir, altın cüzdana geçer.</summary>
        public void FinishWalk(int coins)
        {
            if (PetActions.Walk(pet) == ActionResult.Done)
            {
                GameSession.Data.gold += coins;
                TrackAction("play");
                view.Bounce();
                view.HoldPose("happy", 1.6f);
                Celebrate("walk", false);
                GameSession.Save();
            }
            Refresh();
        }

        public void OpenWalk()
        {
            WokenPet();
            if (pet.energy < PetActions.WalkMinEnergy)
            {
                FeedbackIcon("res_tired");
                return;
            }
            stage.Cancel();
            walk.Open();
        }

        public void OpenGames()
        {
            WokenPet();
            games.Open();
        }

        /// <summary>
        /// Çocuk uyuyan dostla bir şey yapmak istedi (yemek, oyun, ders...): dost uyanır, istek engellenmez.
        /// Gece kendiliğinden yeniden uyumadan bir süre uyanık kalır (PetSimulator.WokenAwakeSeconds).
        /// </summary>
        private PetState WokenPet()
        {
            if (pet.asleep && PetActions.Wake(pet) == ActionResult.Done)
            {
                Sound.Play("wake");
                view.Bounce();
            }
            return pet;
        }

        /// <summary>Paneller (dolap, oyunlar) için uçan yazı.</summary>
        public void ShowMessage(string key)
        {
            Feedback(key);
        }

        /// <summary>Yeni giydirilen parçayı gösterir: dost hemen giyer, zıplar, parıltı ve kalp saçılır.</summary>
        public void ShowOff()
        {
            Refresh();
            view.Bounce();
            Sound.PetVoice(pet.species, 0.7f);
        }

        private bool StageBusy => stage != null && stage.Busy;

        public void Water()
        {
            if (!StageBusy) Show(PetActions.Drink(WokenPet()), "fx_drink", "drink");
        }

        public void Bathe()
        {
            if (!StageBusy) Show(PetActions.Bathe(WokenPet()), "fx_bathe", "bathe");
        }

        public void Brush()
        {
            if (!StageBusy) Show(PetActions.Brush(WokenPet()), "fx_brush", "brush");
        }

        public void Toilet()
        {
            if (!StageBusy) Show(PetActions.UseToilet(WokenPet()), "fx_toilet", "toilet");
        }

        public void Clean() => Show(PetActions.CleanPoop(pet), "fx_clean", "clean");

        /// <summary>Top düğmesi: yorgunsa olmaz; top düşer, dost peşinden koşar. Top sahnedeyken yeni bir vuruş.</summary>
        public void Play()
        {
            if (StageBusy) return;
            if (stage != null && stage.BallActive)
            {
                stage.KickBall();
                return;
            }
            Show(PetActions.Play(WokenPet()), "fx_play", "play");
        }

        /// <summary>Dokunan çocuğa uyuyan dost da karşılık verir: diğer aksiyonlar gibi uyanır (WokenPet).</summary>
        public void Stroke() => Show(PetActions.Stroke(WokenPet()), "fx_stroke", "stroke");

        public void ToggleSleep()
        {
            if (stage != null) stage.Cancel();
            bool wasAsleep = pet.asleep;
            if (wasAsleep) Show(PetActions.Wake(pet), "fx_wake");
            else Show(PetActions.Sleep(pet), "fx_sleep");
            if (pet.asleep != wasAsleep) Sound.Play(pet.asleep ? "lights" : "wake");
        }

        // ---------- Veteriner ----------

        /// <summary>Altın yetiyorsa altınla, yetmiyorsa günün bedava acil müdahalesiyle tedavi eder.</summary>
        public void Treat()
        {
            SaveData wallet = GameSession.Data;
            VetResult result = wallet.gold >= Vet.TreatCost(pet) || !Vet.FreeTreatAvailable(pet, DateTime.Now)
                ? Vet.Treat(pet, wallet)
                : Vet.FreeTreat(pet, DateTime.Now);
            if (result == VetResult.Done) wallet.totalTreatments++;
            ShowVet(result, "vet_done_treat");
        }

        public void Vaccinate() => ShowVet(Vet.Vaccinate(pet, GameSession.Data), "vet_done_vaccine");

        public void Miracle()
        {
            VetResult result = Vet.Miracle(pet, species, GameSession.Data);
            if (result == VetResult.NoMoney)
            {
                // Elması yetmiyorsa mağazaya (ebeveyn kapısı orada)
                ShopController.OpenFor(ProductCatalog.Gems10);
                return;
            }
            if (result == VetResult.Done) GameSession.Data.totalTreatments++;
            ShowVet(result, "vet_done_miracle");
        }

        private void ShowVet(VetResult result, string doneKey)
        {
            string key;
            switch (result)
            {
                case VetResult.Done: key = doneKey; break;
                case VetResult.NotSick: key = "vet_notsick"; break;
                case VetResult.NoMoney: key = "vet_nomoney"; break;
                case VetResult.AlreadyVaccinated: key = "vet_already"; break;
                case VetResult.Critical: key = "vet_critical"; break;
                case VetResult.NotCritical: key = "vet_notcritical"; break;
                default: key = "vet_usedtoday"; break;
            }
            if (result == VetResult.Done)
            {
                view.Bounce();
                GameSession.Save();
                // Veteriner panda yaptığı işin pozuna geçer, biraz sonra yine el sallar
                SetVetPose(doneKey == "vet_done_vaccine" ? "vet_injection" : doneKey == "vet_done_miracle" ? "vet_cheer" : "vet_medicine");
                vetPoseTimer = 2.5f;
                Fx.Burst(transform, "icon_mg_star", PetCenter, 8, 80f, false);
                Sound.Play("sparkle");
                if (doneKey == "vet_done_miracle")
                {
                    Fx.Confetti(transform, PetCenter, 80);
                    Sound.Play("fanfare");
                }
            }
            Feedback(key);
        }

        private void SetVetPose(string key)
        {
            if (vetPanda == null) return;
            Sprite sprite = PetArt.Get(key);
            if (sprite != null) vetPanda.sprite = sprite;
        }

        /// <param name="track">Günlük görev ve bakım serisi için aksiyon adı (Progress.Count); null ise sayılmaz.</param>
        private void Show(ActionResult result, string doneKey, string track = null)
        {
            string key;
            switch (result)
            {
                case ActionResult.Done: key = doneKey; break;
                case ActionResult.Asleep: key = "res_asleep"; break;
                case ActionResult.Overfed: key = "res_overfed"; break;
                case ActionResult.Tired: key = "res_tired"; break;
                default: key = "res_notneeded"; break;
            }
            string text = Loc.Get(key);
            if (result == ActionResult.Done || result == ActionResult.Overfed)
            {
                string reward = track != null ? TrackAction(track) : null;
                if (reward != null) text = reward;
                bool special = reward != null;
                GameSession.Save();
                // Canlı sahne (yemek kabı, top, duş...) bitince kutlanır; sahnesi olmayan aksiyon hemen
                if (stage == null || !stage.Perform(track, () => Celebrate(track, special)))
                {
                    view.Bounce();
                    Celebrate(track, special);
                }
            }
            if (iconFeedback)
            {
                // Yazısız: başarı parçacıklarla anlatılır; olmadıysa nedenin resmi çıkar
                if (result != ActionResult.Done) FeedbackIcon(key);
                Refresh();
                return;
            }
            FeedbackText(text);
        }

        /// <summary>Okuma bilmeyen çocuk için geri bildirim: mesajın resmi dostun başının üstünden yükselir.</summary>
        private void FeedbackIcon(string key)
        {
            string icon = FeedbackIconKey(key);
            if (icon != null) Fx.Burst(transform, icon, PetCenter + Vector3.up * 200f * transform.lossyScale.y, 1, 150f, true, 0.3f);
            if (icon != null) Sound.Play(icon == "icon_ui_check" || icon == "icon_act_photo" ? "sparkle" : "error", 0.7f);
        }

        private static string FeedbackIconKey(string key)
        {
            switch (key)
            {
                case "res_asleep": return "icon_fx_zzz";
                case "res_tired": return "icon_stat_energy";
                case "res_overfed": return "icon_stat_hunger";
                case "vet_nomoney": return "icon_fx_coin";
                case "vet_usedtoday": return "icon_ui_time";
                case "vet_critical": return "icon_stat_health";
                case "wardrobe_locked": return "icon_ui_lock";
                case "school_again": return "icon_act_school";
                case "res_notneeded":
                case "vet_notsick":
                case "vet_already":
                case "vet_notcritical": return "icon_ui_check";
                default: return key.StartsWith("photo") ? "icon_act_photo" : null;
            }
        }

        // ---------- Rehber el (yazısız yönlendirme) ----------

        /// <summary>
        /// Dostun en acil ihtiyacına göre gidilecek oda ve basılacak buton. İhtiyaç yoksa false.
        /// </summary>
        private bool GuideTarget(out int roomIndex, out RectTransform button)
        {
            roomIndex = (int)room;
            button = null;
            switch (Mood.Of(pet))
            {
                case MoodKind.Critical:
                    roomIndex = 6;
                    button = (RectTransform)(miracleButton.activeSelf ? miracleButton.transform : treatLabel.transform.parent);
                    break;
                case MoodKind.Sick:
                    roomIndex = 6;
                    button = (RectTransform)treatLabel.transform.parent;
                    break;
                case MoodKind.NeedsToilet:
                    if (!species.hasToilet) return false;
                    roomIndex = 1;
                    button = (RectTransform)toiletButton.transform;
                    break;
                case MoodKind.Hungry:
                    roomIndex = 0;
                    button = (RectTransform)foodButtons[0].transform;
                    break;
                case MoodKind.Thirsty:
                    if (!species.drinks) return false;
                    roomIndex = 0;
                    button = (RectTransform)waterButton.transform;
                    break;
                case MoodKind.Sleepy:
                    roomIndex = 2;
                    button = (RectTransform)sleepLabel.transform.parent;
                    break;
                case MoodKind.Dirty:
                    roomIndex = 1;
                    button = (RectTransform)(pet.poopOnFloor ? cleanButton.transform : batheButton);
                    break;
                case MoodKind.Bored:
                    roomIndex = 3;
                    button = playButton;
                    break;
                case MoodKind.Sulky:
                    button = petArea;
                    break;
                default:
                    return false;
            }
            return button != null;
        }

        /// <summary>
        /// Çocuk bir süre dokunmazsa el çıkar: önce gidilecek odanın sekmesine, odaya gelince basılacak butona
        /// dokunuyormuş gibi zıplar. Ekrana dokununca kaybolur; açık panel varken görünmez.
        /// </summary>
        private void UpdateGuide()
        {
            if (guideHand == null) return;
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed) idleTime = 0f;
            else idleTime += Time.unscaledDeltaTime;

            RectTransform aim = null;
            if (idleTime > GuideDelay && !PanelPop.AnyOpen && GuideTarget(out int roomIndex, out RectTransform button))
                aim = roomIndex == (int)room ? button : tabFaces[roomIndex].rectTransform;
            bool show = aim != null && aim.gameObject.activeInHierarchy;
            if (guideHand.gameObject.activeSelf != show) guideHand.gameObject.SetActive(show);
            if (guideRing != null && guideRing.gameObject.activeSelf != show) guideRing.gameObject.SetActive(show);
            if (!show) return;

            // Hedefin etrafında sarı halka: büyüyüp solar, baştan başlar ("buraya bas")
            if (guideRing != null)
            {
                float side = Mathf.Max(aim.rect.width, aim.rect.height) * 1.15f;
                float wave = Mathf.Repeat(Time.unscaledTime * 1.2f, 1f);
                guideRing.transform.position = aim.position;
                guideRing.rectTransform.sizeDelta = new Vector2(side, side) * (0.9f + wave * 0.35f);
                guideRing.color = new Color(1f, 0.85f, 0.2f, 0.95f * (1f - wave));
            }

            // Dokunma hareketi: el hedefin üstünden aşağı uzanır (sekmeler ekranın en altında; alttan gösterilemez),
            // parmak ucu hedefin ortasına iner (basar) ve geri kalkar. Çizim parmağı yukarı gösterir: 180° çevrilir.
            float cycle = Mathf.Repeat(Time.unscaledTime * 1.2f, 1f);
            float press = cycle < 0.5f ? Mathf.SmoothStep(0f, 1f, cycle * 2f) : Mathf.SmoothStep(1f, 0f, (cycle - 0.5f) * 2f);
            float unit = transform.lossyScale.y;
            guideHand.transform.position = aim.position + new Vector3(0f, (10f + 60f * (1f - press)) * unit, 0f);
            // İşaret eli yukarıyı gösterir: dikey çevrilir (parmak aşağı, bilek yukarı). Basan el zaten aşağı bakar.
            // Pivot her iki çizimde de parmak ucudur.
            bool tapping = guideTap != null && press > 0.85f;
            guideHand.sprite = tapping ? guideTap : guidePoint;
            guideHand.rectTransform.pivot = tapping ? TapTip : PointTip;
            float s = 1f - press * 0.08f;
            // Elin gövdesi parmak ucunun sağına uzanır: sağ yarıdaki hedefte (revir sekmesi) ekran dışına taşmasın
            // diye yatayda aynalanır
            float flip = aim.position.x > Screen.width * 0.5f ? -1f : 1f;
            guideHand.transform.localScale = new Vector3(s * flip, tapping ? s : -s, 1f);
        }

        /// <summary>Dostun gövdesinin ortası (ekran pikseli): efektler buradan çıkar.</summary>
        private Vector3 PetCenter => view.transform.position + Vector3.up * ((260f * view.VisualScale + view.Hover) * transform.lossyScale.y);

        /// <summary>
        /// Aksiyonun efekti: sevince kalpler, oynayınca top ve yıldızlar, diğerlerinde parıltı.
        /// Yeni rozet ya da seri ödülü varsa üstüne konfeti.
        /// </summary>
        private void Celebrate(string track, bool special)
        {
            Vector3 at = PetCenter;
            switch (track)
            {
                case "stroke":
                    // Kalpler başın üstünden yayılır, yüzü örtmez
                    Fx.Burst(transform, "icon_fx_heart", at + Vector3.up * 230f * view.VisualScale * transform.lossyScale.y, 5, 74f);
                    Sound.Play("heart", 0.7f);
                    Sound.PetVoice(pet.species);
                    break;
                case "play":
                    Fx.Burst(transform, "icon_act_play", at, 2, 110f);
                    Fx.Burst(transform, "icon_fx_note", at, 3, 70f);
                    Fx.Burst(transform, "icon_fx_star", at, 4, 60f, false);
                    Sound.Play("bounce");
                    Sound.PetVoice(pet.species, 0.7f);
                    break;
                case "feed":
                    Fx.Burst(transform, "icon_fx_heart", at, 3, 70f);
                    Fx.Burst(transform, "icon_fx_sparkle", at, 4, 60f, false);
                    Sound.Play("eat");
                    break;
                case "drink":
                    Fx.Burst(transform, "icon_fx_drop", at, 5, 60f);
                    Sound.Play("drink");
                    break;
                case "bathe":
                    Fx.Burst(transform, "icon_fx_bubble", at, 8, 80f);
                    Fx.Burst(transform, "icon_fx_sparkle", at, 4, 60f, false);
                    Sound.Play("splash");
                    break;
                case "brush":
                    Fx.Burst(transform, "icon_fx_bubble", at, 8, 80f);
                    Fx.Burst(transform, "icon_fx_sparkle", at, 4, 60f, false);
                    Sound.Play("brush");
                    break;
                case "clean":
                case "toilet":
                    Fx.Burst(transform, "icon_fx_dust", poop.transform.position, 4, 90f, false, 0.5f);
                    Fx.Burst(transform, "icon_fx_sparkle", at, 4, 60f, false);
                    Sound.Play("sweep");
                    break;
                case "read":
                    Fx.Burst(transform, "icon_fx_star", at, 5, 60f);
                    Sound.Play("page");
                    break;
                default:
                    Fx.Burst(transform, "icon_fx_sparkle", at, 6, 70f, false);
                    Sound.Play("sparkle");
                    break;
            }
            if (special)
            {
                Fx.Confetti(transform, at);
                Sound.Play("fanfare", 0.8f);
            }
        }

        /// <summary>
        /// Bakım aksiyonunu günlük görevlere ve seriye sayar; yeni rozet ya da günün seri ödülü varsa onun
        /// mesajını döndürür (yoksa null). Dolap ve mini oyunlar da çağırır.
        /// </summary>
        public string TrackAction(string action)
        {
            SaveData save = GameSession.Data;
            bool streak = Progress.Count(save, action, DateTime.Now);
            var badges = Progress.CheckBadges(save);
            if (badges.Count > 0) return Loc.Format("badge_new", Loc.Get(badges[0].NameKey));
            if (streak) return Loc.Format("streak_up", save.streakDays, Progress.StreakGold);
            return null;
        }

        private void Feedback(string key)
        {
            if (iconFeedback)
            {
                FeedbackIcon(key);
                Refresh();
            }
            else FeedbackText(Loc.Get(key));
        }

        private void FeedbackText(string text)
        {
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(Float(text));
            Refresh();
        }

        private IEnumerator Float(string message)
        {
            feedbackText.text = message;
            RectTransform rt = feedbackText.rectTransform;
            Vector2 start = feedbackHome;
            const float duration = 1.3f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                rt.anchoredPosition = start + new Vector2(0f, k * 90f);
                feedbackText.color = new Color(feedbackText.color.r, feedbackText.color.g, feedbackText.color.b, 1f - k * k);
                yield return null;
            }
            feedbackText.text = "";
            rt.anchoredPosition = start;
        }

        // ---------- Ekran ----------

        public void Refresh()
        {
            if (pet == null) return;
            refreshTimer = RefreshInterval;

            view.Apply(pet, species);
            // Balon ve geri bildirim dostun başının üstünde durur; dost büyüdükçe yukarı çıkar
            Vector2 petAt = ((RectTransform)view.transform).anchoredPosition;
            float top = petAt.y + PetHeight * view.VisualScale;
            // Şapka takılıysa balon biraz daha sağa: şapkanın üstüne binmesin
            float bubbleX = string.IsNullOrEmpty(pet.hat) ? 150f : 230f;
            bubbleHome = new Vector2(bubbleIcon != null ? petAt.x + bubbleX : 0f, top + (bubbleIcon != null ? 60f : 170f));
            feedbackHome = new Vector2(0f, top + 40f);
            if (feedbackText.text.Length == 0) feedbackText.rectTransform.anchoredPosition = feedbackHome;

            nameText.text = pet.name;
            string stage = Loc.Get("stage_" + Mood.Stage(pet, species).ToString().ToLowerInvariant());
            string mode = Loc.Get(pet.Mode == SpeedMode.Fast ? "mode_fast" : "mode_real");
            infoText.text = $"{Loc.Get(species.NameKey)} · {Loc.Age(pet.AgeDays)} · {stage} · {mode}";
            // Durum satırı: kilo · aşı · hastalık
            string status = Loc.Get(Mood.WeightKey(pet));
            if (Vet.IsVaccinated(pet)) status += " · " + Loc.Get("status_vaccinated");
            Disease disease = DiseaseCatalog.Get(pet.disease);
            if (disease != null) status += " · " + Loc.Format("status_sick", Loc.Get(disease.NameKey));
            SaveData save = GameSession.Data;
            if (Shop.IsBoarding(save, DateTime.UtcNow))
                status += " · " + Loc.Format("boarding_days", Math.Ceiling(new TimeSpan(save.boardingUntilUtcTicks - DateTime.UtcNow.Ticks).TotalDays));
            SeasonEvent seasonEvent = SeasonEvents.Current(DateTime.Now);
            if (seasonEvent != null) status += " · " + Loc.Get(seasonEvent.NameKey);
            weightText.text = status;
            // Yazısız ekran: durum satırı yerine küçük ikonlar
            if (statusIcons != null && statusIcons.Length == 4)
            {
                statusIcons[0].SetActive(Vet.IsVaccinated(pet));
                statusIcons[1].SetActive(disease != null);
                statusIcons[2].SetActive(Shop.IsBoarding(save, DateTime.UtcNow));
                statusIcons[3].SetActive(seasonEvent != null);
            }

            // Merkez butonundaki sayı: alınmayı bekleyen görev ödülleri
            Progress.EnsureDay(save, DateTime.Now);
            int ready = 0;
            for (int i = 0; i < save.taskIds.Count; i++)
                if (Progress.TaskDone(save, i) && !save.taskClaimed[i]) ready++;
            hubBadge.text = ready.ToString();
            hubBadge.transform.parent.gameObject.SetActive(ready > 0);
            if (breedButton != null) breedButton.SetActive(save.pets.Count >= 2);
            ApplySpeciesButtons();
            // Balığın kiri suyundadır: kirlendikçe su yeşillenir
            if (species.moves == MoveKind.Swim && roomBackground != null)
                roomBackground.color = Color.Lerp(Color.white, new Color(0.72f, 0.88f, 0.55f), Mathf.Clamp01((float)((50.0 - pet.hygiene) / 50.0)) * 0.8f);
            goldText.text = GameSession.Data.gold.ToString();
            diamondText.text = GameSession.Data.diamonds.ToString();

            // Halkalar (RingStats) ya da eski 9 çubuk
            bool rings = statFills.Length == RingStats.Length;
            if (ringValues.Length != statFills.Length) ringValues = new float[statFills.Length];
            for (int i = 0; i < statFills.Length; i++)
            {
                float value = (float)(StatValue(rings ? RingStats[i] : i) / 100.0);
                statFills[i].fillAmount = value;
                statFills[i].color = BarColor(value);
                ringValues[i] = value;
            }

            // Balon: ikon varsa ruh halinin resmi (keyfi yerindeyse balon gizlenir), yoksa yazı
            string moodIcon = MoodIcon(Mood.Of(pet));
            Sprite moodSprite = moodIcon != null ? PetArt.Get(moodIcon) : null;
            if (bubbleIcon != null && PetArt.Get("icon_stat_hunger") != null)
            {
                bubble.gameObject.SetActive(moodSprite != null);
                bubbleIcon.sprite = moodSprite;
                bubbleText.text = "";
            }
            else bubbleText.text = Loc.Get(Mood.BubbleKey(pet));
            poop.SetActive(pet.poopOnFloor);
            cleanButton.SetActive(pet.poopOnFloor);
            sleepLabel.text = Loc.Get(pet.asleep ? "act_wake" : "act_sleep");
            SetIcon(sleepIcon, pet.asleep ? "icon_act_wake" : "icon_act_sleep");

            // Türe göre mutfak ve banyo: yiyecek adları/fiyatları, su, tarama, tuvalet
            for (int i = 0; i < foodButtons.Length; i++)
            {
                bool has = i < species.foods.Length;
                foodButtons[i].SetActive(has);
                if (!has) continue;
                Food food = FoodCatalog.Get(species.foods[i]);
                foodLabels[i].text = Loc.Get(food.NameKey);   // fiyat rozette
                if (foodIcons != null && i < foodIcons.Length) SetIcon(foodIcons[i], "icon_food_" + food.id);
                if (foodPrices != null && i < foodPrices.Length) SetPrice(foodPrices[i], food.price);
            }
            waterButton.SetActive(species.drinks);
            brushButton.SetActive(species.brushable);
            toiletButton.SetActive(species.hasToilet);

            // Okul: komut adı ve ilerleme (öğrendiyse "Göster")
            for (int i = 0; i < commandLabels.Length; i++)
            {
                string command = species.commands[i];
                string name = Loc.Get("cmd_" + command);
                commandLabels[i].text = name;   // ilerleme çubukta, öğrenilince yıldız
                if (commandBars != null && i < commandBars.Length && commandBars[i] != null)
                {
                    bool learned = Training.Learned(pet, command);
                    commandBars[i].fillAmount = learned ? 1f : (float)(Training.Progress(pet, command) / 100.0);
                    commandBars[i].transform.parent.gameObject.SetActive(!learned);
                    commandStars[i].SetActive(learned);
                }
                if (commandIcons != null && i < commandIcons.Length) SetIcon(commandIcons[i], CommandIcon(command));
            }

            // Veteriner butonları: fiyatlar ve bedava seçenekler
            bool critical = PetSimulator.IsCritical(pet);
            int treatCost = Vet.TreatCost(pet);
            treatLabel.text = Loc.Get("vet_treat_none");   // fiyatlar rozette
            vaccineLabel.text = Loc.Get("vet_vaccine_name");
            int miracleCost = Vet.MiracleCost(pet, species);
            miracleLabel.text = Loc.Get("vet_miracle_name");
            miracleButton.SetActive(critical);
            // Fiyat rozetleri (yazısız butonlarda): hasta değilken ya da bedavaysa rozet yok
            bool freeTreat = GameSession.Data.gold < treatCost && Vet.FreeTreatAvailable(pet, DateTime.Now);
            SetPrice(treatPrice, pet.IsSick && !freeTreat ? treatCost : 0);
            SetPrice(vaccinePrice, Vet.VaccineCost);
            SetPrice(miraclePrice, miracleCost);

            // Tür ya da duruma göre eksilen butonlar boşluk bırakmasın: satır ortalanır
            CenterActions(roomPanels[0].transform);
            CenterActions(roomPanels[1].transform);
            CenterActions(roomPanels[6].transform);

            criticalBanner.SetActive(critical);
            if (critical)
            {
                // Yazısız: kum saati ve kalan saat (ölüm kapalıysa yalnızca sağlık ikonu)
                if (iconFeedback)
                    criticalText.text = GameSession.Data.noDeath ? "" : Math.Ceiling(PetSimulator.HoursLeft(pet)).ToString();
                else
                    criticalText.text = GameSession.Data.noDeath
                        ? Loc.Get("critical_banner_safe")
                        : Loc.Format("critical_banner", Math.Ceiling(PetSimulator.HoursLeft(pet)));
            }
        }

        private double StatValue(int index)
        {
            switch (index)
            {
                case 0: return pet.fullness;
                case 1: return pet.thirst;
                case 2: return pet.energy;
                case 3: return pet.hygiene;
                case 4: return pet.toilet;
                case 5: return pet.fun;
                case 6: return pet.health;
                case 7: return pet.love;
                case 8: return Mood.Happiness(pet);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static Color BarColor(float value)
        {
            if (value >= 0.6f) return new Color32(0x5C, 0xCF, 0xA8, 255);
            if (value >= 0.3f) return new Color32(0xFF, 0xC9, 0x3C, 255);
            return new Color32(0xFF, 0x6B, 0x6B, 255);
        }

        /// <summary>Fiyat rozeti: fiyat 0 ise gizli.</summary>
        private static void SetPrice(Text price, int amount)
        {
            if (price == null) return;
            price.transform.parent.gameObject.SetActive(amount > 0);
            price.text = amount.ToString();
        }

        /// <summary>Balondaki resim; keyfi yerindeyse (Okay) balon yok.</summary>
        public static string MoodIcon(MoodKind mood)
        {
            switch (mood)
            {
                case MoodKind.Critical: return "icon_stat_health";
                // Uyurken başın üstünde zaten "Zz" var: balon çıkmaz
                case MoodKind.Sleeping: return null;
                case MoodKind.Sick: return "icon_act_treat";
                case MoodKind.NeedsToilet: return "icon_stat_toilet";
                case MoodKind.Hungry: return "icon_stat_hunger";
                case MoodKind.Thirsty: return "icon_stat_thirst";
                case MoodKind.Sleepy: return "icon_act_sleep";
                case MoodKind.Dirty: return "icon_stat_clean";
                case MoodKind.Bored: return "icon_act_play";
                case MoodKind.Sulky: return "icon_stat_love";
                case MoodKind.Funny: return "icon_fx_note";
                case MoodKind.Happy: return "icon_stat_happy";
                default: return null;
            }
        }

        /// <summary>Türe uymayan düğmeler değişir: balıkta "Gezdir" yerine "Yüz", "Top" yerine "Kabarcık", "Yıka" yerine "Temizle".</summary>
        private void ApplySpeciesButtons()
        {
            bool swims = species.moves == MoveKind.Swim, flies = species.moves == MoveKind.Fly;
            if (walkButton != null) SetButton(walkButton.transform, swims ? "act_swim" : flies ? "act_fly" : "act_walk", swims ? "icon_fx_drop" : flies ? "icon_fx_petal" : "icon_ui_paw");
            if (playButton != null) SetButton(playButton, swims ? "act_bubbles" : "act_play", swims ? "icon_fx_bubble" : "icon_act_play");
            if (batheButton != null) SetButton(batheButton, swims ? "act_clean_tank" : "act_bathe", swims ? "icon_act_clean" : "icon_act_bathe");
        }

        private static void SetButton(Transform button, string labelKey, string iconKey)
        {
            Transform label = button.Find("Label"), icon = button.Find("Icon");
            if (label != null)
            {
                if (label.TryGetComponent(out LocalizedText localized)) localized.enabled = false;
                label.GetComponent<Text>().text = Loc.Get(labelKey);
            }
            if (icon != null) SetIcon(icon.GetComponent<Image>(), iconKey);
        }

        /// <summary>Buton ikonunu değiştirir; çizimi yoksa eski ikon kalır.</summary>
        /// <summary>Komutun ikonu: okuyamayan çocuk da üç dersi birbirinden ayırt eder.</summary>
        private static string CommandIcon(string command)
        {
            switch (command)
            {
                case "sit": return "icon_food_treat";     // otur → ödül maması
                case "paw": return "icon_ui_paw";
                case "roll": return "icon_mg_star";
                case "fetch": return "icon_mg_ball";
                case "come": return "icon_ui_heart";      // gel → sevgiyle yanına
                case "spin": return "icon_fx_sparkle";
                case "stand": return "icon_act_wake";     // kalk → güneş
                case "jump": return "icon_fx_star";
                case "speak": return "icon_fx_note";
                case "ball": return "icon_act_play";      // top ol
                case "flap": return "icon_fx_petal";
                case "hoop": return "icon_ui_medal";
                case "peek": return "icon_mg_box";
                case "roar": return "icon_cel_sunburst";
                default: return "icon_act_school";
            }
        }

        private static void SetIcon(Image icon, string key)
        {
            if (icon == null) return;
            Sprite sprite = PetArt.Get(key);
            if (sprite != null) icon.sprite = sprite;
        }
    }
}
