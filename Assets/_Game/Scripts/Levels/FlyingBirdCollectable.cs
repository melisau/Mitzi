using UnityEngine;
using PawPath.Cat;
using PawPath.Economy;
using PawPath.Audio;

namespace PawPath.Levels
{
    /// <summary>Uçuş animasyonunu oynatır ve temas edildiğinde Sevgi ödülü verir.</summary>
    public class FlyingBirdCollectable : MonoBehaviour
    {
        SpriteRenderer renderer;
        Sprite[] frames;
        float speed, leftBound, baseY, bobPhase, frameTimer;
        int frameIndex;
        bool collected;

        public void Configure(SpriteRenderer target, Sprite[] animationFrames, float flightSpeed,
            float minX, float phase)
        {
            renderer = target;
            frames = animationFrames;
            speed = flightSpeed;
            leftBound = minX;
            baseY = transform.position.y;
            bobPhase = phase;
        }

        void Update()
        {
            transform.position += Vector3.left * (speed * Time.deltaTime);
            if (transform.position.x < leftBound)
            {
                Destroy(gameObject);
                return;
            }
            var position = transform.position;
            position.y = baseY + Mathf.Sin(Time.time * 2.2f + bobPhase) * 0.10f;
            transform.position = position;

            if (frames == null || frames.Length == 0 || renderer == null) return;
            frameTimer += Time.deltaTime;
            if (frameTimer < 0.11f) return;
            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            renderer.sprite = frames[frameIndex];
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponent<CatController>() == null) return;
            collected = true;
            CozyEconomyManager.Instance?.AddLove(3, "Kuş ödülü");
            CozyAudioManager.Instance?.PlayConfirm();
            gameObject.SetActive(false);
        }
    }
}
