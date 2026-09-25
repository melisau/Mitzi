using System;
using UnityEngine;
using PawPath.Content;
using PawPath.Cat;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Levels;
using PawPath.Audio;
using PawPath.Gameplay;


namespace PawPath.Core
{
    public enum GameFlowState
    {
        MainMenu,
        InfoOverlay,
        DrawingPreparation,
        Gameplay,
        Pause,
        Victory,
        GameOver,
        Rescue
    }

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
        public GameFlowState State { get; private set; } = GameFlowState.MainMenu;
        public bool InHub => State == GameFlowState.MainMenu;
        public bool GameplayPaused => State != GameFlowState.MainMenu && State != GameFlowState.Gameplay;
        public event Action<GameFlowState> StateChanged;

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
            TransitionTo(GameFlowState.MainMenu);
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
            TransitionTo(GameFlowState.Gameplay);
            SetRoots(hub: false, level: true, hud: true, rescue: false, complete: false, failure: false);
            if (LevelManager.Instance != null)
                LevelManager.Instance.BeginCurrentLevel();
        }

        public void ShowRescue(CatDefinition cat)
        {
            TransitionTo(GameFlowState.Rescue);
            SetRoots(hub: false, level: false, hud: false, rescue: true, complete: false, failure: false);
            var screen = rescueRoot != null ? rescueRoot.GetComponent<UI.RescueScreen>() : null;
            if (screen != null)
                screen.Present(cat);
        }

        public void ShowLevelComplete(int completedLevel, int reward)
        {
            TransitionTo(GameFlowState.Victory);
            SetRoots(hub: false, level: false, hud: false, rescue: false, complete: true, failure: false);
            var screen = levelCompleteRoot != null ? levelCompleteRoot.GetComponent<UI.LevelCompleteUI>() : null;
            if (screen != null)
                screen.Present(completedLevel, reward);
        }

        public void ShowLevelFailure(string reason)
        {
            TransitionTo(GameFlowState.GameOver);
            SetRoots(hub: false, level: false, hud: false, rescue: false, complete: false, failure: true);
            var screen = levelFailureRoot != null ? levelFailureRoot.GetComponent<UI.LevelFailureUI>() : null;
            screen?.Present(reason);
        }

        public void RetryCurrentLevel()
        {
            TransitionTo(GameFlowState.Gameplay);
            SetRoots(hub: false, level: true, hud: true, rescue: false, complete: false, failure: false);
            LevelManager.Instance?.BeginCurrentLevel();
        }

        public void SetGameplayPaused(bool paused)
        {
            if (InHub)
                return;
            TransitionTo(paused ? GameFlowState.Pause : DrawingStillInProgress()
                ? GameFlowState.DrawingPreparation : GameFlowState.Gameplay);
        }

        public void BeginDrawingPreparation()
        {
            if (!InHub)
                TransitionTo(GameFlowState.DrawingPreparation);
        }

        public void StartGameplayFromDrawing(bool finishEarly = false)
        {
            if (State != GameFlowState.DrawingPreparation ||
                !finishEarly && DrawingStillInProgress())
                return;
            if (LineDraw.Instance != null)
            {
                LineDraw.Instance.FinishCurrentStroke();
                LineDraw.Instance.CanDraw = false;
            }
            var cameraFollow = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera>() : null;
            cameraFollow?.ResetView();
            TransitionTo(GameFlowState.Gameplay);
        }

        public void ShowInfoOverlay() => TransitionTo(GameFlowState.InfoOverlay);

        public void DismissInfoOverlay()
        {
            if (State == GameFlowState.InfoOverlay)
                TransitionTo(DrawingStillInProgress()
                    ? GameFlowState.DrawingPreparation : GameFlowState.Gameplay);
        }

        static bool DrawingStillInProgress() => GameplayMode.IsDrawing &&
            LineDraw.Instance != null && LineDraw.Instance.InkLeft > 0.01f;

        void TransitionTo(GameFlowState next)
        {
            if (State == next)
            {
                ApplyTimeScale(next);
                return;
            }

            State = next;
            ApplyTimeScale(next);
            StateChanged?.Invoke(next);
        }

        static void ApplyTimeScale(GameFlowState state)
        {
            bool paused = state == GameFlowState.InfoOverlay ||
                state == GameFlowState.DrawingPreparation || state == GameFlowState.Pause ||
                state == GameFlowState.Victory || state == GameFlowState.GameOver ||
                state == GameFlowState.Rescue;
            Time.timeScale = paused ? 0f : 1f;
            if (paused && CatController.Instance != null)
                CatController.Instance.StopImmediately();
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
