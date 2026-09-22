using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Audio;

namespace PawPath.Levels
{
    public class DogObstacle : MonoBehaviour
    {
        const float TurnDelaySeconds = 2f;
        const float GapRetreatDelaySeconds = 0.65f;
        const float ActivationDistance = 7f;
        SpriteRenderer renderer;
        Sprite[] frames;
        Vector2[] gaps;
        float speed, leftBound, rightBound, groundY, frameTimer, visualWidth, groundInset, frameInterval;
        float turnDelayRemaining;
        float gapRetreatRemaining, jumpElapsed, jumpDuration, jumpStartX, jumpEndX, jumpHeight;
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
            float secondsPerFrame, Vector2[] courseGaps, int courseLevelNumber)
        {
            renderer = target;
            frames = animationFrames;
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
        }

        void Update()
        {
            if (failed || GameFlow.Instance == null || GameFlow.Instance.State != GameFlowState.Gameplay)
                return;

            if (!activated)
            {
                CatController cat = CatController.Instance;
                if (cat == null || cat.IsBusy ||
                    Mathf.Abs(cat.transform.position.x - transform.position.x) > ActivationDistance)
                    return;
                activated = true;
            }

            TryTurnTowardPassingCat();
            if (turnPending)
            {
                turnDelayRemaining -= Time.deltaTime;
                if (turnDelayRemaining > 0f)
                    return;
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
                    return;
                gapRetreatPending = false;
                turnedToChase = true;
                direction = -direction;
                AlignFeetToGround();
            }

            if (!jumpingGap)
                TryStartGapAction(Time.deltaTime);
            if (gapRetreatPending)
                return;
            if (jumpingGap)
                AdvanceGapJump(Time.deltaTime);
            else
                transform.position += Vector3.right * (direction * speed * Time.deltaTime);
            if (transform.position.x < leftBound || transform.position.x > rightBound)
            {
                Destroy(gameObject);
                return;
            }
            if (frames == null || frames.Length == 0 || renderer == null) return;
            frameTimer += Time.deltaTime;
            if (frameTimer < frameInterval) return;
            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
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
                    jumpEndX = (direction < 0 ? gaps[i].x : gaps[i].y) + direction * clearance;
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
                groundY + Mathf.Sin(progress * Mathf.PI) * jumpHeight,
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
            float width = Mathf.Max(0.01f, renderer.sprite.bounds.size.x);
            float scale = visualWidth / width;
            // Farklı boyutlarda gelen kareler gövdeyi büyütüp küçültmesin;
            // patiler her karede yolun aynı Y çizgisine basar.
            renderer.transform.localScale = new Vector3(direction * scale, scale, 1f);
            renderer.transform.localPosition = new Vector3(0f,
                -renderer.sprite.bounds.min.y * scale - groundInset, 0f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var cat = other.GetComponent<CatController>();
            if (failed || cat == null || cat.IsClimbing) return;
            failed = true;
            CozyAudioManager.Instance?.PlayHurt();
            GameFlow.Instance?.ShowLevelFailure("Koşan köpeğe yakalandın. Üzerinden zıpla veya ağaca tırman!");
        }
    }
}
