using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;

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
                levelTitleText.text = $"Bölüm {completedLevel} Tamamlandı!";

            if (rewardText != null)
                rewardText.text = $"Kazandın!  +{reward} Sevgi";

            if (celebrationImage != null && celebrationFaces != null && celebrationFaces.Length > 0)
            {
                celebrationImage.sprite = celebrationFaces[Random.Range(0, celebrationFaces.Length)];
                celebrationImage.enabled = celebrationImage.sprite != null;
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
