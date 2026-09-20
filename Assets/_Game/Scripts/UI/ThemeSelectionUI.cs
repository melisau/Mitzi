using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;

namespace PawPath.UI
{
    /// <summary>Açılmış mekanlar arasında oyuncunun seçim yapmasını sağlar.</summary>
    public class ThemeSelectionUI : MonoBehaviour
    {
        const string ThemeKey = "PawPath.SelectedTheme";
        [SerializeField] Button streetButton;
        [SerializeField] Button forestButton;

        void OnEnable()
        {
            GameEvents.OnHubEntered += Refresh;
            GameEvents.OnLevelStarted += Hide;
            Refresh();
        }

        void OnDisable()
        {
            GameEvents.OnHubEntered -= Refresh;
            GameEvents.OnLevelStarted -= Hide;
        }

        public void Bind(Button street, Button forest)
        {
            streetButton = street;
            forestButton = forest;
            streetButton.onClick.AddListener(() => Select(0));
            forestButton.onClick.AddListener(() => Select(1));
            Refresh();
        }

        void Select(int theme)
        {
            if (theme == 1 && SaveService.Data.highestCompletedLevel < 5)
                return;
            PlayerPrefs.SetInt(ThemeKey, theme);
            PlayerPrefs.Save();
            Refresh();
        }

        void Refresh()
        {
            bool inHub = GameFlow.Instance == null || GameFlow.Instance.InHub;
            SetVisible(inHub);
            if (!inHub)
                return;

            int selected = GetSelectedTheme(SaveService.Data.highestCompletedLevel + 1);
            if (streetButton != null)
                SetLabel(streetButton, selected == 0 ? "✓ Sokak" : "Sokak");
            if (forestButton != null)
            {
                bool unlocked = SaveService.Data.highestCompletedLevel >= 5;
                forestButton.interactable = unlocked;
                SetLabel(forestButton, !unlocked ? "Orman (5. bölüm)" : selected == 1 ? "✓ Orman" : "Orman");
            }
        }

        void Hide() => SetVisible(false);

        void SetVisible(bool visible)
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        static void SetLabel(Button button, string value)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = value;
        }

        public static int GetSelectedTheme(int levelNumber)
        {
            if (PlayerPrefs.HasKey(ThemeKey))
            {
                int chosen = PlayerPrefs.GetInt(ThemeKey, 0);
                return chosen == 1 && SaveService.Data.highestCompletedLevel >= 5 ? 1 : 0;
            }
            return ((Mathf.Max(1, levelNumber) - 1) / 5) % 2;
        }
    }
}
