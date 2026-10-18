using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Audio;
using PawPath.Localization;

namespace PawPath.Levels
{
    public class DogObstacle : MonoBehaviour
    {
        const float TurnDelaySeconds = 2f;
        const float GapRetreatDelaySeconds = 0.65f;
        const float ActivationDistance = 7f;
        SpriteRenderer renderer;
        Sprite[] frames;
        Sprite idleSprite;
        Vector2[] gaps;
        float speed, leftBound, rightBound, groundY, frameTimer, visualWidth, groundInset, frameInterval;
        float turnDelayRemaining;
        float gapRetreatRemaining, jumpElapsed, jumpDuration, jumpStartX, jumpEndX, jumpHeight;
        readonly RaycastHit2D[] terrainHits = new RaycastHit2D[32];
        float surfaceAngle, jumpStartY, jumpEndY;
        int levelNumber;
        int frameIndex;
        bool failed;
        bool activated;
        bool catApproachedFromLeft;
        bool catJumpedNearby;
        bool turnPending;
        bool turnedToChase;
        bool gapRetreatPending;
        bool jumpingGap;
        int direction = -1;

        public void Configure(SpriteRenderer target, Sprite[] animationFrames, float runSpeed,
            float minX, float maxX, float roadSurfaceY, float targetVisualWidth, float feetInset,
            float secondsPerFrame, Vector2[] courseGaps, int courseLevelNumber, Sprite idlePose = null)
        {
            renderer = target;
            frames = animationFrames;
            idleSprite = idlePose;
            speed = runSpeed;
            leftBound = minX;
            rightBound = maxX;
            groundY = roadSurfaceY;
            visualWidth = targetVisualWidth;
            groundInset = feetInset;
            frameInterval = secondsPerFrame;
            gaps = courseGaps;
            levelNumber = courseLevelNumber;
            AlignFeetToGround();
            ShowIdle();
        }

        void ShowIdle()
        {
            if (renderer == null || idleSprite == null) return;
            renderer.sprite = idleSprite;
            AlignFeetToGround();
        }

        void Update()
        {
            if (failed || GameFlow.Instance == null || GameFlow.Instance.State != GameFlowState.Gameplay)
            {
                ShowIdle();
                return;
            }

            if (!activated)
            {
                CatController cat = CatController.Instance;
                if (cat == null || cat.IsBusy ||
                    Mathf.Abs(cat.transform.position.x - transform.position.x) > ActivationDistance)
                {
                    ShowIdle();
                    return;
                }
                activated = true;
            }

            TryTurnTowardPassingCat();
            if (turnPending)
            {
                turnDelayRemaining -= Time.deltaTime;
                if (turnDelayRemaining > 0f)
                {
                    ShowIdle();
                    return;
                }
                turnPending = false;
                turnedToChase = true;
                direction = 1;
                speed *= 1.25f;
                AlignFeetToGround();
            }
            if (gapRetreatPending)
            {
                gapRetreatRemaining -= Time.deltaTime;
                if (gapRetreatRemaining > 0f)
                {
                    ShowIdle();
                    return;
                }
                gapRetreatPending = false;
                turnedToChase = true;
                direction = -direction;
                AlignFeetToGround();
            }

            if(!jumpingGap && CrouchPassage.BlocksDog(transform.position.x,direction*speed*Time.deltaTime,visualWidth*.45f,out float stopX))
            {
                var position=transform.position;position.x=stopX;transform.position=position;
                gapRetreatPending=true;gapRetreatRemaining=GapRetreatDelaySeconds;ShowIdle();return;
            }
            if (!jumpingGap)
                TryStartGapAction(Time.deltaTime);
            if (gapRetreatPending)
            {
                ShowIdle();
                return;
            }
            if (!jumpingGap && Mathf.Abs(speed) < 0.01f) { ShowIdle(); return; }
            if (renderer != null && frames != null && frames.Length > 0 && renderer.sprite == idleSprite)
            {
                renderer.sprite = frames[frameIndex];
                AlignFeetToGround();
            }
            if (jumpingGap)
                AdvanceGapJump(Time.deltaTime);
            else
                transform.position += Vector3.right * (direction * speed * Time.deltaTime);
            FollowTerrain();
            AlignFeetToGround();
            if (transform.position.x < leftBound || transform.position.x > rightBound)
            {
                Destroy(gameObject);
                return;
            }
            if (frames == null || frames.Length == 0 || renderer == null) return;
            frameTimer += Time.deltaTime;
            if (frameTimer < frameInterval) return;
            int advance = Mathf.FloorToInt(frameTimer / frameInterval);
            frameTimer -= advance * frameInterval;
            frameIndex = (frameIndex + advance) % frames.Length;
            renderer.sprite = frames[frameIndex];
            AlignFeetToGround();
        }

        void TryStartGapAction(float deltaTime)
        {
            if (gaps == null)
                return;
            float clearance = visualWidth * 0.5f + 0.12f;
            float nextX = transform.position.x + direction * speed * deltaTime;
            for (int i = 0; i < gaps.Length; i++)
            {
                float nearEdge = direction < 0 ? gaps[i].y : gaps[i].x;
                float launchX = nearEdge - direction * clearance;
                bool reachingEdge = direction < 0
                    ? transform.position.x >= launchX && nextX <= launchX
                    : transform.position.x <= launchX && nextX >= launchX;
                if (!reachingEdge)
                    continue;

                var position = transform.position;
                position.x = launchX;
                transform.position = position;
                // Aynı bölümdeki karar tekrar denemede değişmez; bazı köpekler
                // atlar, diğerleri uçurum kenarında durup geri döner.
                bool jump = ((levelNumber * 17 + i * 13) & 1) == 0;
                if (jump)
                {
                    jumpStartX = launchX;
                    jumpStartY = transform.position.y;
                    jumpEndX = (direction < 0 ? gaps[i].x : gaps[i].y) + direction * clearance;
                    jumpEndY = SurfaceAt(jumpEndX, out _);
                    float distance = Mathf.Abs(jumpEndX - jumpStartX);
                    jumpDuration = Mathf.Clamp(distance / Mathf.Max(0.1f, speed * 1.55f), 0.55f, 1.25f);
                    jumpHeight = Mathf.Max(0.9f, (gaps[i].y - gaps[i].x) * 0.7f + 0.45f);
                    jumpElapsed = 0f;
                    jumpingGap = true;
                }
                else
                {
                    gapRetreatPending = true;
                    gapRetreatRemaining = GapRetreatDelaySeconds;
                }
                return;
            }
        }

        void AdvanceGapJump(float deltaTime)
        {
            jumpElapsed += deltaTime;
            float progress = Mathf.Clamp01(jumpElapsed / jumpDuration);
            transform.position = new Vector3(
                Mathf.Lerp(jumpStartX, jumpEndX, progress),
                Mathf.Lerp(jumpStartY, jumpEndY, progress) + Mathf.Sin(progress * Mathf.PI) * jumpHeight,
                transform.position.z);
            if (progress >= 1f)
                jumpingGap = false;
        }

        void TryTurnTowardPassingCat()
        {
            if (turnedToChase || turnPending || gapRetreatPending || jumpingGap)
                return;
            CatController cat = CatController.Instance;
            if (cat == null || cat.IsBusy || cat.IsClimbing)
                return;

            float deltaX = cat.transform.position.x - transform.position.x;
            if (deltaX < 0f && deltaX > -3.2f)
                catApproachedFromLeft = true;
            if (catApproachedFromLeft && Mathf.Abs(deltaX) < 3.2f &&
                cat.transform.position.y > groundY + 0.8f)
                catJumpedNearby = true;

            // Yakından geçişte iki saniye yerinde bekleyip bir kez döner;
            // uzaktan geçen kedi dönüşü tetiklemez.
            if (!catApproachedFromLeft || !catJumpedNearby || deltaX < 0.18f || deltaX > 2.4f)
                return;
            turnPending = true;
            turnDelayRemaining = TurnDelaySeconds;
        }

        void AlignFeetToGround()
        {
            if (renderer == null || renderer.sprite == null) return;
            FollowTerrain();
            float width = Mathf.Max(0.01f, renderer.sprite.bounds.size.x);
            float scale = visualWidth / width;
            // Farklı boyutlarda gelen kareler gövdeyi büyütüp küçültmesin;
            // patiler her karede yolun aynı Y çizgisine basar.
            renderer.transform.localScale = new Vector3(direction * scale, scale, 1f);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, jumpingGap ? 0f : surfaceAngle);
            renderer.transform.localPosition = renderer.transform.localRotation *
                (Vector3.up * (-renderer.sprite.bounds.min.y * scale - groundInset));
        }

        // Sample the same solid collider surface that supports the cat, rather than
        // advancing horizontally through a hill. The trigger follows the root too.
        void FollowTerrain()
        {
            if (jumpingGap) return;
            var position = transform.position;
            position.y = SurfaceAt(position.x, out surfaceAngle);
            transform.position = position;
        }

        float SurfaceAt(float x, out float angle)
        {
            angle = 0f;
            int count = Physics2D.RaycastNonAlloc(new Vector2(x, groundY + 12f), Vector2.down, terrainHits, 24f);
            float nearest = float.PositiveInfinity, height = groundY;
            for (int i = 0; i < count; i++)
            {
                var hit = terrainHits[i];
                if (hit.collider == null || hit.collider.isTrigger || hit.normal.y < .35f ||
                    hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<CrouchPassage>() != null || hit.collider.attachedRigidbody != null ||
                    hit.distance >= nearest) continue;
                nearest = hit.distance;
                height = hit.point.y;
                angle = Mathf.Clamp(Vector2.SignedAngle(Vector2.up, hit.normal), -35f, 35f);
            }
            return height;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var cat = other.GetComponentInParent<CatController>();
            if (failed || cat == null || cat.IsClimbing) return;
            if (IsStompFromAbove(other))
            {
                failed = true;
                Vector3 puffPosition = GetComponent<Collider2D>().bounds.center;
                GetComponent<Collider2D>().enabled = false;
                cat.BounceAfterDogStomp();
                CozyAudioManager.Instance?.PlayBubble();
                // Çizgi film "puf"u: köpek ezilip söner, toz bulutu ve yıldızlar.
                StompEffect.Spawn(puffPosition, renderer);
                Destroy(gameObject);
                return;
            }
            failed = true;
            CozyAudioManager.Instance?.PlayHurt();
            GameFlow.Instance?.ShowLevelFailure(GameText.L("Koşan köpeğe yakalandın. Üzerinden zıpla veya ağaca tırman!", "A running dog caught you. Jump over it or climb a tree!", "Ein rennender Hund hat dich erwischt. Spring über ihn oder kletter auf einen Baum!"));
        }

        bool IsStompFromAbove(Collider2D catCollider)
        {
            var catBody = catCollider.attachedRigidbody;
            var dogCollider = GetComponent<Collider2D>();
            return catBody != null && dogCollider != null &&
                catBody.velocity.y < -0.5f &&
                catCollider.bounds.min.y >= dogCollider.bounds.max.y - 0.22f;
        }

        // Eski parçacık efekti; StompEffect ile değiştirildi, geri dönüş için saklanıyor.
        void SpawnPuff(Vector3 position)
        {
            var puff = new GameObject("DogStompPuff", typeof(ParticleSystem));
            puff.transform.position = position;
            var particles = puff.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.45f;
            main.startLifetime = 0.42f;
            main.startSpeed = 2.1f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.92f, 0.78f, 0.9f);
            main.gravityModifier = 0.25f;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.28f;
            puff.GetComponent<ParticleSystemRenderer>().sortingOrder = 12;
            particles.Play();
            particles.Emit(18);
            Destroy(puff, 1f);
        }
    }
}
