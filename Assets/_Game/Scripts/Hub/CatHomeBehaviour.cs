using UnityEngine;
using PawPath.Core;

namespace PawPath.Hub
{
    public enum ResidentActivity
    {
        Walking,
        Standing,
        Sitting,
        Sleeping
    }

    /// <summary>
    /// Ev kedisini oda içinde dolaştırır ve aralıklı dinlenme durumları üretir.
    /// Oturma/uyuma sprite'ları geldiğinde CurrentActivity üzerinden değiştirilebilir.
    /// </summary>
    public class CatHomeBehaviour : MonoBehaviour
    {
        [SerializeField] float walkSpeed = 0.65f;
        [SerializeField] Vector2 roomX = new Vector2(-5.4f, 5.4f);
        [SerializeField] float floorY = -1.1f;

        public ResidentActivity CurrentActivity { get; private set; }

        float targetX;
        float stateTimer;
        bool interacting;
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
            if (interacting || GameFlow.Instance == null || !GameFlow.Instance.InHub)
                return;

            stateTimer -= Time.deltaTime;
            if (CurrentActivity == ResidentActivity.Walking)
            {
                if (animator != null)
                    animator.speed = 0.85f;
                var target = new Vector3(targetX, floorY, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
                Face(targetX);
                if (Mathf.Abs(transform.position.x - targetX) < 0.05f)
                    BeginRest();
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
            if (value)
                CurrentActivity = ResidentActivity.Standing;
            else
                BeginRest();
        }

        void BeginWalk()
        {
            CurrentActivity = ResidentActivity.Walking;
            targetX = Random.Range(roomX.x, roomX.y);
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
    }
}
