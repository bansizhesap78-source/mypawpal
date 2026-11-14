using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Mağaza sahnesi: elmas, altın, slot, paket ve bilet kartları (ProductCatalog sırası) ve elmas→altın değişimi.
    /// Gerçek paralı her alımdan önce ebeveyn kapısı ve aylık harcama limiti denetlenir (plan §7b).
    /// Kartın fiyatı mağazadan gelen yerel fiyattır; kalıcı ürün alındıysa "Alındı" yazar.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        public const string ShopScene = "Shop";

        [SerializeField] private Text goldText;
        [SerializeField] private Text diamondText;
        [Tooltip("ProductCatalog.All sırasıyla")]
        [SerializeField] private GameObject[] cards;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Text[] priceTexts;
        [SerializeField] private Image[] priceButtons;
        [SerializeField] private Text[] ownedTexts;
        [SerializeField] private Text exchangeText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject testBadge;
        [SerializeField] private ParentalGate gate;

        private Coroutine messageRoutine;
        /// <summary>Ekran açılınca kaydırılıp gösterilecek ürün (kilide dokunan çocuk aradığını listede aramasın).</summary>
        private static string focusProduct;

        /// <summary>Mağazayı o ürün görünür ve vurgulu olarak açar.</summary>
        public static void OpenFor(string productId)
        {
            focusProduct = productId;
            LoadingScreen.Go(ShopScene);
        }

        private void OnEnable()
        {
            StoreService.Message += OnMessage;
            StoreService.Changed += Refresh;
            Loc.Changed += Refresh;
        }

        private void OnDisable()
        {
            StoreService.Message -= OnMessage;
            StoreService.Changed -= Refresh;
            Loc.Changed -= Refresh;
        }

        private void Start()
        {
            messageText.text = "";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            testBadge.SetActive(StoreService.IsTestStore);
#else
            Destroy(testBadge);
#endif
            StoreService.EnsureConnected();
            Refresh();
            if (focusProduct != null) StartCoroutine(Focus(focusProduct));
            focusProduct = null;
        }

        /// <summary>Ürünün kartına kaydırır ve butonu birkaç kez büyütüp küçültür.</summary>
        private IEnumerator Focus(string productId)
        {
            int index = Array.FindIndex(ProductCatalog.All, p => p.id == productId);
            if (index < 0 || index >= cards.Length || !cards[index].activeInHierarchy) yield break;
            yield return null;   // dizilim otursun
            Canvas.ForceUpdateCanvases();
            RectTransform content = scroll.content, view = scroll.viewport;
            var card = (RectTransform)cards[index].transform;
            float cardTop = -content.InverseTransformPoint(card.TransformPoint(new Vector3(0f, card.rect.yMax, 0f))).y;
            float y = Mathf.Clamp(cardTop - (view.rect.height - card.rect.height) * 0.5f, 0f, Mathf.Max(0f, content.rect.height - view.rect.height));
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);

            Transform button = priceButtons[index].transform;
            Fx.Burst(transform, "icon_fx_sparkle", button.position, 6, 70f, false, 0.8f);
            for (float t = 0f; t < 1.6f; t += Time.unscaledDeltaTime)
            {
                float s = 1f + Mathf.Abs(Mathf.Sin(t * 5.9f)) * 0.14f;
                button.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            button.localScale = Vector3.one;
        }

        public void Back()
        {
            LoadingScreen.Back();
        }

        public void Buy(int index) => PurchaseFlow.Start(gate, ProductCatalog.All[index], Show);

        public void Restore() => gate.Ask(StoreService.Restore);

        public void Exchange()
        {
            Show(Loc.Get(Shop.ExchangeDiamond(GameSession.Data) ? "store_exchanged" : "vet_nomoney"));
            GameSession.Save();
            Refresh();
        }

        private void OnMessage(string key, Product product)
        {
            string name = product != null ? Loc.Get(product.NameKey) : "";
            Show(Loc.Format(key, name));
            Refresh();
        }

        private void Refresh()
        {
            SaveData save = GameSession.Data;
            goldText.text = save.gold.ToString();
            diamondText.text = save.diamonds.ToString();
            exchangeText.text = Loc.Format("store_exchange", Shop.DiamondToGold);

            DateTime now = DateTime.UtcNow;
            for (int i = 0; i < cards.Length; i++)
            {
                Product product = ProductCatalog.All[i];
                // Başlangıç paketi süresi geçince ya da alınınca kaybolur
                if (product.id == ProductCatalog.StarterPack)
                {
                    cards[i].SetActive(Shop.StarterAvailable(save, now));
                    if (!cards[i].activeSelf) continue;
                }
                // Aile paketi yalnızca gerçekten avantajlıyken görünür
                if (product.id == ProductCatalog.FamilyPack)
                {
                    cards[i].SetActive(Shop.CanBuy(save, product, now) || Shop.Owns(save, product.id));
                    if (!cards[i].activeSelf) continue;
                }
                bool canBuy = Shop.CanBuy(save, product, now);
                bool owned = Shop.Owned(save, product, now);
                string price = StoreService.PriceOf(product);
                if (product.id == ProductCatalog.ClubMonthly) price = Loc.Format("per_month", price);
                if (product.id == ProductCatalog.ClubYearly) price = Loc.Format("per_year", price);
                priceTexts[i].text = owned ? Loc.Get("store_owned") : price;
                priceButtons[i].color = canBuy ? new Color32(0x5C, 0xCF, 0xA8, 255) : new Color32(0xB8, 0xB0, 0xBC, 255);
                ownedTexts[i].text = Inventory(save, product);
            }
        }

        /// <summary>Kartın altındaki "Elinde: 2" gibi bilgi.</summary>
        private static string Inventory(SaveData save, Product product)
        {
            int saving = Shop.BundleSaving(save, product);
            if (saving > 0 && !Shop.Owns(save, product.id)) return Loc.Format("store_saving", saving);
            switch (product.grants)
            {
                case "mode": return Loc.Format("store_have", save.modeChangers);
                case "shelter": return Loc.Format("store_have", save.shelterTickets);
                case "boarding": return Loc.Format("store_have", save.boardingTickets);
                case "slot": return Loc.Format("store_slots", Shop.Slots(save), Shop.MaxPets);
                default: return "";
            }
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
    }
}
