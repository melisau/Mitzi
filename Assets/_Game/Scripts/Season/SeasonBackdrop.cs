using UnityEngine;
using PawPath.Data;

namespace PawPath.Season
{
    /// <summary>
    /// Yaz / sonbahar / kış arka plan tonları. Sprite'lar gelince color yerine sprite swap yapılır.
    /// </summary>
    public class SeasonBackdrop : MonoBehaviour
    {
        [SerializeField] SpriteRenderer sky;
        [SerializeField] SpriteRenderer ground;
        [SerializeField] ParticleSystem weather;

        static readonly Color SummerSky = new Color(0.78f, 0.90f, 0.86f);
        static readonly Color SummerGround = new Color(0.72f, 0.84f, 0.62f);
        static readonly Color AutumnSky = new Color(0.96f, 0.84f, 0.68f);
        static readonly Color AutumnGround = new Color(0.78f, 0.52f, 0.32f);
        static readonly Color WinterSky = new Color(0.82f, 0.86f, 0.93f);
        static readonly Color WinterGround = new Color(0.93f, 0.95f, 0.98f);

        public void Apply(SeasonId season)
        {
            switch (season)
            {
                case SeasonId.Autumn:
                    Tint(AutumnSky, AutumnGround);
                    break;
                case SeasonId.Winter:
                    Tint(WinterSky, WinterGround);
                    break;
                default:
                    Tint(SummerSky, SummerGround);
                    break;
            }

            if (weather == null)
                return;
            var emission = weather.emission;
            emission.enabled = season != SeasonId.Summer;
        }

        void Tint(Color skyColor, Color groundColor)
        {
            if (sky != null)
                sky.color = sky.sprite != null ? Color.white : skyColor;
            if (ground != null)
                ground.color = groundColor;
        }

        public void Bind(SpriteRenderer skyRend, SpriteRenderer groundRend, ParticleSystem fx)
        {
            sky = skyRend;
            ground = groundRend;
            weather = fx;
        }
    }
}
