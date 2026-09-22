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
        float speed, leftBound, baseY, bobPhase, frameTimer, visualWidth;
        int frameIndex;
        bool collected;

        public void Configure(SpriteRenderer target, Sprite[] animationFrames, float flightSpeed,
            float minX, float phase, float targetVisualWidth)
        {
            renderer = target;
            frames = animationFrames;
            speed = flightSpeed;
            leftBound = minX;
            baseY = transform.position.y;
            bobPhase = phase;
            visualWidth = targetVisualWidth;
            ApplyFrameScale();
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
            ApplyFrameScale();
        }

        void ApplyFrameScale()
        {
            if (renderer == null || renderer.sprite == null || renderer.sprite.bounds.size.x <= 0f)
                return;
            float scale = visualWidth / renderer.sprite.bounds.size.x;
            transform.localScale = new Vector3(scale, scale, 1f);
            Transform reward = transform.Find("RewardLabel");
            if (reward != null)
                reward.localScale = new Vector3(1f / scale, 1f / scale, 1f);
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
