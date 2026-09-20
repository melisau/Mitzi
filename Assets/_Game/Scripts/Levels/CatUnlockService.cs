using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;

namespace PawPath.Levels
{
    /// <summary>
    /// Her 5 tamamlanan bölümde henüz açılmamış bir kediyi koleksiyona yazar.
    /// </summary>
    public static class CatUnlockService
    {
        public const int Interval = 5;

        public static bool TryUnlockAfterLevel(int completedLevel, out CatDefinition unlocked)
        {
            unlocked = null;
            if (completedLevel <= 0 || completedLevel % Interval != 0)
                return false;

            var catalog = GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
            if (catalog == null)
                return false;

            foreach (var cat in catalog.cats)
            {
                if (cat == null)
                    continue;
                if (cat.unlockAfterLevel != completedLevel)
                    continue;
                if (SaveService.HasCat(cat.id))
                    continue;

                SaveService.UnlockCat(cat.id);
                if (CozyEconomyManager.Instance != null)
                    CozyEconomyManager.Instance.AddLove(40, "Yeni kedi keşfi");
                unlocked = cat;
                GameEvents.CatUnlocked(cat);
                return true;
            }

            return false;
        }
    }
}
