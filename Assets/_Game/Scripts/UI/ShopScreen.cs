using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Localization;

namespace PawPath.UI
{
    public class ShopScreen : MonoBehaviour
    {
        [SerializeField] Transform listRoot;
        [SerializeField] GameObject rowPrefab;
        [SerializeField] Button closeButton;
        [SerializeField] Text pointsText; // Inspector'dan atanabilir Sevgi Puanı Text'i

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
                pointsText.text = $"💖 Sevgi: {points}";
            }
        }

        public void Rebuild()
        {
            if (listRoot == null || GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return;

            // 1. Mevcut Sevgi Puanını Güncelle
            int currentPoints = CozyEconomyManager.Instance != null ? CozyEconomyManager.Instance.LovePoints : 0;
            UpdatePointsDisplay(currentPoints);

            // 2. Sağ Üst Kısımda Puan Rozeti Yoksa Otomatik Header Oluştur
            EnsurePointsHeaderCreated(currentPoints);

            // 3. Eski Grid İçeriğini Temizle
            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);

            // Grid Ayarları
            var grid = listRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = listRoot.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(180, 220); 
                grid.spacing = new Vector2(30, 30);    
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3; 
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
        }

        void EnsurePointsHeaderCreated(int points)
        {
            if (pointsText != null) return;

            // Inspector'da Text atanmamışsa Sağ Üst Köşeye Şık Bir Puan Rozeti Ekler
            var headerGo = transform.Find("PointsHeader")?.gameObject;
            if (headerGo == null)
            {
                headerGo = new GameObject("PointsHeader", typeof(RectTransform));
                headerGo.transform.SetParent(transform, false);
                
                var rt = headerGo.GetComponent<RectTransform>();
                
                // Sağ üst köşeye sabitleme (Anchor: Right-Top)
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                
                // Sağ üstten içeriye doğru boşluk
                rt.anchoredPosition = new Vector2(-25, -20);
                rt.sizeDelta = new Vector2(220, 40);

                // Şeffaf koyu mor/pastel zemin
                var bgImage = headerGo.AddComponent<Image>();
                bgImage.color = new Color(0.25f, 0.22f, 0.35f, 0.85f);

                var txt = CreateText(headerGo.transform, "PointsText", 15);
                txt.fontStyle = FontStyle.Bold;
                txt.color = new Color(1f, 0.6f, 0.75f); // Pembe renkte metin
                
                var txtRt = txt.rectTransform;
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;
                txtRt.anchoredPosition = Vector2.zero;
                txt.alignment = TextAnchor.MiddleCenter;

                pointsText = txt;
            }

            pointsText.text = $"💖 Sevgi: {points}";
        }

        void WireRow(GameObject row, ShopItemDefinition item)
        {
            var texts = row.GetComponentsInChildren<Text>(true);
            if (texts.Length > 0)
                texts[0].text = item.displayName; 
            if (texts.Length > 1)
                texts[1].text = item.description; 
            if (texts.Length > 2)
                texts[2].text = $"{item.lovePointCost} Puan"; 

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

            // KESİN SAHİPLİK VE STOK KONTROLÜ: Hem envanter hem de yerleştirilenler sorgulanır
            bool isOwned = CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.Owns(item.id);
            bool isPlaced = SaveService.Data != null && SaveService.Data.placedItemIds.Contains(item.id);
            bool hasStock = isOwned || isPlaced;

            button.onClick.RemoveAllListeners();

            if (hasStock)
            {
                // --- SATMA MODU ---
                if (label != null)
                    label.text = "SAT";
                
                button.interactable = true;

                button.onClick.AddListener(() => {
                    // Çift tıklamayı önlemek için anında kilitle
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
                // --- ALMA MODU ---
                bool canAfford = CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.LovePoints >= item.lovePointCost;

                if (label != null)
                    label.text = "AL";

                // Yetersiz bakiyede butonu pasife çek
                button.interactable = canAfford;

                var captured = item;
                button.onClick.AddListener(() => {
                    // Tıklandığı salise butonu kilitle (Spam koruması)
                    button.interactable = false;

                    if (CozyEconomyManager.Instance != null)
                    {
                        // STOK DUVARI: Ürün zaten envanterdeyse satın almayı engelle
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
            rowRt.sizeDelta = new Vector2(180, 220);

            var itemBg = go.GetComponent<Image>();
            itemBg.color = new Color(0.38f, 0.35f, 0.48f); 

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0, 45);
            iconRt.sizeDelta = new Vector2(75, 75);

            var title = CreateText(go.transform, "Title", 14);
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white; 
            title.rectTransform.anchoredPosition = new Vector2(0, -5);
            title.rectTransform.sizeDelta = new Vector2(160, 20);

            var desc = CreateText(go.transform, "Desc", 10);
            desc.color = new Color(0.8f, 0.8f, 0.85f); 
            desc.rectTransform.anchoredPosition = new Vector2(0, -22);
            desc.rectTransform.sizeDelta = new Vector2(160, 16);

            var cost = CreateText(go.transform, "Cost", 11);
            cost.color = new Color(0.95f, 0.8f, 0.4f); 
            cost.rectTransform.anchoredPosition = new Vector2(0, -42);
            cost.rectTransform.sizeDelta = new Vector2(160, 16);

            var btnGo = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(go.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            
            btnRt.anchoredPosition = new Vector2(0, -75); 
            btnRt.sizeDelta = new Vector2(140, 32); 
            
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.85f, 0.45f, 0.35f); 

            var label = CreateText(btnGo.transform, "Label", 12);
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white; 
            label.rectTransform.sizeDelta = new Vector2(140, 32);
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
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => gameObject.SetActive(false));
            }
        }
    }
}