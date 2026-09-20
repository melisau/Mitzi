using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Audio;

namespace PawPath.Levels
{
    public enum MovingHazardMotion { Horizontal, Vertical, Swing, Roll }

    /// <summary>Tuşlu moda özel, önceden görülebilen ve düzenli hareket eden engel.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class MovingLevelHazard : MonoBehaviour
    {
        MovingHazardMotion motion;
        Vector2 origin;
        float distance;
        float speed;
        float phase;

        public void Configure(MovingHazardMotion value, float travel, float movementSpeed, float startPhase = 0f)
        {
            motion = value;
            origin = transform.position;
            distance = travel;
            speed = movementSpeed;
            phase = startPhase;
        }

        void FixedUpdate()
        {
            float wave = Mathf.Sin(Time.time * speed + phase);
            switch (motion)
            {
                case MovingHazardMotion.Horizontal:
                    transform.position = origin + Vector2.right * (wave * distance);
                    break;
                case MovingHazardMotion.Vertical:
                    transform.position = origin + Vector2.up * ((wave + 1f) * 0.5f * distance);
                    break;
                case MovingHazardMotion.Swing:
                    transform.rotation = Quaternion.Euler(0f, 0f, wave * distance);
                    break;
                case MovingHazardMotion.Roll:
                    transform.position = origin + Vector2.right * (wave * distance);
                    transform.rotation = Quaternion.Euler(0f, 0f, -Time.time * speed * 120f);
                    break;
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponent<CatController>() == null)
                return;
            CozyAudioManager.Instance?.PlayHurt();
            GameFlow.Instance?.ShowLevelFailure("Hareketli engele çarptın. Hareket düzenini izle ve doğru zamanda geç!");
        }
    }
}
