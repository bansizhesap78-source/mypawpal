using System;

namespace PatiDostum
{
    /// <summary>
    /// Gerçek paralı alımın ortak akışı (mağaza, Merkez): satılabilir mi → ebeveyn kapısı → aylık limit → mağaza.
    /// Plan §7b: her gerçek paralı ekrandan önce ebeveyn kapısı.
    /// </summary>
    public static class PurchaseFlow
    {
        public static void Start(ParentalGate gate, Product product, Action<string> show)
        {
            SaveData save = GameSession.Data;
            if (!Shop.CanBuy(save, product, DateTime.UtcNow))
            {
                // Sessiz kalmasın: elinde mi, yoksa önce başka bir yer mi açılmalı
                show(Loc.Get(Shop.Owned(save, product, DateTime.UtcNow) ? "store_already" : "store_need_prev"));
                return;
            }
            gate.Ask(() =>
            {
                if (!Shop.WithinLimit(save, product, DateTime.Now))
                {
                    show(Loc.Format("store_limit", save.monthlyLimit));
                    return;
                }
                StoreService.Buy(product);
            });
        }
    }
}
