using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Hayvanın ekrandaki hali (çizimler gelene kadar şekillerden yer tutucu): türe göre parçalar (PetLooks),
    /// renk, yaşa göre boyut, kiloya göre en, giydiği kıyafetler, uyurken kapalı göz ve "Zzz", kirliyken lekeler,
    /// nefes alma, göz kırpma, sevince zıplama, numara yapınca dönme.
    /// Kökün pivotu ayaklarının altındadır; büyürken yerden yukarı doğru büyür.
    /// </summary>
    public class PetView : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private Graphic[] tinted;
        [SerializeField] private RectTransform[] eyes;
        [SerializeField] private GameObject zzz;
        [SerializeField] private Graphic[] dirt;
        [Tooltip("Türe göre açılıp kapanan Look_* parçaları")]
        [SerializeField] private GameObject[] looks;
        [Tooltip("Item_<kıyafet id> parçaları")]
        [SerializeField] private GameObject[] clothes;
        [Header("Gerçek çizim (PetArt) varsa şekiller yerine")]
        [SerializeField] private GameObject shapes;
        [SerializeField] private Image art;
        [Tooltip("Çizimin üstündeki kıyafet katmanları, ClothingSlot sırasıyla (şapka, gözlük, boyun, kıyafet, ayak)")]
        [SerializeField] private Image[] artClothes;

        private float scale = 1f, width = 1f, bounce, trick;
        private bool asleep, senior;
        private float blinkTimer = 3f, blink;
        private string shownSpecies;
        private PetState lastPet;
        private Species lastSpecies;
        // Geçici poz (yerken mutlu, giyinme odasında normal); hastayken ya da uyurken zorla verilmediyse uygulanmaz
        private string poseOverride;
        private float poseTimer;
        private bool poseForced;
        private float facing = 1f, faceTarget = 1f;
        private float lean, leanTarget, chew, wiggle;

        /// <summary>Adım atarken ayakların yerden kalkışı (px); PetStage ve WalkPanel yazar.</summary>
        public float StepLift { get; set; }

        /// <summary>Ayakların üstünde devrilme (derece, + sola): 90'a yakınken yan yatmış olur.</summary>
        public float Tilt { get; set; }

        /// <summary>Gövdenin ortası çevresinde takla (derece).</summary>
        public float Spin { get; set; }

        /// <summary>Ezilme: + yayvan ve basık, - ince ve uzun.</summary>
        public float Squish { get; set; }

        /// <summary>Yerden yükseklik (px): balık suda süzülür.</summary>
        public float Hover { get; set; }

        /// <summary>Zıplama ve kayma için kaydırma (px).</summary>
        public Vector2 Offset { get; set; }

        /// <summary>Şakalar bitti ya da yarıda kaldı: dost dimdik durur.</summary>
        public void ResetGesture()
        {
            Tilt = Spin = Squish = 0f;
            Offset = Vector2.zero;
        }

        /// <summary>Başının tepesi (kök konumuna göre px): top çarpması, dönen yıldızlar.</summary>
        public Vector2 HeadTop
        {
            get
            {
                float[] hat = CurrentAnchor(ClothingSlot.Hat);
                float side = art.rectTransform.rect.width;
                if (hat == null) return new Vector2(0f, 430f * scale);
                return new Vector2((hat[0] - 0.5f) * side * scale * Mathf.Sign(faceTarget), (hat[1] - hat[2] * 0.22f) * side * scale);
            }
        }

        /// <summary>Ağzı (kök konumuna göre px): havada yemek kapma.</summary>
        public Vector2 Mouth
        {
            get
            {
                float[] face = CurrentAnchor(ClothingSlot.Glasses);
                float side = art.rectTransform.rect.width;
                if (face == null) return new Vector2(0f, 300f * scale);
                return new Vector2((face[0] - 0.5f) * side * scale * Mathf.Sign(faceTarget), (face[1] - face[2] * 0.3f) * side * scale);
            }
        }

        private float[] CurrentAnchor(ClothingSlot slot)
        {
            if (art == null || art.sprite == null || !art.gameObject.activeSelf) return null;
            float[] anchor = ClothingAnchors.Get(art.sprite.name, slot);
            return anchor != null && anchor.Length == 3 ? anchor : null;
        }

        /// <summary>Ekrandaki boy çarpanı (balonun yüksekliği buna göre).</summary>
        public float VisualScale => scale;

        public void Apply(PetState pet, Species species)
        {
            lastPet = pet;
            lastSpecies = species;
            if (shownSpecies != pet.species)
            {
                shownSpecies = pet.species;
                foreach (GameObject part in looks) part.SetActive(PetLooks.Shows(pet.species, part.name));
            }

            // Yaşlanınca rengi ağarır, hastayken hafif solgun ve yeşilimsi görünür
            Color color = PetLooks.ColorOf(pet.species, pet.color);
            if (Mood.Stage(pet, species) == LifeStage.Senior) color = Color.Lerp(color, new Color(0.85f, 0.85f, 0.85f), 0.35f);
            if (pet.IsSick || PetSimulator.IsCritical(pet)) color = Color.Lerp(color, new Color(0.72f, 0.82f, 0.62f), 0.3f);
            foreach (Graphic g in tinted) g.color = color;
            // Çizim kendi renginde; yalnızca yaşlılıkta biraz ağarır
            art.color = Mood.Stage(pet, species) == LifeStage.Senior ? new Color(0.92f, 0.92f, 0.92f) : Color.white;

            // Türün çizimi varsa şekiller yerine çizimin pozu gösterilir; kıyafetler de çizimin üstüne, pozun
            // noktalarına (ClothingAnchors) konur
            bool hold = poseOverride != null && (poseForced || (!pet.asleep && !pet.IsSick));
            Sprite drawing = hold ? PetArt.ForPet(pet, poseOverride) : PetArt.ForPet(pet);
            bool drawn = drawing != null;
            shapes.SetActive(!drawn);
            art.gameObject.SetActive(drawn);
            if (drawn) art.sprite = drawing;

            foreach (GameObject item in clothes)
            {
                string id = item.name.Substring("Item_".Length);
                item.SetActive(!drawn && (id == pet.hat || id == pet.glasses || id == pet.neck || id == pet.outfit || id == pet.feet));
            }
            if (artClothes != null)
                for (int i = 0; i < artClothes.Length; i++)
                    DressArt(artClothes[i], (ClothingSlot)i, pet, drawn ? drawing.name : null);

            scale = Mood.Scale(pet, species);
            // Gerçek çizimde yavru çok küçük kalıyordu: çizim modunda 0,68'den 1'e büyür
            if (drawn) scale = Mathf.Lerp(0.68f, 1f, (scale - 0.45f) / 0.55f);
            // "Zz" çizimde uyuyan dostun başının üstüne (çizim yer tutucudan alçak)
            ((RectTransform)zzz.transform).anchoredPosition = drawn ? new Vector2(190f, 430f) : new Vector2(250f, 560f);
            // Kilolu daha geniş, zayıf daha ince görünür
            width = Mathf.Lerp(0.88f, 1.18f, (float)(pet.weight / 100.0));
            asleep = pet.asleep;
            senior = Mood.Stage(pet, species) == LifeStage.Senior;
            zzz.SetActive(asleep && !poseForced);

            // Balıkta çamur olmaz: kirlenen akvaryumun suyudur (PetController)
            float dirty = species.moves == MoveKind.Swim ? 0f : Mathf.Clamp01((float)((50.0 - pet.hygiene) / 50.0));
            foreach (Graphic spot in dirt)
            {
                Color c = spot.color;
                c.a = dirty * 0.85f;
                spot.color = c;
            }
        }

        /// <summary>
        /// Çizimdeki bir yuvanın kıyafeti: giyilmişse parçanın resmi pozun noktasına konur. Elbise her pozda ayrı
        /// çizilmiştir (uyurken de üstündedir); takılar kıvrık bedene oturmadığı için uyurken çıkar.
        /// </summary>
        private void DressArt(Image layer, ClothingSlot slot, PetState pet, string pose)
        {
            if (slot == ClothingSlot.Outfit)
            {
                DressOutfit(layer, pet, pose);
                return;
            }
            // Ayak yuvası artık yok: eski kayıtta giyili kalmış patik gösterilmez
            string id = pose != null && slot <= ClothingSlot.Neck ? Wardrobe.Worn(pet, slot) : null;
            float[] anchor = string.IsNullOrEmpty(id) ? null : ClothingAnchors.Get(pose, slot);
            // Uyurken (kıvrılmış poz) dost aksesuarlarını çıkarmış gibi: kıvrık bedende hiçbiri yerine oturmuyor
            bool sleeping = pose != null && pose.EndsWith("_sleep");
            Sprite sprite = anchor != null ? PetArt.Get("icon_cloth_" + id) : null;
            bool show = sprite != null && anchor.Length == 3 && !sleeping;
            if (layer.gameObject.activeSelf != show) layer.gameObject.SetActive(show);
            if (!show) return;
            layer.sprite = sprite;
            float side = art.rectTransform.rect.width;
            layer.rectTransform.anchoredPosition = new Vector2(anchor[0] * side, anchor[1] * side);
            layer.rectTransform.sizeDelta = new Vector2(anchor[2] * side, anchor[2] * side);
        }

        private void DressOutfit(Image layer, PetState pet, string pose)
        {
            Sprite sprite = pose != null && !string.IsNullOrEmpty(pet.outfit) ? PetArt.Get(OutfitKey(pet.outfit, pose)) : null;
            bool show = sprite != null;
            if (layer.gameObject.activeSelf != show) layer.gameObject.SetActive(show);
            if (!show) return;
            layer.sprite = sprite;
            // Katman çizimle aynı kare tuval: üst üste oturur
            float side = art.rectTransform.rect.width;
            layer.rectTransform.anchoredPosition = new Vector2(side * 0.5f, side * 0.5f);
            layer.rectTransform.sizeDelta = new Vector2(side, side);
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> basePoses =
            new System.Collections.Generic.Dictionary<string, string>();

        /// <summary>Pozun elbise katmanı: renk çeşitleri asıl rengin katmanını kullanır (cat_c2_happy → outfit_x_cat_happy).</summary>
        public static string OutfitKey(string outfit, string pose)
        {
            if (!basePoses.TryGetValue(pose, out string plain))
            {
                plain = pose;
                for (int color = 1; color <= 9; color++) plain = plain.Replace("_c" + color + "_", "_");
                basePoses[pose] = plain;
            }
            return "outfit_" + outfit + "_" + plain;
        }

        /// <summary>Bir süre şu pozda kalır (yemek bitince mutlu). force: hasta ya da uyuyorken de.</summary>
        public void HoldPose(string pose, float seconds, bool force = false)
        {
            poseOverride = pose;
            poseTimer = seconds;
            poseForced = force;
            if (lastPet != null) Apply(lastPet, lastSpecies);
        }

        /// <summary>Yüzü sola (-1) ya da sağa (1) döner.</summary>
        public void SetFacing(int direction) => faceTarget = direction;

        /// <summary>Baktığı yöne eğilir (derece): kaptan yerken, sevilirken.</summary>
        public void Lean(float degrees) => leanTarget = degrees;

        /// <summary>Çiğneme: kısa kısa ezilip uzar.</summary>
        public void Chew(float seconds) => chew = seconds;

        /// <summary>Silkelenme: sağa sola titrer.</summary>
        public void Wiggle(float seconds) => wiggle = seconds;

        /// <summary>Sevince ya da bir şey yiyince küçük bir zıplama.</summary>
        public void Bounce()
        {
            bounce = 1f;
        }

        /// <summary>Öğrendiği komutu yapar: zıplayıp kendi etrafında döner.</summary>
        public void Trick()
        {
            trick = 1f;
            bounce = 1f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            bounce = Mathf.MoveTowards(bounce, 0f, dt * 3f);
            trick = Mathf.MoveTowards(trick, 0f, dt * 1.4f);
            float hop = Mathf.Sin(bounce * Mathf.PI) * (trick > 0f ? 0.3f : 0.12f);
            if (poseTimer > 0f && poseTimer < float.MaxValue)
            {
                poseTimer -= dt;
                if (poseTimer <= 0f)
                {
                    poseOverride = null;
                    poseForced = false;
                    if (lastPet != null) Apply(lastPet, lastSpecies);
                }
            }
            facing = Mathf.MoveTowards(facing, faceTarget, dt * 9f);
            lean = Mathf.MoveTowards(lean, leanTarget, dt * 90f);
            chew = Mathf.Max(0f, chew - dt);
            wiggle = Mathf.Max(0f, wiggle - dt);
            float munch = chew > 0f ? Mathf.Sin(Time.unscaledTime * 22f) * 0.05f : 0f;
            float shake = wiggle > 0f ? Mathf.Sin(Time.unscaledTime * 42f) * 9f : 0f;
            // Takla gövdenin ortası çevresinde: pivot ayakta olduğu için dönerken konum düzeltilir. Yan yatınca gövde
            // yere gömülmesin diye biraz yukarı alınır.
            float centre = 230f * scale;
            float spinRad = Spin * Mathf.Deg2Rad;
            Vector2 spinShift = new Vector2(Mathf.Sin(spinRad) * centre, centre - Mathf.Cos(spinRad) * centre);
            float lying = Mathf.Abs(Mathf.Sin(Tilt * Mathf.Deg2Rad)) * 110f * scale;
            body.anchoredPosition = new Vector2(spinShift.x + Offset.x, spinShift.y + Offset.y + Hover + lying + Mathf.Sin(bounce * Mathf.PI) * 70f + StepLift);
            body.localRotation = Quaternion.Euler(0f, 0f, -lean * faceTarget + shake + Tilt + Spin);
            // Yaşlı dost daha yavaş nefes alır
            float speed = (asleep ? 1.6f : 2.6f) * (senior ? 0.7f : 1f);
            float breath = Mathf.Sin(Time.unscaledTime * speed) * (asleep ? 0.03f : 0.018f);
            // Dönerken yatay ölçek kosinüs gibi daralıp açılır (arkasını dönüp geri gelir)
            float turn = trick > 0f ? Mathf.Cos((1f - trick) * Mathf.PI * 2f) : 1f;
            // facing -1..1 arası geçerken dost dönüyormuş gibi daralır
            float side = Mathf.Abs(facing) < 0.15f ? 0.15f * Mathf.Sign(faceTarget) : facing;
            body.localScale = new Vector3(scale * width * (1f - breath * 0.5f + munch + Squish) * turn * side, scale * (1f + breath + hop - munch - Squish * 0.7f), 1f);

            // Göz kırpma; uyurken gözler kapalı (ince çizgi)
            blinkTimer -= dt;
            if (blinkTimer <= 0f)
            {
                blink = 0.14f;
                blinkTimer = Random.Range(2.5f, 5f);
            }
            blink = Mathf.Max(0f, blink - dt);
            float eyeOpen = asleep || blink > 0f ? 0.12f : 1f;
            foreach (RectTransform eye in eyes) eye.localScale = new Vector3(1f, eyeOpen, 1f);
        }
    }
}
