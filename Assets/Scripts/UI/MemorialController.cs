using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Anı Bahçesi: ömrünü tamamlayan ya da hastalıktan ölen dostlar (en yenisi önde). Bir dost az önce öldüyse
    /// üstte nazik bir veda mesajı çıkar. Dostu kalmayan oyuncu buradan yeni dost sahiplenir.
    /// </summary>
    public class MemorialController : MonoBehaviour
    {
        public const string MemorialScene = "Memorial";

        [SerializeField] private GameObject farewell;
        [SerializeField] private Text farewellText;
        [SerializeField] private Text emptyText;
        [SerializeField] private GameObject[] cards;
        [SerializeField] private Image[] cardBadges;
        [SerializeField] private Text[] cardNames;
        [SerializeField] private Text[] cardInfos;
        [SerializeField] private Text[] cardCauses;
        [SerializeField] private GameObject newPalButton;

        private MemorialEntry farewellEntry;

        private void Awake()
        {
            // Veda mesajı bir kez gösterilir
            farewellEntry = GameSession.LastDeath;
            GameSession.ClearLastDeath();
        }

        private void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.Changed -= Refresh;
        }

        public void Back()
        {
            LoadingScreen.Go(HomeController.HomeScene);
        }

        public void NewPal() => LoadingScreen.Go(AdoptController.AdoptScene);

        private void Refresh()
        {
            farewell.SetActive(farewellEntry != null);
            if (farewellEntry != null)
            {
                string age = Loc.Age(farewellEntry.ageDays);
                farewellText.text = Loc.Format("memorial_" + farewellEntry.cause, farewellEntry.name, age);
            }

            var memorial = GameSession.Data.memorial;
            emptyText.gameObject.SetActive(memorial.Count == 0);
            for (int i = 0; i < cards.Length; i++)
            {
                int index = memorial.Count - 1 - i;   // en yenisi önde
                cards[i].SetActive(index >= 0);
                if (index < 0) continue;
                MemorialEntry entry = memorial[index];
                cardBadges[i].color = PetLooks.ColorOf(entry.species, entry.color);
                cardNames[i].text = entry.name;
                cardInfos[i].text = $"{Loc.Get("species_" + entry.species)} · {Loc.Age(entry.ageDays)}";
                cardCauses[i].text = Loc.Get("memorial_card_" + entry.cause);
            }
            newPalButton.SetActive(Shop.CanAdopt(GameSession.Data));
        }
    }
}
