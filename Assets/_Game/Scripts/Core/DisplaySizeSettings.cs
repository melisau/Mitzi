using UnityEngine;

namespace PawPath.Core
{
    public static class DisplaySizeSettings
    {
        const string PreferenceKey = "PawPath.LargeGameplayDisplay";

        public static bool Large => PlayerPrefs.GetInt(PreferenceKey, 1) == 1;
        public static float CatScale => Large ? 1.2f : 1f;

        public static void SetLarge(bool large)
        {
            PlayerPrefs.SetInt(PreferenceKey, large ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
