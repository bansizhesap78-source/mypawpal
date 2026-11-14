using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Ayarlar: dil, bildirimler; ebeveyn kapısının arkasında "Hayvanlar ölmesin" ve aylık harcama limiti (plan §6, §7b).
    /// Dostla ilgili bilet ve haklar da burada kullanılır: zaman modunu değiştir (Mod Değiştirici), pansiyon,
    /// barınağa gönder (bilet ya da 10 elmas, ebeveyn kapısıyla). Hakkı yoksa mağazaya gider.
    /// "Hayvanlar ölmesin" açıkken ihmal edilen dost kritik kalır ama ölmez; ömrünü tamamlayan dost yine huzurla uyur.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text notificationsValue;
        [SerializeField] private Text noDeathValue;
        [SerializeField] private Text limitValue;
        [SerializeField] private Text modeValue;
        [SerializeField] private Text boardingValue;
        [SerializeField] private Text shelterValue;
        [SerializeField] private Text soundValue;
        [SerializeField] private Text musicValue;
        [SerializeField] private ParentalGate gate;
        [Tooltip("Tek dostu ilgilendiren satırlar: ana sayfada hangi dost olduğu belli değil, gizlenir")]
        [SerializeField] private GameObject[] petRows;

        private void Awake()
        {
            root.SetActive(false);
        }

        private void OnEnable()
        {
            Loc.Changed += Refresh;
        }

        private void OnDisable()
        {
            Loc.Changed -= Refresh;
        }

        public void Open()
        {
            bool petRoom = SceneManager.GetActiveScene().name == PetController.PetScene;
            if (petRows != null)
                foreach (GameObject row in petRows) row.SetActive(petRoom);
            Refresh();
            root.SetActive(true);
        }

        public void Close()
        {
            root.SetActive(false);
        }

        public void ToggleLanguage()
        {
            Loc.Set(Loc.Current == Language.Turkish ? Language.English : Language.Turkish);
        }

        public void ToggleNotifications()
        {
            GameSession.SetNotifications(!GameSession.Data.notificationsOn);
            Refresh();
        }

        public void ToggleSound()
        {
            Sound.SfxOn = !Sound.SfxOn;
            Refresh();
        }

        public void ToggleMusic()
        {
            Sound.MusicOn = !Sound.MusicOn;
            Refresh();
        }

        public void ToggleNoDeath()
        {
            gate.Ask(() =>
            {
                GameSession.Data.noDeath = !GameSession.Data.noDeath;
                GameSession.Save();
                Refresh();
            });
        }

        public void ToggleLimit()
        {
            gate.Ask(() =>
            {
                Shop.NextLimit(GameSession.Data);
                GameSession.Save();
                Refresh();
            });
        }

        public void ChangeMode()
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            if (GameSession.Data.modeChangers < 1)
            {
                ShopController.OpenFor(ProductCatalog.ModeChanger);
                return;
            }
            Shop.ChangeMode(GameSession.Data, pet);
            GameSession.Save();
            Refresh();
        }

        public void StartBoarding()
        {
            SaveData save = GameSession.Data;
            if (save.boardingTickets < 1)
            {
                ShopController.OpenFor(ProductCatalog.Boarding);
                return;
            }
            Shop.StartBoarding(save, DateTime.UtcNow);
            GameSession.Save();
            Refresh();
        }

        public void SendToShelter()
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            if (!Shop.CanSendToShelter(GameSession.Data))
            {
                ShopController.OpenFor(ProductCatalog.Shelter);
                return;
            }
            gate.Ask(() =>
            {
                if (GameSession.SendToShelter(pet)) LoadingScreen.Go(MemorialController.MemorialScene);
            });
        }

        private void Refresh()
        {
            SaveData save = GameSession.Data;
            notificationsValue.text = Loc.Get(save.notificationsOn ? "on" : "off");
            if (soundValue != null) soundValue.text = Loc.Get(Sound.SfxOn ? "on" : "off");
            if (musicValue != null) musicValue.text = Loc.Get(Sound.MusicOn ? "on" : "off");
            noDeathValue.text = Loc.Get(save.noDeath ? "on" : "off");
            limitValue.text = save.monthlyLimit > 0 ? Loc.Format("limit_tl", save.monthlyLimit) : Loc.Get("limit_none");

            PetState pet = GameSession.CurrentPet;
            string mode = pet != null && pet.Mode == SpeedMode.Fast ? "mode_fast" : "mode_real";
            modeValue.text = Loc.Format("settings_count", Loc.Get(mode), save.modeChangers);

            DateTime now = DateTime.UtcNow;
            boardingValue.text = Shop.IsBoarding(save, now)
                ? Loc.Format("boarding_days", Math.Ceiling(new TimeSpan(save.boardingUntilUtcTicks - now.Ticks).TotalDays))
                : Loc.Format("settings_tickets", save.boardingTickets);
            shelterValue.text = save.shelterTickets > 0
                ? Loc.Format("settings_tickets", save.shelterTickets)
                : Loc.Format("shelter_diamonds", Shop.ShelterDiamonds);
        }
    }
}
