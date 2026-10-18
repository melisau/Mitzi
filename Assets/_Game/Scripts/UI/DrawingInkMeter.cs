using PawPath.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace PawPath.UI
{
    public sealed class DrawingInkMeter : MonoBehaviour
    {
        Image fill;
        public void Bind(Image value) => fill = value;
        void LateUpdate()
        {
            if (fill == null || LineDraw.Instance == null) return;
            float fraction = Mathf.Clamp01(LineDraw.Instance.InkLeft / Mathf.Max(0.01f, LineDraw.Instance.InkMax));
            fill.rectTransform.sizeDelta = new Vector2(190f * fraction, 16f);
            fill.enabled = fraction > 0.001f;
        }
    }
}
