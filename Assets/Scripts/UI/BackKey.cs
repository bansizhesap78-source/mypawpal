using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;

namespace PatiDostum
{
    /// <summary>Sahnenin tuvalinde durur; Android geri tuşunu (Input System'de Escape) PanelPop'a iletir.</summary>
    public class BackKey : MonoBehaviour
    {
        // Birden çok tuval (ör. sahneler arası yaşayan yükleme ekranı) aynı basışı iki kez işlemesin
        private static int handledFrame = -1;
        // Basış olaydan yakalanır: tuşa basıp bırakmak aynı karede olunca wasPressedThisFrame kaçırıyordu
        // (ilk geri basışı boşa gidiyordu). Basıştan sonraki birkaç kare içinde bir tuval işler.
        private static int pressedFrame = -100;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Listen()
        {
            InputSystem.onAnyButtonPress.Call(control =>
            {
                if (control is KeyControl key && key.keyCode == Key.Escape) pressedFrame = Time.frameCount;
            });
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool pressed = Time.frameCount - pressedFrame <= 2 || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
            if (!pressed || handledFrame >= Time.frameCount - 2) return;
            // Geri butonu ve açık panel yoksa bu tuvalin işi yok (ör. yükleme ekranı), sahnenin tuvali halletsin
            if (!PanelPop.AnyOpen && !HasBack()) return;
            handledFrame = Time.frameCount;
            pressedFrame = -100;
            PanelPop.HandleBack(transform);
        }

        private bool HasBack()
        {
            foreach (UnityEngine.UI.Button button in GetComponentsInChildren<UnityEngine.UI.Button>(false))
                if (button.name == "Back") return true;
            return false;
        }
    }
}
