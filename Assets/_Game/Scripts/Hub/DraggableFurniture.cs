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
        string KeyFlip => $"PawPath.Furniture.{slotType}.Flip";

        // Ev eşyaları yalnızca yerleştirme ve tıklama etkileşimi içindir.
        // Kedilerin tıklanan hedefe yürümesini fiziksel ya da rota engeli olarak
        // kesmez; görsel olarak eşyanın önünden/arkasından geçebilirler.
        public bool BlocksCats => false;

        public Bounds CatObstacleBounds
        {
            get
            {
                if (sprite == null || sprite.sprite == null)
                    return hitbox != null ? hitbox.bounds : new Bounds(transform.position, Vector3.zero);

                Bounds visual = sprite.bounds;
                // Kediler eşyanın tüm dikey görselini değil, yalnızca zemine değen
                // tabanını dolaşır. Böylece yatak/tuvalet görünmez bir duvara dönüşmez.
                float footprintHeight = Mathf.Clamp(visual.size.y * 0.22f, 0.24f, 0.58f);
                float footprintWidth = visual.size.x * (slotType == FurnitureSlotType.Tree ? 0.55f : 0.76f);
                Vector3 center = new Vector3(visual.center.x,
                    visual.min.y + footprintHeight * 0.5f, visual.center.z);
                return new Bounds(center, new Vector3(footprintWidth, footprintHeight, 0.1f));
            }
        }

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
            if (movable && sprite != null)
            {
                var scale = sprite.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (PlayerPrefs.GetInt(KeyFlip, 0) == 1 ? -1f : 1f);
                sprite.transform.localScale = scale;
            }
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
            if (hitbox == null || !hitbox.enabled || !HomeEditMode.Active ||
                GameFlow.Instance == null || !GameFlow.Instance.InHub)
                return;

            Vector2 world = PointerWorld();
            if (PointerDown() && !PointerOverUi() && hitbox.OverlapPoint(world))
            {
                HomeEditMode.Select(this);
                dragging = true;
                grabOffset = (Vector2)transform.position - world;
            }

            if (dragging && PointerHeld())
            {
                Vector2 next = world + grabOffset;
                GetVerticalBounds(out float minY, out float maxY);
                GetHorizontalBounds(out float minX, out float maxX);
                next.x = Mathf.Clamp(next.x, minX, maxX);
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

        public void FlipHorizontal()
        {
            if (sprite == null)
                return;
            var scale = sprite.transform.localScale;
            scale.x *= -1f;
            sprite.transform.localScale = scale;
            PlayerPrefs.SetInt(KeyFlip, scale.x < 0f ? 1 : 0);
            PlayerPrefs.Save();
        }

        void GetHorizontalBounds(out float minX, out float maxX)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                minX = -7.5f;
                maxX = 7.5f;
                return;
            }

            float depth = Mathf.Abs(transform.position.z - cam.transform.position.z);
            float screenLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth)).x;
            float screenRight = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth)).x;
            float halfWidth = sprite != null && sprite.sprite != null
                ? sprite.sprite.bounds.extents.x * Mathf.Abs(transform.localScale.x)
                : 0f;
            const float edgePadding = 0.12f;
            minX = screenLeft + halfWidth + edgePadding;
            maxX = screenRight - halfWidth - edgePadding;

            // Çok geniş bir eşya dar ekrana sığmıyorsa merkezde tutulur.
            if (minX > maxX)
                minX = maxX = (screenLeft + screenRight) * 0.5f;
        }

        void GetVerticalBounds(out float minY, out float maxY)
        {
            switch (slotType)
            {
                case FurnitureSlotType.Poster:
                    // Poster yalnızca duvarın üst bölümüne asılabilir.
                    minY = 0.65f;
                    maxY = 1.55f;
                    break;
                case FurnitureSlotType.Rug:
                case FurnitureSlotType.Sand:
                    minY = -2.35f;
                    maxY = -1.55f;
                    break;
                case FurnitureSlotType.Bowl:
                case FurnitureSlotType.Water:
                case FurnitureSlotType.Toy:
                    minY = -2.65f;
                    maxY = -1.25f;
                    break;
                case FurnitureSlotType.Bed:
                case FurnitureSlotType.Tree:
                    minY = -2.05f;
                    maxY = -0.85f;
                    break;
                default:
                    minY = -2.05f;
                    maxY = -0.75f;
                    break;
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
