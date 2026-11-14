using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PatiDostum
{
    /// <summary>Mağaza arka ucu: Google Play (yayın sürümü) ya da test mağazası (editör ve development build).</summary>
    public interface IStoreBackend
    {
        void Connect(string[] productIds);
        void Buy(string productId);
        void Restore();
        /// <summary>Hakkı verilen alımı kapatır: tüketilir ürün tüketilir, kalıcı ürün onaylanır.</summary>
        void Finish(string token, bool consumable);
    }

    /// <summary>Arka uçtan StoreService'e gelen olaylar (her zaman ana iş parçacığında çağrılır).</summary>
    public interface IStoreEvents
    {
        void OnReady(bool ok);
        void OnPrices(Dictionary<string, string> prices);
        void OnPurchased(string productId, string token, bool alreadyFinished, bool restore);
        void OnPending(string productId);
        void OnFailed(string productId, bool cancelled);
        /// <summary>Sahip olunan alımların listesi bitti (ok: sorgu başarılı; abonelik bildirilmediyse yok demektir).</summary>
        void OnRestored(bool ok);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Test mağazası: gerçek para yok, alım yarım saniye sonra başarılı olur. Editörde ve development APK'da
    /// kullanılır (uygulama Play'e yüklenmeden mağaza akışı denensin diye). Fiyatlar katalogdaki TL fiyatlarıdır.
    /// </summary>
    public class FakeStore : IStoreBackend
    {
        private readonly MonoBehaviour host;
        private readonly IStoreEvents events;

        public FakeStore(MonoBehaviour host, IStoreEvents events)
        {
            this.host = host;
            this.events = events;
        }

        public void Connect(string[] productIds)
        {
            var prices = new Dictionary<string, string>();
            foreach (string id in productIds)
            {
                Product product = ProductCatalog.Get(id);
                if (product != null) prices[id] = StoreService.FormatTl(product.tlPrice) + " (test)";
            }
            events.OnReady(true);
            events.OnPrices(prices);
        }

        public void Buy(string productId)
        {
            host.StartCoroutine(Complete(productId));
        }

        private IEnumerator Complete(string productId)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            events.OnPurchased(productId, "test-" + Guid.NewGuid().ToString("N"), false, false);
        }

        public void Restore() => host.StartCoroutine(RestoreDone());

        private IEnumerator RestoreDone()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            // Play gibi davranır: etkin abonelik yeniden bildirilir, başka alım "unutulmaz"
            if (Club.Active(GameSession.Data, DateTime.UtcNow))
                events.OnPurchased(ProductCatalog.ClubMonthly, "test-abonelik", true, true);
            events.OnRestored(true);
        }

        public void Finish(string token, bool consumable) { }
    }
#endif

    /// <summary>
    /// Google Play Billing (Assets/Plugins/Android/PatiBilling.androidlib). Java olayları kendi iş parçacığında
    /// gelir; kuyruğa alınır ve Pump ile Unity'nin ana iş parçacığında işlenir.
    /// </summary>
    public class GooglePlayStore : IStoreBackend
    {
        private const string BridgeClass = "com.alpigame.mypawpal.billing.PatiBilling";

        private readonly IStoreEvents events;
        private readonly Queue<KeyValuePair<string, string>> queue = new Queue<KeyValuePair<string, string>>();
        private AndroidJavaObject bridge;
        private string[] productIds;

        // JsonUtility ile çözülen olay gövdeleri
        [Serializable] private class CodeEvent { public int code; public string id; }
        [Serializable] private class PriceItem { public string id; public string price; }
        [Serializable] private class PricesEvent { public int code; public PriceItem[] products; }
        [Serializable] private class PurchaseEvent { public string[] ids; public string token; public bool acknowledged; public bool restore; }

        private class Listener : AndroidJavaProxy
        {
            private readonly GooglePlayStore owner;

            public Listener(GooglePlayStore owner) : base(BridgeClass + "$Listener")
            {
                this.owner = owner;
            }

            // Java'dan çağrılır (adı Java arayüzüyle aynı olmalı)
            public void onEvent(string type, string json)
            {
                lock (owner.queue) owner.queue.Enqueue(new KeyValuePair<string, string>(type, json));
            }
        }

        public GooglePlayStore(IStoreEvents events)
        {
            this.events = events;
        }

        public void Connect(string[] ids)
        {
            productIds = ids;
            if (bridge != null)
            {
                // Yeniden deneme: köprü hazırsa yine "ready" der, fiyatlar ve alımlar yeniden sorulur
                bridge.Call("connect");
                return;
            }
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    bridge = new AndroidJavaObject(BridgeClass, activity, new Listener(this));
                }
                bridge.Call("connect");
            }
            catch (Exception error)
            {
                Debug.LogWarning("Google Play Billing başlatılamadı: " + error.Message);
                events.OnReady(false);
            }
        }

        public void Buy(string productId) => bridge?.Call("buy", productId);
        public void Restore() => bridge?.Call("restore");

        public void Finish(string token, bool consumable)
        {
            bridge?.Call(consumable ? "consume" : "acknowledge", token);
        }

        /// <summary>StoreService.Update her karede çağırır.</summary>
        public void Pump()
        {
            while (true)
            {
                KeyValuePair<string, string> item;
                lock (queue)
                {
                    if (queue.Count == 0) return;
                    item = queue.Dequeue();
                }
                Handle(item.Key, item.Value);
            }
        }

        private void Handle(string type, string json)
        {
            switch (type)
            {
                case "ready":
                {
                    bool ok = JsonUtility.FromJson<CodeEvent>(json).code == 0;
                    events.OnReady(ok);
                    if (ok)
                    {
                        var inapp = new List<string>();
                        var subs = new List<string>();
                        foreach (string id in productIds) (ProductCatalog.IsSubscription(id) ? subs : inapp).Add(id);
                        bridge.Call("queryProducts", string.Join(",", inapp), string.Join(",", subs));
                        // Yarım kalan ya da başka cihazda alınmış ürünler
                        bridge.Call("restore");
                    }
                    break;
                }
                case "products":
                {
                    var prices = new Dictionary<string, string>();
                    PricesEvent e = JsonUtility.FromJson<PricesEvent>(json);
                    if (e.products != null)
                        foreach (PriceItem p in e.products) prices[p.id] = p.price;
                    events.OnPrices(prices);
                    break;
                }
                case "purchase":
                {
                    PurchaseEvent e = JsonUtility.FromJson<PurchaseEvent>(json);
                    if (e.ids != null)
                        foreach (string id in e.ids) events.OnPurchased(id, e.token, e.acknowledged, e.restore);
                    break;
                }
                case "pending":
                {
                    PurchaseEvent e = JsonUtility.FromJson<PurchaseEvent>(json);
                    if (e.ids != null && e.ids.Length > 0) events.OnPending(e.ids[0]);
                    break;
                }
                case "failed":
                {
                    CodeEvent e = JsonUtility.FromJson<CodeEvent>(json);
                    // 7: ITEM_ALREADY_OWNED: başka cihazda alınmış ya da yarım kalmış; hak geri yüklemeyle gelir
                    if (e.code == 7) Restore();
                    else events.OnFailed(e.id, e.code == 1);   // 1: USER_CANCELED
                    break;
                }
                case "restored":
                    events.OnRestored(JsonUtility.FromJson<CodeEvent>(json).code == 0);
                    break;
            }
        }
    }
}
