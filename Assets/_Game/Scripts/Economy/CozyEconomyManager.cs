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

            if (!SaveService.Data.placedItemIds.Contains(item.id))
            {
                SaveService.Data.placedItemIds.Add(item.id);
            }

            SaveService.Persist();

            GameEvents.LovePointsChanged(LovePoints);
            GameEvents.ShopChanged();

            return true;
        }

        public bool Owns(string itemId) => SaveService.Data.ownedItemIds.Contains(itemId);

        public bool CanAfford(ShopItemDefinition item) =>
            item != null && !Owns(item.id) && LovePoints >= item.lovePointCost;
    }
}