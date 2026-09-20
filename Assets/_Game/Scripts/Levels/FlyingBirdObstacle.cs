using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Audio;

namespace PawPath.Levels
{
    public class FlyingBirdObstacle : MonoBehaviour
    {
        SpriteRenderer renderer;
        Sprite[] frames;
        float speed;
        float leftBound;
        float rightBound;
        float baseY;
        float bobPhase;
        float frameTimer;
        int frameIndex;

        public void Configure(SpriteRenderer target, Sprite[] animationFrames, float flightSpeed,
            float minX, float maxX, float phase)
        {
            renderer = target;
            frames = animationFrames;
            speed = flightSpeed;
            leftBound = minX;
            rightBound = maxX;
            baseY = transform.position.y;
            bobPhase = phase;
        }

        void Update()
        {
            transform.position += Vector3.left * (speed * Time.deltaTime);
            if (transform.position.x < leftBound)
                transform.position = new Vector3(rightBound, baseY, transform.position.z);
            var p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * 2.2f + bobPhase) * 0.10f;
            transform.position = p;

            if (frames == null || frames.Length == 0 || renderer == null)
                return;
            frameTimer += Time.deltaTime;
            if (frameTimer >= 0.11f)
            {
                frameTimer = 0f;
                frameIndex = (frameIndex + 1) % frames.Length;
                renderer.sprite = frames[frameIndex];
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponent<CatController>() == null)
                return;
            CozyAudioManager.Instance?.PlayHurt();
            GameFlow.Instance?.ShowLevelFailure("Uçan kuşa çarptın. Uçuş yüksekliğini izle ve doğru zamanda geç!");
        }
    }
}
