using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Saklambaç: dost kutulardan birinin altına saklanır, kutular karışır; çocuk hangisinde olduğunu bulur. 5 tur, her
    /// turda karışma hızlanır; puan doğru tahmin sayısı. Seviye arttıkça kutu 3'ten 5'e çıkar. Balık kabuğa saklanır.
    /// </summary>
    public class HideGame : MiniGameBase
    {
        [SerializeField] private RectTransform[] boxes;
        [SerializeField] private RectTransform pet;
        [SerializeField] private Text info;
        [Tooltip("Suda yaşayanlarda kutu yerine")]
        [SerializeField] private Sprite shellSprite;

        private int level, count;
        private bool swims;
        private Vector2[] slots;
        private int[] boxAtSlot;   // her yuvada hangi kutu var
        private int hiddenBox, round, correct;
        private bool choosing;

        protected override void StartGame()
        {
            level = GameLevels.Get(GameSession.Data, MiniGame.Hide);
            count = Mathf.Min(GameLevels.HideBoxes(level), boxes.Length);
            slots = new Vector2[count];
            boxAtSlot = new int[count];
            swims = GameSession.CurrentPet != null && SpeciesCatalog.Get(GameSession.CurrentPet.species).moves == MoveKind.Swim;
            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].gameObject.SetActive(i < count);
                if (swims && shellSprite != null) boxes[i].GetComponent<Image>().sprite = shellSprite;
                if (i >= count) continue;
                slots[i] = SlotPosition(i, count);
                boxes[i].anchoredPosition = slots[i];
                boxAtSlot[i] = i;
            }
            round = correct = 0;
            ShowPetArt(pet);
            StartCoroutine(Round());
        }

        /// <summary>3 kutu tek sıra, 4 kutu iki sıra, 5 kutu 3 + 2.</summary>
        private static Vector2 SlotPosition(int i, int total)
        {
            if (total <= 3) return new Vector2((i - 1) * 320f, -100f);
            if (total == 4) return new Vector2(i % 2 == 0 ? -200f : 200f, i < 2 ? 130f : -260f);
            return i < 3 ? new Vector2((i - 1) * 320f, 130f) : new Vector2(i == 3 ? -180f : 180f, -260f);
        }

        private IEnumerator Round()
        {
            choosing = false;
            UpdateScore();
            info.text = Loc.Get("hide_watch");
            hiddenBox = Random.Range(0, count);

            // Dost saklanacağı kutunun önünde görünür, sonra kutu üstüne iner
            pet.gameObject.SetActive(true);
            pet.anchoredPosition = boxes[hiddenBox].anchoredPosition + new Vector2(0f, -10f);
            yield return Lift(hiddenBox, true);
            yield return new WaitForSeconds(0.6f);
            yield return Lift(hiddenBox, false);
            pet.gameObject.SetActive(false);

            // Karıştır: rastgele iki yuvayı yer değiştir (öğrenirken ya da zorlanırken az ve yavaş)
            bool easy = Coach != null && (Coach.Learning || Coach.Struggling);
            int swaps = easy ? 2 + round / 2 : GameLevels.HideSwaps(level, round);
            float duration = Mathf.Lerp(0.45f, 0.22f, round / (float)MiniGameRules.HideRounds) * (easy ? 1.6f : 1f) / Mathf.Min(1f + (level - 1) * 0.04f, 1.5f);
            shuffling = true;
            for (int s = 0; s < swaps; s++)
            {
                int a = Random.Range(0, slots.Length);
                int b = (a + Random.Range(1, slots.Length)) % slots.Length;
                yield return Swap(a, b, duration);
            }

            shuffling = false;
            info.text = Loc.Get(swims ? "hide_pick_shell" : "hide_pick");
            choosing = true;
        }

        private bool shuffling;

        private void Update()
        {
            if (Coach == null || !Coach.Wanted) return;
            // Öğrenirken el, karışırken dostun saklandığı kutuyu takip eder ("buna bak");
            // seçme anında o kutuya dokunmayı gösterir
            if (shuffling && Coach.Learning) Coach.Point(boxes[hiddenBox].position, false);
            else if (choosing) Coach.Point(boxes[hiddenBox].position);
        }

        public void Pick(int box)
        {
            if (!choosing) return;
            choosing = false;
            StartCoroutine(Reveal(box));
        }

        private IEnumerator Reveal(int box)
        {
            bool right = box == hiddenBox;
            if (right) correct++;
            if (right)
            {
                Sound.Play("sparkle");
                if (GameSession.CurrentPet != null) Sound.PetVoice(GameSession.CurrentPet.species, 0.7f);
                Coach?.Success();
                Fx.Burst(boxes[hiddenBox], "icon_fx_heart", boxes[hiddenBox].position, 5, 80f);
            }
            else
            {
                Sound.Play("error", 0.6f);
                Coach?.Fail();
            }
            info.text = Loc.Get(right ? "hide_right" : "hide_wrong");
            pet.anchoredPosition = boxes[hiddenBox].anchoredPosition + new Vector2(0f, -10f);
            pet.gameObject.SetActive(true);
            yield return Lift(hiddenBox, true);
            UpdateScore();
            yield return new WaitForSeconds(0.9f);
            yield return Lift(hiddenBox, false);
            pet.gameObject.SetActive(false);

            round++;
            UpdateScore();
            if (round >= MiniGameRules.HideRounds)
            {
                info.text = "";   // sonuç kartının arkasından görünmesin
                Host.Finish(correct);
            }
            else StartCoroutine(Round());
        }

        /// <summary>Kutuyu kaldırır (altındaki dost görünür) ya da indirir.</summary>
        private IEnumerator Lift(int box, bool up)
        {
            RectTransform rt = boxes[box];
            Vector2 baseline = SlotOf(box);
            Vector2 from = up ? baseline : baseline + new Vector2(0f, 170f);
            Vector2 to = up ? baseline + new Vector2(0f, 170f) : baseline;
            if (up) Sound.Play("whoosh", 0.5f);
            for (float t = 0f; t < 1f; t += Time.deltaTime * 4f)
            {
                rt.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }
            rt.anchoredPosition = to;
        }

        private IEnumerator Swap(int slotA, int slotB, float duration)
        {
            RectTransform a = boxes[boxAtSlot[slotA]];
            RectTransform b = boxes[boxAtSlot[slotB]];
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float arc = Mathf.Sin(t * Mathf.PI) * 80f;
                a.anchoredPosition = Vector2.Lerp(slots[slotA], slots[slotB], t) + new Vector2(0f, arc);
                b.anchoredPosition = Vector2.Lerp(slots[slotB], slots[slotA], t) - new Vector2(0f, arc);
                yield return null;
            }
            a.anchoredPosition = slots[slotB];
            b.anchoredPosition = slots[slotA];
            (boxAtSlot[slotA], boxAtSlot[slotB]) = (boxAtSlot[slotB], boxAtSlot[slotA]);
        }

        private Vector2 SlotOf(int box)
        {
            for (int i = 0; i < boxAtSlot.Length; i++)
                if (boxAtSlot[i] == box) return slots[i];
            return slots[box];
        }

        private void UpdateScore()
        {
            Host.SetScore(Loc.Format("hide_score", correct, round, MiniGameRules.HideRounds));
        }
    }
}
