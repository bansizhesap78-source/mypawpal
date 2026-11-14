using UnityEngine;
using UnityEngine.EventSystems;

namespace PatiDostum
{
    /// <summary>
    /// Butonun "jöle" gibi hissettirmesi için: beklerken hafifçe nefes alır (isteğe bağlı), parmak değince
    /// yassılaşarak içeri çöker, bırakınca yay gibi biraz fazla büyüyüp yerine oturur.
    /// </summary>
    public class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("Beklerken büyüyüp küçülme oranı (0: kıpırdamaz)")]
        [SerializeField] private float pulse = 0.04f;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pressedScale = 0.88f;

        private const float Stiffness = 520f;
        private const float Damping = 15f;

        private Vector2 scale = Vector2.one, velocity, target = Vector2.one;
        private float phase;

        private void Awake()
        {
            // Yan yana butonlar aynı anda atmasın
            phase = Random.Range(0f, 6f);
        }

        private void OnEnable()
        {
            scale = target = Vector2.one;
            velocity = Vector2.zero;
        }

        private void Update()
        {
            // Durgun ve nefes almayan buton hiçbir şey yazmaz (her karede ölçek yazmak tuvali yeniden hesaplatır)
            if (pulse <= 0f && target == Vector2.one && velocity.sqrMagnitude < 1e-6f && (scale - target).sqrMagnitude < 1e-6f)
            {
                if (scale != Vector2.one)
                {
                    scale = Vector2.one;
                    transform.localScale = Vector3.one;
                }
                return;
            }
            // Sönümlü yay: hedefe doğru çekilir, biraz taşar, yerine oturur
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            velocity += ((target - scale) * Stiffness - velocity * Damping) * dt;
            scale += velocity * dt;
            float breath = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed + phase) * pulse;
            transform.localScale = new Vector3(scale.x * breath, scale.y * breath, 1f);
        }

        // Basınca genişleyip yassılaşır (jöle), bırakınca hedef 1: yay fazla büyüterek geri getirir
        public void OnPointerDown(PointerEventData eventData)
        {
            target = new Vector2(pressedScale * 1.08f, pressedScale * 0.94f);
            Sound.Play("ui_click", 0.7f);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (target == Vector2.one) return;
            target = Vector2.one;
            velocity += new Vector2(-1.5f, 2.5f);   // bırakınca hafif yukarı zıplasın
        }
    }
}
