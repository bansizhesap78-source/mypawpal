using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Açılır panel (karartma + kart): açılınca karartma belirir, kart küçükten yay gibi büyüyerek gelir.
    /// Açık paneller bir yığında tutulur; Android geri tuşu en üstteki panelin Kapat/Geri butonuna basar.
    /// Panel yoksa sahnenin kendi Geri butonuna basar (ana oyun ekranında hiçbir şey yapmaz).
    /// </summary>
    public class PanelPop : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private Image dim;

        private static readonly List<PanelPop> open = new List<PanelPop>();

        public static bool AnyOpen
        {
            get
            {
                foreach (PanelPop panel in open)
                    if (panel != null && panel.isActiveAndEnabled) return true;
                return false;
            }
        }
        private float dimAlpha, time, scale, velocity;

        private void Awake()
        {
            if (dim != null) dimAlpha = dim.color.a;
        }

        private void OnEnable()
        {
            open.Remove(this);
            open.Add(this);
            time = 0f;
            scale = 0.55f;
            velocity = 0f;
            Apply();
            // Sahne açılırken paneller bir an açılıp kapanır (kendi Awake'leri gizler); o an ses çıkmasın
            if (Time.timeSinceLevelLoad > 0.3f) Sound.Play("panel_open", 0.6f);
        }

        private void OnDisable() => open.Remove(this);

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            time += dt;
            velocity += ((1f - scale) * 480f - velocity * 17f) * dt;
            scale += velocity * dt;
            Apply();
        }

        private void Apply()
        {
            if (card != null) card.localScale = new Vector3(scale, scale, 1f);
            if (dim != null)
            {
                Color c = dim.color;
                c.a = dimAlpha * Mathf.Clamp01(time / 0.15f);
                dim.color = c;
            }
        }

        /// <summary>Geri tuşu: en üstteki panelin "Close"/"Back" butonu; panel yoksa sahnenin "Back" butonu.</summary>
        public static void HandleBack(Transform sceneCanvas)
        {
            for (int i = open.Count - 1; i >= 0; i--)
            {
                PanelPop panel = open[i];
                if (panel == null || !panel.isActiveAndEnabled) continue;
                Button close = FindButton(panel.card != null ? panel.card : panel.transform, "Close", "Back");
                if (close != null) close.onClick.Invoke();
                else panel.gameObject.SetActive(false);
                return;
            }
            // Sahnenin görünen "Back" butonu (ör. sahiplenmede adımın içinde durur)
            if (sceneCanvas == null) return;
            Button back = FindButton(sceneCanvas, "Back");
            if (back != null) back.onClick.Invoke();
        }

        private static Button FindButton(Transform root, params string[] names)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(false))
                foreach (string n in names)
                    if (button.name == n) return button;
            return null;
        }
    }
}
