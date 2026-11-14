using System;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Ekran efektleri: parçacık patlaması (kalp, yıldız, parıltı), konfeti ve cüzdana uçan altın/elmas.
    /// Parçacıklar sahnenin kök tuvaline, her şeyin üstüne konur; konumlar ekran pikselidir.
    /// Çizimler PetArt'tan gelir (örn. "icon_ui_heart"); çizim yoksa efekt sessizce atlanır.
    /// </summary>
    public static class Fx
    {
        private static readonly Color[] ConfettiColors =
        {
            new Color32(0xFF, 0x7A, 0x6B, 255), new Color32(0x5C, 0xCF, 0xA8, 255), new Color32(0x8F, 0xD3, 0xF4, 255),
            new Color32(0xFF, 0xC9, 0x3C, 255), new Color32(0xC9, 0xA7, 0xF2, 255), new Color32(0xF9, 0x8F, 0xC0, 255),
        };

        /// <summary>Herhangi bir UI nesnesinin ekran konumu.</summary>
        public static Vector3 PositionOf(Transform t) => t.position;

        /// <summary>Ekranın ortası.</summary>
        public static Vector3 ScreenCenter => new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        /// <summary>
        /// Bir noktadan çizimli parçacıklar saçar (kalp, yıldız...). up: yukarı doğru uçsun (kalpler), yoksa her yöne.
        /// speed &lt; 1: yavaş süzülen (Zzz, koku bulutu), daha uzun yaşar.
        /// </summary>
        public static void Burst(Transform anyInScene, string artKey, Vector3 at, int count, float size = 90f, bool up = true, float speed = 1f)
        {
            Sprite sprite = PetArt.Get(artKey);
            Canvas canvas = RootCanvas(anyInScene);
            if (sprite == null || canvas == null) return;
            float unit = canvas.scaleFactor;
            for (int i = 0; i < count; i++)
            {
                Image img = Make(canvas, sprite, Color.white, size * UnityEngine.Random.Range(0.7f, 1.2f));
                Vector2 dir = up
                    ? new Vector2(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(0.7f, 1.2f))
                    : UnityEngine.Random.insideUnitCircle.normalized;
                float velocity = UnityEngine.Random.Range(380f, 720f) * unit * speed;
                img.transform.position = at + (Vector3)(UnityEngine.Random.insideUnitCircle * 30f * unit);
                img.gameObject.AddComponent<FxParticle>().Init(dir * velocity, (up ? 500f : 900f) * unit * speed,
                    UnityEngine.Random.Range(-90f, 90f) * speed, UnityEngine.Random.Range(0.8f, 1.3f) / Mathf.Max(0.5f, speed));
            }
        }

        /// <summary>Renkli kâğıt yağmuru: noktadan yukarı fışkırır, dönerek düşer.</summary>
        public static void Confetti(Transform anyInScene, Vector3 at, int count = 50)
        {
            Canvas canvas = RootCanvas(anyInScene);
            if (canvas == null) return;
            Sound.Play("confetti");
            float unit = canvas.scaleFactor;
            for (int i = 0; i < count; i++)
            {
                Color color = ConfettiColors[i % ConfettiColors.Length];
                Image img = Make(canvas, null, color, 1f);
                img.rectTransform.sizeDelta = new Vector2(UnityEngine.Random.Range(18f, 30f), UnityEngine.Random.Range(28f, 46f));
                img.transform.position = at;
                Vector2 dir = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.6f, 1.6f));
                img.gameObject.AddComponent<FxParticle>().Init(dir * UnityEngine.Random.Range(700f, 1300f) * unit, 1500f * unit,
                    UnityEngine.Random.Range(-540f, 540f), UnityEngine.Random.Range(1.4f, 2.2f));
            }
        }

        /// <summary>Bir noktadan hedefe kavisle uçan çizim (cüzdana altın); her biri varınca onArrive çağrılır.</summary>
        public static void Fly(Transform anyInScene, string artKey, Vector3 from, RectTransform to, int count, float size, Action onArrive)
        {
            Sprite sprite = PetArt.Get(artKey);
            Canvas canvas = RootCanvas(anyInScene);
            if (sprite == null || canvas == null || to == null) return;
            float unit = canvas.scaleFactor;
            for (int i = 0; i < count; i++)
            {
                Image img = Make(canvas, sprite, Color.white, size);
                Vector3 start = from + (Vector3)(UnityEngine.Random.insideUnitCircle * 60f * unit);
                img.gameObject.AddComponent<FxParticle>().InitFly(start, to, i * 0.06f, 0.55f, onArrive);
            }
        }

        private static Canvas RootCanvas(Transform anyInScene)
        {
            if (anyInScene == null) return null;
            Canvas canvas = anyInScene.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas : null;
        }

        /// <summary>
        /// Parçacıkların alt tuvali (tuvalin en üstünde): uçan parçacıklar yalnızca bu katmanı yeniden hesaplatır.
        /// </summary>
        private static Transform Layer(Canvas canvas)
        {
            Transform layer = canvas.transform.Find("FxLayer");
            if (layer == null)
            {
                var go = new GameObject("FxLayer", typeof(RectTransform), typeof(Canvas));
                var rt = (RectTransform)go.transform;
                rt.SetParent(canvas.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                layer = rt;
            }
            layer.SetAsLastSibling();
            return layer;
        }

        private static Image Make(Canvas canvas, Sprite sprite, Color color, float size)
        {
            var go = new GameObject("Fx", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(Layer(canvas), false);
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }
    }
}
