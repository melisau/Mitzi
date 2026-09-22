using UnityEngine;

namespace PawPath.Levels
{
    /// <summary>Neo engellerinin neon çerçevesini materyal üretmeden titreştirir.</summary>
    public class CyberObstacleGlow : MonoBehaviour
    {
        SpriteRenderer[] innerLines;
        SpriteRenderer[] outerLines;
        float phase;

        public void Configure(SpriteRenderer[] inner, SpriteRenderer[] outer, float startPhase)
        {
            innerLines = inner;
            outerLines = outer;
            phase = startPhase;
            Refresh(0f);
        }

        void Update()
        {
            Refresh(Time.time);
        }

        void Refresh(float time)
        {
            if (innerLines == null || outerLines == null)
                return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3.5f + phase);
            for (int i = 0; i < innerLines.Length; i++)
            {
                Color neon = i < 2
                    ? new Color(0.25f, 0.95f, 1f, 0.68f + pulse * 0.30f)
                    : new Color(1f, 0.27f, 0.92f, 0.62f + pulse * 0.34f);
                innerLines[i].color = neon;
                neon.a = 0.10f + pulse * 0.14f;
                outerLines[i].color = neon;
            }
        }
    }
}
