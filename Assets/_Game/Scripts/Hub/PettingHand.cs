using UnityEngine;
using PawPath.UI;

namespace PawPath.Hub
{
    /// <summary>
    /// Kedi okşanırken fare imleci (ya da parmak) yerine görünen minik el.
    /// Parmağı yumuşakça izler ve sağa sola okşama hareketi yapar; okşama
    /// bitince kaybolur. Masaüstünde bu sırada sistem imleci gizlenir.
    /// </summary>
    public class PettingHand : MonoBehaviour
    {
        const float WorldSize = 0.85f;
        const float StrokeSpeed = 6.5f;

        static PettingHand instance;

        SpriteRenderer handRenderer;
        Vector3 target;
        bool visible;
        float alpha;
        float strokeTime;
        bool cursorHidden;

        /// <summary>Okşama sürerken her karede parmağın dünya konumuyla çağrılır.</summary>
        public static void Show(Vector2 worldPoint)
        {
            var hand = GetOrCreate();
            if (!hand.visible)
            {
                // İlk görünüşte bulunduğu yerden kaymasın, doğrudan parmakta belirsin.
                hand.transform.position = new Vector3(worldPoint.x, worldPoint.y + 0.12f, 0f);
                hand.strokeTime = 0f;
            }
            hand.visible = true;
            hand.target = new Vector3(worldPoint.x, worldPoint.y + 0.12f, 0f);
        }

        public static void Hide()
        {
            if (instance != null)
                instance.visible = false;
        }

        static PettingHand GetOrCreate()
        {
            if (instance != null)
                return instance;
            var go = new GameObject("PettingHand");
            instance = go.AddComponent<PettingHand>();
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = CozyIconFactory.PettingHand();
            renderer.sortingOrder = 60;
            renderer.color = new Color(1f, 1f, 1f, 0f);
            float spriteSize = Mathf.Max(0.01f, renderer.sprite.bounds.size.x);
            go.transform.localScale = Vector3.one * (WorldSize / spriteSize);
            instance.handRenderer = renderer;
            return instance;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            alpha = Mathf.MoveTowards(alpha, visible ? 1f : 0f, dt * (visible ? 10f : 6f));
            handRenderer.color = new Color(1f, 1f, 1f, alpha);
            SetCursorHidden(visible);
            if (alpha <= 0f)
                return;

            // Sağa sola okşama: yatay salınım, inişte hafif bastırma ve dönme.
            strokeTime += dt * StrokeSpeed;
            float swing = Mathf.Sin(strokeTime);
            Vector3 stroke = new Vector3(swing * 0.14f, -Mathf.Abs(swing) * 0.05f, 0f);
            transform.position = Vector3.Lerp(transform.position, target + stroke, 1f - Mathf.Exp(-18f * dt));
            transform.rotation = Quaternion.Euler(0f, 0f, -swing * 12f);
        }

        void SetCursorHidden(bool hidden)
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (cursorHidden == hidden)
                return;
            cursorHidden = hidden;
            Cursor.visible = !hidden;
#endif
        }

        void OnDisable()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            if (cursorHidden)
            {
                Cursor.visible = true;
                cursorHidden = false;
            }
#endif
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
