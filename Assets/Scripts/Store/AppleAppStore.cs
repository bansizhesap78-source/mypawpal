#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// App Store (StoreKit 2; Assets/Plugins/iOS/PatiStore.swift). Native olaylar ana kuyruktan gelir ama yine de
    /// kuyruğa alınır ve Pump ile Unity'nin ana iş parçacığında işlenir (Google Play arka ucuyla aynı kalıp).
    ///  - Tüketilir ürün: hak verilince Finish ile işlem kapatılır.
    ///  - Kalıcı ürün ve abonelik: Transaction.currentEntitlements ile her açılışta ve "geri yükle" ile yeniden bildirilir.
    ///  - Ask to Buy ya da onay bekleyen alım: OnPending; onaylanınca Transaction.updates ile gelir.
    /// </summary>
    public class AppleAppStore : IStoreBackend
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeCallback(IntPtr type, IntPtr json);

        [DllImport("__Internal")] private static extern void PatiStore_SetCallback(NativeCallback callback);
        [DllImport("__Internal")] private static extern void PatiStore_Connect(string productIds);
        [DllImport("__Internal")] private static extern void PatiStore_Buy(string productId);
        [DllImport("__Internal")] private static extern void PatiStore_Restore();
        [DllImport("__Internal")] private static extern void PatiStore_Finish(string token);

        // JsonUtility ile çözülen olay gövdeleri
        [Serializable] private class CodeEvent { public int code; public string id; }
        [Serializable] private class PriceItem { public string id; public string price; }
        [Serializable] private class PricesEvent { public int code; public PriceItem[] products; }
        [Serializable] private class PurchaseEvent { public string[] ids; public string token; public bool acknowledged; public bool restore; }

        private static readonly Queue<KeyValuePair<string, string>> queue = new Queue<KeyValuePair<string, string>>();
        // Çöp toplayıcı temsilciyi toplamasın diye statik alanda tutulur
        private static readonly NativeCallback callback = OnNative;

        private readonly IStoreEvents events;

        public AppleAppStore(IStoreEvents events)
        {
            this.events = events;
            PatiStore_SetCallback(callback);
        }

        [MonoPInvokeCallback(typeof(NativeCallback))]
        private static void OnNative(IntPtr type, IntPtr json)
        {
            string t = Marshal.PtrToStringUTF8(type);
            string j = Marshal.PtrToStringUTF8(json);
            lock (queue) queue.Enqueue(new KeyValuePair<string, string>(t, j));
        }

        public void Connect(string[] productIds)
        {
            try
            {
                // Ürün listesi, fiyatlar ve sahip olunan alımlar native tarafta sırayla sorulur
                PatiStore_Connect(string.Join(",", productIds));
            }
            catch (Exception error)
            {
                Debug.LogWarning("App Store başlatılamadı: " + error.Message);
                events.OnReady(false);
            }
        }

        public void Buy(string productId) => PatiStore_Buy(productId);
        public void Restore() => PatiStore_Restore();
        public void Finish(string token, bool consumable) => PatiStore_Finish(token);

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
                    events.OnReady(JsonUtility.FromJson<CodeEvent>(json).code == 0);
                    break;
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
                    events.OnFailed(e.id, e.code == 1);   // 1: kullanıcı vazgeçti
                    break;
                }
                case "restored":
                    events.OnRestored(JsonUtility.FromJson<CodeEvent>(json).code == 0);
                    break;
            }
        }
    }
}
#endif
