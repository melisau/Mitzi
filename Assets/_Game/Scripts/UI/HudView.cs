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
        [SerializeField] private ThemeSelectionUI themeSelection;
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

        private void OnShopButtonClicked()
        {
            Debug.Log(">>> Dükkan butonuna basıldı! <<<");
        }

        private void OnLevelStarted()
        {
            RefreshLevel();
            HideSelectedCatName();
            if (playButton != null)
                playButton.gameObject.SetActive(false);
            if (shopButton != null)
                shopButton.gameObject.SetActive(false);
            HomeEditMode.Close();
            if (editHomeButton != null)
                editHomeButton.gameObject.SetActive(false);
            if (flipFurnitureButton != null)
                flipFurnitureButton.gameObject.SetActive(false);
            if (playModeMenu != null)
                playModeMenu.SetActive(false);
            if (themeSelection != null)
                themeSelection.Hide();
            if (brushToolbar != null)
                brushToolbar.SetActive(GameplayMode.IsDrawing);
            if (mobileControls != null)
                mobileControls.SetActive(!GameplayMode.IsDrawing);
            if (inkLabel != null)
                inkLabel.gameObject.SetActive(GameplayMode.IsDrawing);
            if (restartButton != null)
                restartButton.gameObject.SetActive(true);
            if (homeButton != null)
                homeButton.gameObject.SetActive(true);
            bool cityTheme = LevelManager.Instance != null &&
                ThemeSelectionUI.GetSelectedTheme(LevelManager.Instance.DisplayLevel) == 2;
            if (cityTheme)
                ShowCityTutorialOnce();
            else if (GameplayMode.IsDrawing)
                ShowBrushTutorialOnce();
        }

        private void OnHubEntered()
        {
            HideSelectedCatName();
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

            inkLabel.text = $"{GameText.Ink}: {LineDraw.Instance.InkLeft:0.0}";
        }

        private void RefreshLove(int total)
        {
            if (loveLabel != null)
                loveLabel.text = $"{GameText.Love}: {total}";
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
            Button drawingPlay, ThemeSelectionUI themeSelector, Button editButton, Button flipButton)
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

        private void ShowBrushTutorialOnce()
        {
            if (brushTutorial == null || PlayerPrefs.GetInt("PawPath.BrushTutorialSeen.v2", 0) == 1)
                return;

            SetTutorialText("YOLU TAMAMLAMA\n\nKedi hazır zeminde kendi yürür.\nYalnızca çukurlara köprü, tümseklere rampa çiz.\n\nSiyah: Normal yol   Mavi: Zıplatır\nKırmızı: Tehlike   Beyaz: Kaygan yol\nSilgi: Çizdiğin yolu siler");
            brushTutorial.SetActive(true);
            PlayerPrefs.SetInt("PawPath.BrushTutorialSeen.v2", 1);
            PlayerPrefs.Save();
            StartCoroutine(HideBrushTutorial());
        }

        private void ShowCityTutorialOnce()
        {
            if (brushTutorial == null || PlayerPrefs.GetInt("PawPath.CityTutorialSeen.v1", 0) == 1)
                return;

            SetTutorialText("CADDE YOLU\n\nArabaların üzerinden atlamak için zıpla.\nKonteynırların bulunduğu çukurlara düşmemek için zamanında zıpla.\n\nDikkatli ilerle ve yolun sonundaki kapıya ulaş!");
            brushTutorial.SetActive(true);
            PlayerPrefs.SetInt("PawPath.CityTutorialSeen.v1", 1);
            PlayerPrefs.Save();
            StartCoroutine(HideBrushTutorial());
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
            yield return new WaitForSeconds(5f);
            if (brushTutorial != null)
                brushTutorial.SetActive(false);
        }
    }
}
