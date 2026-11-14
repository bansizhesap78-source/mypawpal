using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Oyun odasındaki "Oyunlar" listesi: kartta oyunun seviyesi yazar; türe uymayan oyun (balıkta top getir) görünmez.
    /// Dost yorgunsa ya da uyuyorsa oynanmaz.
    /// </summary>
    public class MiniGameMenu : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private PetController owner;
        [Tooltip("MiniGame sırasıyla oyun kartları ve seviye yazıları")]
        [SerializeField] private RectTransform[] cards;
        [SerializeField] private Text[] levels;
        [SerializeField] private float topY = 320f;
        [SerializeField] private float stepY = 165f;

        private void Awake()
        {
            root.SetActive(false);
        }

        public void Open()
        {
            PetState pet = GameSession.CurrentPet;
            if (pet != null)
            {
                Species species = SpeciesCatalog.Get(pet.species);
                int row = 0;
                for (int i = 0; i < cards.Length; i++)
                {
                    bool show = GameLevels.Available(species, (MiniGame)i);
                    cards[i].gameObject.SetActive(show);
                    if (!show) continue;
                    cards[i].anchoredPosition = new Vector2(0f, topY - row * stepY);
                    levels[i].text = Loc.Format("game_level", GameLevels.Get(GameSession.Data, (MiniGame)i));
                    row++;
                }
            }
            root.SetActive(true);
        }

        public void Close() => root.SetActive(false);

        public void Play(int game)
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            if (!Economy.CanPlay(pet))
            {
                Close();
                owner.ShowMessage("res_tired");   // uyuyorsa açarken zaten uyandırıldı (PetController.OpenGames)
                return;
            }
            MiniGameHost.Selected = (MiniGame)game;
            LoadingScreen.Go(MiniGameHost.MiniGameScene);
        }
    }
}
