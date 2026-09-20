using System.Collections.Generic;
using UnityEngine;
using PawPath.Content;
using PawPath.Core;

namespace PawPath.Data
{
    /// <summary>
    /// Tüm kedi ve mağaza tanımlarını tek yerden sunar.
    /// Editor'de asset'leri ata; boşsa runtime'da yedek katalog üretilir.
    /// </summary>
    [CreateAssetMenu(menuName = "Paw Path/Catalog", fileName = "PawPathCatalog")]
    public class PawPathCatalog : ScriptableObject
    {
        public List<CatDefinition> cats = new List<CatDefinition>();
        public List<ShopItemDefinition> shopItems = new List<ShopItemDefinition>();
        public List<LevelDefinition> levels = new List<LevelDefinition>();
        [Header("Arka Planlar")]
        public Sprite homeBackground;
        public Sprite shopBackground;
        public Sprite gameplayBackground;
        public Sprite forestBackground;
        [Header("Bölüm Engelleri")]
        public Sprite roadGapSprite;
        public Sprite moundSprite;
        public Sprite roadPlatformSprite;
        public Sprite forestGapSprite;
        public Sprite forestMoundSprite;
        public Sprite forestPlatformSprite;
        [Header("Cadde Teması")]
        public Sprite cityBackground;
        public Sprite cityGapSprite;
        public Sprite cityPlatformSprite;
        public Sprite cityCarSprite;
        public Sprite cityVanSprite;
        public Sprite cityRoadSprite;
        public Sprite finishPortalSprite;
        [Header("Sesler")]
        public AudioClip homeMusic;
        public AudioClip streetMusic;
        public AudioClip forestMusic;
        public AudioClip cityMusic;
        public AudioClip catStepSfx;
        public AudioClip[] catMeowSfx;
        public AudioClip catHurtSfx;
        public AudioClip catFallSfx;
        public AudioClip catRescueSfx;
        public AudioClip errorSfx;
        public AudioClip uiClickSfx;
        public AudioClip uiConfirmSfx;
        public AudioClip shopBuySfx;
        public AudioClip shopSellSfx;
        [Header("Başarı Ekranı")]
        public Sprite[] completionFaces;
        public Sprite failureBackground;
        public Sprite[] birdFrames;

        public CatDefinition GetCat(string id)
        {
            if (cats == null)
                return null;
            return cats.Find(c => c != null && c.id == id);
        }

        public ShopItemDefinition GetItem(string id)
        {
            if (shopItems == null)
                return null;
            return shopItems.Find(i => i != null && i.id == id);
        }

        public CatDefinition NextLockedCat(int completedLevel)
        {
            CatDefinition best = null;
            foreach (var cat in cats)
            {
                if (cat == null || cat.unlockAfterLevel <= 0)
                    continue;
                if (cat.unlockAfterLevel > completedLevel)
                    continue;
                if (best == null || cat.unlockAfterLevel > best.unlockAfterLevel)
                    best = cat;
            }
            return best;
        }
    }
}
