using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;

namespace PawPath.Levels
{
    /// <summary>
    /// Belirlenen kurtarma bölümlerinde henüz açılmamış bir kediyi koleksiyona yazar.
    /// </summary>
    public static class CatUnlockService
    {

        public static CatDefinition WaitingAtLevel(int level)
        {
            if(level<=0)return null;
            var catalog=GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
            if(catalog==null)return null;
            return catalog.cats.Find(cat=>cat!=null && cat.unlockAfterLevel>0 && cat.unlockAfterLevel<=level && !SaveService.HasCat(cat.id));
        }

        public static bool TryUnlockAfterLevel(int completedLevel, out CatDefinition unlocked)
        {
            unlocked = null;
            if (completedLevel <= 0)
                return false;

            var catalog = GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
            if (catalog == null)
                return false;

            foreach (var cat in catalog.cats)
            {
                if (cat == null)
                    continue;
                if (cat.unlockAfterLevel <= 0 || cat.unlockAfterLevel > completedLevel)
                    continue;
                if (SaveService.HasCat(cat.id))
                    continue;

                SaveService.UnlockCat(cat.id);
                if (CozyEconomyManager.Instance != null)
                    CozyEconomyManager.Instance.AddShoppingPoints(10, "Yeni kedi keşfi");
                unlocked = cat;
                GameEvents.CatUnlocked(cat);
                return true;
            }

            return false;
        }
    }
}
