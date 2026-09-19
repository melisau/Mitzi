using UnityEngine;

namespace PawPath.Content
{
    public enum FurnitureSlotType
    {
        Bowl,   // Mama Kabı
        Bed,    // Kedi Yatağı
        Rug,    // Kilim / Halı
        Toy     // Oyuncak vs.
    }

    [CreateAssetMenu(fileName = "ShopItem", menuName = "Paw Path/Shop Item")]
    public class ShopItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string description;
        public int lovePointCost;
        public Sprite placedSprite;

        [Header("Slot Yapılandırması")]
        public FurnitureSlotType slotType; // Eşyanın ait olduğu sabit slot türü
    }
}