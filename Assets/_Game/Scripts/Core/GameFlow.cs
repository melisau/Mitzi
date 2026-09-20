using UnityEngine;
using PawPath.Content;
using PawPath.Cat;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Levels;
using PawPath.Audio;


namespace PawPath.Core
{
    /// <summary>
    /// Tek sahneli akış: Hub <-> Seviye. DontDestroyOnLoad gerekmez; sahne kurucu bunu kök nesneye koyar.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] PawPathCatalog catalog;
        [SerializeField] GameObject hubRoot;
        [SerializeField] GameObject levelRoot;
        [SerializeField] GameObject hudRoot;
        [SerializeField] GameObject rescueRoot;
        [SerializeField] GameObject levelCompleteRoot;
        [SerializeField] GameObject levelFailureRoot;

        public PawPathCatalog Catalog => catalog;
        public bool InHub { get; private set; } = true;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SaveService.Load();
            if (catalog == null)
                catalog = CatalogFactory.CreateRuntime();
        }

        void Start()
        {
            EnterHub();
        }

        public void EnterHub()
        {
            InHub = true;
            var cameraFollow = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera>() : null;
            if (cameraFollow != null)
                cameraFollow.ResetView();
            if (LineDraw.Instance != null)
            {
                LineDraw.Instance.CanDraw = false;
                LineDraw.Instance.ClearStrokes();
            }
            SetRoots(hub: true, level: false, hud: true, rescue: false, complete: false, failure: false);
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayHomeMusic();
            GameEvents.HubEntered();
        }

        public void StartNextLevel()
        {
            // Başlatma hangi düğmeden gelirse gelsin enerji kuralı atlanamaz.
            if (CatNeedsSystem.Instance != null && !CatNeedsSystem.Instance.CanStartLevel())
            {
                EnterHub();
                return;
            }
            InHub = false;
            SetRoots(hub: false, level: true, hud: true, rescue: false, complete: false, failure: false);
            if (LevelManager.Instance != null)
                LevelManager.Instance.BeginCurrentLevel();
        }

        public void ShowRescue(CatDefinition cat)
        {
            SetRoots(hub: false, level: false, hud: false, rescue: true, complete: false, failure: false);
            var screen = rescueRoot != null ? rescueRoot.GetComponent<UI.RescueScreen>() : null;
            if (screen != null)
                screen.Present(cat);
        }

        public void ShowLevelComplete(int completedLevel, int reward)
        {
            SetRoots(hub: false, level: false, hud: false, rescue: false, complete: true, failure: false);
            var screen = levelCompleteRoot != null ? levelCompleteRoot.GetComponent<UI.LevelCompleteUI>() : null;
            if (screen != null)
                screen.Present(completedLevel, reward);
        }

        public void ShowLevelFailure(string reason)
        {
            SetRoots(hub: false, level: false, hud: false, rescue: false, complete: false, failure: true);
            var screen = levelFailureRoot != null ? levelFailureRoot.GetComponent<UI.LevelFailureUI>() : null;
            screen?.Present(reason);
        }

        public void RetryCurrentLevel()
        {
            InHub = false;
            SetRoots(hub: false, level: true, hud: true, rescue: false, complete: false, failure: false);
            LevelManager.Instance?.BeginCurrentLevel();
        }

        public void BindCatalog(PawPathCatalog value) => catalog = value;

        public void BindRoots(GameObject hub, GameObject level, GameObject hud, GameObject rescue,
            GameObject complete, GameObject failure)
        {
            hubRoot = hub;
            levelRoot = level;
            hudRoot = hud;
            rescueRoot = rescue;
            levelCompleteRoot = complete;
            levelFailureRoot = failure;
        }

        void SetRoots(bool hub, bool level, bool hud, bool rescue, bool complete, bool failure)
        {
            if (hubRoot) hubRoot.SetActive(hub);
            if (levelRoot) levelRoot.SetActive(level);
            if (hudRoot) hudRoot.SetActive(hud);
            if (rescueRoot) rescueRoot.SetActive(rescue);
            if (levelCompleteRoot) levelCompleteRoot.SetActive(complete);
            if (levelFailureRoot) levelFailureRoot.SetActive(failure);
        }
    }
}
