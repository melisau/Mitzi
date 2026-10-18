using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Cat;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Economy;
using PawPath.Levels;
using PawPath.Localization;
using PawPath.Gameplay;
using PawPath.Hub;

namespace PawPath.UI
{
    /// <summary>
    /// Hub ve bölüm HUD metinlerini bağlar. Sahne kurucu düğmeleri üretir.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private Text loveLabel;
        [Tooltip("Para ikonu + sayıdan oluşan bakiye sayacının kök nesnesi.")]
        [SerializeField] private GameObject balanceRoot;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text inkLabel;
        [SerializeField] private Text selectedCatLabel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button directPlayButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button quitGameButton;
        [SerializeField] private CatNeedsUI needsUI;
        [SerializeField] private GameObject brushToolbar;
        [SerializeField] private GameObject brushTutorial;
        [SerializeField] private GameObject mobileControls;
        [SerializeField] private GameObject playModeMenu;
        [SerializeField] private Button drawingPlayButton;
        [SerializeField] private Button editHomeButton;
        [SerializeField] private Button flipFurnitureButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private ThemeSelectionUI themeSelection;
        [SerializeField] private Button previousDrawViewButton;
        [SerializeField] private Button nextDrawViewButton;
        [SerializeField] private Button startDrawingGameButton;
        public static readonly Color EditIdleColor = new Color(0.66f, 0.49f, 0.37f, 1f);
        public static readonly Color EditActiveColor = new Color(0.98f, 0.76f, 0.30f, 1f);
        GameplayPlayMode pendingMode;
        Coroutine selectedNameRoutine;

        private void Awake()
        {
            // Inspector üzerinde boşsa alt objelerden butonları otomatik bul
            if (playButton == null)
            {
                Transform playTransform = transform.Find("HUD/PlayButton") ?? transform.Find("PlayButton");
                if (playTransform != null)
                    playButton = playTransform.GetComponent<Button>();
            }

            if (shopButton == null)
            {
                Transform shopTransform = transform.Find("HUD/ShopButton") ?? transform.Find("ShopButton");
                if (shopTransform != null)
                    shopButton = shopTransform.GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnLovePointsChanged += RefreshLove;
            GameEvents.OnPlayableCatChanged += RefreshCat;
            GameEvents.OnLevelStarted += OnLevelStarted;
            GameEvents.OnHubEntered += OnHubEntered;

            WireButtons();
        }

        private void OnDisable()
        {
            GameEvents.OnLovePointsChanged -= RefreshLove;
            GameEvents.OnPlayableCatChanged -= RefreshCat;
            GameEvents.OnLevelStarted -= OnLevelStarted;
            GameEvents.OnHubEntered -= OnHubEntered;
        }

        // TEK VE TEMİZ ONPLAYBUTTONCLICKED METODU
        private void OnPlayButtonClicked()
        {
            if (playModeMenu != null)
                playModeMenu.SetActive(!playModeMenu.activeSelf);
            if (themeSelection != null)
                themeSelection.Hide();
        }

        private void OnDrawingPlayButtonClicked() => ChooseMode(GameplayPlayMode.Drawing);

        private void OnDirectPlayButtonClicked()
        {
            ChooseMode(GameplayPlayMode.DirectControl);
        }

        private void ChooseMode(GameplayPlayMode mode)
        {
            if (needsUI != null && !needsUI.CheckEnergyAndStartLevel())
                return;
            if (needsUI == null && CatNeedsSystem.Instance != null && !CatNeedsSystem.Instance.CanStartLevel())
                return;

            pendingMode = mode;
            if (playModeMenu != null)
                playModeMenu.SetActive(false);
            if (themeSelection != null)
            {
                themeSelection.Show();
                return;
            }
            StartSelectedMode();
        }

        private void StartSelectedMode()
        {
            GameplayMode.Select(pendingMode);
            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.StartNextLevel();
            }
            else
            {
                Debug.LogError("HATA: GameFlow.Instance sahnede bulunamadı! RuntimeBootstrap nesnesinin aktif olduğundan emin olun.");
            }
        }

        private void OnLevelStarted()
        {
            RefreshLevel();
            HideSelectedCatName();
            SetBalanceVisible(false);
            if (playButton != null)
                playButton.gameObject.SetActive(false);
            if (shopButton != null)
                shopButton.gameObject.SetActive(false);
            HomeEditMode.Close();
            if (editHomeButton != null)
                editHomeButton.gameObject.SetActive(false);
            if (flipFurnitureButton != null)
                flipFurnitureButton.gameObject.SetActive(false);
            if (settingsButton != null)
                settingsButton.gameObject.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            if (playModeMenu != null)
                playModeMenu.SetActive(false);
            if (themeSelection != null)
                themeSelection.Hide();
            if (brushToolbar != null)
                brushToolbar.SetActive(GameplayMode.IsDrawing);
            RefreshDrawingPreparationButtons();
            if (mobileControls != null)
            {
                bool drawing = GameplayMode.IsDrawing;
                foreach (Transform control in mobileControls.transform)
                    control.gameObject.SetActive(!drawing || control.name == "Jump");
                mobileControls.SetActive(!drawing);
            }
            if (inkLabel != null)
                inkLabel.gameObject.SetActive(GameplayMode.IsDrawing);
            if (restartButton != null)
                restartButton.gameObject.SetActive(true);
            if (homeButton != null)
                homeButton.gameObject.SetActive(true);
            if (quitGameButton != null)
                quitGameButton.gameObject.SetActive(true);
            bool tutorialShown = ShowGameplayTutorialOnce();
            if (!tutorialShown)
            {
                if (GameplayMode.IsDrawing)
                    GameFlow.Instance?.BeginDrawingPreparation();
                else
                    GameFlow.Instance?.SetGameplayPaused(false);
            }
        }

        private void OnHubEntered()
        {
            HideSelectedCatName();
            SetBalanceVisible(true);
            if (loveLabel != null)
                RefreshLove(SaveService.Data.lovePoints);
            if (playButton != null)
                playButton.gameObject.SetActive(true);
            if (shopButton != null)
                shopButton.gameObject.SetActive(true);
            if (editHomeButton != null)
            {
                editHomeButton.gameObject.SetActive(true);
                SetEditButtonLabel(false);
            }
            if (flipFurnitureButton != null)
                flipFurnitureButton.gameObject.SetActive(false);
            if (settingsButton != null)
                settingsButton.gameObject.SetActive(true);
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
            // Bölüm başlarken bu alt buton kapatılıyor. Eve dönüldüğünde menü
            // yeniden açılmadan önce tekrar etkinleştirilmezse yalnızca çizim
            // seçeneği görünüyordu.
            if (directPlayButton != null)
                directPlayButton.gameObject.SetActive(true);
            if (drawingPlayButton != null)
                drawingPlayButton.gameObject.SetActive(true);
            if (playModeMenu != null)
                playModeMenu.SetActive(false);
            if (themeSelection != null)
                themeSelection.Hide();
            if (brushToolbar != null)
                brushToolbar.SetActive(false);
            SetDrawingPreparationButtonsVisible(false);
            if (restartButton != null)
                restartButton.gameObject.SetActive(false);
            if (homeButton != null)
                homeButton.gameObject.SetActive(false);
            if (quitGameButton != null)
                quitGameButton.gameObject.SetActive(false);
            if (mobileControls != null)
                mobileControls.SetActive(false);
            // Mürekkep yazısı yalnızca bölüm içinde çizim yaparken anlamlı;
            // evde bölümden kalan "Fırça: …" metni görünmesin.
            if (inkLabel != null)
            {
                inkLabel.text = "";
                inkLabel.gameObject.SetActive(false);
            }
            if (LevelManager.Instance != null)
                RefreshLevel();
        }

        private void Update()
        {
            RefreshDrawingPreparationButtons();
            if (inkLabel == null || LineDraw.Instance == null || !GameplayMode.IsDrawing)
                return;
            if (GameFlow.Instance != null && GameFlow.Instance.InHub)
                return;

            bool preparing = GameFlow.Instance != null &&
                GameFlow.Instance.State == GameFlowState.DrawingPreparation;
            if (preparing)
            {
                var cameraFollow = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera>() : null;
                int view = cameraFollow != null ? cameraFollow.PreparationViewIndex + 1 : 1;
                int total = cameraFollow != null ? cameraFollow.PreparationViewCount : 1;
                inkLabel.text = $"{view} / {total}";
            }
            else
                inkLabel.text = GameText.Ink + $": {LineDraw.Instance.InkLeft:0.0}";
        }

        void MoveDrawingView(int direction)
        {
            LineDraw.Instance?.FinishCurrentStroke();
            var cameraFollow = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera>() : null;
            cameraFollow?.MovePreparationView(direction);
            RefreshDrawingPreparationButtons();
        }

        void RefreshDrawingPreparationButtons()
        {
            bool preparing = GameFlow.Instance != null &&
                GameFlow.Instance.State == GameFlowState.DrawingPreparation;
            SetDrawingPreparationButtonsVisible(preparing);
            if (brushToolbar != null && GameplayMode.IsDrawing &&
                GameFlow.Instance != null && !GameFlow.Instance.InHub &&
                GameFlow.Instance.State != GameFlowState.InfoOverlay)
                brushToolbar.SetActive(preparing);
            bool drawingGameplay = GameplayMode.IsDrawing && GameFlow.Instance != null &&
                GameFlow.Instance.State == GameFlowState.Gameplay;
            if (mobileControls != null && GameplayMode.IsDrawing && GameFlow.Instance != null &&
                !GameFlow.Instance.InHub)
                mobileControls.SetActive(drawingGameplay);
            if (inkLabel != null)
                inkLabel.gameObject.SetActive(GameplayMode.IsDrawing && preparing);

            if (!preparing)
                return;
            var cameraFollow = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera>() : null;
            if (previousDrawViewButton != null)
                previousDrawViewButton.interactable = cameraFollow != null &&
                    cameraFollow.PreparationViewIndex > 0;
            if (nextDrawViewButton != null)
                nextDrawViewButton.interactable = cameraFollow != null &&
                    cameraFollow.PreparationViewIndex < cameraFollow.PreparationViewCount - 1;
        }

        void SetDrawingPreparationButtonsVisible(bool visible)
        {
            if (previousDrawViewButton != null)
                previousDrawViewButton.gameObject.SetActive(visible);
            if (nextDrawViewButton != null)
                nextDrawViewButton.gameObject.SetActive(visible);
            if (startDrawingGameButton != null)
                startDrawingGameButton.gameObject.SetActive(visible);
        }

        private void RefreshLove(int total)
        {
            // Sade sayaç: yanında para ikonu olduğu için yazıya gerek yok.
            if (loveLabel != null)
                loveLabel.text = total.ToString();
        }

        public void BindBalanceRoot(GameObject root)
        {
            balanceRoot = root;
        }

        void SetBalanceVisible(bool visible)
        {
            if (balanceRoot != null)
            {
                balanceRoot.SetActive(visible);
                return;
            }
            if (loveLabel != null)
                loveLabel.gameObject.SetActive(visible);
        }

        private void RefreshCat(CatDefinition cat)
        {
            if (selectedCatLabel == null || cat == null)
                return;
            if (GameFlow.Instance != null && !GameFlow.Instance.InHub)
            {
                HideSelectedCatName();
                return;
            }
            if (selectedNameRoutine != null)
                StopCoroutine(selectedNameRoutine);
            selectedCatLabel.text = cat.displayName;
            selectedCatLabel.gameObject.SetActive(true);
            selectedNameRoutine = StartCoroutine(HideSelectedCatNameAfterDelay());
        }

        IEnumerator HideSelectedCatNameAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            HideSelectedCatName();
        }

        void HideSelectedCatName()
        {
            if (selectedNameRoutine != null)
            {
                StopCoroutine(selectedNameRoutine);
                selectedNameRoutine = null;
            }
            if (selectedCatLabel != null)
                selectedCatLabel.gameObject.SetActive(false);
        }

        private void RefreshLevel()
        {
            if (levelLabel != null && LevelManager.Instance != null)
                levelLabel.text = GameText.LevelLabel(LevelManager.Instance.DisplayLevel);
        }

        public void Bind(Text love, Text level, Text ink, Text selected, Button play, Button shop,
            Button directPlay, Button restart, Button home, CatNeedsUI careUI, GameObject brushes,
            GameObject tutorial, GameObject controls, GameObject modeMenu,
            Button drawingPlay, ThemeSelectionUI themeSelector, Button editButton, Button flipButton,
            Button settings, GameObject settingsRoot, Button previousDrawView,
            Button nextDrawView, Button startDrawingGame, Button quitGame)
        {
            loveLabel = love;
            levelLabel = level;
            inkLabel = ink;
            selectedCatLabel = selected;
            playButton = play;
            shopButton = shop;
            directPlayButton = directPlay;
            restartButton = restart;
            homeButton = home;
            quitGameButton = quitGame;
            needsUI = careUI;
            brushToolbar = brushes;
            brushTutorial = tutorial;
            mobileControls = controls;
            playModeMenu = modeMenu;
            drawingPlayButton = drawingPlay;
            editHomeButton = editButton;
            flipFurnitureButton = flipButton;
            settingsButton = settings;
            settingsPanel = settingsRoot;
            previousDrawViewButton = previousDrawView;
            nextDrawViewButton = nextDrawView;
            startDrawingGameButton = startDrawingGame;
            themeSelection = themeSelector;
            if (themeSelection != null)
                themeSelection.SetSelectionCallback(_ => StartSelectedMode());

            // RuntimeBootstrap, HudView bileşenini alt UI nesnelerinden önce oluşturur.
            // Bu nedenle OnEnable sırasında butonlar henüz atanmış olmayabilir.
            WireButtons();
            RefreshInitialState();
        }

        private void WireButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayButtonClicked);
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            if (directPlayButton != null)
            {
                directPlayButton.onClick.RemoveListener(OnDirectPlayButtonClicked);
                directPlayButton.onClick.AddListener(OnDirectPlayButtonClicked);
            }

            if (drawingPlayButton != null)
            {
                drawingPlayButton.onClick.RemoveListener(OnDrawingPlayButtonClicked);
                drawingPlayButton.onClick.AddListener(OnDrawingPlayButtonClicked);
            }

            if (previousDrawViewButton != null)
            {
                previousDrawViewButton.onClick.RemoveAllListeners();
                previousDrawViewButton.onClick.AddListener(() => MoveDrawingView(-1));
            }
            if (nextDrawViewButton != null)
            {
                nextDrawViewButton.onClick.RemoveAllListeners();
                nextDrawViewButton.onClick.AddListener(() => MoveDrawingView(1));
            }
            if (startDrawingGameButton != null)
            {
                startDrawingGameButton.onClick.RemoveAllListeners();
                startDrawingGameButton.onClick.AddListener(() => GameFlow.Instance?.StartGameplayFromDrawing(true));
            }

            if (editHomeButton != null)
            {
                editHomeButton.onClick.RemoveAllListeners();
                editHomeButton.onClick.AddListener(() =>
                {
                    bool active = HomeEditMode.Toggle();
                    SetEditButtonLabel(active);
                    if (flipFurnitureButton != null)
                        flipFurnitureButton.gameObject.SetActive(active);
                });
            }

            if (flipFurnitureButton != null)
            {
                flipFurnitureButton.onClick.RemoveAllListeners();
                flipFurnitureButton.onClick.AddListener(HomeEditMode.FlipSelected);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveAllListeners();
                restartButton.onClick.AddListener(() =>
                {
                    if (LevelManager.Instance != null)
                        LevelManager.Instance.BeginCurrentLevel();
                });
            }

            if (homeButton != null)
            {
                homeButton.onClick.RemoveAllListeners();
                homeButton.onClick.AddListener(() =>
                {
                    if (GameFlow.Instance != null)
                        GameFlow.Instance.EnterHub();
                });
            }
        }

        void SetEditButtonLabel(bool active)
        {
            if (editHomeButton == null)
                return;
            var label = editHomeButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = active ? "✓ ✎" : "✎";
            // İkonlu yuvarlak düğmede düzenleme modu renkle belirtilir.
            var background = editHomeButton.GetComponent<Image>();
            if (background != null && label == null)
                background.color = active ? EditActiveColor : EditIdleColor;
        }

        private void RefreshInitialState()
        {
            RefreshLove(SaveService.Data.lovePoints);
            RefreshLevel();

            HideSelectedCatName();
        }

        private bool ShowGameplayTutorialOnce()
        {
            var mode = GameplayMode.IsDrawing ? GameplayPlayMode.Drawing : GameplayPlayMode.DirectControl;
            var tutorial = brushTutorial != null ? brushTutorial.GetComponent<GameplayTutorialUI>() : null;
            if (tutorial == null || GameplayTutorialUI.HasSeen(mode))
                return false;
            GameFlow.Instance?.ShowInfoOverlay();
            tutorial.Open(mode, () => GameFlow.Instance?.DismissInfoOverlay());
            return true;
        }
    }
}
