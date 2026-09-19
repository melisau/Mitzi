using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;

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
                pointsText.text = $"💖  {points}";
        }

        public void Rebuild()
        {
            if (listRoot == null || GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return;

            int currentPoints = CozyEconomyManager.Instance != null ? CozyEconomyManager.Instance.LovePoints : 0;
            
            // 1. Sağ Üst Şık Sevgi Rozetini Güncelle/Oluştur
            EnsurePointsHeaderCreated(currentPoints);

            // 2. Eski Listeyi Temizle
            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);

            // 3. Grid Ayarları (Kart Boyutları ve Boşluklar)
            var grid = listRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = listRoot.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(210, 260); 
                grid.spacing = new Vector2(25, 20);
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
            }

            // ListRoot Pozisyonu
            var listRt = listRoot.GetComponent<RectTransform>();
            if (listRt != null)
            {
                listRt.anchorMin = new Vector2(0.5f, 0.5f);
                listRt.anchorMax = new Vector2(0.5f, 0.5f);
                listRt.pivot = new Vector2(0.5f, 0.5f);
                listRt.anchoredPosition = new Vector2(0, -20);
                listRt.sizeDelta = new Vector2(700, 300);
            }

            // Dükkan Elemanlarını Oluştur
            foreach (var item in GameFlow.Instance.Catalog.shopItems)
            {
                if (item == null) continue;
                var row = rowPrefab != null ? Instantiate(rowPrefab, listRoot) : CreateRow(listRoot);
                WireRow(row, item);
            }
        }

        void EnsurePointsHeaderCreated(int points)
        {
            if (pointsText != null)
            {
                pointsText.text = $"💖  {points}";
                return;
            }

            var headerGo = transform.Find("PointsHeader")?.gameObject;
            if (headerGo == null)
            {
                headerGo = new GameObject("PointsHeader", typeof(RectTransform), typeof(Image));
                headerGo.transform.SetParent(transform, false);

                var rt = headerGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-40, -35); 
                rt.sizeDelta = new Vector2(110, 36);

                var bg = headerGo.GetComponent<Image>();
                bg.color = new Color(1f, 1f, 1f, 0.85f); 

                var txt = CreateText(headerGo.transform, "PointsText", 15);
                txt.fontStyle = FontStyle.Bold;
                txt.color = new Color(0.3f, 0.15f, 0.1f); 

                var txtRt = txt.rectTransform;
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                pointsText = txt;
            }

            pointsText.text = $"💖  {points}";
        }

        void WireRow(GameObject row, ShopItemDefinition item)
        {
            var texts = row.GetComponentsInChildren<Text>(true);

            if (texts.Length > 0)
            {
                texts[0].text = item.displayName;
                texts[0].color = new Color(0.2f, 0.15f, 0.1f); 
            }
            if (texts.Length > 1)
            {
                texts[1].text = item.description;
                texts[1].color = new Color(0.4f, 0.35f, 0.3f); 
            }
            if (texts.Length > 2)
            {
                texts[2].text = $"{item.lovePointCost} Puan";
                texts[2].color = new Color(0.8f, 0.35f, 0.2f); 
            }

            var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null && item.placedSprite != null)
            {
                iconImg.sprite = item.placedSprite;
                iconImg.color = Color.white;
            }

            var button = row.GetComponentInChildren<Button>();
            if (button == null) return;

            var label = button.GetComponentInChildren<Text>();
            bool isOwned = CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.Owns(item.id);
            bool isPlaced = SaveService.Data != null && SaveService.Data.placedItemIds.Contains(item.id);
            bool hasStock = isOwned || isPlaced;

            button.onClick.RemoveAllListeners();

            if (hasStock)
            {
                if (label != null) label.text = "Sat";
                var btnImg = button.GetComponent<Image>();
                if (btnImg != null) btnImg.color = new Color(0.8f, 0.45f, 0.4f); 

                button.onClick.AddListener(() => {
                    if (CozyEconomyManager.Instance != null && SaveService.Data != null)
                    {
                        SaveService.Data.placedItemIds.Remove(item.id);
                        SaveService.Data.ownedItemIds.Remove(item.id);
                        SaveService.Persist();
                        CozyEconomyManager.Instance.AddLove(Mathf.CeilToInt(item.lovePointCost * 0.5f), "Satış");
                    }
                    Rebuild();
                });
            }
            else
            {
                bool canAfford = CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.LovePoints >= item.lovePointCost;
                if (label != null) label.text = "Satın Al";
                var btnImg = button.GetComponent<Image>();
                if (btnImg != null) btnImg.color = canAfford ? new Color(0.4f, 0.65f, 0.45f) : new Color(0.7f, 0.7f, 0.7f);

                button.interactable = canAfford;
                button.onClick.AddListener(() => {
                    if (CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.TryBuy(item))
                    {
                        Rebuild();
                    }
                });
            }
        }

        static GameObject CreateRow(Transform parent)
        {
            var go = new GameObject("GridItem", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            
            var itemBg = go.GetComponent<Image>();
            itemBg.color = new Color(1f, 0.97f, 0.94f, 0.45f); 

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0, 55);
            iconRt.sizeDelta = new Vector2(85, 85);

            var title = CreateText(go.transform, "Title", 15);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.anchoredPosition = new Vector2(0, -5);
            title.rectTransform.sizeDelta = new Vector2(190, 22);

            var desc = CreateText(go.transform, "Desc", 11);
            desc.rectTransform.anchoredPosition = new Vector2(0, -28);
            desc.rectTransform.sizeDelta = new Vector2(190, 24);

            var cost = CreateText(go.transform, "Cost", 13);
            cost.fontStyle = FontStyle.Bold;
            cost.rectTransform.anchoredPosition = new Vector2(0, -52);
            cost.rectTransform.sizeDelta = new Vector2(190, 20);

            var btnGo = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(go.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchoredPosition = new Vector2(0, -88);
            btnRt.sizeDelta = new Vector2(100, 32);

            var label = CreateText(btnGo.transform, "Label", 12);
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(100, 32);

            return go;
        }

        static Text CreateText(Transform parent, string name, int size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        // RuntimeBootstrap.cs tarafından çağrılan başlatma metodu
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