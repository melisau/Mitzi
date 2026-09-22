using UnityEngine;

namespace PawPath.Core
{
    /// <summary>
    /// Tek bir arka plan sprite'ını yedi yatay karo hâline getirir ve kameradan
    /// daha yavaş taşıyarak ilerleme/parallax hissi verir.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float followFactor = 0.32f;

        Camera targetCamera;
        SpriteRenderer centerRenderer;
        float startCameraX;
        float startLayerX;

        public void Bind(Camera cameraToFollow, SpriteRenderer source, float factor = 0.32f)
        {
            targetCamera = cameraToFollow;
            centerRenderer = source;
            followFactor = Mathf.Clamp01(factor);
            startCameraX = targetCamera != null ? targetCamera.transform.position.x : 0f;
            startLayerX = transform.position.x;
            BuildSideTiles();
        }

        void LateUpdate()
        {
            if (targetCamera == null)
                return;
            float cameraTravel = targetCamera.transform.position.x - startCameraX;
            var position = transform.position;
            position.x = startLayerX + cameraTravel * followFactor;
            transform.position = position;
        }

        void BuildSideTiles()
        {
            if (centerRenderer == null || centerRenderer.sprite == null)
                return;

            float localWidth = centerRenderer.sprite.bounds.size.x;
            CreateTile("BackdropTile_-2", -2f * localWidth, false);
            CreateTile("BackdropTile_-3", -3f * localWidth, true);
            CreateTile("BackdropTile_-1", -localWidth, true);
            CreateTile("BackdropTile_1", localWidth, true);
            CreateTile("BackdropTile_2", 2f * localWidth, false);
            CreateTile("BackdropTile_3", 3f * localWidth, true);
        }

        public void SetSprite(Sprite sprite)
        {
            if (centerRenderer == null || sprite == null)
                return;
            centerRenderer.sprite = sprite;
            float localWidth = sprite.bounds.size.x;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var renderer = child.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    continue;
                renderer.sprite = sprite;
                string suffix = child.name.Replace("BackdropTile_", "");
                if (!int.TryParse(suffix, out int tileIndex))
                    continue;
                child.localPosition = new Vector3(tileIndex * localWidth, 0f, 0f);
                renderer.flipX = Mathf.Abs(tileIndex) % 2 == 1;
            }
        }

        void CreateTile(string tileName, float localX, bool mirror)
        {
            var tile = new GameObject(tileName);
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = new Vector3(localX, 0f, 0f);
            var renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = centerRenderer.sprite;
            renderer.flipX = mirror;
            renderer.color = centerRenderer.color;
            renderer.sortingLayerID = centerRenderer.sortingLayerID;
            renderer.sortingOrder = centerRenderer.sortingOrder;
        }
    }
}
