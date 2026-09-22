using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using PawPath.Cat;
using PawPath.Content;
using PawPath.Core;

namespace PawPath.Hub
{
    public class FurnitureInteraction : MonoBehaviour
    {
        ShopItemDefinition item;
        bool running;
        Collider2D hitbox;

        public void Configure(ShopItemDefinition definition)
        {
            item = definition;
            hitbox = GetComponent<Collider2D>();
        }

        void Update()
        {
            if (running || item == null || item.interactionType == ItemInteractionType.None ||
                GameFlow.Instance == null || !GameFlow.Instance.InHub || HomeEditMode.Active)
                return;
            bool pressed = Input.touchCount > 0
                ? Input.GetTouch(0).phase == TouchPhase.Began
                : Input.GetMouseButtonDown(0);
            if (!pressed || PointerOverUi()) return;
            var camera = Camera.main;
            if (camera == null) return;
            Vector3 screen = Input.touchCount > 0 ? (Vector3)Input.GetTouch(0).position : Input.mousePosition;
            screen.z = Mathf.Abs(transform.position.z - camera.transform.position.z);
            Vector2 world = camera.ScreenToWorldPoint(screen);
            if (hitbox == null) hitbox = GetComponent<Collider2D>();
            if (hitbox != null && hitbox.enabled && hitbox.OverlapPoint(world))
                TryInteract();
        }

        void OnMouseDown() => TryInteract();

        void TryInteract()
        {
            if (running || item == null || item.interactionType == ItemInteractionType.None ||
                HomeEditMode.Active || GameFlow.Instance == null || !GameFlow.Instance.InHub) return;
            var cats = FindObjectsOfType<CatHomeBehaviour>();
            if (cats.Length == 0) return;
            CatHomeBehaviour nearest = null;
            if (item.interactionType == ItemInteractionType.Eating ||
                item.interactionType == ItemInteractionType.Climbing)
            {
                string selectedId = SaveService.Data != null ? SaveService.Data.selectedCatId : "mitzi";
                foreach (var candidate in cats)
                    if (candidate.Definition != null && candidate.Definition.id == selectedId)
                    {
                        nearest = candidate;
                        break;
                    }
            }
            if (nearest == null) nearest = cats[0];
            float best = Vector2.SqrMagnitude(nearest.transform.position - transform.position);
            if (item.interactionType != ItemInteractionType.Eating &&
                item.interactionType != ItemInteractionType.Climbing)
            {
                for (int i = 0; i < cats.Length; i++)
                {
                    float distance = Vector2.SqrMagnitude(cats[i].transform.position - transform.position);
                    if (distance < best) { best = distance; nearest = cats[i]; }
                }
            }
            StartCoroutine(Interact(nearest));
        }

        static bool PointerOverUi()
        {
            if (EventSystem.current == null) return false;
            return Input.touchCount > 0
                ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        IEnumerator Interact(CatHomeBehaviour cat)
        {
            running = true;
            var activity = item.interactionType == ItemInteractionType.Eating
                ? ResidentActivity.Eating
                : item.interactionType == ItemInteractionType.Sleeping
                    ? ResidentActivity.Sleeping : ResidentActivity.Playing;
            Vector2 destination = transform.position;
            if (item.interactionType == ItemInteractionType.Eating)
            {
                // Seçilen kedi kabın dibine gelir; çevredeki diğer kediler ona
                // alan açmak için geçici olarak uzaklaşır.
                foreach (var other in FindObjectsOfType<CatHomeBehaviour>())
                    if (other != cat && Vector2.Distance(other.transform.position, transform.position) < 2.0f)
                        other.MoveAwayFrom(transform.position, 2.15f);
                float side = cat.transform.position.x <= transform.position.x ? -1f : 1f;
                destination.x += side * 0.48f;
                destination.y += 0.04f;
                cat.WalkDirectTo(destination, activity);
            }
            else if (item.interactionType == ItemInteractionType.Climbing)
            {
                float side = cat.transform.position.x <= transform.position.x ? -1f : 1f;
                destination.x += side * 0.58f;
                destination.y -= 0.10f;
                cat.WalkDirectTo(destination, activity);
            }
            else
                cat.WalkTo(destination, activity);
            float timeout = 8f;
            while (timeout > 0f && cat.CurrentActivity == ResidentActivity.Walking)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (item.interactionAnimation != null)
            {
                var animator = cat.GetComponentInChildren<Animator>();
                if (animator != null) animator.Play(item.interactionAnimation.name, 0, 0f);
            }
            if (item.interactionType == ItemInteractionType.Eating &&
                cat.Definition != null && cat.Definition.id == "mitzi" &&
                item.interactionFrames != null && item.interactionFrames.Length > 0)
            {
                // Coroutine zorla kesilmez; son kareden sonra sprite, ölçek ve
                // Animator her durumda normal haline geri döner.
                yield return PlayInteractionFrames(cat, item.interactionFrames, 2.4f);
                CatNeedsSystem.Instance?.FeedCat(cat.Definition.id);
                running = false;
                yield break;
            }
            if (item.interactionType == ItemInteractionType.Climbing)
            {
                Sprite[] climbFrames = cat.Definition != null && cat.Definition.id == "mitzi"
                    ? item.interactionFrames : null;
                yield return PlaySmallHomeClimb(cat, climbFrames);
                CatNeedsSystem.Instance?.RewardInteraction(3, "Kedi ağacına tırmanma");
                running = false;
                yield break;
            }
            if (item.interactionType == ItemInteractionType.Eating)
                CatNeedsSystem.Instance?.FeedCat(cat.Definition != null ? cat.Definition.id : null);
            else if (item.interactionType == ItemInteractionType.Sleeping)
                CatNeedsSystem.Instance?.PutToSleep();
            else if (item.interactionType == ItemInteractionType.Playing)
                CatNeedsSystem.Instance?.RewardInteraction(5, "Oyuncakla oynama");
            yield return new WaitForSeconds(2f);
            running = false;
        }

        IEnumerator PlayInteractionFrames(CatHomeBehaviour cat, Sprite[] frames, float duration)
        {
            var renderer = cat.GetComponentInChildren<SpriteRenderer>();
            var animator = cat.GetComponentInChildren<Animator>();
            if (renderer == null) yield break;
            Sprite previous = renderer.sprite;
            Transform visual = renderer.transform;
            Vector3 previousScale = visual.localScale;
            Vector3 previousPosition = visual.localPosition;
            float displayedWidth = previous != null
                ? previous.bounds.size.x * Mathf.Abs(previousScale.x) : 2.20f;
            float baselineBottom = previous != null
                ? previousPosition.y + previous.bounds.min.y * Mathf.Abs(previousScale.y)
                : previousPosition.y;
            bool animatorWasEnabled = animator != null && animator.enabled;
            var sleepAnimator = cat.GetComponent<MitziSleepAnimator>();
            if (sleepAnimator != null) sleepAnimator.SetExternalAnimation(true);
            if (animator != null) animator.enabled = false;
            float elapsed = 0f;
            int frame = 0;
            while (elapsed < duration)
            {
                if (frames[frame] != null)
                {
                    // Her kaynak görselin boşluk/bounds değeri farklıdır. Dünya
                    // üzerindeki görünen genişliği sabitleyerek büyüme-küçülmeyi önle.
                    float frameScale = frames[frame].bounds.size.x > 0f
                        ? displayedWidth / frames[frame].bounds.size.x
                        : Mathf.Abs(previousScale.y);
                    renderer.sprite = frames[frame];
                    visual.localScale = new Vector3(
                        Mathf.Sign(previousScale.x) * frameScale, frameScale, 1f);
                    var framePosition = visual.localPosition;
                    framePosition.y = baselineBottom - frames[frame].bounds.min.y * frameScale;
                    visual.localPosition = framePosition;
                }
                yield return new WaitForSeconds(0.16f);
                elapsed += 0.16f;
                frame = (frame + 1) % frames.Length;
            }
            renderer.sprite = cat.Definition != null && cat.Definition.idleSprite != null
                ? cat.Definition.idleSprite : previous;
            visual.localScale = previousScale;
            visual.localPosition = previousPosition;
            if (animator != null) animator.enabled = animatorWasEnabled;
            if (sleepAnimator != null) sleepAnimator.SetExternalAnimation(false);
        }

        IEnumerator PlaySmallHomeClimb(CatHomeBehaviour cat, Sprite[] frames)
        {
            var renderer = cat.GetComponentInChildren<SpriteRenderer>();
            var animator = cat.GetComponentInChildren<Animator>();
            if (renderer == null) yield break;
            var sleepAnimator = cat.GetComponent<MitziSleepAnimator>();
            Sprite previous = renderer.sprite;
            Vector3 previousScale = renderer.transform.localScale;
            Vector3 start = cat.transform.position;
            float displayedWidth = previous != null
                ? previous.bounds.size.x * Mathf.Abs(previousScale.x) : 2.2f;
            bool animatorWasEnabled = animator != null && animator.enabled;
            if (sleepAnimator != null) sleepAnimator.SetExternalAnimation(true);
            if (animator != null) animator.enabled = false;

            const float riseHeight = 0.58f;
            const float phaseDuration = 0.72f;
            float elapsed = 0f;
            while (elapsed < phaseDuration * 2f)
            {
                bool descending = elapsed >= phaseDuration;
                float phase = (descending ? elapsed - phaseDuration : elapsed) / phaseDuration;
                float height = descending ? 1f - phase : phase;
                cat.transform.position = start + Vector3.up * (Mathf.SmoothStep(0f, 1f, height) * riseHeight);

                if (frames != null && frames.Length > 0)
                {
                    int index = Mathf.Clamp(Mathf.FloorToInt(phase * frames.Length), 0, frames.Length - 1);
                    if (descending) index = frames.Length - 1 - index;
                    Sprite frame = frames[index];
                    if (frame != null)
                    {
                        renderer.sprite = frame;
                        float scale = frame.bounds.size.x > 0f ? displayedWidth / frame.bounds.size.x : Mathf.Abs(previousScale.y);
                        renderer.transform.localScale = new Vector3(Mathf.Sign(previousScale.x) * scale, scale, 1f);
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            cat.transform.position = start;
            renderer.sprite = previous;
            renderer.transform.localScale = previousScale;
            if (animator != null) animator.enabled = animatorWasEnabled;
            if (sleepAnimator != null) sleepAnimator.SetExternalAnimation(false);
        }
    }
}
