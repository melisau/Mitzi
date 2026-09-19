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
        [SerializeField] float followOffset = 2.5f;
        [SerializeField] float minX;
        [SerializeField] float maxX = 12f;
        [SerializeField] float smoothTime = 0.22f;

        float startY;
        float velocityX;

        void Awake()
        {
            startY = transform.position.y;
        }

        void LateUpdate()
        {
            float desiredX = minX;
            if (GameFlow.Instance != null && !GameFlow.Instance.InHub && target != null)
                desiredX = Mathf.Clamp(target.position.x + followOffset, minX, maxX);

            float x = Mathf.SmoothDamp(transform.position.x, desiredX, ref velocityX, smoothTime);
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
    }
}
