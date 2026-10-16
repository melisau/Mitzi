using UnityEngine;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.Hub
{
    public class HubFurnitureView : MonoBehaviour
    {
        public static HubFurnitureView Instance { get; private set; }
        [SerializeField] private Transform[] slots;

        void Awake() => Instance = this;

        public Vector3 BowlPosition => slots != null && slots.Length > (int)PawPath.Content.FurnitureSlotType.Bowl &&
            slots[(int)PawPath.Content.FurnitureSlotType.Bowl] != null
            ? slots[(int)PawPath.Content.FurnitureSlotType.Bowl].position
            : new Vector3(-3.4f, -1.65f, 0f);

        public Vector3 WaterPosition => slots != null && slots.Length > (int)PawPath.Content.FurnitureSlotType.Water &&
            slots[(int)PawPath.Content.FurnitureSlotType.Water] != null
            ? slots[(int)PawPath.Content.FurnitureSlotType.Water].position
            : new Vector3(-1.8f, -1.65f, 0f);

        /// <summary>Slotta gerçekten yerleştirilmiş bir eşya varsa onun etkileşimi.</summary>
        public FurnitureInteraction PlacedInteraction(PawPath.Content.FurnitureSlotType type)
        {
            int index = (int)type;
            if (slots == null || index < 0 || index >= slots.Length || slots[index] == null)
                return null;
            var renderer = slots[index].GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                return null;
            return slots[index].GetComponent<FurnitureInteraction>();
        }

        // Hangi model olursa olsun (çelik, bakır, krem…) o yerde bir eşya varsa yeterli.
        public bool HasPlacedWater => HasPlacedInSlot(PawPath.Content.FurnitureSlotType.Water);

        public bool HasPlacedBowl => HasPlacedInSlot(PawPath.Content.FurnitureSlotType.Bowl);

        [SerializeField] private SpriteRenderer roomBackground;
        [SerializeField] private Sprite defaultRoom;

        public bool HasPlacedInSlot(PawPath.Content.FurnitureSlotType slot)
        {
            if (SaveService.Data == null || SaveService.Data.placedItemIds == null ||
                GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return false;
            foreach (var id in SaveService.Data.placedItemIds)
            {
                var item = GameFlow.Instance.Catalog.GetItem(id);
                if (item != null && item.slotType == slot)
                    return true;
            }
            return false;
        }

        /// <summary>Duvar rengi eşyaları odanın arka plan görselini değiştirir.</summary>
        public void BindRoom(SpriteRenderer background, Sprite defaultSprite)
        {
            roomBackground = background;
            defaultRoom = defaultSprite;
        }

        private void OnEnable()
        {
            GameEvents.OnShopChanged += Refresh;
            GameEvents.OnHubEntered += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnShopChanged -= Refresh;
            GameEvents.OnHubEntered -= Refresh;
        }

        public void Bind(Transform[] furnitureSlots) => slots = furnitureSlots;

        public void Refresh()
        {
            if (GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return;
            if (slots == null || slots.Length == 0)
                return;

            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    var existingSr = slot.GetComponent<SpriteRenderer>();
                    if (existingSr != null)
                    {
                        existingSr.sprite = null;
                    }
                    var drag = slot.GetComponent<DraggableFurniture>();
                    if (drag != null)
                        drag.DisableInteraction();
                }
            }

            Sprite room = defaultRoom;

            if (SaveService.Data == null || SaveService.Data.placedItemIds == null)
            {
                ApplyRoom(room);
                return;
            }

            foreach (var id in SaveService.Data.placedItemIds)
            {
                var item = GameFlow.Instance.Catalog.GetItem(id);
                if (item == null)
                    continue;

                if (item.slotType == PawPath.Content.FurnitureSlotType.Wallpaper)
                {
                    if (item.placedSprite != null)
                        room = item.placedSprite;
                    continue;
                }

                int slotIndex = (int)item.slotType;
                if (slotIndex < 0 || slotIndex >= slots.Length)
                    continue;
                Transform targetSlot = slots[slotIndex];
                if (targetSlot != null)
                {
                    var sr = targetSlot.GetComponent<SpriteRenderer>();
                    if (sr == null)
                        sr = targetSlot.gameObject.AddComponent<SpriteRenderer>();

                    sr.sprite = item.placedSprite != null ? item.placedSprite : FallbackSprite.WhiteCircle();
                    sr.color = Color.white;
                    sr.sortingOrder = item.slotType == PawPath.Content.FurnitureSlotType.Rug ||
                        item.slotType == PawPath.Content.FurnitureSlotType.Sand ? 2 :
                        item.slotType == PawPath.Content.FurnitureSlotType.Poster ? 3 : 4;
                    FitItem(sr, item.slotType);
                    var drag = targetSlot.GetComponent<DraggableFurniture>();
                    if (drag == null)
                        drag = targetSlot.gameObject.AddComponent<DraggableFurniture>();
                    drag.Configure(item.slotType, sr);
                    var interaction = targetSlot.GetComponent<FurnitureInteraction>();
                    if (interaction == null)
                        interaction = targetSlot.gameObject.AddComponent<FurnitureInteraction>();
                    interaction.Configure(item);
                }
            }
            ApplyRoom(room);
        }

        void ApplyRoom(Sprite sprite)
        {
            if (roomBackground == null || sprite == null)
                return;
            if (roomBackground.sprite == sprite)
                return;
            roomBackground.sprite = sprite;
            var cam = Camera.main;
            Vector2 size = sprite.bounds.size;
            if (cam == null || size.x <= 0f || size.y <= 0f)
                return;
            // RuntimeBootstrap.FitSpriteToCamera ile aynı: ekranı kaplayacak kadar büyüt.
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            float cover = Mathf.Max(width / size.x, height / size.y) * 1.12f;
            roomBackground.transform.localScale = new Vector3(cover, cover, 1f);
        }

        static void FitItem(SpriteRenderer renderer, PawPath.Content.FurnitureSlotType type)
        {
            if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
                return;
            // Görseller kırpılmış ve zemin gölgeli; ölçüler evdeki kediye (~2.2 birim) göre ayarlandı.
            float targetHeight = type switch
            {
                PawPath.Content.FurnitureSlotType.Rug => 1.35f,
                PawPath.Content.FurnitureSlotType.Bed => 1.55f,
                PawPath.Content.FurnitureSlotType.Bowl => 0.80f,
                PawPath.Content.FurnitureSlotType.Water => 0.72f,
                PawPath.Content.FurnitureSlotType.Sand => 2.85f,
                PawPath.Content.FurnitureSlotType.Poster => 2.05f,
                PawPath.Content.FurnitureSlotType.Tree => 4.40f,
                PawPath.Content.FurnitureSlotType.Toy => 0.95f,
                _ => 2f
            };
            float scale = targetHeight / renderer.sprite.bounds.size.y;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
