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
        int purchaseRoom=-1;

        void OnEnable()
        {
            purchaseRoom=SaveService.Data.activeHomeRoom;
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

            int currentPoints = CozyEconomyManager.Instance != null ? CozyEconomyManager.Instance.ShoppingPoints : 0;
            UpdatePointsDisplay(currentPoints);

            var scrollRect = listRoot.GetComponentInParent<ScrollRect>();
            bool restoreScroll = scrollRect != null && listRoot.childCount > 0;
            float previousScroll = scrollRect != null ? scrollRect.verticalNormalizedPosition : 1f;

            // 1. Sağ Üst Kısma Küçük, Şeffaf/Soft Zeminli Sevgi Rozeti Oluştur
            EnsurePointsHeaderCreated(currentPoints);

            // 2. Eski Grid İçeriğini Temizle
            for (int i = listRoot.childCount - 1; i >= 0; i--)
            {
                var old=listRoot.GetChild(i).gameObject;
                old.SetActive(false);
                if(Application.isPlaying) Destroy(old); else DestroyImmediate(old);
            }

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
            grid.cellSize = new Vector2(250, 390);
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
                if(!restoreScroll) listRt.anchoredPosition = Vector2.zero;
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

            // Wallet and inventory events can rebuild twice in one frame.
            // Restore synchronously so the second rebuild keeps the same offset.
            if (scrollRect != null && listRt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(listRt);
                Canvas.ForceUpdateCanvases();
                if(restoreScroll) scrollRect.verticalNormalizedPosition = previousScroll;
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
            
            // Sade kart: büyük görsel, ad ve fiyatlı al düğmesi. Açıklama ve
            // oda/depo dökümü okuma bilmeyen çocuk için gereksiz kalabalıktı.
            if (texts.Length > 0)
            {
                texts[0].text = GameText.ItemName(item);
                texts[0].fontSize = 22;
                texts[0].fontStyle = FontStyle.Bold;
                texts[0].resizeTextForBestFit = true;
                texts[0].resizeTextMinSize = 16;
                texts[0].resizeTextMaxSize = 22;
                texts[0].rectTransform.anchoredPosition = new Vector2(0f, -38f);
                texts[0].rectTransform.sizeDelta = new Vector2(226f, 40f);
                texts[0].color = new Color(0.30f, 0.20f, 0.13f);
            }
            if (texts.Length > 1)
                texts[1].gameObject.SetActive(false);
            if (texts.Length > 2)
                texts[2].gameObject.SetActive(false);

            StyleCard(row);
            var iconRtRow = row.transform.Find("Icon") as RectTransform;
            if (iconRtRow != null)
            {
                iconRtRow.anchoredPosition = new Vector2(0f, 82f);
                iconRtRow.sizeDelta = new Vector2(196f, 196f);
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

            var economy = CozyEconomyManager.Instance;
            int owned = economy != null ? economy.OwnedCount(item.id) : 0;
            int stored = economy != null ? economy.StoredCount(item.id) : 0;
            if (owned > 0)
                AddOwnedBadge(row.transform, owned);
            button.onClick.RemoveAllListeners();
            bool affordable = economy != null && economy.CanAfford(item);
            button.interactable = affordable;
            if (label != null) label.text = (economy != null ? economy.GetPurchasePrice(item) : item.lovePointCost).ToString();
            button.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -100f);
            button.GetComponent<Image>().color = affordable ? BuyColor : UnaffordableColor;
            button.onClick.AddListener(() =>
            {
                if (economy != null && economy.TryBuy(item,purchaseRoom)) CozyAudioManager.Instance?.PlayShopBuy();
                Rebuild();
            });
            var place = Instantiate(button.gameObject, row.transform).GetComponent<Button>();
            place.name = "PlaceStoredButton";
            var placeRt = place.GetComponent<RectTransform>();
            placeRt.anchoredPosition = new Vector2(-50f, -130f);
            StyleInventoryIcon(place, CozyIconFactory.Unpack(), false);
            place.onClick.RemoveAllListeners(); place.interactable = stored > 0;
            place.gameObject.SetActive(stored > 0);
            place.GetComponent<Image>().color = stored > 0 ? BuyColor : UnaffordableColor;
            place.onClick.AddListener(() => { if(economy != null && economy.Place(item,purchaseRoom)) CozyAudioManager.Instance?.PlayConfirm(); Rebuild(); });
            var sell = Instantiate(place.gameObject, row.transform).GetComponent<Button>();
            sell.name = "SellOneButton";
            sell.GetComponent<RectTransform>().anchoredPosition = new Vector2(stored > 0 ? 50f : 0f, -130f);
            StyleInventoryIcon(sell, CozyIconFactory.Coin(), true);
            sell.onClick.RemoveAllListeners(); sell.interactable = owned > 0;
            sell.gameObject.SetActive(owned > 0);
            sell.GetComponent<Image>().color = owned > 0 ? SellColor : UnaffordableColor;
            sell.onClick.AddListener(() => { if(economy != null && economy.SellOne(item)) CozyAudioManager.Instance?.PlayShopSell(); Rebuild(); });
            // One action row: purchase, place a stored copy, sell one copy.
            int actions=(stored>0 ? 1:0)+(owned>0 ? 1:0);
            float buyWidth=actions==2 ? 104f:actions==1 ? 164f:220f;
            float total=buyWidth+actions*60f;
            float left=-total*.5f;
            var buyRect=button.GetComponent<RectTransform>();
            buyRect.sizeDelta=new Vector2(buyWidth,60);
            buyRect.anchoredPosition=new Vector2(left+buyWidth*.5f,-130);
            var buyIcon=button.transform.Find("Icon").GetComponent<RectTransform>();
            buyIcon.sizeDelta=new Vector2(30,30); buyIcon.anchoredPosition=new Vector2(-buyWidth*.5f+23,0);
            if(label!=null) { label.rectTransform.sizeDelta=new Vector2(buyWidth-48,48); label.rectTransform.anchoredPosition=new Vector2(18,0); }
            float cursor=left+buyWidth+8;
            foreach(var action in new[]{place,sell})
            {
                if(!action.gameObject.activeSelf) continue;
                var actionRect=action.GetComponent<RectTransform>(); actionRect.sizeDelta=new Vector2(52,52);
                actionRect.anchoredPosition=new Vector2(cursor+26,-130); cursor+=60;
            }
        }

        static void StyleInventoryIcon(Button button, Sprite sprite, bool sell)
        {
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
            var image = button.GetComponent<Image>();
            image.sprite = CozyIconFactory.CircleButton();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.gameObject.SetActive(false);
            var icon = button.transform.Find("Icon").GetComponent<Image>();
            icon.gameObject.SetActive(true);
            icon.sprite = sprite;
            icon.rectTransform.anchoredPosition = new Vector2(0f, 1f);
            icon.rectTransform.sizeDelta = new Vector2(40f, 40f);
            var colors = button.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.40f);
            button.colors = colors;
        }

        static readonly Color CardColor = new Color(1f, 0.96f, 0.90f, 0.93f);

        /// <summary>Kartı yuvarlak köşeli, yumuşak gölgeli krem zemine çevirir.</summary>
        static void StyleCard(GameObject row)
        {
            var bg = row.GetComponent<Image>();
            if (bg == null)
                return;
            bg.sprite = CozyIconFactory.Pill();
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1.1f;
            bg.color = CardColor;
            if (row.GetComponent<Shadow>() == null)
            {
                var shadow = row.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.25f, 0.15f, 0.08f, 0.18f);
                shadow.effectDistance = new Vector2(0f, -6f);
            }
        }

        /// <summary>Sahip olunan adet: kartın sağ üst köşesinde küçük karamel rozet.</summary>
        static void AddOwnedBadge(Transform row, int count)
        {
            var badge = new GameObject("OwnedBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(row, false);
            var rt = badge.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-30f, -30f);
            rt.sizeDelta = new Vector2(48f, 48f);
            var image = badge.GetComponent<Image>();
            image.sprite = CozyIconFactory.CircleButton();
            image.preserveAspect = true;
            image.color = SellColor;
            image.raycastTarget = false;
            var text = CreateText(badge.transform, "Count", 24);
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.text = count.ToString();
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>Düz "Dükkan" yazısı yerine ev ekranındaki sayaçla aynı koyu hap başlık.</summary>
        void StyleTitle()
        {
            foreach (var t in GetComponentsInChildren<Text>(true))
            {
                if (t.name != "Title" || (listRoot != null && t.transform.IsChildOf(listRoot)))
                    continue;
                if (t.transform.parent != null && t.transform.parent.name == "TitlePill")
                    return;
                var rt = t.rectTransform;
                var pill = new GameObject("TitlePill", typeof(RectTransform), typeof(Image));
                pill.transform.SetParent(rt.parent, false);
                pill.transform.SetSiblingIndex(rt.GetSiblingIndex());
                var pillRt = pill.GetComponent<RectTransform>();
                pillRt.anchorMin = pillRt.anchorMax = rt.anchorMin;
                pillRt.anchoredPosition = rt.anchoredPosition + new Vector2(0f, 6f);
                pillRt.sizeDelta = new Vector2(270f, 76f);
                var bg = pill.GetComponent<Image>();
                bg.sprite = CozyIconFactory.Pill();
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.20f, 0.13f, 0.09f, 0.72f);
                bg.raycastTarget = false;

                var icon = new GameObject("TitleIcon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(pill.transform, false);
                var iconRt = icon.GetComponent<RectTransform>();
                iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(44f, 0f);
                iconRt.sizeDelta = new Vector2(50f, 50f);
                var iconImage = icon.GetComponent<Image>();
                iconImage.sprite = CozyIconFactory.Storefront();
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                t.transform.SetParent(pill.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = new Vector2(78f, 0f);
                rt.offsetMax = new Vector2(-18f, 0f);
                t.fontSize = 32;
                t.fontStyle = FontStyle.Bold;
                t.color = new Color(1f, 0.95f, 0.82f);
                return;
            }
        }

        static GameObject CreateRow(Transform parent)
        {
            var go = new GameObject("GridItem", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rowRt = go.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(280, 360);

            var itemBg = go.GetComponent<Image>();
            itemBg.color = new Color(1f, 0.97f, 0.91f, 0.94f);

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
            StyleTitle();
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
