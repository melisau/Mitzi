using UnityEngine;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.Hub
{
    public class HubFurnitureView : MonoBehaviour
    {
        [SerializeField] private Transform[] slots;

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
                }
            }

            if (SaveService.Data == null || SaveService.Data.placedItemIds == null)
                return;

            foreach (var id in SaveService.Data.placedItemIds)
            {
                var item = GameFlow.Instance.Catalog.GetItem(id);
                if (item == null)
                    continue;

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
                    sr.sortingOrder = item.slotType == PawPath.Content.FurnitureSlotType.Rug ? 2 : 4;
                    FitItem(sr, item.slotType);
                }
            }
        }

        static void FitItem(SpriteRenderer renderer, PawPath.Content.FurnitureSlotType type)
        {
            if (renderer.sprite == null || renderer.sprite.bounds.size.y <= 0f)
                return;
            float targetHeight = type switch
            {
                PawPath.Content.FurnitureSlotType.Rug => 1.45f,
                PawPath.Content.FurnitureSlotType.Bed => 2.1f,
                PawPath.Content.FurnitureSlotType.Bowl => 1.0f,
                _ => 5f
            };
            float scale = targetHeight / renderer.sprite.bounds.size.y;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
