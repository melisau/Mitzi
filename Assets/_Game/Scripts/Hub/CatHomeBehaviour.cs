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
        Eating
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
        float stateTimer;
        bool interacting;
        ResidentActivity arrivalActivity = ResidentActivity.Standing;
        Transform visual;
        Animator animator;

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
            if (CurrentActivity == ResidentActivity.Walking)
            {
                if (animator != null)
                    animator.speed = 0.85f;
                var target = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
                float speed = interacting ? walkSpeed * 0.72f : walkSpeed;
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                Face(targetPosition.x);
                UpdateDepthOrder();
                if (Vector2.Distance(transform.position, targetPosition) < 0.05f)
                {
                    if (arrivalActivity == ResidentActivity.Eating)
                    {
                        CurrentActivity = ResidentActivity.Eating;
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
            else if (animator != null)
            {
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
            targetPosition = new Vector2(
                Mathf.Clamp(worldPoint.x, roomX.x, roomX.y),
                Mathf.Clamp(worldPoint.y, roomY.x, roomY.y));
            arrivalActivity = whenArrived;
            CurrentActivity = ResidentActivity.Walking;
            stateTimer = 20f;
        }

        void BeginWalk()
        {
            CurrentActivity = ResidentActivity.Walking;
            targetPosition = new Vector2(Random.Range(roomX.x, roomX.y), Random.Range(roomY.x, roomY.y));
            arrivalActivity = ResidentActivity.Standing;
            stateTimer = 12f;
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
