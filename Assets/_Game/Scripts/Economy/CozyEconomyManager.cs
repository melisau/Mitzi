using UnityEngine;
using PawPath.Core;
using PawPath.Data;
using PawPath.Content;

namespace PawPath.Economy
{
    public class CozyEconomyManager : MonoBehaviour
    {
        public static CozyEconomyManager Instance { get; private set; }

        // Legacy save field retained to preserve existing balances. Cat affection
        // is a separate per-cat need and is never purchased or rewarded by ads.
        public int ShoppingPoints => SaveService.Data.lovePoints;
        public int LovePoints => ShoppingPoints;

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

        public void AddLove(int amount, string reason = null) => AddShoppingPoints(amount, reason);

        public void AddShoppingPoints(int amount, string reason = null)
        {
            if (amount <= 0)
                return;

            SaveService.Data.lovePoints += amount;
            SaveService.Persist();
            GameEvents.LovePointsChanged(LovePoints);

            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[PawPath] +{amount} Alışveriş puanı · {reason}");
        }

        public void RemoveLove(int amount, string reason = null)
        {
            if (amount <= 0)
                return;

            SaveService.Data.lovePoints = Mathf.Max(0, SaveService.Data.lovePoints - amount);
            SaveService.Persist();
            GameEvents.LovePointsChanged(LovePoints);

            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[PawPath] -{amount} Alışveriş puanı · {reason}");
        }

        public int OwnedCount(string itemId) => SaveService.Data.furniture.FindAll(f => f.itemId == itemId).Count;
        public int StoredCount(string itemId) => SaveService.Data.furniture.FindAll(f => f.itemId == itemId && !f.placed).Count;
        public int PlacedCount(string itemId, int room) => SaveService.Data.furniture.FindAll(f => f.itemId == itemId && f.placed && f.roomId == room).Count;
        public bool Owns(string itemId) => OwnedCount(itemId) > 0;
        public int PurchaseCount(string itemId) => SaveService.Data.itemPurchases.Find(p => p.itemId == itemId)?.count ?? 0;

        public long GetPurchasePrice(ShopItemDefinition item)
        {
            if (item == null) return long.MaxValue;
            long price = Mathf.Max(0, item.lovePointCost);
            if (price == 0) return 0;
            for (int i = 0; i < PurchaseCount(item.id); i++)
            {
                if (price > long.MaxValue / 3) return long.MaxValue;
                price *= 3;
            }
            return price;
        }

        public bool CanAfford(ShopItemDefinition item) => item != null && ShoppingPoints >= GetPurchasePrice(item);

        public bool TryBuy(ShopItemDefinition item, int room = -1)
        {
            if (!CanAfford(item)) return false;
            SaveService.Data.lovePoints -= (int)GetPurchasePrice(item);
            var history = SaveService.Data.itemPurchases.Find(p => p.itemId == item.id);
            if (history == null) { history = new SaveService.ItemPurchaseCount { itemId = item.id }; SaveService.Data.itemPurchases.Add(history); }
            if (history.count < int.MaxValue) history.count++;
            var copy = new SaveService.FurnitureCopy { instanceId = System.Guid.NewGuid().ToString("N"), itemId = item.id };
            SaveService.Data.furniture.Add(copy);
            PlaceCopy(copy, item, room);
            SaveService.Persist();
            GameEvents.LovePointsChanged(LovePoints);
            GameEvents.ShopChanged();
            return true;
        }

        public bool Place(ShopItemDefinition item, int room = -1)
        {
            if (item == null) return false;
            var copy = SaveService.Data.furniture.Find(f => f.itemId == item.id && !f.placed);
            if (copy == null) return false;
            PlaceCopy(copy, item, room);
            SaveService.Persist();
            GameEvents.ShopChanged();
            return true;
        }

        void PlaceCopy(SaveService.FurnitureCopy copy, ShopItemDefinition item, int targetRoom = -1)
        {
            int room = targetRoom < 0 ? SaveService.Data.activeHomeRoom : Mathf.Clamp(targetRoom,0,1);
            // A wall has one covering, while every physical item supports copies.
            if (item.slotType == FurnitureSlotType.Wallpaper && GameFlow.Instance != null)
                foreach (var other in SaveService.Data.furniture)
                {
                    var definition = GameFlow.Instance.Catalog.GetItem(other.itemId);
                    if (other.placed && other.roomId == room && definition != null && definition.slotType == FurnitureSlotType.Wallpaper)
                        other.placed = false;
                }
            copy.roomId = room;
            copy.placed = true;
            copy.hasPosition = false;
            copy.legacyPlacement = false;
        }

        public bool Store(string instanceId)
        {
            var copy = SaveService.Data.furniture.Find(f => f.instanceId == instanceId);
            if (copy == null || !copy.placed) return false;
            copy.placed = false;
            SaveService.Persist();
            GameEvents.ShopChanged();
            return true;
        }

        public bool SellOne(ShopItemDefinition item)
        {
            if (item == null) return false;
            var copy = SaveService.Data.furniture.Find(f => f.itemId == item.id && !f.placed)
                ?? SaveService.Data.furniture.Find(f => f.itemId == item.id && f.roomId == SaveService.Data.activeHomeRoom)
                ?? SaveService.Data.furniture.Find(f => f.itemId == item.id);
            if (copy == null) return false;
            SaveService.Data.furniture.Remove(copy);
            SaveService.Data.lovePoints += Mathf.CeilToInt(Mathf.Max(0, item.lovePointCost) * 0.5f);
            SaveService.Persist();
            GameEvents.LovePointsChanged(LovePoints);
            GameEvents.ShopChanged();
            return true;
        }

        public bool IsPlaced(ShopItemDefinition item) => item != null && PlacedCount(item.id, SaveService.Data.activeHomeRoom) > 0;

        public bool MoveToRoom(string instanceId, int room)
        {
            var copy = SaveService.Data.furniture.Find(f => f.instanceId == instanceId);
            if (copy == null || !copy.placed) return false;
            copy.roomId = Mathf.Clamp(room, 0, 1); copy.hasPosition = false; copy.legacyPlacement = false;
            SaveService.Persist(); GameEvents.ShopChanged(); return true;
        }
    }
}
