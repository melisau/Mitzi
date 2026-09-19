using System.Collections;
using UnityEngine;
using PawPath.Audio;
using PawPath.Core;
using PawPath.Data;
using PawPath.Levels;

namespace PawPath.Cat
{
    /// <summary>
    /// Çizilen EdgeCollider yollarında Rigidbody2D ile yürür.
    /// Düşünce hasar yok: pembe baloncuk içinde spawn noktasına döner.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class CatController : MonoBehaviour
    {
        public static CatController Instance { get; private set; }

        [Header("Yürüyüş")]
        [SerializeField] float moveSpeed = 1.8f;
        [SerializeField] float groundedProbe = 0.28f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float airGrace = 0.45f;
        [SerializeField] float visualHeight = 1.1f;

        [Header("Kurtarma")]
        [SerializeField] float bubbleRise = 1.6f;
        [SerializeField] float returnDuration = 1.35f;
        [SerializeField] SpriteRenderer bubbleRenderer;
        [SerializeField] Transform visual;

        Rigidbody2D body;
        SpriteRenderer sprite;
        Animator animator;
        Vector3 spawnPosition;
        bool busy;
        float airTimer;
        CatDefinition definition;
        int facing = 1;

        public bool IsBusy => busy;
        public CatDefinition Definition => definition;

        void Awake()
        {
            Instance = this;
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.gravityScale = 2.4f;
            sprite = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
            animator = visual != null ? visual.GetComponent<Animator>() : GetComponentInChildren<Animator>();
            if (bubbleRenderer != null)
                bubbleRenderer.enabled = false;
        }

        public void ApplyCat(CatDefinition cat)
        {
            definition = cat;
            if (sprite != null && cat != null)
            {
                if (cat.idleSprite != null)
                    sprite.sprite = cat.idleSprite;
                sprite.color = cat.furTint;
                FitVisualToHeight();
            }

            if (animator != null)
            {
                animator.runtimeAnimatorController = cat != null ? cat.animator : null;
                animator.enabled = animator.runtimeAnimatorController != null;
            }
        }

        void FitVisualToHeight()
        {
            if (visual == null || sprite == null || sprite.sprite == null || sprite.sprite.bounds.size.y <= 0f)
                return;

            float scale = visualHeight / sprite.sprite.bounds.size.y;
            visual.localScale = new Vector3(scale * facing, scale, 1f);
        }

        public void PlaceAtSpawn(Vector2 world)
        {
            spawnPosition = world;
            transform.position = world;
            body.velocity = Vector2.zero;
            busy = false;
            airTimer = 0f;
            if (bubbleRenderer != null)
                bubbleRenderer.enabled = false;
        }

        void FixedUpdate()
        {
            if (busy)
                return;
            if (GameFlow.Instance != null && GameFlow.Instance.InHub)
            {
                body.velocity = Vector2.zero;
                return;
            }

            bool grounded = IsGrounded();
            if (grounded)
            {
                airTimer = 0f;
                body.velocity = new Vector2(facing * moveSpeed, body.velocity.y);
                if (animator != null)
                    animator.speed = 1f;
            }
            else
            {
                airTimer += Time.fixedDeltaTime;
                if (animator != null)
                    animator.speed = 0f;
            }

            if (visual != null)
            {
                var s = visual.localScale;
                s.x = Mathf.Abs(s.x) * facing;
                visual.localScale = s;
            }

            var level = LevelManager.Instance != null ? LevelManager.Instance.Current : null;
            float fallY = level != null ? level.fallY : -7.5f;
            if (transform.position.y < fallY || airTimer > airGrace + 1.8f)
                StartCoroutine(RescueRoutine());
        }

        bool IsGrounded()
        {
            var hits = Physics2D.CircleCastAll(transform.position, 0.12f, Vector2.down, groundedProbe, groundMask);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null)
                    continue;
                if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform))
                    continue;
                return true;
            }
            return false;
        }

        public void FlipTowards(Vector2 target)
        {
            facing = target.x >= transform.position.x ? 1 : -1;
        }

        IEnumerator RescueRoutine()
        {
            if (busy)
                yield break;

            busy = true;
            GameEvents.CatFell();
            body.velocity = Vector2.zero;
            body.simulated = false;
            if (bubbleRenderer != null)
                bubbleRenderer.enabled = true;
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayBubble();

            float t = 0f;
            Vector3 start = transform.position;
            Vector3 hover = start + Vector3.up * bubbleRise;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, hover, t / 0.45f);
                yield return null;
            }

            t = 0f;
            Vector3 from = transform.position;
            while (t < returnDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / returnDuration);
                transform.position = Vector3.Lerp(from, spawnPosition, k);
                yield return null;
            }

            transform.position = spawnPosition;
            if (bubbleRenderer != null)
                bubbleRenderer.enabled = false;
            body.simulated = true;
            body.velocity = Vector2.zero;
            airTimer = 0f;
            busy = false;
            GameEvents.CatRescued();
        }

        public void BindVisual(Transform vis, SpriteRenderer bubble)
        {
            visual = vis;
            bubbleRenderer = bubble;
            sprite = vis != null ? vis.GetComponent<SpriteRenderer>() : sprite;
            animator = vis != null ? vis.GetComponent<Animator>() : animator;
        }
    }
}
