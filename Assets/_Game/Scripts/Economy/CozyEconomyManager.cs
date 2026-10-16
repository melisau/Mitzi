using UnityEngine;
using PawPath.Core;
using PawPath.Data;
using PawPath.Content;

namespace PawPath.Economy
{
    public class CozyEconomyManager : MonoBehaviour
    {
        public static CozyEconomyManager Instance { get; private set; }

        public int LovePoints => SaveService.Data.lovePoints;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            GameEvents.LovePointsChanged(LovePoints);
            GameEvents.ShopChanged();
        }

        public void AddLove(int amount, string reason = null)
        {
            if (amount <= 0)
                return;

            SaveService.Data.lovePoints += amount;
            SaveService.Persist();
            GameEvents.LovePointsChanged(LovePoints);

            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[PawPath] +{amount} Sevgi · {reason}");
        }

        public void RemoveLove(int amount, string reason = null)
        {
            if (amount <= 0)
                return;

            SaveService.Data.lovePoints = Mathf.Max(0, SaveService.Data.lovePoints - amount);
            SaveService.Persist();
            GameEvents.LovePointsChanged(LovePoints);

            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[PawPath] -{amount} Sevgi · {reason}");
        }

        public bool TryBuy(ShopItemDefinition item)
        {
            if (item == null)
                return false;
            if (Owns(item.id))
                return false;
            if (LovePoints < item.lovePointCost)
                return false;

            SaveService.Data.lovePoints -= item.lovePointCost;
            SaveService.Data.ownedItemIds.Add(item.id);

            // Yeni alınan eşya hemen evde kullanılır; aynı yerdeki eski eşya depoya kalkar.
            PlaceInternal(item);

            SaveService.Persist();

            GameEvents.LovePointsChanged(LovePoints);
            GameEvents.ShopChanged();

            return true;
        }

        /// <summary>
        /// Sahip olunan bir eşyayı evde kullanır. Her yerde (yatak, halı, duvar…) aynı anda
        /// tek eşya bulunur; o yerdeki önceki eşya satılmaz, sahip olunanlarda kalır.
        /// </summary>
        public bool Place(ShopItemDefinition item)
        {
            if (item == null || !Owns(item.id))
                return false;
            PlaceInternal(item);
            SaveService.Persist();
            GameEvents.ShopChanged();
            return true;
        }

        public bool IsPlaced(ShopItemDefinition item) =>
            item != null && SaveService.Data.placedItemIds.Contains(item.id);

        void PlaceInternal(ShopItemDefinition item)
        {
            var placed = SaveService.Data.placedItemIds;
            var catalog = GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
            if (catalog != null)
            {
                placed.RemoveAll(id =>
                {
                    if (id == item.id)
                        return false;
                    var other = catalog.GetItem(id);
                    return other != null && other.slotType == item.slotType;
                });
            }
            if (!placed.Contains(item.id))
                placed.Add(item.id);
        }

        public bool Owns(string itemId) => SaveService.Data.ownedItemIds.Contains(itemId);

        public bool CanAfford(ShopItemDefinition item) =>
            item != null && !Owns(item.id) && LovePoints >= item.lovePointCost;
    }
}
