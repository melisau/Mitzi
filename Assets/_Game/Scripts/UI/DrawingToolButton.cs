using PawPath.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace PawPath.UI
{
    /// <summary>Shows the active drawing tool and fades unavailable page arrows.</summary>
    public sealed class DrawingToolButton : MonoBehaviour
    {
        PathSurfaceType type;
        bool eraser;
        GameObject selection;
        Button button;
        CanvasGroup group;

        void Awake()
        {
            button = GetComponent<Button>();
            group = gameObject.AddComponent<CanvasGroup>();
        }

        public void Bind(PathSurfaceType brush, bool isEraser, GameObject ring)
        {
            type = brush;
            eraser = isEraser;
            selection = ring;
            Refresh();
        }

        void OnEnable() => Refresh();
        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (group != null && button != null)
                group.alpha = button.interactable ? 1f : 0.35f;
            if (selection == null) return;
            var drawing = LineDraw.Instance;
            bool active = drawing != null ? eraser ? drawing.IsEraser :
                !drawing.IsEraser && drawing.CurrentBrush == type : !eraser && type == PathSurfaceType.Normal;
            if (selection.activeSelf != active) selection.SetActive(active);
        }
    }
}
