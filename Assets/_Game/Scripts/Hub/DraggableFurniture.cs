using UnityEngine;
using UnityEngine.EventSystems;
using PawPath.Content;
using PawPath.Core;

namespace PawPath.Hub
{
    /// <summary>Ev eşyalarını oda sınırları içinde sürükler ve konumunu saklar.</summary>
    public class DraggableFurniture : MonoBehaviour
    {
        [SerializeField] FurnitureSlotType slotType;
        SpriteRenderer sprite;
        Collider2D hitbox;
        bool dragging;
        Vector2 grabOffset;

        string KeyX => $"PawPath.Furniture.{slotType}.X";
        string KeyY => $"PawPath.Furniture.{slotType}.Y";

        public void Configure(FurnitureSlotType type, SpriteRenderer renderer)
        {
            slotType = type;
            sprite = renderer;
            hitbox = GetComponent<Collider2D>();
            if (hitbox == null)
                hitbox = gameObject.AddComponent<BoxCollider2D>();

            bool movable = type != FurnitureSlotType.Wallpaper && sprite != null && sprite.sprite != null;
            hitbox.enabled = movable;
            if (hitbox is BoxCollider2D box && sprite != null && sprite.sprite != null)
                box.size = sprite.sprite.bounds.size;

            if (movable && PlayerPrefs.HasKey(KeyX))
                transform.position = new Vector3(PlayerPrefs.GetFloat(KeyX), PlayerPrefs.GetFloat(KeyY), transform.position.z);
        }

        public void DisableInteraction()
        {
            dragging = false;
            if (hitbox == null)
                hitbox = GetComponent<Collider2D>();
            if (hitbox != null)
                hitbox.enabled = false;
        }

        void Update()
        {
            if (hitbox == null || !hitbox.enabled || GameFlow.Instance == null || !GameFlow.Instance.InHub)
                return;

            Vector2 world = PointerWorld();
            if (PointerDown() && !PointerOverUi() && hitbox.OverlapPoint(world))
            {
                dragging = true;
                grabOffset = (Vector2)transform.position - world;
            }

            if (dragging && PointerHeld())
            {
                Vector2 next = world + grabOffset;
                float minY = slotType == FurnitureSlotType.Rug ? -2.35f : -2.05f;
                float maxY = slotType == FurnitureSlotType.Rug ? -1.55f : -0.75f;
                next.x = Mathf.Clamp(next.x, -5.1f, 5.1f);
                next.y = Mathf.Clamp(next.y, minY, maxY);
                transform.position = new Vector3(next.x, next.y, transform.position.z);
            }

            if (dragging && PointerUp())
            {
                dragging = false;
                PlayerPrefs.SetFloat(KeyX, transform.position.x);
                PlayerPrefs.SetFloat(KeyY, transform.position.y);
                PlayerPrefs.Save();
            }
        }

        static bool PointerOverUi()
        {
            if (EventSystem.current == null)
                return false;
            return Input.touchCount > 0
                ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        static Vector2 PointerWorld()
        {
            Vector3 screen = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
            var cam = Camera.main;
            screen.z = Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(screen);
        }

        static bool PointerDown() => Input.touchCount > 0
            ? Input.GetTouch(0).phase == TouchPhase.Began
            : Input.GetMouseButtonDown(0);

        static bool PointerHeld()
        {
            if (Input.touchCount > 0)
            {
                var phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
            }
            return Input.GetMouseButton(0);
        }

        static bool PointerUp()
        {
            if (Input.touchCount > 0)
            {
                var phase = Input.GetTouch(0).phase;
                return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
            }
            return Input.GetMouseButtonUp(0);
        }
    }
}
