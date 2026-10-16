using UnityEngine;
using UnityEngine.UI;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Levels;

namespace PawPath.UI
{
    /// <summary>
    /// Okuma gerektirmeyen bölüm ilerlemesi: kedi yüzü, yeşil dolan yol boyunca
    /// damalı bitiş bayrağına doğru ilerler. Yalnızca bölüm içindeyken görünür.
    /// </summary>
    public class LevelProgressBar : MonoBehaviour
    {
        RectTransform track;
        RectTransform fill;
        RectTransform marker;
        CanvasGroup group;
        float shown;

        public void Bind(RectTransform trackRect, RectTransform fillRect, RectTransform markerRect)
        {
            track = trackRect;
            fill = fillRect;
            marker = markerRect;
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;
        }

        void LateUpdate()
        {
            if (group == null)
                return;
            var flow = GameFlow.Instance;
            // Çizim hazırlığında üst ortadaki Önceki/Başlat/Sonraki düğmeleriyle çakışmasın.
            bool inLevel = flow != null && !flow.InHub && LevelManager.Instance != null &&
                CatController.Instance != null && flow.State != GameFlowState.DrawingPreparation;
            group.alpha = inLevel ? 1f : 0f;
            if (!inLevel || track == null)
                return;

            float start = LevelManager.Instance.SpawnPosition.x;
            float goal = LevelManager.Instance.GoalPosition.x;
            float catX = CatController.Instance.transform.position.x;
            float target = Mathf.Abs(goal - start) > 0.01f ? Mathf.Clamp01(Mathf.InverseLerp(start, goal, catX)) : 0f;
            // Yeni bölümde geriye atlayabilsin; ileri giderken yumuşak kaysın.
            shown = target < shown - 0.2f ? target : Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.5f);

            float width = track.rect.width;
            if (fill != null)
                fill.sizeDelta = new Vector2(Mathf.Max(fill.rect.height, width * shown), fill.sizeDelta.y);
            if (marker != null)
                marker.anchoredPosition = new Vector2(width * shown, marker.anchoredPosition.y);
        }
    }
}
