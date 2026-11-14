using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Hafıza kartları: seviye arttıkça kart çoğalır (8'den 20'ye), resimler her oyunda karışır; ikişer açılır, aynıysa
    /// açık kalır. Bütün çiftler bulununca puan GameLevels.MemoryScore(hamle, çift).
    /// </summary>
    public class MemoryGame : MiniGameBase
    {
        [SerializeField] private Image[] faces;
        [SerializeField] private Color[] pairColors;
        [SerializeField] private Color backColor;
        [Tooltip("Çizimler varsa: kapalı kartta arka yüz, açık kartta çiftin resmi (kartın çocukları)")]
        [SerializeField] private Image[] backs;
        [SerializeField] private Image[] symbols;
        [SerializeField] private Sprite[] pairSprites;

        private int[] deck;
        private bool[] open;
        private int first = -1, moves, found, pairs;
        private int[] pictures;
        private bool busy;

        protected override void StartGame()
        {
            pairs = GameLevels.MemoryPairs(GameLevels.Get(GameSession.Data, MiniGame.Memory));
            var random = new System.Random();
            deck = MiniGameRules.MemoryDeck(pairs, random);
            open = new bool[deck.Length];
            // Her oyunda başka resimler
            pictures = new int[pairSprites != null ? pairSprites.Length : 0];
            for (int i = 0; i < pictures.Length; i++) pictures[i] = i;
            for (int i = pictures.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (pictures[i], pictures[j]) = (pictures[j], pictures[i]);
            }
            LayOut();
            first = -1;
            moves = found = 0;
            UpdateScore();
            StartCoroutine(Preview());
        }

        /// <summary>Kartları sayılarına göre dizer; fazlası gizlenir. Çok kartta biraz küçülür.</summary>
        private void LayOut()
        {
            int count = deck.Length, rows = Mathf.CeilToInt(count / 4f);
            float stepY = rows >= 5 ? 245f : 270f, scale = rows >= 5 ? 0.9f : 1f;
            float topY = -40f + (rows - 1) * 0.5f * stepY;
            for (int i = 0; i < faces.Length; i++)
            {
                bool show = i < count;
                faces[i].gameObject.SetActive(show);
                if (!show) continue;
                faces[i].rectTransform.anchoredPosition = new Vector2((i % 4 - 1.5f) * 250f, topY - (i / 4) * stepY);
                faces[i].rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        /// <summary>
        /// Başta bütün kartlar kısa süre açık durur (öğrenirken daha uzun): çocuk oyunun ne olduğunu görür,
        /// yerleri aklında tutmaya çalışır.
        /// </summary>
        private IEnumerator Preview()
        {
            busy = true;
            for (int i = 0; i < deck.Length; i++) Open(i);
            Sound.Play("flip", 0.8f);
            yield return new WaitForSeconds((Coach != null && Coach.Learning ? 2.5f : 1.3f) + pairs * 0.08f);
            for (int i = 0; i < deck.Length; i++) Close(i);
            Sound.Play("flip", 0.8f, 0.9f);
            busy = false;
        }

        private void Update()
        {
            if (busy || Coach == null || !Coach.Wanted || deck == null) return;
            // Rehber el: bir kart açıksa eşini, değilse kapalı bir kartı gösterir (dokun ve çevir)
            int hint = -1;
            if (first >= 0)
            {
                for (int i = 0; i < deck.Length; i++)
                    if (i != first && !open[i] && deck[i] == deck[first]) hint = i;
            }
            else
            {
                for (int i = 0; i < deck.Length && hint < 0; i++)
                    if (!open[i]) hint = i;
            }
            if (hint >= 0) Coach.Point(faces[hint].transform.position);
        }

        private bool HasArt => backs != null && backs.Length == faces.Length && pairSprites != null && pairSprites.Length > 0;

        private void Close(int index)
        {
            faces[index].color = backColor;
            if (!HasArt) return;
            backs[index].gameObject.SetActive(true);
            symbols[index].gameObject.SetActive(false);
        }

        private void Open(int index)
        {
            if (!HasArt)
            {
                faces[index].color = pairColors[deck[index] % pairColors.Length];
                return;
            }
            // Açık kart: çiftin rengi açık tonda, ortasında resmi
            faces[index].color = Color.Lerp(pairColors[deck[index] % pairColors.Length], Color.white, 0.6f);
            backs[index].gameObject.SetActive(false);
            symbols[index].sprite = pairSprites[pictures[deck[index] % pictures.Length]];
            symbols[index].gameObject.SetActive(true);
        }

        public void Flip(int index)
        {
            if (busy || open[index] || index == first) return;
            Open(index);
            Sound.Play("flip", 0.8f);
            if (first < 0)
            {
                first = index;
                return;
            }
            moves++;
            if (deck[first] == deck[index])
            {
                open[first] = open[index] = true;
                found++;
                Sound.Play("sparkle", 0.7f);
                Coach?.Success();
                // Eşleşen iki kart parlar
                Fx.Burst(faces[first].transform, "icon_fx_sparkle", faces[first].transform.position, 3, 60f, false);
                Fx.Burst(faces[index].transform, "icon_fx_sparkle", faces[index].transform.position, 3, 60f, false);
                first = -1;
                UpdateScore();
                if (found == pairs) StartCoroutine(Done());
                return;
            }
            Coach?.Fail();
            StartCoroutine(Hide(first, index));
            first = -1;
            UpdateScore();
        }

        private IEnumerator Hide(int a, int b)
        {
            busy = true;
            yield return new WaitForSeconds(0.7f);
            Close(a);
            Close(b);
            busy = false;
        }

        private IEnumerator Done()
        {
            yield return new WaitForSeconds(0.5f);
            Host.Finish(GameLevels.MemoryScore(moves, pairs));
        }

        private void UpdateScore()
        {
            Host.SetScore(Loc.Format("memory_score", found, pairs, moves));
        }
    }
}
