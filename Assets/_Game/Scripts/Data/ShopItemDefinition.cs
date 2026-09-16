using UnityEngine;

namespace PawPath.Data
{
    public enum ShopItemType
    {
        Furniture,
        Outfit,
        Toy
    }

    [CreateAssetMenu(menuName = "Paw Path/Shop Item", fileName = "Item_")]
    public class ShopItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public ShopItemType type = ShopItemType.Furniture;
        public int lovePointCost = 40;
        public Sprite icon;
        public Sprite placedSprite;
    }
}
