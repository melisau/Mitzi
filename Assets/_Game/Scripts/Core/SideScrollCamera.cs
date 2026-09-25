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
            float desiredX = minX;
            bool preparing = GameFlow.Instance != null &&
                GameFlow.Instance.State == GameFlowState.DrawingPreparation;
            if (preparing)
                desiredX = Mathf.Min(maxX, minX + preparationViewIndex * PreparationStep);
            else if (GameFlow.Instance != null && !GameFlow.Instance.InHub && target != null)
                desiredX = Mathf.Clamp(target.position.x + followOffset, minX, maxX);

            float x = Mathf.SmoothDamp(transform.position.x, desiredX, ref velocityX,
                smoothTime, Mathf.Infinity, preparing ? Time.unscaledDeltaTime : Time.deltaTime);
            transform.position = new Vector3(x, startY, transform.position.z);

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

        public void Bind(Transform followTarget, params Transform[] backgrounds)
        {
            target = followTarget;
            backdropLayers = backgrounds;
        }

        public void ResetView()
        {
            preparationViewIndex = 0;
            velocityX = 0f;
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
