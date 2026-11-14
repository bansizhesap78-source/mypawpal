using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>Bir Text'in içeriğini seçili dile göre doldurur ve dil değişince yeniler.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private Text text;

        private void Awake()
        {
            text = GetComponent<Text>();
        }

        private void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            Loc.Changed -= Refresh;
        }

        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

        private void Refresh()
        {
            if (text == null) text = GetComponent<Text>();
            text.text = Loc.Get(key);
        }
    }
}
