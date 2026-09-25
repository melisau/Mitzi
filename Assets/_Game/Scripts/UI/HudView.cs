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
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text inkLabel;
        [SerializeField] private Text selectedCatLabel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button directPlayButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
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
        GameplayPlayMode pendingMode;
        Coroutine selectedNameRoutine;
        Coroutine tutorialRoutine;

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

        private void OnShopButtonClicked()
        {
            Debug.Log(">>> Dükkan butonuna basıldı! <<<");
        }

        private void OnLevelStarted()
        {
            RefreshLevel();
            HideSelectedCatName();
            if (loveLabel != null)
                loveLabel.transform.parent.Find("ShopBalanceBackground")?.gameObject.SetActive(false);
            if (loveLabel != null)
                loveLabel.gameObject.SetActive(false);
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
            bool cityTheme = LevelManager.Instance != null &&
                ThemeSelectionUI.GetSelectedTheme(LevelManager.Instance.DisplayLevel) == 2;
            bool tutorialShown = cityTheme
                ? ShowCityTutorialOnce()
                : GameplayMode.IsDrawing && ShowBrushTutorialOnce();
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
            if (loveLabel != null)
            {
                loveLabel.gameObject.SetActive(true);
                loveLabel.transform.parent.Find("ShopBalanceBackground")?.gameObject.SetActive(true);
                RefreshLove(SaveService.Data.lovePoints);
            }
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
            if (mobileControls != null)
                mobileControls.SetActive(false);
            if (inkLabel != null)
                inkLabel.gameObject.SetActive(true);
            if (LevelManager.Instance != null)
                RefreshLevel();
        }

        private void Update()
        {
            if (inkLabel == null || LineDraw.Instance == null)
                return;
            if (GameFlow.Instance != null && GameFlow.Instance.InHub)
                return;

            bool preparing = GameFlow.Instance != null &&
                GameFlow.Instance.State == GameFlowState.DrawingPreparation;
            RefreshDrawingPreparationButtons();
            if (preparing)
            {
                var cameraFollow = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera>() : null;
                int view = cameraFollow != null ? cameraFollow.PreparationViewIndex + 1 : 1;
                int total = cameraFollow != null ? cameraFollow.PreparationViewCount : 1;
                inkLabel.text = LineDraw.Instance.AvailableInkForCurrentView() <= 0.01f &&
                    LineDraw.Instance.InkLeft > 0.01f
                    ? $"Sonraki ekrana geç ▶ · Kalan {LineDraw.Instance.InkLeft:0.0}"
                    : $"Çizim: {LineDraw.Instance.InkLeft:0.0} · Ekran {view}/{total}";
            }
            else
                inkLabel.text = $"{GameText.Ink}: {LineDraw.Instance.InkLeft:0.0}";
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
            if (inkLabel != null && GameplayMode.IsDrawing && GameFlow.Instance != null &&
                !GameFlow.Instance.InHub)
                inkLabel.gameObject.SetActive(preparing);

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
            if (loveLabel != null)
                loveLabel.text = $"Dükkan Bakiyesi: {total}";
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
            Button nextDrawView, Button startDrawingGame)
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

            if (shopButton != null)
            {
                shopButton.onClick.RemoveListener(OnShopButtonClicked);
                shopButton.onClick.AddListener(OnShopButtonClicked);
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
        }

        private void RefreshInitialState()
        {
            RefreshLove(SaveService.Data.lovePoints);
            RefreshLevel();

            HideSelectedCatName();
        }

        private bool ShowBrushTutorialOnce()
        {
            if (brushTutorial == null || PlayerPrefs.GetInt("PawPath.BrushTutorialSeen.v2", 0) == 1)
                return false;

            SetTutorialText("YOLU TAMAMLAMA\n\nÖnceki/Sonraki ile bölümün tamamını gezip yolları çiz.\nMürekkebin yarısı sonraki ekranlar için korunur.\nBitince Oyunu Başlat'a bas veya mürekkebi tüket.\n\nSiyah: Yol   Mavi: Zıpla   Beyaz: Buz   Silgi: Sil");
            PlayerPrefs.SetInt("PawPath.BrushTutorialSeen.v2", 1);
            PlayerPrefs.Save();
            PresentTutorial();
            return true;
        }

        private bool ShowCityTutorialOnce()
        {
            if (brushTutorial == null || PlayerPrefs.GetInt("PawPath.CityTutorialSeen.v1", 0) == 1)
                return false;

            SetTutorialText("CADDE YOLU\n\nArabaların üzerinden atlamak için zıpla.\nKonteynırların bulunduğu çukurlara düşmemek için zamanında zıpla.\n\nDikkatli ilerle ve yolun sonundaki kapıya ulaş!");
            PlayerPrefs.SetInt("PawPath.CityTutorialSeen.v1", 1);
            PlayerPrefs.Save();
            PresentTutorial();
            return true;
        }

        private void PresentTutorial()
        {
            GameFlow.Instance?.ShowInfoOverlay();
            brushTutorial.SetActive(true);
            var dismissButton = brushTutorial.GetComponent<Button>();
            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveListener(DismissTutorial);
                dismissButton.onClick.AddListener(DismissTutorial);
            }
            if (tutorialRoutine != null)
                StopCoroutine(tutorialRoutine);
            tutorialRoutine = StartCoroutine(HideBrushTutorial());
        }

        private void DismissTutorial()
        {
            if (tutorialRoutine != null)
            {
                StopCoroutine(tutorialRoutine);
                tutorialRoutine = null;
            }
            if (brushTutorial != null)
                brushTutorial.SetActive(false);
            GameFlow.Instance?.DismissInfoOverlay();
        }

        private void SetTutorialText(string value)
        {
            if (brushTutorial == null)
                return;
            var label = brushTutorial.transform.Find("TutorialText")?.GetComponent<Text>();
            if (label == null)
                label = brushTutorial.GetComponentInChildren<Text>();
            if (label != null)
                label.text = value;
        }

        private IEnumerator HideBrushTutorial()
        {
            yield return new WaitForSecondsRealtime(5f);
            tutorialRoutine = null;
            DismissTutorial();
        }
    }
}
