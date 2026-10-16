using UnityEngine;
using UnityEngine.UI;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Hub;

namespace PawPath.UI
{
    /// <summary>
    /// Seçili kedinin başının üstünde, okuma gerektirmeyen duygu balonu.
    /// Bir ihtiyaç azaldığında balon o ihtiyacın ikonunu (mama kabı, su damlası,
    /// kalp) gösterir ve sallanır; kedi uyurken ay, her şey yolundayken ara ara
    /// gülen kedi yüzü görünür. İhtiyaç giderildiği anda kedi "teşekkür" eder.
    /// </summary>
    public class CatMoodBubble : MonoBehaviour
    {
        enum Mood { None, Happy, Hungry, Thirsty, Lonely, Sleepy }

        const float HappyShowSeconds = 3.5f;
        const float HappyIntervalSeconds = 12f;
        const float PopSeconds = 0.18f;
        const float WorldGap = 0.08f;

        RectTransform rect;
        RectTransform parentRect;
        Image icon;
        CanvasGroup group;
        UiShake shake;
        CatNeedsSystem needs;

        Color foodColor, waterColor, loveColor, sleepColor, faceColor;

        Transform target;
        SpriteRenderer targetRenderer;
        CatHomeBehaviour targetBehaviour;
        string targetId;

        Mood mood = Mood.None;
        bool visible;
        float showUntil;
        float nextHappyShow;
        float appearedAt;
        float nextNudge;

        public void Bind(RectTransform parent, Image iconImage, CatNeedsSystem needsSystem,
            Color food, Color water, Color love, Color sleep, Color face)
        {
            rect = GetComponent<RectTransform>();
            parentRect = parent;
            icon = iconImage;
            needs = needsSystem;
            foodColor = food;
            waterColor = water;
            loveColor = love;
            sleepColor = sleep;
            faceColor = face;
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            shake = GetComponent<UiShake>();
            if (shake == null)
                shake = gameObject.AddComponent<UiShake>();
            nextHappyShow = Time.unscaledTime + 2f;
            SetVisible(false);
        }

        void LateUpdate()
        {
            if (rect == null || parentRect == null)
                return;
            bool inHub = GameFlow.Instance == null || GameFlow.Instance.InHub;
            var cam = Camera.main;
            if (!inHub || cam == null || !ResolveTarget())
            {
                SetVisible(false);
                return;
            }

            float now = Time.unscaledTime;
            Mood next = DecideMood();
            if (next != mood)
            {
                // Bir ihtiyaç giderildiyse kedi hemen mutlu yüzünü gösterir.
                if (next == Mood.Happy && mood != Mood.Happy && mood != Mood.None)
                    showUntil = now + HappyShowSeconds;
                mood = next;
                ApplyMoodVisual();
                if (mood != Mood.Happy && mood != Mood.Sleepy)
                    nextNudge = now + 0.3f;
            }

            bool persistent = mood != Mood.Happy;
            if (!persistent && now >= nextHappyShow)
            {
                showUntil = now + HappyShowSeconds;
                nextHappyShow = now + HappyIntervalSeconds;
            }
            SetVisible(persistent || now < showUntil);
            if (!visible)
                return;

            if (mood == Mood.Hungry || mood == Mood.Thirsty || mood == Mood.Lonely)
            {
                if (now >= nextNudge)
                {
                    shake.Play();
                    nextNudge = now + 4f;
                }
            }

            // Kedinin görselinin tepesine yerleş, hafifçe süzül.
            Bounds bounds = targetRenderer.bounds;
            Vector3 world = new Vector3(bounds.center.x, bounds.max.y + WorldGap, 0f);
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screen, null, out Vector2 local))
            {
                float bob = Mathf.Sin(now * 2.6f) * 6f;
                rect.anchoredPosition = local + new Vector2(0f, bob);
            }

            float t = Mathf.Clamp01((now - appearedAt) / PopSeconds);
            float scale = Mathf.Lerp(0.55f, 1f, 1f - (1f - t) * (1f - t));
            rect.localScale = new Vector3(scale, scale, 1f);
        }

        Mood DecideMood()
        {
            if (targetBehaviour != null && targetBehaviour.CurrentActivity == ResidentActivity.Sleeping)
                return Mood.Sleepy;
            if (needs == null)
                return Mood.Happy;
            var state = needs.GetNeeds(targetId);
            if (state == null)
                return Mood.Happy;
            int lowest = Mathf.Min(state.hunger, Mathf.Min(state.water, state.affection));
            if (lowest >= NeedGauge.LowThreshold)
                return Mood.Happy;
            if (lowest == state.hunger) return Mood.Hungry;
            if (lowest == state.water) return Mood.Thirsty;
            return Mood.Lonely;
        }

        void ApplyMoodVisual()
        {
            if (icon == null)
                return;
            switch (mood)
            {
                case Mood.Hungry:
                    icon.sprite = CozyIconFactory.FoodBowl();
                    icon.color = foodColor;
                    break;
                case Mood.Thirsty:
                    icon.sprite = CozyIconFactory.WaterDrop();
                    icon.color = waterColor;
                    break;
                case Mood.Lonely:
                    icon.sprite = CozyIconFactory.Heart();
                    icon.color = loveColor;
                    break;
                case Mood.Sleepy:
                    icon.sprite = CozyIconFactory.Moon();
                    icon.color = sleepColor;
                    break;
                default:
                    icon.sprite = CozyIconFactory.HappyCatFace();
                    icon.color = faceColor;
                    break;
            }
        }

        bool ResolveTarget()
        {
            string selectedId = SaveService.Data != null && !string.IsNullOrEmpty(SaveService.Data.selectedCatId)
                ? SaveService.Data.selectedCatId : "mitzi";
            if (target != null && targetRenderer != null && selectedId == targetId)
                return true;

            target = null;
            targetRenderer = null;
            targetBehaviour = null;
            targetId = selectedId;
            if (CatHouseManager.Instance == null)
                return false;
            var found = CatHouseManager.Instance.transform.Find("Resident_" + selectedId);
            if (found == null)
                return false;
            target = found;
            targetRenderer = found.GetComponentInChildren<SpriteRenderer>();
            targetBehaviour = found.GetComponent<CatHomeBehaviour>();
            if (targetRenderer == null)
            {
                target = null;
                return false;
            }
            mood = Mood.None;
            return true;
        }

        void SetVisible(bool value)
        {
            if (group == null)
                return;
            if (value && !visible)
                appearedAt = Time.unscaledTime;
            visible = value;
            group.alpha = value ? 1f : 0f;
        }
    }
}
