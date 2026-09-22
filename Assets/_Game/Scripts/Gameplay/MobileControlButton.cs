using UnityEngine;
using UnityEngine.EventSystems;

namespace PawPath.Gameplay
{
    public enum MobileAction { Left, Right, Jump, Crouch }

    public static class MobileControlState
    {
        static bool left, right, crouch, jumpQueued;
        public static float Horizontal => (right ? 1f : 0f) - (left ? 1f : 0f);
        public static bool CrouchHeld => crouch;
        public static bool JumpQueued => jumpQueued;
        public static bool ConsumeJump() { bool value = jumpQueued; jumpQueued = false; return value; }
        public static void Set(MobileAction action, bool pressed)
        {
            switch (action)
            {
                case MobileAction.Left: left = pressed; break;
                case MobileAction.Right: right = pressed; break;
                case MobileAction.Jump: if (pressed) jumpQueued = true; break;
                case MobileAction.Crouch: crouch = pressed; break;
            }
        }
        public static void Reset() => left = right = crouch = jumpQueued = false;
    }

    public class MobileControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] MobileAction action;
        public void Configure(MobileAction value) => action = value;
        public void OnPointerDown(PointerEventData eventData) => MobileControlState.Set(action, true);
        public void OnPointerUp(PointerEventData eventData) => MobileControlState.Set(action, false);
        public void OnPointerExit(PointerEventData eventData) => MobileControlState.Set(action, false);
        void OnDisable() => MobileControlState.Set(action, false);
    }
}
