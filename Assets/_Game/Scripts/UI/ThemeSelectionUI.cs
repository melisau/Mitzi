using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using System;
using PawPath.Localization;

namespace PawPath.UI
{
    /// <summary>Açılmış mekanlar arasında oyuncunun seçim yapmasını sağlar.</summary>
    public class ThemeSelectionUI : MonoBehaviour
    {
        const string ThemeKey = "PawPath.SelectedTheme";
        [SerializeField] Button streetButton;
        [SerializeField] Button forestButton;
        [SerializeField] Button cityButton;
        [SerializeField] Button cyberButton;
        Action<int> onThemeChosen;
        bool requestedVisible;

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

        public void Bind(Button street, Button forest, Button city = null, Button cyber = null)
        {
            streetButton = street;
            forestButton = forest;
            cityButton = city;
            cyberButton = cyber;
            streetButton.onClick.AddListener(() => Select(0));
            forestButton.onClick.AddListener(() => Select(1));
            if (cityButton != null)
                cityButton.onClick.AddListener(() => Select(2));
            if (cyberButton != null)
                cyberButton.onClick.AddListener(() => Select(3));
            Refresh();
        }

        public void SetSelectionCallback(Action<int> callback) => onThemeChosen = callback;

        public void Show()
        {
            requestedVisible = true;
            Refresh();
        }

        void Select(int theme)
        {
            if(!TrySelectTheme(theme))return;
            Refresh();
            onThemeChosen?.Invoke(theme);
        }

        void Refresh()
        {
            bool inHub = GameFlow.Instance == null || GameFlow.Instance.InHub;
            SetVisible(inHub && requestedVisible);
            if (!inHub)
                return;

            int selected = GetSelectedTheme(SaveService.Data.highestCompletedLevel + 1);
            if (streetButton != null)
                SetLabel(streetButton, GameText.Checked(GameText.L("Sokak", "Street", "Straße"), selected == 0));
            if (forestButton != null)
            {
                UpdateChoice(forestButton,1,selected);
            }
            if (cityButton != null)
            {
                UpdateChoice(cityButton,2,selected);
            }
            if (cyberButton != null)
            {
                UpdateChoice(cyberButton,3,selected);
            }
        }

        public void Hide()
        {
            requestedVisible = false;
            SetVisible(false);
        }

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
            int chosen=PlayerPrefs.GetInt(ThemeKey,0);
            return IsUnlocked(chosen)?chosen:0;
        }
        public static int UnlockAfter(int theme)=>Mathf.Clamp(theme,0,3)*10;
        public static bool IsUnlocked(int theme)=>theme>=0&&theme<4&&SaveService.Data.highestCompletedLevel>=UnlockAfter(theme);
        public static string ThemeName(int theme)=>theme==1?GameText.L("Orman","Forest","Wald"):theme==2?GameText.L("Cadde","City","Stadt"):theme==3?GameText.L("Neo Teknoloji","Neo Tech","Neo-Tech"):GameText.L("Sokak","Street","Straße");
        public static bool TrySelectTheme(int theme)
        {
            if(!IsUnlocked(theme))return false;
            PlayerPrefs.SetInt(ThemeKey,theme);PlayerPrefs.Save();return true;
        }
        static void UpdateChoice(Button button,int theme,int selected)
        {
            bool unlocked=IsUnlocked(theme);button.interactable=unlocked;
            SetLabel(button,unlocked?GameText.Checked(ThemeName(theme),selected==theme):ThemeName(theme)+GameText.L($" · Bölüm {UnlockAfter(theme)}",$" · Level {UnlockAfter(theme)}",$" · Level {UnlockAfter(theme)}"));
        }
    }
}
