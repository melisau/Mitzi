using UnityEngine;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.Hub
{
    public enum ResidentActivity
    {
        Walking,
        Standing,
        Sitting,
        Sleeping,
        Eating,
        Drinking,
        Playing,
        Lying       // uyanık uzanma (boşta bekleme çeşitlemesi)
    }

    /// <summary>
    /// Ev kedisini oda içinde dolaştırır ve aralıklı dinlenme durumları üretir.
    /// Oturma/uyuma sprite'ları geldiğinde CurrentActivity üzerinden değiştirilebilir.
    /// </summary>
    public class CatHomeBehaviour : MonoBehaviour
    {
        [SerializeField] float walkSpeed = 0.78f;
        [SerializeField] float directedSpeedMultiplier = 2.15f;
        [SerializeField] float directedAnimationSpeed = 1.42f;
        [SerializeField] Vector2 roomX = new Vector2(-5.4f, 5.4f);
        // Kediler zeminde derinlik hissi verecek kadar hareket eder; üst sınır
        // yükselirse süpürgelik/duvar üzerine çıkmış gibi görünürler.
        [SerializeField] Vector2 roomY = new Vector2(-1.82f, -1.12f);

        public ResidentActivity CurrentActivity { get; private set; }
        public CatDefinition Definition { get; private set; }

        Vector2 targetPosition;
        Vector2 finalPosition;
        float stateTimer;
        bool interacting;
        bool navigatingDetour;
        bool directedMovement;
        ResidentActivity arrivalActivity = ResidentActivity.Standing;
        Transform visual;
        Animator animator;
        // Kontrolcüsü olmayan Animator'a Play/speed göndermek konsolu uyarıyla doldurur.
        bool HasAnimator => animator != null && animator.runtimeAnimatorController != null;
        ResidentActivity animatedActivity = (ResidentActivity)(-1);
        Vector2 lastProgressPosition;
        float stuckTimer;

        static readonly int WalkState = Animator.StringToHash("Walk");
        static readonly int SitState = Animator.StringToHash("Sit");
        static readonly int IdleState = Animator.StringToHash("Idle");
        static readonly int LieState = Animator.StringToHash("Lie");

        void Awake()
        {
            visual = transform.Find("Visual");
            animator = visual != null ? visual.GetComponent<Animator>() : GetComponentInChildren<Animator>();
            BeginRest();
            // Kediler aynı anda kalkıp yürümeye başlamasın.
            stateTimer += Random.Range(0f, 3f);
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
                float speed = directedMovement ? walkSpeed * directedSpeedMultiplier :
                    interacting ? walkSpeed * 0.72f : walkSpeed;
                if (HasAnimator)
                    animator.speed = directedMovement ? directedAnimationSpeed : 0.92f;
                transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
                if (Vector2.Distance(transform.position, lastProgressPosition) >= 0.025f)
                {
                    lastProgressPosition = transform.position;
                    stuckTimer = 0f;
                }
                else
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer >= 0.85f)
                    {
                        RecoverFromBlockedArea();
                        return;
                    }
                }
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
                    directedMovement = false;
                    if (arrivalActivity == ResidentActivity.Eating || arrivalActivity == ResidentActivity.Drinking ||
                        arrivalActivity == ResidentActivity.Playing || arrivalActivity == ResidentActivity.Sleeping)
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
            if (!HasAnimator || animatedActivity == CurrentActivity)
                return;

            animatedActivity = CurrentActivity;
            if (CurrentActivity == ResidentActivity.Walking)
            {
                if (animator.HasState(0, WalkState))
                    animator.Play(WalkState, 0, 0f);
                animator.speed = directedMovement ? directedAnimationSpeed : 0.92f;
            }
            else if (CurrentActivity == ResidentActivity.Sitting && animator.HasState(0, SitState))
            {
                animator.Play(SitState, 0, 0f);
                animator.speed = Random.Range(0.85f, 1.15f);
            }
            else if (CurrentActivity == ResidentActivity.Lying && animator.HasState(0, LieState))
            {
                animator.Play(LieState, 0, 0f);
                animator.speed = 1f;
            }
            else if (CurrentActivity == ResidentActivity.Standing && animator.HasState(0, IdleState))
            {
                // Ayakta bekleme: nefes alan duruş. Her kedi döngünün farklı yerinden ve
                // biraz farklı hızda başlar, böylece hepsi aynı anda aynı pozda olmaz.
                animator.Play(IdleState, 0, Random.value);
                animator.speed = Random.Range(0.75f, 1.25f);
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

        public void BindDefinition(CatDefinition definition) => Definition = definition;

        public void WalkTo(Vector2 worldPoint, ResidentActivity whenArrived = ResidentActivity.Standing)
        {
            // Oyuncunun dokunarak verdiği hedefler, rastgele oda dolaşmasından
            // daha hızlı ve daha akıcı tamamlanır. Detour kullanılması bunu bozmaz.
            directedMovement = true;
            finalPosition = new Vector2(
                Mathf.Clamp(worldPoint.x, roomX.x, roomX.y),
                Mathf.Clamp(worldPoint.y, roomY.x, roomY.y));
            arrivalActivity = whenArrived;
            CurrentActivity = ResidentActivity.Walking;
            stateTimer = 20f;
            ResetProgressWatch();
            PlanPathToFinal();
        }

        public void WalkDirectTo(Vector2 worldPoint, ResidentActivity whenArrived = ResidentActivity.Standing,
            bool exactHeight = false)
        {
            directedMovement = true;
            finalPosition = new Vector2(
                // Kenara yerleştirilen mama kabı/kedi ağacı da erişilebilir olsun.
                Mathf.Clamp(worldPoint.x, roomX.x - 1.20f, roomX.y + 1.20f),
                // Eşyaya giderken yükseklik eşyanın zeminine göre hesaplanır;
                // oda bandına sıkıştırmak kediyi kabın altında bırakıyordu.
                exactHeight ? worldPoint.y : Mathf.Clamp(worldPoint.y, roomY.x, roomY.y));
            targetPosition = finalPosition;
            arrivalActivity = whenArrived;
            navigatingDetour = false;
            CurrentActivity = ResidentActivity.Walking;
            stateTimer = 20f;
            ResetProgressWatch();
        }

        public void MoveAwayFrom(Vector2 point, float distance = 1.8f)
        {
            float direction = transform.position.x <= point.x ? -1f : 1f;
            Vector2 target = new Vector2(point.x + direction * distance,
                Mathf.Clamp(transform.position.y + Random.Range(-0.20f, 0.20f), roomY.x, roomY.y));
            WalkDirectTo(target, ResidentActivity.Standing);
        }

        void BeginWalk()
        {
            directedMovement = false;
            CurrentActivity = ResidentActivity.Walking;
            finalPosition = new Vector2(Random.Range(roomX.x, roomX.y), Random.Range(roomY.x, roomY.y));
            arrivalActivity = ResidentActivity.Standing;
            stateTimer = 12f;
            ResetProgressWatch();
            PlanPathToFinal();
        }

        void PlanPathToFinal()
        {
            Vector2 start = transform.position;
            var hits = Physics2D.LinecastAll(start, finalPosition);
            foreach (var hit in hits)
            {
                var furniture = hit.collider != null ? hit.collider.GetComponent<DraggableFurniture>() : null;
                // Halı zeminin bir parçasıdır; kedi üzerinden yürüyebilir. Kase,
                // su kabı, tuvalet, yatak ve tırmalama ağacı ise dolaşılır.
                if (furniture == null || !furniture.BlocksCats)
                    continue;

                Bounds bounds = furniture.CatObstacleBounds;
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

        void ResetProgressWatch()
        {
            lastProgressPosition = transform.position;
            stuckTimer = 0f;
        }

        void RecoverFromBlockedArea()
        {
            // Yakındaki dört yönü ve çaprazları tara; eşya tabanına denk gelmeyen
            // ilk noktaya çık. Hiçbiri uygun değilse yeni serbest hedef seç.
            Vector2 origin = transform.position;
            for (int ring = 1; ring <= 3; ring++)
            {
                float radius = 0.48f * ring;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI * 0.25f;
                    Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    candidate.x = Mathf.Clamp(candidate.x, roomX.x, roomX.y);
                    candidate.y = Mathf.Clamp(candidate.y, roomY.x, roomY.y);
                    if (IsFreeForCat(candidate))
                    {
                        targetPosition = candidate;
                        finalPosition = candidate;
                        navigatingDetour = false;
                        ResetProgressWatch();
                        return;
                    }
                }
            }

            BeginWalk();
        }

        bool IsFreeForCat(Vector2 point)
        {
            var hits = Physics2D.OverlapCircleAll(point, 0.26f);
            foreach (var hit in hits)
            {
                var furniture = hit.GetComponent<DraggableFurniture>();
                if (furniture != null && furniture.BlocksCats &&
                    furniture.CatObstacleBounds.Contains(point))
                    return false;
            }
            return true;
        }

        void BeginRest()
        {
            // Boşta bekleme: ayakta, oturma, uzanma veya uyuma rastgele seçilir;
            // süre ve bakılan yön de her kedi için farklıdır.
            float roll = Random.value;
            CurrentActivity = roll < 0.30f ? ResidentActivity.Standing
                : roll < 0.58f ? ResidentActivity.Sitting
                : roll < 0.80f ? ResidentActivity.Lying
                : ResidentActivity.Sleeping;
            stateTimer = CurrentActivity == ResidentActivity.Sleeping ? Random.Range(6f, 12f)
                : CurrentActivity == ResidentActivity.Lying ? Random.Range(4f, 9f)
                : Random.Range(2.5f, 6f);
            if (Random.value < 0.4f && visual != null)
                Face(transform.position.x + (visual.localScale.x >= 0f ? -1f : 1f));
        }

        void Face(float x)
        {
            if (visual == null)
                return;
            var scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * (x >= transform.position.x ? 1f : -1f);
            visual.localScale = scale;
        }

        public void FaceTowards(float x) => Face(x);

        /// <summary>Uyku düğmesi: kedi olduğu yerde (ör. yatağında) kıvrılıp bir süre uyur.</summary>
        public void SleepFor(float seconds)
        {
            directedMovement = false;
            arrivalActivity = ResidentActivity.Standing;
            CurrentActivity = ResidentActivity.Sleeping;
            stateTimer = seconds;
        }

        void UpdateDepthOrder()
        {
            var renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 10 + Mathf.RoundToInt(-transform.position.y * 10f);
        }
    }
}
