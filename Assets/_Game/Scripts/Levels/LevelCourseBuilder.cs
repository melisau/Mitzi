using UnityEngine;
using PawPath.Hub;

namespace PawPath.Levels
{
    /// <summary>
    /// Kedinin kendiliğinden yürüyebileceği hazır yolu kurar. Oyuncu yalnızca
    /// yolun eksik kısımlarını ve yüksek engelleri çizerek aşar.
    /// </summary>
    public class LevelCourseBuilder : MonoBehaviour
    {
        const float RoadCenterY = -1.85f;
        const float RoadHeight = 0.70f;
        static readonly Color RoadColor = new Color(0.72f, 0.54f, 0.38f, 1f);
        static readonly Color ObstacleColor = new Color(0.61f, 0.42f, 0.29f, 1f);

        Transform generatedRoot;
        Sprite gapSprite;
        Sprite moundSprite;
        Sprite roadSprite;

        public float CatSpawnY => RoadCenterY + RoadHeight * 0.5f + 0.32f;
        public float GoalY => CatSpawnY + 0.15f;

        public void BindVisuals(Sprite gap, Sprite mound, Sprite road)
        {
            gapSprite = gap;
            moundSprite = mound;
            roadSprite = road;
        }

        public void Build(int levelNumber)
        {
            ClearGenerated();
            generatedRoot = new GameObject("GeneratedCourse").transform;
            generatedRoot.SetParent(transform, false);

            int pattern = Mathf.Abs(levelNumber - 1) % 5;
            switch (pattern)
            {
                case 0:
                    // İlk bölüm: tek, kolay bir çukur.
                    Road(-5.15f, 4.7f);
                    Road(2.95f, 9.1f);
                    Gap(-2.2f, 1.2f);
                    break;
                case 1:
                    // İki kısa çukur.
                    Road(-5.45f, 4.1f);
                    Road(-0.15f, 4.1f);
                    Road(5.25f, 4.5f);
                    Gap(-2.8f, 1.2f);
                    Gap(2.45f, 1.1f);
                    break;
                case 2:
                    // Bir çukur ve üzerinden çizilecek yüksek bir tümsek.
                    Road(-5.25f, 4.5f);
                    Road(2.8f, 9.4f);
                    Gap(-2.45f, 1.1f);
                    Obstacle(2.0f, 0.9f, 0.9f);
                    break;
                case 3:
                    // Tümsekten sonra ikinci bir kopuk yol.
                    Road(-5.45f, 4.1f);
                    Road(-0.25f, 4.1f);
                    Road(5.25f, 4.5f);
                    Gap(-2.8f, 1.2f);
                    Gap(2.45f, 1.1f);
                    Obstacle(-0.7f, 0.85f, 0.8f);
                    break;
                default:
                    // Sezon sonu: iki çukur ve daha geniş bir tümsek.
                    Road(-5.55f, 3.9f);
                    Road(-0.35f, 4.1f);
                    Road(5.25f, 4.5f);
                    Gap(-2.95f, 1.1f);
                    Gap(2.5f, 1.0f);
                    Obstacle(0.15f, 1.15f, 1.0f);
                    break;
            }
        }

        void Road(float centerX, float width)
        {
            var go = new GameObject("Road");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(centerX, RoadCenterY);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width, RoadHeight);

            if (roadSprite == null)
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = FallbackSprite.WhiteSquare();
                renderer.color = RoadColor;
                renderer.sortingOrder = 1;
                go.transform.localScale = new Vector3(width, RoadHeight, 1f);
                collider.size = Vector2.one;
                return;
            }

            int tileCount = Mathf.Max(1, Mathf.CeilToInt(width / 3.1f));
            float tileWidth = width / tileCount;
            float left = centerX - width * 0.5f;
            for (int i = 0; i < tileCount; i++)
            {
                float x = left + tileWidth * (i + 0.5f);
                // Görsel dosyasının üstünde/altında şeffaf pay var. X ve Y'yi
                // ayrı ölçekleyerek taş kesitini kalın, yürüme yüzeyini düz tutuyoruz.
                Decoration("RoadVisual", roadSprite, new Vector2(x, RoadCenterY - 0.02f),
                    tileWidth + 0.12f, 1, 2.85f);
            }
        }

        void Obstacle(float centerX, float width, float height)
        {
            float obstacleWidth = width * 1.45f;
            float obstacleHeight = height * 1.45f;
            float roadTop = RoadCenterY + RoadHeight * 0.5f;
            float centerY = roadTop + obstacleHeight * 0.5f;
            var go = new GameObject("Hump");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(centerX, centerY);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(obstacleWidth, obstacleHeight);

            if (moundSprite != null)
            {
                // Üretilen orman tümseği PNG'sinin altında geniş şeffaf tuval payı var.
                // Görünen yosun/toprak tabanını yolun içine oturtmak için yalnızca bu
                // görsele daha fazla bindirme uygula.
                float visualOverlap = moundSprite.name.Contains("forest") ? 0.52f : 0.04f;
                DecorationBottomAligned("HumpVisual", moundSprite, centerX, roadTop - visualOverlap,
                    obstacleWidth * 1.82f, 2);
            }
            else
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = FallbackSprite.WhiteSquare();
                renderer.color = ObstacleColor;
                renderer.sortingOrder = 2;
                go.transform.localScale = new Vector3(obstacleWidth, obstacleHeight, 1f);
                collider.size = Vector2.one;
            }
        }

        void Gap(float centerX, float width)
        {
            if (gapSprite != null)
                // Üst kırık kenarlar yol hizasında kalır; yalnızca aşağıdaki
                // toprak kesiti büyütülerek çukur daha derin görünür.
                Decoration("GapVisual", gapSprite, new Vector2(centerX, RoadCenterY - 0.28f),
                    width + 1.55f, 0, 2.10f);
        }

        void Decoration(string objectName, Sprite sprite, Vector2 position, float targetWidth,
            int sortingOrder, float targetCanvasHeight = 0f)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            if (sprite.bounds.size.x > 0f)
            {
                float scaleX = targetWidth / sprite.bounds.size.x;
                float scaleY = targetCanvasHeight > 0f && sprite.bounds.size.y > 0f
                    ? targetCanvasHeight / sprite.bounds.size.y
                    : scaleX;
                go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }

        void DecorationBottomAligned(string objectName, Sprite sprite, float centerX,
            float bottomY, float targetWidth, int sortingOrder)
        {
            if (sprite == null || sprite.bounds.size.x <= 0f)
                return;

            float scale = targetWidth / sprite.bounds.size.x;
            // Sprite pivot'i ve şeffaf kenarları değişse bile görünen bounds'in altını
            // yol yüzeyine birkaç piksel gömerek aradaki boşluğu tamamen kapatır.
            float centerY = bottomY - sprite.bounds.min.y * scale;
            Decoration(objectName, sprite, new Vector2(centerX, centerY), targetWidth, sortingOrder);
        }

        void CreateSolid(string objectName, Vector2 position, Vector2 size, Color color, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = FallbackSprite.WhiteSquare();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        void ClearGenerated()
        {
            if (generatedRoot != null)
                Destroy(generatedRoot.gameObject);
        }
    }
}
