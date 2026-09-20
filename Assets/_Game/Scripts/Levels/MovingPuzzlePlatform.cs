using UnityEngine;

namespace PawPath.Levels
{
    /// <summary>Çizilmiş rampanın bağlanabileceği, yavaş ve öngörülebilir platform.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class MovingPuzzlePlatform : MonoBehaviour
    {
        Vector2 start;
        Vector2 end;
        float speed;
        Rigidbody2D body;

        public void Configure(Vector2 from, Vector2 to, float movementSpeed)
        {
            start = from;
            end = to;
            speed = movementSpeed;
            transform.position = from;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void FixedUpdate()
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            body.MovePosition(Vector2.Lerp(start, end, t));
        }
    }
}
