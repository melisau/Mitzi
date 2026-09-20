using UnityEngine;

namespace PawPath.Gameplay
{
    public enum GameplayPlayMode { Drawing, DirectControl }

    public static class GameplayMode
    {
        public static GameplayPlayMode Current { get; private set; } = GameplayPlayMode.Drawing;
        public static bool IsDrawing => Current == GameplayPlayMode.Drawing;

        public static void Select(GameplayPlayMode mode)
        {
            Current = mode;
            PlayerPrefs.SetInt("PawPath.PlayMode", (int)mode);
            PlayerPrefs.Save();
            MobileControlState.Reset();
        }
    }
}
