using UnityEngine;
using UnityEngine.EventSystems;

namespace PatiDostum
{
    /// <summary>Kabarcığa dokunuş: basar basmaz patlar (kabarcık hareket ettiği için Button'un tıklaması kaçabiliyordu).</summary>
    public class BubbleHit : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private BubbleGame game;
        [SerializeField] private int index;

        public void OnPointerDown(PointerEventData eventData) => game.Pop(index);
    }
}
