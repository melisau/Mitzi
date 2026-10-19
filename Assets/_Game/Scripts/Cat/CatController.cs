using System.Collections;
using UnityEngine;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Economy;
using PawPath.Levels;
using PawPath.Gameplay;
using PawPath.Audio;
using PawPath.Localization;

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
        [SerializeField] float moveSpeed = 2.55f;
        [SerializeField] float iceSpeedMultiplier = 1.65f;
        [SerializeField] float bounceForce = 7.2f;
        [SerializeField] float walkAnimationSpeed = 2.05f;
        [SerializeField] float groundedProbe = 0.12f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float airGrace = 0.45f;
        // Pack 64x64 karelerde kedi içeriği ~%50; 4.0 ≈ görünen ~2 birim boy.
        [SerializeField] float visualHeight = 2.0f;
        [SerializeField, Min(0.1f)] float visualSizeMultiplier = 1.25f;
        // Pack karelerinde patiler tuvalin altından ~%25 yukarıda (pivot ile aynı).
        [SerializeField, Range(0f, 0.35f)] float spriteBottomPadding = 0.25f;

        [Header("Kurtarma")]
        [SerializeField] SpriteRenderer bubbleRenderer;
        [SerializeField] Transform visual;

        Rigidbody2D body;
        CircleCollider2D bodyCollider;
        BoxCollider2D standingProfile;
        readonly RaycastHit2D[] groundVisualHits = new RaycastHit2D[8];
        readonly ContactPoint2D[] movementContacts = new ContactPoint2D[16];
        CrouchPassage recoveringPassage;
        float passageRetreatX;
        SpriteRenderer sprite;
        Animator animator;
        // Kontrolcüsü olmayan Animator'a Play/speed göndermek konsolu uyarıyla doldurur.
        bool HasAnimator => animator != null && animator.runtimeAnimatorController != null;
        Vector3 spawnPosition;
        bool busy;
        float airTimer;
        CatDefinition definition;
        int facing = 1;
        bool crouching;
        public bool IsCrouching => crouching;
        bool keyboardJumpQueued;
        int jumpsRemaining = 2;
        bool jumpAnimationPlaying;
        bool intentionalJump;
        bool jumpLeftGround;
        float stepTimer;
        bool climbing;
        bool perched;
        bool perchJump;
        float climbUntil;
        float climbFrameTimer;
        int climbFrameIndex;
        Sprite[] climbFrames;
        float normalGravityScale;
        Vector2 climbStartPosition;
        Vector2 climbTargetPosition;
        float climbStartTime;
        float climbRiseDuration;
        const float ClimbAlignDuration = 0.28f;
        const float ClimbFramesPerSecond = 24f;
        bool climbTopPoseApplied;
        float climbMountAngle;

        static readonly int WalkState = Animator.StringToHash("Walk");
        static readonly int JumpState = Animator.StringToHash("Jump");
        static readonly int CrouchState = Animator.StringToHash("Crouch");
        static readonly int CrouchWalkState = Animator.StringToHash("CrouchWalk");
        static readonly int IdleState = Animator.StringToHash("Idle");

        public bool IsBusy => busy;
        public CatDefinition Definition => definition;
        public bool IsClimbing => climbing || perched;
        public bool CanStartClimb => !IsClimbing && !intentionalJump && body != null && IsGrounded(out _);

        public void BounceAfterDogStomp()
        {
            if (body == null || busy)
                return;
            intentionalJump = true; jumpLeftGround = false;
            body.velocity = new Vector2(body.velocity.x, bounceForce * 0.72f);
            jumpsRemaining = Mathf.Max(jumpsRemaining, 1);
            airTimer = 0f;
        }

        public void StopImmediately()
        {
            if (body != null)
                body.velocity = Vector2.zero;
            keyboardJumpQueued = false;
        }

        void Awake()
        {
            Instance = this;
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CircleCollider2D>();
            var profile=transform.Find("StandingProfile");
            if(profile==null) { profile=new GameObject("StandingProfile").transform;profile.SetParent(transform,false); }
            standingProfile=profile.GetComponent<BoxCollider2D>();
            if(standingProfile==null)standingProfile=profile.gameObject.AddComponent<BoxCollider2D>();
            standingProfile.sharedMaterial=bodyCollider.sharedMaterial;
            UpdateStandingProfile();
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.gravityScale = 2.4f;
            facing = 1;
            normalGravityScale = body.gravityScale;
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
                {
                    sprite.sprite = cat.idleSprite;
                    // The imported ground pivot is also the registered paw baseline.
                    spriteBottomPadding = cat.idleSprite.pivot.y / cat.idleSprite.rect.height;
                }
                walkAnimationSpeed = 1f;
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

            // Always fit against idle. A recalibrated walk sprite has a different
            // PPU; refitting to that transient frame would undo its size correction.
            var reference = definition != null && definition.idleSprite != null ? definition.idleSprite : sprite.sprite;
            float scale = visualHeight * visualSizeMultiplier * DisplaySizeSettings.CatScale / reference.bounds.size.y;
            // Eğilme için ayrı bir poz kullanılıyor; görseli dikey sıkıştırmak
            // kediyi ezilmiş gösterdiği için tüm pozlar orantılı ölçeklenir.
            visual.localScale = new Vector3(scale * facing, scale, 1f);
            UpdateStandingProfile();
            AlignVisualFeet();
        }

        void UpdateStandingProfile()
        {
            if(standingProfile==null)return;
            float scale=DisplaySizeSettings.CatScale;
            standingProfile.size=new Vector2(.82f,.62f)*scale;
            standingProfile.offset=new Vector2(0,.55f)*scale;
            standingProfile.enabled=!crouching;
        }

        void LateUpdate()
        {
            ApplyAirbornePose();
            // Animator kare değiştirdiğinde sprite sınırları da değişebilir.
            AlignVisualFeet();
        }

        void AlignVisualFeet()
        {
            if (visual == null || sprite == null || sprite.sprite == null)
                return;

            if (bodyCollider == null)
                return;

            // The registered paw baseline excludes the transparent bottom margin.
            var bounds = sprite.sprite.bounds;
            float visibleFootY = bounds.min.y + bounds.size.y * spriteBottomPadding;
            // The tucked pose has a hanging tail; keep the torso aligned instead
            // of lifting the whole cat to register that tail as a grounded paw.
            if (definition != null && sprite.sprite == definition.jumpApexSprite)
                visibleFootY += bounds.size.y * .14f;
            float colliderFootY = bodyCollider.offset.y - bodyCollider.radius;
            Vector3 footPosition = new Vector3(0f, colliderFootY, 0f);
            float surfaceAngle = 0f;
            if (body != null && !intentionalJump && !climbing && !perched &&
                TryVisualGround(bodyCollider.bounds.center.x, out var centreGround) &&
                Mathf.Abs(centreGround.y - bodyCollider.bounds.min.y) < .30f)
            {
                // Fit a paw contact line across the body, including ascending motion.
                // A single centre normal flips abruptly at a polygon's summit.
                float span = .58f * DisplaySizeSettings.CatScale;
                bool hasLeft = TryVisualGround(centreGround.x - span, out var left);
                bool hasRight = TryVisualGround(centreGround.x + span, out var right);
                if (hasLeft && hasRight)
                    surfaceAngle = Mathf.Clamp(Mathf.Atan2(right.y-left.y, right.x-left.x)*Mathf.Rad2Deg, -35f, 35f);
                surfaceAngle = Mathf.MoveTowardsAngle(visual.localEulerAngles.z, surfaceAngle, Mathf.Max(.001f, Time.deltaTime)*300f);
                float slope = Mathf.Tan(surfaceAngle*Mathf.Deg2Rad);
                float height = centreGround.y;
                if (hasLeft) height = Mathf.Max(height, left.y - slope*(left.x-centreGround.x));
                if (hasRight) height = Mathf.Max(height, right.y - slope*(right.x-centreGround.x));
                footPosition = transform.InverseTransformPoint(new Vector3(centreGround.x, height+.035f, transform.position.z));
            }
            visual.localRotation = Quaternion.Euler(0f, 0f,
                climbing && !climbTopPoseApplied ? climbMountAngle : surfaceAngle);
            visual.localPosition = footPosition - visual.localRotation *
                (Vector3.up * visibleFootY * Mathf.Abs(visual.localScale.y));
        }

        bool TryVisualGround(float x, out Vector2 point)
        {
            point = default;
            Vector2 origin = new Vector2(x, bodyCollider.bounds.max.y + .75f);
            int count = Physics2D.RaycastNonAlloc(origin, Vector2.down, groundVisualHits,
                bodyCollider.bounds.size.y + 1.5f, groundMask);
            float nearest = float.PositiveInfinity;
            for (int i=0;i<count;i++)
            {
                var hit = groundVisualHits[i];
                if (hit.collider == null || hit.collider == bodyCollider || hit.collider.isTrigger ||
                    hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<CrouchPassage>() != null ||
                    hit.normal.y < .35f || hit.distance >= nearest) continue;
                nearest = hit.distance; point = hit.point;
            }
            return nearest < float.PositiveInfinity;
        }

        void Update()
        {
            if (GameFlow.Instance != null && GameFlow.Instance.GameplayPaused)
                return;
            if (!climbing && !perched &&
                (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
                keyboardJumpQueued = true;
            if (climbing)
                UpdateClimbing();
            else if (perched)
                UpdatePerched();
        }

        public void BeginClimb(Vector2 perchPosition, Sprite[] frames, float duration)
        {
            if (climbing || perched || intentionalJump || body == null || perchPosition.y <= transform.position.y + .25f) return;
            keyboardJumpQueued = false;
            climbing = true;
            perched = false;
            // Her kedi kendi renginde tırmanır; tanımda kare yoksa ağacın ortak kareleri.
            climbFrames = definition != null && definition.climbFrames != null && definition.climbFrames.Length > 0
                ? definition.climbFrames : frames;
            climbFrameIndex = 0;
            climbFrameTimer = 0f;
            climbTopPoseApplied = false;
            climbMountAngle = 0f;
            climbStartTime = Time.time;
            climbRiseDuration = Mathf.Clamp(duration * 0.80f, 2.1f, 2.8f);
            climbUntil = Time.time + Mathf.Max(duration, ClimbAlignDuration + climbRiseDuration + 0.35f);
            body.velocity = Vector2.zero;
            body.gravityScale = 0f;
            climbStartPosition = transform.position;
            // Kedi ağacın üzerinde dikey yükselir. Hedef ağacın tepesidir fakat
            // bozuk bir pivot olması halinde tek tırmanışta aşırı yükselmez.
            climbTargetPosition = new Vector2(perchPosition.x - facing * 0.35f,
                Mathf.Min(perchPosition.y, climbStartPosition.y + 5.2f));
            if (animator != null) animator.enabled = false;
            if (sprite != null && climbFrames != null && climbFrames.Length > 0 && climbFrames[0] != null)
            {
                sprite.sprite = climbFrames[0];
                FitVisualToHeight();
            }
        }

        void UpdateClimbing()
        {
            // Taps made while mounting must not queue another ascent at the top.
            MobileControlState.ConsumeJump();
            body.velocity = Vector2.zero;
            float elapsed = Time.time - climbStartTime;
            if (elapsed < ClimbAlignDuration)
            {
                float align = Mathf.Clamp01(elapsed / ClimbAlignDuration);
                transform.position = new Vector2(
                    Mathf.Lerp(climbStartPosition.x, climbTargetPosition.x, align),
                    climbStartPosition.y);
                return;
            }

            // Gövdeye hizalandıktan sonra X sabit kalır; yalnızca Y ekseninde
            // ağacın tepesine doğru kademeli olarak yükselir.
            float rise = Mathf.Clamp01((elapsed - ClimbAlignDuration) / climbRiseDuration);
            // Reach the upper branch, then bring the torso over it rather than
            // continuing vertically into empty space above the canopy.
            climbMountAngle = -75f * facing * Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((rise - 0.55f) / 0.45f));
            float easedRise = rise * rise * (3f - 2f * rise);
            transform.position = new Vector2(climbTargetPosition.x,
                Mathf.Lerp(climbStartPosition.y, climbTargetPosition.y, easedRise));
            if (rise < 0.995f && climbFrames != null && climbFrames.Length > 0)
            {
                // Paw strokes repeat while the body ascends; they must not each
                // freeze for a fraction of the entire climb.
                int nextFrame = Mathf.FloorToInt((elapsed - ClimbAlignDuration) * ClimbFramesPerSecond)
                    % climbFrames.Length;
                if (nextFrame != climbFrameIndex && climbFrames[nextFrame] != null)
                {
                    climbFrameIndex = nextFrame;
                    sprite.sprite = climbFrames[climbFrameIndex];
                    FitVisualToHeight();
                }
            }
            // Tepeye varınca tırmanma pozunda asılı kalmaz; normal ayakta/yürüme
            // görseline dönerek köpek geçene kadar dalın üzerinde bekler.
            if (rise >= 0.995f && !climbTopPoseApplied)
            {
                climbTopPoseApplied = true;
                if (definition != null && definition.idleSprite != null)
                {
                    sprite.sprite = definition.idleSprite;
                    FitVisualToHeight();
                }
            }
            if (Time.time < climbUntil) return;
            climbing = false;
            perched = true;
            body.velocity = Vector2.zero;
            body.gravityScale = 0f;
            if (definition != null && definition.idleSprite != null)
                sprite.sprite = definition.idleSprite;
            if (animator != null)
                animator.enabled = false;
            FitVisualToHeight();
        }

        void UpdatePerched()
        {
            body.velocity = Vector2.zero;
            transform.position = climbTargetPosition;
            bool moveRequested = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
                Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) ||
                Mathf.Abs(MobileControlState.Horizontal) > 0.01f;
            bool jumpRequested = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.UpArrow) || MobileControlState.JumpQueued;
            if (GameplayMode.IsDrawing && Input.GetMouseButtonDown(0))
                moveRequested = true;
            if (!moveRequested && !jumpRequested)
                return;

            if (jumpRequested)
                MobileControlState.ConsumeJump();
            float horizontal = MobileControlState.Horizontal;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Mathf.Abs(horizontal) > .01f) facing = horizontal > 0f ? 1 : -1;
            perched = false;
            body.gravityScale = normalGravityScale;
            if (animator != null)
                animator.enabled = animator.runtimeAnimatorController != null;
            if (jumpRequested)
            {
                perchJump = true;
                jumpsRemaining = 2;
                keyboardJumpQueued = true;
                body.velocity = new Vector2(facing * moveSpeed, 0f);
                TryJump();
            }
            keyboardJumpQueued = false;
        }

        public void PlaceAtSpawn(Vector2 world)
        {
            recoveringPassage = null;
            intentionalJump = false; jumpLeftGround = false; jumpAnimationPlaying = false;
            climbing = false;
            perched = false;
            perchJump = false;
            facing = 1;
            if (body != null)
                body.gravityScale = normalGravityScale;
            SetCrouching(false,true);
            keyboardJumpQueued = false;
            jumpsRemaining = 2;
            FitVisualToHeight();
            var circle = GetComponent<CircleCollider2D>();
            if (circle != null)
            {
                circle.radius = 0.336f;
                circle.offset = Vector2.zero;
            }
            AlignVisualFeet();
            spawnPosition = world;
            transform.position = world;
            body.simulated = true;
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
            if (climbing || perched)
            {
                body.velocity = Vector2.zero;
                return;
            }
            if (GameFlow.Instance != null && GameFlow.Instance.InHub)
            {
                body.velocity = Vector2.zero;
                return;
            }
            if (GameFlow.Instance != null && GameFlow.Instance.GameplayPaused)
            {
                body.velocity = Vector2.zero;
                return;
            }

            bool grounded = IsGrounded(out var surface);
            UpdateJumpAnimation(grounded);
            if (GameplayMode.IsDrawing)
                SetCrouching(grounded && (MobileControlState.CrouchHeld || Input.GetKey(KeyCode.S) ||
                    Input.GetKey(KeyCode.DownArrow) || CrouchPassage.NeedsCrouch(this,.45f)), grounded);
            if (!GameplayMode.IsDrawing)
            {
                UpdateDirectControl(grounded, surface);
                return;
            }
            if (RecoverFromPassageLedge(grounded)) return;
            if (grounded && body.velocity.y <= 0.15f)
            {
                jumpsRemaining = 2;
                perchJump = false;
            }
            TryJump();
            // Auto-run must not keep pressing into the side of a low passage.
            // Releasing that pressure lets gravity bring the cat down to its entrance.
            float forward = BlockedByPassageSide() ? 0f : facing;
            if (grounded)
            {
                airTimer = 0f;
                float speed = (surface == PathSurfaceType.Ice ? moveSpeed * iceSpeedMultiplier : moveSpeed) * (crouching ? .55f : 1f);
                body.velocity = new Vector2(forward * speed, body.velocity.y);
                if (HasAnimator)
                    animator.speed = walkAnimationSpeed;
            }
            else
            {
                airTimer += Time.fixedDeltaTime;
                // Tümsek tepesindeki kısa collider ayrımlarında grounded bir fizik
                // karesi boyunca false dönebilir. Yatay hızı korumazsak kedi tam
                // tepede asılı kalıyordu; havadayken de ileri momentumunu sürdürüyor.
                float airSpeed = surface == PathSurfaceType.Ice ? moveSpeed * iceSpeedMultiplier : moveSpeed;
                body.velocity = new Vector2(forward * airSpeed, body.velocity.y);
                if (HasAnimator)
                    animator.speed = jumpAnimationPlaying ? 1f : 0f;
            }

            if (visual != null)
            {
                var s = visual.localScale;
                s.x = Mathf.Abs(s.x) * facing;
                visual.localScale = s;
            }
            if(GameplayMode.IsDrawing && crouching && grounded && HasAnimator && animator.HasState(0,CrouchWalkState))
                animator.Play(CrouchWalkState,0,Mathf.Repeat(Time.time*2f,1f));
            UpdateFootsteps(grounded, body.velocity.x);

            var level = LevelManager.Instance != null ? LevelManager.Instance.Current : null;
            float fallY = level != null ? level.fallY : -7.5f;
            if (transform.position.y < fallY || airTimer > airGrace + 1.8f)
                StartCoroutine(RescueRoutine());
        }

        bool BlockedByPassageSide()
        {
            int count = body.GetContacts(movementContacts);
            for (int i = 0; i < count; i++)
            {
                var contact = movementContacts[i];
                if (contact.normal.x * facing < -.5f && Mathf.Abs(contact.normal.y) < .55f &&
                    (contact.collider.GetComponentInParent<CrouchPassage>() != null ||
                     contact.otherCollider.GetComponentInParent<CrouchPassage>() != null))
                    return true;
            }
            return false;
        }

        bool RecoverFromPassageLedge(bool grounded)
        {
            if (recoveringPassage == null)
            {
                int count = body.GetContacts(movementContacts);
                for (int i = 0; i < count; i++)
                {
                    var contact = movementContacts[i];
                    var passage = contact.collider.GetComponentInParent<CrouchPassage>();
                    if (passage == null) passage = contact.otherCollider.GetComponentInParent<CrouchPassage>();
                    if (passage == null || bodyCollider.bounds.min.y < passage.FloorY + .15f) continue;
                    bool side = contact.normal.x * facing < -.5f;
                    bool ledge = contact.normal.y > .35f;
                    if (!side && !ledge) continue;
                    recoveringPassage = passage;
                    float edge = facing > 0 ? passage.Left : passage.Right;
                    foreach (var obstacle in passage.GetComponentsInChildren<Collider2D>())
                        if (!obstacle.isTrigger)
                            edge = facing > 0 ? Mathf.Min(edge, obstacle.bounds.min.x) : Mathf.Max(edge, obstacle.bounds.max.x);
                    passageRetreatX = edge - facing * (bodyCollider.radius + .22f * DisplaySizeSettings.CatScale);
                    break;
                }
            }
            if (recoveringPassage == null) return false;
            MobileControlState.ConsumeJump(); keyboardJumpQueued = false;
            if ((grounded && bodyCollider.bounds.min.y <= recoveringPassage.FloorY + .12f) ||
                bodyCollider.bounds.min.y < recoveringPassage.FloorY - 1f)
            {
                recoveringPassage = null;
                return false;
            }
            bool outside = facing > 0 ? body.position.x <= passageRetreatX : body.position.x >= passageRetreatX;
            body.velocity = new Vector2(outside ? 0f : -facing * moveSpeed * .7f, Mathf.Min(body.velocity.y, 0f));
            return true;
        }

        void SetCrouching(bool value, bool grounded)
        {
            if (value != crouching)
            {
                crouching = value;
                UpdateStandingProfile();
                if (HasAnimator)
                {
                    if (crouching && grounded && !jumpAnimationPlaying && animator.HasState(0, CrouchState))
                    {
                        animator.Play(CrouchState, 0, 0f);
                        animator.speed = 0f;
                        animator.Update(0f);
                    }
                    else if (!jumpAnimationPlaying && animator.HasState(0, WalkState))
                    {
                        animator.Play(WalkState, 0, 0f);
                        animator.Update(0f);
                    }
                }
                FitVisualToHeight();
                var circle = GetComponent<CircleCollider2D>();
                if (circle != null)
                {
                    circle.radius = crouching ? 0.252f : 0.336f;
                    circle.offset = crouching ? new Vector2(0f, -0.096f) : Vector2.zero;
                }
            }

        }

        void TryJump()
        {
            bool jumpRequested = MobileControlState.ConsumeJump() | keyboardJumpQueued;
            keyboardJumpQueued = false;
            if (!jumpRequested || jumpsRemaining <= 0)
                return;

            bool secondJump = jumpsRemaining == 1;
            float jumpPower = bounceForce * (secondJump ? 1.22f : 1.02f);
            intentionalJump = true; jumpLeftGround = false;
            body.velocity = new Vector2(body.velocity.x, jumpPower);
            jumpsRemaining--;
            if (HasAnimator && animator.HasState(0, JumpState))
            {
                jumpAnimationPlaying = true;
                animator.Play(JumpState, 0, 0f);
                animator.speed = 1f;
            }
        }

void UpdateDirectControl(bool grounded, PathSurfaceType surface)
        {
            if (grounded && body.velocity.y <= 0.15f)
            {
                jumpsRemaining = 2;
                perchJump = false;
            }

            float keyboardHorizontal = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyboardHorizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyboardHorizontal += 1f;
            float horizontal = Mathf.Clamp(MobileControlState.Horizontal + keyboardHorizontal, -1f, 1f);
            if (perchJump && !grounded && Mathf.Abs(horizontal) < .01f) horizontal = facing;
            float speed = surface == PathSurfaceType.Ice ? moveSpeed * iceSpeedMultiplier : moveSpeed;
            body.velocity = new Vector2(horizontal * speed, body.velocity.y);

            // Tuşa basıldığında yönü doğru şekilde alıyoruz (Sağ: 1, Sol: -1)
            if (Mathf.Abs(horizontal) > 0.01f)
                facing = horizontal > 0f ? 1 : -1;
                
            TryJump();

            bool shouldCrouch = MobileControlState.CrouchHeld || Input.GetKey(KeyCode.S) ||
                Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            SetCrouching(shouldCrouch || (crouching && CrouchPassage.NeedsCrouch(this)), grounded);

            if (crouching && grounded)
                body.velocity = new Vector2(body.velocity.x * 0.55f, body.velocity.y);
            if (HasAnimator)
            {
                if (grounded && !jumpAnimationPlaying)
                {
                    bool moving = Mathf.Abs(horizontal) > 0.01f;
                    int state = crouching ? (moving ? CrouchWalkState : CrouchState)
                        : moving ? WalkState : IdleState;
                    if (animator.HasState(0, state) && animator.GetCurrentAnimatorStateInfo(0).shortNameHash != state)
                        animator.Play(state, 0, 0f);
                    animator.speed = crouching && !moving ? 0f : moving ? walkAnimationSpeed : 1f;
                }
                else animator.speed = jumpAnimationPlaying ? 1f : 0f;
            }
                        
            if (visual != null)
            {
                var scale = visual.localScale;
                // Every side-view pose faces right in the source; jumping preserves direction.
                scale.x = Mathf.Abs(scale.x) * facing;
                visual.localScale = scale;
            }
            
            if(GameplayMode.IsDrawing && crouching && grounded && HasAnimator && animator.HasState(0,CrouchWalkState))
                animator.Play(CrouchWalkState,0,Mathf.Repeat(Time.time*2f,1f));
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

        void ApplyAirbornePose()
        {
            if (!intentionalJump || !jumpAnimationPlaying || climbing || perched || body == null || sprite == null || definition == null)
                return;
            bool supported = bodyCollider != null && bodyCollider.IsTouchingLayers(groundMask);
            supported |= IsGrounded(out _);
            if (supported)
            {
                if (sprite.sprite == definition.jumpApexSprite)
                    sprite.sprite = definition.walkFrames != null && definition.walkFrames.Length > 0 ? definition.walkFrames[0] : definition.idleSprite;
                return;
            }
            float verticalSpeed = body.velocity.y;
            if (intentionalJump && Mathf.Abs(verticalSpeed) <= 1.8f && definition.jumpApexSprite != null)
                sprite.sprite = definition.jumpApexSprite;
            else if (definition.jumpFrames != null && definition.jumpFrames.Length >= 3)
                sprite.sprite = definition.jumpFrames[verticalSpeed > 4.5f ? 1 : 2];
        }

        void UpdateJumpAnimation(bool grounded)
        {
            if (!HasAnimator)
                return;

            bool supported = grounded || (bodyCollider != null && bodyCollider.IsTouchingLayers(groundMask));
            if (!supported) jumpLeftGround = true;
            if (supported && (!intentionalJump || jumpLeftGround))
            {
                intentionalJump = false; jumpLeftGround = false;
                if (sprite != null && definition != null && sprite.sprite == definition.jumpApexSprite)
                    sprite.sprite = definition.walkFrames != null && definition.walkFrames.Length > 0 ? definition.walkFrames[0] : definition.idleSprite;
                if (jumpAnimationPlaying && animator.HasState(0, WalkState))
                    animator.Play(WalkState, 0, 0f);
                jumpAnimationPlaying = false;
            }
            else if (!supported && intentionalJump && !jumpAnimationPlaying && animator.HasState(0, JumpState))
            {
                jumpAnimationPlaying = true;
                animator.Play(JumpState, 0, 0f);
                animator.speed = 1f;
            }
        }

        bool IsGrounded(out PathSurfaceType surface)
        {
            surface = PathSurfaceType.Normal;
            var circle = GetComponent<CircleCollider2D>();
            float colliderRadius = circle != null ? circle.radius : 0.336f;
            Vector2 colliderOffset = circle != null ? circle.offset : Vector2.zero;
            const float probeRadius = 0.12f;
            Vector2 probeOrigin = (Vector2)transform.position + colliderOffset +
                Vector2.down * Mathf.Max(0f, colliderRadius - probeRadius);
            var hits = Physics2D.CircleCastAll(probeOrigin, probeRadius, Vector2.down,
                groundedProbe, groundMask);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].collider.isTrigger)
                    continue;
                if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform))
                    continue;
                if (hits[i].normal.y < .35f)
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
                intentionalJump = true; jumpLeftGround = false;
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
            GameFlow.Instance?.ShowLevelFailure(GameText.L("Tehlikeli alana temas ettin. Daha güvenli bir yol dene!", "You touched a danger zone. Try a safer path!", "Du hast eine Gefahrenzone berührt. Versuch einen sichereren Weg!"));
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

            body.simulated = true;
            body.velocity = Vector2.zero;
            airTimer = 0f;
            busy = false;
            GameFlow.Instance?.ShowLevelFailure(GameText.L("Kedi çukura düştü. Zıplama zamanını veya çizdiğin yolu değiştir!", "Your cat fell into a gap. Change your jump timing or the path you drew!", "Deine Katze ist in eine Lücke gefallen. Spring zu einem anderen Zeitpunkt oder ändere deinen Weg!"));
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
