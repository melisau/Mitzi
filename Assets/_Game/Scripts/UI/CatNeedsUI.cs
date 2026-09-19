using UnityEngine;
using UnityEngine.UI;
using PawPath.Cat;
using PawPath.Economy;

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
                catNeedsSystem = FindObjectOfType<CatNeedsSystem>();

            if (feedButton != null)
                feedButton.onClick.AddListener(OnFeedClicked);

            if (waterButton != null)
                waterButton.onClick.AddListener(OnWaterClicked);

            if (sleepButton != null)
                sleepButton.onClick.AddListener(OnSleepClicked);
        }

        private void Start()
        {
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (catNeedsSystem == null) return;

            int currentDaily = PlayerPrefs.GetInt("DailyPettingPoints", 0);
            if (dailyPettingProgressText != null)
            {
                dailyPettingProgressText.text = $"Günlük Okşama: {currentDaily} / {catNeedsSystem.maxDailyPettingPoints}";
            }

            if (energyWarningText != null)
            {
                energyWarningText.gameObject.SetActive(false);
            }
        }

        private void OnFeedClicked()
        {
            if (catNeedsSystem != null)
            {
                catNeedsSystem.FeedCat();
                UpdateUI();
            }
        }

        private void OnWaterClicked()
        {
            if (catNeedsSystem != null)
            {
                catNeedsSystem.GiveWater();
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
                return false;
            }

            if (energyWarningText != null)
                energyWarningText.gameObject.SetActive(false);

            return true;
        }
    }
}