using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Foto stüdyosu (plan §7): çerçeve seçilir (ilk ikisi bedava, son ikisi Foto Paketiyle), "Çek" denince
    /// arayüz gizlenir, dostun etrafına çerçeve ve altına adıyla tarih yazılır, ekran görüntüsü galeriye kaydedilir.
    /// İnternete hiçbir şey gönderilmez.
    /// </summary>
    public class PhotoPanel : MonoBehaviour
    {
        public const int FreeFrames = 2;

        [SerializeField] private GameObject root;
        [SerializeField] private PetController owner;
        [SerializeField] private Image[] frameButtons;
        [SerializeField] private GameObject[] frameLocks;
        [SerializeField] private Color[] frameColors;
        [SerializeField] private GameObject frame;
        [SerializeField] private Image[] frameBands;
        [SerializeField] private Text caption;
        [Tooltip("Çekim sırasında gizlenen arayüz")]
        [SerializeField] private GameObject[] hideWhileShooting;
        [Tooltip("Çizimli çerçeveler (çiçek, yıldız, kalp, pati); varsa bantların üstünde dostun etrafına konur")]
        [SerializeField] private Image frameArt;
        [SerializeField] private Sprite[] frameSprites;

        private int selected;

        // iOS'ta izin penceresi kapanınca gelen sonuç (Android ve editörde tetiklenmez)
        private void OnEnable() => Gallery.LateResult += OnLateResult;
        private void OnDisable() => Gallery.LateResult -= OnLateResult;
        private void OnLateResult(string key) => owner.ShowMessage(key);

        private void Awake()
        {
            root.SetActive(false);
            frame.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            root.SetActive(true);
        }

        public void Close() => root.SetActive(false);

        public void Pick(int index)
        {
            if (index >= FreeFrames && !Shop.Owns(GameSession.Data, ProductCatalog.PhotoPack))
            {
                ShopController.OpenFor(ProductCatalog.PhotoPack);
                return;
            }
            selected = index;
            Refresh();
        }

        public void Shoot()
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            // Panel (root) kendini gizleyeceği için çekim odanın denetleyicisinde sürer; burada sürseydi
            // root kapanınca coroutine de dururdu
            owner.StartCoroutine(ShootRoutine(pet));
        }

        private IEnumerator ShootRoutine(PetState pet)
        {
            root.SetActive(false);
            foreach (GameObject go in hideWhileShooting) go.SetActive(false);
            foreach (Image band in frameBands) band.color = frameColors[selected];
            bool art = frameArt != null && frameSprites != null && selected < frameSprites.Length && frameSprites[selected] != null;
            if (frameArt != null) frameArt.gameObject.SetActive(art);
            if (art) frameArt.sprite = frameSprites[selected];
            caption.text = $"{pet.name} · {DateTime.Now:dd.MM.yyyy}";
            frame.SetActive(true);

            yield return new WaitForEndOfFrame();
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] png = shot.EncodeToPNG();
            Destroy(shot);

            frame.SetActive(false);
            foreach (GameObject go in hideWhileShooting) go.SetActive(true);
            owner.ShowMessage(Gallery.Save(png, $"AlpisPawPal_{pet.name}_{DateTime.Now:yyyyMMdd_HHmmss}"));
            owner.Refresh();
        }

        private void Refresh()
        {
            bool pack = Shop.Owns(GameSession.Data, ProductCatalog.PhotoPack);
            for (int i = 0; i < frameButtons.Length; i++)
            {
                frameButtons[i].rectTransform.localScale = Vector3.one * (i == selected ? 1.12f : 0.9f);
                frameLocks[i].SetActive(i >= FreeFrames && !pack);
            }
        }
    }
}
