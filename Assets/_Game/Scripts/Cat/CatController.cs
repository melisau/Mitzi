using System.Collections;
using UnityEngine;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Economy;
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
        [SerializeField] float moveSpeed = 2.8f;
        [SerializeField] float iceSpeedMultiplier = 1.65f;
        [SerializeField] float bounceForce = 7.2f;
        [SerializeField] float walkAnimationSpeed = 1.35f;
        [SerializeField] float groundedProbe = 0.28f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float airGrace = 0.45f;
        [SerializeField] float visualHeight = 1.1f;

        [Header("Kurtarma")]
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
                // Sprite zaten kendi rengini içeriyor; furTint ile çarpmak görseli koyulaştırıyordu.
                sprite.color = Color.white;
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

            bool grounded = IsGrounded(out var surface);
            if (grounded)
            {
                airTimer = 0f;
                float speed = surface == PathSurfaceType.Ice ? moveSpeed * iceSpeedMultiplier : moveSpeed;
                body.velocity = new Vector2(facing * speed, body.velocity.y);
                if (animator != null)
                    animator.speed = walkAnimationSpeed;
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

        bool IsGrounded(out PathSurfaceType surface)
        {
            surface = PathSurfaceType.Normal;
            var hits = Physics2D.CircleCastAll(transform.position, 0.12f, Vector2.down, groundedProbe, groundMask);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null)
                    continue;
                if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform))
                    continue;
                var path = hits[i].collider.GetComponent<PathSurface>();
                if (path != null)
                    surface = path.Type;
                return true;
            }
            return false;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var path = collision.collider.GetComponent<PathSurface>();
            if (path == null || busy)
                return;

            if (path.Type == PathSurfaceType.Bounce)
            {
                body.velocity = new Vector2(body.velocity.x, bounceForce);
            }
            else if (path.Type == PathSurfaceType.Hazard)
            {
                StartCoroutine(HazardRestartRoutine());
            }
        }

        IEnumerator HazardRestartRoutine()
        {
            busy = true;
            body.velocity = Vector2.zero;
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.RemoveLove(10, "Kırmızı yola temas");
            yield return new WaitForSeconds(0.35f);
            busy = false;
            if (LevelManager.Instance != null)
                LevelManager.Instance.BeginCurrentLevel();
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
                bubbleRenderer.enabled = false;

            yield return new WaitForSeconds(0.2f);

            transform.position = spawnPosition;
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
