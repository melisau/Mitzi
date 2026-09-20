using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;

namespace PawPath.UI
{
    public class LevelFailureUI : MonoBehaviour
    {
        [SerializeField] Text reasonText;
        [SerializeField] Button retryButton;
        [SerializeField] Button homeButton;

        public void Bind(Text reason, Button retry, Button home)
        {
            reasonText = reason;
            retryButton = retry;
            homeButton = home;
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => GameFlow.Instance?.RetryCurrentLevel());
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(() => GameFlow.Instance?.EnterHub());
        }

        public void Present(string reason)
        {
            gameObject.SetActive(true);
            if (reasonText != null)
                reasonText.text = reason;
        }
    }
}
