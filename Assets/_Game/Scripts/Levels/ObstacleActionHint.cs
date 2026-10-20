using UnityEngine;
using PawPath.UI;

namespace PawPath.Levels
{
    /// <summary>World-space direction and verb; contains no collider or input target.</summary>
    public static class ObstacleActionHint
    {
        public static GameObject Create(Transform parent, bool climb, string caption)
        {
            var root = new GameObject(climb ? "ClimbHint" : "CrouchHint");
            root.transform.SetParent(parent, false);
            // Tree artwork is scaled independently; keep both hints the same world size.
            root.transform.localScale = new Vector3(1f / parent.lossyScale.x, 1f / parent.lossyScale.y, 1f);
            var arrow = new GameObject(climb ? "UpArrow" : "DownArrow");
            arrow.transform.SetParent(root.transform, false);
            var icon = arrow.AddComponent<SpriteRenderer>();
            icon.sprite = climb ? CozyIconFactory.ArrowUp() : CozyIconFactory.ArrowDown();
            icon.sortingOrder = 12;
            // Scale the artwork on children so the caption retains a predictable world size.
            var face = new GameObject("Disc"); face.transform.SetParent(root.transform, false);
            var faceRenderer = face.AddComponent<SpriteRenderer>();
            faceRenderer.sprite = CozyIconFactory.CircleButton();
            faceRenderer.color = climb ? new Color(.45f, .72f, .38f) : new Color(.36f, .63f, .90f);
            faceRenderer.sortingOrder = 11;
            face.transform.localScale = Vector3.one * (.52f / faceRenderer.sprite.bounds.size.x);
            arrow.transform.localScale = Vector3.one * (.29f / icon.sprite.bounds.size.x);
            var label = new GameObject("ActionLabel"); label.transform.SetParent(root.transform, false);
            label.transform.localPosition = new Vector3(0f, .43f, 0f);
            var text = label.AddComponent<TextMesh>();
            text.text = caption; text.fontSize = 64; text.characterSize = .055f;
            text.fontStyle = FontStyle.Bold; text.anchor = TextAnchor.MiddleCenter;
            text.color = new Color(1f, .97f, .88f);
            label.GetComponent<MeshRenderer>().sortingOrder = 12;
            // Both action labels use the same light text and small shadow, without a panel.
            var shadow = new GameObject("CaptionShadow"); shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = label.transform.localPosition + new Vector3(.025f, -.025f, 0f);
            var shadowText = shadow.AddComponent<TextMesh>();
            shadowText.text = caption; shadowText.fontSize = text.fontSize; shadowText.characterSize = text.characterSize;
            shadowText.fontStyle = text.fontStyle; shadowText.anchor = text.anchor;
            shadowText.color = new Color(.12f, .10f, .07f, .9f);
            shadow.GetComponent<MeshRenderer>().sortingOrder = 11;
            root.SetActive(false);
            return root;
        }
    }
}
