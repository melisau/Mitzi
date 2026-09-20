using System.Collections;
using UnityEngine;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Economy;
using PawPath.Levels;
using PawPath.Gameplay;
using PawPath.Audio;

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
        [SerializeField] float moveSpeed = 2.2f;
        [SerializeField] float iceSpeedMultiplier = 1.65f;
        [SerializeField] float bounceForce = 7.2f;
        [SerializeField] float walkAnimationSpeed = 1.75f;
        [SerializeField] float groundedProbe = 0.336f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float airGrace = 0.45f;
        [SerializeField] float visualHeight = 1.656f;

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
        bool crouching;
        bool keyboardJumpQueued;
        int jumpsRemaining = 2;
        bool jumpAnimationPlaying;
        float stepTimer;

        static readonly int WalkState = Animator.StringToHash("Walk");
        static readonly int JumpState = Animator.StringToHash("Jump");

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
            if (cat != null)
                CozyAudioManager.Instance?.PlayRandomMeow();
        }

        void FitVisualToHeight()
        {
            if (visual == null || sprite == null || sprite.sprite == null || sprite.sprite.bounds.size.y <= 0f)
                return;

            float scale = visualHeight / sprite.sprite.bounds.size.y;
            visual.localScale = new Vector3(scale * facing, scale * (crouching ? 0.58f : 1f), 1f);
            visual.localPosition = new Vector3(0f, crouching ? -visualHeight * 0.21f : 0f, 0f);
        }

        void Update()
        {
            if (!GameplayMode.IsDrawing &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
                keyboardJumpQueued = true;
        }

        public void PlaceAtSpawn(Vector2 world)
        {
            crouching = false;
            keyboardJumpQueued = false;
            jumpsRemaining = 2;
            FitVisualToHeight();
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                circle.radius = 0.336f;
                circle.offset = Vector2.zero;
            }
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
            UpdateJumpAnimation(grounded);
            if (!GameplayMode.IsDrawing)
            {
                UpdateDirectControl(grounded, surface);
                return;
            }
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
                // Tümsek tepesindeki kısa collider ayrımlarında grounded bir fizik
                // karesi boyunca false dönebilir. Yatay hızı korumazsak kedi tam
                // tepede asılı kalıyordu; havadayken de ileri momentumunu sürdürüyor.
                float airSpeed = surface == PathSurfaceType.Ice ? moveSpeed * iceSpeedMultiplier : moveSpeed;
                body.velocity = new Vector2(facing * airSpeed, body.velocity.y);
                if (animator != null)
                    animator.speed = jumpAnimationPlaying ? 1f : 0f;
            }

            if (visual != null)
            {
                var s = visual.localScale;
                s.x = Mathf.Abs(s.x) * facing;
                visual.localScale = s;
            }
            UpdateFootsteps(grounded, body.velocity.x);

            var level = LevelManager.Instance != null ? LevelManager.Instance.Current : null;
            float fallY = level != null ? level.fallY : -7.5f;
            if (transform.position.y < fallY || airTimer > airGrace + 1.8f)
                StartCoroutine(RescueRoutine());
        }

        void UpdateDirectControl(bool grounded, PathSurfaceType surface)
        {
            if (grounded && body.velocity.y <= 0.15f)
                jumpsRemaining = 2;

            float keyboardHorizontal = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyboardHorizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyboardHorizontal += 1f;
            float horizontal = Mathf.Clamp(MobileControlState.Horizontal + keyboardHorizontal, -1f, 1f);
            float speed = surface == PathSurfaceType.Ice ? moveSpeed * iceSpeedMultiplier : moveSpeed;
            body.velocity = new Vector2(horizontal * speed, body.velocity.y);

            if (Mathf.Abs(horizontal) > 0.01f)
                facing = horizontal > 0f ? 1 : -1;
            bool jumpRequested = MobileControlState.ConsumeJump() || keyboardJumpQueued;
            keyboardJumpQueued = false;
            if (jumpRequested && jumpsRemaining > 0)
            {
                bool secondJump = jumpsRemaining == 1;
                float jumpPower = bounceForce * (secondJump ? 1.22f : 1.02f);
                body.velocity = new Vector2(body.velocity.x, jumpPower);
                jumpsRemaining--;
            }

            bool shouldCrouch = MobileControlState.CrouchHeld || Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (shouldCrouch != crouching)
            {
                crouching = shouldCrouch;
                FitVisualToHeight();
                var circle = GetComponent<CircleCollider2D>();
                if (circle != null)
                {
                    circle.radius = crouching ? 0.252f : 0.336f;
                    circle.offset = crouching ? new Vector2(0f, -0.096f) : Vector2.zero;
                }
            }

            if (animator != null)
                animator.speed = !grounded && jumpAnimationPlaying
                    ? 1f
                    : Mathf.Abs(horizontal) > 0.01f ? walkAnimationSpeed : 0f;
            if (visual != null)
            {
                var scale = visual.localScale;
                scale.x = Mathf.Abs(scale.x) * facing;
                visual.localScale = scale;
            }
            UpdateFootsteps(grounded, body.velocity.x);

            var level = LevelManager.Instance != null ? LevelManager.Instance.Current : null;
            float fallY = level != null ? level.fallY : -7.5f;
            if (transform.position.y < fallY)
                StartCoroutine(RescueRoutine());
        }

        void UpdateFootsteps(bool grounded, float horizontalSpeed)
        {
            if (!grounded || Mathf.Abs(horizontalSpeed) < 0.15f)
            {
                stepTimer = 0f;
                return;
            }
            stepTimer -= Time.fixedDeltaTime;
            if (stepTimer > 0f)
                return;
            stepTimer = 0.34f;
            CozyAudioManager.Instance?.PlayStep();
        }

        void UpdateJumpAnimation(bool grounded)
        {
            if (animator == null)
                return;

            if (!grounded && !jumpAnimationPlaying && animator.HasState(0, JumpState))
            {
                jumpAnimationPlaying = true;
                animator.Play(JumpState, 0, 0f);
                animator.speed = 1f;
            }
            else if (grounded && jumpAnimationPlaying)
            {
                jumpAnimationPlaying = false;
                if (animator.HasState(0, WalkState))
                    animator.Play(WalkState, 0, 0f);
            }
        }

        bool IsGrounded(out PathSurfaceType surface)
        {
            surface = PathSurfaceType.Normal;
            var hits = Physics2D.CircleCastAll(transform.position, 0.144f, Vector2.down, groundedProbe, groundMask);
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
            CozyAudioManager.Instance?.PlayHurt();
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
            CozyAudioManager.Instance?.PlayFall();
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.RemoveLove(10, "Yoldan düşme");
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
            CozyAudioManager.Instance?.PlayRescue();
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
