using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Giyinme odası: üstte dost (dokunulan parçayı hemen üstünde dener), altında sekmeler ve kaydırılan parçalar.
    /// "Dolabım" sekmesi alınan her şeyi koleksiyon gibi gösterir; giyili olan ilk sıradadır ve "Çıkar" yazar.
    /// Diğer sekmeler (şapka, gözlük, boyun, elbise) hem dolaptakileri hem alınabilecekleri gösterir.
    /// Parçalar bir kez alınır, bütün dostlar giyebilir (Wardrobe.Tap).
    /// </summary>
    public class WardrobePanel : MonoBehaviour
    {
        /// <summary>Sekmeler: 0 Dolabım, sonra ClothingSlot sırasıyla yuvalar.</summary>
        public const int MineTab = 0;

        [SerializeField] private GameObject root;
        [SerializeField] private PetController owner;
        [SerializeField] private PetView preview;
        [SerializeField] private Text countText;
        [SerializeField] private Text emptyText;
        [SerializeField] private Image[] tabs;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Image[] cards;
        [SerializeField] private Image[] previews;
        [SerializeField] private Text[] names;
        [SerializeField] private Image[] stateBacks;
        [SerializeField] private Text[] states;

        private static readonly Color TabOn = new Color32(0xFF, 0xD8, 0x6B, 255);
        private static readonly Color TabOff = new Color32(0xF9, 0xDD, 0xE8, 255);
        private static readonly Color Wear = new Color32(0x5C, 0xCF, 0xA8, 255);
        private static readonly Color Remove = new Color32(0xFF, 0x7A, 0x6B, 255);
        private static readonly Color Buy = new Color32(0xFF, 0xB8, 0x2E, 255);
        private static readonly Color Locked = new Color32(0xB8, 0xB0, 0xBC, 255);

        private readonly List<ClothingItem> items = new List<ClothingItem>();
        private int tab;

        private void Awake()
        {
            root.SetActive(false);
        }

        public void Open()
        {
            root.SetActive(true);
            preview.HoldPose("normal", float.MaxValue, true);
            // Dolabında bir şey varsa koleksiyonla başlar, yoksa şapkalarla
            ShowTab(Wardrobe.OwnedCount(GameSession.Data) > 0 ? MineTab : 1);
        }

        public void Close()
        {
            root.SetActive(false);
            owner.ShowOff();
        }

        public void ShowTab(int index)
        {
            tab = index;
            for (int i = 0; i < tabs.Length; i++)
                tabs[i].color = i == index ? TabOn : TabOff;
            scroll.verticalNormalizedPosition = 1f;
            Refresh();
        }

        public void Tap(int index)
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null || index >= items.Count) return;
            WardrobeResult result = Wardrobe.Tap(GameSession.Data, pet, items[index]);
            if (result == WardrobeResult.NoMoney || result == WardrobeResult.Locked)
            {
                Sound.Play("error", 0.7f);
                Fx.Burst(transform, result == WardrobeResult.NoMoney ? "icon_fx_coin" : "icon_ui_lock", cards[index].transform.position, 3, 70f);
                return;
            }
            // Giydirmek günlük göreve sayılır
            if (result != WardrobeResult.Removed) owner.TrackAction("wear");
            Sound.Play(result == WardrobeResult.Removed ? "pickup" : "sparkle", 0.8f);
            preview.Bounce();
            Vector3 at = preview.transform.position + Vector3.up * (180f * transform.lossyScale.y);
            if (result != WardrobeResult.Removed) Fx.Burst(transform, "icon_fx_sparkle", at, 6, 70f, false);
            GameSession.Save();
            Refresh();
        }

        /// <summary>Üstündeki her şeyi çıkarır; parçalar dolapta kalır.</summary>
        public void RemoveAll()
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            Wardrobe.RemoveAll(pet);
            GameSession.Save();
            Sound.Play("pickup", 0.8f);
            preview.Bounce();
            Refresh();
        }

        /// <summary>Çizimi olan parça: elbise her türde ayrı çizilir (balık giymez).</summary>
        private static bool HasArt(ClothingItem item, PetState pet)
        {
            if (item.slot == ClothingSlot.Feet) return false;
            if (item.slot != ClothingSlot.Outfit) return PetArt.Get("icon_cloth_" + item.id) != null;
            Sprite pose = PetArt.ForPet(pet, "normal");
            return pose != null && PetArt.Get(PetView.OutfitKey(item.id, pose.name)) != null;
        }

        private void Collect(PetState pet)
        {
            SaveData save = GameSession.Data;
            items.Clear();
            foreach (ClothingItem item in Wardrobe.All)
            {
                if (!HasArt(item, pet)) continue;
                bool owned = save.ownedItems.Contains(item.id);
                if (tab == MineTab ? owned : (int)item.slot == tab - 1 && Wardrobe.Visible(save, item, System.DateTime.Now))
                    items.Add(item);
            }
            // Giyili olanlar önce, sonra dolaptakiler, en sonda alınabilecekler
            int Rank(ClothingItem item) => Wardrobe.IsWorn(pet, item) ? 0 : save.ownedItems.Contains(item.id) ? 1 : 2;
            var ordered = new List<ClothingItem>(items);
            items.Sort((a, b) =>
            {
                int rank = Rank(a).CompareTo(Rank(b));
                return rank != 0 ? rank : ordered.IndexOf(a).CompareTo(ordered.IndexOf(b));
            });
        }

        private void Refresh()
        {
            PetState pet = GameSession.CurrentPet;
            if (pet == null) return;
            SaveData save = GameSession.Data;
            preview.Apply(pet, SpeciesCatalog.Get(pet.species));
            Collect(pet);
            countText.text = Loc.Format("closet_count", Wardrobe.OwnedCount(save), Wardrobe.All.Length);
            emptyText.gameObject.SetActive(items.Count == 0);
            emptyText.text = Loc.Get(tab == MineTab ? "closet_empty" : "closet_no_outfit");

            for (int i = 0; i < cards.Length; i++)
            {
                bool show = i < items.Count;
                if (cards[i].gameObject.activeSelf != show) cards[i].gameObject.SetActive(show);
                if (!show) continue;
                ClothingItem item = items[i];
                bool worn = Wardrobe.IsWorn(pet, item);
                bool owned = save.ownedItems.Contains(item.id);
                previews[i].sprite = PetArt.Get("icon_cloth_" + item.id);
                previews[i].color = Color.white;
                previews[i].preserveAspect = true;
                names[i].text = Loc.Get(item.NameKey);
                cards[i].color = worn ? new Color32(0xD8, 0xF6, 0xEA, 255) : Color.white;
                if (owned)
                {
                    states[i].text = Loc.Get(worn ? "wardrobe_remove" : "wardrobe_wear");
                    stateBacks[i].color = worn ? Remove : Wear;
                }
                else if (item.source == "pass" || item.source == "club")
                {
                    states[i].text = Loc.Get("wardrobe_from_" + item.source);
                    stateBacks[i].color = Locked;
                }
                else
                {
                    states[i].text = Loc.Format("price_gold_short", item.price);
                    stateBacks[i].color = Buy;
                }
            }
        }
    }
}
