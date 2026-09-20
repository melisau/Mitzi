using UnityEngine;
using PawPath.Core;

namespace PawPath.Hub
{
    public enum ResidentActivity
    {
        Walking,
        Standing,
        Sitting,
        Sleeping,
        Eating,
        Drinking
    }

    /// <summary>
    /// Ev kedisini oda içinde dolaştırır ve aralıklı dinlenme durumları üretir.
    /// Oturma/uyuma sprite'ları geldiğinde CurrentActivity üzerinden değiştirilebilir.
    /// </summary>
    public class CatHomeBehaviour : MonoBehaviour
    {
        [SerializeField] float walkSpeed = 0.65f;
        [SerializeField] Vector2 roomX = new Vector2(-5.4f, 5.4f);
        [SerializeField] Vector2 roomY = new Vector2(-1.80f, -0.45f);

        public ResidentActivity CurrentActivity { get; private set; }

        Vector2 targetPosition;
        Vector2 finalPosition;
        float stateTimer;
        bool interacting;
        bool navigatingDetour;
        ResidentActivity arrivalActivity = ResidentActivity.Standing;
        Transform visual;
        Animator animator;
        ResidentActivity animatedActivity = (ResidentActivity)(-1);

        static readonly int WalkState = Animator.StringToHash("Walk");
        static readonly int SitState = Animator.StringToHash("Sit");

        void Awake()
        {
            visual = transform.Find("Visual");
            animator = visual != null ? visual.GetComponent<Animator>() : GetComponentInChildren<Animator>();
            BeginRest();
        }

        void Update()
        {
            if (GameFlow.Instance == null || !GameFlow.Instance.InHub)
                return;

            stateTimer -= Time.deltaTime;
            UpdateAnimation();
            if (CurrentActivity == ResidentActivity.Walking)
            {
                var target = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
                float speed = interacting ? walkSpeed * 0.72f : walkSpeed;
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                Face(targetPosition.x);
                UpdateDepthOrder();
                if (Vector2.Distance(transform.position, targetPosition) < 0.05f)
                {
                    if (navigatingDetour)
                    {
                        navigatingDetour = false;
                        PlanPathToFinal();
                        return;
                    }
                    if (arrivalActivity == ResidentActivity.Eating || arrivalActivity == ResidentActivity.Drinking)
                    {
                        CurrentActivity = arrivalActivity;
                        stateTimer = 3.2f;
                        arrivalActivity = ResidentActivity.Standing;
                    }
                    else
                        BeginRest();
                }
            }
            else if (stateTimer <= 0f)
            {
                BeginWalk();
            }
        }

        void UpdateAnimation()
        {
            if (animator == null || animatedActivity == CurrentActivity)
                return;

            animatedActivity = CurrentActivity;
            if (CurrentActivity == ResidentActivity.Walking)
            {
                if (animator.HasState(0, WalkState))
                    animator.Play(WalkState, 0, 0f);
                animator.speed = 0.85f;
            }
            else if (CurrentActivity == ResidentActivity.Sitting && animator.HasState(0, SitState))
            {
                animator.Play(SitState, 0, 0f);
                animator.speed = 1f;
            }
            else
            {
                if (animator.HasState(0, WalkState))
                    animator.Play(WalkState, 0, 0f);
                animator.speed = 0f;
            }
        }

        public void SetInteracting(bool value)
        {
            interacting = value;
            if (value && CurrentActivity != ResidentActivity.Walking)
                BeginWalk();
            else if (!value && CurrentActivity == ResidentActivity.Standing)
                BeginRest();
        }

        public void WalkTo(Vector2 worldPoint, ResidentActivity whenArrived = ResidentActivity.Standing)
        {
            finalPosition = new Vector2(
                Mathf.Clamp(worldPoint.x, roomX.x, roomX.y),
                Mathf.Clamp(worldPoint.y, roomY.x, roomY.y));
            arrivalActivity = whenArrived;
            CurrentActivity = ResidentActivity.Walking;
            stateTimer = 20f;
            PlanPathToFinal();
        }

        void BeginWalk()
        {
            CurrentActivity = ResidentActivity.Walking;
            finalPosition = new Vector2(Random.Range(roomX.x, roomX.y), Random.Range(roomY.x, roomY.y));
            arrivalActivity = ResidentActivity.Standing;
            stateTimer = 12f;
            PlanPathToFinal();
        }

        void PlanPathToFinal()
        {
            Vector2 start = transform.position;
            var hits = Physics2D.LinecastAll(start, finalPosition);
            foreach (var hit in hits)
            {
                var furniture = hit.collider != null ? hit.collider.GetComponent<DraggableFurniture>() : null;
                if (furniture == null)
                    continue;

                Bounds bounds = hit.collider.bounds;
                const float clearance = 0.32f;
                float above = Mathf.Clamp(bounds.max.y + clearance, roomY.x, roomY.y);
                float below = Mathf.Clamp(bounds.min.y - clearance, roomY.x, roomY.y);
                float detourY = Mathf.Abs(above - start.y) <= Mathf.Abs(below - start.y) ? above : below;
                float detourX = start.x <= bounds.center.x
                    ? bounds.min.x - clearance
                    : bounds.max.x + clearance;
                targetPosition = new Vector2(
                    Mathf.Clamp(detourX, roomX.x, roomX.y),
                    detourY);
                navigatingDetour = true;
                return;
            }

            targetPosition = finalPosition;
            navigatingDetour = false;
        }

        void BeginRest()
        {
            float roll = Random.value;
            CurrentActivity = roll < 0.45f
                ? ResidentActivity.Standing
                : roll < 0.78f ? ResidentActivity.Sitting : ResidentActivity.Sleeping;
            stateTimer = CurrentActivity == ResidentActivity.Sleeping
                ? Random.Range(5f, 9f)
                : Random.Range(2f, 5f);
        }

        void Face(float x)
        {
            if (visual == null)
                return;
            var scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * (x >= transform.position.x ? 1f : -1f);
            visual.localScale = scale;
        }

        void UpdateDepthOrder()
        {
            var renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 10 + Mathf.RoundToInt(-transform.position.y * 10f);
        }
    }
}
