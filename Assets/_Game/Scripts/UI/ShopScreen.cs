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
                texts[2].text = $"{item.lovePointCost} {GameText.Love}";

            var button = row.GetComponentInChildren<Button>();
            if (button == null)
                return;

            bool owned = CozyEconomyManager.Instance != null && CozyEconomyManager.Instance.Owns(item.id);
            var label = button.GetComponentInChildren<Text>();
            if (owned)
            {
                button.interactable = false;
                if (label != null)
                    label.text = GameText.Owned;
            }
            else
            {
                button.interactable = true;
                if (label != null)
                    label.text = GameText.Buy;
                var captured = item;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => CozyEconomyManager.Instance.TryBuy(captured));
            }
        }

        static GameObject CreateRow(Transform parent)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var title = CreateText(go.transform, "Title", 22);
            title.rectTransform.anchoredPosition = new Vector2(0, 28);
            var desc = CreateText(go.transform, "Desc", 16);
            desc.rectTransform.anchoredPosition = new Vector2(0, 0);
            var cost = CreateText(go.transform, "Cost", 16);
            cost.rectTransform.anchoredPosition = new Vector2(0, -24);
            var btnGo = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(go.transform, false);
            CreateText(btnGo.transform, "Label", 18);
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
            t.color = new Color(0.35f, 0.28f, 0.32f);
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
