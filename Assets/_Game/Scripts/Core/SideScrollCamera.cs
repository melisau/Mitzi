using UnityEngine;

namespace PawPath.Core
{
    /// <summary>
    /// Kedi ekranın sol tarafındaki güvenli bölgeyi geçince kamerayı yatay takip ettirir.
    /// Düz renk arka plan katmanlarını kamerayla taşıyarak sonsuz fon hissi verir.
    /// </summary>
    public class SideScrollCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Transform[] backdropLayers;
        [SerializeField] float followOffset = 1.35f;
        [SerializeField] float minX;
        [SerializeField] float maxX = 13.2f;
        [SerializeField] float smoothTime = 0.22f;

        float startY;
        float velocityX;
        float velocityY;
        PawPath.Cat.CatController followedCat;
        SpriteRenderer catVisual;
        int preparationViewIndex;

        float PreparationStep
        {
            get
            {
                var camera = GetComponent<Camera>();
                return camera != null ? Mathf.Max(1f, camera.orthographicSize * 2f * camera.aspect * 0.82f) : 13f;
            }
        }

        public int PreparationViewIndex => preparationViewIndex;
        public int PreparationViewCount => Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / PreparationStep) + 1);
        public bool IsSecondPreparationHalf => preparationViewIndex >= PreparationViewCount / 2;

        public void MovePreparationView(int direction)
        {
            if (GameFlow.Instance == null || GameFlow.Instance.State != GameFlowState.DrawingPreparation)
                return;
            preparationViewIndex = Mathf.Clamp(preparationViewIndex + direction, 0, PreparationViewCount - 1);
            velocityX = 0f;
            transform.position = new Vector3(Mathf.Min(maxX, minX + preparationViewIndex * PreparationStep),
                startY, transform.position.z);
        }

        void Awake()
        {
            startY = transform.position.y;
        }

        void LateUpdate()
        {
            if (GameFlow.Instance != null && GameFlow.Instance.InHub && PawPath.Hub.HomeRooms.Instance != null)
            {
                var homePosition = transform.position;
                homePosition.y = startY;
                transform.position = homePosition;
                velocityY = 0f;
                return;
            }
            float desiredX = minX;
            bool preparing = GameFlow.Instance != null &&
                GameFlow.Instance.State == GameFlowState.DrawingPreparation;
            if (preparing)
                desiredX = Mathf.Min(maxX, minX + preparationViewIndex * PreparationStep);
            else if (GameFlow.Instance != null && !GameFlow.Instance.InHub && target != null)
                desiredX = Mathf.Clamp(target.position.x + followOffset, minX, maxX);

            float x = Mathf.SmoothDamp(transform.position.x, desiredX, ref velocityX,
                smoothTime, Mathf.Infinity, preparing ? Time.unscaledDeltaTime : Time.deltaTime);
            float desiredY = ClimbingViewY(preparing);
            float y = Mathf.SmoothDamp(transform.position.y, desiredY, ref velocityY,
                smoothTime, Mathf.Infinity, preparing ? Time.unscaledDeltaTime : Time.deltaTime);
            transform.position = new Vector3(x, y, transform.position.z);

            if (backdropLayers == null)
                return;

            foreach (var layer in backdropLayers)
            {
                if (layer == null)
                    continue;
                var p = layer.position;
                layer.position = new Vector3(x, p.y, p.z);
            }
        }

        float ClimbingViewY(bool preparing)
        {
            var view = GetComponent<Camera>();
            if (!preparing && followedCat != null && followedCat.IsClimbing && catVisual != null && view != null)
                return startY + Mathf.Max(0f, catVisual.bounds.max.y - (startY + view.orthographicSize * 0.65f));
            return startY;
        }

        public void Bind(Transform followTarget, params Transform[] backgrounds)
        {
            target = followTarget;
            followedCat = target != null ? target.GetComponent<PawPath.Cat.CatController>() : null;
            catVisual = target != null ? target.GetComponentInChildren<SpriteRenderer>() : null;
            backdropLayers = backgrounds;
        }

        public void ResetView()
        {
            preparationViewIndex = 0;
            velocityX = 0f;
            velocityY = 0f;
            transform.position = new Vector3(minX, startY, transform.position.z);
        }

        public void SetCourseGoalX(float goalX)
        {
            // Kapı ekranın sağ tarafında kalsın; uzatılmış yolun sonunda
            // kameranın eski 13.2 sınırında takılması engellenir.
            maxX = Mathf.Max(minX, goalX - 6.15f);
        }
    }
}
