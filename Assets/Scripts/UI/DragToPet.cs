using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Yiyecek/su butonu sürüklenebilir: parmağın altında butonun resmi gezer, dostun üstüne bırakılınca
    /// butona basılmış sayılır (besler). Dokunup bırakmak da eskisi gibi çalışır.
    /// </summary>
    public class DragToPet : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Image icon;

        private RectTransform ghost;
        private Canvas canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (icon == null || icon.sprite == null) return;
            canvas = GetComponentInParent<Canvas>().rootCanvas;
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = icon.sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            ghost = img.rectTransform;
            ghost.sizeDelta = new Vector2(180f, 180f);
            ghost.position = eventData.position;
            Sound.Play("pickup", 0.7f);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost == null) return;
            ghost.position = eventData.position;
            // Dostun üstündeyken büyür: "bırakabilirsin"
            bool over = target != null && RectTransformUtility.RectangleContainsScreenPoint(target, eventData.position, null);
            float s = Mathf.MoveTowards(ghost.localScale.x, over ? 1.25f : 1f, Time.unscaledDeltaTime * 4f);
            ghost.localScale = new Vector3(s, s, 1f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ghost == null) return;
            bool over = target != null && RectTransformUtility.RectangleContainsScreenPoint(target, eventData.position, null);
            Destroy(ghost.gameObject);
            ghost = null;
            if (over && TryGetComponent(out Button button) && button.interactable) button.onClick.Invoke();
            else Sound.Play("drop", 0.6f);
        }
    }
}
