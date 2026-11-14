using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Yavru: odadaki dostla diğer dostlardan biri. Her satırda iki dostun resmi, olur mu olmaz mı (nedeninin resmi ve kısa
    /// yazısı), bedel (altın ya da elmas düğmesi; resim ve sayı, çocuk seçer). Yer yoksa düğme sıradaki yeri mağazada gösterir. Yavru doğunca kutlama:
    /// anne-baba yan yana gelir, kalpler, yavru "pıt" diye çıkar, adını çocuk verir.
    /// </summary>
    public class BreedPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private PetController owner;
        [SerializeField] private Text title;
        [SerializeField] private GameObject[] rows;
        [SerializeField] private Image[] portraitsA;
        [SerializeField] private Image[] portraitsB;
        [SerializeField] private Text[] names;
        [SerializeField] private Text[] states;
        [Tooltip("Durumun resmi: okuyamayan çocuk da neden olmadığını anlar")]
        [SerializeField] private Image[] stateIcons;
        [SerializeField] private Image[] goldIcons;
        [SerializeField] private GameObject[] goldButtons;
        [SerializeField] private Text[] goldLabels;
        [SerializeField] private GameObject[] gemButtons;
        [SerializeField] private Text[] gemLabels;

        [Header("Doğum kutlaması")]
        [SerializeField] private GameObject reveal;
        [SerializeField] private RectTransform revealA;
        [SerializeField] private RectTransform revealB;
        [SerializeField] private RectTransform revealBaby;
        [SerializeField] private InputField babyName;
        [SerializeField] private GameObject revealDone;

        private readonly PetState[] partners = new PetState[Shop.MaxPets - 1];
        private readonly BreedResult[] checks = new BreedResult[Shop.MaxPets - 1];
        private PetState baby;

        private void Awake()
        {
            root.SetActive(false);
            reveal.SetActive(false);
        }

        public void Open()
        {
            Refresh();
            root.SetActive(true);
        }

        public void Close() => root.SetActive(false);

        public void BreedGold(int index) => Breed(index, false);
        public void BreedGem(int index) => Breed(index, true);

        private void Breed(int index, bool diamonds)
        {
            PetState pet = GameSession.CurrentPet;
            PetState partner = partners[index];
            if (pet == null || partner == null) return;
            // Yer yoksa sıradaki yer mağazada gösterilir (satın alma orada, ebeveyn kapısıyla)
            if (checks[index] == BreedResult.NoSlot)
            {
                string next = Shop.NextSlotProduct(GameSession.Data, DateTime.UtcNow);
                if (next != null) ShopController.OpenFor(next);
                return;
            }
            SaveData save = GameSession.Data;
            string name = AdoptController.Names[UnityEngine.Random.Range(0, AdoptController.Names.Length)];
            baby = Breeding.Breed(save, pet, partner, name, diamonds, DateTime.UtcNow, out BreedResult result);
            if (baby == null)
            {
                owner.ShowMessage(result == BreedResult.NoMoney ? "vet_nomoney" : "breed_" + result.ToString().ToLowerInvariant());
                Refresh();
                return;
            }
            Progress.CheckBadges(save);
            GameSession.Save();
            StartCoroutine(Reveal(pet, partner));
        }

        private static void SetArt(RectTransform rt, PetState pet, string pose)
        {
            Image image = rt.GetComponent<Image>();
            image.sprite = PetArt.ForPet(pet, pose);
            image.enabled = image.sprite != null;
        }

        private IEnumerator Reveal(PetState a, PetState b)
        {
            reveal.SetActive(true);
            revealDone.SetActive(false);
            babyName.gameObject.SetActive(false);
            revealBaby.gameObject.SetActive(false);
            SetArt(revealA, a, "happy");
            SetArt(revealB, b, "happy");
            Sound.Play("whoosh", 0.7f);
            Vector2 aStart = new Vector2(-620f, 80f), bStart = new Vector2(620f, 80f);
            Vector2 aEnd = new Vector2(-240f, 80f), bEnd = new Vector2(240f, 80f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                revealA.anchoredPosition = Vector2.Lerp(aStart, aEnd, k);
                revealB.anchoredPosition = Vector2.Lerp(bStart, bEnd, k);
                yield return null;
            }
            Vector3 middle = reveal.transform.position + Vector3.up * 200f * transform.lossyScale.y;
            for (int i = 0; i < 4; i++)
            {
                Fx.Burst(transform, "icon_fx_heart", middle, 4, 80f);
                Sound.Play("heart", 0.7f, 1f + i * 0.1f);
                yield return new WaitForSecondsRealtime(0.35f);
            }
            // Yavru pıt diye çıkar
            SetArt(revealBaby, baby, "happy");
            revealBaby.gameObject.SetActive(true);
            Sound.Play("fanfare");
            Sound.PetVoice(baby.species);
            Fx.Confetti(transform, middle, 90);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                float s = Mathf.Sin(t * Mathf.PI * 0.75f) / 0.707f;
                revealBaby.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            revealBaby.localScale = Vector3.one;
            babyName.text = baby.name;
            babyName.gameObject.SetActive(true);
            revealDone.SetActive(true);
        }

        /// <summary>Yavrunun adı onaylandı: odasına geçilir.</summary>
        public void FinishReveal()
        {
            if (baby == null) return;
            string name = babyName.text != null ? babyName.text.Trim() : "";
            if (name.Length > 0) baby.name = name;
            GameSession.SelectPet(baby);
            LoadingScreen.Go(PetController.PetScene);
        }

        public void RandomBabyName()
        {
            babyName.text = AdoptController.Names[UnityEngine.Random.Range(0, AdoptController.Names.Length)];
        }

        /// <summary>Durumun resmi: kalp hazır, yavru dost büyümeli, sevgi az, ilaç hasta, kilit yer yok, kum saati beklemeli.</summary>
        private static Sprite StateIcon(BreedResult check, PetState pet)
        {
            switch (check)
            {
                case BreedResult.Done: return PetArt.Get("icon_fx_heart");
                case BreedResult.NotAdult: return pet != null ? PetArt.Preview(pet.species, pet.color, "happy") : null;
                case BreedResult.NotLoved: return PetArt.Get("icon_stat_love");
                case BreedResult.Sick: return PetArt.Get("icon_act_treat");
                case BreedResult.NoSlot: return PetArt.Get("icon_ui_lock");
                case BreedResult.TooSoon: return PetArt.Get("icon_ui_time");
                case BreedResult.DifferentSpecies: return PetArt.Get("icon_ui_paw");
                default: return null;
            }
        }

        private void Refresh()
        {
            SaveData save = GameSession.Data;
            PetState pet = GameSession.CurrentPet;
            title.text = Loc.Format("breed_title", pet != null ? pet.name : "");
            int row = 0;
            foreach (PetState other in pet != null ? save.pets : new System.Collections.Generic.List<PetState>())
            {
                if (other == pet || row >= rows.Length) continue;
                partners[row] = other;
                BreedResult check = Breeding.Check(save, pet, other);
                checks[row] = check;
                portraitsA[row].sprite = PetArt.ForPet(pet, "normal");
                portraitsB[row].sprite = PetArt.ForPet(other, "normal");
                names[row].text = other.name;
                bool ready = check == BreedResult.Done;
                states[row].text = ready ? Loc.Get("breed_ready") : Loc.Get("breed_" + check.ToString().ToLowerInvariant());
                stateIcons[row].sprite = StateIcon(check, pet);
                stateIcons[row].enabled = stateIcons[row].sprite != null;
                goldIcons[row].sprite = PetArt.Get(ready ? "icon_fx_coin" : "icon_ui_lock");
                goldButtons[row].SetActive(ready || check == BreedResult.NoSlot);
                gemButtons[row].SetActive(ready);
                goldLabels[row].text = ready ? Breeding.GoldCost.ToString() : Loc.Get("home_open_slot");
                gemLabels[row].text = Breeding.DiamondCost.ToString();
                rows[row].SetActive(true);
                row++;
            }
            for (; row < rows.Length; row++)
            {
                partners[row] = null;
                rows[row].SetActive(false);
            }
        }
    }
}
