using System;

namespace PatiDostum
{
    public sealed class Theme
    {
        public string id;
        public int gold;
        /// <summary>Ev paketiyle (decor_pack) açılır.</summary>
        public bool premium;

        public string NameKey => "theme_" + id;
    }

    public enum DecorResult { Selected, Bought, NoMoney, NeedPack }

    /// <summary>Ev dekorasyonu: odaların renk teması (plan §7). Bazıları altınla, bazıları Ev Paketiyle.</summary>
    public static class Decor
    {
        public static readonly Theme[] All =
        {
            new Theme { id = "classic" },
            new Theme { id = "forest", gold = 200 },
            new Theme { id = "ocean", gold = 200 },
            new Theme { id = "space", premium = true },
            new Theme { id = "princess", premium = true },
        };

        public static Theme Get(string id)
        {
            foreach (Theme theme in All)
                if (theme.id == id) return theme;
            return All[0];
        }

        public static bool Owns(SaveData save, Theme theme)
        {
            if (theme.gold == 0 && !theme.premium) return true;
            if (theme.premium) return Shop.Owns(save, ProductCatalog.DecorPack);
            return save.ownedThemes.Contains(theme.id);
        }

        public static DecorResult Tap(SaveData save, Theme theme)
        {
            if (!Owns(save, theme))
            {
                if (theme.premium) return DecorResult.NeedPack;
                if (save.gold < theme.gold) return DecorResult.NoMoney;
                save.gold -= theme.gold;
                save.ownedThemes.Add(theme.id);
                save.theme = theme.id;
                return DecorResult.Bought;
            }
            save.theme = theme.id;
            return DecorResult.Selected;
        }
    }

    public sealed class SeasonEvent
    {
        public string id;
        public int startMonth, startDay, endMonth, endDay;
        /// <summary>Yalnızca etkinlik süresince altınla alınabilen kıyafet (alınan kalır).</summary>
        public string item;

        public string NameKey => "event_" + id;
    }

    /// <summary>Yıl boyu sezon etkinlikleri (internetsiz, tarih tablosundan; plan §7).</summary>
    public static class SeasonEvents
    {
        public static readonly SeasonEvent[] All =
        {
            new SeasonEvent { id = "newyear", startMonth = 12, startDay = 20, endMonth = 1, endDay = 5, item = "santa_hat" },
            new SeasonEvent { id = "children", startMonth = 4, startDay = 20, endMonth = 4, endDay = 26, item = "star_bow" },
            new SeasonEvent { id = "summer", startMonth = 6, startDay = 15, endMonth = 8, endDay = 31, item = "sun_hat" },
            new SeasonEvent { id = "halloween", startMonth = 10, startDay = 25, endMonth = 11, endDay = 2, item = "witch_hat" },
        };

        public static SeasonEvent Current(DateTime local)
        {
            int today = local.Month * 100 + local.Day;
            foreach (SeasonEvent e in All)
            {
                int start = e.startMonth * 100 + e.startDay, end = e.endMonth * 100 + e.endDay;
                bool inside = start <= end ? today >= start && today <= end : today >= start || today <= end;
                if (inside) return e;
            }
            return null;
        }

        public static bool IsActive(string id, DateTime local)
        {
            SeasonEvent current = Current(local);
            return current != null && current.id == id;
        }
    }
}
