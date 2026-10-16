using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Hub;
using PawPath.Audio;
using PawPath.Content;

namespace PawPath.UI
{
    /// <summary>
    /// Kedi bakım arayüzü. Okuma bilmeyen çocuklar için tamamen görseldir:
    /// mama kabı, su damlası ve kalp ikonları değer kadar dolar; değer düşünce
    /// ikon kırmızıya döner, sallanır ve ilgili alt düğme "nefes alır".
    /// Günlük okşama ilerlemesi dolan patilerle gösterilir.
    /// </summary>
    public class CatNeedsUI : MonoBehaviour
    {
        [Header("Referanslar")]
        [SerializeField] private CatNeedsSystem catNeedsSystem;

        [Header("Bakım düğmeleri")]
        [SerializeField] private Button feedButton;
        [SerializeField] private Button waterButton;
        [SerializeField] private Button sleepButton;

        [Header("Görsel göstergeler")]
        [SerializeField] private NeedGauge foodGauge;
        [SerializeField] private NeedGauge waterGauge;
        [SerializeField] private NeedGauge loveGauge;
        [SerializeField] private Image[] pettingPaws;

        [Header("Ebeveyn için kısa bilgi (isteğe bağlı)")]
        [SerializeField] private Text toastText;

        static readonly Color PawFilled = new Color(0.55f, 0.33f, 0.20f, 1f);
        static readonly Color PawEmpty = new Color(1f, 1f, 1f, 0.45f);

        UiPulse shopHint;
        Coroutine toastRoutine;
        Coroutine shopHintRoutine;

        private void Awake()
        {
            if (catNeedsSystem == null)
                catNeedsSystem = CatNeedsSystem.Instance != null ? CatNeedsSystem.Instance : FindObjectOfType<CatNeedsSystem>();
            WireButtons();
        }

        void OnEnable()
        {
            if (catNeedsSystem != null)
                catNeedsSystem.NeedsChanged += UpdateUI;
            GameEvents.OnLovePointsChanged += OnLoveChanged;
            GameEvents.OnPlayableCatChanged += OnSelectedCatChanged;
            GameEvents.OnHubEntered += UpdateUI;
            GameEvents.OnLevelStarted += HideForLevel;
            UpdateUI();
        }

        void OnDisable()
        {
            if (catNeedsSystem != null)
                catNeedsSystem.NeedsChanged -= UpdateUI;
            GameEvents.OnLovePointsChanged -= OnLoveChanged;
            GameEvents.OnPlayableCatChanged -= OnSelectedCatChanged;
            GameEvents.OnHubEntered -= UpdateUI;
            GameEvents.OnLevelStarted -= HideForLevel;
        }

        private void Start()
        {
            UpdateUI();
        }

        void OnSelectedCatChanged(PawPath.Data.CatDefinition _) => UpdateUI();

        public void UpdateUI()
        {
            if (catNeedsSystem == null) return;

            bool inHub = GameFlow.Instance == null || GameFlow.Instance.InHub;
            SetVisible(inHub);

            var needs = catNeedsSystem.GetNeeds(SelectedCatId());
            bool newlyLow = false;
            if (needs != null)
            {
                if (foodGauge != null) newlyLow |= foodGauge.SetValue(needs.hunger);
                if (waterGauge != null) newlyLow |= waterGauge.SetValue(needs.water);
                if (loveGauge != null) newlyLow |= loveGauge.SetValue(needs.affection);
            }
            // Bir ihtiyaç yeni azaldıysa kedi miyavlayarak "bana bak" der.
            if (newlyLow && inHub && CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayMeow();

            UpdatePettingPaws();

            SetPulse(feedButton, foodGauge != null && foodGauge.IsLow);
            SetPulse(waterButton, waterGauge != null && waterGauge.IsLow);
            SetButtonAvailable(feedButton, true);
            SetButtonAvailable(waterButton, true);
            SetButtonAvailable(sleepButton, catNeedsSystem.CanSleepToday);
        }

        void UpdatePettingPaws()
        {
            if (pettingPaws == null || pettingPaws.Length == 0)
                return;
            int max = Mathf.Max(1, catNeedsSystem.maxDailyPettingPoints);
            float perPaw = max / (float)pettingPaws.Length;
            int filled = Mathf.Clamp(Mathf.FloorToInt(catNeedsSystem.DailyPettingPoints / perPaw),
                0, pettingPaws.Length);
            for (int i = 0; i < pettingPaws.Length; i++)
            {
                if (pettingPaws[i] != null)
                    pettingPaws[i].color = i < filled ? PawFilled : PawEmpty;
            }
        }

        private void OnFeedClicked()
        {
            if (catNeedsSystem == null)
                return;
            if (HubFurnitureView.Instance == null || !HubFurnitureView.Instance.HasPlacedBowl)
            {
                ShowMissingItemHint(feedButton, "Önce dükkândan mama kabı al.");
                return;
            }
            // Kedi kabın önüne yürür, yer; mama değeri yemek bitince dolar.
            if (StartCare(FurnitureSlotType.Bowl, CareAction.Eat))
                return;
            bool fed = catNeedsSystem.FeedCat();
            if (fed && CatHouseInteraction.Instance != null)
                CatHouseInteraction.Instance.SendSelectedToBowl();
            UpdateUI();
        }

        private void OnWaterClicked()
        {
            if (catNeedsSystem == null)
                return;
            if (HubFurnitureView.Instance == null || !HubFurnitureView.Instance.HasPlacedWater)
            {
                ShowMissingItemHint(waterButton, "Önce dükkândan su kabı al.");
                return;
            }
            // Kedi su kabının önüne yürür, içer; su değeri içince dolar.
            if (StartCare(FurnitureSlotType.Water, CareAction.Drink))
                return;
            bool drank = catNeedsSystem.GiveWater();
            if (drank && CatHouseInteraction.Instance != null)
                CatHouseInteraction.Instance.SendSelectedToWater();
            UpdateUI();
        }

        private void OnSleepClicked()
        {
            if (catNeedsSystem == null)
                return;
            // Yatak varsa kedi yatağına yürüyüp kıvrılır; yoksa olduğu yerde uyur.
            if (StartCare(FurnitureSlotType.Bed, CareAction.Sleep))
            {
                UpdateUI();
                return;
            }
            catNeedsSystem.PutToSleep();
            if (CatHouseInteraction.Instance != null)
                CatHouseInteraction.Instance.SleepSelectedHere(9f);
            UpdateUI();
        }

        static bool StartCare(FurnitureSlotType slot, CareAction action)
        {
            var view = HubFurnitureView.Instance;
            var interaction = view != null ? view.PlacedInteraction(slot) : null;
            return interaction != null && interaction.StartCareAction(action);
        }

        /// <summary>
        /// Oyna'ya basıldığında çağrılır. Kedi hazır değilse yazı yerine eksik
        /// ihtiyacın göstergesi ve düğmesi sallanır, hata sesi çalar.
        /// </summary>
        public bool CheckEnergyAndStartLevel()
        {
            if (catNeedsSystem == null || catNeedsSystem.CanStartLevel())
                return true;

            var needs = catNeedsSystem.GetNeeds(SelectedCatId());
            int minimum = catNeedsSystem.minimumNeedToPlay;
            if (needs != null)
            {
                if (needs.hunger < minimum)
                {
                    if (foodGauge != null) foodGauge.Nudge();
                    Shake(feedButton);
                }
                if (needs.water < minimum)
                {
                    if (waterGauge != null) waterGauge.Nudge();
                    Shake(waterButton);
                }
                if (needs.affection < minimum && loveGauge != null)
                    loveGauge.Nudge();
            }
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayError();
            return false;
        }

        public void Bind(CatNeedsSystem needs, Button feed, Button water, Button sleep,
            NeedGauge food, NeedGauge drink, NeedGauge love, Image[] paws, Text toast)
        {
            catNeedsSystem = needs;
            feedButton = feed;
            waterButton = water;
            sleepButton = sleep;
            foodGauge = food;
            waterGauge = drink;
            loveGauge = love;
            pettingPaws = paws;
            toastText = toast;
            if (toastText != null)
                toastText.gameObject.SetActive(false);
            WireButtons();
            UpdateUI();
        }

        /// <summary>Mama/su kabı yokken dükkân düğmesini kısa süre "nefes aldırmak" için.</summary>
        public void BindShopHint(Button shopButton)
        {
            if (shopButton == null)
                return;
            shopHint = shopButton.GetComponent<UiPulse>();
            if (shopHint == null)
                shopHint = shopButton.gameObject.AddComponent<UiPulse>();
            shopHint.Pulsing = false;
        }

        void WireButtons()
        {
            if (feedButton != null)
            {
                feedButton.onClick.RemoveListener(OnFeedClicked);
                feedButton.onClick.AddListener(OnFeedClicked);
            }
            if (waterButton != null)
            {
                waterButton.onClick.RemoveListener(OnWaterClicked);
                waterButton.onClick.AddListener(OnWaterClicked);
            }
            if (sleepButton != null)
            {
                sleepButton.onClick.RemoveListener(OnSleepClicked);
                sleepButton.onClick.AddListener(OnSleepClicked);
            }
        }

        void ShowMissingItemHint(Button careButton, string parentMessage)
        {
            Shake(careButton);
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayError();
            if (shopHint != null)
            {
                if (shopHintRoutine != null)
                    StopCoroutine(shopHintRoutine);
                shopHintRoutine = StartCoroutine(PulseShopForAWhile());
            }
            ShowToast(parentMessage);
        }

        IEnumerator PulseShopForAWhile()
        {
            shopHint.Pulsing = true;
            yield return new WaitForSecondsRealtime(4f);
            shopHint.Pulsing = false;
            shopHintRoutine = null;
        }

        void ShowToast(string message)
        {
            if (toastText == null)
                return;
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            if (toastRoutine != null)
                StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(HideToastLater());
        }

        IEnumerator HideToastLater()
        {
            yield return new WaitForSecondsRealtime(3f);
            if (toastText != null)
                toastText.gameObject.SetActive(false);
            toastRoutine = null;
        }

        static void Shake(Button button)
        {
            if (button == null)
                return;
            var shake = button.GetComponent<UiShake>();
            if (shake != null)
                shake.Play();
        }

        static void SetPulse(Button button, bool pulsing)
        {
            if (button == null)
                return;
            var pulse = button.GetComponent<UiPulse>();
            if (pulse != null)
                pulse.Pulsing = pulsing;
        }

        /// <summary>Kullanılamayan düğme kaybolmaz, soluklaşır; yerleşim sabit kalır.</summary>
        static void SetButtonAvailable(Button button, bool available)
        {
            if (button == null)
                return;
            button.interactable = available;
            var group = button.GetComponent<CanvasGroup>();
            if (group == null)
                group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = available ? 1f : 0.45f;
        }

        static string SelectedCatId()
        {
            return SaveService.Data != null && !string.IsNullOrEmpty(SaveService.Data.selectedCatId)
                ? SaveService.Data.selectedCatId : "mitzi";
        }

        void OnLoveChanged(int _) => UpdateUI();
        void HideForLevel() => SetVisible(false);

        void SetVisible(bool visible)
        {
            var group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
