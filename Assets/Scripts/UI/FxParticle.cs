using System;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Fx'in tek parçacığı (kalp, yıldız, konfeti, uçan altın). Kendi kendine hareket eder ve ömrü bitince silinir.
    /// Konumlar ekran pikselidir (tuval Screen Space Overlay).
    /// </summary>
    public class FxParticle : MonoBehaviour
    {
        // Serbest parçacık
        private Vector3 velocity;
        private float gravity, spin, life, age, startScale;
        private Image image;

        // Uçan parçacık (cüzdana)
        private bool flying;
        private Vector3 from, control;
        private RectTransform to;
        private float delay, duration;
        private Action arrived;

        public void Init(Vector3 velocity, float gravity, float spin, float life)
        {
            this.velocity = velocity;
            this.gravity = gravity;
            this.spin = spin;
            this.life = life;
            Setup();
        }

        public void InitFly(Vector3 from, RectTransform to, float delay, float duration, Action arrived)
        {
            flying = true;
            this.from = from;
            this.to = to;
            this.delay = delay;
            this.duration = duration;
            this.arrived = arrived;
            // Kavis: yolun ortasından yana ve yukarı
            Vector3 mid = (from + to.position) * 0.5f;
            control = mid + new Vector3(UnityEngine.Random.Range(-1f, 1f) * Screen.width * 0.25f, Screen.height * 0.08f, 0f);
            transform.position = from;
            Setup();
        }

        private void Setup()
        {
            image = GetComponent<Image>();
            startScale = transform.localScale.x;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            age += dt;
            if (flying) Fly();
            else Drift(dt);
        }

        private void Drift(float dt)
        {
            velocity.y -= gravity * dt;
            transform.position += velocity * dt;
            transform.Rotate(0f, 0f, spin * dt);
            // Hızlı belirir, ömrünün son üçte birinde solar
            float k = age / life;
            float s = startScale * Mathf.Min(1f, age * 8f);
            transform.localScale = new Vector3(s, s, 1f);
            if (image != null)
            {
                Color c = image.color;
                c.a = Mathf.Clamp01((1f - k) * 3f);
                image.color = c;
            }
            if (age >= life) Destroy(gameObject);
        }

        private void Fly()
        {
            if (to == null) { Destroy(gameObject); return; }
            float t = (age - delay) / duration;
            if (t < 0f)
            {
                // Çıkış: yerinde pıt diye belirir
                float pop = startScale * Mathf.Min(1f, age * 10f);
                transform.localScale = new Vector3(pop, pop, 1f);
                return;
            }
            t = Mathf.Clamp01(t);
            float e = t * t * (3f - 2f * t);   // yumuşak başla, yumuşak bitir
            Vector3 end = to.position;
            Vector3 a = Vector3.Lerp(from, control, e), b = Vector3.Lerp(control, end, e);
            transform.position = Vector3.Lerp(a, b, e);
            float s = startScale * Mathf.Lerp(1f, 0.7f, e);
            transform.localScale = new Vector3(s, s, 1f);
            if (t >= 1f)
            {
                arrived?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
