using UnityEngine;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.Hub
{
    /// <summary>
    /// Satın alınan mobilyayı evde basit sprite olarak gösterir.
    /// </summary>
    public class HubFurnitureView : MonoBehaviour
    {
        [SerializeField] Transform[] slots;

        void OnEnable()
        {
            GameEvents.OnShopChanged += Refresh;
            GameEvents.OnHubEntered += Refresh;
        }

        void OnDisable()
        {
            GameEvents.OnShopChanged -= Refresh;
            GameEvents.OnHubEntered -= Refresh;
        }

        public void Bind(Transform[] furnitureSlots) => slots = furnitureSlots;

        void Refresh()
        {
            if (GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return;
            if (slots == null)
                return;

            int i = 0;
            foreach (var id in SaveService.Data.placedItemIds)
            {
                if (i >= slots.Length)
                    break;
                var item = GameFlow.Instance.Catalog.GetItem(id);
                if (item == null)
                    continue;
                var sr = slots[i].GetComponent<SpriteRenderer>();
                if (sr == null)
                    sr = slots[i].gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = item.placedSprite != null ? item.placedSprite : FallbackSprite.WhiteCircle();
                sr.color = new Color(0.82f, 0.70f, 0.58f);
                sr.sortingOrder = 3;
                i++;
            }
        }
    }
}
