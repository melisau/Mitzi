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

            // STOK KONTROLÜ: Eşya şu an odanın kayıt listesinde var mı?
            bool owned = SaveService.Data != null && SaveService.Data.placedItemIds.Contains(item.id);
            var label = button.GetComponentInChildren<Text>();
            
            if (owned)
            {
                button.interactable = true; 
                if (label != null)
                    label.text = "SAT"; // Sahipse sadece SAT yazar (Aynısından bir daha ALINAMAZ)
                
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    if (CozyEconomyManager.Instance != null && SaveService.Data != null)
                    {
                        // 1. Eşyayı odanın kayıt listesinden tamamen çıkartıyoruz!
                        SaveService.Data.placedItemIds.Remove(item.id);
                        
                        // 2. PARAYI İADE ETME: Oyuncuya parasının %50'sini (yarısını) geri veriyoruz
                        int refund = Mathf.CeilToInt(item.lovePointCost * 0.5f);
                        CozyEconomyManager.Instance.AddLove(refund, "Eşya Satışı"); 
                        
                        // 3. EVDEKİ MOBİLYA GÖRSELİNİ YOK ETME: Ev koduna anında yenilenme talimatı uçuruyoruz!
                        // Sahnede açık olan HubFurnitureView bileşenini bulup zorla tetikliyoruz
                        var furnitureView = FindFirstObjectByType<PawPath.Hub.HubFurnitureView>();
                        if (furnitureView != null)
                        {
                            // Evdeki eşyaların doğduğu slots alanını el ile tamamen sıfırlıyoruz!
                            // Satılan eşyanın yerindeki SpriteRenderer'ı anında siliyoruz
                            furnitureView.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
                        }

                        // 4. Dükkan listesini kendi içinde tık diye anında yeniliyoruz!
                        Rebuild(); 
                    }
                });
            }
            else
            {
                button.interactable = true;
                if (label != null)
                    label.text = "AL"; // Satın alınmadıysa AL yazar
                
                var captured = item;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    if (CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.TryBuy(captured))
                    {
                        if (SaveService.Data != null && !SaveService.Data.placedItemIds.Contains(captured.id))
                        {
                            // Satın alma başarılıysa eşyayı odanın kayıt listesine ekle!
                            SaveService.Data.placedItemIds.Add(captured.id);
                        }

                        // Evdeki görselleri anında çizmesi için ev kodunu tetikle!
                        var furnitureView = FindFirstObjectByType<PawPath.Hub.HubFurnitureView>();
                        if (furnitureView != null)
                        {
                            furnitureView.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
                        }

                        // Dükkanı tık diye anında yenile!
                        Rebuild();
                    }
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
