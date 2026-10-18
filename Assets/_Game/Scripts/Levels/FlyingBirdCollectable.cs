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
        SpriteRenderer glow;
        bool collected;

        public void Configure(SpriteRenderer target, Sprite[] animationFrames, float flightSpeed,
            float minX, float phase, float targetVisualWidth)
        {
            renderer = target;
            // Source bird frames face right; this bird travels left.
            if (renderer != null) renderer.flipX = true;
            frames = animationFrames;
            speed = flightSpeed;
            leftBound = minX;
            baseY = transform.position.y;
            bobPhase = phase;
            visualWidth = targetVisualWidth;
            var glowTransform = transform.Find("Glow");
            glow = glowTransform != null ? glowTransform.GetComponent<SpriteRenderer>() : null;
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
            // Belirgin süzülme + nabız atan ışık: "bunu yakala" mesajı.
            position.y = baseY + Mathf.Sin(Time.time * 2.2f + bobPhase) * 0.18f;
            transform.position = position;
            if (glow != null)
            {
                float pulse = (Mathf.Sin(Time.time * 3.4f + bobPhase) + 1f) * 0.5f;
                var color = glow.color;
                color.a = Mathf.Lerp(0.50f, 0.85f, pulse);
                glow.color = color;
                float parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.localScale.x));
                float size = visualWidth * Mathf.Lerp(0.82f, 0.95f, pulse) / parentScale;
                glow.transform.localScale = new Vector3(size, size, 1f);
            }

            if (frames == null || frames.Length == 0 || renderer == null) return;
            frameTimer += Time.deltaTime;
            const float frameInterval = 1f / 24f;
            if (frameTimer < frameInterval) return;
            int advance = Mathf.FloorToInt(frameTimer / frameInterval);
            frameTimer -= advance * frameInterval;
            frameIndex = (frameIndex + advance) % frames.Length;
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
            if (glow != null)
            {
                float size = visualWidth * 0.9f / scale;
                glow.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponentInParent<CatController>() == null) return;
            collected = true;
            CozyEconomyManager.Instance?.AddLove(3, "Kuş ödülü");
            CozyAudioManager.Instance?.PlayConfirm();
            gameObject.SetActive(false);
        }
    }
}
