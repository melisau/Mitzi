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
        static DraggableFurniture activeDrag;
        Vector2 grabOffset;
        SaveService.FurnitureCopy copy;
        public string InstanceId => copy != null ? copy.instanceId : null;

        string KeyX => $"PawPath.Furniture.v2.{slotType}.X";
        string KeyY => $"PawPath.Furniture.v2.{slotType}.Y";
        string KeyFlip => $"PawPath.Furniture.v2.{slotType}.Flip";

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

        public void Configure(FurnitureSlotType type, SpriteRenderer renderer, SaveService.FurnitureCopy instance = null)
        {
            copy = instance;
            slotType = type;
            sprite = renderer;
            hitbox = GetComponent<Collider2D>();
            if (hitbox == null)
                hitbox = gameObject.AddComponent<BoxCollider2D>();

            bool movable = type != FurnitureSlotType.Wallpaper && sprite != null && sprite.sprite != null;
            hitbox.enabled = movable;
            if (hitbox is BoxCollider2D box && sprite != null && sprite.sprite != null)
                box.size = sprite.sprite.bounds.size;

            if (movable && copy != null && copy.hasPosition)
                transform.position = new Vector3(copy.x + (HomeRooms.Instance != null ? HomeRooms.Instance.Offset(copy.roomId) : copy.roomId * 16f), copy.y, transform.position.z);
            else if (movable && (copy == null || copy.legacyPlacement) && PlayerPrefs.HasKey(KeyX))
                transform.position = new Vector3(PlayerPrefs.GetFloat(KeyX), PlayerPrefs.GetFloat(KeyY), transform.position.z);
            // Eski kayıtlarda duvara çıkmış eşyalar ilk karede zemine çekilir
            // (ölçek ayarı Configure'dan sonra yapıldığı için bir kare beklenir).
            needsClamp = movable;
            if (movable && sprite != null)
            {
                var scale = sprite.transform.localScale;
                bool flipped = copy != null && !copy.legacyPlacement ? copy.flipped : PlayerPrefs.GetInt(KeyFlip, 0) == 1;
                scale.x = Mathf.Abs(scale.x) * (flipped ? -1f : 1f);
                sprite.transform.localScale = scale;
            }
        }

        public void DisableInteraction()
        {
            dragging = false;
            if(activeDrag==this) activeDrag=null;
            if (hitbox == null)
                hitbox = GetComponent<Collider2D>();
            if (hitbox != null)
                hitbox.enabled = false;
        }

        bool needsClamp;

        void LateUpdate()
        {
            if (!needsClamp)
                return;
            needsClamp = false;
            GetVerticalBounds(out float minY, out float maxY);
            GetHorizontalBounds(out float minX, out float maxX);
            var p = transform.position;
            var clamped = new Vector3(Mathf.Clamp(p.x, minX, maxX), Mathf.Clamp(p.y, minY, maxY), p.z);
            if ((clamped - p).sqrMagnitude > 1e-6f)
            {
                transform.position = clamped;
                if (copy != null) SavePlacement();
                if (copy == null && PlayerPrefs.HasKey(KeyX))
                {
                    PlayerPrefs.SetFloat(KeyX, clamped.x);
                    PlayerPrefs.SetFloat(KeyY, clamped.y);
                    PlayerPrefs.Save();
                }
            }
            if (copy != null && !copy.hasPosition) SavePlacement();
        }

        void Update()
        {
            if (hitbox == null || !hitbox.enabled || !HomeEditMode.Active ||
                GameFlow.Instance == null || !GameFlow.Instance.InHub)
            {
                dragging=false;
                if(activeDrag==this) activeDrag=null;
                return;
            }

            Vector2 world = PointerWorld();
            if (PointerDown() && !PointerOverUi() && hitbox.OverlapPoint(world) && PickAt(world)==this && activeDrag==null)
            {
                activeDrag=this;
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
                if(activeDrag==this) activeDrag=null;
                SavePlacement();
            }
        }

        void OnDisable() { dragging=false; if(activeDrag==this) activeDrag=null; }

        static DraggableFurniture PickAt(Vector2 point)
        {
            DraggableFurniture best=null;
            foreach(var collider in Physics2D.OverlapPointAll(point))
            {
                var candidate=collider.GetComponent<DraggableFurniture>();
                if(candidate==null || !candidate.enabled || candidate.sprite==null) continue;
                if(best==null || candidate.sprite.sortingOrder>best.sprite.sortingOrder ||
                    candidate.sprite.sortingOrder==best.sprite.sortingOrder && candidate.GetInstanceID()>best.GetInstanceID()) best=candidate;
            }
            return best;
        }

        public void FlipHorizontal()
        {
            if (sprite == null)
                return;
            var scale = sprite.transform.localScale;
            scale.x *= -1f;
            sprite.transform.localScale = scale;
            SavePlacement();
        }

        void SavePlacement()
        {
            if (copy != null)
            {
                copy.x = transform.position.x - (HomeRooms.Instance != null ? HomeRooms.Instance.Offset(copy.roomId) : copy.roomId * 16f);
                copy.y = transform.position.y; copy.hasPosition = true; copy.legacyPlacement = false;
                copy.flipped = sprite != null && sprite.transform.localScale.x < 0f;
                SaveService.Persist();
            }
            else
            {
                PlayerPrefs.SetFloat(KeyX, transform.position.x); PlayerPrefs.SetFloat(KeyY, transform.position.y);
                PlayerPrefs.SetInt(KeyFlip, sprite != null && sprite.transform.localScale.x < 0f ? 1 : 0); PlayerPrefs.Save();
            }
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
            if (copy != null && HomeRooms.Instance != null)
            {
                var bounds = HomeRooms.Instance.RoomBounds(copy.roomId);
                screenLeft = bounds.x; screenRight = bounds.y;
            }
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

        // Zeminin arka kenarı (süpürgelik hizası). Kedilerin yürüyebildiği en üst
        // nokta da budur (CatHomeBehaviour.roomY). Yere konan hiçbir eşyanın tabanı
        // bunun üstüne çıkamaz; aksi halde duvara tırmanmış gibi görünür.
        const float FloorBackY = -1.10f;
        const float BottomPadding = 0.10f;

        void GetVerticalBounds(out float minY, out float maxY)
        {
            if (slotType == FurnitureSlotType.Poster)
            {
                // Poster yalnızca duvarın üst bölümüne asılabilir.
                minY = 0.65f;
                maxY = 1.55f;
                return;
            }

            // Sınırlar görselin gerçek alt/üst kenarına göre hesaplanır (eşyaların
            // boyları ve pivotları farklı olduğu için sabit pivot sınırı yetmez).
            float pos = transform.position.y;
            float bottomOffset = -0.5f, topOffset = 0.5f;
            if (sprite != null && sprite.sprite != null)
            {
                Bounds b = sprite.bounds;
                bottomOffset = b.min.y - pos;
                topOffset = b.max.y - pos;
            }

            float screenBottom = -4.45f;
            var cam = Camera.main;
            if (cam != null)
            {
                float depth = Mathf.Abs(transform.position.z - cam.transform.position.z);
                screenBottom = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth)).y;
            }

            // Ön sınır: eşyanın alt kenarı ekranın altına kadar inebilir.
            minY = screenBottom + BottomPadding - bottomOffset;

            switch (slotType)
            {
                case FurnitureSlotType.Rug:
                    // Halı yere yatık durur: arka kenarı süpürgeliği geçemez.
                    maxY = FloorBackY + 0.05f - topOffset;
                    break;
                case FurnitureSlotType.Bed:
                    // Yatağın tabanı zeminin içinde, süpürgeliğin biraz önünde kalır.
                    maxY = FloorBackY - 0.25f - bottomOffset;
                    break;
                case FurnitureSlotType.Bowl:
                case FurnitureSlotType.Water:
                case FurnitureSlotType.Toy:
                    maxY = FloorBackY - 0.15f - bottomOffset;
                    break;
                case FurnitureSlotType.Sand:
                    maxY = FloorBackY - 0.15f - bottomOffset;
                    break;
                default:
                    // Kedi ağacı ve diğer büyük eşyalar duvara yaslanabilir.
                    maxY = FloorBackY - 0.05f - bottomOffset;
                    break;
            }

            if (maxY < minY)
                maxY = minY;
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
