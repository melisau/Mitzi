using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Audio;

namespace PawPath.Levels
{
    public class DogObstacle : MonoBehaviour
    {
        SpriteRenderer renderer;
        Sprite[] frames;
        float speed, leftBound, groundY, frameTimer;
        int frameIndex;
        bool failed;

        public void Configure(SpriteRenderer target, Sprite[] animationFrames, float runSpeed,
            float minX, float roadSurfaceY)
        {
            renderer = target;
            frames = animationFrames;
            speed = runSpeed;
            leftBound = minX;
            groundY = roadSurfaceY;
            AlignFeetToGround();
        }

        void Update()
        {
            transform.position += Vector3.left * (speed * Time.deltaTime);
            if (transform.position.x < leftBound)
            {
                Destroy(gameObject);
                return;
            }
            if (frames == null || frames.Length == 0 || renderer == null) return;
            frameTimer += Time.deltaTime;
            if (frameTimer < 0.055f) return;
            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            renderer.sprite = frames[frameIndex];
            AlignFeetToGround();
        }

        void AlignFeetToGround()
        {
            if (renderer == null || renderer.sprite == null) return;
            float scaleY = Mathf.Abs(transform.lossyScale.y);
            var position = transform.position;
            // Karelerde patilerin altında şeffaf pay bulunduğu için sprite sınırını
            // yol yüzeyinin biraz altına göm; koşarken havada süzülmez.
            position.y = groundY - renderer.sprite.bounds.min.y * scaleY - 0.42f;
            transform.position = position;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var cat = other.GetComponent<CatController>();
            if (failed || cat == null || cat.IsClimbing) return;
            failed = true;
            CozyAudioManager.Instance?.PlayHurt();
            GameFlow.Instance?.ShowLevelFailure("Koşan köpeğe yakalandın. Üzerinden zıpla veya ağaca tırman!");
        }
    }
}
