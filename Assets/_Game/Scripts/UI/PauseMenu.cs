using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Gameplay;

namespace PawPath.UI
{
    /// <summary>
    /// Bölüm içindeki tek duraklat (⏸) düğmesi ve açtığı menü. Yanlışlıkla
    /// dokunulunca bölümü bitiren köşe düğmeleri yerine kullanılır: önce oyun
    /// durur, sonra büyük ikonlarla devam / yeniden başla / eve dön seçilir.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        Button pauseButton;
        GameObject panel;

        public void Bind(Button pause, GameObject menuPanel, Button resume, Button restart, Button home)
        {
            pauseButton = pause;
            panel = menuPanel;
            if (panel != null)
                panel.SetActive(false);

            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(Open);
            resume.onClick.RemoveAllListeners();
            resume.onClick.AddListener(Resume);
            restart.onClick.RemoveAllListeners();
            restart.onClick.AddListener(() =>
            {
                Close();
                GameFlow.Instance?.RetryCurrentLevel();
            });
            home.onClick.RemoveAllListeners();
            home.onClick.AddListener(() =>
            {
                Close();
                GameFlow.Instance?.EnterHub();
            });
            Refresh();
        }

        void Open()
        {
            var flow = GameFlow.Instance;
            if (flow == null || flow.InHub)
                return;
            MobileControlState.Reset();
            flow.PauseGame();
            if (flow.State != GameFlowState.Pause)
                return;
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
        }

        void Resume()
        {
            Close();
            GameFlow.Instance?.ResumeGame();
        }

        void Close()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        void Update() => Refresh();

        void Refresh()
        {
            var flow = GameFlow.Instance;
            bool inLevel = flow != null && !flow.InHub &&
                flow.State != GameFlowState.Victory && flow.State != GameFlowState.GameOver &&
                flow.State != GameFlowState.Rescue;
            if (pauseButton != null && pauseButton.gameObject.activeSelf != inLevel)
                pauseButton.gameObject.SetActive(inLevel);
            // Oyun başka bir yoldan duraklamadan çıktıysa (ör. eve dönüş) menü kapanır.
            if (panel != null && panel.activeSelf && (flow == null || flow.State != GameFlowState.Pause))
                panel.SetActive(false);
        }
    }
}
