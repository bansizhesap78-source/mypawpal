using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PatiDostum.EditorTools
{
    /// <summary>
    /// Tüm sahneleri (Boot, Home) ve oyuncu ayarlarını koddan kurar.
    /// Menü: Pati Dostum > Sahneleri Kur. Sahneler elle düzenlenmez; değişiklik burada yapılır.
    /// Çizimler gelene kadar her şey düz renkli yer tutucu şekillerle (daire, yuvarlak kare, pati) kurulur.
    /// Ekran dikeydir: tasarım alanı 1080x1920.
    /// </summary>
    public static class SceneBuilder
    {
        private const string Tag = "[Pati Dostum] ";
        private const string PlaceholderDir = "Assets/Art/Placeholder";
        private const string IconDir = "Assets/Art/Icon";
        private const string FontPath = "Assets/Fonts/Baloo2-ExtraBold.ttf";
        private const string BootScene = "Boot";

        public const string ProductName = "Alpi's Paw Pal";
        public const string PackageName = "com.alpigame.mypawpal";

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        // Yer tutucu palet (yeni marka: yumuşak pastel)
        private static readonly Color Cream = new Color32(0xFF, 0xF6, 0xEC, 255);
        private static readonly Color Coral = new Color32(0xFF, 0x7F, 0x6E, 255);
        private static readonly Color Mint = new Color32(0x5C, 0xCF, 0xA8, 255);
        private static readonly Color SkyBlue = new Color32(0x8F, 0xD3, 0xF5, 255);
        private static readonly Color Plum = new Color32(0x4A, 0x33, 0x55, 255);
        private static readonly Color GoldColor = new Color32(0xFF, 0xC9, 0x3C, 255);
        private static readonly Color DiamondColor = new Color32(0x3D, 0xA5, 0xFF, 255);

        private static Font font;
        private static Sprite circle, roundRect, triangle;
        // Şeker arayüz: jöle buton (gövde + alt kalınlık + gölge), parlaklık şeridi, gölgeli kart
        private static Sprite jellyRect, jellyCircle, gloss, cardSprite, ringSprite;

        [MenuItem("Pati Dostum/Sahneleri Kur")]
        public static void Build()
        {
            EnsureFolder(PlaceholderDir);
            EnsureFolder("Assets/Scenes");

            font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            circle = CreateShape("Circle", 128, 64, 0);
            roundRect = CreateShape("RoundRect", 96, 28, 30);
            triangle = CreateTriangle("Triangle", 128);
            jellyRect = CreateJelly("JellyRect", false);
            jellyCircle = CreateJelly("JellyCircle", true);
            gloss = CreateGloss("Gloss");
            cardSprite = CreateCard("Card");
            ringSprite = CreateRing("Ring", 192, 22f);

            CreatePetArt();
            AssetDatabase.SaveAssets();

            BuildHomeScene();
            BuildAdoptScene();
            BuildPetScene();
            BuildMemorialScene();
            BuildMiniGameScene();
            BuildShopScene();
            BuildHubScene();
            BuildBootScene();

            // İlk sahne açılış (yükleme) ekranıdır
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath(BootScene), true),
                new EditorBuildSettingsScene(ScenePath(HomeController.HomeScene), true),
                new EditorBuildSettingsScene(ScenePath(AdoptController.AdoptScene), true),
                new EditorBuildSettingsScene(ScenePath(PetController.PetScene), true),
                new EditorBuildSettingsScene(ScenePath(MemorialController.MemorialScene), true),
                new EditorBuildSettingsScene(ScenePath(MiniGameHost.MiniGameScene), true),
                new EditorBuildSettingsScene(ScenePath(ShopController.ShopScene), true),
                new EditorBuildSettingsScene(ScenePath(HubController.HubScene), true)
            };

            ApplyPlayerSettings();
            SetAndroidIcons();
            SetStartup();
            StoreSettings.Apply();

            AssetDatabase.SaveAssets();
            Debug.Log(Tag + "Sahneler kuruldu: Boot, Home, Adopt, Pet, Memorial, MiniGame, Shop, Hub");
        }

        private static string ScenePath(string scene) => $"Assets/Scenes/{scene}.unity";

        private static void ApplyPlayerSettings()
        {
            // Alpi ailesindeki diğer oyunlar gibi: telefonda simgenin altındaki ad her dilde "Alpi's Paw Pal"
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = "aLpi Game";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.bundleVersion = "0.1.0";

            // Dikey, tek elle oynanır
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        // ---------- Ana sayfa ----------

        /// <summary>
        /// Ana sayfa: Alpi logosu, cüzdan, dost kartları (2 sütun) ve altta büyük Görevler, Mağaza, Anılar, Ayarlar.
        /// </summary>
        private static void BuildHomeScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(Cream);
            Transform canvas = CreateCanvas();
            Background(canvas, "bg_acilis");
            var home = canvas.gameObject.AddComponent<HomeController>();

            Sprite logoArt = AssetDatabase.LoadAssetAtPath<Sprite>($"{FrameArtDir}/logo_alpi.png");
            Image logo = Img("Logo", canvas, logoArt, Color.white, TopCenter, new Vector2(0f, -16f), new Vector2(520f, 490f));
            logo.preserveAspect = true;

            Text gold = WalletPlate("Gold", canvas, TopLeft, new Vector2(24f, -30f), new Vector2(230f, 90f), GoldColor, false);
            Text diamonds = WalletPlate("Diamonds", canvas, TopLeft, new Vector2(24f, -130f), new Vector2(230f, 90f), DiamondColor, true);
            ClickToShop(gold, home.OpenShop);
            ClickToShop(diamonds, home.OpenShop);

            // Dost kartları: 5. kart ortada
            int count = Shop.MaxPets;
            var cards = new Image[count];
            var petGroups = new GameObject[count];
            var portraits = new Image[count];
            var names = new Text[count];
            var infos = new Text[count];
            var moods = new Image[count];
            var moodTexts = new Text[count];
            var newGroups = new GameObject[count];
            var lockGroups = new GameObject[count];
            var lockPrices = new Text[count];
            for (int i = 0; i < count; i++)
            {
                float x = i == 4 ? 0f : (i % 2 == 0 ? -262f : 262f);
                Image card = Img("PetCard" + i, canvas, roundRect, Color.white, Center, new Vector2(x, 300f - (i / 2) * 320f), new Vector2(505f, 300f));
                Card(card);
                card.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(card.gameObject.AddComponent<Button>().onClick, home.TapCard, i);
                Juice(card.gameObject, 0f);
                cards[i] = card;

                RectTransform pet = NewUI("Pet", card.transform);
                Stretch(pet);
                petGroups[i] = pet.gameObject;
                // Solda dostun sahnesi, sağda yazılar: resim yazıya binmez
                Color[] tints = { new Color32(0xD8, 0xF2, 0xE6, 255), new Color32(0xDD, 0xEE, 0xFB, 255), new Color32(0xFF, 0xEC, 0xD2, 255), new Color32(0xEC, 0xE2, 0xFA, 255), new Color32(0xFD, 0xE3, 0xEA, 255) };
                Image stageDisc = Img("StageDisc", pet, circle, tints[i % tints.Length], Center, new Vector2(-124f, 0f), new Vector2(232f, 232f));
                portraits[i] = Img("Portrait", pet, null, Color.white, Center, new Vector2(-124f, 2f), new Vector2(226f, 226f));
                portraits[i].preserveAspect = true;
                names[i] = Txt("Name", pet, "", 50, TextAnchor.MiddleLeft, Center, new Vector2(126f, 82f), new Vector2(226f, 66f));
                names[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                names[i].verticalOverflow = VerticalWrapMode.Truncate;
                names[i].resizeTextForBestFit = true;
                names[i].resizeTextMinSize = 30;
                names[i].resizeTextMaxSize = 50;
                infos[i] = Txt("Info", pet, "", 32, TextAnchor.MiddleLeft, Center, new Vector2(126f, 30f), new Vector2(226f, 44f));
                infos[i].color = new Color(Plum.r, Plum.g, Plum.b, 0.65f);
                // Ruh hali: ince renkli şeritte resim ve kısa adı
                Image pill = Img("MoodDisc", pet, roundRect, new Color(Mint.r, Mint.g, Mint.b, 0.22f), Center, new Vector2(126f, -62f), new Vector2(226f, 92f));
                Sliced(pill);
                moods[i] = Img("Mood", pill.transform, null, Color.white, Center, new Vector2(-74f, 0f), new Vector2(68f, 68f));
                moods[i].preserveAspect = true;
                moodTexts[i] = Txt("MoodText", pill.transform, "", 30, TextAnchor.MiddleLeft, Center, new Vector2(38f, 0f), new Vector2(142f, 84f));
                moodTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                moodTexts[i].verticalOverflow = VerticalWrapMode.Truncate;
                moodTexts[i].resizeTextForBestFit = true;
                moodTexts[i].resizeTextMinSize = 20;
                moodTexts[i].resizeTextMaxSize = 30;
                moodTexts[i].lineSpacing = 0.85f;

                RectTransform fresh = NewUI("New", card.transform);
                Stretch(fresh);
                newGroups[i] = fresh.gameObject;
                Image plus = Img("Plus", fresh, circle, Mint, Center, new Vector2(0f, 44f), new Vector2(150f, 150f));
                Jelly(plus, true);
                Juice(plus.gameObject, 0.1f);   // ilk bakılacak yer burası
                White(Txt("Label", plus.transform, "+", 110, TextAnchor.MiddleCenter, Center, new Vector2(0f, 10f), new Vector2(150f, 150f)));
                Localize(Txt("Text", fresh, Loc.Get("home_new_pal"), 50, TextAnchor.MiddleCenter, Center, new Vector2(0f, -86f), new Vector2(480f, 70f)), "home_new_pal");

                RectTransform locked = NewUI("Locked", card.transform);
                Stretch(locked);
                lockGroups[i] = locked.gameObject;
                Image padlock = Img("Lock", locked, IconSprite("ui_lock"), Color.white, Center, new Vector2(-120f, 0f), new Vector2(170f, 170f));
                padlock.preserveAspect = true;
                Localize(Txt("Text", locked, Loc.Get("home_open_slot"), 50, TextAnchor.MiddleCenter, Center, new Vector2(92f, 40f), new Vector2(290f, 70f)), "home_open_slot");
                lockPrices[i] = Txt("Price", locked, "", 34, TextAnchor.MiddleCenter, Center, new Vector2(92f, -30f), new Vector2(290f, 56f));
                lockPrices[i].color = Coral;
                lockPrices[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                lockPrices[i].verticalOverflow = VerticalWrapMode.Truncate;
                lockPrices[i].resizeTextForBestFit = true;
                lockPrices[i].resizeTextMinSize = 20;
                lockPrices[i].resizeTextMaxSize = 34;
            }

            // Alt: büyük gezinme butonları
            RectTransform nav = NewUI("Nav", canvas);
            Place(nav, BottomCenter, new Vector2(0f, 36f), new Vector2(1080f, 220f));
            Text tasks = ActionButton(nav, "nav_tasks", Mint, Slot(0, 4), home.OpenTasks);
            ButtonIcon(tasks, "act_star", true);
            Image tasksDot = Img("Dot", tasks.transform.parent, circle, Coral, Center, new Vector2(90f, 80f), new Vector2(64f, 64f));
            Text tasksBadge = White(Txt("Count", tasksDot.transform, "", 40, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(64f, 64f)));
            ButtonIcon(ActionButton(nav, "nav_shop", GoldColor, Slot(1, 4), home.OpenShop), "ui_shop", true);
            Text memories = ActionButton(nav, "nav_memories", new Color32(0xC9, 0xA7, 0xF2, 255), Slot(2, 4), home.OpenMemorial);
            ButtonIcon(memories, "badge_lifelong", true);
            ButtonIcon(ActionButton(nav, "nav_settings", SkyBlue, Slot(3, 4), home.OpenSettings), "ui_settings", true);

            SettingsPanel settings = BuildSettings(canvas);

            SetRef(home, "goldText", gold);
            SetRef(home, "diamondText", diamonds);
            SetArray(home, "cards", cards);
            SetArray(home, "petGroups", petGroups);
            SetArray(home, "portraits", portraits);
            SetArray(home, "names", names);
            SetArray(home, "infos", infos);
            SetArray(home, "moods", moods);
            SetArray(home, "moodTexts", moodTexts);
            SetArray(home, "newGroups", newGroups);
            SetArray(home, "lockGroups", lockGroups);
            SetArray(home, "lockPrices", lockPrices);
            SetRef(home, "tasksButton", tasks.transform.parent.GetComponent<Image>());
            SetRef(home, "tasksBadge", tasksBadge);
            SetRef(home, "memoriesButton", memories.transform.parent.GetComponent<Image>());
            SetRef(home, "settings", settings);

            EditorSceneManager.SaveScene(scene, ScenePath(HomeController.HomeScene));
        }

        private static Text WalletPlate(string name, Transform canvas, Vector2 pos, Color iconColor, bool diamond)
        {
            return WalletPlate(name, canvas, TopLeft, pos, new Vector2(260f, 100f), iconColor, diamond);
        }

        private static Text WalletPlate(string name, Transform canvas, Vector2 anchor, Vector2 pos, Vector2 size, Color iconColor, bool diamond)
        {
            Image plate = Img(name + "Plate", canvas, roundRect, new Color(1f, 1f, 1f, 0.92f), anchor, pos, size);
            Card(plate);
            Image icon = Img("Icon", plate.transform, diamond ? roundRect : circle, iconColor, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(64f, 64f));
            icon.rectTransform.pivot = new Vector2(0f, 0.5f);
            Sprite drawing = IconSprite(diamond ? "ui_diamond" : "ui_gold");
            if (drawing != null)
            {
                icon.sprite = drawing;
                icon.color = Color.white;
                icon.preserveAspect = true;
                icon.rectTransform.anchoredPosition = new Vector2(12f, 0f);
                icon.rectTransform.sizeDelta = new Vector2(76f, 76f);
            }
            // Elmas yer tutucusu: 45 derece dönmüş kare
            else if (diamond)
            {
                icon.rectTransform.sizeDelta = new Vector2(52f, 52f);
                icon.rectTransform.pivot = Center;
                icon.rectTransform.anchoredPosition = new Vector2(50f, 0f);
                icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            Text value = Txt("Value", plate.transform, "0", 54, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(100f, 2f), new Vector2(150f, 90f));
            value.rectTransform.pivot = new Vector2(0f, 0.5f);
            value.color = Plum;
            // Artınca altınlar uçarak gelir ve sayı tıkır tıkır artar
            var counter = value.gameObject.AddComponent<CurrencyCounter>();
            var so = new SerializedObject(counter);
            so.FindProperty("diamond").boolValue = diamond;
            so.FindProperty("icon").objectReferenceValue = icon.rectTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return value;
        }

        private static Image LanguageButton(string name, Transform canvas, string label, Vector2 pos, UnityAction onClick)
        {
            Image button = Img(name, canvas, circle, Color.white, TopRight, pos, new Vector2(120f, 120f));
            button.rectTransform.pivot = TopRight;
            button.raycastTarget = true;
            UnityEventTools.AddPersistentListener(button.gameObject.AddComponent<Button>().onClick, onClick);
            Juice(button.gameObject, 0f);
            Text text = Txt("Label", button.transform, label, 46, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(120f, 120f));
            text.color = Coral;
            return button;
        }

        // ---------- Sahiplenme ----------

        /// <summary>
        /// İki adım: (1) 10 türün kartı, yalnızca açık olanlar seçilebilir, diğerlerinde "Yakında";
        /// (2) ad, renk, zaman modu ve "Sahiplen".
        /// </summary>
        private static void BuildAdoptScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(Cream);
            Transform canvas = CreateCanvas();
            Background(canvas, "bg_barinak");
            var adopt = canvas.gameObject.AddComponent<AdoptController>();

            // Adım 1: tür kartları (3 sütun)
            RectTransform speciesStep = NewUI("SpeciesStep", canvas);
            Stretch(speciesStep);
            Localize(Title(speciesStep, "adopt_title"), "adopt_title");
            BackButton(speciesStep, adopt.Back);
            Color[] cardColors = { Coral, Mint, SkyBlue, GoldColor, new Color32(0xC9, 0xA7, 0xF2, 255) };
            int count = SpeciesCatalog.All.Length;
            var packLocks = new Object[count];
            for (int i = 0; i < count; i++)
            {
                Species species = SpeciesCatalog.All[i];
                int row = i / 3, col = i % 3;
                // Son satırda tek kart kalırsa ortalanır
                float x = row == (count - 1) / 3 && count % 3 == 1 ? 0f : (col - 1) * 330f;
                Image card = Img("Card_" + species.id, speciesStep, roundRect, new Color(1f, 1f, 1f, 0.92f), Center,
                    new Vector2(x, 600f - row * 272f), new Vector2(300f, 250f));
                Card(card);
                card.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(card.gameObject.AddComponent<Button>().onClick, adopt.SelectSpecies, i);
                Juice(card.gameObject, 0.02f);

                // Türün çizimi (yavru, normal poz) varsa onun resmi; yoksa renkli harf rozeti
                string name = Loc.Get(species.NameKey);
                Sprite petArt = AssetDatabase.LoadAssetAtPath<Sprite>($"{PetArtDir}/{species.id}_normal.png");
                if (petArt != null)
                {
                    Img("Glow", card.transform, circle, new Color(cardColors[i % cardColors.Length].r, cardColors[i % cardColors.Length].g, cardColors[i % cardColors.Length].b, 0.35f),
                        Center, new Vector2(0f, 10f), new Vector2(190f, 190f));
                    Image pic = Img("Pet", card.transform, petArt, Color.white, Center, new Vector2(0f, 26f), new Vector2(230f, 230f));
                    pic.preserveAspect = true;
                }
                else
                {
                    Image badge = Img("Badge", card.transform, circle, cardColors[i % cardColors.Length], Center, new Vector2(0f, 30f), new Vector2(140f, 140f));
                    White(Txt("Letter", badge.transform, name.Substring(0, 1), 84, TextAnchor.MiddleCenter, Center, new Vector2(0f, 4f), new Vector2(140f, 140f)));
                }
                Text label = Txt("Name", card.transform, name, 32, TextAnchor.MiddleCenter, Center, new Vector2(0f, -95f), new Vector2(290f, 50f));
                Localize(label, species.NameKey);

                // Paketli türler (Dino Paketi alınana kadar kilitli; AdoptController açar/kapatır): asma kilit resmi
                if (!string.IsNullOrEmpty(species.pack))
                {
                    Image dim = Img("PackLock", card.transform, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.55f), Center, new Vector2(0f, 4f), new Vector2(284f, 236f));
                    Sliced(dim);
                    Sprite lockIcon = IconSprite("ui_lock");
                    if (lockIcon != null)
                    {
                        Image padlock = Img("Lock", dim.transform, lockIcon, Color.white, Center, new Vector2(0f, 20f), new Vector2(130f, 130f));
                        padlock.preserveAspect = true;
                    }
                    else Localize(White(Txt("Label", dim.transform, Loc.Get("dino_locked"), 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, 30f), new Vector2(290f, 80f))), "dino_locked");
                    packLocks[i] = dim.gameObject;
                }
            }
            var locks = new SerializedObject(adopt);
            SerializedProperty lockArray = locks.FindProperty("packLocks");
            lockArray.arraySize = count;
            for (int i = 0; i < count; i++) lockArray.GetArrayElementAtIndex(i).objectReferenceValue = packLocks[i];
            locks.ApplyModifiedPropertiesWithoutUndo();

            // Adım 2: ayrıntılar
            RectTransform details = NewUI("DetailsStep", canvas);
            Stretch(details);
            Text speciesTitle = Title(details, "adopt_details");
            BackButton(details, adopt.ShowSpecies);

            Image preview = Img("Preview", details, circle, Coral, Center, new Vector2(0f, 560f), new Vector2(260f, 260f));
            Paw("Paw", preview.transform, Vector2.zero, 0.9f, new Color(1f, 1f, 1f, 0.6f));
            // Türün çizimi varsa denetleyici yuvarlak yerine bunu gösterir
            Image previewArt = Img("Art", preview.transform, null, Color.white, Center, new Vector2(0f, 20f), new Vector2(360f, 360f));
            previewArt.preserveAspect = true;
            previewArt.gameObject.SetActive(false);

            Localize(Label(details, "adopt_name", 360f), "adopt_name");
            InputField nameInput = NameInput(details, new Vector2(-60f, 270f));
            Image dice = Img("Dice", details, roundRect, SkyBlue, Center, new Vector2(370f, 270f), new Vector2(120f, 120f));
            Sliced(dice);
            dice.raycastTarget = true;
            UnityEventTools.AddPersistentListener(dice.gameObject.AddComponent<Button>().onClick, adopt.RandomName);
            Juice(dice.gameObject, 0f);
            White(Txt("Label", dice.transform, "?", 70, TextAnchor.MiddleCenter, Center, new Vector2(0f, 4f), new Vector2(120f, 120f)));

            Localize(Label(details, "adopt_color", 140f), "adopt_color");
            var swatches = new Image[PetLooks.ColorCount];
            for (int i = 0; i < swatches.Length; i++)
            {
                float x = (i - (swatches.Length - 1) * 0.5f) * 160f;
                Image ring = Img("Swatch" + i, details, circle, Color.white, Center, new Vector2(x, 40f), new Vector2(130f, 130f));
                ring.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(ring.gameObject.AddComponent<Button>().onClick, adopt.SelectColor, i);
                Img("Color", ring.transform, circle, PetLooks.ColorOf("cat", i), Center, Vector2.zero, new Vector2(106f, 106f));
                // Özel Renkler paketiyle açılanlar: kilit yazısı (AdoptController ikinci çocuğu açıp kapatır)
                if (i >= PetLooks.FreeColors)
                {
                    Image locked = Img("Locked", ring.transform, circle, new Color(Plum.r, Plum.g, Plum.b, 0.5f), Center, Vector2.zero, new Vector2(106f, 106f));
                    // Yazısız: asma kilit (okumayan çocuk da anlar)
                    Sprite lockIcon = IconSprite("ui_lock");
                    if (lockIcon != null)
                    {
                        Image padlock = Img("Lock", locked.transform, lockIcon, Color.white, Center, Vector2.zero, new Vector2(70f, 70f));
                        padlock.preserveAspect = true;
                    }
                    else White(Txt("Label", locked.transform, Loc.Get("product_premium_colors"), 22, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(100f, 100f)))
                        .horizontalOverflow = HorizontalWrapMode.Wrap;
                }
                swatches[i] = ring;
            }

            Localize(Label(details, "adopt_mode", -100f), "adopt_mode");
            Image realCard = ModeCard(details, "RealCard", "mode_real", new Vector2(-240f, -290f), adopt.SelectReal, out Text realDesc);
            Image fastCard = ModeCard(details, "FastCard", "mode_fast", new Vector2(240f, -290f), adopt.SelectFast, out Text fastDesc);
            Text note = Txt("Note", details, Loc.Get("adopt_mode_note"), 38, TextAnchor.MiddleCenter, Center, new Vector2(0f, -470f), new Vector2(1000f, 60f));
            note.color = Coral;
            Localize(note, "adopt_mode_note");

            BigButton("Confirm", details, "adopt", Coral, new Vector2(0f, -660f), new Vector2(660f, 170f), adopt.Confirm);

            SetRef(adopt, "speciesStep", speciesStep.gameObject);
            SetRef(adopt, "detailsStep", details.gameObject);
            SetRef(adopt, "speciesTitle", speciesTitle);
            SetRef(adopt, "nameInput", nameInput);
            SetArray(adopt, "colorSwatches", swatches);
            SetRef(adopt, "preview", preview);
            SetRef(adopt, "previewArt", previewArt);
            SetRef(adopt, "realCard", realCard);
            SetRef(adopt, "fastCard", fastCard);
            SetRef(adopt, "realDesc", realDesc);
            SetRef(adopt, "fastDesc", fastDesc);

            EditorSceneManager.SaveScene(scene, ScenePath(AdoptController.AdoptScene));
        }

        private static Text Title(Transform parent, string key)
        {
            Text title = Txt("Title", parent, Loc.Get(key), 84, TextAnchor.MiddleCenter, TopCenter, new Vector2(0f, -190f), new Vector2(900f, 130f));
            title.color = Plum;
            return title;
        }

        private static Text Label(Transform parent, string key, float y)
        {
            Text label = Txt("Label_" + key, parent, Loc.Get(key), 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, y), new Vector2(800f, 70f));
            label.color = new Color(Plum.r, Plum.g, Plum.b, 0.8f);
            return label;
        }

        private static void BackButton(Transform parent, UnityAction onClick)
        {
            Image back = Img("Back", parent, circle, Color.white, TopLeft, new Vector2(24f, -60f), new Vector2(150f, 150f));
            Jelly(back, true);
            back.raycastTarget = true;
            UnityEventTools.AddPersistentListener(back.gameObject.AddComponent<Button>().onClick, onClick);
            Juice(back.gameObject, 0f);
            Text arrow = Txt("Label", back.transform, "<", 104, TextAnchor.MiddleCenter, Center, new Vector2(-4f, 8f), new Vector2(140f, 140f));
            arrow.color = Coral;
        }

        private static InputField NameInput(Transform parent, Vector2 pos)
        {
            Image field = Img("NameInput", parent, roundRect, Color.white, Center, pos, new Vector2(560f, 120f));
            Sliced(field);
            field.raycastTarget = true;
            Text text = Txt("Text", field.transform, "", 60, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(520f, 110f));
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            Text placeholder = Txt("Placeholder", field.transform, "Pamuk", 60, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(520f, 110f));
            placeholder.color = new Color(Plum.r, Plum.g, Plum.b, 0.35f);
            var input = field.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.characterLimit = 14;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private static Image ModeCard(Transform parent, string name, string titleKey, Vector2 pos, UnityAction onClick, out Text desc)
        {
            Image card = Img(name, parent, roundRect, Color.white, Center, pos, new Vector2(440f, 270f));
            Sliced(card);
            card.raycastTarget = true;
            UnityEventTools.AddPersistentListener(card.gameObject.AddComponent<Button>().onClick, onClick);
            Localize(Txt("Title", card.transform, Loc.Get(titleKey), 58, TextAnchor.MiddleCenter, Center, new Vector2(0f, 70f), new Vector2(420f, 80f)), titleKey);
            desc = Txt("Desc", card.transform, "", 34, TextAnchor.MiddleCenter, Center, new Vector2(0f, -40f), new Vector2(410f, 130f));
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            return card;
        }

        // ---------- Dostun odası ----------

        private static readonly Color[] RoomColors =
        {
            new Color32(0xFF, 0xE3, 0xC8, 255),   // mutfak
            new Color32(0xD6, 0xEE, 0xFA, 255),   // banyo
            new Color32(0xE3, 0xDA, 0xF5, 255),   // yatak odası
            new Color32(0xD5, 0xF2, 0xE3, 255),   // oyun
            new Color32(0xFF, 0xF1, 0xC4, 255),   // okul
            new Color32(0xF9, 0xDD, 0xE8, 255),   // dolap
            new Color32(0xF2, 0xF5, 0xF7, 255),   // veteriner
        };

        /// <summary>
        /// Üstte ad, yaş ve 9 ölçüm; ortada dost, balon ve yerdeki kaka; altta aksiyonlar ve oda sekmeleri.
        /// </summary>
        private static void BuildPetScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(RoomColors[0]);
            Transform canvas = CreateCanvas();
            Image roomBg = Img("RoomBackground", canvas, null, RoomColors[0], Center, Vector2.zero, Vector2.zero);
            Stretch(roomBg.rectTransform);
            // Oda çizimi (9:16) ekranı kırpmadan doldursun: uzun telefonda yanlardan taşar
            var roomFitter = roomBg.gameObject.AddComponent<AspectRatioFitter>();
            roomFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            roomFitter.aspectRatio = 9f / 16f;
            Image floor = Img("Floor", canvas, circle, new Color(1f, 1f, 1f, 0.45f), Center, new Vector2(0f, -330f), new Vector2(1000f, 260f));
            // Uyurken odayı karartan gece katmanı (denetleyici saydamlığını değiştirir); dost bunun önünde kalır
            Image night = Img("Night", canvas, null, new Color(0.09f, 0.1f, 0.28f, 0f), Center, Vector2.zero, Vector2.zero);
            Stretch(night.rectTransform);
            night.enabled = false;   // gündüz hiç çizilmez (tam ekran saydam katman boşuna boyanmasın)

            var controller = canvas.gameObject.AddComponent<PetController>();

            // Üst: ad, bilgi, cüzdan, dil
            // Ad ve bilgi satırının arkasında yarı saydam şerit: çizilmiş odanın önünde de okunsun
            // Sol üst: Ana Sayfa (adı Back: telefonun geri tuşu da buna basar)
            Image homeButton = Img("Back", canvas, circle, Color.white, TopLeft, new Vector2(22f, -24f), new Vector2(140f, 140f));
            Jelly(homeButton, true);
            homeButton.raycastTarget = true;
            UnityEventTools.AddPersistentListener(homeButton.gameObject.AddComponent<Button>().onClick, controller.GoHome);
            Juice(homeButton.gameObject, 0f);
            Image homeIcon = Img("Icon", homeButton.transform, IconSprite("ui_home"), Color.white, Center, new Vector2(0f, 8f), new Vector2(104f, 104f));
            homeIcon.preserveAspect = true;
            // Yazı butonun çocuğu ve dokunulabilir: çocuk yazıya basınca da çalışır
            Text homeLabel = Localize(Outlined(Txt("HomeLabel", homeButton.transform, Loc.Get("nav_home"), 30, TextAnchor.MiddleCenter, Center, new Vector2(0f, -92f), new Vector2(180f, 44f))), "nav_home");
            homeLabel.raycastTarget = true;

            Image headerPlate = Img("HeaderPlate", canvas, roundRect, new Color(Cream.r, Cream.g, Cream.b, 0.85f), TopLeft, new Vector2(176f, -36f), new Vector2(510f, 172f));
            Sliced(headerPlate);
            Text nameText = Txt("Name", canvas, "Pamuk", 72, TextAnchor.MiddleLeft, TopLeft, new Vector2(196f, -46f), new Vector2(480f, 96f));
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            nameText.resizeTextForBestFit = true;
            nameText.resizeTextMinSize = 36;
            nameText.resizeTextMaxSize = 72;
            nameText.color = Plum;
            Text infoText = Txt("Info", canvas, "", 32, TextAnchor.MiddleLeft, TopLeft, new Vector2(42f, -140f), new Vector2(690f, 60f));
            infoText.color = new Color(Plum.r, Plum.g, Plum.b, 0.75f);
            Text gold = WalletPlate("Gold", canvas, TopRight, new Vector2(-160f, -36f), new Vector2(210f, 84f), GoldColor, false);
            Text diamonds = WalletPlate("Diamonds", canvas, TopRight, new Vector2(-160f, -130f), new Vector2(210f, 84f), DiamondColor, true);
            // Cüzdana dokununca mağaza
            ClickToShop(gold, controller.OpenShop);
            ClickToShop(diamonds, controller.OpenShop);
            // Mağaza
            Image shopButton = Img("ShopButton", canvas, circle, Color.white, TopRight, new Vector2(-30f, -150f), new Vector2(110f, 110f));
            shopButton.raycastTarget = true;
            UnityEventTools.AddPersistentListener(shopButton.gameObject.AddComponent<Button>().onClick, controller.OpenShop);
            Juice(shopButton.gameObject, 0f);
            Image shopIcon = Img("Icon", shopButton.transform, IconSprite("ui_shop"), Color.white, Center, Vector2.zero, new Vector2(84f, 84f));
            shopIcon.preserveAspect = true;
            // Ayarlar butonu (çark); panel ve ebeveyn kapısı en sonda, her şeyin üstünde kurulur
            Image settingsButton = Img("SettingsButton", canvas, circle, Color.white, TopRight, new Vector2(-30f, -36f), new Vector2(110f, 110f));
            settingsButton.raycastTarget = true;
            Juice(settingsButton.gameObject, 0f);
            Image settingsIcon = Img("Icon", settingsButton.transform, IconSprite("ui_settings"), Color.white, Center, Vector2.zero, new Vector2(84f, 84f));
            settingsIcon.preserveAspect = true;

            // Durum: ikon çizimleri varsa tek satır ikon halkası (6), yoksa eski 3x3 çubuk
            bool ringArt = IconSprite(PetController.RingIcons[0]) != null;
            Image[] fills;
            RectTransform[] rings = null;
            float statusY;
            if (ringArt)
            {
                // Üstteki "Dostlarım" butonunun (y -150..-250) altından başlar
                Image strip = Img("Stats", canvas, roundRect, new Color(1f, 1f, 1f, 0.78f), TopCenter, new Vector2(0f, -258f), new Vector2(1030f, 180f));
                Card(strip);
                Isolate(strip.gameObject);   // azalan halka nabız gibi atar
                fills = new Image[PetController.RingStats.Length];
                rings = new RectTransform[fills.Length];
                for (int i = 0; i < fills.Length; i++)
                {
                    RectTransform cell = NewUI("Ring_" + PetController.StatKeys[PetController.RingStats[i]], strip.transform);
                    Place(cell, Center, new Vector2((i - 2.5f) * 166f, 4f), new Vector2(150f, 150f));
                    Image disc = Img("Disc", cell, circle, new Color(1f, 1f, 1f, 0.95f), Center, Vector2.zero, new Vector2(150f, 150f));
                    // Dokununca o ihtiyacın odasına gider
                    disc.raycastTarget = true;
                    UnityEventTools.AddIntPersistentListener(disc.gameObject.AddComponent<Button>().onClick, controller.RingTap, i);
                    Img("Track", cell, ringSprite, new Color(Plum.r, Plum.g, Plum.b, 0.13f), Center, Vector2.zero, new Vector2(150f, 150f));
                    Image fill = Img("Fill", cell, ringSprite, Mint, Center, Vector2.zero, new Vector2(150f, 150f));
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Radial360;
                    fill.fillOrigin = (int)Image.Origin360.Top;
                    fill.fillClockwise = false;
                    fill.fillAmount = 0.7f;
                    Image icon = Img("Icon", cell, IconSprite(PetController.RingIcons[i]), Color.white, Center, new Vector2(0f, 2f), new Vector2(92f, 92f));
                    icon.preserveAspect = true;
                    fills[i] = fill;
                    rings[i] = cell;
                }
                statusY = -452f;
            }
            else
            {
                Image panel = Img("Stats", canvas, roundRect, new Color(1f, 1f, 1f, 0.85f), TopCenter, new Vector2(0f, -255f), new Vector2(1020f, 305f));
                Sliced(panel);
                fills = new Image[PetController.StatKeys.Length];
                for (int i = 0; i < fills.Length; i++)
                {
                    int row = i / 3, col = i % 3;
                    RectTransform cell = NewUI("Stat_" + PetController.StatKeys[i], panel.transform);
                    Place(cell, Center, new Vector2((col - 1) * 330f, 98f - row * 98f), new Vector2(310f, 92f));
                    Text label = Txt("Label", cell, Loc.Get(PetController.StatKeys[i]), 32, TextAnchor.MiddleLeft, Center, new Vector2(0f, 22f), new Vector2(300f, 46f));
                    Localize(label, PetController.StatKeys[i]);
                    Image track = Img("Track", cell, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.12f), Center, new Vector2(0f, -22f), new Vector2(300f, 30f));
                    Sliced(track);
                    Image fill = Img("Fill", track.transform, roundRect, Mint, Center, Vector2.zero, new Vector2(300f, 30f));
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                    fill.fillAmount = 0.7f;
                    fills[i] = fill;
                }
                statusY = -568f;
            }
            Image statusPlate = Img("StatusPlate", canvas, roundRect, new Color(1f, 1f, 1f, 0.8f), TopCenter, new Vector2(0f, statusY), new Vector2(1020f, 58f));
            Sliced(statusPlate);
            Text weightText = Txt("Weight", canvas, "", 34, TextAnchor.MiddleCenter, TopCenter, new Vector2(0f, statusY - 4f), new Vector2(1000f, 50f));
            weightText.color = new Color(Plum.r, Plum.g, Plum.b, 0.85f);
            // Yazısız ekran: bilgi satırı ve durum yazısı yerine sağ kenarda küçük ikonlar (aşılı, hasta, pansiyon, etkinlik)
            GameObject[] statusIcons = null;
            if (ringArt)
            {
                statusPlate.gameObject.SetActive(false);
                weightText.gameObject.SetActive(false);
                infoText.gameObject.SetActive(false);
                headerPlate.rectTransform.sizeDelta = new Vector2(510f, 116f);
                RectTransform row = NewUI("StatusIcons", canvas);
                Place(row, TopLeft, new Vector2(24f, statusY - 10f), new Vector2(400f, 84f));
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.spacing = 10f;
                layout.childControlWidth = layout.childControlHeight = false;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                string[] keys = { "act_vaccine", "act_treat", "ui_calendar", "ui_gift" };
                statusIcons = new GameObject[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    Image disc = Img("Status_" + keys[i], row, circle, new Color(1f, 1f, 1f, 0.9f), Center, Vector2.zero, new Vector2(96f, 96f));
                    Image mark = Img("Icon", disc.transform, IconSprite(keys[i]), Color.white, Center, Vector2.zero, new Vector2(74f, 74f));
                    mark.preserveAspect = true;
                    statusIcons[i] = disc.gameObject;
                }
            }

            // Kritik uyarı: yumuşak mercan jöle, sağlık ikonu, nabız gibi atar; dokununca veterinere
            Image banner = Img("CriticalBanner", canvas, roundRect, new Color32(0xFF, 0x7A, 0x6B, 255), TopCenter,
                new Vector2(ringArt ? 170f : 0f, statusY - 64f), new Vector2(ringArt ? 420f : 940f, 110f));   // yazısızda sağda: soldaki durum ikonlarına binmesin
            Jelly(banner);
            banner.raycastTarget = true;
            UnityEventTools.AddIntPersistentListener(banner.gameObject.AddComponent<Button>().onClick, controller.ShowRoom, 6);
            Juice(banner.gameObject, 0.03f);
            // Yazısız: sağlık kalbi + kum saati + kalan saat sayısı
            Text bannerText = ringArt
                ? White(Txt("Text", banner.transform, "", 58, TextAnchor.MiddleLeft, Center, new Vector2(110f, 6f), new Vector2(150f, 90f)))
                : White(Txt("Text", banner.transform, "", 40, TextAnchor.MiddleCenter, Center, new Vector2(40f, 6f), new Vector2(800f, 90f)));
            Sprite healthIcon = IconSprite("stat_health");
            if (healthIcon != null)
            {
                Image heart = Img("Icon", banner.transform, healthIcon, Color.white, Center, new Vector2(ringArt ? -140f : -400f, 6f), new Vector2(84f, 84f));
                heart.preserveAspect = true;
            }
            Sprite hourglass = IconSprite("ui_time");
            if (ringArt && hourglass != null)
            {
                Image clock = Img("Time", banner.transform, hourglass, Color.white, Center, new Vector2(-30f, 6f), new Vector2(76f, 76f));
                clock.preserveAspect = true;
            }

            // Orta: balon, dost, sevme alanı, kaka, geri bildirim
            Image bubble;
            Text bubbleText;
            Image bubbleIcon = null;
            if (ringArt)
            {
                // Düşünce balonu: yuvarlak bulut, iki küçük kabarcık kuyruk, ortada ruh halinin resmi
                bubble = Img("Bubble", canvas, circle, Color.white, Center, new Vector2(150f, 300f), new Vector2(180f, 180f));
                Img("Tail1", bubble.transform, circle, Color.white, Center, new Vector2(-78f, -82f), new Vector2(48f, 48f));
                Img("Tail2", bubble.transform, circle, Color.white, Center, new Vector2(-112f, -118f), new Vector2(28f, 28f));
                bubbleIcon = Img("Icon", bubble.transform, null, Color.white, Center, Vector2.zero, new Vector2(128f, 128f));
                bubbleIcon.preserveAspect = true;
                bubbleText = Txt("Text", bubble.transform, "", 30, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(170f, 170f));
                // Dokununca dostun istediği yere gider
                bubble.raycastTarget = true;
                UnityEventTools.AddPersistentListener(bubble.gameObject.AddComponent<Button>().onClick, controller.GoToNeed);
            }
            else
            {
                bubble = Img("Bubble", canvas, roundRect, Color.white, Center, new Vector2(0f, 300f), new Vector2(700f, 120f));
                Sliced(bubble);
                bubbleText = Txt("Text", bubble.transform, "", 46, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(680f, 110f));
            }

            PetView view = Critter(canvas, new Vector2(0f, -430f));
            Isolate(view.gameObject);     // dost her karede nefes alır, göz kırpar
            Isolate(bubble.gameObject);   // balon süzülür
            // Veteriner panda (yalnızca veteriner odasında; denetleyici açar). Ayakları dostunkiyle aynı hizada.
            Image vetPanda = null;
            Sprite panda = AssetDatabase.LoadAssetAtPath<Sprite>($"{PetArtDir}/vet_wave.png");
            if (panda != null)
            {
                vetPanda = Img("VetPanda", canvas, panda, Color.white, Center, new Vector2(-350f, -430f + 190f), new Vector2(380f, 380f));
                vetPanda.preserveAspect = true;
                vetPanda.gameObject.SetActive(false);
            }
            Image strokeArea = Img("StrokeArea", canvas, null, new Color(1f, 1f, 1f, 0f), Center, new Vector2(0f, -170f), new Vector2(560f, 560f));
            strokeArea.raycastTarget = true;
            UnityEventTools.AddPersistentListener(strokeArea.gameObject.AddComponent<Button>().onClick, controller.Stroke);
            // Parmak gezdirince: banyoda köpükle yıkama, diğer odalarda okşama
            SetRef(strokeArea.gameObject.AddComponent<PetRub>(), "controller", controller);

            RectTransform poop = NewUI("Poop", canvas);
            Place(poop, Center, new Vector2(330f, -400f), new Vector2(100f, 100f));
            Color brown = new Color32(0x8A, 0x5A, 0x3A, 255);
            Part("Base", poop, circle, brown, new Vector2(0f, 25f), new Vector2(100f, 54f), 0f, Center);
            Part("Middle", poop, circle, brown, new Vector2(0f, 55f), new Vector2(74f, 44f), 0f, Center);
            Part("Top", poop, circle, brown, new Vector2(0f, 80f), new Vector2(44f, 32f), 0f, Center);

            // Dostun önündeki katman: yemek kabı, top, köpük (PetStage)
            RectTransform stageFront = NewUI("StageFront", canvas);
            Stretch(stageFront);
            Isolate(stageFront.gameObject);
            var stage = canvas.gameObject.AddComponent<PetStage>();
            SetRef(stage, "view", view);
            SetRef(stage, "petRoot", view.transform);
            SetRef(stage, "shadow", floor.rectTransform);
            SetRef(stage, "strokeArea", strokeArea.rectTransform);
            SetRef(stage, "front", stageFront);

            Text feedback = Txt("Feedback", canvas, "", 70, TextAnchor.MiddleCenter, Center, new Vector2(0f, 170f), new Vector2(900f, 110f));
            feedback.color = Coral;
            var shadow = feedback.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(1f, 1f, 1f, 0.9f);
            shadow.effectDistance = new Vector2(0f, -4f);

            // Sağ kenar: Merkez (görev sayısıyla) ve Foto
            Image hubButton = SideButton(canvas, "Hub", "hub_button", GoldColor, new Vector2(440f, 300f), controller.OpenHub);
            Image hubDot = Img("Dot", hubButton.transform, circle, Coral, Center, new Vector2(48f, 48f), new Vector2(54f, 54f));
            Juice(hubDot.gameObject, 0.14f);   // alınacak görev varken nabız gibi atar
            Text hubBadge = White(Txt("Count", hubDot.transform, "", 34, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(54f, 54f)));
            Image photoButton = SideButton(canvas, "Photo", "act_photo", SkyBlue, new Vector2(440f, 90f), controller.OpenPhoto);
            Image breedButton = SideButton(canvas, "Breed", "nav_baby", Coral, new Vector2(440f, -120f), controller.OpenBreed);

            // Alt: oda sekmeleri ve her odanın aksiyonları
            // Çocuk parmağına göre büyük: sekme çubuğu 230, aksiyon butonları 250x210
            Image tabBar = Img("TabBar", canvas, null, new Color(Plum.r, Plum.g, Plum.b, 0.92f), BottomCenter, Vector2.zero, new Vector2(1080f, TabBarHeight));
            tabBar.rectTransform.anchorMin = new Vector2(0f, 0f);
            tabBar.rectTransform.anchorMax = new Vector2(1f, 0f);
            tabBar.rectTransform.sizeDelta = new Vector2(0f, TabBarHeight);
            // 7 oda (PetController.Room sırası)
            string[] roomKeys = { "room_kitchen", "room_bath", "room_bed", "room_play", "room_school", "room_closet", "room_vet" };
            var tabs = new Image[roomKeys.Length];
            var panels = new GameObject[roomKeys.Length];
            const float tabStep = 1080f / 7f;
            string[] tabIcons = { "food_pet_food", "act_bathe", "act_sleep", "act_play", "act_school", "cloth_party_hat", "act_treat" };
            for (int i = 0; i < roomKeys.Length; i++)
            {
                Image tab = Img("Tab_" + roomKeys[i], tabBar.transform, roundRect, Color.white, BottomCenter, new Vector2((i - 3f) * tabStep, 18f), new Vector2(tabStep - 6f, 190f));
                Sliced(tab);
                tab.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(tab.gameObject.AddComponent<Button>().onClick, controller.ShowRoom, i);
                Image swatch = Img("Color", tab.transform, roundRect, RoomColors[i], Center, new Vector2(0f, 0f), new Vector2(tabStep - 18f, 174f));
                Jelly(swatch);
                Text tabLabel = Txt("Label", tab.transform, Loc.Get(roomKeys[i]), 27, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(tabStep - 10f, 120f));
                Localize(tabLabel, roomKeys[i]);
                // Sekme yazısız: odanın resmi yeter
                Sprite tabSprite = IconSprite(tabIcons[i]);
                if (tabSprite != null)
                {
                    // Resim ve altında odanın adı
                    Image icon = Img("Icon", tab.transform, tabSprite, Color.white, Center, new Vector2(0f, 26f), new Vector2(116f, 116f));
                    icon.preserveAspect = true;
                    tabLabel.rectTransform.anchoredPosition = new Vector2(0f, -60f);
                    tabLabel.rectTransform.sizeDelta = new Vector2(tabStep - 12f, 48f);
                    tabLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    tabLabel.verticalOverflow = VerticalWrapMode.Truncate;
                    tabLabel.resizeTextForBestFit = true;
                    tabLabel.resizeTextMinSize = 22;
                    tabLabel.resizeTextMaxSize = 34;
                    tabLabel.transform.SetAsLastSibling();
                }
                tabs[i] = tab;

                RectTransform roomPanel = NewUI("Room_" + roomKeys[i], canvas);
                Place(roomPanel, BottomCenter, new Vector2(0f, TabBarHeight + 24f), new Vector2(1080f, 220f));
                panels[i] = roomPanel.gameObject;
            }

            // Mutfak: 3 yiyecek yeri (adı ve fiyatı türe göre denetleyiciden) ve su
            Color kitchen = new Color32(0xF2, 0x8C, 0x5A, 255);
            var foodButtons = new GameObject[3];
            var foodLabels = new Text[3];
            var foodIcons = new Image[3];
            var foodPrices = new Text[3];
            for (int i = 0; i < foodButtons.Length; i++)
            {
                foodLabels[i] = Dynamic(ActionButton(panels[0].transform, "food_pet_food", kitchen, Slot(i, 4), null, controller.FeedFood, i));
                foodButtons[i] = foodLabels[i].transform.parent.gameObject;
                foodIcons[i] = ButtonIcon(foodLabels[i], "food_pet_food", true);   // türün yiyeceğini denetleyici koyar
                foodPrices[i] = PriceChip(foodLabels[i], false);
                DragOntoPet(foodButtons[i], foodIcons[i], strokeArea.rectTransform);
            }
            Text waterLabel = ActionButton(panels[0].transform, "act_water", SkyBlue, Slot(3, 4), controller.Water);
            DragOntoPet(waterLabel.transform.parent.gameObject, ButtonIcon(waterLabel, "food_water", true), strokeArea.rectTransform);

            Color bath = new Color32(0x4F, 0xA8, 0xE0, 255);
            Text batheLabel = ActionButton(panels[1].transform, "act_bathe", bath, Slot(0, 4), controller.Bathe);
            ButtonIcon(batheLabel, "act_bathe", true);
            Text brushLabel = ActionButton(panels[1].transform, "act_brush", bath, Slot(1, 4), controller.Brush);
            ButtonIcon(brushLabel, "act_brush", true);
            Text toiletLabel = ActionButton(panels[1].transform, "act_toilet", new Color32(0xB0, 0x8A, 0x60, 255), Slot(2, 4), controller.Toilet);
            ButtonIcon(toiletLabel, "act_toilet", true);
            Text cleanLabel = ActionButton(panels[1].transform, "act_clean", Coral, Slot(3, 4), controller.Clean);
            ButtonIcon(cleanLabel, "act_clean", true);

            Text sleepLabel = ActionButton(panels[2].transform, "act_sleep", new Color32(0x8A, 0x74, 0xC9, 255), Slot(0, 1), controller.ToggleSleep);
            sleepLabel.GetComponent<LocalizedText>().enabled = false;   // "Uyut"/"Uyandır" denetleyiciden gelir
            Image sleepIcon = ButtonIcon(sleepLabel, "act_sleep", true);

            Text playLabel = ActionButton(panels[3].transform, "act_play", Mint, Slot(0, 4), controller.Play);
            ButtonIcon(playLabel, "act_play", true);
            ButtonIcon(ActionButton(panels[3].transform, "act_stroke", Coral, Slot(1, 4), controller.Stroke), "act_stroke", true);
            Text walkLabel = ActionButton(panels[3].transform, "act_walk", SkyBlue, Slot(2, 4), controller.OpenWalk);
            ButtonIcon(walkLabel, "ui_paw", true);
            ButtonIcon(ActionButton(panels[3].transform, "act_games", GoldColor, Slot(3, 4), controller.OpenGames), "act_games", true);

            // Okul: türün 3 komutu (ilerlemesiyle) ve kitap
            // Yazısız: her komutun kendi rengi, altta öğrenme çubuğu, öğrenince köşede altın yıldız
            Color[] schoolColors = { new Color32(0xE8, 0xA8, 0x3A, 255), new Color32(0x5C, 0xB8, 0xE8, 255), new Color32(0x6C, 0xC8, 0x8A, 255) };
            var commandLabels = new Text[3];
            var commandBars = new Image[3];
            var commandStars = new GameObject[3];
            var commandIcons = new Image[3];
            for (int i = 0; i < commandLabels.Length; i++)
            {
                commandLabels[i] = Dynamic(ActionButton(panels[4].transform, "cmd_sit", schoolColors[i], Slot(i, 4), null, controller.Command, i));
                // İkon türün komutuna göre oyunda değişir (PetController.CommandIcon); kep yalnızca başlangıç resmi
                commandIcons[i] = ButtonIcon(commandLabels[i], "act_school", true);
                bool iconOnly = commandIcons[i] != null;
                if (!iconOnly) continue;
                RectTransform button = (RectTransform)commandLabels[i].transform.parent;
                Image track = Img("Track", button, roundRect, new Color(1f, 1f, 1f, 0.45f), Center, new Vector2(0f, 88f), new Vector2(190f, 22f));
                Sliced(track);
                commandBars[i] = Img("Fill", track.transform, roundRect, new Color32(0xFF, 0xD8, 0x3C, 255), Center, Vector2.zero, new Vector2(190f, 22f));
                commandBars[i].type = Image.Type.Filled;
                commandBars[i].fillMethod = Image.FillMethod.Horizontal;
                commandBars[i].fillOrigin = (int)Image.OriginHorizontal.Left;
                Image star = Img("Learned", button, IconSprite("fx_star"), Color.white, Center, new Vector2(95f, 80f), new Vector2(80f, 80f));
                star.preserveAspect = true;
                commandStars[i] = star.gameObject;
            }
            Text book = ActionButton(panels[4].transform, "act_book", new Color32(0x8A, 0x74, 0xC9, 255), Slot(3, 4), controller.ReadBook);
            book.fontSize = 38;
            book.lineSpacing = 0.9f;
            ButtonIcon(book, "act_book", true);
            if (commandBars[0] != null)
            {
                SetArray(controller, "commandBars", commandBars);
                SetArray(controller, "commandStars", commandStars);
                SetArray(controller, "commandIcons", commandIcons);
            }

            // Dolap: giyinme odasını açar; tek dokunuşla hepsini çıkarır
            Color closet = new Color32(0xE0, 0x6F, 0x9C, 255);
            ButtonIcon(ActionButton(panels[5].transform, "closet_open", closet, Slot(0, 2), controller.OpenCloset), "cloth_party_hat", true);
            ButtonIcon(ActionButton(panels[5].transform, "closet_remove_all", new Color32(0xA9, 0x8B, 0xB8, 255), Slot(1, 2), controller.RemoveAllClothes), "act_clean", true);

            // Veteriner: yazısız; fiyat rozetlerini denetleyici yazar
            Text treatLabel = Dynamic(ActionButton(panels[6].transform, "vet_treat_none", new Color32(0x4C, 0xAF, 0x8A, 255), Slot(0, 3), controller.Treat));
            Text vaccineLabel = Dynamic(ActionButton(panels[6].transform, "vet_vaccine", SkyBlue, Slot(1, 3), controller.Vaccinate));
            Text miracleLabel = Dynamic(ActionButton(panels[6].transform, "vet_miracle", DiamondColor, Slot(2, 3), controller.Miracle));
            ButtonIcon(treatLabel, "act_treat", true);
            ButtonIcon(vaccineLabel, "act_vaccine", true);
            ButtonIcon(miracleLabel, "act_miracle", true);
            Text treatPrice = PriceChip(treatLabel, false);
            Text vaccinePrice = PriceChip(vaccineLabel, false);
            Text miraclePrice = PriceChip(miracleLabel, true);

            // Rehber el: okumayı bilmeyen çocuğa gidilecek odayı ve basılacak butonu gösterir (parmak ucu
            // pivotta). Panellerin altında kalır; denetleyici açar.
            Image guideRing = Img("GuideRing", canvas, ringSprite, new Color(1f, 0.85f, 0.2f, 0f), Center, Vector2.zero, new Vector2(200f, 200f));
            Isolate(guideRing.gameObject);
            guideRing.GetComponent<GraphicRaycaster>().enabled = false;
            guideRing.gameObject.SetActive(false);
            Image guideHand = Img("GuideHand", canvas, IconSprite("ui_paw"), Color.white, Center, Vector2.zero, new Vector2(240f, 240f));
            guideHand.rectTransform.pivot = new Vector2(0.45f, 0.95f);
            guideHand.preserveAspect = true;
            Isolate(guideHand.gameObject);
            guideHand.GetComponent<GraphicRaycaster>().enabled = false;

            // Sıra önemli: sonra kurulan üstte çizilir (ayarlar ve ebeveyn kapısı her şeyin üstünde)
            BreedPanel breed = BuildBreedPanel(canvas, controller);
            WalkPanel walk = BuildWalk(canvas, controller);
            WardrobePanel wardrobe = BuildWardrobe(canvas, controller);
            MiniGameMenu games = BuildGamesMenu(canvas, controller);
            SimonPanel simon = BuildSimon(canvas, controller);
            SettingsPanel settings = BuildSettings(canvas);
            UnityEventTools.AddPersistentListener(settingsButton.gameObject.AddComponent<Button>().onClick, settings.Open);
            // Foto çerçevesi her şeyin üstünde (çekimde arayüzün geri kalanını örter)
            PhotoPanel photo = BuildPhotoPanel(canvas, controller,
                new[] { bubble.gameObject, hubButton.gameObject, photoButton.gameObject, breedButton.gameObject });

            SetRef(controller, "view", view);
            SetRef(controller, "nameText", nameText);
            SetRef(controller, "infoText", infoText);
            SetRef(controller, "weightText", weightText);
            SetRef(controller, "goldText", gold);
            SetRef(controller, "diamondText", diamonds);
            SetArray(controller, "statFills", fills);
            if (rings != null) SetArray(controller, "statRings", rings);
            SetRef(controller, "bubble", bubble.rectTransform);
            SetRef(controller, "bubbleText", bubbleText);
            if (bubbleIcon != null) SetRef(controller, "bubbleIcon", bubbleIcon);
            SetRef(controller, "guideHand", guideHand);
            SetRef(controller, "guideRing", guideRing);
            SetRef(controller, "batheButton", batheLabel.transform.parent);
            SetRef(controller, "playButton", playLabel.transform.parent);
            SetRef(controller, "petArea", strokeArea.rectTransform);
            if (ringArt)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("iconFeedback").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                SetArray(controller, "statusIcons", statusIcons);
            }
            SetRef(controller, "night", night);
            if (foodPrices[0] != null)
            {
                SetArray(controller, "foodPrices", foodPrices);
                SetRef(controller, "treatPrice", treatPrice);
                SetRef(controller, "vaccinePrice", vaccinePrice);
                SetRef(controller, "miraclePrice", miraclePrice);
            }
            SetRef(controller, "feedbackText", feedback);
            SetRef(controller, "poop", poop.gameObject);
            SetRef(controller, "roomBackground", roomBg);
            SetRef(controller, "roomFitter", roomFitter);
            SetRef(controller, "floor", floor);
            SetColors(controller, "roomColors", RoomColors);
            SetArray(controller, "roomPanels", panels);
            SetArray(controller, "tabFaces", tabs);
            SetRef(controller, "sleepLabel", sleepLabel);
            // İkon çizimleri yoksa butonlar yazıyla kalır
            if (sleepIcon != null) SetRef(controller, "sleepIcon", sleepIcon);
            if (vetPanda != null) SetRef(controller, "vetPanda", vetPanda);
            if (foodIcons[0] != null) SetArray(controller, "foodIcons", foodIcons);
            SetRef(controller, "cleanButton", cleanLabel.transform.parent.gameObject);
            SetRef(controller, "treatLabel", treatLabel);
            SetRef(controller, "vaccineLabel", vaccineLabel);
            SetRef(controller, "miracleLabel", miracleLabel);
            SetRef(controller, "miracleButton", miracleLabel.transform.parent.gameObject);
            SetRef(controller, "criticalBanner", banner.gameObject);
            SetRef(controller, "criticalText", bannerText);
            SetArray(controller, "foodButtons", foodButtons);
            SetArray(controller, "foodLabels", foodLabels);
            SetRef(controller, "waterButton", waterLabel.transform.parent.gameObject);
            SetRef(controller, "brushButton", brushLabel.transform.parent.gameObject);
            SetRef(controller, "toiletButton", toiletLabel.transform.parent.gameObject);
            SetArray(controller, "commandLabels", commandLabels);
            SetRef(controller, "wardrobe", wardrobe);
            SetRef(controller, "walk", walk);
            SetRef(controller, "breedButton", breedButton.gameObject);
            SetRef(controller, "walkButton", walkLabel.transform.parent.gameObject);
            SetRef(controller, "stage", stage);
            SetRef(controller, "games", games);
            SetRef(controller, "simon", simon);
            SetRef(controller, "photo", photo);
            SetRef(controller, "breed", breed);
            SetRef(controller, "hubBadge", hubBadge);

            EditorSceneManager.SaveScene(scene, ScenePath(PetController.PetScene));
        }

        /// <summary>Odanın sağ kenarındaki yuvarlak buton (yazılı).</summary>
        private static Image SideButton(Transform canvas, string name, string key, Color color, Vector2 pos, UnityAction onClick)
        {
            Image button = Img(name + "Button", canvas, circle, color, Center, pos, new Vector2(150f, 150f));
            Jelly(button, true);
            button.raycastTarget = true;
            UnityEventTools.AddPersistentListener(button.gameObject.AddComponent<Button>().onClick, onClick);
            Juice(button.gameObject, 0.03f);
            Sprite sprite = IconSprite(key == "hub_button" ? "act_star" : key == "nav_baby" ? "badge_family" : key);
            if (sprite != null)
            {
                Image icon = Img("Icon", button.transform, sprite, Color.white, Center, new Vector2(0f, 9f), new Vector2(104f, 104f));
                icon.preserveAspect = true;
            }
            // Altında okunaklı adı
            // Yazıya basınca da çalışır
            Localize(Outlined(Txt("Label", button.transform, Loc.Get(key), 34, TextAnchor.MiddleCenter, Center, new Vector2(0f, -96f), new Vector2(200f, 46f))), key).raycastTarget = true;
            return button;
        }

        /// <summary>
        /// Foto stüdyosu: çerçeve seçimi (4; son ikisi Foto Paketiyle) ve "Çek". Çekimde dostun etrafındaki
        /// pencere dışında her yeri çerçeve bantları örter, altta ad ve tarih yazar.
        /// </summary>
        private static PhotoPanel BuildPhotoPanel(Transform canvas, PetController controller, GameObject[] hide)
        {
            // Çerçeve: pencere x ±420, y -560..380 (merkeze göre)
            RectTransform frame = NewUI("PhotoFrame", canvas);
            Stretch(frame);
            var bands = new List<Image>();
            Image Band(string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
            {
                Image band = Img(name, frame, null, Coral, Center, Vector2.zero, Vector2.zero);
                band.rectTransform.anchorMin = min;
                band.rectTransform.anchorMax = max;
                band.rectTransform.offsetMin = offMin;
                band.rectTransform.offsetMax = offMax;
                bands.Add(band);
                return band;
            }
            Band("Top", new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 380f), Vector2.zero);
            Image bottom = Band("Bottom", new Vector2(0f, 0f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, -560f));
            Band("Left", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -560f), new Vector2(-420f, 380f));
            Band("Right", new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(420f, -560f), new Vector2(0f, 380f));
            Text caption = White(Txt("Caption", bottom.transform, "Pamuk", 64, TextAnchor.MiddleCenter, TopCenter, new Vector2(0f, -110f), new Vector2(1000f, 100f)));
            Text brand = White(Txt("Brand", bottom.transform, Loc.Get("title"), 40, TextAnchor.MiddleCenter, TopCenter, new Vector2(0f, -200f), new Vector2(1000f, 70f)));
            Localize(brand, "title");
            Paw("PawL", frame, new Vector2(-460f, 430f), 0.5f, new Color(1f, 1f, 1f, 0.8f));
            Paw("PawR", frame, new Vector2(460f, 430f), 0.5f, new Color(1f, 1f, 1f, 0.8f));
            // Çizimli çerçeveler (çiçek, yıldız, kalp, pati): bantların üstünde, dostun etrafında
            string[] frameKeys = { "frame_flower", "frame_star", "frame_heart", "frame_paw" };
            var frameSprites = new Sprite[frameKeys.Length];
            for (int i = 0; i < frameKeys.Length; i++) frameSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{FrameArtDir}/{frameKeys[i]}.png");
            Image frameArt = null;
            if (frameSprites[0] != null)
            {
                frameArt = Img("FrameArt", frame, null, Color.white, Center, new Vector2(0f, -70f), new Vector2(980f, 1120f));
                frameArt.gameObject.SetActive(false);
            }

            // Seçim paneli
            // Kart üstte ve karartma çok hafif: çerçeve seçerken dost görünür (çerçeve penceresi y -560..380)
            RectTransform root = Overlay("Photo", canvas, new Vector2(880f, 540f), out RectTransform card, 0.15f, 680f);
            var photo = root.gameObject.AddComponent<PhotoPanel>();
            Localize(Txt("Title", card, Loc.Get("photo_title"), 60, TextAnchor.MiddleCenter, Center, new Vector2(0f, 200f), new Vector2(820f, 90f)), "photo_title");
            Color[] frameColors = { Coral, Mint, GoldColor, new Color32(0x9C, 0x7B, 0xE0, 255) };
            var buttons = new Image[frameColors.Length];
            var locks = new GameObject[frameColors.Length];
            for (int i = 0; i < frameColors.Length; i++)
            {
                Image button = Img("Frame" + i, card, roundRect, frameColors[i], Center, new Vector2((i - 1.5f) * 200f, 40f), new Vector2(170f, 170f));
                Jelly(button);
                button.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(button.gameObject.AddComponent<Button>().onClick, photo.Pick, i);
                Img("Window", button.transform, roundRect, Cream, Center, Vector2.zero, new Vector2(110f, 110f));
                if (frameSprites[i] != null)
                {
                    Image thumb = Img("Thumb", button.transform, frameSprites[i], Color.white, Center, new Vector2(0f, 6f), new Vector2(150f, 150f));
                    thumb.preserveAspect = true;
                }
                Image locked = Img("Locked", button.transform, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.65f), Center, Vector2.zero, new Vector2(170f, 170f));
                Localize(White(Txt("Label", locked.transform, Loc.Get("photo_locked"), 28, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(160f, 160f))), "photo_locked");
                buttons[i] = button;
                locks[i] = locked.gameObject;
            }
            BigButton("Shoot", card, "photo_shoot", Coral, new Vector2(-130f, -175f), new Vector2(440f, 130f), photo.Shoot);
            BigButton("Close", card, "close", new Color(Plum.r, Plum.g, Plum.b, 0.8f), new Vector2(250f, -175f), new Vector2(260f, 110f), photo.Close)
                .transform.Find("Label").GetComponent<Text>().fontSize = 44;

            SetRef(photo, "root", root.gameObject);
            SetRef(photo, "owner", controller);
            SetArray(photo, "frameButtons", buttons);
            SetArray(photo, "frameLocks", locks);
            SetColors(photo, "frameColors", frameColors);
            SetRef(photo, "frame", frame.gameObject);
            SetArray(photo, "frameBands", bands.ToArray());
            SetRef(photo, "caption", caption);
            SetArray(photo, "hideWhileShooting", hide);
            if (frameArt != null)
            {
                SetRef(photo, "frameArt", frameArt);
                SetArray(photo, "frameSprites", frameSprites);
            }
            return photo;
        }

        /// <summary>
        /// Yavru paneli: odadaki dost + diğer dostlar için satır (iki resim, durum, altınla ya da elmasla) ve doğum kutlaması.
        /// </summary>
        private static BreedPanel BuildBreedPanel(Transform canvas, PetController controller)
        {
            RectTransform root = Overlay("Breed", canvas, new Vector2(980f, 1120f), out RectTransform card);
            var breed = root.gameObject.AddComponent<BreedPanel>();
            Text title = Txt("Title", card, "", 60, TextAnchor.MiddleCenter, Center, new Vector2(0f, 470f), new Vector2(920f, 100f));
            int partnerRows = Shop.MaxPets - 1;
            var rows = new GameObject[partnerRows];
            var portraitsA = new Image[partnerRows];
            var portraitsB = new Image[partnerRows];
            var names = new Text[partnerRows];
            var states = new Text[partnerRows];
            var stateIcons = new Image[partnerRows];
            var goldIcons = new Image[partnerRows];
            var goldButtons = new GameObject[partnerRows];
            var goldLabels = new Text[partnerRows];
            var gemButtons = new GameObject[partnerRows];
            var gemLabels = new Text[partnerRows];
            for (int i = 0; i < rows.Length; i++)
            {
                Image row = Img("Row" + i, card, roundRect, Color.white, Center, new Vector2(0f, 300f - i * 205f), new Vector2(920f, 190f));
                Sliced(row);
                portraitsA[i] = Img("PetA", row.transform, null, Color.white, Center, new Vector2(-370f, 0f), new Vector2(150f, 150f));
                portraitsA[i].preserveAspect = true;
                Text plus = Txt("Plus", row.transform, "+", 60, TextAnchor.MiddleCenter, Center, new Vector2(-285f, 2f), new Vector2(50f, 70f));
                plus.color = Coral;
                portraitsB[i] = Img("PetB", row.transform, null, Color.white, Center, new Vector2(-200f, 0f), new Vector2(150f, 150f));
                portraitsB[i].preserveAspect = true;
                names[i] = Txt("Name", row.transform, "", 40, TextAnchor.MiddleLeft, Center, new Vector2(30f, 52f), new Vector2(260f, 56f));
                names[i].resizeTextForBestFit = true;
                names[i].resizeTextMinSize = 24;
                names[i].resizeTextMaxSize = 40;
                stateIcons[i] = Img("StateIcon", row.transform, null, Color.white, Center, new Vector2(-62f, -30f), new Vector2(84f, 84f));
                stateIcons[i].preserveAspect = true;
                states[i] = Txt("State", row.transform, "", 30, TextAnchor.MiddleLeft, Center, new Vector2(88f, -30f), new Vector2(200f, 104f));
                states[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                states[i].verticalOverflow = VerticalWrapMode.Truncate;
                states[i].resizeTextForBestFit = true;
                states[i].resizeTextMinSize = 22;
                states[i].resizeTextMaxSize = 30;
                states[i].color = new Color(Plum.r, Plum.g, Plum.b, 0.8f);
                goldButtons[i] = RowButton(row.transform, new Vector2(340f, 42f), new Vector2(210f, 80f), breed.BreedGold, i, out goldLabels[i]).gameObject;
                goldButtons[i].GetComponent<Image>().color = GoldColor;
                goldIcons[i] = Img("Icon", goldButtons[i].transform, null, Color.white, Center, new Vector2(-66f, 3f), new Vector2(60f, 60f));
                goldIcons[i].preserveAspect = true;
                goldLabels[i].rectTransform.anchoredPosition = new Vector2(28f, 2f);
                goldLabels[i].rectTransform.sizeDelta = new Vector2(140f, 70f);
                goldLabels[i].resizeTextForBestFit = true;
                goldLabels[i].resizeTextMinSize = 22;
                goldLabels[i].resizeTextMaxSize = 40;
                gemButtons[i] = RowButton(row.transform, new Vector2(340f, -44f), new Vector2(210f, 80f), breed.BreedGem, i, out gemLabels[i]).gameObject;
                gemButtons[i].GetComponent<Image>().color = DiamondColor;
                Image gemIcon = Img("Icon", gemButtons[i].transform, IconSprite("fx_gem"), Color.white, Center, new Vector2(-66f, 3f), new Vector2(60f, 60f));
                gemIcon.preserveAspect = true;
                gemLabels[i].rectTransform.anchoredPosition = new Vector2(28f, 2f);
                gemLabels[i].rectTransform.sizeDelta = new Vector2(140f, 70f);
                rows[i] = row.gameObject;
            }
            BigButton("Close", card, "close", new Color(Plum.r, Plum.g, Plum.b, 0.8f), new Vector2(0f, -480f), new Vector2(400f, 100f), breed.Close)
                .transform.Find("Label").GetComponent<Text>().fontSize = 48;

            // Doğum kutlaması: panelin üstünde tam ekran
            Image reveal = Img("Reveal", root, null, new Color(1f, 0.95f, 0.86f, 0.98f), Center, Vector2.zero, Vector2.zero);
            Stretch(reveal.rectTransform);
            reveal.raycastTarget = true;
            Text born = Txt("Born", reveal.transform, Loc.Get("breed_born"), 92, TextAnchor.MiddleCenter, Center, new Vector2(0f, 560f), new Vector2(1000f, 130f));
            born.color = Coral;
            Localize(born, "breed_born");
            Image parentA = Img("ParentA", reveal.transform, null, Color.white, Center, new Vector2(-240f, 80f), new Vector2(420f, 420f));
            parentA.preserveAspect = true;
            Image parentB = Img("ParentB", reveal.transform, null, Color.white, Center, new Vector2(240f, 80f), new Vector2(420f, 420f));
            parentB.preserveAspect = true;
            Image babyArt = Img("Baby", reveal.transform, null, Color.white, Center, new Vector2(0f, -210f), new Vector2(380f, 380f));
            babyArt.preserveAspect = true;
            InputField babyName = NameInput(reveal.transform, new Vector2(-60f, -480f));
            Image dice = Img("Dice", babyName.transform, roundRect, SkyBlue, Center, new Vector2(370f, 0f), new Vector2(120f, 120f));
            Sliced(dice);
            dice.raycastTarget = true;
            UnityEventTools.AddPersistentListener(dice.gameObject.AddComponent<Button>().onClick, breed.RandomBabyName);
            White(Txt("Label", dice.transform, "?", 70, TextAnchor.MiddleCenter, Center, new Vector2(0f, 4f), new Vector2(120f, 120f)));
            Image done = BigButton("Done", reveal.transform, "breed_name_ok", Mint, new Vector2(0f, -680f), new Vector2(560f, 150f), breed.FinishReveal);

            SetRef(breed, "root", root.gameObject);
            SetRef(breed, "owner", controller);
            SetRef(breed, "title", title);
            SetArray(breed, "rows", rows);
            SetArray(breed, "portraitsA", portraitsA);
            SetArray(breed, "portraitsB", portraitsB);
            SetArray(breed, "names", names);
            SetArray(breed, "states", states);
            SetArray(breed, "stateIcons", stateIcons);
            SetArray(breed, "goldIcons", goldIcons);
            SetArray(breed, "goldButtons", goldButtons);
            SetArray(breed, "goldLabels", goldLabels);
            SetArray(breed, "gemButtons", gemButtons);
            SetArray(breed, "gemLabels", gemLabels);
            SetRef(breed, "reveal", reveal.gameObject);
            SetRef(breed, "revealA", parentA.rectTransform);
            SetRef(breed, "revealB", parentB.rectTransform);
            SetRef(breed, "revealBaby", babyArt.rectTransform);
            SetRef(breed, "babyName", babyName);
            SetRef(breed, "revealDone", done.gameObject);
            return breed;
        }

        /// <summary>Cüzdan plakasını (yazısının ebeveyni) mağaza butonu yapar.</summary>
        private static void ClickToShop(Text walletValue, UnityAction openShop)
        {
            GameObject plate = walletValue.transform.parent.gameObject;
            plate.GetComponent<Image>().raycastTarget = true;
            UnityEventTools.AddPersistentListener(plate.AddComponent<Button>().onClick, openShop);
        }

        /// <summary>Beyaz konturlu koyu yazı: odanın ya da düğmenin önünde de okunur.</summary>
        private static Text Outlined(Text text)
        {
            text.color = Plum;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(3f, -3f);
            return text;
        }

        /// <summary>
        /// Giyinme odası: üstte dost (üstünde dener), koleksiyon sayısı, sekmeler (Dolabım, şapka, gözlük, boyun,
        /// elbise) ve 3 sütun kaydırılan parça kartları.
        /// </summary>
        private static WardrobePanel BuildWardrobe(Transform canvas, PetController controller)
        {
            RectTransform root = Overlay("Wardrobe", canvas, new Vector2(1050f, 1880f), out RectTransform card);
            var wardrobe = root.gameObject.AddComponent<WardrobePanel>();

            Img("Spot", card, circle, new Color(Mint.r, Mint.g, Mint.b, 0.35f), Center, new Vector2(0f, 512f), new Vector2(440f, 110f));
            PetView preview = Critter(card, new Vector2(0f, 500f));
            preview.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            Isolate(preview.gameObject);
            Text count = Txt("Count", card, "", 36, TextAnchor.MiddleCenter, Center, new Vector2(0f, 452f), new Vector2(760f, 50f));

            // Sekmeler
            string[] tabKeys = { "closet_mine", "slot_hat", "slot_glasses", "slot_neck", "slot_outfit" };
            string[] tabIcons = { "ui_gift", "cloth_party_hat", "cloth_round_glasses", "cloth_bow_tie", "cloth_sweater" };
            Color closet = new Color32(0xF9, 0xC8, 0xDC, 255);
            var tabs = new Image[tabKeys.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                Image tab = Img("Tab" + i, card, roundRect, Color.white, Center, new Vector2((i - 2f) * 202f, 340f), new Vector2(194f, 160f));
                Sliced(tab);
                tab.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(tab.gameObject.AddComponent<Button>().onClick, wardrobe.ShowTab, i);
                Juice(tab.gameObject, 0f);
                Image face = Img("Color", tab.transform, roundRect, closet, Center, Vector2.zero, new Vector2(182f, 148f));
                Jelly(face);
                tabs[i] = face;
                Image icon = Img("Icon", tab.transform, IconSprite(tabIcons[i]), Color.white, Center, new Vector2(0f, 24f), new Vector2(92f, 92f));
                icon.preserveAspect = true;
                Text label = Txt("Label", tab.transform, Loc.Get(tabKeys[i]), 34, TextAnchor.MiddleCenter, Center, new Vector2(0f, -46f), new Vector2(184f, 48f));
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 24;
                label.resizeTextMaxSize = 34;
                Localize(label, tabKeys[i]);
            }

            // Kaydırılan parçalar
            RectTransform view = NewUI("Viewport", card);
            Place(view, Center, new Vector2(0f, -285f), new Vector2(1010f, 1060f));
            view.gameObject.AddComponent<RectMask2D>();
            Image viewHit = view.gameObject.AddComponent<Image>();
            viewHit.color = new Color(1f, 1f, 1f, 0f);
            RectTransform content = NewUI("Content", view);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(324f, 366f);
            grid.spacing = new Vector2(12f, 14f);
            grid.padding = new RectOffset(6, 6, 6, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            int slots = Wardrobe.All.Length;
            var cards = new Image[slots];
            var previews = new Image[slots];
            var names = new Text[slots];
            var stateBacks = new Image[slots];
            var states = new Text[slots];
            for (int i = 0; i < slots; i++)
            {
                Image item = Img("Item" + i, content, roundRect, Color.white, Center, Vector2.zero, new Vector2(324f, 366f));
                Card(item);
                item.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(item.gameObject.AddComponent<Button>().onClick, wardrobe.Tap, i);
                Juice(item.gameObject, 0f);
                previews[i] = Img("Preview", item.transform, circle, Color.white, Center, new Vector2(0f, 62f), new Vector2(210f, 210f));
                names[i] = Txt("Name", item.transform, "", 36, TextAnchor.MiddleCenter, Center, new Vector2(0f, -66f), new Vector2(304f, 62f));
                names[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                names[i].verticalOverflow = VerticalWrapMode.Truncate;
                names[i].resizeTextForBestFit = true;
                names[i].resizeTextMinSize = 24;
                names[i].resizeTextMaxSize = 36;
                stateBacks[i] = Img("State", item.transform, roundRect, Mint, Center, new Vector2(0f, -136f), new Vector2(286f, 78f));
                Jelly(stateBacks[i]);
                states[i] = White(Txt("Text", stateBacks[i].transform, "", 40, TextAnchor.MiddleCenter, Center, new Vector2(0f, 4f), new Vector2(274f, 66f)));
                cards[i] = item;
            }
            Text empty = Txt("Empty", card, "", 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, -120f), new Vector2(900f, 300f));
            empty.horizontalOverflow = HorizontalWrapMode.Wrap;
            empty.color = new Color(Plum.r, Plum.g, Plum.b, 0.8f);

            Image removeAll = BigButton("RemoveAll", card, "closet_remove_all", new Color32(0xA9, 0x8B, 0xB8, 255), new Vector2(-250f, -860f), new Vector2(480f, 120f), wardrobe.RemoveAll);
            removeAll.transform.Find("Label").GetComponent<Text>().fontSize = 48;
            BigButton("Close", card, "close", Coral, new Vector2(250f, -860f), new Vector2(480f, 120f), wardrobe.Close)
                .transform.Find("Label").GetComponent<Text>().fontSize = 54;

            SetRef(wardrobe, "root", root.gameObject);
            SetRef(wardrobe, "owner", controller);
            SetRef(wardrobe, "preview", preview);
            SetRef(wardrobe, "countText", count);
            SetRef(wardrobe, "emptyText", empty);
            SetArray(wardrobe, "tabs", tabs);
            SetRef(wardrobe, "scroll", scroll);
            SetArray(wardrobe, "cards", cards);
            SetArray(wardrobe, "previews", previews);
            SetArray(wardrobe, "names", names);
            SetArray(wardrobe, "stateBacks", stateBacks);
            SetArray(wardrobe, "states", states);
            return wardrobe;
        }

        /// <summary>Dışarıda gezinti: akan park, yürüyen dost, toplanacaklar ve "Eve Dön".</summary>
        private static WalkPanel BuildWalk(Transform canvas, PetController controller)
        {
            Image surface = Img("Walk", canvas, null, new Color(0.62f, 0.86f, 0.98f, 1f), Center, Vector2.zero, Vector2.zero);
            Stretch(surface.rectTransform);
            surface.raycastTarget = true;
            Isolate(surface.gameObject);
            RectTransform card = NewUI("Card", surface.transform);
            Stretch(card);
            var pop = surface.gameObject.AddComponent<PanelPop>();
            SetRef(pop, "card", card);
            var walk = surface.gameObject.AddComponent<WalkPanel>();
            UnityEventTools.AddPersistentListener(surface.gameObject.AddComponent<Button>().onClick, walk.Jump);

            // Aynalı karolar: dikey park çizimi yan yana, ek yeri görünmez (genişlik 2400 yüksekliğe göre)
            Sprite park = AssetDatabase.LoadAssetAtPath<Sprite>($"{RoomArtDir}/bg_oyun_park.png");
            var tiles = new RectTransform[4];
            for (int i = 0; i < tiles.Length; i++)
            {
                Image tile = Img("Tile" + i, card, park, Color.white, Center, Vector2.zero, new Vector2(1350f, 2400f));
                tile.rectTransform.localScale = new Vector3(i % 2 == 0 ? 1f : -1f, 1f, 1f);
                tiles[i] = tile.rectTransform;
            }
            const float floorY = -520f;
            Image shadow = Img("Shadow", card, circle, new Color(0f, 0f, 0f, 0.16f), Center, new Vector2(-120f, floorY + 4f), new Vector2(430f, 90f));
            PetView view = Critter(card, new Vector2(-120f, floorY));
            RectTransform items = NewUI("Items", card);
            Stretch(items);

            Text coins = WalletPlate("Coins", card, TopRight, new Vector2(-30f, -36f), new Vector2(240f, 100f), GoldColor, false);
            Object.DestroyImmediate(coins.GetComponent<CurrencyCounter>());
            BigButton("Close", card, "walk_done", Coral, new Vector2(-200f, -780f), new Vector2(520f, 150f), walk.Close)
                .transform.Find("Label").GetComponent<Text>().fontSize = 60;
            BigButton("Flip", card, "walk_flip", Mint, new Vector2(300f, -780f), new Vector2(380f, 150f), walk.Flip)
                .transform.Find("Label").GetComponent<Text>().fontSize = 60;

            SetRef(walk, "root", surface.gameObject);
            SetRef(walk, "owner", controller);
            SetArray(walk, "tiles", tiles);
            SetRef(walk, "view", view);
            SetRef(walk, "petRoot", view.transform);
            SetRef(walk, "petShadow", shadow.rectTransform);
            SetRef(walk, "itemLayer", items);
            SetRef(walk, "coinText", coins);
            return walk;
        }

        /// <summary>Mini oyun listesi: 4 oyun kartı (adı ve kısa açıklaması).</summary>
        private static MiniGameMenu BuildGamesMenu(Transform canvas, PetController controller)
        {
            RectTransform root = Overlay("Games", canvas, new Vector2(900f, 1200f), out RectTransform card);
            var menu = root.gameObject.AddComponent<MiniGameMenu>();
            Localize(Txt("Title", card, Loc.Get("games_title"), 66, TextAnchor.MiddleCenter, Center, new Vector2(0f, 510f), new Vector2(860f, 100f)), "games_title");
            Color[] colors = { Mint, Coral, SkyBlue, GoldColor, new Color32(0x9C, 0x7B, 0xE0, 255) };
            string[] gameIcons = { "mg_ball", "mg_bowl", "mg_card", "mg_box", "fx_bubble" };
            var gameCards = new RectTransform[colors.Length];
            var gameLevels = new Text[colors.Length];
            for (int i = 0; i < MiniGameHost.TitleKeys.Length; i++)
            {
                Image game = Img("Game" + i, card, roundRect, colors[i], Center, new Vector2(0f, 340f - i * 170f), new Vector2(820f, 156f));
                Jelly(game);
                game.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(game.gameObject.AddComponent<Button>().onClick, menu.Play, i);
                Juice(game.gameObject, 0f);
                string key = MiniGameHost.TitleKeys[i];
                // İkon solda; yazılar sağdaki alana kayar
                Sprite sprite = IconSprite(gameIcons[i]);
                float textX = sprite != null ? 30f : 0f, textW = sprite != null ? 560f : 780f;
                gameCards[i] = game.rectTransform;
                Image chip = Img("LevelChip", game.transform, roundRect, new Color(1f, 1f, 1f, 0.92f), Center, new Vector2(320f, 38f), new Vector2(150f, 58f));
                Sliced(chip);
                gameLevels[i] = Txt("Level", chip.transform, "", 30, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(146f, 54f));
                if (sprite != null)
                {
                    Image icon = Img("Icon", game.transform, sprite, Color.white, Center, new Vector2(-315f, 0f), new Vector2(130f, 130f));
                    icon.preserveAspect = true;
                }
                Localize(White(Txt("Name", game.transform, Loc.Get(key), 50, TextAnchor.MiddleCenter, Center, new Vector2(textX - 30f, 28f), new Vector2(textW, 66f))), key);
                Text desc = White(Txt("Desc", game.transform, Loc.Get(key + "_desc"), 32, TextAnchor.MiddleCenter, Center, new Vector2(textX + 30f, -36f), new Vector2(textW + 110f, 50f)));
                desc.resizeTextForBestFit = true;
                desc.resizeTextMinSize = 22;
                desc.resizeTextMaxSize = 32;
                Localize(desc, key + "_desc");
            }
            BigButton("Close", card, "close", new Color(Plum.r, Plum.g, Plum.b, 0.8f), new Vector2(0f, -525f), new Vector2(420f, 110f), menu.Close)
                .transform.Find("Label").GetComponent<Text>().fontSize = 50;

            SetRef(menu, "root", root.gameObject);
            SetRef(menu, "owner", controller);
            SetArray(menu, "cards", gameCards);
            SetArray(menu, "levels", gameLevels);
            SetFloat(menu, "topY", 340f);
            SetFloat(menu, "stepY", 170f);
            return menu;
        }

        /// <summary>Okul dersi: 4 renkli düğme (sıra hafızası) ve yönerge.</summary>
        private static SimonPanel BuildSimon(Transform canvas, PetController controller)
        {
            // Kart ekranın üst yarısında ve karartma hafif: dost aşağıda görünür kalır, dersi izler
            RectTransform root = Overlay("Lesson", canvas, new Vector2(860f, 900f), out RectTransform card, 0.35f, 500f);
            var simon = root.gameObject.AddComponent<SimonPanel>();
            Text prompt = Txt("Prompt", card, Loc.Get("simon_watch"), 64, TextAnchor.MiddleCenter, Center, new Vector2(0f, 360f), new Vector2(820f, 100f));
            Color[] colors = { Coral, Mint, SkyBlue, GoldColor };
            string[] symbols = { "ui_paw", "mg_star", "ui_heart", "act_play" };
            var pads = new Image[MiniGameRules.SimonPads];
            for (int i = 0; i < pads.Length; i++)
            {
                Vector2 pos = new Vector2((i % 2 == 0 ? -1f : 1f) * 160f, 150f - (i / 2) * 300f);
                Image pad = Img("Pad" + i, card, roundRect, colors[i], Center, pos, new Vector2(280f, 280f));
                Jelly(pad);
                pad.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(pad.gameObject.AddComponent<Button>().onClick, simon.Press, i);
                Juice(pad.gameObject, 0f);
                // Her pedin sembolü: renk körü çocuk da ayırt etsin
                Sprite symbol = IconSprite(symbols[i]);
                if (symbol != null)
                {
                    Image mark = Img("Symbol", pad.transform, symbol, Color.white, Center, new Vector2(0f, 8f), new Vector2(150f, 150f));
                    mark.preserveAspect = true;
                }
                pads[i] = pad;
            }
            Image close = Img("Close", card, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.8f), Center, new Vector2(0f, -385f), new Vector2(320f, 90f));
            Jelly(close);
            close.raycastTarget = true;
            UnityEventTools.AddPersistentListener(close.gameObject.AddComponent<Button>().onClick, simon.Close);
            Localize(White(Txt("Label", close.transform, Loc.Get("close"), 42, TextAnchor.MiddleCenter, Center, new Vector2(0f, 4f), new Vector2(300f, 80f))), "close");

            // Rehber el: sıra çocuğa gelince basılacak pedi gösterir (ilk derslerde, zorlanınca, bekleyince)
            Image hand = Img("CoachHand", card, IconSprite("hand_point") ?? IconSprite("ui_paw"), Color.white, Center, Vector2.zero, new Vector2(200f, 200f));
            hand.preserveAspect = true;
            var coach = root.gameObject.AddComponent<GameCoach>();
            SetRef(coach, "hand", hand);
            SetRef(simon, "coach", coach);

            SetRef(simon, "root", root.gameObject);
            SetRef(simon, "owner", controller);
            SetArray(simon, "pads", pads);
            SetRef(simon, "prompt", prompt);
            return simon;
        }

        /// <summary>Etiketi denetleyici yazacak: dil değişince sözlükten üzerine yazılmasın, iki satır sığsın.</summary>
        private static Text Dynamic(Text label)
        {
            Object.DestroyImmediate(label.GetComponent<LocalizedText>());
            label.fontSize = 38;
            label.lineSpacing = 0.9f;
            return label;
        }

        /// <summary>Tam ekran karartma ve ortada beyaz kart (ayarlar, ebeveyn kapısı).</summary>
        private static RectTransform Overlay(string name, Transform canvas, Vector2 cardSize, out RectTransform card,
            float dimAlpha = 0.65f, float cardY = 0f)
        {
            Image dim = Img(name, canvas, null, new Color(Plum.r, Plum.g, Plum.b, dimAlpha), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;   // arkadaki butonlara dokunulmasın
            Image face = Img("Card", dim.transform, roundRect, Cream, Center, new Vector2(0f, cardY), cardSize);
            Card(face);
            face.raycastTarget = true;
            card = face.rectTransform;
            // Açılınca kart yay gibi büyüyerek gelir; geri tuşu en üstteki paneli kapatır. Panel kendi alt
            // tuvalinde: açılış animasyonu arkadaki ekranı yeniden hesaplatmasın
            Isolate(dim.gameObject);
            var pop = dim.gameObject.AddComponent<PanelPop>();
            SetRef(pop, "card", card);
            SetRef(pop, "dim", dim);
            return dim.rectTransform;
        }

        private static SettingsPanel BuildSettings(Transform canvas)
        {
            RectTransform root = Overlay("Settings", canvas, new Vector2(900f, 1720f), out RectTransform card);
            var settings = root.gameObject.AddComponent<SettingsPanel>();
            Localize(Txt("Title", card, Loc.Get("settings"), 70, TextAnchor.MiddleCenter, Center, new Vector2(0f, 760f), new Vector2(800f, 100f)), "settings");

            BigButton("Language", card, "settings_language", SkyBlue, new Vector2(0f, 630f), new Vector2(700f, 120f), settings.ToggleLanguage)
                .transform.Find("Label").GetComponent<Text>().fontSize = 54;
            // Ses ve müzik (cihaz ayarı)
            Text sound = SettingRow(card, "settings_sound", 500f, settings.ToggleSound);
            Text music = SettingRow(card, "settings_music", 370f, settings.ToggleMusic);
            Text notifications = SettingRow(card, "settings_notifications", 240f, settings.ToggleNotifications);
            Text noDeath = SettingRow(card, "settings_nodeath", 110f, settings.ToggleNoDeath);
            Text limit = SettingRow(card, "settings_limit", -20f, settings.ToggleLimit);
            // Dostla ilgili haklar ve biletler
            Text mode = SettingRow(card, "settings_mode", -170f, settings.ChangeMode);
            Text boarding = SettingRow(card, "settings_boarding", -300f, settings.StartBoarding);
            Text shelter = SettingRow(card, "settings_shelter", -430f, settings.SendToShelter);
            foreach (Text value in new[] { mode, boarding, shelter })
            {
                value.fontSize = 34;
                value.transform.parent.GetComponent<Image>().color = new Color32(0xE0, 0x6F, 0x9C, 255);
            }
            BigButton("Close", card, "close", Coral, new Vector2(0f, -700f), new Vector2(500f, 130f), settings.Close)
                .transform.Find("Label").GetComponent<Text>().fontSize = 58;

            var petRows = new List<GameObject>();
            foreach (string key in new[] { "settings_mode", "settings_boarding", "settings_shelter" })
            {
                petRows.Add(card.Find("Label_" + key).gameObject);
                petRows.Add(card.Find("Toggle_" + key).gameObject);
            }
            SetArray(settings, "petRows", petRows.ToArray());

            ParentalGate gate = BuildGate(canvas);
            SetRef(settings, "root", root.gameObject);
            SetRef(settings, "soundValue", sound);
            SetRef(settings, "musicValue", music);
            SetRef(settings, "notificationsValue", notifications);
            SetRef(settings, "noDeathValue", noDeath);
            SetRef(settings, "limitValue", limit);
            SetRef(settings, "modeValue", mode);
            SetRef(settings, "boardingValue", boarding);
            SetRef(settings, "shelterValue", shelter);
            SetRef(settings, "gate", gate);
            return settings;
        }

        /// <summary>Solda ayarın adı, sağda AÇIK/KAPALI butonu; butonun yazısını döndürür.</summary>
        private static Text SettingRow(Transform card, string key, float y, UnityAction onClick)
        {
            Localize(Txt("Label_" + key, card, Loc.Get(key), 44, TextAnchor.MiddleLeft, Center, new Vector2(-170f, y), new Vector2(460f, 90f)), key);
            Image toggle = Img("Toggle_" + key, card, roundRect, Mint, Center, new Vector2(230f, y), new Vector2(300f, 110f));
            Jelly(toggle);
            toggle.raycastTarget = true;
            UnityEventTools.AddPersistentListener(toggle.gameObject.AddComponent<Button>().onClick, onClick);
            Juice(toggle.gameObject, 0f);
            return White(Txt("Value", toggle.transform, Loc.Get("on"), 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(290f, 100f)));
        }

        private static ParentalGate BuildGate(Transform canvas)
        {
            RectTransform root = Overlay("ParentalGate", canvas, new Vector2(820f, 860f), out RectTransform card);
            var gate = root.gameObject.AddComponent<ParentalGate>();
            Localize(Txt("Title", card, Loc.Get("gate_title"), 58, TextAnchor.MiddleCenter, Center, new Vector2(0f, 330f), new Vector2(780f, 90f)), "gate_title");
            Text question = Txt("Question", card, "7 × 8 = ?", 96, TextAnchor.MiddleCenter, Center, new Vector2(0f, 200f), new Vector2(780f, 130f));
            question.color = Coral;
            var answers = new Text[4];
            for (int i = 0; i < answers.Length; i++)
            {
                Vector2 pos = new Vector2((i % 2 == 0 ? -1f : 1f) * 180f, 30f - (i / 2) * 170f);
                Image button = Img("Answer" + i, card, roundRect, SkyBlue, Center, pos, new Vector2(320f, 140f));
                Sliced(button);
                button.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(button.gameObject.AddComponent<Button>().onClick, gate.Answer, i);
                Juice(button.gameObject, 0f);
                answers[i] = White(Txt("Label", button.transform, "56", 66, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(300f, 130f)));
            }
            Text message = Txt("Message", card, "", 40, TextAnchor.MiddleCenter, Center, new Vector2(0f, -250f), new Vector2(780f, 70f));
            message.color = Coral;
            Image cancel = Img("Cancel", card, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.8f), Center, new Vector2(0f, -350f), new Vector2(360f, 100f));
            Sliced(cancel);
            cancel.raycastTarget = true;
            UnityEventTools.AddPersistentListener(cancel.gameObject.AddComponent<Button>().onClick, gate.Cancel);
            Localize(White(Txt("Label", cancel.transform, Loc.Get("close"), 46, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(340f, 90f))), "close");

            SetRef(gate, "root", root.gameObject);
            SetRef(gate, "question", question);
            SetArray(gate, "answers", answers);
            SetRef(gate, "message", message);
            return gate;
        }

        // ---------- Merkez ----------

        /// <summary>
        /// Merkez: Görevler, Pati Pass, Rozetler, Şov, Ev, Kulüp sekmeleri (HubController). Altta mesaj satırı;
        /// gerçek paralı alımlar için ebeveyn kapısı en üstte.
        /// </summary>
        private static void BuildHubScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(Cream);
            Transform canvas = CreateCanvas();
            Background(canvas, "bg_merkez");
            var hub = canvas.gameObject.AddComponent<HubController>();

            Localize(Title(canvas, "hub_button"), "hub_button").rectTransform.anchoredPosition = new Vector2(0f, -115f);
            BackButton(canvas, hub.Back);
            Text gold = WalletPlate("Gold", canvas, TopRight, new Vector2(-30f, -36f), new Vector2(210f, 84f), GoldColor, false);
            Text diamonds = WalletPlate("Diamonds", canvas, TopRight, new Vector2(-30f, -130f), new Vector2(210f, 84f), DiamondColor, true);
            ClickToShop(gold, hub.OpenShop);
            ClickToShop(diamonds, hub.OpenShop);

            string[] tabKeys = { "hub_tasks", "hub_pass", "hub_badges", "hub_show", "hub_home", "hub_club" };
            Color[] tabColors = { Mint, GoldColor, Coral, SkyBlue, new Color32(0xF9, 0xC8, 0xDC, 255), new Color32(0xC9, 0xA7, 0xF2, 255) };
            string[] hubTabIcons = { "ui_calendar", "ui_ticket", "ui_medal", "ui_trophy", "ui_home", "ui_crown" };
            var tabs = new Image[tabKeys.Length];
            var panels = new GameObject[tabKeys.Length];
            for (int i = 0; i < tabKeys.Length; i++)
            {
                Image tab = Img("Tab_" + tabKeys[i], canvas, roundRect, Color.white, TopCenter, new Vector2((i - 2.5f) * 178f, -232f), new Vector2(172f, 176f));
                Sliced(tab);
                tab.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(tab.gameObject.AddComponent<Button>().onClick, hub.ShowTab, i);
                Image face = Img("Color", tab.transform, roundRect, tabColors[i], Center, Vector2.zero, new Vector2(160f, 164f));
                Jelly(face);
                Text tabLabel = Txt("Label", tab.transform, Loc.Get(tabKeys[i]), 34, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(160f, 90f));
                Localize(tabLabel, tabKeys[i]);
                Outlined(tabLabel);
                Sprite tabSprite = IconSprite(hubTabIcons[i]);
                if (tabSprite != null)
                {
                    Image icon = Img("Icon", tab.transform, tabSprite, Color.white, Center, new Vector2(0f, 26f), new Vector2(92f, 92f));
                    icon.preserveAspect = true;
                    tabLabel.rectTransform.anchoredPosition = new Vector2(0f, -50f);
                    tabLabel.rectTransform.sizeDelta = new Vector2(164f, 48f);
                    tabLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                    tabLabel.verticalOverflow = VerticalWrapMode.Truncate;
                    tabLabel.resizeTextForBestFit = true;
                    tabLabel.resizeTextMinSize = 24;
                    tabLabel.resizeTextMaxSize = 36;
                }
                tabs[i] = tab;
                // Paneller sekmelerin hemen altından başlar (uzun ekranda boşluk kalmasın)
                RectTransform panel = NewUI("Panel_" + tabKeys[i], canvas);
                Place(panel, TopCenter, new Vector2(0f, -425f), new Vector2(1080f, 1400f));
                panels[i] = panel.gameObject;
            }

            // 0 Görevler
            Transform p = panels[0].transform;
            Text streak = Txt("Streak", p, "", 60, TextAnchor.MiddleCenter, Center, new Vector2(0f, 560f), new Vector2(1000f, 90f));
            streak.color = Coral;
            Hint(p, "hub_streak_hint", 480f);
            Localize(Label(p, "hub_today", 370f), "hub_today");
            var taskNames = new Text[Progress.TasksPerDay];
            var taskIcons = new Image[Progress.TasksPerDay];
            var taskProgress = new Text[Progress.TasksPerDay];
            var taskButtons = new Image[Progress.TasksPerDay];
            var taskLabels = new Text[Progress.TasksPerDay];
            for (int i = 0; i < taskNames.Length; i++)
            {
                Image row = Img("Task" + i, p, roundRect, Color.white, Center, new Vector2(0f, 240f - i * 200f), new Vector2(1000f, 180f));
                Card(row);
                // Görevin resmi solda (HubController koyar), adı yanında
                taskIcons[i] = Img("Icon", row.transform, null, Color.white, Center, new Vector2(-415f, 4f), new Vector2(136f, 136f));
                taskIcons[i].preserveAspect = true;
                taskNames[i] = Txt("Name", row.transform, "", 50, TextAnchor.MiddleLeft, Center, new Vector2(-110f, 0f), new Vector2(430f, 90f));
                taskNames[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                taskNames[i].verticalOverflow = VerticalWrapMode.Truncate;
                taskNames[i].resizeTextForBestFit = true;
                taskNames[i].resizeTextMinSize = 30;
                taskNames[i].resizeTextMaxSize = 50;
                taskProgress[i] = Txt("Progress", row.transform, "", 50, TextAnchor.MiddleCenter, Center, new Vector2(165f, 0f), new Vector2(150f, 90f));
                taskButtons[i] = RowButton(row.transform, new Vector2(365f, 0f), new Vector2(230f, 124f), hub.ClaimTask, i, out taskLabels[i]);
                taskLabels[i].fontSize = 46;
            }

            // 1 Pati Pass
            p = panels[1].transform;
            Text level = Txt("Level", p, "", 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, 580f), new Vector2(1000f, 70f));
            Image track = Img("Track", p, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.15f), Center, new Vector2(0f, 515f), new Vector2(840f, 40f));
            Sliced(track);
            Image fill = Img("Fill", track.transform, roundRect, GoldColor, Center, Vector2.zero, new Vector2(840f, 40f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            Hint(p, "pass_hint", 455f);
            Localize(Label(p, "pass_free", 395f), "pass_free").rectTransform.anchoredPosition = new Vector2(-200f, 395f);
            Localize(Label(p, "pass_premium", 395f), "pass_premium").rectTransform.anchoredPosition = new Vector2(250f, 395f);
            var freeTexts = new Text[PatiPass.Levels];
            var freeButtons = new Image[PatiPass.Levels];
            var premiumTexts = new Text[PatiPass.Levels];
            var premiumButtons = new Image[PatiPass.Levels];
            for (int i = 0; i < PatiPass.Levels; i++)
            {
                float y = 330f - i * 80f;
                Text number = Txt("Level" + (i + 1), p, (i + 1).ToString(), 40, TextAnchor.MiddleCenter, Center, new Vector2(-440f, y), new Vector2(80f, 70f));
                number.color = Coral;
                freeButtons[i] = RowButton(p, new Vector2(-200f, y), new Vector2(360f, 70f), hub.ClaimFree, i, out freeTexts[i]);
                premiumButtons[i] = RowButton(p, new Vector2(250f, y), new Vector2(400f, 70f), hub.ClaimPremium, i, out premiumTexts[i]);
                freeTexts[i].fontSize = premiumTexts[i].fontSize = 34;
                foreach (Text rowText in new[] { freeTexts[i], premiumTexts[i] })
                {
                    rowText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    rowText.verticalOverflow = VerticalWrapMode.Truncate;
                    rowText.resizeTextForBestFit = true;
                    rowText.resizeTextMinSize = 22;
                    rowText.resizeTextMaxSize = 34;
                }
            }
            Image buyPass = BigButton("BuyPass", p, "pass_buy", DiamondColor, new Vector2(0f, -560f), new Vector2(760f, 120f), hub.BuyPass);
            Text buyPassLabel = buyPass.transform.Find("Label").GetComponent<Text>();
            buyPassLabel.fontSize = 44;
            Object.DestroyImmediate(buyPassLabel.GetComponent<LocalizedText>());

            // 2 Rozetler
            p = panels[2].transform;
            var badgeCards = new Image[Progress.Badges.Length];
            var badgeNames = new Text[Progress.Badges.Length];
            var badgeDescs = new Text[Progress.Badges.Length];
            var badgeIcons = new Image[Progress.Badges.Length];
            for (int i = 0; i < badgeCards.Length; i++)
            {
                Vector2 pos = new Vector2((i % 3 - 1) * 345f, 560f - (i / 3) * 228f);
                Image badge = Img("Badge" + i, p, roundRect, Color.white, Center, pos, new Vector2(335f, 214f));
                Card(badge);
                // Madalya solda, yazılar sağda (çizim yoksa yazılar ortada)
                Sprite medal = IconSprite("badge_" + Progress.Badges[i].id);
                float textX = medal != null ? 56f : 0f, textW = medal != null ? 214f : 310f;
                if (medal != null)
                {
                    badgeIcons[i] = Img("Medal", badge.transform, medal, Color.white, Center, new Vector2(-104f, 0f), new Vector2(116f, 116f));
                    badgeIcons[i].preserveAspect = true;
                }
                // Ad tek satır; sığmayan adı HubController küçültür
                badgeNames[i] = Txt("Name", badge.transform, "", 38, TextAnchor.MiddleCenter, Center, new Vector2(textX, 66f), new Vector2(textW, 52f));
                badgeDescs[i] = Txt("Desc", badge.transform, "", 30, TextAnchor.MiddleCenter, Center, new Vector2(textX, -24f), new Vector2(textW, 124f));
                badgeDescs[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                badgeDescs[i].resizeTextForBestFit = true;
                badgeDescs[i].resizeTextMinSize = 20;
                badgeDescs[i].resizeTextMaxSize = 30;
                badgeDescs[i].verticalOverflow = VerticalWrapMode.Truncate;
                badgeDescs[i].color = new Color(Plum.r, Plum.g, Plum.b, 0.7f);
                badgeCards[i] = badge;
            }

            // 3 Şov
            p = panels[3].transform;
            Text normalMine, starMine, normalButton, starButton;
            Text[] normalRivals = ShowColumn(p, "show_normal", -255f, hub.EnterShow, out normalMine, out normalButton);
            Text[] starRivals = ShowColumn(p, "show_star", 255f, hub.EnterStarShow, out starMine, out starButton);
            Text result = Txt("Result", p, "", 46, TextAnchor.MiddleCenter, Center, new Vector2(0f, -240f), new Vector2(980f, 160f));
            result.color = Coral;
            result.horizontalOverflow = HorizontalWrapMode.Wrap;
            Hint(p, "show_hint", -430f);

            // 4 Ev
            p = panels[4].transform;
            Localize(Title(p, "theme_title"), "theme_title").rectTransform.anchoredPosition = new Vector2(0f, -110f);
            var themeCards = new Image[Decor.All.Length];
            var themeStates = new Text[Decor.All.Length];
            for (int i = 0; i < themeCards.Length; i++)
            {
                Vector2 pos = i < 3 ? new Vector2((i - 1) * 330f, 300f) : new Vector2((i - 3.5f) * 330f, -20f);
                Image card = Img("Theme" + i, p, roundRect, Color.white, Center, pos, new Vector2(300f, 280f));
                Card(card);
                card.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(card.gameObject.AddComponent<Button>().onClick, hub.PickTheme, i);
                Juice(card.gameObject, 0f);
                Localize(Txt("Name", card.transform, Loc.Get(Decor.All[i].NameKey), 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, 50f), new Vector2(290f, 70f)), Decor.All[i].NameKey);
                themeStates[i] = Txt("State", card.transform, "", 34, TextAnchor.MiddleCenter, Center, new Vector2(0f, -60f), new Vector2(290f, 60f));
                themeStates[i].color = Coral;
                themeCards[i] = card;
            }

            // 5 Kulüp
            p = panels[5].transform;
            Text clubTitle = Txt("ClubTitle", p, Loc.Get("club_title"), 70, TextAnchor.MiddleCenter, Center, new Vector2(0f, 580f), new Vector2(1000f, 100f));
            clubTitle.color = new Color32(0x9C, 0x7B, 0xE0, 255);
            Localize(clubTitle, "club_title");
            Text clubStatus = Txt("Status", p, "", 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, 495f), new Vector2(1000f, 70f));
            Image benefitsCard = Img("Benefits", p, roundRect, Color.white, Center, new Vector2(0f, 285f), new Vector2(900f, 330f));
            Sliced(benefitsCard);
            Text benefits = Txt("Text", benefitsCard.transform, Loc.Get("club_benefits"), 34, TextAnchor.MiddleCenter, Center, Vector2.zero, new Vector2(860f, 310f));
            Localize(benefits, "club_benefits");
            Text diamondLabel = Dynamic(ActionButton(p, "club_claim_diamond", DiamondColor, new Vector2(-220f, 45f), hub.ClaimClubDiamond));
            Text boardingLabel = Dynamic(ActionButton(p, "club_claim_boarding", Mint, new Vector2(220f, 45f), hub.ClaimClubBoarding));
            foreach (Text label in new[] { diamondLabel, boardingLabel })
            {
                ((RectTransform)label.transform.parent).sizeDelta = new Vector2(400f, 130f);
                label.rectTransform.sizeDelta = new Vector2(390f, 120f);
            }
            Image monthly = BigButton("Monthly", p, "per_month", Coral, new Vector2(-220f, -115f), new Vector2(400f, 130f), hub.BuyMonthly);
            Text monthlyLabel = monthly.transform.Find("Label").GetComponent<Text>();
            Image yearly = BigButton("Yearly", p, "per_year", new Color32(0x9C, 0x7B, 0xE0, 255), new Vector2(220f, -115f), new Vector2(400f, 130f), hub.BuyYearly);
            Text yearlyLabel = yearly.transform.Find("Label").GetComponent<Text>();
            foreach (Text label in new[] { monthlyLabel, yearlyLabel })
            {
                label.fontSize = 38;
                Object.DestroyImmediate(label.GetComponent<LocalizedText>());
                label.rectTransform.sizeDelta = new Vector2(380f, 110f);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 22;
                label.resizeTextMaxSize = 38;
            }
            Image manage = BigButton("Manage", p, "club_manage", new Color(Plum.r, Plum.g, Plum.b, 0.75f), new Vector2(0f, -270f), new Vector2(700f, 100f), hub.ManageSubscription);
            manage.transform.Find("Label").GetComponent<Text>().fontSize = 36;
            Hint(p, "club_terms", -440f).rectTransform.sizeDelta = new Vector2(980f, 180f);

            Text message = Outlined(Txt("Message", canvas, "", 46, TextAnchor.MiddleCenter, BottomCenter, new Vector2(0f, 70f), new Vector2(1020f, 90f)));
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            ParentalGate gate = BuildGate(canvas);

            SetArray(hub, "tabs", tabs);
            SetArray(hub, "panels", panels);
            SetRef(hub, "goldText", gold);
            SetRef(hub, "diamondText", diamonds);
            SetRef(hub, "messageText", message);
            SetRef(hub, "gate", gate);
            SetRef(hub, "streakText", streak);
            SetArray(hub, "taskNames", taskNames);
            SetArray(hub, "taskIcons", taskIcons);
            SetArray(hub, "taskProgress", taskProgress);
            SetArray(hub, "taskButtons", taskButtons);
            SetArray(hub, "taskButtonLabels", taskLabels);
            SetRef(hub, "passLevelText", level);
            SetRef(hub, "passFill", fill);
            SetArray(hub, "freeTexts", freeTexts);
            SetArray(hub, "freeButtons", freeButtons);
            SetArray(hub, "premiumTexts", premiumTexts);
            SetArray(hub, "premiumButtons", premiumButtons);
            SetRef(hub, "buyPassLabel", buyPassLabel);
            SetArray(hub, "badgeCards", badgeCards);
            SetArray(hub, "badgeNames", badgeNames);
            SetArray(hub, "badgeDescs", badgeDescs);
            if (badgeIcons[0] != null) SetArray(hub, "badgeIcons", badgeIcons);
            SetArray(hub, "normalRivals", normalRivals);
            SetArray(hub, "starRivals", starRivals);
            SetRef(hub, "normalMine", normalMine);
            SetRef(hub, "starMine", starMine);
            SetRef(hub, "normalButtonLabel", normalButton);
            SetRef(hub, "starButtonLabel", starButton);
            SetRef(hub, "showResult", result);
            SetArray(hub, "themeCards", themeCards);
            SetArray(hub, "themeStates", themeStates);
            SetRef(hub, "clubStatus", clubStatus);
            SetRef(hub, "clubDiamondLabel", diamondLabel);
            SetRef(hub, "clubBoardingLabel", boardingLabel);
            SetRef(hub, "monthlyLabel", monthlyLabel);
            SetRef(hub, "yearlyLabel", yearlyLabel);

            EditorSceneManager.SaveScene(scene, ScenePath(HubController.HubScene));
        }

        /// <summary>Açıklama yazısı (küçük, sarmalı).</summary>
        private static Text Hint(Transform parent, string key, float y)
        {
            Text hint = Txt("Hint_" + key, parent, Loc.Get(key), 34, TextAnchor.MiddleCenter, Center, new Vector2(0f, y), new Vector2(960f, 80f));
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.color = new Color(Plum.r, Plum.g, Plum.b, 0.7f);
            Localize(hint, key);
            return hint;
        }

        /// <summary>Satırdaki renkli buton (rengini denetleyici yönetir); yazısını out ile verir.</summary>
        private static Image RowButton(Transform parent, Vector2 pos, Vector2 size, UnityAction<int> onClick, int arg, out Text label)
        {
            Image button = Img("Button", parent, roundRect, Mint, Center, pos, size);
            Jelly(button);
            button.raycastTarget = true;
            UnityEventTools.AddIntPersistentListener(button.gameObject.AddComponent<Button>().onClick, onClick, arg);
            label = White(Txt("Label", button.transform, "", 36, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), size - new Vector2(10f, 6f)));
            return button;
        }

        /// <summary>Şov sütunu: başlık, 4 rakip, dostun puanı ve "Katıl".</summary>
        private static Text[] ShowColumn(Transform parent, string titleKey, float x, UnityAction enter, out Text mine, out Text button)
        {
            Image card = Img("Show_" + titleKey, parent, roundRect, Color.white, Center, new Vector2(x, 260f), new Vector2(480f, 640f));
            Sliced(card);
            Text title = Txt("Title", card.transform, Loc.Get(titleKey), 48, TextAnchor.MiddleCenter, Center, new Vector2(0f, 260f), new Vector2(460f, 70f));
            title.color = Coral;
            Localize(title, titleKey);
            Localize(Txt("Rivals", card.transform, Loc.Get("show_rivals"), 30, TextAnchor.MiddleCenter, Center, new Vector2(0f, 195f), new Vector2(460f, 50f)), "show_rivals");
            var rivals = new Text[PetShow.Rivals.Length];
            for (int i = 0; i < rivals.Length; i++)
                rivals[i] = Txt("Rival" + i, card.transform, "", 38, TextAnchor.MiddleCenter, Center, new Vector2(0f, 130f - i * 60f), new Vector2(460f, 56f));
            mine = Txt("Mine", card.transform, "", 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, -140f), new Vector2(460f, 60f));
            mine.color = new Color32(0x4C, 0xAF, 0x8A, 255);
            Image enterButton = BigButton("Enter", card.transform, "show_enter", Coral, new Vector2(0f, -240f), new Vector2(400f, 100f), enter);
            button = enterButton.transform.Find("Label").GetComponent<Text>();
            button.fontSize = 36;
            Object.DestroyImmediate(button.GetComponent<LocalizedText>());
            return rivals;
        }

        // ---------- Mağaza ----------

        /// <summary>
        /// Kaydırılan ürün listesi (ProductCatalog.All sırası) ve en altta elmas→altın değişimi; altta mesaj,
        /// "Satın alımları geri yükle" ve fiyat/iade notu. Gerçek paralı her alım ebeveyn kapısından geçer.
        /// </summary>
        private static void BuildShopScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(Cream);
            Transform canvas = CreateCanvas();
            Background(canvas, "bg_magaza");
            var shop = canvas.gameObject.AddComponent<ShopController>();

            Localize(Title(canvas, "shop"), "shop").rectTransform.anchoredPosition = new Vector2(0f, -115f);
            BackButton(canvas, shop.Back);
            Text gold = WalletPlate("Gold", canvas, TopRight, new Vector2(-30f, -36f), new Vector2(210f, 84f), GoldColor, false);
            Text diamonds = WalletPlate("Diamonds", canvas, TopRight, new Vector2(-30f, -130f), new Vector2(210f, 84f), DiamondColor, true);
            Text test = Txt("TestBadge", canvas, Loc.Get("shop_test"), 34, TextAnchor.MiddleCenter, TopCenter, new Vector2(0f, -250f), new Vector2(620f, 50f));
            test.color = Coral;
            Localize(test, "shop_test");

            // Kaydırılan liste
            RectTransform view = NewUI("Viewport", canvas);
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(0f, 360f);
            view.offsetMax = new Vector2(0f, -320f);
            view.gameObject.AddComponent<RectMask2D>();
            Image viewHit = view.gameObject.AddComponent<Image>();
            viewHit.color = new Color(1f, 1f, 1f, 0f);
            const float step = 232f;
            int count = ProductCatalog.All.Length;
            RectTransform content = NewUI("Content", view);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, (count + 1) * step + 40f);
            content.anchoredPosition = Vector2.zero;
            // Kartlar alt alta dizilir; gizlenen kart (süresi geçen başlangıç paketi) boşluk bırakmaz
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.spacing = step - 214f;
            layout.padding = new RectOffset(0, 0, 20, 20);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;

            var cards = new GameObject[count];
            var prices = new Text[count];
            var buttons = new Image[count];
            var owned = new Text[count];
            for (int i = 0; i < count; i++)
            {
                Product product = ProductCatalog.All[i];
                Image card = ProductCard(content, i, step, product.NameKey, product.DescKey, ProductIcon(product.id), out Text ownedText);
                Image button = Img("Buy", card.transform, roundRect, Mint, Center, new Vector2(345f, 0f), new Vector2(280f, 130f));
                Jelly(button);
                button.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(button.gameObject.AddComponent<Button>().onClick, shop.Buy, i);
                Juice(button.gameObject, 0f);
                prices[i] = White(Txt("Price", button.transform, "", 40, TextAnchor.MiddleCenter, Center, new Vector2(0f, 4f), new Vector2(262f, 108f)));
                // Uzun fiyat ("449,99 TL / yıl") butondan taşmasın
                prices[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                prices[i].verticalOverflow = VerticalWrapMode.Truncate;
                prices[i].resizeTextForBestFit = true;
                prices[i].resizeTextMinSize = 22;
                prices[i].resizeTextMaxSize = 40;
                prices[i].lineSpacing = 0.85f;
                cards[i] = card.gameObject;
                buttons[i] = button;
                owned[i] = ownedText;
            }

            // Elmas → altın (gerçek para değil)
            Image exchange = ProductCard(content, count, step, "store_exchange", "product_gold_500_desc", "ui_gold", out Text unused);
            Text exchangeText = exchange.transform.Find("Name").GetComponent<Text>();
            Object.DestroyImmediate(exchangeText.GetComponent<LocalizedText>());
            Image exchangeButton = Img("Buy", exchange.transform, roundRect, GoldColor, Center, new Vector2(345f, 0f), new Vector2(280f, 130f));
            Jelly(exchangeButton);
            exchangeButton.raycastTarget = true;
            UnityEventTools.AddPersistentListener(exchangeButton.gameObject.AddComponent<Button>().onClick, shop.Exchange);
            Localize(White(Txt("Label", exchangeButton.transform, Loc.Get("store_exchange_button"), 38, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(250f, 100f))), "store_exchange_button");

            // Alt: mesaj, geri yükleme, not
            Text message = Outlined(Txt("Message", canvas, "", 46, TextAnchor.MiddleCenter, BottomCenter, new Vector2(0f, 270f), new Vector2(1020f, 84f)));
            message.horizontalOverflow = HorizontalWrapMode.Wrap;
            Image restore = Img("Restore", canvas, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.8f), BottomCenter, new Vector2(0f, 140f), new Vector2(620f, 100f));
            Sliced(restore);
            restore.raycastTarget = true;
            UnityEventTools.AddPersistentListener(restore.gameObject.AddComponent<Button>().onClick, shop.Restore);
            Localize(White(Txt("Label", restore.transform, Loc.Get("shop_restore"), 38, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(600f, 90f))), "shop_restore");
            Text note = Txt("Note", canvas, Loc.Get("shop_note"), 28, TextAnchor.MiddleCenter, BottomCenter, new Vector2(0f, 14f), new Vector2(1020f, 120f));
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            note.color = Plum;
            var noteLine = note.gameObject.AddComponent<Outline>();
            noteLine.effectColor = new Color(1f, 1f, 1f, 0.8f);
            noteLine.effectDistance = new Vector2(2f, -2f);
            Localize(note, "shop_note");

            ParentalGate gate = BuildGate(canvas);

            SetRef(shop, "goldText", gold);
            SetRef(shop, "diamondText", diamonds);
            SetArray(shop, "cards", cards);
            SetRef(shop, "scroll", scroll);
            SetArray(shop, "priceTexts", prices);
            SetArray(shop, "priceButtons", buttons);
            SetArray(shop, "ownedTexts", owned);
            SetRef(shop, "exchangeText", exchangeText);
            SetRef(shop, "messageText", message);
            SetRef(shop, "testBadge", test.gameObject);
            SetRef(shop, "gate", gate);

            EditorSceneManager.SaveScene(scene, ScenePath(ShopController.ShopScene));
        }

        /// <summary>Mağaza listesinde bir kart: adı, açıklaması ve "Elinde: 2" satırı (butonu çağıran ekler).</summary>
        private static Image ProductCard(RectTransform content, int index, float step, string nameKey, string descKey, string iconKey, out Text ownedText)
        {
            Image card = Img("Card" + index, content, roundRect, Color.white, TopCenter, new Vector2(0f, -20f - index * step), new Vector2(1030f, 214f));
            Card(card);
            // İkon solda; yazılar ikonun sağından başlar (çizim yoksa eski yerinde)
            Sprite sprite = IconSprite(iconKey);
            float textX = sprite != null ? -105f : -150f, textW = sprite != null ? 540f : 660f;
            if (sprite != null)
            {
                Image icon = Img("Icon", card.transform, sprite, Color.white, Center, new Vector2(-440f, 0f), new Vector2(136f, 136f));
                icon.preserveAspect = true;
            }
            Text title = Txt("Name", card.transform, Loc.Get(nameKey), 50, TextAnchor.MiddleLeft, Center, new Vector2(textX, 66f), new Vector2(textW, 64f));
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 30;
            title.resizeTextMaxSize = 50;
            title.verticalOverflow = VerticalWrapMode.Truncate;
            Localize(title, nameKey);
            Text desc = Txt("Desc", card.transform, Loc.Get(descKey), 34, TextAnchor.MiddleLeft, Center, new Vector2(textX, -4f), new Vector2(textW, 78f));
            desc.horizontalOverflow = HorizontalWrapMode.Wrap;
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 22;
            desc.resizeTextMaxSize = 34;
            desc.verticalOverflow = VerticalWrapMode.Truncate;
            desc.color = new Color(Plum.r, Plum.g, Plum.b, 0.7f);
            Localize(desc, descKey);
            ownedText = Txt("Owned", card.transform, "", 32, TextAnchor.MiddleLeft, Center, new Vector2(textX, -74f), new Vector2(textW, 44f));
            ownedText.color = Coral;
            return card;
        }

        private static string IconOr(string key, string fallback) => IconSprite(key) != null ? key : fallback;

        /// <summary>Mağaza ürününün ikonu (Assets/Art/Icons).</summary>
        private static string ProductIcon(string id)
        {
            switch (id)
            {
                case ProductCatalog.StarterPack: return "ui_gift";
                // Paketler büyüdükçe resim de büyür (çizim yoksa tek ikon)
                case ProductCatalog.Gems10: return IconOr("pack_gem_1", "ui_diamond");
                case ProductCatalog.Gems30: return IconOr("pack_gem_2", "ui_diamond");
                case ProductCatalog.Gems80: return IconOr("pack_gem_3", "ui_diamond");
                case ProductCatalog.Gold500: return IconOr("pack_gold_1", "ui_gold");
                case ProductCatalog.Gold1500: return IconOr("pack_gold_2", "ui_gold");
                case ProductCatalog.Gold4000: return IconOr("pack_gold_3", "ui_gold");
                case ProductCatalog.Slot2:
                case ProductCatalog.Slot3:
                case ProductCatalog.Slot4:
                case ProductCatalog.Slot5: return "ui_paw";
                case ProductCatalog.DinoPack: return "act_star";
                case ProductCatalog.ModeChanger: return "ui_time";
                case ProductCatalog.Shelter: return "ui_heart";
                case ProductCatalog.Boarding: return "ui_calendar";
                case ProductCatalog.ClubMonthly:
                case ProductCatalog.ClubYearly: return "ui_crown";
                case ProductCatalog.Pass: return "ui_ticket";
                case ProductCatalog.PremiumColors: return "mg_star";
                case ProductCatalog.PhotoPack: return "act_photo";
                case ProductCatalog.DecorPack: return "ui_home";
                default: return "ui_shop";
            }
        }

        // ---------- Mini oyunlar ----------

        /// <summary>
        /// Tek sahnede 4 oyun (MiniGame sırası); MiniGameHost seçili olanı açar. Üstte ad, puan ve geri;
        /// oyun bitince sonuç kartı (kazanılan altın, Tekrar, Odaya dön).
        /// </summary>
        private static void BuildMiniGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Color grass = new Color32(0xDC, 0xF2, 0xD8, 255);
            CreateCamera(grass);
            Transform canvas = CreateCanvas();
            Image bg = Img("Background", canvas, null, grass, Center, Vector2.zero, Vector2.zero);
            Stretch(bg.rectTransform);
            var host = canvas.gameObject.AddComponent<MiniGameHost>();
            // Her oyunun kendi arka planı (MiniGame sırası); host seçili oyununkini koyar
            string[] gameBgs = { "bg_oyun_park", "bg_oyun_piknik", "bg_oyun_masa", "bg_oyun_salon", "bg_oyun_park" };
            var bgSprites = new Sprite[gameBgs.Length];
            for (int i = 0; i < gameBgs.Length; i++) bgSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{RoomArtDir}/{gameBgs[i]}.png");
            if (bgSprites[0] != null)
            {
                var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = bgSprites[0].rect.width / bgSprites[0].rect.height;
                SetRef(host, "background", bg);
                SetArray(host, "backgrounds", bgSprites);
            }

            var games = new MiniGameBase[]
            {
                BuildFetch(canvas), BuildCatch(canvas), BuildMemory(canvas), BuildHide(canvas), BuildBubble(canvas)
            };

            // Üst şerit oyunların üstünde çizilsin
            Text title = Txt("Title", canvas, "", 76, TextAnchor.MiddleCenter, TopCenter, new Vector2(0f, -130f), new Vector2(800f, 110f));
            title.color = Plum;
            Text score = Txt("Score", canvas, "", 44, TextAnchor.MiddleCenter, TopCenter, new Vector2(0f, -230f), new Vector2(1000f, 70f));
            score.color = new Color(Plum.r, Plum.g, Plum.b, 0.8f);
            // Çizimli arka planın önünde okunsun: beyaz kontur
            if (bgSprites[0] != null)
            {
                LogoStyle(title);
                score.gameObject.AddComponent<Outline>().effectColor = Color.white;
            }
            BackButton(canvas, host.Back);

            RectTransform result = Overlay("Result", canvas, new Vector2(780f, 860f), out RectTransform card);
            // Kutlama: kartın arkasında dönen ışın patlaması, kartın tepesinde kupa
            Sprite sunburst = IconSprite("cel_sunburst"), trophy = IconSprite("cel_trophy");
            if (sunburst != null)
            {
                Image rays = Img("Sunburst", result, sunburst, new Color(1f, 1f, 1f, 0.85f), Center, new Vector2(0f, 200f), new Vector2(1000f, 1000f));
                rays.preserveAspect = true;
                rays.transform.SetSiblingIndex(card.GetSiblingIndex());   // kartın arkasında
                rays.gameObject.AddComponent<Spin>();
            }
            if (trophy != null)
            {
                Image cup = Img("Trophy", card, trophy, Color.white, Center, new Vector2(0f, 450f), new Vector2(280f, 280f));
                cup.preserveAspect = true;
                Juice(cup.gameObject, 0.05f);
            }
            Text resultText = Txt("Text", card, "", 66, TextAnchor.MiddleCenter, Center, new Vector2(0f, 238f), new Vector2(740f, 160f));
            resultText.color = Coral;
            // 1-3 yıldız (host sırayla doldurur)
            var stars = new Image[3];
            Sprite starSprite = IconSprite("fx_star");
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = Img("Star" + i, card, starSprite ?? circle, starSprite != null ? Color.white : GoldColor, Center,
                    new Vector2((i - 1) * 170f, i == 1 ? 85f : 65f), new Vector2(i == 1 ? 160f : 130f, i == 1 ? 160f : 130f));
                stars[i].preserveAspect = true;
            }
            Text levelText = Txt("Level", card, "", 38, TextAnchor.MiddleCenter, Center, new Vector2(0f, -52f), new Vector2(740f, 90f));
            levelText.color = Plum;
            levelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            levelText.verticalOverflow = VerticalWrapMode.Truncate;
            levelText.resizeTextForBestFit = true;
            levelText.resizeTextMinSize = 24;
            levelText.resizeTextMaxSize = 38;
            SetRef(host, "levelText", levelText);
            Image again = BigButton("Again", card, "game_again", Mint, new Vector2(0f, -175f), new Vector2(560f, 130f), host.Again);
            BigButton("Back", card, "game_back", Coral, new Vector2(0f, -325f), new Vector2(560f, 130f), host.Back)
                .transform.Find("Label").GetComponent<Text>().fontSize = 64;
            again.transform.Find("Label").GetComponent<Text>().fontSize = 64;

            // Rehber el her şeyin üstünde (oyunlar nereyi göstereceğini söyler)
            Image hand = Img("CoachHand", canvas, IconSprite("hand_point") ?? IconSprite("ui_paw"), Color.white, Center, Vector2.zero, new Vector2(200f, 200f));
            hand.preserveAspect = true;
            Isolate(hand.gameObject);
            hand.GetComponent<GraphicRaycaster>().enabled = false;
            var coach = canvas.gameObject.AddComponent<GameCoach>();
            SetRef(coach, "hand", hand);
            SetRef(host, "coach", coach);
            SetArray(host, "resultStars", stars);

            SetArray(host, "games", games);
            SetRef(host, "title", title);
            SetRef(host, "scoreText", score);
            SetRef(host, "result", result.gameObject);
            SetRef(host, "resultText", resultText);
            SetRef(host, "againButton", again.gameObject);

            EditorSceneManager.SaveScene(scene, ScenePath(MiniGameHost.MiniGameScene));
        }

        private static RectTransform GameRoot(Transform canvas, string name)
        {
            RectTransform root = NewUI(name, canvas);
            Stretch(root);
            return root;
        }

        private static Text GameInfo(Transform root, float y)
        {
            Text info = Txt("Info", root, "", 54, TextAnchor.MiddleCenter, Center, new Vector2(0f, y), new Vector2(1000f, 90f));
            info.color = Plum;
            return info;
        }

        private static FetchGame BuildFetch(Transform canvas)
        {
            RectTransform root = GameRoot(canvas, "Fetch");
            var game = root.gameObject.AddComponent<FetchGame>();
            Text info = GameInfo(root, 560f);

            RectTransform pet = Paw("Pet", root, new Vector2(0f, -640f), 0.8f, Coral);
            PetArtSlot(pet, 300f, 90f);   // "AT!" butonunun üstünde kalsın
            Image ball = Img("Ball", root, circle, new Color32(0x4F, 0x8F, 0xE8, 255), Center, new Vector2(0f, -520f), new Vector2(90f, 90f));
            UseIcon(ball, "mg_ball");

            const float barWidth = 800f;
            Image bar = Img("Bar", root, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.2f), Center, new Vector2(0f, -360f), new Vector2(barWidth, 60f));
            Sliced(bar);
            float target = (MiniGameRules.FetchTarget - 0.5f) * barWidth;
            Image good = Img("Good", bar.transform, null, new Color(Mint.r, Mint.g, Mint.b, 0.45f), Center, new Vector2(target, 0f), new Vector2(0.3f * barWidth, 60f));
            Image targetZone = Img("Target", bar.transform, null, Mint, Center, new Vector2(target, 0f), new Vector2(0.12f * barWidth, 60f));
            Image marker = Img("Marker", bar.transform, roundRect, Plum, Center, Vector2.zero, new Vector2(22f, 110f));
            Sliced(marker);

            Image throwButton = BigButton("Throw", root, "fetch_throw", Coral, new Vector2(0f, -800f), new Vector2(520f, 170f), game.Throw);

            SetRef(game, "throwButton", throwButton.rectTransform);
            SetRef(game, "target", targetZone);
            SetRef(game, "good", good.rectTransform);
            SetRef(game, "bar", bar.rectTransform);
            SetRef(game, "marker", marker.rectTransform);
            SetRef(game, "ball", ball.rectTransform);
            SetRef(game, "pet", pet);
            SetRef(game, "info", info);
            return game;
        }

        private static CatchGame BuildCatch(Transform canvas)
        {
            RectTransform root = GameRoot(canvas, "Catch");
            // Bütün alan dokunmayı yakalar (kase parmağı izler)
            Image area = root.gameObject.AddComponent<Image>();
            area.color = new Color(1f, 1f, 1f, 0f);
            var game = root.gameObject.AddComponent<CatchGame>();

            var drops = new Image[16];
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = Img("Drop" + i, root, circle, Coral, Center, Vector2.zero, new Vector2(90f, 90f));
                drops[i].gameObject.SetActive(false);
            }
            Image bowl = Img("Bowl", root, roundRect, new Color32(0xE0, 0x6F, 0x9C, 255), Center, new Vector2(0f, -720f), new Vector2(230f, 90f));
            Sliced(bowl);
            if (UseIcon(bowl, "mg_bowl")) bowl.rectTransform.sizeDelta = new Vector2(300f, 190f);

            SetRef(game, "area", root);
            SetRef(game, "bowl", bowl.rectTransform);
            SetArray(game, "drops", drops);
            SetColors(game, "foodColors", new[] { Coral, GoldColor, Mint, (Color)new Color32(0xF2, 0x8C, 0x5A, 255) });
            var so = new SerializedObject(game);
            so.FindProperty("junkColor").colorValue = new Color32(0x6E, 0x6A, 0x72, 255);
            // Çizimler: düşen yiyecekler ve çöp
            Sprite junk = IconSprite("mg_trash");
            var foods = new List<Sprite>();
            foreach (string key in new[] { "food_fish", "food_carrot", "food_fruit", "food_bone", "food_treat", "food_greens" })
            {
                Sprite s = IconSprite(key);
                if (s != null) foods.Add(s);
            }
            if (junk != null && foods.Count > 0)
            {
                so.FindProperty("junkSprite").objectReferenceValue = junk;
                so.FindProperty("starSprite").objectReferenceValue = IconSprite("fx_star");
                SerializedProperty list = so.FindProperty("foodSprites");
                list.arraySize = foods.Count;
                for (int i = 0; i < foods.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = foods[i];
                foreach (Image drop in drops)
                {
                    drop.preserveAspect = true;
                    drop.rectTransform.sizeDelta = new Vector2(140f, 140f);
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return game;
        }

        private static MemoryGame BuildMemory(Transform canvas)
        {
            RectTransform root = GameRoot(canvas, "Memory");
            var game = root.gameObject.AddComponent<MemoryGame>();
            var faces = new Image[MiniGameRules.MemoryMaxPairs * 2];   // kaçı görüneceğini seviye belirler
            var backs = new Image[faces.Length];
            var symbols = new Image[faces.Length];
            Sprite back = IconSprite("mg_card");
            for (int i = 0; i < faces.Length; i++)
            {
                int row = i / 4, col = i % 4;
                Image face = Img("Card" + i, root, roundRect, Plum, Center, new Vector2((col - 1.5f) * 250f, 300f - row * 270f), new Vector2(230f, 250f));
                Sliced(face);
                face.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(face.gameObject.AddComponent<Button>().onClick, game.Flip, i);
                faces[i] = face;
                if (back == null) continue;
                // Kapalıyken kart arkası, açılınca çiftin resmi
                backs[i] = Img("Back", face.transform, back, Color.white, Center, Vector2.zero, new Vector2(220f, 240f));
                backs[i].preserveAspect = true;
                symbols[i] = Img("Symbol", face.transform, null, Color.white, Center, Vector2.zero, new Vector2(170f, 170f));
                symbols[i].preserveAspect = true;
                symbols[i].gameObject.SetActive(false);
            }
            SetArray(game, "faces", faces);
            if (back != null)
            {
                SetArray(game, "backs", backs);
                SetArray(game, "symbols", symbols);
                var pairs = new List<Sprite>();
                foreach (string key in new[] { "food_fish", "food_carrot", "food_bone", "food_fruit", "food_treat", "food_worm", "food_greens", "food_seeds", "mg_ball", "mg_star", "ui_heart", "ui_paw", "ui_crown", "fx_note" })
                    if (IconSprite(key) != null) pairs.Add(IconSprite(key));
                SetArray(game, "pairSprites", pairs.ToArray());
            }
            SetColors(game, "pairColors", new[]
            {
                Coral, Mint, SkyBlue, GoldColor, (Color)new Color32(0x9C, 0x7B, 0xE0, 255), (Color)new Color32(0xE0, 0x6F, 0x9C, 255)
            });
            var so = new SerializedObject(game);
            so.FindProperty("backColor").colorValue = new Color(Plum.r, Plum.g, Plum.b, 0.85f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return game;
        }

        private static HideGame BuildHide(Transform canvas)
        {
            RectTransform root = GameRoot(canvas, "Hide");
            var game = root.gameObject.AddComponent<HideGame>();
            Text info = GameInfo(root, 450f);
            // Dost kutuların arkasında çizilir: kutu kalkınca görünür
            RectTransform pet = Paw("HiddenPet", root, new Vector2(0f, -100f), 0.9f, Coral);
            PetArtSlot(pet, 300f);
            var boxes = new RectTransform[MiniGameRules.HideMaxBoxes];   // kaçı görüneceğini seviye belirler
            Sprite shell = IconSprite("mg_shell");
            if (shell != null) SetRef(game, "shellSprite", shell);
            for (int i = 0; i < boxes.Length; i++)
            {
                Image box = Img("Box" + i, root, roundRect, new Color32(0xB0, 0x8A, 0x60, 255), Center, new Vector2((i - 1) * 320f, -100f), new Vector2(280f, 260f));
                Sliced(box);
                box.raycastTarget = true;
                UnityEventTools.AddIntPersistentListener(box.gameObject.AddComponent<Button>().onClick, game.Pick, i);
                // Karton kutu çizimi varsa kapak gerekmez
                if (!UseIcon(box, "mg_box"))
                    Img("Lid", box.transform, roundRect, new Color32(0x8A, 0x66, 0x44, 255), Center, new Vector2(0f, 110f), new Vector2(300f, 50f));
                boxes[i] = box.rectTransform;
            }
            SetArray(game, "boxes", boxes);
            SetRef(game, "pet", pet);
            SetRef(game, "info", info);
            return game;
        }

        private static BubbleGame BuildBubble(Transform canvas)
        {
            RectTransform root = GameRoot(canvas, "Bubble");
            var game = root.gameObject.AddComponent<BubbleGame>();
            Sprite art = IconSprite("fx_bubble");
            const int count = 16;
            var bubbles = new Image[count];
            var marks = new Image[count];
            for (int i = 0; i < count; i++)
            {
                bubbles[i] = Img("Bubble" + i, root, art ?? circle, art != null ? Color.white : SkyBlue, Center, Vector2.zero, new Vector2(200f, 200f));
                bubbles[i].preserveAspect = true;
                bubbles[i].raycastTarget = true;
                var hit = bubbles[i].gameObject.AddComponent<BubbleHit>();
                SetRef(hit, "game", game);
                var so = new SerializedObject(hit);
                so.FindProperty("index").intValue = i;
                so.ApplyModifiedPropertiesWithoutUndo();
                marks[i] = Img("Mark", bubbles[i].transform, null, Color.white, Center, Vector2.zero, new Vector2(110f, 110f));
                marks[i].preserveAspect = true;
                marks[i].gameObject.SetActive(false);
                bubbles[i].gameObject.SetActive(false);
            }
            SetRef(game, "area", root);
            SetArray(game, "bubbles", bubbles);
            SetArray(game, "marks", marks);
            SetRef(game, "starSprite", IconSprite("fx_star"));
            SetRef(game, "junkSprite", IconSprite("mg_trash"));
            return game;
        }

        // ---------- Anı Bahçesi ----------

        private static void BuildMemorialScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Color garden = new Color32(0xE4, 0xF1, 0xE0, 255);
            CreateCamera(garden);
            Transform canvas = CreateCanvas();
            if (!Background(canvas, "bg_ani_bahcesi"))
            {
                Object.DestroyImmediate(canvas.Find("Background").gameObject);
                Object.DestroyImmediate(canvas.Find("BlobTop").gameObject);
                Object.DestroyImmediate(canvas.Find("BlobBottom").gameObject);
                Image bg = Img("Background", canvas, null, garden, Center, Vector2.zero, Vector2.zero);
                Stretch(bg.rectTransform);
            }
            var memorial = canvas.gameObject.AddComponent<MemorialController>();

            Localize(Title(canvas, "memorial"), "memorial");
            BackButton(canvas, memorial.Back);

            Image farewell = Img("Farewell", canvas, roundRect, Color.white, TopCenter, new Vector2(0f, -310f), new Vector2(980f, 280f));
            Sliced(farewell);
            Text farewellText = Txt("Text", farewell.transform, "", 44, TextAnchor.MiddleCenter, Center, new Vector2(0f, 0f), new Vector2(920f, 260f));
            farewellText.horizontalOverflow = HorizontalWrapMode.Wrap;

            Text empty = Txt("Empty", canvas, Loc.Get("memorial_empty"), 46, TextAnchor.MiddleCenter, Center, new Vector2(0f, 100f), new Vector2(900f, 80f));
            empty.color = new Color(Plum.r, Plum.g, Plum.b, 0.6f);
            Localize(empty, "memorial_empty");

            const int slots = 6;
            var cards = new GameObject[slots];
            var badges = new Image[slots];
            var names = new Text[slots];
            var infos = new Text[slots];
            var causes = new Text[slots];
            for (int i = 0; i < slots; i++)
            {
                Vector2 pos = new Vector2((i % 2 == 0 ? -1f : 1f) * 245f, 130f - (i / 2) * 250f);
                Image card = Img("Card" + i, canvas, roundRect, new Color(1f, 1f, 1f, 0.9f), Center, pos, new Vector2(470f, 225f));
                Sliced(card);
                badges[i] = Img("Badge", card.transform, circle, Coral, Center, new Vector2(-150f, 10f), new Vector2(130f, 130f));
                Paw("Paw", badges[i].transform, Vector2.zero, 0.45f, new Color(1f, 1f, 1f, 0.7f));
                names[i] = Txt("Name", card.transform, "", 46, TextAnchor.MiddleLeft, Center, new Vector2(70f, 55f), new Vector2(290f, 60f));
                infos[i] = Txt("Info", card.transform, "", 30, TextAnchor.MiddleLeft, Center, new Vector2(70f, 0f), new Vector2(290f, 50f));
                infos[i].color = new Color(Plum.r, Plum.g, Plum.b, 0.7f);
                causes[i] = Txt("Cause", card.transform, "", 30, TextAnchor.MiddleLeft, Center, new Vector2(70f, -50f), new Vector2(290f, 50f));
                causes[i].color = Coral;
                cards[i] = card.gameObject;
            }

            Image newPal = BigButton("NewPal", canvas, "new_pal", Coral, new Vector2(0f, -760f), new Vector2(620f, 160f), memorial.NewPal);

            SetRef(memorial, "farewell", farewell.gameObject);
            SetRef(memorial, "farewellText", farewellText);
            SetRef(memorial, "emptyText", empty);
            SetArray(memorial, "cards", cards);
            SetArray(memorial, "cardBadges", badges);
            SetArray(memorial, "cardNames", names);
            SetArray(memorial, "cardInfos", infos);
            SetArray(memorial, "cardCauses", causes);
            SetRef(memorial, "newPalButton", newPal.gameObject);

            EditorSceneManager.SaveScene(scene, ScenePath(MemorialController.MemorialScene));
        }

        /// <summary>Bir sıradaki <paramref name="count"/> butondan <paramref name="index"/>. olanın x konumu.</summary>
        private static Vector2 Slot(int index, int count) => new Vector2((index - (count - 1) * 0.5f) * 264f, 0f);

        /// <summary>Oyun ekranının alt çubuğu (oda sekmeleri); aksiyon butonları hemen üstünde.</summary>
        private const float TabBarHeight = 230f;

        /// <summary>Oda aksiyon butonu (yazılı yuvarlak kare); etiketini döndürür.</summary>
        private static Text ActionButton(Transform parent, string key, Color color, Vector2 pos, UnityAction onClick,
            UnityAction<int> onIntClick = null, int arg = 0)
        {
            Image button = Img("Btn_" + key, parent, roundRect, color, Center, pos, new Vector2(250f, 210f));
            Jelly(button);
            button.raycastTarget = true;
            var b = button.gameObject.AddComponent<Button>();
            if (onIntClick != null) UnityEventTools.AddIntPersistentListener(b.onClick, onIntClick, arg);
            else UnityEventTools.AddPersistentListener(b.onClick, onClick);
            Juice(button.gameObject, 0f);
            Text label = White(Txt("Label", button.transform, Loc.Get(key), 46, TextAnchor.MiddleCenter, Center, new Vector2(0f, 2f), new Vector2(220f, 150f)));
            Localize(label, key);
            return label;
        }

        /// <summary>Assets/Art/Icons/icon_&lt;key&gt;.png; çizim yoksa null.</summary>
        private static Sprite IconSprite(string key) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconArtDir}/icon_{key}.png");

        /// <summary>
        /// Butonun sağ üst köşesine fiyat rozeti (altın ya da elmas ikonu + sayı); yazısını döndürür. Rozet =
        /// yazının ebeveyni; denetleyici fiyat 0 iken gizler. İkon çizimi yoksa null.
        /// </summary>
        private static Text PriceChip(Text label, bool diamond)
        {
            Sprite coin = IconSprite(diamond ? "fx_gem" : "fx_coin");
            if (coin == null) return null;
            var button = (RectTransform)label.transform.parent;
            Image chip = Img("Price", button, roundRect, diamond ? DiamondColor : new Color32(0xFF, 0xB8, 0x2E, 255), Center,
                new Vector2(button.sizeDelta.x * 0.5f - 40f, button.sizeDelta.y * 0.5f - 4f), new Vector2(118f, 56f));
            Jelly(chip);
            Image icon = Img("Coin", chip.transform, coin, Color.white, Center, new Vector2(-32f, 4f), new Vector2(44f, 44f));
            icon.preserveAspect = true;
            Text value = White(Txt("Value", chip.transform, "", 34, TextAnchor.MiddleCenter, Center, new Vector2(18f, 6f), new Vector2(70f, 50f)));
            value.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.25f);
            chip.gameObject.SetActive(false);
            return value;
        }

        /// <summary>Butonu dostun üstüne sürüklenebilir yapar (bırakınca butona basılmış sayılır).</summary>
        private static void DragOntoPet(GameObject button, Image icon, RectTransform target)
        {
            if (icon == null) return;
            var drag = button.AddComponent<DragToPet>();
            SetRef(drag, "target", target);
            SetRef(drag, "icon", icon);
        }

        /// <summary>Halka (ikon halkalarının çerçevesi ve dolumu): beyaz, kenarları yumuşak.</summary>
        private static Sprite CreateRing(string name, int size, float thickness)
        {
            string path = $"{PlaceholderDir}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float c = size * 0.5f, outer = c - 1f, inner = outer - thickness;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f)));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            return ImportShape(path, Vector4.zero);
        }

        /// <summary>Yer tutucu şeklin yerine ikon çizimini koyar; çizim yoksa false.</summary>
        private static bool UseIcon(Image image, string key)
        {
            Sprite sprite = IconSprite(key);
            if (sprite == null) return false;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = true;
            return true;
        }

        /// <summary>
        /// Mini oyundaki pati yer tutucusuna kapalı bir "Art" çocuğu ekler; oyun açılınca dostun çizimi varsa
        /// MiniGameBase.ShowPetArt onu açıp patiyi gizler.
        /// </summary>
        private static void PetArtSlot(RectTransform pet, float size, float raise = 0f)
        {
            Image art = Img("Art", pet, null, Color.white, Center, new Vector2(0f, raise), new Vector2(size, size));
            art.preserveAspect = true;
            art.gameObject.SetActive(false);
        }

        /// <summary>
        /// Aksiyon butonuna üstte ikon koyar, yazıyı alta sığdırır. İkon çizimi yoksa buton olduğu gibi kalır
        /// (null döner).
        /// </summary>
        private static Image ButtonIcon(Text label, string key, bool iconOnly = false)
        {
            Sprite sprite = IconSprite(key);
            if (sprite == null) return null;
            var button = (RectTransform)label.transform.parent;
            float w = button.sizeDelta.x, h = button.sizeDelta.y;
            if (iconOnly)
            {
                // Üstte büyük ikon, altında kısa ve okunaklı ad (denetleyici yazabilir)
                float big = h * 0.6f;
                Image only = Img("Icon", button, sprite, Color.white, Center, new Vector2(0f, h * 0.5f - big * 0.5f - 6f), new Vector2(big, big));
                only.preserveAspect = true;
                only.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
                label.rectTransform.anchoredPosition = new Vector2(0f, -h * 0.5f + 40f);
                label.rectTransform.sizeDelta = new Vector2(w - 14f, 58f);
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.resizeTextForBestFit = true;
                label.resizeTextMaxSize = 42;
                label.resizeTextMinSize = 22;
                label.lineSpacing = 0.85f;
                return only;
            }
            // Üstte ikon (yüksekliğin %56'sı), altta kalan şeritte iki satıra kadar yazı; üst üste binmezler
            const float margin = 6f;
            float side = h * 0.56f;
            Image icon = Img("Icon", button, sprite, Color.white, Center, new Vector2(0f, h * 0.5f - margin - side * 0.5f), new Vector2(side, side));
            icon.preserveAspect = true;
            icon.transform.SetSiblingIndex(label.transform.GetSiblingIndex());   // parlaklığın üstünde, yazının altında
            float textHeight = h - side - margin * 2f;
            label.rectTransform.anchoredPosition = new Vector2(0f, -h * 0.5f + margin + textHeight * 0.5f);
            label.rectTransform.sizeDelta = new Vector2(w - 12f, textHeight);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = Mathf.Min(label.fontSize, 30);
            label.resizeTextMinSize = 16;
            label.lineSpacing = 0.78f;
            return icon;
        }

        /// <summary>Büyük yazılı buton (Sahiplen gibi).</summary>
        private static Image BigButton(string name, Transform parent, string key, Color color, Vector2 pos, Vector2 size, UnityAction onClick)
        {
            Image button = Img(name, parent, roundRect, color, Center, pos, size);
            Jelly(button);
            button.raycastTarget = true;
            UnityEventTools.AddPersistentListener(button.gameObject.AddComponent<Button>().onClick, onClick);
            Juice(button.gameObject, 0.03f);
            Localize(White(Txt("Label", button.transform, Loc.Get(key), 78, TextAnchor.MiddleCenter, Center, new Vector2(0f, 4f), size)), key);
            return button;
        }

        /// <summary>
        /// Şekillerden yer tutucu kedi. Kök (Body) pivotu ayakların altında; yetişkin boyu yaklaşık 620 px.
        /// Kuyruk, gövde, patiler, baş ve kulaklar sahiplenirken seçilen renge boyanır.
        /// </summary>
        private static PetView Critter(Transform parent, Vector2 floorPos)
        {
            RectTransform root = NewUI("Pet", parent);
            Place(root, Center, floorPos, new Vector2(600f, 640f));
            RectTransform body = NewUI("Body", root);
            Place(body, BottomCenter, Vector2.zero, new Vector2(600f, 640f));
            // Şekillerden yer tutucu (gerçek çizim gelince PetView kapatır)
            RectTransform shapes = NewUI("Shapes", body);
            Stretch(shapes);

            Color dark = new Color32(0x3B, 0x2A, 0x44, 255);
            Color pink = new Color32(0xFF, 0x9E, 0xAE, 255);
            Color orange = new Color32(0xF5, 0xA0, 0x30, 255);
            Color white = new Color(1f, 1f, 1f, 0.55f);
            var tinted = new List<Object>();
            var looks = new List<Object>();

            // Türe göre açılan parça grubu (PetLooks): gövdeyle aynı alanı kaplar, koordinatlar aynı
            RectTransform Look(string name)
            {
                RectTransform rt = NewUI(name, shapes);
                Stretch(rt);
                looks.Add(rt.gameObject);
                return rt;
            }
            // Dostun rengine boyanan parça
            Image Skin(string name, Transform parent, Sprite sprite, Vector2 center, Vector2 size, float rotation)
            {
                Image img = Part(name, parent, sprite, Color.white, center, size, rotation, BottomCenter);
                tinted.Add(img);
                return img;
            }

            // Arkadakiler: diken, kuyruklar
            RectTransform spikes = Look("Look_Spikes");
            Part("Back", spikes, circle, new Color32(0x7A, 0x5C, 0x48, 255), new Vector2(0f, 300f), new Vector2(470f, 470f), 0f, BottomCenter);
            for (int i = 0; i < 9; i++)
            {
                float angle = Mathf.Lerp(-80f, 80f, i / 8f);
                Vector2 pos = new Vector2(0f, 300f) + (Vector2)(Quaternion.Euler(0f, 0f, -angle) * new Vector2(0f, 240f));
                Part("Spike" + i, spikes, triangle, new Color32(0x5E, 0x46, 0x36, 255), pos, new Vector2(80f, 110f), -angle, BottomCenter);
            }
            Image tail = Skin("Tail", Look("Look_TailLong"), roundRect, new Vector2(205f, 200f), new Vector2(70f, 260f), -30f);
            Sliced(tail);
            Sliced(Skin("Tail", Look("Look_TailShort"), roundRect, new Vector2(195f, 175f), new Vector2(56f, 140f), -50f));
            Skin("Tail", Look("Look_TailFeathers"), triangle, new Vector2(160f, 110f), new Vector2(100f, 240f), 145f);
            Skin("Tail", Look("Look_FishTail"), triangle, new Vector2(245f, 170f), new Vector2(200f, 190f), 90f);
            Skin("Tail", Look("Look_DinoTail"), triangle, new Vector2(250f, 110f), new Vector2(150f, 300f), -105f);

            Skin("Torso", shapes, circle, new Vector2(0f, 160f), new Vector2(400f, 320f), 0f);
            Part("Belly", shapes, circle, white, new Vector2(0f, 135f), new Vector2(230f, 200f), 0f, BottomCenter);
            Part("Puff", Look("Look_TailPuff"), circle, Color.white, new Vector2(200f, 85f), new Vector2(96f, 96f), 0f, BottomCenter);

            RectTransform shell = Look("Look_Shell");
            Part("Shell", shell, circle, new Color32(0x4E, 0x7A, 0x40, 255), new Vector2(0f, 195f), new Vector2(450f, 310f), 0f, BottomCenter);
            for (int i = 0; i < 3; i++)
                Part("Plate" + i, shell, circle, new Color32(0x6E, 0x9A, 0x58, 255), new Vector2((i - 1) * 120f, 210f + (i == 1 ? 30f : 0f)), new Vector2(110f, 100f), 0f, BottomCenter);

            RectTransform wings = Look("Look_Wings");
            Skin("WingL", wings, circle, new Vector2(-200f, 190f), new Vector2(120f, 210f), -20f);
            Skin("WingR", wings, circle, new Vector2(200f, 190f), new Vector2(120f, 210f), 20f);
            RectTransform fins = Look("Look_Fins");
            Skin("FinL", fins, triangle, new Vector2(-215f, 150f), new Vector2(90f, 110f), 100f);
            Skin("FinR", fins, triangle, new Vector2(140f, 330f), new Vector2(90f, 120f), -20f);

            RectTransform paws = Look(PetLooks.Paws);
            Skin("PawL", paws, circle, new Vector2(-95f, 22f), new Vector2(120f, 64f), 0f);
            Skin("PawR", paws, circle, new Vector2(95f, 22f), new Vector2(120f, 64f), 0f);
            RectTransform birdFeet = Look("Look_BirdFeet");
            Part("FootL", birdFeet, roundRect, orange, new Vector2(-70f, 14f), new Vector2(100f, 32f), 0f, BottomCenter);
            Part("FootR", birdFeet, roundRect, orange, new Vector2(70f, 14f), new Vector2(100f, 32f), 0f, BottomCenter);

            // Kulaklar (başın arkasında)
            RectTransform earsPointy = Look("Look_EarsPointy");
            Skin("EarL", earsPointy, triangle, new Vector2(-100f, 545f), new Vector2(120f, 140f), 18f);
            Skin("EarR", earsPointy, triangle, new Vector2(100f, 545f), new Vector2(120f, 140f), -18f);
            Part("InnerEarL", earsPointy, triangle, pink, new Vector2(-98f, 535f), new Vector2(60f, 74f), 18f, BottomCenter);
            Part("InnerEarR", earsPointy, triangle, pink, new Vector2(98f, 535f), new Vector2(60f, 74f), -18f, BottomCenter);
            RectTransform earsLong = Look("Look_EarsLong");
            Skin("EarL", earsLong, circle, new Vector2(-70f, 630f), new Vector2(92f, 270f), 8f);
            Skin("EarR", earsLong, circle, new Vector2(70f, 630f), new Vector2(92f, 270f), -8f);
            Part("InnerEarL", earsLong, circle, pink, new Vector2(-70f, 630f), new Vector2(46f, 190f), 8f, BottomCenter);
            Part("InnerEarR", earsLong, circle, pink, new Vector2(70f, 630f), new Vector2(46f, 190f), -8f, BottomCenter);
            RectTransform earsRound = Look("Look_EarsRound");
            Skin("EarL", earsRound, circle, new Vector2(-112f, 520f), new Vector2(104f, 104f), 0f);
            Skin("EarR", earsRound, circle, new Vector2(112f, 520f), new Vector2(104f, 104f), 0f);
            Part("InnerEarL", earsRound, circle, pink, new Vector2(-112f, 520f), new Vector2(56f, 56f), 0f, BottomCenter);
            Part("InnerEarR", earsRound, circle, pink, new Vector2(112f, 520f), new Vector2(56f, 56f), 0f, BottomCenter);

            // Dinozor: sırt plakaları ve triceratops yakası (başın arkasında)
            RectTransform plates = Look("Look_Plates");
            Color plateColor = new Color32(0x3E, 0x6A, 0x34, 230);
            for (int i = 0; i < 3; i++)
                Part("Plate" + i, plates, triangle, plateColor, new Vector2((i - 1) * 80f, 550f + (i == 1 ? 25f : 0f)), new Vector2(80f, 100f), (1 - i) * 18f, BottomCenter);
            RectTransform frill = Look("Look_Frill");
            Skin("Frill", frill, circle, new Vector2(0f, 470f), new Vector2(440f, 300f), 0f);
            Part("FrillInner", frill, circle, new Color(1f, 1f, 1f, 0.3f), new Vector2(0f, 480f), new Vector2(360f, 230f), 0f, BottomCenter);

            Skin("Head", shapes, circle, new Vector2(0f, 400f), new Vector2(320f, 290f), 0f);

            // Dinozor: boynuzlar ve benekler (başın önünde)
            RectTransform horns = Look("Look_Horns");
            Part("HornL", horns, triangle, Cream, new Vector2(-75f, 535f), new Vector2(46f, 90f), 12f, BottomCenter);
            Part("HornR", horns, triangle, Cream, new Vector2(75f, 535f), new Vector2(46f, 90f), -12f, BottomCenter);
            Part("HornNose", horns, triangle, Cream, new Vector2(0f, 372f), new Vector2(40f, 56f), 0f, BottomCenter);
            RectTransform spots = Look("Look_Spots");
            Color spot = new Color(0f, 0f, 0f, 0.15f);
            Part("Spot1", spots, circle, spot, new Vector2(-110f, 220f), new Vector2(70f, 56f), 0f, BottomCenter);
            Part("Spot2", spots, circle, spot, new Vector2(120f, 180f), new Vector2(56f, 46f), 0f, BottomCenter);
            Part("Spot3", spots, circle, spot, new Vector2(90f, 480f), new Vector2(46f, 40f), 0f, BottomCenter);

            // Başın önünde: sarkık kulak, yanak, gözler, burun, gaga
            RectTransform earsFloppy = Look("Look_EarsFloppy");
            Skin("EarL", earsFloppy, circle, new Vector2(-150f, 385f), new Vector2(110f, 220f), -15f);
            Skin("EarR", earsFloppy, circle, new Vector2(150f, 385f), new Vector2(110f, 220f), 15f);
            RectTransform cheeks = Look("Look_Cheeks");
            Part("CheekL", cheeks, circle, white, new Vector2(-100f, 360f), new Vector2(120f, 96f), 0f, BottomCenter);
            Part("CheekR", cheeks, circle, white, new Vector2(100f, 360f), new Vector2(120f, 96f), 0f, BottomCenter);

            Image eyeL = Part("EyeL", shapes, circle, dark, new Vector2(-64f, 415f), new Vector2(48f, 62f), 0f, BottomCenter);
            Image eyeR = Part("EyeR", shapes, circle, dark, new Vector2(64f, 415f), new Vector2(48f, 62f), 0f, BottomCenter);
            Part("Shine", eyeL.transform, circle, Color.white, new Vector2(8f, 12f), new Vector2(16f, 16f), 0f, Center);
            Part("Shine", eyeR.transform, circle, Color.white, new Vector2(8f, 12f), new Vector2(16f, 16f), 0f, Center);
            Part("Nose", Look(PetLooks.Nose), circle, pink, new Vector2(0f, 368f), new Vector2(36f, 26f), 0f, BottomCenter);
            Part("Beak", Look("Look_Beak"), triangle, orange, new Vector2(0f, 352f), new Vector2(64f, 60f), 180f, BottomCenter);
            Part("Bill", Look("Look_Bill"), roundRect, orange, new Vector2(0f, 355f), new Vector2(150f, 58f), 0f, BottomCenter);

            // Gerçek çizim (PetArt): ayakları gövdenin altında, kare
            Image artImage = Img("Art", body, null, Color.white, BottomCenter, Vector2.zero, new Vector2(620f, 620f));
            artImage.preserveAspect = true;
            artImage.gameObject.SetActive(false);
            // Çizimin üstündeki kıyafetler (PetView yerleştirir). ClothingSlot sırasıyla tutulur ama çizim sırası
            // alttan üste: kıyafet, ayak, boyun, gözlük, şapka
            var artClothes = new Image[5];
            foreach (int slot in new[] { 3, 4, 2, 1, 0 })
            {
                Image layer = Img("Cloth_" + (ClothingSlot)slot, artImage.transform, null, Color.white, Vector2.zero, Vector2.zero, new Vector2(100f, 100f));
                layer.rectTransform.pivot = Center;
                layer.preserveAspect = true;
                layer.gameObject.SetActive(false);
                artClothes[slot] = layer;
            }

            Image[] dirt;
            if (IconSprite("mud_1") != null)
            {
                // Sevimli çamur çıkartmaları (saydamlığını PetView kirliliğe göre ayarlar)
                Image Mud(string name, string key, Vector2 pos, float size, float angle)
                {
                    Image m = Part(name, body, IconSprite(key), new Color(1f, 1f, 1f, 0f), pos, new Vector2(size, size), angle, BottomCenter);
                    m.preserveAspect = true;
                    return m;
                }
                dirt = new[]
                {
                    Mud("Dirt1", "mud_1", new Vector2(-90f, 170f), 110f, 10f),
                    Mud("Dirt2", "mud_6", new Vector2(105f, 110f), 86f, -20f),
                    Mud("Dirt3", "mud_2", new Vector2(45f, 400f), 74f, 0f),
                    Mud("Dirt4", "mud_5", new Vector2(-30f, 290f), 64f, 30f),
                };
            }
            else
            {
                Color mud = new Color32(0x7A, 0x5C, 0x3E, 0);
                dirt = new[]
                {
                    Part("Dirt1", body, circle, mud, new Vector2(-95f, 185f), new Vector2(76f, 60f), 0f, BottomCenter),
                    Part("Dirt2", body, circle, mud, new Vector2(115f, 120f), new Vector2(60f, 50f), 0f, BottomCenter),
                    Part("Dirt3", body, circle, mud, new Vector2(50f, 440f), new Vector2(54f, 44f), 0f, BottomCenter),
                };
            }

            GameObject[] clothes = Clothes(body);

            // Uyku: Zzz bulutu çizimi (yoksa yazı); ayrıca denetleyici başın üstünden Zzz parçacıkları yükseltir
            GameObject zzz;
            Sprite zzzSprite = IconSprite("fx_zzz");
            if (zzzSprite != null)
            {
                Image cloud = Img("Zzz", body, zzzSprite, Color.white, BottomCenter, new Vector2(250f, 560f), new Vector2(150f, 150f));
                cloud.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                cloud.preserveAspect = true;
                zzz = cloud.gameObject;
            }
            else
            {
                Text zzzText = Txt("Zzz", body, "Zz", 80, TextAnchor.MiddleCenter, BottomCenter, new Vector2(250f, 560f), new Vector2(200f, 120f));
                zzzText.color = new Color32(0x8A, 0x74, 0xC9, 255);
                zzz = zzzText.gameObject;
            }

            // Kök (sahne konumu) ile gövde (ölçeklenen) ayrı: gövdenin pivotu ayak altında
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = floorPos;

            var view = root.gameObject.AddComponent<PetView>();
            SetRef(view, "body", body);
            SetArray(view, "tinted", tinted.ToArray());
            SetArray(view, "eyes", new Object[] { eyeL.rectTransform, eyeR.rectTransform });
            SetRef(view, "zzz", zzz.gameObject);
            SetArray(view, "dirt", dirt);
            SetArray(view, "looks", looks.ToArray());
            SetArray(view, "clothes", clothes);
            SetRef(view, "shapes", shapes.gameObject);
            SetRef(view, "art", artImage);
            SetArray(view, "artClothes", artClothes);
            return view;
        }

        /// <summary>Kıyafetlerin yer tutucuları (Item_&lt;id&gt;); PetView giyilenleri açar.</summary>
        private static GameObject[] Clothes(RectTransform body)
        {
            var items = new List<GameObject>();
            RectTransform Item(string id)
            {
                RectTransform rt = NewUI("Item_" + id, body);
                Stretch(rt);
                items.Add(rt.gameObject);
                return rt;
            }
            Color C(string id) => PetLooks.ItemColors[id];
            Color glassDark = new Color32(0x22, 0x22, 0x2A, 240);

            RectTransform outfit = Item("sweater");
            Sliced(Part("Body", outfit, roundRect, C("sweater"), new Vector2(0f, 165f), new Vector2(360f, 170f), 0f, BottomCenter));
            outfit = Item("raincoat");
            Sliced(Part("Body", outfit, roundRect, C("raincoat"), new Vector2(0f, 170f), new Vector2(390f, 210f), 0f, BottomCenter));
            Part("CollarL", outfit, triangle, Color.white, new Vector2(-45f, 250f), new Vector2(70f, 60f), 180f, BottomCenter);
            Part("CollarR", outfit, triangle, Color.white, new Vector2(45f, 250f), new Vector2(70f, 60f), 180f, BottomCenter);

            RectTransform feet = Item("booties");
            Sliced(Part("BootL", feet, roundRect, C("booties"), new Vector2(-95f, 22f), new Vector2(132f, 68f), 0f, BottomCenter));
            Sliced(Part("BootR", feet, roundRect, C("booties"), new Vector2(95f, 22f), new Vector2(132f, 68f), 0f, BottomCenter));

            RectTransform neck = Item("bow_tie");
            Part("Left", neck, triangle, C("bow_tie"), new Vector2(-40f, 270f), new Vector2(80f, 80f), 90f, BottomCenter);
            Part("Right", neck, triangle, C("bow_tie"), new Vector2(40f, 270f), new Vector2(80f, 80f), -90f, BottomCenter);
            Part("Knot", neck, circle, C("bow_tie"), new Vector2(0f, 270f), new Vector2(38f, 38f), 0f, BottomCenter);
            neck = Item("scarf");
            Sliced(Part("Band", neck, roundRect, C("scarf"), new Vector2(0f, 275f), new Vector2(290f, 58f), 0f, BottomCenter));
            Sliced(Part("End", neck, roundRect, C("scarf"), new Vector2(75f, 215f), new Vector2(58f, 120f), 10f, BottomCenter));

            RectTransform glasses = Item("round_glasses");
            Part("FrameL", glasses, circle, C("round_glasses"), new Vector2(-64f, 415f), new Vector2(96f, 96f), 0f, BottomCenter);
            Part("FrameR", glasses, circle, C("round_glasses"), new Vector2(64f, 415f), new Vector2(96f, 96f), 0f, BottomCenter);
            Part("LensL", glasses, circle, new Color(1f, 1f, 1f, 0.35f), new Vector2(-64f, 415f), new Vector2(74f, 74f), 0f, BottomCenter);
            Part("LensR", glasses, circle, new Color(1f, 1f, 1f, 0.35f), new Vector2(64f, 415f), new Vector2(74f, 74f), 0f, BottomCenter);
            Part("Bridge", glasses, roundRect, C("round_glasses"), new Vector2(0f, 425f), new Vector2(44f, 12f), 0f, BottomCenter);
            glasses = Item("sunglasses");
            Sliced(Part("LensL", glasses, roundRect, glassDark, new Vector2(-66f, 412f), new Vector2(104f, 72f), 0f, BottomCenter));
            Sliced(Part("LensR", glasses, roundRect, glassDark, new Vector2(66f, 412f), new Vector2(104f, 72f), 0f, BottomCenter));
            Part("Bridge", glasses, roundRect, glassDark, new Vector2(0f, 428f), new Vector2(40f, 12f), 0f, BottomCenter);

            RectTransform hat = Item("party_hat");
            Part("Cone", hat, triangle, C("party_hat"), new Vector2(0f, 610f), new Vector2(140f, 180f), 0f, BottomCenter);
            Part("Pompom", hat, circle, Color.white, new Vector2(0f, 700f), new Vector2(44f, 44f), 0f, BottomCenter);
            hat = Item("cap");
            Part("Dome", hat, circle, C("cap"), new Vector2(0f, 555f), new Vector2(220f, 130f), 0f, BottomCenter);
            Sliced(Part("Brim", hat, roundRect, C("cap"), new Vector2(70f, 520f), new Vector2(190f, 38f), 0f, BottomCenter));
            hat = Item("crown");
            Sliced(Part("Band", hat, roundRect, C("crown"), new Vector2(0f, 572f), new Vector2(180f, 70f), 0f, BottomCenter));
            for (int i = 0; i < 3; i++)
                Part("Point" + i, hat, triangle, C("crown"), new Vector2((i - 1) * 58f, 628f + (i == 1 ? 10f : 0f)), new Vector2(56f, 76f), 0f, BottomCenter);

            // Etkinlik, Pass ve kulüp parçaları (M5)
            hat = Item("witch_hat");
            Sliced(Part("Brim", hat, roundRect, C("witch_hat"), new Vector2(0f, 540f), new Vector2(300f, 40f), 0f, BottomCenter));
            Part("Cone", hat, triangle, C("witch_hat"), new Vector2(15f, 640f), new Vector2(170f, 210f), -8f, BottomCenter);
            Part("Band", hat, roundRect, new Color32(0xFF, 0x9A, 0x3C, 255), new Vector2(5f, 570f), new Vector2(150f, 24f), 0f, BottomCenter);
            hat = Item("santa_hat");
            Part("Cone", hat, triangle, C("santa_hat"), new Vector2(20f, 620f), new Vector2(170f, 170f), -18f, BottomCenter);
            Sliced(Part("Fur", hat, roundRect, Color.white, new Vector2(0f, 545f), new Vector2(230f, 50f), 0f, BottomCenter));
            Part("Pompom", hat, circle, Color.white, new Vector2(60f, 700f), new Vector2(50f, 50f), 0f, BottomCenter);
            hat = Item("sun_hat");
            Part("Brim", hat, circle, C("sun_hat"), new Vector2(0f, 545f), new Vector2(380f, 90f), 0f, BottomCenter);
            Part("Dome", hat, circle, C("sun_hat"), new Vector2(0f, 580f), new Vector2(200f, 120f), 0f, BottomCenter);
            Part("Band", hat, roundRect, new Color32(0xE0, 0x6F, 0x9C, 255), new Vector2(0f, 560f), new Vector2(190f, 22f), 0f, BottomCenter);
            hat = Item("flower_crown");
            for (int i = 0; i < 5; i++)
            {
                Color petal = i % 2 == 0 ? C("flower_crown") : (Color)new Color32(0xFF, 0xF1, 0x8A, 255);
                Part("Flower" + i, hat, circle, petal, new Vector2((i - 2) * 52f, 555f + (i == 2 ? 12f : 0f)), new Vector2(56f, 56f), 0f, BottomCenter);
            }

            glasses = Item("star_glasses");
            Part("StarL", glasses, triangle, C("star_glasses"), new Vector2(-64f, 425f), new Vector2(110f, 90f), 0f, BottomCenter);
            Part("StarL2", glasses, triangle, C("star_glasses"), new Vector2(-64f, 405f), new Vector2(110f, 90f), 180f, BottomCenter);
            Part("StarR", glasses, triangle, C("star_glasses"), new Vector2(64f, 425f), new Vector2(110f, 90f), 0f, BottomCenter);
            Part("StarR2", glasses, triangle, C("star_glasses"), new Vector2(64f, 405f), new Vector2(110f, 90f), 180f, BottomCenter);

            neck = Item("star_bow");
            Part("Left", neck, triangle, C("star_bow"), new Vector2(-40f, 270f), new Vector2(80f, 80f), 90f, BottomCenter);
            Part("Right", neck, triangle, C("star_bow"), new Vector2(40f, 270f), new Vector2(80f, 80f), -90f, BottomCenter);
            Part("Knot", neck, circle, new Color32(0xFF, 0xC9, 0x3C, 255), new Vector2(0f, 270f), new Vector2(40f, 40f), 0f, BottomCenter);
            neck = Item("club_bow");
            Part("Left", neck, triangle, C("club_bow"), new Vector2(-46f, 270f), new Vector2(92f, 92f), 90f, BottomCenter);
            Part("Right", neck, triangle, C("club_bow"), new Vector2(46f, 270f), new Vector2(92f, 92f), -90f, BottomCenter);
            Part("Knot", neck, circle, Coral, new Vector2(0f, 270f), new Vector2(42f, 42f), 0f, BottomCenter);

            outfit = Item("cape");
            Part("Cape", outfit, triangle, C("cape"), new Vector2(0f, 140f), new Vector2(430f, 300f), 180f, BottomCenter);
            Part("Clasp", outfit, circle, new Color32(0xFF, 0xC9, 0x3C, 255), new Vector2(0f, 275f), new Vector2(40f, 40f), 0f, BottomCenter);

            foreach (GameObject item in items) item.SetActive(false);
            return items.ToArray();
        }

        /// <summary>Ebeveynin <paramref name="anchor"/> noktasına göre <paramref name="center"/> konumuna ortalanan (döndürülebilir) parça.</summary>
        private static Image Part(string name, Transform parent, Sprite sprite, Color color, Vector2 center, Vector2 size, float rotation, Vector2 anchor)
        {
            RectTransform rt = NewUI(name, parent);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = Center;
            rt.anchoredPosition = center;
            rt.sizeDelta = size;
            rt.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        // ---------- Açılış (yükleme) sahnesi ----------

        /// <summary>
        /// Unity logosunun yerine gelen açılış ekranı: büyük pati, oyunun adı, kısa ipucu ve üstünde pati
        /// sekip ilerleyen dolum çubuğu. Ekran sahne geçişlerinde de kullanılır (LoadingScreen).
        /// </summary>
        private static void BuildBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(Cream);
            Transform canvas = CreateCanvas();
            canvas.GetComponent<Canvas>().sortingOrder = 100;
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            // Açılış çizimi varsa: dostlar ortada, ad gökyüzünde, çubuk çimenlerde; ipucu yazısı gizli (okumayan çocuk)
            bool art = Background(canvas, "bg_acilis");

            // Alpi logosu: ailedeki diğer oyunların açılışı gibi
            Sprite logoArt = AssetDatabase.LoadAssetAtPath<Sprite>($"{FrameArtDir}/logo_alpi.png");
            Image logo = Img("Logo", canvas, logoArt, Color.white, Center, new Vector2(0f, 240f), new Vector2(820f, 770f));
            logo.preserveAspect = true;

            Text tip = Txt("Tip", canvas, Loc.Get("tip_1"), 46, TextAnchor.MiddleCenter, Center, new Vector2(0f, -330f), new Vector2(900f, 160f));
            tip.horizontalOverflow = HorizontalWrapMode.Wrap;
            tip.color = new Color(Plum.r, Plum.g, Plum.b, 0.8f);
            if (art) tip.enabled = false;

            // Dolum çubuğu
            const float barWidth = 760f;
            Image track = Img("Bar", canvas, roundRect, new Color(Plum.r, Plum.g, Plum.b, 0.85f), Center, new Vector2(0f, art ? -800f : -600f), new Vector2(barWidth + 50f, 80f));
            Sliced(track);
            Image fill = Img("Fill", track.transform, roundRect, Mint, Center, Vector2.zero, new Vector2(barWidth, 50f));
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            // Önizlemede dolu görünsün; çalışınca LoadingScreen sıfırdan başlatır
            fill.fillAmount = 0.35f;

            RectTransform lane = NewUI("Lane", track.transform);
            Place(lane, Center, new Vector2(0f, 30f), new Vector2(barWidth, 10f));
            RectTransform runner = Paw("Runner", lane, Vector2.zero, 0.42f, Coral);
            runner.pivot = new Vector2(0.5f, 0f);
            runner.anchoredPosition = new Vector2(-barWidth * 0.5f + barWidth * fill.fillAmount, 20f);

            var loading = canvas.gameObject.AddComponent<LoadingScreen>();
            SetRef(loading, "canvas", canvas.GetComponent<Canvas>());
            SetRef(loading, "group", group);
            SetRef(loading, "fill", fill);
            SetRef(loading, "runner", runner);
            SetFloat(loading, "barWidth", barWidth);
            SetRef(loading, "tip", tip);

            EditorSceneManager.SaveScene(scene, ScenePath(BootScene));
        }

        /// <summary>
        /// Unity açılış logosu kapatılır; telefonda oyun yüklenirken krem zemin görünür, böylece ilk karede
        /// yükleme ekranına kesintisiz geçilir. Açılış görseli varsa (Art/Icon/splash_android.png) o kullanılır.
        /// </summary>
        private static void SetStartup()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = Cream;

            var splash = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconDir}/splash_android.png");
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty image = settings.FindProperty("androidSplashScreen");
            SerializedProperty scale = settings.FindProperty("AndroidSplashScreenScale");
            if (image != null) image.objectReferenceValue = splash;
            // 2: ekranı doldur (kenarlarda boşluk kalmasın)
            if (scale != null) scale.intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Android ikonları ----------

        /// <summary>
        /// Android'in uyarlanabilir ikonunu doldurur (Art/Icon/icon_adaptive_bg.png ve _fg.png). Çizimler henüz
        /// yok; o zamana kadar Unity'nin varsayılan ikonu kullanılır. AndroidBuilder her derlemede yeniden çağırır,
        /// çünkü Android modülü yalnızca hedef platform Android iken derlenir.
        /// </summary>
        public static void SetAndroidIcons()
        {
#if UNITY_ANDROID
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconDir}/icon_adaptive_bg.png");
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconDir}/icon_adaptive_fg.png");
            if (background == null || foreground == null)
            {
                Debug.LogWarning(Tag + "Android ikon dosyaları henüz yok: " + IconDir);
                return;
            }

            PlatformIconKind kind = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            var layers = new[] { background, foreground };
            foreach (PlatformIcon icon in icons)
                for (int layer = 0; layer < layers.Length && layer < icon.maxLayerCount; layer++)
                    icon.SetTexture(layers[layer], layer);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            Debug.Log(Tag + "Android ikonları ayarlandı");
#endif
        }

        // ---------- Gerçek çizimler ----------

        /// <summary>
        /// Dost pozları (tools/slice_sheet.py çıktıları: cat_normal.png, cat_c2_happy.png ...). Resources altında:
        /// PetArt onları kütüphaneye koymaz, gerektikçe tek tek yükler (13 tür × 2 yaş × 6 renk × 4 poz hafızaya
        /// birden sığmaz).
        /// </summary>
        private const string PetArtDir = "Assets/Resources/Pets";
        /// <summary>Türe özel ortamlar (balık fanusta...): dostlar gibi gerektikçe yüklenir, hepsi birden hafızaya girmez.</summary>
        private const string HabitatArtDir = "Assets/Resources/Rooms";
        private const string RoomArtDir = "Assets/Art/Rooms";
        private const string IconArtDir = "Assets/Art/Icons";
        private const string FrameArtDir = "Assets/Art/Frames";   // foto çerçeveleri (büyük gösterilir)
        /// <summary>Dost pozlarının en uzun kenarı: telefonda en çok ~620 px görünür.</summary>
        private const int PetArtSize = 640;

        /// <summary>
        /// Assets/Art/Rooms (room_kitchen.png ...), Assets/Art/Icons (icon_act_bathe.png ...) ve Assets/Art/Frames
        /// çizimlerini Resources/PetArt.asset'e koyar; dost pozlarını yalnızca içe aktarma ayarlarıyla hazırlar.
        /// Klasör boşsa oyun yer tutucularla çalışır.
        /// </summary>
        private static void CreatePetArt()
        {
            EnsureFolder("Assets/Resources");
            const string path = "Assets/Resources/PetArt.asset";
            var art = AssetDatabase.LoadAssetAtPath<PetArt>(path);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<PetArt>();
                AssetDatabase.CreateAsset(art, path);
            }

            var entries = new List<PetArt.Entry>();
            foreach (string dir in new[] { PetArtDir, HabitatArtDir, RoomArtDir, IconArtDir, FrameArtDir })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string file in Directory.GetFiles(dir, "*.png"))
                {
                    string assetPath = file.Replace('\\', '/');
                    // Odalar ekranı dolduracak kadar büyük, ikonlar 256'ya kadar; dostlar küçük ve daha sıkı
                    // sıkıştırılır (yüzlerce poz var; yumuşak vinil çizimde fark görünmez)
                    bool pet = dir == PetArtDir || dir == HabitatArtDir;
                    int maxSize = dir == RoomArtDir ? 2048 : dir == HabitatArtDir ? 1280 : dir == IconArtDir ? 256 : pet ? PetArtSize : 1024;
                    Sprite sprite = ImportAsSprite(assetPath, 100f, maxSize, pet ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6);
                    if (sprite != null && !pet) entries.Add(new PetArt.Entry { key = Path.GetFileNameWithoutExtension(file), sprite = sprite });
                }
            }
            art.entries = entries.ToArray();
            EditorUtility.SetDirty(art);
            Debug.Log(Tag + $"Çizim kütüphanesi: {entries.Count} çizim");
        }

        /// <summary>
        /// Görseli sprite olarak içe aktarır. Android'de ASTC ile sıkıştırılır ve en uzun kenar
        /// maxSize ile sınırlanır.
        /// </summary>
        private static Sprite ImportAsSprite(string path, float pixelsPerUnit, int maxSize,
            TextureImporterFormat format = TextureImporterFormat.ASTC_6x6)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return null;
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            if (importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != maxSize
                || importer.mipmapEnabled || !android.overridden || android.format != format || android.maxTextureSize != maxSize)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.maxTextureSize = maxSize;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                android.overridden = true;
                android.format = format;
                android.maxTextureSize = maxSize;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- Yer tutucu çizimler ----------

        /// <summary>
        /// Arka plan: çizimi varsa (Assets/Art/Rooms/&lt;art&gt;.png, dikey 9:16) ekranı kırpmadan doldurur; yoksa
        /// krem zemin ve köşelerde iki yumuşak renk lekesi. Çizim kullanıldıysa true.
        /// </summary>
        private static bool Background(Transform canvas, string art = null)
        {
            Sprite drawing = art != null ? AssetDatabase.LoadAssetAtPath<Sprite>($"{RoomArtDir}/{art}.png") : null;
            if (drawing != null)
            {
                Image bg = Img("Background", canvas, drawing, Color.white, Center, Vector2.zero, Vector2.zero);
                Stretch(bg.rectTransform);
                var fitter = bg.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = drawing.rect.width / drawing.rect.height;
                return true;
            }
            PlainBackground(canvas);
            return false;
        }

        /// <summary>Oyunun adı açılış çiziminin gökyüzünde: kalın beyaz kontur ve yumuşak gölge (logo gibi).</summary>
        private static void LogoStyle(Text title)
        {
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(7f, -7f);
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(Plum.r, Plum.g, Plum.b, 0.35f);
            shadow.effectDistance = new Vector2(0f, -12f);
        }

        private static void PlainBackground(Transform canvas)
        {
            Image bg = Img("Background", canvas, null, Cream, Center, Vector2.zero, Vector2.zero);
            Stretch(bg.rectTransform);
            Img("BlobTop", canvas, circle, new Color(Mint.r, Mint.g, Mint.b, 0.25f), TopRight, new Vector2(260f, 260f), new Vector2(900f, 900f))
                .rectTransform.pivot = TopRight;
            Img("BlobBottom", canvas, circle, new Color(SkyBlue.r, SkyBlue.g, SkyBlue.b, 0.3f), new Vector2(0f, 0f), new Vector2(-300f, -260f), new Vector2(1000f, 1000f))
                .rectTransform.pivot = new Vector2(0f, 0f);
        }

        /// <summary>Dairelerden pati: bir büyük taban, dört parmak. scale 1 = yaklaşık 200 px.</summary>
        private static RectTransform Paw(string name, Transform parent, Vector2 pos, float scale, Color color)
        {
            RectTransform paw = NewUI(name, parent);
            Place(paw, Center, pos, new Vector2(200f, 200f) * scale);
            Img("Pad", paw, circle, color, Center, new Vector2(0f, -30f) * scale, new Vector2(120f, 104f) * scale);
            Vector2[] toes = { new Vector2(-70f, 30f), new Vector2(-26f, 72f), new Vector2(26f, 72f), new Vector2(70f, 30f) };
            for (int i = 0; i < toes.Length; i++)
                Img("Toe" + i, paw, circle, color, Center, toes[i] * scale, new Vector2(46f, 54f) * scale);
            return paw;
        }

        /// <summary>
        /// Beyaz yer tutucu şekil üretir: daire ya da (border > 0 ise) dokuz parçaya dilimlenen yuvarlak köşeli kare.
        /// </summary>
        private static Sprite CreateShape(string name, int size, int radius, int border)
        {
            string path = $"{PlaceholderDir}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Köşe merkezine uzaklık: yuvarlak köşeli karenin dışı saydam
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - dist)));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Vector4 borders = new Vector4(border, border, border, border);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != borders
                || importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = borders;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Beyaz, yukarı bakan ikizkenar üçgen (kulak).</summary>
        private static Sprite CreateTriangle(string name, int size)
        {
            string path = $"{PlaceholderDir}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float c = size * 0.5f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Yükseldikçe daralır; kenarlar bir piksel yumuşak
                    float half = c * (1f - (y + 0.5f) / size);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(half - Mathf.Abs(x + 0.5f - c) + 0.5f)));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            return CreateShape(name, size, 0, 0);
        }

        // ---------- Şeker arayüz şekilleri (koddan, beyaz-gri: Image rengiyle boyanır) ----------

        private const int JellyLip = 12, JellyShadow = 7, JellyRadius = 40;

        /// <summary>Yuvarlak köşeli dikdörtgenin (x0..x1, y0..y1, köşe r) pikseldeki kaplama oranı.</summary>
        private static float RoundRectCover(float px, float py, float x0, float y0, float x1, float y1, float r)
        {
            float cx = Mathf.Clamp(px, x0 + r, x1 - r), cy = Mathf.Clamp(py, y0 + r, y1 - r);
            return Mathf.Clamp01(r - Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) + 0.5f);
        }

        /// <summary>a'yı b'nin üstüne koyar (önceden çarpılmamış alfa).</summary>
        private static Color Over(Color top, Color under)
        {
            float a = top.a + under.a * (1f - top.a);
            if (a <= 0f) return Color.clear;
            Color c = (top * top.a + under * under.a * (1f - top.a)) / a;
            c.a = a;
            return c;
        }

        /// <summary>
        /// Jöle buton: üstten alta hafif kararan gövde, altında koyu "kalınlık" (basılabilir 3D his) ve yumuşak gölge.
        /// Beyaz-gri çizilir; Image rengi boyar. Dikdörtgen olan dokuz parçaya dilimlenir (alt kenar kalın).
        /// </summary>
        private static Sprite CreateJelly(string name, bool round)
        {
            string path = $"{PlaceholderDir}/{name}.png";
            int size = round ? 192 : 160;
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float bodyBottom = JellyLip + JellyShadow;
                float radius = round ? (size - bodyBottom) * 0.5f - 1f : JellyRadius;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float body, lip, shadow;
                    if (round)
                    {
                        float cx = size * 0.5f, cy = size - 1f - radius;
                        body = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) + 0.5f);
                        lip = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy - JellyLip)) + 0.5f);
                        shadow = Mathf.Clamp01((radius + 3f - Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy - JellyLip - JellyShadow))) / 6f);
                    }
                    else
                    {
                        body = RoundRectCover(px, py, 0f, bodyBottom, size, size, radius);
                        lip = RoundRectCover(px, py, 0f, JellyShadow, size, size - JellyLip, radius);
                        shadow = RoundRectCover(px, py, 3f, 0f, size - 3f, size - JellyLip - JellyShadow, radius);
                    }
                    // Gövde yukarıda açık, aşağıda biraz koyu; kalınlık koyu; gölge siyah ve saydam
                    float t = Mathf.Clamp01((py - bodyBottom) / (size - bodyBottom));
                    float shade = Mathf.Lerp(0.86f, 1f, t);
                    Color c = new Color(0f, 0f, 0f, shadow * 0.2f);
                    c = Over(new Color(0.66f, 0.66f, 0.66f, lip), c);
                    c = Over(new Color(shade, shade, shade, body), c);
                    tex.SetPixel(x, y, c);
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            // Kenarlar: sol/sağ/üst köşe kadar, alt köşe + kalınlık + gölge kadar
            Vector4 border = round ? Vector4.zero : new Vector4(JellyRadius, JellyRadius + JellyLip + JellyShadow, JellyRadius, JellyRadius);
            return ImportShape(path, border);
        }

        /// <summary>Parlaklık şeridi: üstte belirgin, aşağı doğru kaybolan beyaz yuvarlak şerit.</summary>
        private static Sprite CreateGloss(string name)
        {
            string path = $"{PlaceholderDir}/{name}.png";
            const int size = 96, radius = 30;
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cover = RoundRectCover(x + 0.5f, y + 0.5f, 0f, 0f, size, size, radius);
                    float fade = Mathf.Clamp01((y + 0.5f) / size);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, cover * fade * fade));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            return ImportShape(path, new Vector4(radius, radius, radius, radius));
        }

        /// <summary>Kart/panel: yuvarlak köşeli beyaz yüz ve altında yumuşak gölge.</summary>
        private static Sprite CreateCard(string name)
        {
            string path = $"{PlaceholderDir}/{name}.png";
            const int size = 160, radius = 44, drop = 10;
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float face = RoundRectCover(px, py, 4f, drop + 4f, size - 4f, size, radius);
                    // Gölge: yüzün biraz altında, kenarları yumuşak
                    float cx = Mathf.Clamp(px, 4f + radius, size - 4f - radius), cy = Mathf.Clamp(py, 4f + radius, size - drop - 4f - radius);
                    float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    float shadow = Mathf.Clamp01((radius + 4f - d) / 10f) * 0.16f;
                    tex.SetPixel(x, y, Over(new Color(1f, 1f, 1f, face), new Color(0f, 0f, 0f, shadow)));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            return ImportShape(path, new Vector4(radius + 4, radius + drop + 4, radius + 4, radius + 4));
        }

        private static Sprite ImportShape(string path, Vector4 border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// Butonu jöle yapar: jöle şekli + üst yarıda parlaklık şeridi. Küçük butonlarda dilim kenarları
        /// sığsın diye ölçeklenir.
        /// </summary>
        private static void Jelly(Image img, bool round = false)
        {
            RectTransform rt = img.rectTransform;
            if (round)
            {
                img.sprite = jellyCircle;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            else
            {
                img.sprite = jellyRect;
                Sliced(img);
                // Dilim kenarlarının toplamı (üst + alt) butonun %85'ini geçmesin
                float edges = JellyRadius * 2f + JellyLip + JellyShadow;
                img.pixelsPerUnitMultiplier = Mathf.Max(1f, edges / (rt.sizeDelta.y * 0.85f));
            }
            Image shine = Img("Gloss", rt, gloss, new Color(1f, 1f, 1f, 0.5f), Center, Vector2.zero, Vector2.zero);
            Sliced(shine);
            RectTransform s = shine.rectTransform;
            if (round)
            {
                s.anchorMin = new Vector2(0.22f, 0.6f);
                s.anchorMax = new Vector2(0.78f, 0.92f);
                s.offsetMin = s.offsetMax = Vector2.zero;
                shine.pixelsPerUnitMultiplier = 2.5f;
            }
            else
            {
                s.anchorMin = new Vector2(0f, 0.55f);
                s.anchorMax = Vector2.one;
                s.offsetMin = new Vector2(12f, 0f);
                s.offsetMax = new Vector2(-12f, -7f);
                shine.pixelsPerUnitMultiplier = Mathf.Max(1.5f, 60f / (rt.sizeDelta.y * 0.45f));
            }
            shine.transform.SetAsFirstSibling();
        }

        /// <summary>Kartı gölgeli kart şekline çevirir (rengi aynı kalır).</summary>
        private static void Card(Image img)
        {
            img.sprite = cardSprite;
            Sliced(img);
            float edges = 48f * 2f + 10f;
            img.pixelsPerUnitMultiplier = Mathf.Max(1f, edges / (Mathf.Min(img.rectTransform.sizeDelta.x, img.rectTransform.sizeDelta.y) * 0.9f));
        }

        // ---------- Sahne / UI yardımcıları ----------

        private static void CreateCamera(Color background)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();
        }

        private static Transform CreateCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            // Tasarım alanı (1080x1920) her ekran oranında tümüyle görünür: uzun telefonda üstte/altta,
            // tablette yanlarda boşluk açılır, hiçbir şey kırpılmaz
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            canvasGo.AddComponent<BackKey>();   // Android geri tuşu
            return canvasGo.transform;
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Image Img(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            RectTransform rt = NewUI(name, parent);
            Place(rt, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void Sliced(Image img)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
        }

        private static Text Txt(string name, Transform parent, string value, int fontSize, TextAnchor align,
            Vector2 anchor, Vector2 pos, Vector2 size)
        {
            RectTransform rt = NewUI(name, parent);
            Place(rt, anchor, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = value;
            t.fontSize = fontSize;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = Plum;
            t.raycastTarget = false;
            return t;
        }

        private static Text White(Text t)
        {
            t.color = Color.white;
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(0f, -4f);
            return t;
        }

        private static Text Localize(Text text, string key)
        {
            SetString(text.gameObject.AddComponent<LocalizedText>(), "key", key);
            return text;
        }

        private static void Juice(GameObject button, float pulse)
        {
            var juice = button.AddComponent<ButtonJuice>();
            SetFloat(juice, "pulse", pulse);
            // Sürekli nefes alan buton kendi alt tuvalinde: her karede bütün ekran yeniden hesaplanmasın
            if (pulse > 0f) Isolate(button);
        }

        /// <summary>
        /// Her karede hareket eden parçayı (dost, balon, nabız atan buton, panel) kendi alt tuvaline alır. Unity bir
        /// tuvalde tek bir şey kıpırdasa bütün tuvali yeniden toplar; alt tuval yalnızca kendini toplar.
        /// Dokunma için kendi GraphicRaycaster'ı olur.
        /// </summary>
        private static void Isolate(GameObject go)
        {
            if (go.GetComponent<Canvas>() != null) return;
            go.AddComponent<Canvas>();
            go.AddComponent<GraphicRaycaster>();
        }

        private static void SetRef(Object target, string field, Object value)
        {
            if (value == null) Debug.LogError(Tag + $"Boş bağlantı: {target.GetType().Name}.{field}");
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null) Debug.LogError(Tag + $"Boş bağlantı: {target.GetType().Name}.{field}[{i}]");
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColors(Object target, string field, Color[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).colorValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
