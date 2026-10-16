using UnityEngine;
using PawPath.UI;

namespace PawPath.Cat
{
    /// <summary>
    /// Kedinin ayaklarının altına yumuşak, oval bir gölge koyar; kediyi zemine
    /// oturtur. followGround açıkken gölge yere yansıtılır: kedi zıplayınca
    /// gölge yerde kalıp küçülür, çukurun üstünde kaybolur.
    /// </summary>
    public class SoftShadow : MonoBehaviour
    {
        const float MaxDrop = 3.2f;

        SpriteRenderer target;
        SpriteRenderer shadow;
        Collider2D[] ownColliders;
        bool followGround;
        float widthFactor;
        float footLift;
        float baseAlpha;

        public static SoftShadow Attach(GameObject owner, SpriteRenderer target, bool followGround,
            int sortingOrder, float widthFactor = 0.62f, float footLift = 0.05f, float alpha = 0.38f)
        {
            var shadowGo = new GameObject("SoftShadow");
            shadowGo.transform.SetParent(owner.transform, false);
            var renderer = shadowGo.AddComponent<SpriteRenderer>();
            renderer.sprite = CozyIconFactory.SoftEllipse();
            renderer.sortingOrder = sortingOrder;
            renderer.color = new Color(0.10f, 0.06f, 0.03f, alpha);

            var component = shadowGo.AddComponent<SoftShadow>();
            component.target = target;
            component.shadow = renderer;
            component.followGround = followGround;
            component.widthFactor = widthFactor;
            component.footLift = footLift;
            component.baseAlpha = alpha;
            component.ownColliders = owner.GetComponentsInChildren<Collider2D>(true);
            return component;
        }

        void LateUpdate()
        {
            if (target == null || shadow == null || target.sprite == null)
            {
                if (shadow != null) shadow.enabled = false;
                return;
            }

            Bounds bounds = target.bounds;
            float feetY = bounds.min.y + bounds.size.y * footLift;
            float x = bounds.center.x;
            float groundY = feetY;
            float strength = 1f;

            if (followGround)
            {
                if (!TryFindGround(new Vector2(x, feetY + 0.15f), out groundY))
                {
                    shadow.enabled = false;
                    return;
                }
                float height = Mathf.Max(0f, feetY - groundY);
                strength = Mathf.Clamp01(1f - height / 2.4f);
            }

            shadow.enabled = target.enabled && strength > 0.02f;
            float width = bounds.size.x * widthFactor * Mathf.Lerp(0.55f, 1f, strength);
            float heightScale = width * 0.22f;
            transform.position = new Vector3(x, groundY + 0.01f, transform.position.z);
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            transform.localScale = new Vector3(width / SafeAbs(parentScale.x),
                heightScale / SafeAbs(parentScale.y), 1f);
            var color = shadow.color;
            color.a = baseAlpha * strength;
            shadow.color = color;
        }

        bool TryFindGround(Vector2 origin, out float groundY)
        {
            groundY = origin.y;
            var hits = Physics2D.RaycastAll(origin, Vector2.down, MaxDrop);
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger || IsOwn(hit.collider))
                    continue;
                groundY = hit.point.y;
                return true;
            }
            return false;
        }

        bool IsOwn(Collider2D collider)
        {
            if (ownColliders == null)
                return false;
            foreach (var own in ownColliders)
                if (own == collider)
                    return true;
            return false;
        }

        static float SafeAbs(float value)
        {
            float abs = Mathf.Abs(value);
            return abs < 0.0001f ? 1f : abs;
        }
    }
}
