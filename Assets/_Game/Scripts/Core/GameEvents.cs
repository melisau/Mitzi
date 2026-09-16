using System;
using PawPath.Data;

namespace PawPath.Core
{
    /// <summary>
    /// Sahne ve sistemler arası gevşek bağlı iletişim. Inspector referansı zincirini küçültür.
    /// </summary>
    public static class GameEvents
    {
        public static event Action OnHubEntered;
        public static event Action OnLevelStarted;
        public static event Action OnLevelCompleted;
        public static event Action OnCatFell;
        public static event Action OnCatRescued;
        public static event Action<CatDefinition> OnCatUnlocked;
        public static event Action<int> OnLovePointsChanged;
        public static event Action<CatDefinition> OnPlayableCatChanged;
        public static event Action OnShopChanged;

        public static void HubEntered() => OnHubEntered?.Invoke();
        public static void LevelStarted() => OnLevelStarted?.Invoke();
        public static void LevelCompleted() => OnLevelCompleted?.Invoke();
        public static void CatFell() => OnCatFell?.Invoke();
        public static void CatRescued() => OnCatRescued?.Invoke();
        public static void CatUnlocked(CatDefinition cat) => OnCatUnlocked?.Invoke(cat);
        public static void LovePointsChanged(int total) => OnLovePointsChanged?.Invoke(total);
        public static void PlayableCatChanged(CatDefinition cat) => OnPlayableCatChanged?.Invoke(cat);
        public static void ShopChanged() => OnShopChanged?.Invoke();
    }
}
