using UnityEngine;
using UnityEngine.EventSystems;

namespace PatiDostum
{
    /// <summary>
    /// Dostun üstündeki dokunma alanı: parmak gezdirildikçe PetController.Rub'a mesafe bildirir
    /// (banyoda köpükle yıkama, diğer odalarda okşama). Kısa dokunuş alanın Button'ına kalır.
    /// </summary>
    public class PetRub : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private PetController controller;

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            // Ekran pikselini tasarım birimine çevir (telefon çözünürlüğünden bağımsız eşik)
            float scale = transform.lossyScale.x > 0f ? transform.lossyScale.x : 1f;
            controller.Rub(eventData.position, eventData.delta.magnitude / scale);
        }
    }
}
