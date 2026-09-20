using UnityEngine;
using PawPath.Data;

namespace PawPath.Hub
{
    /// <summary>Mitzi'nin yatış ve uyanış karelerini zemine sabit biçimde oynatır.</summary>
    public class MitziSleepAnimator : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        Animator animator;
        CatHomeBehaviour behaviour;
        Sprite awakeSprite;
        Sprite[] frames;
        float targetWidth;
        float baselineBottom;
        float transitionTime;
        bool wasSleeping;
        Vector3 awakeScale;
        Vector3 awakePosition;

        public void Bind(CatDefinition cat, SpriteRenderer renderer, CatHomeBehaviour homeBehaviour)
        {
            spriteRenderer = renderer;
            behaviour = homeBehaviour;
            animator = renderer != null ? renderer.GetComponent<Animator>() : null;
            awakeSprite = renderer != null ? renderer.sprite : null;
            frames = cat != null ? cat.sleepFrames : null;
            if (spriteRenderer != null && awakeSprite != null)
            {
                awakeScale = spriteRenderer.transform.localScale;
                awakePosition = spriteRenderer.transform.localPosition;
                targetWidth = awakeSprite.bounds.size.x * Mathf.Abs(awakeScale.x);
                baselineBottom = spriteRenderer.transform.localPosition.y +
                    awakeSprite.bounds.min.y * Mathf.Abs(spriteRenderer.transform.localScale.y);
            }
        }

        void Update()
        {
            if (spriteRenderer == null || behaviour == null || frames == null || frames.Length == 0)
                return;
            bool sleeping = behaviour.CurrentActivity == ResidentActivity.Sleeping;
            if (sleeping != wasSleeping)
            {
                wasSleeping = sleeping;
                transitionTime = 0f;
                if (sleeping)
                {
                    if (animator != null) animator.enabled = false;
                    spriteRenderer.sprite = awakeSprite;
                }
                else
                {
                    if (animator != null) animator.enabled = false;
                    spriteRenderer.sprite = awakeSprite;
                    var crouched = awakeScale;
                    crouched.x *= 1.08f;
                    crouched.y *= 0.72f;
                    spriteRenderer.transform.localScale = crouched;
                    spriteRenderer.transform.localPosition = awakePosition + Vector3.down * 0.18f;
                }
            }

            transitionTime += Time.deltaTime;
            if (sleeping)
            {
                // İlk 0.7 saniyede ayakta poz gerçek zamanlı olarak çömelip alçalır.
                if (transitionTime < 0.70f)
                {
                    float t = Mathf.SmoothStep(0f, 1f, transitionTime / 0.70f);
                    var crouched = awakeScale;
                    crouched.x *= Mathf.Lerp(1f, 1.08f, t);
                    crouched.y *= Mathf.Lerp(1f, 0.72f, t);
                    spriteRenderer.transform.localScale = crouched;
                    spriteRenderer.transform.localPosition = Vector3.Lerp(awakePosition,
                        awakePosition + Vector3.down * 0.18f, t);
                }
                else if (transitionTime < 1.18f)
                    ApplyFrame(frames[0]);
                else
                    ApplyFrame(frames[frames.Length - 1]);
            }
            else
            {
                float t = Mathf.Clamp01(transitionTime / 0.52f);
                spriteRenderer.transform.localScale = Vector3.Lerp(spriteRenderer.transform.localScale, awakeScale, t);
                spriteRenderer.transform.localPosition = Vector3.Lerp(spriteRenderer.transform.localPosition, awakePosition, t);
                if (t >= 1f)
                {
                    spriteRenderer.sprite = awakeSprite;
                    RestoreAwake();
                }
            }
        }

        void ApplyFrame(Sprite frame)
        {
            if (frame == null || frame.bounds.size.y <= 0f)
                return;
            spriteRenderer.sprite = frame;
            // Yatan kediyi ayakta pozun yüksekliğine büyütmek yerine gövde uzunluğunu
            // koru; böylece uyku pozu evde dev bir görsel olarak belirmez.
            float scale = targetWidth / frame.bounds.size.x;
            var current = spriteRenderer.transform.localScale;
            spriteRenderer.transform.localScale = new Vector3(Mathf.Sign(current.x) * scale, scale, 1f);
            var position = spriteRenderer.transform.localPosition;
            position.y = baselineBottom - frame.bounds.min.y * scale;
            spriteRenderer.transform.localPosition = position;
        }

        void RestoreAwake()
        {
            spriteRenderer.sprite = awakeSprite;
            spriteRenderer.transform.localScale = awakeScale;
            spriteRenderer.transform.localPosition = awakePosition;
            if (animator != null)
                animator.enabled = animator.runtimeAnimatorController != null;
        }
    }
}
