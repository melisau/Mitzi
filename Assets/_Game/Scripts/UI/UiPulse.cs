using UnityEngine;
using UnityEngine.UI;

namespace PawPath.UI
{
    /// <summary>
    /// Düğmeye hafif "nefes alma" ölçek animasyonu verir. Ana aksiyonu ve
    /// ihtiyacı olan bakım düğmesini öne çıkarmak için kullanılır.
    /// Oyun duraklasa da çalışsın diye ölçeklenmemiş zaman kullanır.
    /// </summary>
    public class UiPulse : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.06f;
        [SerializeField] float speed = 2.6f;
        Vector3 baseScale = Vector3.one;
        bool pulsing = true;
        float phase;

        public bool Pulsing
        {
            get => pulsing;
            set
            {
                if (pulsing == value)
                    return;
                pulsing = value;
                phase = Time.unscaledTime;
                if (!pulsing)
                    transform.localScale = baseScale;
            }
        }

        public void Configure(float scaleAmplitude, float cyclesPerSecondTimesTwoPi)
        {
            amplitude = scaleAmplitude;
            speed = cyclesPerSecondTimesTwoPi;
        }

        void Awake() => baseScale = transform.localScale;

        void OnDisable() => transform.localScale = baseScale;

        void Update()
        {
            if (!pulsing)
                return;
            float wave = (Mathf.Sin((Time.unscaledTime - phase) * speed) + 1f) * 0.5f;
            transform.localScale = baseScale * (1f + wave * amplitude);
        }
    }
}
