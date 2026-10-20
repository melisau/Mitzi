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

        public static CatDefinition NextLockedCat()
        {
            var catalog=GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
            if(catalog==null)return null;
            CatDefinition next = null;
            foreach (var cat in catalog.cats)
                if (cat != null && cat.unlockAfterLevel > 0 && !SaveService.HasCat(cat.id) &&
                    (next == null || cat.unlockAfterLevel < next.unlockAfterLevel)) next = cat;
            return next;
        }

        public static CatDefinition WaitingAtLevel(int level)
        {
            var next = NextLockedCat();
            return level > 0 && next != null && next.unlockAfterLevel <= level ? next : null;
        }

        public static bool TryUnlockAfterLevel(int completedLevel, out CatDefinition unlocked)
        {
            unlocked = null;
            if (completedLevel <= 0)
                return false;

            var cat = WaitingAtLevel(completedLevel);
            if (cat == null)
                return false;
            SaveService.UnlockCat(cat.id);
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.AddShoppingPoints(10, "Yeni kedi keşfi");
            unlocked = cat;
            GameEvents.CatUnlocked(cat);
            return true;
        }
    }
}
