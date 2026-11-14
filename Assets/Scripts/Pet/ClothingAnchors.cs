using System;
using System.Collections.Generic;
using UnityEngine;

namespace PatiDostum
{
    /// <summary>
    /// Çizimli dostların her pozunda kıyafetlerin yeri (Resources/ClothingAnchors.json; tools/kiyafet_noktalari.py
    /// gözlerden ve siluetten hesaplar). Her yuva için [x, y, kenar]: çizimin kare tuvaline oranla 0..1, y aşağıdan.
    /// </summary>
    public static class ClothingAnchors
    {
        [Serializable]
        private class Entry
        {
            public string key;
            public float[] hat, glasses, neck, outfit, feet;
        }

        [Serializable]
        private class Table
        {
            public Entry[] entries;
        }

        private static Dictionary<string, Entry> map;

        /// <summary>Pozun (çizim adı, ör. "cat_normal") yuvadaki yeri; yoksa null.</summary>
        public static float[] Get(string pose, ClothingSlot slot)
        {
            if (map == null) Load();
            if (pose == null || !map.TryGetValue(pose, out Entry e)) return null;
            switch (slot)
            {
                case ClothingSlot.Hat: return e.hat;
                case ClothingSlot.Glasses: return e.glasses;
                case ClothingSlot.Neck: return e.neck;
                case ClothingSlot.Outfit: return e.outfit;
                default: return e.feet;
            }
        }

        private static void Load()
        {
            map = new Dictionary<string, Entry>();
            var file = Resources.Load<TextAsset>("ClothingAnchors");
            if (file == null) return;
            Table table = JsonUtility.FromJson<Table>(file.text);
            if (table?.entries == null) return;
            foreach (Entry e in table.entries) map[e.key] = e;
        }
    }
}
