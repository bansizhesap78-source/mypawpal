using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Merkez sahnesi (plan §8): altı sekme.
    ///  0 Görevler: bakım serisi ve günün 3 görevi.  1 Pati Pass: 10 basamak, bedava ve premium yol.
    ///  2 Rozetler.  3 Şov: Pet Şov ve Yıldızlar Şovu (rakip puanları katılmadan görünür).
    ///  4 Ev: oda temaları.  5 Kulüp: Pati Kulübü aboneliği ve günlük/aylık hediyeleri.
    /// Gerçek paralı alımlar (Pass, kulüp) PurchaseFlow ile ebeveyn kapısından geçer.
    /// </summary>
    public class HubController : MonoBehaviour
    {
        public const string HubScene = "Hub";

        [SerializeField] private Image[] tabs;
        [SerializeField] private GameObject[] panels;
        [SerializeField] private Text goldText;
        [SerializeField] private Text diamondText;
        [SerializeField] private Text messageText;
        [SerializeField] private ParentalGate gate;

        [Header("Görevler")]
        [SerializeField] private Text streakText;
        [SerializeField] private Text[] taskNames;
        [SerializeField] private Image[] taskIcons;
        [SerializeField] private Text[] taskProgress;
        [SerializeField] private Image[] taskButtons;
        [SerializeField] private Text[] taskButtonLabels;

        [Header("Pati Pass")]
        [SerializeField] private Text passLevelText;
        [SerializeField] private Image passFill;
        [SerializeField] private Text[] freeTexts;
        [SerializeField] private Image[] freeButtons;
        [SerializeField] private Text[] premiumTexts;
        [SerializeField] private Image[] premiumButtons;
        [SerializeField] private Text buyPassLabel;

        [Header("Rozetler")]
        [SerializeField] private Image[] badgeCards;
        [SerializeField] private Text[] badgeNames;
        [SerializeField] private Text[] badgeDescs;
        [SerializeField] private Image[] badgeIcons;

        [Header("Şov")]
        [SerializeField] private Text[] normalRivals;
        [SerializeField] private Text[] starRivals;
        [SerializeField] private Text normalMine;
        [SerializeField] private Text starMine;
        [SerializeField] private Text normalButtonLabel;
        [SerializeField] private Text starButtonLabel;
        [SerializeField] private Text showResult;

        [Header("Ev")]
        [SerializeField] private Image[] themeCards;
        [SerializeField] private Text[] themeStates;

        [Header("Kulüp")]
        [SerializeField] private Text clubStatus;
        [SerializeField] private Text clubDiamondLabel;
        [SerializeField] private Text clubBoardingLabel;
        [SerializeField] private Text monthlyLabel;
        [SerializeField] private Text yearlyLabel;

        private static readonly Color On = new Color32(0x5C, 0xCF, 0xA8, 255);
        private static readonly Color Off = new Color32(0xB8, 0xB0, 0xBC, 255);
        private static int lastTab;
        private Coroutine messageRoutine;

        private void OnEnable()
        {
            Loc.Changed += Refresh;
            StoreService.Changed += Refresh;
            StoreService.Message += OnStoreMessage;
        }

        private void OnDisable()
        {
            Loc.Changed -= Refresh;
            StoreService.Changed -= Refresh;
            StoreService.Message -= OnStoreMessage;
        }

        private void Start()
        {
            messageText.text = "";
            showResult.text = "";
            StoreService.EnsureConnected();
            ShowTab(lastTab);
        }

        public void Back() => LoadingScreen.Back();
        public void OpenShop() => LoadingScreen.Go(ShopController.ShopScene);

        /// <summary>Günlük görevin resmi (Assets/Art/Icons).</summary>
        private static string TaskIcon(string id)
        {
            switch (id)
            {
                case "feed": return "icon_stat_hunger";
                case "drink": return "icon_stat_thirst";
                case "bathe": return "icon_act_bathe";
                case "play": return "icon_act_play";
                case "stroke": return "icon_act_stroke";
                case "minigame": return "icon_act_games";
                case "lesson": return "icon_act_school";
                case "read": return "icon_act_book";
                case "wear": return "icon_cloth_party_hat";
                default: return "icon_act_star";
            }
        }

        public void ShowTab(int index)
        {
            lastTab = index;
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].SetActive(i == index);
                tabs[i].color = i == index ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }
            Refresh();
        }

        // ---------- Görevler ----------

        public void ClaimTask(int index)
        {
            if (Progress.Claim(GameSession.Data, index, DateTime.Now)) Done("hub_task_claimed");
        }

        // ---------- Pass ----------

        public void ClaimFree(int index) => ClaimPass(index, false);
        public void ClaimPremium(int index) => ClaimPass(index, true);

        private void ClaimPass(int index, bool premium)
        {
            if (PatiPass.Claim(GameSession.Data, index + 1, premium, DateTime.Now)) Done("hub_reward_claimed");
            else if (premium && !PatiPass.HasPremium(GameSession.Data, DateTime.Now)) BuyPass();
        }

        public void BuyPass() => PurchaseFlow.Start(gate, ProductCatalog.Get(ProductCatalog.Pass), Show);

        // ---------- Şov ----------

        public void EnterShow() => Enter(false);
        public void EnterStarShow() => Enter(true);

        private void Enter(bool star)
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            ShowResult result = PetShow.Enter(GameSession.Data, pet, star, DateTime.Now);
            if (result == null)
            {
                Show(Loc.Get(star && GameSession.Data.diamonds < PetShow.StarEntryDiamonds ? "vet_nomoney" : "show_done"));
                return;
            }
            string prize = result.diamonds > 0
                ? Loc.Format("show_prize_both", result.gold, result.diamonds)
                : Loc.Format("show_prize_gold", result.gold);
            showResult.text = Loc.Format("show_place", pet.name, result.place) + "\n" + prize;
            Done(null);
        }

        // ---------- Ev ----------

        public void PickTheme(int index)
        {
            switch (Decor.Tap(GameSession.Data, Decor.All[index]))
            {
                case DecorResult.NoMoney: Show(Loc.Get("vet_nomoney")); break;
                case DecorResult.NeedPack: PurchaseFlow.Start(gate, ProductCatalog.Get(ProductCatalog.DecorPack), Show); break;
                default: Done("hub_theme_set"); break;
            }
        }

        // ---------- Kulüp ----------

        public void ClaimClubDiamond()
        {
            if (Club.ClaimDiamond(GameSession.Data, DateTime.Now)) Done("hub_reward_claimed");
        }

        public void ClaimClubBoarding()
        {
            if (Club.ClaimBoarding(GameSession.Data, DateTime.Now)) Done("hub_reward_claimed");
        }

        public void BuyMonthly() => PurchaseFlow.Start(gate, ProductCatalog.Get(ProductCatalog.ClubMonthly), Show);
        public void BuyYearly() => PurchaseFlow.Start(gate, ProductCatalog.Get(ProductCatalog.ClubYearly), Show);
        public void ManageSubscription() => gate.Ask(StoreService.OpenSubscriptions);

        // ---------- Ortak ----------

        private void Done(string key)
        {
            var badges = Progress.CheckBadges(GameSession.Data);
            GameSession.Save();
            if (badges.Count > 0) Show(Loc.Format("badge_new", Loc.Get(badges[0].NameKey)));
            else if (key != null) Show(Loc.Get(key));
            Refresh();
        }

        private void OnStoreMessage(string key, Product product)
        {
            Show(Loc.Format(key, product != null ? Loc.Get(product.NameKey) : ""));
            Refresh();
        }

        private void Show(string message)
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(ShowRoutine(message));
        }

        private IEnumerator ShowRoutine(string message)
        {
            messageText.text = message;
            yield return new WaitForSecondsRealtime(3f);
            messageText.text = "";
        }

        private void Refresh()
        {
            SaveData save = GameSession.Data;
            DateTime now = DateTime.Now;
            goldText.text = save.gold.ToString();
            diamondText.text = save.diamonds.ToString();

            // Görevler
            Progress.EnsureDay(save, now);
            streakText.text = Loc.Format("hub_streak", save.streakDays);
            for (int i = 0; i < taskNames.Length; i++)
            {
                DailyTask task = Progress.Task(save.taskIds[i]);
                taskNames[i].text = Loc.Get(task.NameKey);
                // Okumayı bilmeyen çocuk görevi resminden tanır
                if (taskIcons != null && i < taskIcons.Length && taskIcons[i] != null)
                {
                    Sprite icon = PetArt.Get(TaskIcon(task.id));
                    taskIcons[i].sprite = icon;
                    taskIcons[i].enabled = icon != null;
                }
                taskProgress[i].text = $"{Math.Min(save.taskCounts[i], task.target)}/{task.target}";
                bool done = Progress.TaskDone(save, i);
                taskButtons[i].color = done && !save.taskClaimed[i] ? On : Off;
                taskButtonLabels[i].text = Loc.Get(save.taskClaimed[i] ? "hub_claimed" : "hub_claim");
            }

            // Pass
            PatiPass.EnsureSeason(save, now);
            bool premium = PatiPass.HasPremium(save, now);
            passLevelText.text = Loc.Format("pass_level", PatiPass.Level(save), PatiPass.Levels, PatiPass.DaysLeft(now));
            passFill.fillAmount = save.passXp / (float)(PatiPass.Levels * PatiPass.XpPerLevel);
            for (int i = 0; i < PatiPass.Levels; i++)
            {
                freeTexts[i].text = RewardText(PatiPass.Free[i]);
                premiumTexts[i].text = RewardText(PatiPass.Premium[i]);
                freeButtons[i].color = PatiPass.CanClaim(save, i + 1, false, now) ? On : Off;
                premiumButtons[i].color = PatiPass.CanClaim(save, i + 1, true, now) ? On : Off;
            }
            buyPassLabel.text = premium ? Loc.Get("pass_premium_on") : Loc.Format("pass_buy", StoreService.PriceOf(ProductCatalog.Get(ProductCatalog.Pass)));

            // Rozetler
            for (int i = 0; i < badgeCards.Length; i++)
            {
                Badge badge = Progress.Badges[i];
                bool earned = save.badges.Contains(badge.id);
                badgeCards[i].color = earned ? new Color32(0xFF, 0xF1, 0xC4, 255) : new Color(1f, 1f, 1f, 0.92f);
                badgeNames[i].text = Loc.Get(badge.NameKey);
                // Uzun ad tek satıra sığana kadar küçülür
                badgeNames[i].fontSize = 38;
                float width = badgeNames[i].rectTransform.rect.width;
                while (badgeNames[i].fontSize > 18 && badgeNames[i].preferredWidth > width) badgeNames[i].fontSize -= 2;
                badgeNames[i].color = earned ? new Color32(0xD0, 0x78, 0x10, 255) : new Color(0.29f, 0.2f, 0.33f, 0.8f);
                badgeDescs[i].text = Loc.Get(badge.DescKey);
                // Kazanılmamış rozet soluk görünür
                if (badgeIcons != null && i < badgeIcons.Length && badgeIcons[i] != null)
                    badgeIcons[i].color = earned ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            }

            // Şov
            PetState pet = GameSession.CurrentPet;
            ShowColumn(save, pet, false, normalRivals, normalMine, normalButtonLabel, now);
            ShowColumn(save, pet, true, starRivals, starMine, starButtonLabel, now);

            // Ev
            for (int i = 0; i < themeCards.Length; i++)
            {
                Theme theme = Decor.All[i];
                themeCards[i].color = Color.Lerp(Color.white, PetLooks.ThemeTint(theme.id), theme.id == "classic" ? 0f : 0.7f);
                if (save.theme == theme.id) themeStates[i].text = Loc.Get("theme_selected");
                else if (Decor.Owns(save, theme)) themeStates[i].text = Loc.Get("theme_pick");
                else if (theme.premium) themeStates[i].text = Loc.Get("theme_pack");
                else themeStates[i].text = Loc.Format("price_gold_short", theme.gold);
            }

            // Kulüp
            DateTime utc = DateTime.UtcNow;
            bool club = Club.Active(save, utc);
            clubStatus.text = Loc.Get(club ? "club_active" : "club_inactive");
            clubDiamondLabel.text = Loc.Get(!club ? "club_members_only" : Club.CanClaimDiamond(save, now) ? "club_claim_diamond" : "club_claimed_today");
            clubBoardingLabel.text = Loc.Get(!club ? "club_members_only" : Club.CanClaimBoarding(save, now) ? "club_claim_boarding" : "club_claimed_month");
            // Üyeyken fiyat yerine "Alındı", buton soluk
            monthlyLabel.text = club ? Loc.Get("store_owned") : Loc.Format("per_month", StoreService.PriceOf(ProductCatalog.Get(ProductCatalog.ClubMonthly)));
            yearlyLabel.text = club ? Loc.Get("store_owned") : Loc.Format("per_year", StoreService.PriceOf(ProductCatalog.Get(ProductCatalog.ClubYearly)));
            foreach (Text label in new[] { monthlyLabel, yearlyLabel })
            {
                Image back = label.transform.parent.GetComponent<Image>();
                Color c = back.color;
                c.a = club ? 0.45f : 1f;
                back.color = c;
            }
        }

        private static string RewardText(PassReward reward)
        {
            if (!string.IsNullOrEmpty(reward.item)) return Loc.Get("item_" + reward.item);
            if (reward.gold > 0 && reward.diamonds > 0) return Loc.Format("show_prize_both", reward.gold, reward.diamonds);
            if (reward.diamonds > 0) return Loc.Format("reward_diamonds", reward.diamonds);
            return Loc.Format("price_gold_short", reward.gold);
        }

        private static void ShowColumn(SaveData save, PetState pet, bool star, Text[] rivals, Text mine, Text button, DateTime now)
        {
            int[] scores = PetShow.RivalScores(now, star);
            for (int i = 0; i < rivals.Length; i++) rivals[i].text = $"{PetShow.Rivals[i]}  {scores[i]}";
            mine.text = pet != null ? $"{pet.name}  {PetShow.Score(pet)}" : "";
            int week = PetShow.Week(now);
            bool entered = (star ? save.lastStarShowWeek : save.lastShowWeek) == week;
            if (entered) button.text = Loc.Get("show_done");
            else if (star && !Club.Active(save, now.ToUniversalTime())) button.text = Loc.Format("show_enter_star", PetShow.StarEntryDiamonds);
            else button.text = Loc.Get("show_enter");
        }
    }
}
