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
        const float RoadCenterY = -1.72f;
        const float RoadHeight = 0.42f;
        static readonly Color RoadColor = new Color(0.72f, 0.54f, 0.38f, 1f);
        static readonly Color ObstacleColor = new Color(0.61f, 0.42f, 0.29f, 1f);

        Transform generatedRoot;

        public float CatSpawnY => RoadCenterY + RoadHeight * 0.5f + 0.32f;
        public float GoalY => CatSpawnY + 0.15f;

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
                    Road(2.65f, 9.7f);
                    break;
                case 1:
                    // İki kısa çukur.
                    Road(-5.45f, 4.1f);
                    Road(-0.15f, 4.1f);
                    Road(5.25f, 4.5f);
                    break;
                case 2:
                    // Bir çukur ve üzerinden çizilecek yüksek bir tümsek.
                    Road(-5.25f, 4.5f);
                    Road(2.65f, 9.7f);
                    Obstacle(2.0f, 0.9f, 0.9f);
                    break;
                case 3:
                    // Tümsekten sonra ikinci bir kopuk yol.
                    Road(-5.45f, 4.1f);
                    Road(-0.25f, 4.1f);
                    Road(5.25f, 4.5f);
                    Obstacle(-0.7f, 0.85f, 0.8f);
                    break;
                default:
                    // Sezon sonu: iki çukur ve daha geniş bir tümsek.
                    Road(-5.55f, 3.9f);
                    Road(-0.35f, 4.1f);
                    Road(5.25f, 4.5f);
                    Obstacle(0.15f, 1.15f, 1.0f);
                    break;
            }
        }

        void Road(float centerX, float width)
        {
            CreateSolid("Road", new Vector2(centerX, RoadCenterY), new Vector2(width, RoadHeight), RoadColor, 1);
        }

        void Obstacle(float centerX, float width, float height)
        {
            float centerY = RoadCenterY + RoadHeight * 0.5f + height * 0.5f;
            CreateSolid("Hump", new Vector2(centerX, centerY), new Vector2(width, height), ObstacleColor, 2);
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
