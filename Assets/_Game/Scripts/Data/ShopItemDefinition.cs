using UnityEngine;

namespace PawPath.Content
{
    public enum FurnitureSlotType
    {
        Rug,
        Bed,
        Bowl,
        Wallpaper,
        Poster,
        Water,
        Sand,
        Tree
    }

    [CreateAssetMenu(fileName = "ShopItem", menuName = "PawPath/Shop Item")]
    public class ShopItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string description;
        public int lovePointCost;
        public Sprite icon;
        public Sprite placedSprite;
        public FurnitureSlotType slotType;
    }
}
