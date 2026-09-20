using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Content; // ShopItemDefinition ve türler için eklendi

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
                pointsText.text = $"💖 {points}";
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
                grid.constraintCount = 3; 
            }
            grid.cellSize = new Vector2(280, 360);
            grid.spacing = new Vector2(28, 24);

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
                
                // Küçük ve kibar rozet boyutları
                rt.anchoredPosition = new Vector2(-60, -20);
                rt.sizeDelta = new Vector2(180, 48);

                // Renksiz / Şeffaf - Hafif Siyah Transparan Zemin
                var bgImage = headerGo.AddComponent<Image>();
                bgImage.color = new Color(0f, 0f, 0f, 0.25f); 

                var txt = CreateText(headerGo.transform, "PointsText", 22);
                txt.fontStyle = FontStyle.Bold;
                txt.color = new Color(0.25f, 0.18f, 0.15f); // Koyu Sıcak Kahve Yazı Renk
                
                var txtRt = txt.rectTransform;
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
                txtRt.anchoredPosition = Vector2.zero;
                txt.alignment = TextAnchor.MiddleCenter;

                pointsText = txt;
            }

            pointsText.text = $"💖 {points}";
        }

        void WireRow(GameObject row, ShopItemDefinition item)
        {
            var texts = row.GetComponentsInChildren<Text>(true);
            
            // KOYU RENK YAZILAR (Açık Arka Planda Tam Okunabilirlik İçin)
            if (texts.Length > 0)
            {
                texts[0].text = item.displayName; 
                texts[0].fontSize = 24;
                texts[0].color = new Color(0.2f, 0.15f, 0.1f); 
            }
            if (texts.Length > 1)
            {
                texts[1].text = item.description; 
                texts[1].fontSize = 18;
                texts[1].color = new Color(0.35f, 0.3f, 0.25f); 
            }
            if (texts.Length > 2)
            {
                texts[2].text = $"{item.lovePointCost} Puan"; 
                texts[2].fontSize = 20;
                texts[2].color = new Color(0.65f, 0.25f, 0.15f); 
            }

            var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null && item.placedSprite != null)
            {
                iconImg.sprite = item.placedSprite;
                iconImg.color = Color.white;
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

            if (hasStock)
            {
                // SAT BUTONU TASARIMI
                if (label != null)
                {
                    label.text = "Sat";
                    label.color = Color.white;
                }
                
                var btnImg = button.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = Color.white;
                }

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
                {
                    label.text = "Satın Al";
                    label.color = Color.white;
                }

                var btnImg = button.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = canAfford
                        ? Color.white
                        : new Color(0.62f, 0.60f, 0.56f, 0.72f);
                }

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
            cost.rectTransform.anchoredPosition = new Vector2(0, -105);
            cost.rectTransform.sizeDelta = new Vector2(275, 32);

            // 4. KÜÇÜLTÜLMÜŞ BUTON
            var btnGo = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(go.transform, false);
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

        static void StyleWoodButton(Button button, Text label)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(182f, 54f);

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RusticUiSpriteFactory.WoodButton();
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            if (button.GetComponent<Outline>() == null)
            {
                var goldBorder = button.gameObject.AddComponent<Outline>();
                goldBorder.effectColor = new Color(0.88f, 0.69f, 0.30f, 0.95f);
                goldBorder.effectDistance = new Vector2(3f, -3f);
                var softShadow = button.gameObject.AddComponent<Shadow>();
                softShadow.effectColor = new Color(0.10f, 0.06f, 0.03f, 0.55f);
                softShadow.effectDistance = new Vector2(0f, -4f);
            }

            if (label != null)
            {
                label.fontSize = 20;
                label.fontStyle = FontStyle.Bold;
                label.color = new Color(1f, 0.96f, 0.86f);
                label.rectTransform.sizeDelta = new Vector2(176f, 50f);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.06f, 0.92f);
            colors.pressedColor = new Color(0.82f, 0.72f, 0.60f);
            colors.disabledColor = new Color(0.58f, 0.55f, 0.50f, 0.72f);
            button.colors = colors;
        }

        static void StyleHomeButton(Button button)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 72f);
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RusticUiSpriteFactory.ParchmentPanel();
                image.type = Image.Type.Sliced;
                image.color = new Color(0.94f, 0.88f, 1f, 0.95f);
            }
            if (button.GetComponent<Outline>() == null)
            {
                var border = button.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(0.35f, 0.24f, 0.19f, 0.72f);
                border.effectDistance = new Vector2(4f, -4f);
            }
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "Kedi Evi";
                label.fontSize = 27;
                label.color = new Color(0.28f, 0.20f, 0.20f);
            }
        }

        void ReturnToCatHouse()
        {
            gameObject.SetActive(false);
            if (GameFlow.Instance != null)
                GameFlow.Instance.EnterHub();
        }
    }
}
