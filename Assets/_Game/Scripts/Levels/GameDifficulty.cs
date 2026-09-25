using UnityEngine;

namespace PawPath.Levels
{
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    public static class GameDifficulty
    {
        const string PreferenceKey = "PawPath.Difficulty";

        // Yeni oyuncular için kolay başlar; mevcut Orta ayarı seçildiğinde eski denge korunur.
        public static DifficultyLevel Selected =>
            (DifficultyLevel)Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, 2);

        public static void Select(DifficultyLevel difficulty)
        {
            PlayerPrefs.SetInt(PreferenceKey, Mathf.Clamp((int)difficulty, 0, 2));
            PlayerPrefs.Save();
        }
    }
}
