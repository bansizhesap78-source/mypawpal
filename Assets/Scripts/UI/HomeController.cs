using System;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Ana sayfa: Alpi logosu, cüzdan, bütün dostların kartları (dokununca odasına gider), boş yer ("Yeni Dost"),
    /// kilitli yerler (mağazaya gider) ve altta Görevler, Mağaza, Anılar, Ayarlar. Oyun her açılışta buradan başlar;
    /// dostun odasından sol üstteki ev butonu ya da telefonun geri tuşu buraya döner.
    /// </summary>
    public class HomeController : MonoBehaviour
    {
        public const string HomeScene = "Home";

        [SerializeField] private Text goldText;
        [SerializeField] private Text diamondText;

        [Header("Dost kartları (Shop.MaxPets kadar)")]
        [SerializeField] private Image[] cards;
        [SerializeField] private GameObject[] petGroups;
        [SerializeField] private Image[] portraits;
        [SerializeField] private Text[] names;
        [SerializeField] private Text[] infos;
        [SerializeField] private Image[] moods;
        [SerializeField] private Text[] moodTexts;
        [SerializeField] private GameObject[] newGroups;
        [SerializeField] private GameObject[] lockGroups;
        [SerializeField] private Text[] lockPrices;

        [Header("Alt butonlar")]
        [SerializeField] private Image tasksButton;
        [SerializeField] private Text tasksBadge;
        [SerializeField] private Image memoriesButton;
        [SerializeField] private SettingsPanel settings;

        private void OnEnable()
        {
            Loc.Changed += Refresh;
            GameSession.Advanced += Refresh;
            GameSession.PetDied += OnPetDied;
            StoreService.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.Changed -= Refresh;
            GameSession.Advanced -= Refresh;
            GameSession.PetDied -= OnPetDied;
            StoreService.Changed -= Refresh;
        }

        private static void OnPetDied() => LoadingScreen.Go(MemorialController.MemorialScene);

        /// <summary>Karta dokunuş: dost varsa odası, boş yer varsa sahiplenme, kilitliyse mağaza.</summary>
        public void TapCard(int index)
        {
            SaveData save = GameSession.Data;
            if (index < save.pets.Count)
            {
                GameSession.SelectPet(save.pets[index]);
                LoadingScreen.Go(PetController.PetScene);
            }
            else if (index < Shop.Slots(save)) LoadingScreen.Go(AdoptController.AdoptScene);
            else
            {
                string next = Shop.NextSlotProduct(save, DateTime.UtcNow);
                if (next != null) ShopController.OpenFor(next);
                else LoadingScreen.Go(ShopController.ShopScene);
            }
        }

        public void OpenTasks()
        {
            if (GameSession.CurrentPet != null) LoadingScreen.Go(HubController.HubScene);
            else LoadingScreen.Go(AdoptController.AdoptScene);
        }

        public void OpenShop() => LoadingScreen.Go(ShopController.ShopScene);
        public void OpenSettings() => settings.Open();

        public void OpenMemorial()
        {
            if (GameSession.Data.memorial.Count > 0) LoadingScreen.Go(MemorialController.MemorialScene);
        }

        private void Refresh()
        {
            SaveData save = GameSession.Data;
            goldText.text = save.gold.ToString();
            diamondText.text = save.diamonds.ToString();

            int slots = Shop.Slots(save);
            for (int i = 0; i < cards.Length; i++)
            {
                bool hasPet = i < save.pets.Count;
                bool free = !hasPet && i < slots;
                petGroups[i].SetActive(hasPet);
                newGroups[i].SetActive(free);
                lockGroups[i].SetActive(!hasPet && !free);
                cards[i].color = hasPet || free ? Color.white : new Color(1f, 1f, 1f, 0.72f);
                if (hasPet) ShowPet(i, save.pets[i]);
                else if (!free) ShowLock(i, slots);
            }

            Progress.EnsureDay(save, DateTime.Now);
            int ready = 0;
            for (int i = 0; i < save.taskIds.Count; i++)
                if (Progress.TaskDone(save, i) && !save.taskClaimed[i]) ready++;
            tasksBadge.text = ready.ToString();
            tasksBadge.transform.parent.gameObject.SetActive(ready > 0 && save.pets.Count > 0);
            // Henüz işe yaramayan buton soluk (rengi kalır)
            SetAlpha(tasksButton, save.pets.Count > 0 ? 1f : 0.6f);
            SetAlpha(memoriesButton, save.memorial.Count > 0 ? 1f : 0.55f);
        }

        private static void SetAlpha(Image image, float alpha)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        private void ShowPet(int i, PetState pet)
        {
            Sprite art = PetArt.ForPet(pet);
            portraits[i].sprite = art;
            portraits[i].enabled = art != null;
            names[i].text = pet.name;
            infos[i].text = Loc.Age(pet.AgeDays);
            MoodKind mood = Mood.Of(pet);
            string icon = mood == MoodKind.Sleeping ? "icon_fx_zzz" : PetController.MoodIcon(mood);
            Sprite sprite = icon != null ? PetArt.Get(icon) : null;
            moods[i].transform.parent.gameObject.SetActive(sprite != null);
            moods[i].sprite = sprite;
            moodTexts[i].text = Loc.Get(CardMoodKey(mood));
        }

        /// <summary>Kartta yer dar: uzun ruh hali cümlelerinin kısası.</summary>
        private static string CardMoodKey(MoodKind mood)
        {
            string key = Mood.Key(mood);
            return key == "mood_critical" ? "card_mood_critical" : key == "mood_sick" ? "card_mood_sick" : key;
        }

        private void ShowLock(int i, int slots)
        {
            // Yalnızca sıradaki yerin fiyatı yazar: yerler sırayla açılır
            string next = i == slots ? Shop.NextSlotProduct(GameSession.Data, DateTime.UtcNow) : null;
            lockPrices[i].text = next != null ? StoreService.PriceOf(ProductCatalog.Get(next)) : "";
        }
    }
}
