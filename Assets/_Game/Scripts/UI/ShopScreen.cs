using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Localization;
using PawPath.Content; // ShopItemDefinition ve türler için eklendi
using PawPath.Audio;

namespace PawPath.UI
{
    public class ShopScreen : MonoBehaviour
    {
        [SerializeField] Transform listRoot;
        [SerializeField] GameObject rowPrefab;
        [SerializeField] Button closeButton;
        [SerializeField] Text pointsText; 

        void OnEnable()
        {
            GameEvents.OnShopChanged += Rebuild;
            GameEvents.OnLovePointsChanged += OnLove;
            Rebuild();
        }

        void OnDisable()
        {
            GameEvents.OnShopChanged -= Rebuild;
            GameEvents.OnLovePointsChanged -= OnLove;
        }

        void OnLove(int points)
        {
            UpdatePointsDisplay(points);
            Rebuild();
        }

        void UpdatePointsDisplay(int points)
        {
            if (pointsText != null)
            {
                pointsText.text = points.ToString();
            }
        }

        public void Rebuild()
        {
            if (listRoot == null || GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return;

            int currentPoints = CozyEconomyManager.Instance != null ? CozyEconomyManager.Instance.LovePoints : 0;
            UpdatePointsDisplay(currentPoints);

            var scrollRect = listRoot.GetComponentInParent<ScrollRect>();
            bool restoreScroll = scrollRect != null && listRoot.childCount > 0;
            float previousScroll = scrollRect != null ? scrollRect.verticalNormalizedPosition : 1f;

            // 1. Sağ Üst Kısma Küçük, Şeffaf/Soft Zeminli Sevgi Rozeti Oluştur
            EnsurePointsHeaderCreated(currentPoints);

            // 2. Eski Grid İçeriğini Temizle
            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);

            // Grid Ayarları ve Konumlandırma
            var grid = listRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = listRoot.gameObject.AddComponent<GridLayoutGroup>();
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
            }
            // Geniş ekranlarda ürünler tek sırada dört kart olarak görünür.
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(250, 350);
            grid.spacing = new Vector2(22, 24);

            var fitter = listRoot.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = listRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ListRoot'u biraz aşağı kaydırarak tepeyle çakışmayı önleyelim
            var listRt = listRoot.GetComponent<RectTransform>();
            if (listRt != null)
            {
                listRt.anchorMin = new Vector2(0f, 1f);
                listRt.anchorMax = new Vector2(1f, 1f);
                listRt.pivot = new Vector2(0.5f, 1f);
                listRt.anchoredPosition = Vector2.zero;
            }

            // Dükkan Ürünlerini Listele
            foreach (var item in GameFlow.Instance.Catalog.shopItems)
            {
                if (item == null)
                    continue;
                var row = rowPrefab != null
                    ? Instantiate(rowPrefab, listRoot)
                    : CreateRow(listRoot);
                WireRow(row, item);
            }

            if (restoreScroll)
                StartCoroutine(RestoreScrollPosition(scrollRect, previousScroll));
        }

        static IEnumerator RestoreScrollPosition(ScrollRect scrollRect, float normalizedPosition)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = normalizedPosition;
                scrollRect.StopMovement();
            }
        }

        void EnsurePointsHeaderCreated(int points)
        {
            if (pointsText != null) return;

            var headerGo = transform.Find("PointsHeader")?.gameObject;
            if (headerGo == null)
            {
                headerGo = new GameObject("PointsHeader", typeof(RectTransform));
                headerGo.transform.SetParent(transform, false);
                
                var rt = headerGo.GetComponent<RectTransform>();
                
                // Sağ üst köşeye sabitleme
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                
                // Ev ekranındaki bakiye sayacıyla aynı: koyu hap, para ikonu + sayı.
                rt.anchoredPosition = new Vector2(-60, -24);
                rt.sizeDelta = new Vector2(230, 76);

                var bgImage = headerGo.AddComponent<Image>();
                bgImage.sprite = CozyIconFactory.Pill();
                bgImage.type = Image.Type.Sliced;
                bgImage.color = new Color(0.20f, 0.13f, 0.09f, 0.72f);
                bgImage.raycastTarget = false;

                var coinGo = new GameObject("Coin", typeof(RectTransform), typeof(Image));
                coinGo.transform.SetParent(headerGo.transform, false);
                var coinRt = coinGo.GetComponent<RectTransform>();
                coinRt.anchorMin = coinRt.anchorMax = new Vector2(0f, 0.5f);
                coinRt.anchoredPosition = new Vector2(42f, 0f);
                coinRt.sizeDelta = new Vector2(62f, 62f);
                var coinImage = coinGo.GetComponent<Image>();
                coinImage.sprite = CozyIconFactory.Coin();
                coinImage.preserveAspect = true;
                coinImage.raycastTarget = false;

                var txt = CreateText(headerGo.transform, "PointsText", 32);
                txt.fontStyle = FontStyle.Bold;
                txt.color = new Color(1f, 0.95f, 0.82f);
                
                var txtRt = txt.rectTransform;
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(76f, 0f);
                txtRt.offsetMax = new Vector2(-14f, 0f);
                txt.alignment = TextAnchor.MiddleCenter;

                pointsText = txt;
            }

            pointsText.text = points.ToString();
        }

        void WireRow(GameObject row, ShopItemDefinition item)
        {
            var texts = row.GetComponentsInChildren<Text>(true);
            
            // KOYU RENK YAZILAR (Açık Arka Planda Tam Okunabilirlik İçin)
            if (texts.Length > 0)
            {
                texts[0].text = GameText.ItemName(item); 
                texts[0].fontSize = 24;
                texts[0].color = new Color(0.2f, 0.15f, 0.1f); 
            }
            if (texts.Length > 1)
            {
                texts[1].text = GameText.ItemDescription(item); 
                texts[1].fontSize = 18;
                texts[1].color = new Color(0.35f, 0.3f, 0.25f); 
            }
            if (texts.Length > 2)
            {
                texts[2].text = GameText.L($"{item.lovePointCost} Puan", $"{item.lovePointCost} Points", $"{item.lovePointCost} Punkte"); 
                texts[2].fontSize = 20;
                texts[2].color = new Color(0.65f, 0.25f, 0.15f); 
            }

            var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
            // Görseli olmayan eşyada beyaz kare göstermek yerine ikonu gizle.
            if (iconImg != null)
                iconImg.enabled = item.placedSprite != null;
            if (iconImg != null && item.placedSprite != null)
            {
                iconImg.sprite = item.placedSprite;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
            }

            var button = row.GetComponentInChildren<Button>();
            if (button == null)
                return;

            var label = button.GetComponentInChildren<Text>();
            StyleWoodButton(button, label);

            bool isOwned = CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.Owns(item.id);
            bool isPlaced = SaveService.Data != null && SaveService.Data.placedItemIds.Contains(item.id);
            bool hasStock = isOwned || isPlaced;

            button.onClick.RemoveAllListeners();

            if (texts.Length > 2 && isPlaced)
                texts[2].text = GameText.L("✓ Evde", "✓ In your home", "✓ Im Haus");
            else if (texts.Length > 2 && isOwned)
                texts[2].text = GameText.L("Sende (depoda)", "Owned (stored)", "Gekauft (gelagert)");

            if (isOwned && !isPlaced)
            {
                // Sahip olunan ama kullanılmayan eşya: aynı yerdeki eşyanın yerine koy.
                if (label != null)
                {
                    label.text = GameText.L("Yerleştir", "Place", "Aufstellen");
                    label.color = Color.white;
                }
                var placeImg = button.GetComponent<Image>();
                if (placeImg != null)
                    placeImg.color = new Color(0.86f, 0.95f, 0.84f, 1f);
                button.interactable = true;
                var toPlace = item;
                button.onClick.AddListener(() =>
                {
                    button.interactable = false;
                    if (CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.Place(toPlace))
                        CozyAudioManager.Instance?.PlayConfirm();
                    Rebuild();
                });
            }
            else if (hasStock)
            {
                // SAT BUTONU TASARIMI
                if (label != null)
                    label.text = GameText.L("Sat", "Sell", "Verkaufen");

                var btnImg = button.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = SellColor;

                button.interactable = true;

                button.onClick.AddListener(() => {
                    button.interactable = false;

                    if (CozyEconomyManager.Instance != null && SaveService.Data != null)
                    {
                        if (CozyEconomyManager.Instance.Owns(item.id) || SaveService.Data.placedItemIds.Contains(item.id))
                        {
                            SaveService.Data.placedItemIds.Remove(item.id);
                            SaveService.Data.ownedItemIds.Remove(item.id);
                            SaveService.Persist();

                            int refund = Mathf.CeilToInt(item.lovePointCost * 0.5f);
                            CozyEconomyManager.Instance.AddLove(refund, "Eşya Satışı"); 
                            CozyAudioManager.Instance?.PlayShopSell();
                            
                            var furnitureView = FindFirstObjectByType<PawPath.Hub.HubFurnitureView>();
                            if (furnitureView != null)
                            {
                                furnitureView.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
                            }
                        }
                    }

                    Rebuild(); 
                });
            }
            else
            {
                // AL BUTONU TASARIMI
                bool canAfford = CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.LovePoints >= item.lovePointCost;

                if (label != null)
                    label.text = GameText.L("Satın Al", "Buy", "Kaufen");

                // Parası yetmiyorsa soluk gri; yazıya gerek kalmadan "şimdilik alamazsın".
                var btnImg = button.GetComponent<Image>();
                if (btnImg != null)
                    btnImg.color = canAfford ? BuyColor : UnaffordableColor;
                var group = button.GetComponent<CanvasGroup>();
                if (group == null)
                    group = button.gameObject.AddComponent<CanvasGroup>();
                group.alpha = canAfford ? 1f : 0.6f;

                button.interactable = canAfford;

                var captured = item;
                button.onClick.AddListener(() => {
                    button.interactable = false;

                    if (CozyEconomyManager.Instance != null)
                    {
                        if (CozyEconomyManager.Instance.Owns(captured.id) || (SaveService.Data != null && SaveService.Data.placedItemIds.Contains(captured.id)))
                        {
                            Rebuild();
                            return;
                        }

                        bool success = CozyEconomyManager.Instance.TryBuy(captured);
                        if (success)
                        {
                            CozyAudioManager.Instance?.PlayShopBuy();
                            var furnitureView = FindFirstObjectByType<PawPath.Hub.HubFurnitureView>();
                            if (furnitureView != null)
                            {
                                furnitureView.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
                            }
                        }
                    }

                    Rebuild();
                });
            }
        }

        static GameObject CreateRow(Transform parent)
        {
            var go = new GameObject("GridItem", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rowRt = go.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(280, 360);

            var itemBg = go.GetComponent<Image>();
            itemBg.color = new Color(0, 0, 0, 0f); 

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0, 86);
            iconRt.sizeDelta = new Vector2(170, 170);

            // 1. Ürün Adı
            var title = CreateText(go.transform, "Title", 24);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.2f, 0.15f, 0.1f); 
            title.rectTransform.anchoredPosition = new Vector2(0, -12);
            title.rectTransform.sizeDelta = new Vector2(275, 42);

            // 2. Açıklama Metni
            var desc = CreateText(go.transform, "Desc", 18);
            desc.color = new Color(0.35f, 0.3f, 0.25f); 
            desc.rectTransform.anchoredPosition = new Vector2(0, -58);
            desc.rectTransform.sizeDelta = new Vector2(275, 72);

            // 3. Puan Metni
            var cost = CreateText(go.transform, "Cost", 20);
            cost.fontStyle = FontStyle.Bold;
            cost.color = new Color(0.65f, 0.25f, 0.15f); 
            cost.rectTransform.anchoredPosition = new Vector2(0, -98);
            cost.rectTransform.sizeDelta = new Vector2(275, 32);

            // 4. KÜÇÜLTÜLMÜŞ BUTON
            var btnGo = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(go.transform, false);
            btnGo.AddComponent<UiButtonSound>();
            var btnRt = btnGo.GetComponent<RectTransform>();
            
            btnRt.anchoredPosition = new Vector2(0, -148);
            btnRt.sizeDelta = new Vector2(170, 52);
            
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.75f, 0.4f, 0.35f); 

            var label = CreateText(btnGo.transform, "Label", 18);
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white; 
            label.rectTransform.sizeDelta = new Vector2(170, 52);
            label.rectTransform.anchoredPosition = Vector2.zero;

            return go;
        }

        static Text CreateText(Transform parent, string name, int size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null)
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        public void Bind(Transform list, Button close)
        {
            listRoot = list;
            closeButton = close;
            if (closeButton != null)
            {
                StyleHomeButton(closeButton);
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ReturnToCatHouse);
            }
        }

        // Ev ekranıyla aynı palet: yaprak yeşili (al), karamel (sat/eve dön).
        static readonly Color BuyColor = new Color(0.45f, 0.72f, 0.38f);
        static readonly Color SellColor = new Color(0.66f, 0.49f, 0.37f);
        static readonly Color UnaffordableColor = new Color(0.66f, 0.63f, 0.59f);

        static void StyleWoodButton(Button button, Text label)
        {
            // Ad korunuyor (eski çağrılar için); görünüm artık ikonlu hap düğme.
            StylePill(button, label, new Vector2(212f, 64f), CozyIconFactory.Coin(), 22, true);
        }

        static void StyleHomeButton(Button button)
        {
            var label = button.GetComponentInChildren<Text>();
            StylePill(button, label, new Vector2(300f, 104f), CozyIconFactory.House(), 32, false);
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = SellColor;
            if (label != null)
                label.text = GameText.House;
        }

        /// <summary>
        /// Ev ekranındaki hap düğmelerle aynı görünüm: koyu halka, degrade, gölge,
        /// solda ikon, beyaz kalın yazı. Renk Image.color ile verilir.
        /// </summary>
        static void StylePill(Button button, Text label, Vector2 size, Sprite icon, int fontSize, bool coloredIcon)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            if (button.name == "BuyButton")
                rt.anchoredPosition = new Vector2(0f, -142f);

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = CozyIconFactory.PillButton();
                image.type = Image.Type.Sliced;
                // Hap köşeleri yükseklik ne olursa olsun tam yuvarlak kalsın.
                image.pixelsPerUnitMultiplier = 104f / Mathf.Max(1f, size.y);
                image.color = Color.white;
            }
            if (button.GetComponent<Shadow>() == null)
            {
                var shadow = button.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
                shadow.effectDistance = new Vector2(0f, -5f);
            }

            float iconSize = size.y * 0.56f;
            float iconX = -size.x * 0.5f + size.y * 0.5f + 4f;
            if (icon != null && button.transform.Find("Icon") == null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(button.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = new Vector2(iconX, 1f);
                iconRt.sizeDelta = new Vector2(iconSize, iconSize);
                var iconImage = iconGo.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                if (!coloredIcon)
                {
                    var iconShadow = iconGo.AddComponent<Shadow>();
                    iconShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
                    iconShadow.effectDistance = new Vector2(0f, -3f);
                }
            }

            if (label != null)
            {
                float left = icon != null ? iconX + iconSize * 0.5f + 4f : -size.x * 0.5f + 18f;
                float right = size.x * 0.5f - size.y * 0.35f;
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(right - left, size.y - 12f);
                label.rectTransform.anchoredPosition = new Vector2((left + right) * 0.5f, 1f);
                label.fontSize = fontSize;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 14;
                label.resizeTextMaxSize = fontSize;
                if (label.GetComponent<Shadow>() == null)
                {
                    var textShadow = label.gameObject.AddComponent<Shadow>();
                    textShadow.effectColor = new Color(0.10f, 0.06f, 0.03f, 0.55f);
                    textShadow.effectDistance = new Vector2(0f, -2f);
                }
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.90f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            button.colors = colors;
        }

        void ReturnToCatHouse()
        {
            gameObject.SetActive(false);
            if (GameFlow.Instance != null)
                GameFlow.Instance.EnterHub();
        }
    }
}
