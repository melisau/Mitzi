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
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private CatNeedsUI needsUI;
        [SerializeField] private GameObject brushToolbar;
        [SerializeField] private GameObject brushTutorial;

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
            if (needsUI != null && !needsUI.CheckEnergyAndStartLevel())
                return;
            if (needsUI == null && CatNeedsSystem.Instance != null && !CatNeedsSystem.Instance.CanStartLevel())
                return;

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
            if (playButton != null)
                playButton.gameObject.SetActive(false);
            if (shopButton != null)
                shopButton.gameObject.SetActive(false);
            if (brushToolbar != null)
                brushToolbar.SetActive(true);
            if (restartButton != null)
                restartButton.gameObject.SetActive(true);
            if (homeButton != null)
                homeButton.gameObject.SetActive(true);
            ShowBrushTutorialOnce();
        }

        private void OnHubEntered()
        {
            if (playButton != null)
                playButton.gameObject.SetActive(true);
            if (shopButton != null)
                shopButton.gameObject.SetActive(true);
            if (brushToolbar != null)
                brushToolbar.SetActive(false);
            if (restartButton != null)
                restartButton.gameObject.SetActive(false);
            if (homeButton != null)
                homeButton.gameObject.SetActive(false);
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
            if (selectedCatLabel != null && cat != null)
                selectedCatLabel.text = $"{GameText.PlayingAs}: {cat.displayName}";
        }

        private void RefreshLevel()
        {
            if (levelLabel != null && LevelManager.Instance != null)
                levelLabel.text = GameText.LevelLabel(LevelManager.Instance.DisplayLevel);
        }

        public void Bind(Text love, Text level, Text ink, Text selected, Button play, Button shop,
            Button restart, Button home, CatNeedsUI careUI, GameObject brushes, GameObject tutorial)
        {
            loveLabel = love;
            levelLabel = level;
            inkLabel = ink;
            selectedCatLabel = selected;
            playButton = play;
            shopButton = shop;
            restartButton = restart;
            homeButton = home;
            needsUI = careUI;
            brushToolbar = brushes;
            brushTutorial = tutorial;

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

        private void RefreshInitialState()
        {
            RefreshLove(SaveService.Data.lovePoints);
            RefreshLevel();

            var catalog = GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
            var selected = catalog != null ? catalog.GetCat(SaveService.Data.selectedCatId) : null;
            if (selected != null)
                RefreshCat(selected);
        }

        private void ShowBrushTutorialOnce()
        {
            if (brushTutorial == null || PlayerPrefs.GetInt("PawPath.BrushTutorialSeen.v2", 0) == 1)
                return;

            brushTutorial.SetActive(true);
            PlayerPrefs.SetInt("PawPath.BrushTutorialSeen.v2", 1);
            PlayerPrefs.Save();
            StartCoroutine(HideBrushTutorial());
        }

        private IEnumerator HideBrushTutorial()
        {
            yield return new WaitForSeconds(5f);
            if (brushTutorial != null)
                brushTutorial.SetActive(false);
        }
    }
}
