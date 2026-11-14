using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Sahiplenme: önce tür seçilir (M1'de yalnızca kedi açık), sonra ad, renk ve zaman modu.
    /// Zaman modu sonradan değiştirilemez (değiştirmek için ileride "Mod Değiştirici" satılır).
    /// </summary>
    public class AdoptController : MonoBehaviour
    {
        public const string AdoptScene = "Adopt";

        public static readonly string[] Names =
            { "Pamuk", "Boncuk", "Tarçın", "Fındık", "Zeytin", "Lokum", "Mia", "Luna", "Leo", "Max", "Bulut", "Karamel" };

        [Header("Adım 1: tür (SpeciesCatalog.All sırasıyla)")]
        [SerializeField] private GameObject speciesStep;
        [Tooltip("Paketle açılan türlerin kilit örtüsü (SpeciesCatalog.All sırasıyla; paketsizlerde boş)")]
        [SerializeField] private GameObject[] packLocks;

        [Header("Adım 2: ayrıntılar")]
        [SerializeField] private GameObject detailsStep;
        [SerializeField] private Text speciesTitle;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Image[] colorSwatches;
        [SerializeField] private Image preview;
        [SerializeField] private Image previewArt;
        [SerializeField] private Image realCard;
        [SerializeField] private Image fastCard;
        [SerializeField] private Text realDesc;
        [SerializeField] private Text fastDesc;

        private Species species;
        private int color;
        private SpeedMode mode = SpeedMode.Real;

        private void OnEnable()
        {
            Loc.Changed += Refresh;
        }

        private void OnDisable()
        {
            Loc.Changed -= Refresh;
        }

        private void Start()
        {
            ShowSpecies();
        }

        public void ShowSpecies()
        {
            speciesStep.SetActive(true);
            detailsStep.SetActive(false);
            for (int i = 0; i < packLocks.Length; i++)
                if (packLocks[i] != null) packLocks[i].SetActive(!Shop.SpeciesUnlocked(GameSession.Data, SpeciesCatalog.All[i]));
        }

        /// <summary>Tür listesinden geri: geldiği ekrana.</summary>
        public void Back()
        {
            LoadingScreen.Back();
        }

        public void SelectSpecies(int index)
        {
            // Paketli tür (Dino) ya da boş slot yoksa mağazaya
            Species picked = SpeciesCatalog.All[index];
            if (!picked.available) return;
            if (!Shop.SpeciesUnlocked(GameSession.Data, picked))
            {
                ShopController.OpenFor(ProductCatalog.DinoPack);
                return;
            }
            if (!Shop.CanAdopt(GameSession.Data))
            {
                string next = Shop.NextSlotProduct(GameSession.Data, System.DateTime.UtcNow);
                if (next != null) ShopController.OpenFor(next);
                else LoadingScreen.Back();
                return;
            }
            species = picked;
            if (string.IsNullOrWhiteSpace(nameInput.text)) RandomName();
            speciesStep.SetActive(false);
            detailsStep.SetActive(true);
            Refresh();
        }

        /// <summary>Son iki renk Özel Renkler paketiyle açılır; kilitliyse mağazaya.</summary>
        public void SelectColor(int index)
        {
            if (index >= Shop.ColorChoices(GameSession.Data))
            {
                ShopController.OpenFor(ProductCatalog.PremiumColors);
                return;
            }
            color = index;
            Refresh();
        }

        public void SelectReal()
        {
            mode = SpeedMode.Real;
            Refresh();
        }

        public void SelectFast()
        {
            mode = SpeedMode.Fast;
            Refresh();
        }

        public void RandomName()
        {
            string current = nameInput.text;
            string next = current;
            // Var olan dostların adları da önerilmez (iki "Mia" karışmasın); sonsuz döngüye karşı en çok 50 deneme
            for (int i = 0; i < 50 && (next == current || GameSession.Data.pets.Exists(p => p.name == next)); i++)
                next = Names[Random.Range(0, Names.Length)];
            nameInput.text = next;
        }

        public void Confirm()
        {
            if (species == null) return;
            string petName = nameInput.text.Trim();
            if (petName.Length == 0)
            {
                RandomName();
                petName = nameInput.text;
            }
            GameSession.Adopt(species, petName, color, mode);
            // Dostun acıktığını uygulama kapalıyken de haber verebilmek için (Android 13+ izin ister)
            if (GameSession.Data.notificationsOn) Notifications.RequestPermission();
            LoadingScreen.Go(PetController.PetScene);
        }

        private void Refresh()
        {
            if (species == null) return;
            speciesTitle.text = Loc.Get(species.NameKey);
            preview.color = PetLooks.ColorOf(species.id, color);
            // Türün çizimi varsa yuvarlak yer tutucu yerine dostun mutlu pozu
            if (previewArt != null)
            {
                Sprite art = PetArt.Preview(species.id, color, "happy");   // seçilen rengin çizimi
                previewArt.gameObject.SetActive(art != null);
                previewArt.sprite = art;
                preview.enabled = art == null;
                foreach (Transform child in preview.transform)
                    if (child != previewArt.transform) child.gameObject.SetActive(art == null);
            }
            int choices = Shop.ColorChoices(GameSession.Data);
            for (int i = 0; i < colorSwatches.Length; i++)
            {
                colorSwatches[i].rectTransform.localScale = Vector3.one * (i == color ? 1.15f : 0.85f);
                colorSwatches[i].transform.GetChild(0).GetComponent<Image>().color = PetLooks.ColorOf(species.id, i);
                // Kilitli özel renkte "Özel" yazısı (ikinci çocuk)
                if (colorSwatches[i].transform.childCount > 1)
                    colorSwatches[i].transform.GetChild(1).gameObject.SetActive(i >= choices);
            }

            realDesc.text = Loc.Format("mode_real_desc", species.lifespanYears.ToString("0.#"));
            fastDesc.text = Loc.Format("mode_fast_desc", (species.lifespanYears / (int)SpeedMode.Fast).ToString("0.#"));
            Highlight(realCard, mode == SpeedMode.Real);
            Highlight(fastCard, mode == SpeedMode.Fast);
        }

        private static void Highlight(Image card, bool on)
        {
            card.color = on ? new Color(1f, 0.5f, 0.43f) : new Color(1f, 1f, 1f, 0.85f);
            card.rectTransform.localScale = Vector3.one * (on ? 1.04f : 1f);
            foreach (Text t in card.GetComponentsInChildren<Text>())
                t.color = on ? Color.white : new Color(0.29f, 0.2f, 0.33f);
        }
    }
}
