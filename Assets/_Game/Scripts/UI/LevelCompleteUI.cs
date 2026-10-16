using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Localization;

namespace PawPath.UI
{
    /// <summary>
    /// Bölüm bittiğinde açılan tebrik ve ödül ekranı.
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        [Header("UI Elemanları")]
        [SerializeField] Text levelTitleText;
        [SerializeField] Text rewardText;
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button homeButton;
        [SerializeField] Image celebrationImage;
        [SerializeField] Sprite[] celebrationFaces;

        void Awake()
        {
            WireButtons();
        }

        public void Present(int completedLevel, int reward)
        {
            gameObject.SetActive(true);

            if (levelTitleText != null)
                levelTitleText.text = GameText.LevelCompletedLabel(completedLevel);

            if (rewardText != null)
                rewardText.text = GameText.L($"Kazandın!  +{reward} Sevgi", $"You won!  +{reward} Love", $"Gewonnen!  +{reward} Liebe");

            if (celebrationImage != null)
            {
                // Öncelik: oyuncunun seçili kedisinin portresi (kendi renginde). Yoksa
                // katalogdaki kutlama görsellerinden biri. Hiçbiri yoksa boş beyaz kutu
                // göstermek yerine görsel gizlenir.
                Sprite face = null;
                var catalog = GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
                if (catalog != null && catalog.cats != null)
                {
                    string id = SaveService.Data != null ? SaveService.Data.selectedCatId : null;
                    var cat = catalog.cats.Find(c => c != null && c.id == id)
                              ?? catalog.cats.Find(c => c != null && c.isStarterCat);
                    if (cat != null)
                        face = cat.portrait != null ? cat.portrait : cat.idleSprite;
                }
                if (face == null && celebrationFaces != null && celebrationFaces.Length > 0)
                    face = celebrationFaces[Random.Range(0, celebrationFaces.Length)];
                celebrationImage.sprite = face;
                celebrationImage.enabled = face != null;
            }
        }

        void OnNextLevelClicked()
        {
            gameObject.SetActive(false);
            if (GameFlow.Instance != null)
                GameFlow.Instance.StartNextLevel();
        }

        void OnHomeClicked()
        {
            gameObject.SetActive(false);
            if (GameFlow.Instance != null)
                GameFlow.Instance.EnterHub();
        }

        public void Bind(Text title, Text reward, Button next, Button home, Image face, Sprite[] faces)
        {
            levelTitleText = title;
            rewardText = reward;
            nextLevelButton = next;
            homeButton = home;
            celebrationImage = face;
            celebrationFaces = faces;
            WireButtons();
        }

        void WireButtons()
        {
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            }
            if (homeButton != null)
            {
                homeButton.onClick.RemoveAllListeners();
                homeButton.onClick.AddListener(OnHomeClicked);
            }
        }
    }
}
