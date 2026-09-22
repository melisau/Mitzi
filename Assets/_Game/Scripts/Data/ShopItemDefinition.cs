using UnityEngine;

namespace PawPath.Content
{
    public enum ItemInteractionType
    {
        None,
        Eating,
        Playing,
        Sleeping,
        Climbing,
        Toileting
    }

    public enum FurnitureSlotType
    {
        Rug,
        Bed,
        Bowl,
        Wallpaper,
        Poster,
        Water,
        Sand,
        Tree,
        Toy
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
        public ItemInteractionType interactionType;
        public AnimationClip interactionAnimation;
        public Sprite[] interactionFrames;
        [Tooltip("Birleşik etkileşim karelerinin yerleştirilmiş iteme göre görsel ölçeği.")]
        public float interactionVisualScale = 1f;

        public float EffectiveInteractionVisualScale => interactionVisualScale > 0.01f
            ? interactionVisualScale : 1f;
    }
}
