using UnityEngine;

namespace PatiDostum
{
    /// <summary>Kendi etrafında yavaşça döner ve hafifçe nefes alır (ödülün arkasındaki ışın patlaması).</summary>
    public class Spin : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 30f;
        [SerializeField] private float pulse = 0.06f;

        private void Update()
        {
            transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
            float s = 1f + Mathf.Sin(Time.unscaledTime * 2.4f) * pulse;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
