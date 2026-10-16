using UnityEngine;
using UnityEngine.UI;

namespace PawPath.UI
{
    /// <summary>Kısa bir sağa-sola sallanma: "buna bak" demenin yazısız yolu.</summary>
    public class UiShake : MonoBehaviour
    {
        const float Duration = 0.55f;
        const float Angle = 12f;
        float startTime = -10f;

        public void Play() => startTime = Time.unscaledTime;

        void OnDisable() => transform.localRotation = Quaternion.identity;

        void Update()
        {
            float t = Time.unscaledTime - startTime;
            if (t > Duration)
            {
                if (transform.localRotation != Quaternion.identity)
                    transform.localRotation = Quaternion.identity;
                return;
            }
            float falloff = 1f - t / Duration;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 38f) * Angle * falloff);
        }
    }
}
