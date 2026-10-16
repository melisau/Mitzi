using UnityEngine;
using UnityEngine.UI;

namespace PawPath.UI
{
    /// <summary>
    /// Okuma gerektirmeyen ihtiyaç göstergesi: ikon siluetinin içi değer kadar
    /// aşağıdan yukarı dolar. Değer düşünce ikon kırmızıya döner ve ara ara sallanır.
    /// </summary>
    public class NeedGauge : MonoBehaviour
    {
        public const int LowThreshold = 35;

        Image fill;
        Color normalColor;
        UiShake shake;
        bool low;
        float nextNudge;

        static readonly Color LowColor = new Color(0.90f, 0.22f, 0.20f, 1f);

        public bool IsLow => low;

        public void Bind(Image fillImage, Color color, UiShake shaker)
        {
            fill = fillImage;
            normalColor = color;
            shake = shaker;
        }

        /// <summary>value 0..100. Az önce düşük seviyeye indiyse true döner.</summary>
        public bool SetValue(int value)
        {
            if (fill == null)
                return false;
            float amount = Mathf.Clamp01(value / 100f);
            // Tamamen boş kap da biraz görünsün; çocuk ikonun ne olduğunu tanısın.
            fill.fillAmount = Mathf.Lerp(0.08f, 1f, amount);
            bool wasLow = low;
            low = value < LowThreshold;
            fill.color = low ? LowColor : normalColor;
            if (low && !wasLow)
            {
                Nudge();
                return true;
            }
            return false;
        }

        public void Nudge()
        {
            shake?.Play();
            nextNudge = Time.unscaledTime + 4f;
        }

        void Update()
        {
            if (low && Time.unscaledTime >= nextNudge)
                Nudge();
        }
    }
}
