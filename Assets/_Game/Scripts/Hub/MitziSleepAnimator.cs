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
        bool initialized;
        bool transitioning;
        bool externalAnimationActive;
        Vector3 awakeScale;
        Vector3 awakePosition;
        const float FrameDuration = 0.18f;

        public void Bind(CatDefinition cat, SpriteRenderer renderer, CatHomeBehaviour homeBehaviour)
        {
            spriteRenderer = renderer;
            behaviour = homeBehaviour;
            animator = renderer != null ? renderer.GetComponent<Animator>() : null;
            awakeSprite = renderer != null ? renderer.sprite : null;
            frames = cat != null ? cat.sleepFrames : null;
            if (spriteRenderer != null && awakeSprite != null)
            {
                // Animator ilk etkinleştiğinde prefab ölçeğini 1'e çekebilir. Evdeki
                // bütün kedilerle aynı kesin yüksekliği sprite ölçüsünden yeniden kur.
                float normalizedScale = 2.20f / awakeSprite.bounds.size.y;
                float direction = Mathf.Sign(spriteRenderer.transform.localScale.x);
                if (Mathf.Approximately(direction, 0f)) direction = 1f;
                awakeScale = new Vector3(direction * normalizedScale, normalizedScale, 1f);
                spriteRenderer.transform.localScale = awakeScale;
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
            // Mama yeme gibi özel bir animasyon aynı SpriteRenderer'ı kullanırken
            // uyanık pozu her karede geri yükleyip animasyonu ezme.
            if (externalAnimationActive)
                return;
            bool sleeping = behaviour.CurrentActivity == ResidentActivity.Sleeping;
            if (!initialized)
            {
                initialized = true;
                wasSleeping = sleeping;
                transitionTime = 0f;
                transitioning = sleeping;
                if (sleeping)
                {
                    if (animator != null) animator.enabled = false;
                    spriteRenderer.sprite = awakeSprite;
                }
                else
                    RestoreAwake();
                return;
            }
            if (sleeping != wasSleeping)
            {
                wasSleeping = sleeping;
                transitionTime = 0f;
                transitioning = true;
                if (sleeping)
                {
                    if (animator != null) animator.enabled = false;
                    spriteRenderer.sprite = awakeSprite;
                }
                else
                {
                    if (animator != null) animator.enabled = false;
                    ApplyFrame(frames[frames.Length - 1]);
                }
            }

            transitionTime += Time.deltaTime;
            if (sleeping)
            {
                int index = Mathf.Min(frames.Length - 1,
                    Mathf.FloorToInt(transitionTime / FrameDuration));
                ApplyFrame(frames[index]);
            }
            else
            {
                if (!transitioning)
                {
                    RestoreAwake();
                    return;
                }
                int step = Mathf.FloorToInt(transitionTime / FrameDuration);
                int index = frames.Length - 1 - step;
                if (index >= 0)
                    ApplyFrame(frames[index]);
                else
                {
                    RestoreAwake();
                    transitioning = false;
                }
            }
        }

        public void SetExternalAnimation(bool active)
        {
            externalAnimationActive = active;
            if (active)
                transitioning = false;
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
            // CatHomeBehaviour.Face yürüyüş yönüne göre X işaretini değiştirir.
            // Uyanık kareyi yenilerken bu işareti ilk yöne sıfırlamak Mitzi'nin
            // geri geri yürümesine neden oluyordu; yalnızca boyutu geri yükle.
            float direction = Mathf.Sign(spriteRenderer.transform.localScale.x);
            if (Mathf.Approximately(direction, 0f))
                direction = Mathf.Sign(awakeScale.x);
            spriteRenderer.transform.localScale = new Vector3(
                direction * Mathf.Abs(awakeScale.x), awakeScale.y, awakeScale.z);
            spriteRenderer.transform.localPosition = awakePosition;
            if (animator != null)
                animator.enabled = animator.runtimeAnimatorController != null;
        }
    }
}
