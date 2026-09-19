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

            int index = 0;
            foreach (var id in SaveService.Data.placedItemIds)
            {
                if (index >= slots.Length)
                    break;

                var item = GameFlow.Instance.Catalog.GetItem(id);
                if (item == null)
                    continue;

                Transform targetSlot = slots[index];
                if (targetSlot != null)
                {
                    var sr = targetSlot.GetComponent<SpriteRenderer>();
                    if (sr == null)
                        sr = targetSlot.gameObject.AddComponent<SpriteRenderer>();

                    sr.sprite = item.placedSprite != null ? item.placedSprite : FallbackSprite.WhiteCircle();
                    sr.color = Color.white;
                    sr.sortingOrder = 3;
                }
                index++;
            }
        }
    }
}