using UnityEngine;
using PawPath.Audio;

namespace PawPath.Drawing
{
    /// <summary>
    /// Parmak kalkınca çizginin "gerçeğe dönüşmesi": bir uçtan diğerine ışık
    /// dalgası geçer, dalganın arkasında pastel boya çizgisi fırçanın malzemesine
    /// (tahta, jöle, buz) dönüşür, sonunda çizgi hafifçe kabarıp yerine oturur.
    /// Ardından: jöle kedi bastıkça esner, buz ara ara parıldar.
    /// Hazırlık aşamasında zaman durduğu için ölçeklenmemiş zaman kullanılır.
    /// </summary>
    public class StrokeSolidify : MonoBehaviour
    {
        const float RevealSeconds = 0.45f;
        const float PopSeconds = 0.20f;
        const float WobbleSeconds = 0.55f;
        const float ShimmerSeconds = 0.75f;
        const float ShimmerInterval = 2.8f;

        LineRenderer crayon;
        LineRenderer solid;
        LineRenderer glow;
        LineRenderer outline;
        LineRenderer highlight;
        PathSurfaceType type;
        Color crayonColor;
        Color solidColor;
        float solidWidth, outlineWidth, highlightWidth;
        float startTime;
        float finishTime = -10f;
        float wobbleStart = -10f;
        float nextShimmer;
        bool revealing;

        public void Begin(LineRenderer crayonLine, LineRenderer outlineLine, LineRenderer highlightLine,
            PathSurfaceType surface)
        {
            crayon = crayonLine;
            outline = outlineLine;
            highlight = highlightLine;
            type = surface;
            crayonColor = StrokeStyle.CrayonColor(surface);
            solidColor = StrokeStyle.SolidColor(surface);

            solidWidth = crayon.widthMultiplier;
            outlineWidth = outline != null ? outline.widthMultiplier : 0f;
            highlightWidth = highlight != null ? highlight.widthMultiplier : 0f;

            solid = CopyLine("StrokeSolid", StrokeStyle.Surface(surface), crayon.sortingOrder, solidWidth,
                LineTextureMode.Tile);
            glow = CopyLine("StrokeGlow", StrokeStyle.Glow(), crayon.sortingOrder + 2, solidWidth * 2.4f,
                LineTextureMode.Stretch);

            startTime = Time.unscaledTime;
            revealing = true;
            ApplyReveal(0f);
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayBubble();
        }

        LineRenderer CopyLine(string name, Material material, int sortingOrder, float width, LineTextureMode mode)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            int count = crayon.positionCount;
            var positions = new Vector3[count];
            crayon.GetPositions(positions);
            line.positionCount = count;
            line.SetPositions(positions);
            line.numCapVertices = crayon.numCapVertices;
            line.numCornerVertices = crayon.numCornerVertices;
            line.textureMode = mode;
            line.alignment = LineAlignment.TransformZ;
            line.sortingOrder = sortingOrder;
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.widthCurve = crayon.widthCurve;
            return line;
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (revealing)
            {
                float t = Mathf.Clamp01((now - startTime) / RevealSeconds);
                ApplyReveal(t);
                if (t >= 1f)
                    FinishReveal(now);
                return;
            }

            // Yerine otururken kısa kabarma.
            float pop = Mathf.Clamp01((now - finishTime) / PopSeconds);
            float scale = 1f + 0.18f * (1f - pop) * (1f - pop);

            // Jöle: kedi bastıkça sönümlenen esneme.
            float wobbleT = (now - wobbleStart) / WobbleSeconds;
            if (wobbleT >= 0f && wobbleT < 1f)
                scale *= 1f + 0.35f * Mathf.Sin(wobbleT * Mathf.PI * 4f) * (1f - wobbleT);

            SetWidths(scale);

            if (type == PathSurfaceType.Ice)
                UpdateShimmer(now);
        }

        void ApplyReveal(float t)
        {
            // Dalga çizginin biraz dışından girip biraz dışından çıkar.
            float front = Mathf.Lerp(-0.15f, 1.15f, t);
            if (solid != null)
                solid.colorGradient = Step(solidColor, front, 1f, 0f);
            if (crayon != null)
                crayon.colorGradient = Step(crayonColor, front, 0f, 1f);
            if (glow != null)
                glow.colorGradient = Band(front, 0.13f, 0.85f);
        }

        void FinishReveal(float now)
        {
            revealing = false;
            finishTime = now;
            if (solid != null)
                solid.colorGradient = Step(solidColor, 2f, 1f, 1f);
            if (crayon != null)
                crayon.enabled = false;
            if (glow != null)
                glow.enabled = false;
            nextShimmer = now + ShimmerInterval * 0.5f;
        }

        void UpdateShimmer(float now)
        {
            if (glow == null)
                return;
            float t = (now - nextShimmer) / ShimmerSeconds;
            if (t < 0f)
                return;
            if (t >= 1f)
            {
                glow.enabled = false;
                nextShimmer = now + ShimmerInterval;
                return;
            }
            glow.enabled = true;
            glow.colorGradient = Band(Mathf.Lerp(-0.15f, 1.15f, t), 0.10f, 0.45f);
        }

        void SetWidths(float scale)
        {
            if (solid != null) solid.widthMultiplier = solidWidth * scale;
            if (outline != null) outline.widthMultiplier = outlineWidth * scale;
            if (highlight != null) highlight.widthMultiplier = highlightWidth * scale;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (type == PathSurfaceType.Bounce && !revealing)
                wobbleStart = Time.unscaledTime;
        }

        /// <summary>front'un gerisi beforeAlpha, ilerisi afterAlpha.</summary>
        static Gradient Step(Color color, float front, float beforeAlpha, float afterAlpha)
        {
            var gradient = new Gradient();
            var colors = new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) };
            GradientAlphaKey[] alphas;
            if (front <= 0f)
                alphas = new[] { new GradientAlphaKey(afterAlpha, 0f), new GradientAlphaKey(afterAlpha, 1f) };
            else if (front >= 1f)
                alphas = new[] { new GradientAlphaKey(beforeAlpha, 0f), new GradientAlphaKey(beforeAlpha, 1f) };
            else
                alphas = new[]
                {
                    new GradientAlphaKey(beforeAlpha, 0f),
                    new GradientAlphaKey(beforeAlpha, Mathf.Max(0f, front - 0.015f)),
                    new GradientAlphaKey(afterAlpha, Mathf.Min(1f, front + 0.015f)),
                    new GradientAlphaKey(afterAlpha, 1f)
                };
            gradient.SetKeys(colors, alphas);
            return gradient;
        }

        /// <summary>front etrafında halfWidth genişliğinde parlak bant.</summary>
        static Gradient Band(float front, float halfWidth, float peak)
        {
            var gradient = new Gradient();
            var white = new Color(1f, 0.97f, 0.86f);
            var colors = new[] { new GradientColorKey(white, 0f), new GradientColorKey(white, 1f) };
            // Parlaklık tepe noktasından iki yana doğrusal söner; anahtarlar bu
            // fonksiyondan örneklenir, böylece uçlarda da tutarlı kalır.
            float[] times =
            {
                0f, Mathf.Clamp01(front - halfWidth), Mathf.Clamp01(front),
                Mathf.Clamp01(front + halfWidth), 1f
            };
            var alphas = new GradientAlphaKey[times.Length];
            for (int i = 0; i < times.Length; i++)
            {
                float alpha = peak * Mathf.Clamp01(1f - Mathf.Abs(times[i] - front) / halfWidth);
                alphas[i] = new GradientAlphaKey(alpha, times[i]);
            }
            gradient.SetKeys(colors, alphas);
            return gradient;
        }
    }
}
