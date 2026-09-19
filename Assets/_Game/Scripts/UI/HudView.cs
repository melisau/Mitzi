using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
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
            Debug.Log(">>> Yola Çık butonuna basıldı! <<<");

            if (GameFlow.Instance != null)
            {
                Debug.Log("GameFlow bulundu, bölüm başlatılıyor...");
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
        }

        private void OnHubEntered()
        {
            if (playButton != null)
                playButton.gameObject.SetActive(true);
            if (shopButton != null)
                shopButton.gameObject.SetActive(true);
            if (brushToolbar != null)
                brushToolbar.SetActive(false);
            ShowBrushTutorialOnce();
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

        public void Bind(Text love, Text level, Text ink, Text selected, Button play, Button shop, GameObject brushes, GameObject tutorial)
        {
            loveLabel = love;
            levelLabel = level;
            inkLabel = ink;
            selectedCatLabel = selected;
            playButton = play;
            shopButton = shop;
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
            if (brushTutorial == null || PlayerPrefs.GetInt("PawPath.BrushTutorialSeen", 0) == 1)
                return;

            brushTutorial.SetActive(true);
            PlayerPrefs.SetInt("PawPath.BrushTutorialSeen", 1);
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
