using UnityEngine;
using PawPath.Core;
using PawPath.UI;

namespace PawPath.Levels
{
    public enum DifficultyLevel { Easy, Medium, Hard }

    public static class GameDifficulty
    {
        const string PreferenceKey = "PawPath.Difficulty";
        const string AutomaticKey = "PawPath.Difficulty.Automatic";
        // Preserve an explicitly selected difficulty from older saves.
        public static bool IsAutomatic => PlayerPrefs.GetInt(AutomaticKey, PlayerPrefs.HasKey(PreferenceKey) ? 0 : 1) == 1;
        public static DifficultyLevel Selected => ForTheme(ThemeSelectionUI.GetSelectedTheme(SaveService.Data.highestCompletedLevel + 1));
        public static int CompletedInTheme(int theme) => SaveService.Data.themeCompletedLevels[Mathf.Clamp(theme,0,3)];
        public static DifficultyLevel ForTheme(int theme)
        {
            if (!IsAutomatic) return (DifficultyLevel)Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey,0),0,2);
            int completed = CompletedInTheme(theme);
            return completed < 10 ? DifficultyLevel.Easy : completed < 20 ? DifficultyLevel.Medium : DifficultyLevel.Hard;
        }
        public static void RecordCompletion(int theme)
        {
            var counts = SaveService.Data.themeCompletedLevels;
            theme = Mathf.Clamp(theme,0,3);
            if (counts[theme] < int.MaxValue) counts[theme]++;
        }
        public static void SelectAutomatic()
        {
            PlayerPrefs.SetInt(AutomaticKey,1);PlayerPrefs.Save();
        }
        public static void Select(DifficultyLevel difficulty)
        {
            PlayerPrefs.SetInt(PreferenceKey,Mathf.Clamp((int)difficulty,0,2));
            PlayerPrefs.SetInt(AutomaticKey,0);PlayerPrefs.Save();
        }
    }
}
