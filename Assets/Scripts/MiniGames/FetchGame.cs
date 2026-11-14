using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Top getir: güç çubuğundaki işaret gidip gelir; "AT!" ile durdurulur. Yeşil hedefe ne kadar yakınsa o kadar
    /// yıldız (0–3). Top uçar, dost getirir; puan toplam yıldız. Seviye arttıkça atış çoğalır, işaret hızlanır, hedef
    /// daralır; 3. seviyeden sonra hedef her atışta başka yerde olur (GameLevels).
    /// </summary>
    public class FetchGame : MiniGameBase
    {
        [SerializeField] private RectTransform bar;
        [SerializeField] private RectTransform marker;
        [SerializeField] private RectTransform ball;
        [SerializeField] private RectTransform pet;
        [SerializeField] private Text info;
        [Tooltip("AT! butonu (rehber el buna basar) ve çubuktaki yeşil hedef (işaret içindeyken parlar)")]
        [SerializeField] private RectTransform throwButton;
        [SerializeField] private Image target;
        [Tooltip("Hedefin çevresindeki açık yeşil bölge")]
        [SerializeField] private RectTransform good;

        private float power, direction = 1f;
        private bool aiming;
        private int throwsLeft, stars, level, totalThrows;
        private float aimAt = MiniGameRules.FetchTarget;
        private Vector2 ballHome, petHome;

        protected override void StartGame()
        {
            ballHome = ball.anchoredPosition;
            petHome = pet.anchoredPosition;
            ShowPetArt(pet);
            level = GameLevels.Get(GameSession.Data, MiniGame.Fetch);
            totalThrows = GameLevels.FetchThrows(level);
            throwsLeft = totalThrows;
            stars = 0;
            PlaceTarget();
            info.text = Loc.Get("fetch_hint");
            aiming = true;
            UpdateScore();
        }

        private void Update()
        {
            if (!aiming) return;
            // Her atışta biraz hızlanır; öğrenirken ya da zorlanırken yavaş
            float speed = GameLevels.FetchSpeed(level) + (totalThrows - throwsLeft) * 0.1f;
            if (Coach != null && (Coach.Learning || Coach.Struggling)) speed *= 0.6f;
            power += direction * speed * Time.deltaTime;
            if (power >= 1f) { power = 1f; direction = -1f; }
            if (power <= 0f) { power = 0f; direction = 1f; }
            marker.anchoredPosition = new Vector2((power - 0.5f) * bar.sizeDelta.x, 0f);

            // İşaret hedefteyken hedef parlar: "şimdi bas!"
            bool inZone = GameLevels.FetchStars(power, aimAt, level) >= 2;
            if (target != null)
            {
                float glow = inZone ? 0.75f + Mathf.Sin(Time.time * 18f) * 0.25f : 1f;
                target.transform.localScale = new Vector3(1f, inZone ? 1.35f : 1f, 1f);
                target.color = Color.Lerp(target.color, inZone ? new Color(1f, 0.95f, 0.4f, 1f) * glow : new Color(0.36f, 0.81f, 0.66f, 1f), 0.4f);
            }
            // Rehber el AT! butonunun üstünde bekler, işaret hedefe girince basar
            if (Coach != null && throwButton != null && Coach.Wanted) Coach.Point(throwButton.position, inZone);
        }

        public void Throw()
        {
            if (!aiming) return;
            aiming = false;
            int got = GameLevels.FetchStars(power, aimAt, level);
            stars += got;
            throwsLeft--;
            if (Coach != null)
            {
                if (got >= 2) Coach.Success();
                else Coach.Fail();
            }
            Sound.Play("whoosh");
            StartCoroutine(Fly(got));
        }

        private IEnumerator Fly(int got)
        {
            info.text = Loc.Get("fetch_stars_" + got);
            UpdateScore();
            // Top güce göre uzağa uçar (yayla), dost koşup getirir
            Vector2 target = ballHome + new Vector2(0f, 250f + power * 700f);
            for (float t = 0f; t < 1f; t += Time.deltaTime * 1.6f)
            {
                ball.anchoredPosition = Vector2.Lerp(ballHome, target, t) + new Vector2(Mathf.Sin(t * Mathf.PI) * 120f, 0f);
                ball.localScale = Vector3.one * (1f - t * 0.4f);
                yield return null;
            }
            for (float t = 0f; t < 1f; t += Time.deltaTime * 2f)
            {
                pet.anchoredPosition = Vector2.Lerp(petHome, target, t);
                yield return null;
            }
            for (float t = 0f; t < 1f; t += Time.deltaTime * 2f)
            {
                pet.anchoredPosition = Vector2.Lerp(target, petHome, t);
                ball.anchoredPosition = pet.anchoredPosition + new Vector2(0f, 60f);
                yield return null;
            }
            ball.anchoredPosition = ballHome;
            ball.localScale = Vector3.one;
            pet.anchoredPosition = petHome;
            // Topu getirdi: yıldız aldıysa sevinir
            if (got > 0)
            {
                Sound.Play("sparkle", 0.6f, 0.9f + got * 0.1f);
                if (GameSession.CurrentPet != null) Sound.PetVoice(GameSession.CurrentPet.species, 0.6f);
                // Kazanılan yıldızlar dostun başından fışkırır; 3 yıldızda konfeti
                Fx.Burst(pet, "icon_fx_star", pet.position + Vector3.up * 150f * pet.lossyScale.y, got * 2, 80f);
                if (got == 3) Fx.Confetti(pet, pet.position, 40);
            }
            else Sound.Play("drop", 0.6f);

            if (throwsLeft <= 0)
            {
                info.text = "";   // sonuç kartının arkasından görünmesin
                Host.Finish(stars);
            }
            else
            {
                info.text = Loc.Get("fetch_hint");
                PlaceTarget();
                aiming = true;
            }
        }

        /// <summary>Hedefi seviyenin genişliğine getirir; yeterince yüksek seviyede başka yere taşır.</summary>
        private void PlaceTarget()
        {
            if (target == null || good == null) return;
            aimAt = GameLevels.FetchMovingTarget(level) ? Random.Range(0.18f, 0.82f) : MiniGameRules.FetchTarget;
            float width = bar.sizeDelta.x, zone = GameLevels.FetchZone(level), x = (aimAt - 0.5f) * width;
            target.rectTransform.anchoredPosition = new Vector2(x, 0f);
            target.rectTransform.sizeDelta = new Vector2(zone * 2f * width, target.rectTransform.sizeDelta.y);
            good.anchoredPosition = new Vector2(x, 0f);
            good.sizeDelta = new Vector2(zone * 5f * width, good.sizeDelta.y);
        }

        private void UpdateScore()
        {
            Host.SetScore(Loc.Format("fetch_score", stars, totalThrows - throwsLeft, totalThrows));
        }
    }
}
