using UnityEngine;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.Hub
{
    /// <summary>Home-only front/rear sprites; the side animator remains intact for care and levels.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class HomeDirectionalAnimator : MonoBehaviour
    {
        public enum View { Side, Toward, Away }
        public View CurrentView { get; private set; }
        public SpriteRenderer DirectionRenderer => directionRenderer;
        CatDefinition definition;
        CatHomeBehaviour resident;
        SpriteRenderer sideRenderer, directionRenderer;
        float clock;
        const float FramesPerSecond = 9f;

        public void Bind(CatDefinition cat, SpriteRenderer source, CatHomeBehaviour behaviour)
        {
            definition = cat; sideRenderer = source; resident = behaviour;
            if (directionRenderer == null)
            {
                var holder = new GameObject("HomeDirectionVisual"); holder.transform.SetParent(transform, false);
                directionRenderer = holder.AddComponent<SpriteRenderer>();
                directionRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            }
            ShowSide();
        }

        public void Refresh(float deltaTime)
        {
            if (sideRenderer == null || resident == null || definition == null || definition.idleSprite == null || directionRenderer == null) return;
            var sleep = resident.GetComponent<MitziSleepAnimator>();
            bool allowed = GameFlow.Instance != null && GameFlow.Instance.InHub && !resident.HasExternalAnimation &&
                (resident.CurrentActivity == ResidentActivity.Walking || resident.CurrentActivity == ResidentActivity.Standing) &&
                (sleep == null || !sleep.BlocksWalking);
            if (!allowed) { ShowSide(); return; }
            Vector2 movement = resident.HomeMovementDirection;
            View next = CurrentView;
            if (movement.sqrMagnitude > .0001f)
            {
                // Keep a small hysteresis band around diagonals to avoid rapid pose switching.
                float threshold = CurrentView == View.Side ? 1.05f : .8f;
                next = Mathf.Abs(movement.y) > Mathf.Abs(movement.x) * threshold
                    ? (movement.y < 0 ? View.Toward : View.Away) : View.Side;
            }
            if (next == View.Side) { ShowSide(); return; }
            var frames = next == View.Toward ? definition.homeTowardFrames : definition.homeAwayFrames;
            if (frames == null || frames.Length == 0) { ShowSide(); return; }
            if (next != CurrentView) clock = 0f;
            CurrentView = next;
            if (resident.IsMovingInHome) clock += Mathf.Max(0,deltaTime);
            else clock = 0;
            int index = resident.IsMovingInHome ? Mathf.FloorToInt(clock * FramesPerSecond) % frames.Length : 0;
            var frame = frames[index];
            if (frame == null) { ShowSide(); return; }
            sideRenderer.enabled = false;
            directionRenderer.enabled = true; directionRenderer.sprite = frame;
            directionRenderer.color = sideRenderer.color;
            directionRenderer.sortingLayerID = sideRenderer.sortingLayerID;
            directionRenderer.sortingOrder = sideRenderer.sortingOrder;
            // Match the visible ear-to-paw height of the existing side cat, excluding its blank canvas.
            float referenceHeight = definition.idleSprite.bounds.size.y * Mathf.Abs(sideRenderer.transform.localScale.y) * definition.homeVisibleHeightRatio;
            float bodyHeight = next == View.Toward ? definition.homeTowardBodyHeight : definition.homeAwayBodyHeight;
            float scale = referenceHeight / (bodyHeight > .01f ? bodyHeight : frames[0].bounds.size.y);
            directionRenderer.transform.localScale = new Vector3(scale,scale,1);
            var original = definition.idleSprite;
            float floor = sideRenderer.transform.localPosition.y +
                (original.bounds.min.y + original.bounds.size.y * original.pivot.y / original.rect.height) * Mathf.Abs(sideRenderer.transform.localScale.y);
            directionRenderer.transform.localPosition = new Vector3(0,floor-frame.bounds.min.y*scale,sideRenderer.transform.localPosition.z);
        }

        void ShowSide()
        {
            CurrentView = View.Side; clock = 0;
            if (sideRenderer != null) sideRenderer.enabled = true;
            if (directionRenderer != null) directionRenderer.enabled = false;
        }
        void LateUpdate() => Refresh(Time.deltaTime);
        void OnDisable() => ShowSide();
    }
}
