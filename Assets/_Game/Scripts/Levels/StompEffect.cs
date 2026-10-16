using System.Collections.Generic;
using UnityEngine;
using PawPath.UI;

namespace PawPath.Levels
{
    /// <summary>
    /// Kedi köpeğin üstüne zıplayınca çıkan çizgi film "puf"u: köpek ezilip
    /// söner, yanlara yumuşak toz bulutları açılır, birkaç sarı yıldız dönerek
    /// yukarı fırlar ve beyaz bir halka genişler. Varsayılan parçacık
    /// materyaline bağlı değildir; tamamen kodla çizilen sprite'larla çalışır.
    /// </summary>
    public class StompEffect : MonoBehaviour
    {
        const float Lifetime = 0.75f;

        struct Piece
        {
            public Transform transform;
            public SpriteRenderer renderer;
            public Vector2 velocity;
            public float spin;
            public float startScale, endScale;
            public float duration, delay;
            public float startAlpha;
            public float gravity;
        }

        readonly List<Piece> pieces = new List<Piece>();
        Transform squashTarget;
        SpriteRenderer squashRenderer;
        Vector3 squashBaseScale;
        float age;

        static readonly Color DustColor = new Color(1f, 0.95f, 0.86f, 1f);
        static readonly Color DustShade = new Color(0.86f, 0.76f, 0.64f, 1f);
        static readonly Color StarColor = new Color(1f, 0.84f, 0.30f, 1f);

        /// <summary>position: köpeğin sırtı. dogRenderer: ezilip sönecek köpek görseli (kopyası).</summary>
        public static void Spawn(Vector3 position, SpriteRenderer dogRenderer)
        {
            var go = new GameObject("DogStompEffect");
            go.transform.position = position;
            go.AddComponent<StompEffect>().Build(position, dogRenderer);
        }

        void Build(Vector3 position, SpriteRenderer dogRenderer)
        {
            int baseOrder = 12;
            if (dogRenderer != null && dogRenderer.sprite != null)
            {
                // Köpeğin bir kopyası ezilip söner; gerçek köpek hemen kaldırılabilir.
                var ghost = new GameObject("SquashedDog");
                ghost.transform.position = dogRenderer.transform.position;
                ghost.transform.rotation = dogRenderer.transform.rotation;
                ghost.transform.localScale = dogRenderer.transform.lossyScale;
                ghost.transform.SetParent(transform, true);
                squashRenderer = ghost.AddComponent<SpriteRenderer>();
                squashRenderer.sprite = dogRenderer.sprite;
                squashRenderer.flipX = dogRenderer.flipX;
                squashRenderer.color = dogRenderer.color;
                squashRenderer.sortingOrder = dogRenderer.sortingOrder;
                squashTarget = ghost.transform;
                squashBaseScale = ghost.transform.localScale;
                baseOrder = dogRenderer.sortingOrder + 2;
            }

            float feetY = dogRenderer != null ? dogRenderer.bounds.min.y : position.y - 0.4f;
            Vector3 ground = new Vector3(position.x, feetY + 0.12f, 0f);

            // Beyaz halka: çarpmanın olduğu yerde hızla genişler.
            AddPiece(CozyIconFactory.Ring(), position, Color.white, Vector2.zero, 0f,
                0.25f, 1.25f, 0.28f, 0f, 0.85f, 0f, baseOrder + 1);

            // Yanlara açılan yumuşak toz bulutları (önde açık, arkada gölgeli ton).
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float spread = 0.8f + (i / 2) * 0.55f;
                var color = i < 2 ? DustShade : DustColor;
                AddPiece(CozyIconFactory.Cloud(), ground + new Vector3(side * 0.15f, 0.04f * i, 0f), color,
                    new Vector2(side * spread, 0.25f + 0.12f * (i / 2)), 0f,
                    0.28f, 0.62f - 0.06f * (i / 2), 0.50f, 0.02f * i, 0.95f, -0.4f, baseOrder - (i < 2 ? 1 : 0));
            }

            // Dönen yıldızlar yay çizerek yukarı fırlar.
            for (int i = 0; i < 5; i++)
            {
                float angle = Mathf.Lerp(35f, 145f, i / 4f) * Mathf.Deg2Rad;
                float speed = 2.6f + (i % 2) * 0.6f;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                AddPiece(CozyIconFactory.Star(), position + Vector3.up * 0.1f, StarColor, velocity,
                    (i % 2 == 0 ? 1f : -1f) * 420f, 0.20f, 0.12f, 0.62f, 0.03f * i, 1f, 7.5f, baseOrder + 2);
            }
        }

        void AddPiece(Sprite sprite, Vector3 position, Color color, Vector2 velocity, float spin,
            float startScale, float endScale, float duration, float delay, float alpha, float gravity, int order)
        {
            var go = new GameObject(sprite != null ? sprite.name : "Piece");
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(color.r, color.g, color.b, 0f);
            renderer.sortingOrder = order;
            go.transform.localScale = Vector3.one * startScale;
            pieces.Add(new Piece
            {
                transform = go.transform,
                renderer = renderer,
                velocity = velocity,
                spin = spin,
                startScale = startScale,
                endScale = endScale,
                duration = duration,
                delay = delay,
                startAlpha = alpha,
                gravity = gravity
            });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;

            UpdateSquash();

            for (int i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece.transform == null)
                    continue;
                float local = age - piece.delay;
                if (local < 0f)
                    continue;
                float t = Mathf.Clamp01(local / piece.duration);
                piece.velocity += Vector2.down * piece.gravity * dt;
                piece.velocity *= 1f - 2.2f * dt * (piece.gravity < 0f ? 1f : 0.3f);
                piece.transform.position += (Vector3)(piece.velocity * dt);
                piece.transform.Rotate(0f, 0f, piece.spin * dt);
                float eased = 1f - (1f - t) * (1f - t);
                piece.transform.localScale = Vector3.one * Mathf.Lerp(piece.startScale, piece.endScale, eased);
                // Hızlı belirir, sonra yumuşakça söner.
                float fadeIn = Mathf.Clamp01(t / 0.12f);
                float fadeOut = 1f - Mathf.Clamp01((t - 0.45f) / 0.55f);
                var color = piece.renderer.color;
                color.a = piece.startAlpha * fadeIn * fadeOut;
                piece.renderer.color = color;
                pieces[i] = piece;
            }

            if (age >= Lifetime)
                Destroy(gameObject);
        }

        void UpdateSquash()
        {
            if (squashTarget == null)
                return;
            // 0–0.12 sn: yassılaş; 0.12–0.32 sn: küçülüp sön.
            const float flatten = 0.12f;
            const float vanish = 0.32f;
            Vector3 scale;
            float alpha = 1f;
            if (age < flatten)
            {
                float k = age / flatten;
                scale = new Vector3(squashBaseScale.x * Mathf.Lerp(1f, 1.30f, k),
                    squashBaseScale.y * Mathf.Lerp(1f, 0.45f, k), squashBaseScale.z);
            }
            else
            {
                float k = Mathf.Clamp01((age - flatten) / (vanish - flatten));
                float shrink = Mathf.Lerp(1f, 0.2f, k * k);
                scale = new Vector3(squashBaseScale.x * 1.30f * shrink,
                    squashBaseScale.y * 0.45f * shrink, squashBaseScale.z);
                alpha = 1f - k;
            }
            // Ayak hizası sabit kalsın: yassılaşırken aşağı değil yere doğru ezilir.
            float bottomBefore = squashRenderer.bounds.min.y;
            squashTarget.localScale = scale;
            float bottomAfter = squashRenderer.bounds.min.y;
            squashTarget.position += Vector3.up * (bottomBefore - bottomAfter);
            var color = squashRenderer.color;
            color.a = alpha;
            squashRenderer.color = color;
            if (alpha <= 0f)
            {
                Destroy(squashTarget.gameObject);
                squashTarget = null;
            }
        }
    }
}
