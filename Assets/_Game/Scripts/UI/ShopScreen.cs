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

        void OnEnable()
        {
            GameEvents.OnShopChanged += Rebuild;
            GameEvents.OnLovePointsChanged += _ => Rebuild();
            Rebuild();
        }

        void OnDisable()
        {
            GameEvents.OnShopChanged -= Rebuild;
            GameEvents.OnLovePointsChanged -= OnLove;
        }

        void OnLove(int _) => Rebuild();

        public void Rebuild()
        {
            if (listRoot == null || GameFlow.Instance == null || GameFlow.Instance.Catalog == null)
                return;

            for (int i = listRoot.childCount - 1; i >= 0; i--)
                Destroy(listRoot.GetChild(i).gameObject);

            // Izgara Düzenleyiciyi (Grid Layout) Liste Köküne Otomatik Kuruyoruz
            var grid = listRoot.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = listRoot.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(180, 220); // Her bir ürün kutusunun boyutu (Tam senin görseldeki oran)
                grid.spacing = new Vector2(30, 30);    // Kutular arası yan yana ve alt alta boşluklar
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3; // Yan yana TAM 3 TANE KUTU dizecek!
            }

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

        void WireRow(GameObject row, ShopItemDefinition item)
        {
            var texts = row.GetComponentsInChildren<Text>(true);
            if (texts.Length > 0)
                texts[0].text = item.displayName; // Ürün Adı
            if (texts.Length > 1)
                texts[1].text = item.description; // İsmin altındaki minik açıklama
            if (texts.Length > 2)
                texts[2].text = $"{item.lovePointCost} Puan"; // Fiyat yazısı butonun hemen üstünde kalacak

            var iconImg = row.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null && item.placedSprite != null)
            {
                iconImg.sprite = item.placedSprite;
                iconImg.color = Color.white;
            }

            var button = row.GetComponentInChildren<Button>();
            if (button == null)
                return;

            bool owned = CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.Owns(item.id);
            var label = button.GetComponentInChildren<Text>();
            
            // Kalabalık yazıları kaldırıp sadece AL / SAT yapıyoruz!
            if (owned)
            {
                button.interactable = true; 
                if (label != null)
                    label.text = "SAT"; 
                
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    // =========================================================
                    // GÜNCELLEME: Eğer projede TrySell varsa parayı iade eder 
                    // ve eşyayı odadan (SaveData) ANINDA SİLER!
                    // =========================================================
                    if (CozyEconomyManager.Instance != null)
                    {
                        // Eşyayı kayıttan çıkartıyoruz (Evden kaldırma sinyali)
                        SaveService.Data.placedItemIds.Remove(item.id);
                        
                        // Oyuncuya parasını geri veriyoruz (Örn: Fiyatın yarısı iade)
                        int refund = Mathf.CeilToInt(item.lovePointCost * 0.5f);
                        CozyEconomyManager.Instance.AddLovePoints(refund); 
                        
                        // Tüm odadaki mobilyaları ve dükkanı ANINDA YENİLİYORUZ!
                        GameEvents.OnShopChanged?.Invoke(); 
                    }
                });
            }
            else
            {
                button.interactable = true;
                if (label != null)
                    label.text = "AL"; 
                
                var captured = item;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    if (CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.TryBuy(captured))
                    {
                        // Satın alma başarılıysa eşyayı odanın kayıt listesine ANINDA ekle!
                        if (!SaveService.Data.placedItemIds.Contains(captured.id))
                        {
                            SaveService.Data.placedItemIds.Add(captured.id);
                        }
                        // Odayı ve dükkan listesini tık diye ANINDA tazele!
                        GameEvents.OnShopChanged?.Invoke();
                    }
                });
            }


        static GameObject CreateRow(Transform parent)
        {
            // Gönderdiğin görseldeki gibi şık, morumsu/tatlı dikey bir ürün kutusu
            var go = new GameObject("GridItem", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rowRt = go.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(180, 220);

            var itemBg = go.GetComponent<Image>();
            itemBg.color = new Color(0.38f, 0.35f, 0.48f); // Görselindeki o şık koyu mor/mavi arka plan rengi

            // 1. Ürünün Mini İkonu (Kutunun üst yarısında büyükçe duracak)
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0, 45);
            iconRt.sizeDelta = new Vector2(75, 75);

            // 2. Ürün Adı (Görselin hemen altında)
            var title = CreateText(go.transform, "Title", 14);
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white; // Mor üzerinde beyaz yazı şık durur
            title.rectTransform.anchoredPosition = new Vector2(0, -5);
            title.rectTransform.sizeDelta = new Vector2(160, 20);

            // 3. Ürün Açıklaması (İsmin hemen altında, minicik ve hiç yer kaplamayan alan)
            var desc = CreateText(go.transform, "Desc", 10);
            desc.color = new Color(0.8f, 0.8f, 0.85f); // Hafif soft beyaz
            desc.rectTransform.anchoredPosition = new Vector2(0, -22);
            desc.rectTransform.sizeDelta = new Vector2(160, 16);

            // 4. Maliyet Puanı (Butonun hemen üzerinde belirecek)
            var cost = CreateText(go.transform, "Cost", 11);
            cost.color = new Color(0.95f, 0.8f, 0.4f); // Altın sarısı puan rengi
            cost.rectTransform.anchoredPosition = new Vector2(0, -42);
            cost.rectTransform.sizeDelta = new Vector2(160, 16);

            // 5. Basit Al/Sat Butonu (Kutunun en altında temiz bir dikdörtgen)
            var btnGo = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(go.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchoredPosition = new Vector2(0, -75); 
            btnRt.sizeDelta = new Vector2(140, 32); 
            
            var btnImg = btnGo.GetComponent<Image>();
            btnImg.color = new Color(0.85f, 0.45f, 0.35f); // Şık kiremit/turuncu tonu

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
