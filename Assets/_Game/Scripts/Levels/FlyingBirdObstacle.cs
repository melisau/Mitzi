using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Audio;
using PawPath.Localization;

namespace PawPath.Levels
{
    [RequireComponent(typeof(MobSimulationGate))]
    public class FlyingBirdObstacle : MonoBehaviour
    {
        SpriteRenderer renderer;
        Sprite[] frames;
        float speed;
        float leftBound;
        float rightBound;
        float baseY;
        float bobPhase;
        float frameTimer;
        int frameIndex;

        public void Configure(SpriteRenderer target, Sprite[] animationFrames, float flightSpeed,
            float minX, float maxX, float phase)
        {
            renderer = target;
            if (renderer != null) renderer.flipX = true;
            frames = animationFrames;
            speed = flightSpeed;
            leftBound = minX;
            rightBound = maxX;
            baseY = transform.position.y;
            bobPhase = phase;
        }

        void Update()
        {
            if (!MobSimulationGate.GameplayActive) return;
            transform.position += Vector3.left * (speed * Time.deltaTime);
            if (transform.position.x < leftBound)
                transform.position = new Vector3(rightBound, baseY, transform.position.z);
            var p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * 2.2f + bobPhase) * 0.10f;
            transform.position = p;

            if (frames == null || frames.Length == 0 || renderer == null)
                return;
            frameTimer += Time.deltaTime;
            const float frameInterval = 1f / 24f;
            if (frameTimer >= frameInterval)
            {
                int advance = Mathf.FloorToInt(frameTimer / frameInterval);
                frameTimer -= advance * frameInterval;
                frameIndex = (frameIndex + advance) % frames.Length;
                renderer.sprite = frames[frameIndex];
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!MobSimulationGate.GameplayActive) return;
            if (collision.collider.GetComponentInParent<CatController>() == null)
                return;
            CozyAudioManager.Instance?.PlayHurt();
            GameFlow.Instance?.ShowLevelFailure(GameText.L("Uçan kuşa çarptın. Uçuş yüksekliğini izle ve doğru zamanda geç!", "You bumped into a flying bird. Watch its height and pass at the right moment!", "Du bist gegen einen Vogel gestoßen. Achte auf seine Flughöhe und lauf im richtigen Moment vorbei!"));
        }
    }
}
