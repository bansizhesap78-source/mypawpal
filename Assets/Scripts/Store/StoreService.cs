using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// Gerçek paralı satın almalar. Açılışta kendini kurar ve sahneler arasında yaşar.
    ///  - Yayın sürümünde Google Play Billing; editörde ve development APK'da test mağazası (para çekilmez).
    ///  - Başarılı alımın hakkı Shop.Grant ile verilir, sonra Play'de tüketilir/onaylanır. Aynı jeton iki kez hak vermez.
    ///  - Ebeveyn kapısı ve aylık limit denetimi arayüzde (ShopController) yapılır; burası yalnızca harcamayı sayar.
    /// </summary>
    public class StoreService : MonoBehaviour, IStoreEvents
    {
        private static StoreService instance;

        private IStoreBackend backend;
        private readonly Dictionary<string, string> prices = new Dictionary<string, string>();
        /// <summary>Etkin aboneliğin son bildirildiği an: üst üste çalışan iki sorgu birbirinin bilgisini silmesin.</summary>
        private float subscriptionSeenAt = -1000f;
        private const float SubscriptionSeenWindow = 60f;
        /// <summary>Geri yüklemeyi kullanıcı istedi: sonunda mutlaka bir sonuç söylenir.</summary>
        private bool userRestore;
        private int restoredCount;
        private string[] productIds;
        /// <summary>Açılışta bağlantı henüz otururken sorgu başarısız olursa kaç kez daha denenir.</summary>
        private int retriesLeft = 2;

        public static bool Ready { get; private set; }
        public static bool IsTestStore { get; private set; }

        /// <summary>Mağazadan bir sonuç geldi: mesaj anahtarı ve (varsa) ürün.</summary>
        public static event Action<string, Product> Message;
        /// <summary>Fiyatlar ya da haklar değişti: ekran yenilensin.</summary>
        public static event Action Changed;

        public static StoreService Instance
        {
            get
            {
                if (instance == null) new GameObject("StoreService").AddComponent<StoreService>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Yalnızca editör ve development build: para çekilmeyen test mağazası
            IsTestStore = true;
            backend = new FakeStore(this, this);
#elif UNITY_IOS
            // iOS yayın sürümü App Store (StoreKit 2) kullanır
            IsTestStore = false;
            backend = new AppleAppStore(this);
#else
            // Yayın sürümü her zaman Google Play Billing kullanır
            IsTestStore = false;
            backend = new GooglePlayStore(this);
#endif
            var ids = new List<string>();
            foreach (Product p in ProductCatalog.All) ids.Add(p.id);
            productIds = ids.ToArray();
            backend.Connect(productIds);
        }

        private void Update()
        {
#if UNITY_IOS && !UNITY_EDITOR
            (backend as AppleAppStore)?.Pump();
#else
            (backend as GooglePlayStore)?.Pump();
#endif
        }

        public static string FormatTl(double price) => price.ToString("0.00", new CultureInfo("tr-TR")) + " TL";

        /// <summary>Mağazanın yerel fiyatı; gelmediyse katalogdaki TL fiyatı.</summary>
        public static string PriceOf(Product product)
        {
            return Instance.prices.TryGetValue(product.id, out string price) && !string.IsNullOrEmpty(price)
                ? price
                : FormatTl(product.tlPrice);
        }

        /// <summary>
        /// Mağaza ekranı açılırken: açılışta internet yoktuysa bağlantı ve fiyatlar yeniden istenir (uygulamayı
        /// kapatıp açmak gerekmesin).
        /// </summary>
        public static void EnsureConnected()
        {
            StoreService store = Instance;
            if (!Ready || store.prices.Count == 0) store.backend.Connect(store.productIds);
        }

        /// <summary>Ebeveyn kapısından ve limit denetiminden sonra çağrılır.</summary>
        public static void Buy(Product product)
        {
            if (!Ready)
            {
                Message?.Invoke("store_unavailable", product);
                EnsureConnected();
                return;
            }
            Instance.backend.Buy(product.id);
        }

        public static void Restore()
        {
            if (!Ready)
            {
                Message?.Invoke("store_unavailable", null);
                EnsureConnected();
                return;
            }
            Instance.userRestore = true;
            Instance.restoredCount = 0;
            Message?.Invoke("store_restoring", null);
            Instance.backend.Restore();
        }

        // ---------- Arka uçtan gelenler ----------

        public void OnReady(bool ok)
        {
            Ready = ok;
            if (!ok) Debug.LogWarning("[Pati Dostum] Mağaza hazır değil (uygulama Play'den yüklenmemiş olabilir).");
            Changed?.Invoke();
        }

        public void OnPrices(Dictionary<string, string> list)
        {
            foreach (KeyValuePair<string, string> pair in list) prices[pair.Key] = pair.Value;
            Changed?.Invoke();
        }

        public void OnPurchased(string productId, string token, bool alreadyFinished, bool restore)
        {
            Product product = ProductCatalog.Get(productId);
            if (product == null) return;
            if (product.kind == ProductKind.Subscription) subscriptionSeenAt = Time.realtimeSinceStartup;
            SaveData save = GameSession.Data;
            bool granted = Shop.Grant(save, product, token);
            if (granted && !restore) Shop.RecordSpend(save, product, DateTime.Now);
            GameSession.Save();

            // Hak verildikten (ya da daha önce verildiği anlaşıldıktan) sonra Play'de kapat
            if (!alreadyFinished || product.kind == ProductKind.Consumable)
                backend.Finish(token, product.kind == ProductKind.Consumable);

            if (granted && restore) restoredCount++;
            if (granted) Message?.Invoke(restore ? "store_restored" : "store_thanks", product);
            Changed?.Invoke();
        }

        public void OnPending(string productId)
        {
            Message?.Invoke("store_pending", ProductCatalog.Get(productId));
        }

        public void OnFailed(string productId, bool cancelled)
        {
            Message?.Invoke(cancelled ? "store_cancelled" : "store_failed", ProductCatalog.Get(productId ?? ""));
        }

        /// <summary>Play etkin abonelik bildirmediyse kulüp kapanır (iptal edilmiş ya da süresi bitmiş).</summary>
        public void OnRestored(bool ok)
        {
            // Yarım kalan alımlar ve fiyatlar kısa süre sonra yeniden sorulur
            if (!ok && !userRestore && retriesLeft > 0)
            {
                retriesLeft--;
                Invoke(nameof(Reconnect), 3f);
            }
            bool seen = Time.realtimeSinceStartup - subscriptionSeenAt < SubscriptionSeenWindow;
            if (ok && !seen && Club.Active(GameSession.Data, DateTime.UtcNow))
            {
                Club.Lapse(GameSession.Data);
                GameSession.Save();
                Changed?.Invoke();
            }
            if (userRestore)
            {
                userRestore = false;
                if (!ok) Message?.Invoke("store_unavailable", null);
                else if (restoredCount == 0) Message?.Invoke("store_restore_none", null);
            }
        }

        private void Reconnect() => backend.Connect(productIds);

        /// <summary>Aboneliği yönetme (iptal) sayfası: Google Play abonelikleri.</summary>
        public static void OpenSubscriptions()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Application.OpenURL("https://apps.apple.com/account/subscriptions");
#else
            Application.OpenURL("https://play.google.com/store/account/subscriptions?package=" + Application.identifier);
#endif
        }
    }
}
