using UnityEngine;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.Hub
{
    /// <summary>
    /// Satın alınan mobilyayı evde SABİT slot pozisyonlarına göre gösterir.
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

        public void Refresh()
        {
            if (GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return;
            if (slots == null || slots.Length == 0)
                return;

            // 1. Önce tüm slotlardaki görselleri temizle
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

            // 2. Her ürünü KENDİ SABİT SLOTUNA eşleştirerek yerleştir
            foreach (var id in SaveService.Data.placedItemIds)
            {
                var item = GameFlow.Instance.Catalog.GetItem(id);
                if (item == null)
                    continue;

                // Ürün ID'sine karşılık gelen doğru slotu bul
                Transform targetSlot = GetTargetSlotForItem(item.id);

                if (targetSlot != null)
                {
                    var sr = targetSlot.GetComponent<SpriteRenderer>();
                    if (sr == null)
                        sr = targetSlot.gameObject.AddComponent<SpriteRenderer>();

                    sr.sprite = item.placedSprite != null ? item.placedSprite : FallbackSprite.WhiteCircle();
                    sr.color = Color.white;
                    sr.sortingOrder = 3;
                }
            }
        }

        /// <summary>
        /// Ürünün id'sine (veya türüne) göre sahnedeki doğru slot Transform'unu döndürür.
        /// </summary>
        private Transform GetTargetSlotForItem(string itemId)
        {
            if (slots == null) return null;

            string searchKey = "";

            // Ürün ID'sine göre aranacak slot anahtar kelimesi
            if (itemId.Contains("bed"))
                searchKey = "bed";
            else if (itemId.Contains("rug"))
                searchKey = "rug";
            else if (itemId.Contains("bowl"))
                searchKey = "bowl";

            // 1. Önce isminde bu anahtar kelime geçen slotu ara (Örn: "Slot_bed", "Rug_Slot")
            if (!string.IsNullOrEmpty(searchKey))
            {
                foreach (var slot in slots)
                {
                    if (slot != null && slot.name.ToLower().Contains(searchKey))
                    {
                        return slot;
                    }
                }
            }

            // 2. Eğer özel isim bulunamazsa veya eşleşmezse sabit index eşleşmesi yap
            // slots[0] -> Bed, slots[1] -> Rug, slots[2] -> Bowl gibi
            if (itemId.Contains("bed") && slots.Length > 0) return slots[0];
            if (itemId.Contains("rug") && slots.Length > 1) return slots[1];
            if (itemId.Contains("bowl") && slots.Length > 2) return slots[2];

            return null;
        }
    }
}