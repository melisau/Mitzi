using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;

namespace PawPath.UI
{
    /// <summary>
    /// Bölüm bittiğinde açılan tebrik ve ödül ekranı.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        [Header("UI Elemanları")]
        [SerializeField] GameObject panel;
        [SerializeField] Text levelTitleText;
        [SerializeField] Text rewardText;
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button homeButton;

        [Header("Kilit Açılma Duyurusu (Her 5 Seviyede Bir)")]
        [SerializeField] GameObject newCatUnlockedPanel;
        [SerializeField] Text newCatNameText;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (newCatUnlockedPanel != null) newCatUnlockedPanel.SetActive(false);

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeClicked);
        }

        void OnEnable()
        {
            // Level bitti olayını dinle
            GameEvents.OnLevelCompleted += HandleLevelCompleted;
        }

        void OnDisable()
        {
            GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        }

        private void HandleLevelCompleted()
        {
            ShowWinPanel(1, null);
        }

        public void ShowWinPanel(int completedLevelIndex, CatDefinition unlockedCat)
        {
            if (panel == null) return;

            panel.SetActive(true);

            if (levelTitleText != null)
                levelTitleText.text = $"Bölüm Tamamlandı!";

            if (rewardText != null)
                rewardText.text = "+15 💖";

            // Eğer yeni kedi açıldıysa özel paneli göster
            if (unlockedCat != null && newCatUnlockedPanel != null)
            {
                newCatUnlockedPanel.SetActive(true);
                if (newCatNameText != null)
                    newCatNameText.text = $"{unlockedCat.displayName} Eve Davet Edildi!";
            }
            else if (newCatUnlockedPanel != null)
            {
                newCatUnlockedPanel.SetActive(false);
            }
        }

        void OnNextLevelClicked()
        {
            if (panel != null) panel.SetActive(false);

            // Sahnedeki LevelManager veya GameFlow bileşenine NextLevel çağrısı gönder
            var gameController = FindObjectOfType<MonoBehaviour>();
            if (gameController != null)
            {
                gameController.SendMessage("NextLevel", SendMessageOptions.DontRequireReceiver);
            }
        }

        void OnHomeClicked()
        {
            if (panel != null) panel.SetActive(false);

            // Sahneyi yeniden yükleyerek veya ana akışa dönerek Hub ekranını açar
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}