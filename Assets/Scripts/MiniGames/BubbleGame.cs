using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Kabarcık patlat: kabarcıklar aşağıdan yükselir, dokununca patlar (+1). Yıldızlı kabarcık +3, içinde çöp olan −2.
    /// 30 saniye; seviye arttıkça hızlanır ve sıklaşır. Her türe uygun (balığın en sevdiği oyun).
    /// </summary>
    public class BubbleGame : MiniGameBase
    {
        private const float Seconds = 30f;

        [SerializeField] private RectTransform area;
        [SerializeField] private Image[] bubbles;
        [SerializeField] private Image[] marks;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private Sprite junkSprite;

        private readonly int[] values = new int[16];
        private readonly float[] speeds = new float[16], phases = new float[16];
        private float timeLeft, spawnTimer;
        private int score, level;
        private bool running;

        protected override void StartGame()
        {
            level = GameLevels.Get(GameSession.Data, MiniGame.Bubble);
            timeLeft = Seconds;
            score = 0;
            spawnTimer = 0.2f;
            foreach (Image bubble in bubbles) bubble.gameObject.SetActive(false);
            running = true;
        }

        public void Pop(int index)
        {
            if (!running || !bubbles[index].gameObject.activeSelf) return;
            score = Mathf.Max(0, score + values[index]);
            bubbles[index].gameObject.SetActive(false);
            Vector3 at = bubbles[index].transform.position;
            if (values[index] < 0)
            {
                Sound.Play("error", 0.6f);
                Coach?.Fail();
                return;
            }
            Sound.Play(values[index] > 1 ? "gem" : "bubble", 0.8f, Random.Range(1f, 1.35f));
            Fx.Burst(bubbles[index].transform, values[index] > 1 ? "icon_fx_star" : "icon_fx_sparkle", at, values[index] > 1 ? 5 : 2, 56f, false, 0.8f);
            Coach?.Success();
        }

        private void Update()
        {
            if (!running) return;
            float dt = Time.deltaTime;
            timeLeft -= dt;
            Host.SetScore(Loc.Format("bubble_score", score, Mathf.CeilToInt(Mathf.Max(0f, timeLeft))));
            if (timeLeft <= 0f)
            {
                running = false;
                foreach (Image bubble in bubbles) bubble.gameObject.SetActive(false);
                Host.Finish(score);
                return;
            }

            float elapsed = Seconds - timeLeft, speed = GameLevels.BubbleSpeed(level);
            bool easy = Coach != null && (Coach.Learning || Coach.Struggling);
            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                spawnTimer = Mathf.Lerp(0.6f, 0.34f, elapsed / Seconds) / Mathf.Sqrt(speed) * (easy ? 1.3f : 1f);
                Spawn(Mathf.Lerp(300f, 460f, elapsed / Seconds) * speed * (easy ? 0.75f : 1f));
            }

            float top = area.rect.height * 0.5f + 120f;
            RectTransform guide = null;
            for (int i = 0; i < bubbles.Length; i++)
            {
                if (!bubbles[i].gameObject.activeSelf) continue;
                RectTransform rt = bubbles[i].rectTransform;
                Vector2 p = rt.anchoredPosition;
                p.y += speeds[i] * dt;
                p.x += Mathf.Sin(Time.time * 2f + phases[i]) * 40f * dt;
                rt.anchoredPosition = p;
                if (p.y > top) bubbles[i].gameObject.SetActive(false);
                else if (values[i] > 0 && (guide == null || p.y > guide.anchoredPosition.y) && p.y < 500f) guide = rt;
            }
            if (Coach != null && Coach.Wanted && guide != null) Coach.Point(guide.position);
        }

        private void Spawn(float speed)
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                if (bubbles[i].gameObject.activeSelf) continue;
                float roll = Random.value;
                values[i] = roll < GameLevels.CatchJunk(level) * 0.7f ? -2 : roll > 0.9f ? 3 : 1;
                speeds[i] = speed * Random.Range(0.85f, 1.2f);
                phases[i] = Random.Range(0f, 6f);
                float size = Random.Range(170f, 240f);
                bubbles[i].rectTransform.sizeDelta = new Vector2(size, size);
                bubbles[i].rectTransform.anchoredPosition = new Vector2(
                    Random.Range(-area.rect.width * 0.5f + 120f, area.rect.width * 0.5f - 120f), -area.rect.height * 0.5f - 120f);
                marks[i].rectTransform.sizeDelta = new Vector2(size * 0.55f, size * 0.55f);
                marks[i].gameObject.SetActive(values[i] != 1);
                if (values[i] != 1) marks[i].sprite = values[i] > 1 ? starSprite : junkSprite;
                bubbles[i].gameObject.SetActive(true);
                return;
            }
        }
    }
}
