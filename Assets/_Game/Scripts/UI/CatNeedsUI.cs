using UnityEngine;
using UnityEngine.UI;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Economy;
using PawPath.Hub;
using PawPath.Audio;

namespace PawPath.UI
{
    /// <summary>
    /// Kedinin Mama, Su ve Uyku butonları ile günlük sevgi bütçesini gösteren UI ekranı.
    /// </summary>
    public class CatNeedsUI : MonoBehaviour
    {
        [Header("Referanslar")]
        [SerializeField] private CatNeedsSystem catNeedsSystem;

        [Header("UI Butonları")]
        [SerializeField] private Button feedButton;
        [SerializeField] private Button waterButton;
        [SerializeField] private Button sleepButton;

        [Header("Metin ve Göstergeler")]
        [SerializeField] private Text dailyPettingProgressText;
        [SerializeField] private Text energyWarningText;

        private void Awake()
        {
            if (catNeedsSystem == null)
                catNeedsSystem = CatNeedsSystem.Instance ?? FindObjectOfType<CatNeedsSystem>();
            WireButtons();
        }

        void OnEnable()
        {
            GameEvents.OnLovePointsChanged += OnLoveChanged;
            GameEvents.OnHubEntered += UpdateUI;
            GameEvents.OnLevelStarted += HideForLevel;
            UpdateUI();
        }

        void OnDisable()
        {
            GameEvents.OnLovePointsChanged -= OnLoveChanged;
            GameEvents.OnHubEntered -= UpdateUI;
            GameEvents.OnLevelStarted -= HideForLevel;
        }

        private void Start()
        {
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (catNeedsSystem == null) return;

            SetVisible(GameFlow.Instance == null || GameFlow.Instance.InHub);

            int currentDaily = PlayerPrefs.GetInt("DailyPettingPoints", 0);
            if (dailyPettingProgressText != null)
            {
                dailyPettingProgressText.text = $"Günlük Okşama: {currentDaily} / {catNeedsSystem.maxDailyPettingPoints}";
            }

            if (energyWarningText != null)
            {
                bool ready = catNeedsSystem.CanStartLevel();
                energyWarningText.gameObject.SetActive(true);
                energyWarningText.text = ready
                    ? "Kedin yola çıkmaya hazır"
                    : $"Yola çıkmak için {catNeedsSystem.requiredEnergyToPlay} Sevgi gerekli";
            }

            UpdateCareButton(feedButton, catNeedsSystem.CanFeedToday, $"Mama +{catNeedsSystem.foodPoints}");
            UpdateCareButton(waterButton, catNeedsSystem.CanGiveWaterToday, $"Su +{catNeedsSystem.waterPoints}");
            UpdateCareButton(sleepButton, catNeedsSystem.CanSleepToday, $"Uyu +{catNeedsSystem.sleepPoints}");
        }

        private void OnFeedClicked()
        {
            if (catNeedsSystem != null)
            {
                if (HubFurnitureView.Instance == null || !HubFurnitureView.Instance.HasPlacedBowl)
                {
                    ShowCareMessage("Önce dükkândan mama kabı satın al. Puan için kediyi sev veya bölüm geç.");
                    CozyAudioManager.Instance?.PlayError();
                    return;
                }
                bool fed = catNeedsSystem.FeedCat();
                if (fed && CatHouseInteraction.Instance != null)
                    CatHouseInteraction.Instance.SendSelectedToBowl();
                UpdateUI();
            }
        }

        private void OnWaterClicked()
        {
            if (catNeedsSystem != null)
            {
                if (HubFurnitureView.Instance == null || !HubFurnitureView.Instance.HasPlacedWater)
                {
                    ShowCareMessage("Önce dükkândan su kabı satın al. Puan için kediyi sev veya bölüm geç.");
                    CozyAudioManager.Instance?.PlayError();
                    return;
                }
                bool drank = catNeedsSystem.GiveWater();
                if (drank && CatHouseInteraction.Instance != null)
                    CatHouseInteraction.Instance.SendSelectedToWater();
                UpdateUI();
            }
        }

        private void OnSleepClicked()
        {
            if (catNeedsSystem != null)
            {
                catNeedsSystem.PutToSleep();
                UpdateUI();
            }
        }

        /// <summary>
        /// Yola Çık butonuna basıldığında çağrılır.
        /// </summary>
        public bool CheckEnergyAndStartLevel()
        {
            if (catNeedsSystem != null && !catNeedsSystem.CanStartLevel())
            {
                if (energyWarningText != null)
                {
                    energyWarningText.gameObject.SetActive(true);
                    energyWarningText.text = $"Yola çıkmak için en az {catNeedsSystem.requiredEnergyToPlay} Sevgi Puanı gerekli!";
                }
                CozyAudioManager.Instance?.PlayError();
                return false;
            }

            if (energyWarningText != null)
                energyWarningText.gameObject.SetActive(false);

            return true;
        }

        public void Bind(CatNeedsSystem needs, Button feed, Button water, Button sleep, Text daily, Text warning)
        {
            catNeedsSystem = needs;
            feedButton = feed;
            waterButton = water;
            sleepButton = sleep;
            dailyPettingProgressText = daily;
            energyWarningText = warning;
            WireButtons();
            UpdateUI();
        }

        void WireButtons()
        {
            if (feedButton != null)
            {
                feedButton.onClick.RemoveListener(OnFeedClicked);
                feedButton.onClick.AddListener(OnFeedClicked);
            }
            if (waterButton != null)
            {
                waterButton.onClick.RemoveListener(OnWaterClicked);
                waterButton.onClick.AddListener(OnWaterClicked);
            }
            if (sleepButton != null)
            {
                sleepButton.onClick.RemoveListener(OnSleepClicked);
                sleepButton.onClick.AddListener(OnSleepClicked);
            }
        }

        void ShowCareMessage(string message)
        {
            if (energyWarningText == null)
                return;
            energyWarningText.gameObject.SetActive(true);
            energyWarningText.text = message;
        }

        static void UpdateCareButton(Button button, bool available, string availableText)
        {
            if (button == null)
                return;

            button.gameObject.SetActive(available);
            if (!available)
                return;

            button.interactable = true;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = availableText;
        }

        void OnLoveChanged(int _) => UpdateUI();
        void HideForLevel() => SetVisible(false);

        void SetVisible(bool visible)
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
