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
        public const float GameplayScale = 1.20f;
        const float RoadCenterY = -1.85f;
        const float RoadHeight = 0.96f * GameplayScale;
        const float SecondSectionOffset = 13.0f;
        static readonly Color RoadColor = new Color(0.72f, 0.54f, 0.38f, 1f);
        static readonly Color ObstacleColor = new Color(0.61f, 0.42f, 0.29f, 1f);

        Transform generatedRoot;
        Sprite gapSprite;
        Sprite moundSprite;
        Sprite roadSprite;

        public float CatSpawnY => RoadCenterY + RoadHeight * 0.5f + 0.40f * GameplayScale;
        public float GoalY => CatSpawnY + 0.15f;
        public float GoalX => 19.35f;

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
            // Kameranın ilk ve son ekranında kaldırımın kadraj dışında da devam
            // etmesini sağlar; yalnızca tasarlanmış çukurlar açık kalır.
            Road(-8.25f, 2.2f);
            BuildPattern(pattern, 0f);
            BuildPattern((pattern + 2) % 5, SecondSectionOffset);
            Road(21.55f, 2.9f);
        }

        void BuildPattern(int pattern, float offsetX)
        {
            switch (pattern)
            {
                case 0:
                    // İlk bölüm: tek, kolay bir çukur.
                    Road(-5.15f + offsetX, 4.7f);
                    Road(2.95f + offsetX, 9.1f);
                    Gap(-2.2f + offsetX, 1.35f);
                    break;
                case 1:
                    // İki kısa çukur.
                    Road(-5.45f + offsetX, 4.1f);
                    Road(-0.15f + offsetX, 4.1f);
                    Road(5.25f + offsetX, 4.5f);
                    Gap(-2.8f + offsetX, 1.35f);
                    Gap(2.45f + offsetX, 1.25f);
                    break;
                case 2:
                    // Bir çukur ve üzerinden çizilecek yüksek bir tümsek.
                    Road(-5.25f + offsetX, 4.5f);
                    Road(2.8f + offsetX, 9.4f);
                    Gap(-2.45f + offsetX, 1.25f);
                    Obstacle(2.0f + offsetX, 1.08f, 1.05f);
                    break;
                case 3:
                    // Tümsekten sonra ikinci bir kopuk yol.
                    Road(-5.45f + offsetX, 4.1f);
                    Road(-0.25f + offsetX, 4.1f);
                    Road(5.25f + offsetX, 4.5f);
                    Gap(-2.8f + offsetX, 1.35f);
                    Gap(2.45f + offsetX, 1.25f);
                    Obstacle(-0.7f + offsetX, 1.05f, 0.95f);
                    break;
                default:
                    // Sezon sonu: iki çukur ve daha geniş bir tümsek.
                    Road(-5.55f + offsetX, 3.9f);
                    Road(-0.35f + offsetX, 4.1f);
                    Road(5.25f + offsetX, 4.5f);
                    Gap(-2.95f + offsetX, 1.25f);
                    Gap(2.5f + offsetX, 1.18f);
                    Obstacle(0.15f + offsetX, 1.32f, 1.18f);
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
            float roadTop = RoadCenterY + RoadHeight * 0.5f;
            CreateRoadUnderfill(centerX, width + 0.18f, roadTop);
            for (int i = 0; i < tileCount; i++)
            {
                float x = left + tileWidth * (i + 0.5f);
                // Üst çim/yürüme yüzeyi eski doğru konumunda kalır. Ekranın altına
                // uzanan kalınlık ayrı bir dolgu görseliyle sağlanır.
                Decoration("RoadVisual", roadSprite, new Vector2(x, RoadCenterY - 0.02f),
                    tileWidth + 0.18f, 1, 3.35f * GameplayScale);
            }
        }

        void Obstacle(float centerX, float width, float height)
        {
            bool isTallVariant = Random.value >= 0.5f;
            // Uzun varyant eskiden 2 kat yüksek ve normal varyantla aynı genişlikteydi.
            // Bu, yamacı gereğinden dikleştiriyor ve çift zıplamayla bile geçilemeyen
            // bir collider tepesi oluşturuyordu. Biraz alçaltıp tabanını genişletiyoruz.
            float heightVariant = isTallVariant ? 1.55f : 1f;
            float widthVariant = isTallVariant ? 1.15f : 1f;
            float obstacleWidth = width * 1.62f * widthVariant * GameplayScale;
            float obstacleHeight = height * 1.62f * heightVariant * GameplayScale;
            float visualWidth = obstacleWidth * 1.95f;
            float visualOverlap = (moundSprite != null && moundSprite.name.Contains("forest") ? 0.52f : 0.95f) *
                heightVariant * GameplayScale;
            float colliderHeight = obstacleHeight;
            if (moundSprite != null && moundSprite.bounds.size.x > 0f)
            {
                // Görsel genişlikten ölçeklendiği için, hedef yükseklik oranını korumak
                // adına genişlik artışını dikey ölçekten çıkarıyoruz. Görselin yolun
                // içine gömülen kısmı yürünebilir tepe değildir; collider hesabından
                // çıkarılmazsa kedi görünen tümseğin üstünde havada kalır.
                float spriteHeightScale = heightVariant / widthVariant;
                float visibleRatioHeight = visualWidth * moundSprite.bounds.size.y / moundSprite.bounds.size.x * spriteHeightScale;
                float visibleHeightAboveRoad = Mathf.Max(0.35f, visibleRatioHeight - visualOverlap);
                colliderHeight = Mathf.Min(obstacleHeight, visibleHeightAboveRoad * 0.90f);
            }
            float roadTop = RoadCenterY + RoadHeight * 0.5f;
            // Alfa boşluğu yalnızca görsel yerleşimine aittir. Fizik şeklinin
            // tabanı her zaman gerçek kaldırım yüzeyinde kalmalıdır.
            float centerY = roadTop + colliderHeight * 0.5f;
            var go = new GameObject("Hump");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(centerX, centerY);
            var collider = go.AddComponent<PolygonCollider2D>();
            // Kedi eğimde ve özellikle tepe birleşiminde sürtünmeye takılmasın.
            collider.sharedMaterial = new PhysicsMaterial2D("HumpNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            float halfW = obstacleWidth * 0.5f;
            float halfH = colliderHeight * 0.5f;
            // Dikdörtgen collider kediyi görselin boş köşelerinde havada tutuyordu.
            // Tepeyi tek keskin nokta yerine kısa bir düzlük yapıyoruz. CircleCollider
            // aksi halde iki eğimin birleştiği noktaya sıkışıp ilerleyemiyordu.
            collider.points = new[]
            {
                new Vector2(-halfW, -halfH),
                new Vector2(-halfW * 0.86f, -halfH * 0.62f),
                new Vector2(-halfW * 0.52f, halfH * 0.12f),
                new Vector2(-halfW * 0.18f, halfH * 0.76f),
                new Vector2(halfW * 0.18f, halfH * 0.76f),
                new Vector2(halfW * 0.52f, halfH * 0.12f),
                new Vector2(halfW * 0.86f, -halfH * 0.62f),
                new Vector2(halfW, -halfH)
            };

            if (moundSprite != null)
            {
                // Üretilen orman tümseği PNG'sinin altında geniş şeffaf tuval payı var.
                // Görünen yosun/toprak tabanını yolun içine oturtmak için yalnızca bu
                // görsele daha fazla bindirme uygula.
                DecorationBottomAligned("HumpVisual", moundSprite, centerX, roadTop - visualOverlap,
                    visualWidth, 2, heightVariant / widthVariant);
            }
            else
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = FallbackSprite.WhiteSquare();
                renderer.color = ObstacleColor;
                renderer.sortingOrder = 2;
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(obstacleWidth, obstacleHeight);
            }
        }

        void Gap(float centerX, float width)
        {
            if (gapSprite != null)
            {
                // Üst kırık kenarlar yol hizasında kalır; yalnızca aşağıdaki
                // toprak kesiti büyütülerek çukur daha derin görünür. Merkez
                // konumu yol yüzeyinden ölçeklendiği için büyürken bağlantı kopmaz.
                float roadTop = RoadCenterY + RoadHeight * 0.5f;
                // Görselin en yüksek uçları doğrudan yol yüzeyine sabitlenir; böylece
                // orman çukurunun yan dudakları yükselmez ve arada boşluk kalmaz.
                DecorationTopAligned("GapVisual", gapSprite, centerX,
                    roadTop, (width + 1.95f) * GameplayScale, -2, 5.8f);
            }
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
            float bottomY, float targetWidth, int sortingOrder, float heightScale = 1f)
        {
            if (sprite == null || sprite.bounds.size.x <= 0f)
                return;

            float scale = targetWidth / sprite.bounds.size.x;
            // Sprite pivot'i ve şeffaf kenarları değişse bile görünen bounds'in altını
            // yol yüzeyine birkaç piksel gömerek aradaki boşluğu tamamen kapatır.
            float scaleY = scale * heightScale;
            float centerY = bottomY - sprite.bounds.min.y * scaleY;
            Decoration(objectName, sprite, new Vector2(centerX, centerY), targetWidth, sortingOrder,
                sprite.bounds.size.y * scaleY);
        }

        void DecorationTopAligned(string objectName, Sprite sprite, float centerX,
            float topY, float targetWidth, int sortingOrder, float targetHeight)
        {
            if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
                return;

            float scaleX = targetWidth / sprite.bounds.size.x;
            float scaleY = targetHeight / sprite.bounds.size.y;
            // Sprite'ın gerçek üst bounds'u tam yol yüzeyine oturur. X ekseninde
            // komşu karolar hafifçe bindirilerek aradaki ince çizgiler kapatılır.
            float centerY = topY - sprite.bounds.max.y * scaleY;
            Decoration(objectName, sprite, new Vector2(centerX, centerY), targetWidth,
                sortingOrder, targetHeight);
        }

        void CreateRoadUnderfill(float centerX, float width, float topY)
        {
            const float bottomY = -5.35f;
            float height = topY - bottomY;
            var go = new GameObject("RoadUnderfill");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(centerX, bottomY + height * 0.5f);
            go.transform.localScale = new Vector3(width, height, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = FallbackSprite.WhiteSquare();
            bool forest = roadSprite != null && roadSprite.name.ToLowerInvariant().Contains("forest");
            renderer.color = forest
                ? new Color(0.115f, 0.095f, 0.065f, 1f)
                : new Color(0.34f, 0.20f, 0.12f, 1f);
            renderer.sortingOrder = -1;
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
