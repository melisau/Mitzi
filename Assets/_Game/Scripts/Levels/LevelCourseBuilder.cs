using System.Collections.Generic;
using UnityEngine;
using PawPath.Hub;
using PawPath.Gameplay;

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
        const float SectionOffset = 13.0f;
        const int IntroLevelCount = 5;
        const float CourseLeft = -9.35f;
        const float CourseRight = 49f;
        static readonly Color RoadColor = new Color(0.72f, 0.54f, 0.38f, 1f);
        static readonly Color ObstacleColor = new Color(0.61f, 0.42f, 0.29f, 1f);
        static readonly Color CityRoadFillColor = new Color(0.70f, 0.66f, 0.60f, 1f);

        Transform generatedRoot;
        Sprite gapSprite;
        Sprite streetGapLeftSprite;
        Sprite streetGapRightSprite;
        Sprite moundSprite;
        Sprite alternateMoundSprite;
        Sprite roadSprite;
        Sprite roadUnderfillSprite;
        Sprite[] birdFrames;
        Sprite[] dogRunFrames;
        Sprite climbTreeSprite;
        Sprite[] climbingCatFrames;
        bool cyberTheme;
        Sprite croppedCyberRoadSprite;
        Sprite[] cyberObstacleSprites;
        readonly Sprite[] croppedCyberObstacleSprites = new Sprite[4];
        int cyberObstacleCount;
        // Fizik, kaplama ve çukur uçları aynı dünya koordinatlarını kullanır.
        readonly List<RoadSpan> roads = new List<RoadSpan>();
        readonly List<GapSpan> gaps = new List<GapSpan>();
        readonly List<Vector2> moundVisualRanges = new List<Vector2>();
        readonly List<UnityEngine.Object> generatedRuntimeAssets = new List<UnityEngine.Object>();
        int currentLevelNumber;
        System.Random layoutRandom;

        internal readonly struct RoadSpan
        {
            public readonly float Left;
            public readonly float Right;
            public readonly float Top;
            public readonly float Bottom;
            public float Width => Right - Left;
            public float CenterX => (Left + Right) * 0.5f;
            public float CenterY => (Top + Bottom) * 0.5f;

            public RoadSpan(float left, float right, float top, float bottom)
            {
                Left = left;
                Right = right;
                Top = top;
                Bottom = bottom;
            }

            public bool Contains(float x) => x >= Left && x <= Right;
            public float MaxCenteredWidth(float x, float inset) =>
                Mathf.Max(0f, 2f * Mathf.Min(x - Left - inset, Right - x - inset));
        }

        internal readonly struct GapSpan
        {
            public readonly float Left;
            public readonly float Right;
            public float Width => Right - Left;
            public float CenterX => (Left + Right) * 0.5f;

            public GapSpan(float left, float right)
            {
                Left = left;
                Right = right;
            }
        }

        public float CatSpawnY => RoadCenterY + RoadHeight * 0.5f + 0.40f * GameplayScale;
        public float GoalY => CatSpawnY + 0.15f;
        public float GoalX => CourseRight - 4.10f;

        public void BindVisuals(Sprite gap, Sprite mound, Sprite road, Sprite alternateMound = null,
            Sprite underfill = null, Sprite[] birds = null, Sprite[] dogs = null,
            Sprite tree = null, Sprite[] climbingFrames = null,
            Sprite streetGapLeft = null, Sprite streetGapRight = null, bool useCyberTheme = false,
            Sprite[] cyberObstacles = null)
        {
            gapSprite = CreateCityTrashContainer(gap);
            moundSprite = mound;
            roadSprite = CreateCitySidewalkSurface(road);
            alternateMoundSprite = alternateMound;
            roadUnderfillSprite = underfill;
            birdFrames = birds;
            dogRunFrames = dogs;
            climbTreeSprite = tree;
            climbingCatFrames = climbingFrames;
            streetGapLeftSprite = streetGapLeft;
            streetGapRightSprite = streetGapRight;
            cyberTheme = useCyberTheme;
            cyberObstacleSprites = cyberObstacles;
            croppedCyberRoadSprite = null;
        }

        static Sprite CreateCitySidewalkSurface(Sprite source)
        {
            // Cadde için artık yalnızca temiz taş kaldırım dokusu kullanılıyor;
            // eski borulu görselde gereken kırpma yeni görseli bozuyordu.
            return source;
        }

        static Sprite CreateCityTrashContainer(Sprite source)
        {
            // Yeni PNG zaten yalnızca şeffaf arka planlı konteynırı içeriyor.
            return source;
        }

        public void Build(int levelNumber)
        {
            ClearGenerated();
            currentLevelNumber = levelNumber;
            layoutRandom = new System.Random(unchecked(levelNumber * 48611 + 919));
            roads.Clear();
            gaps.Clear();
            moundVisualRanges.Clear();
            cyberObstacleCount = 0;
            for (int i = 0; i < croppedCyberObstacleSprites.Length; i++)
                croppedCyberObstacleSprites[i] = null;
            if (cyberTheme && birdFrames != null && birdFrames.Length == 1 && birdFrames[0] != null)
            {
                Texture2D texture = birdFrames[0].texture;
                if (texture.width == 536 && texture.height == 427)
                {
                    var croppedBird = Sprite.Create(texture, new Rect(90f, 104f, 305f, 271f),
                        new Vector2(0.5f, 0.5f), birdFrames[0].pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    croppedBird.name = "RoboticBirdVisible";
                    generatedRuntimeAssets.Add(croppedBird);
                    birdFrames = new[] { croppedBird };
                }
            }
            generatedRoot = new GameObject("GeneratedCourse").transform;
            generatedRoot.SetParent(transform, false);

            // Cadde asfaltı bütün bölüm boyunca tek görseldir. Parça parça çizmek
            // şeritlerin her kaldırım ve konteynır altında yeniden başlamasına yol açıyordu.
            if (IsContinuousCityUnderfill())
            {
                const float cityRoadBottom = -5.35f;
                float cityRoadTop = RoadCenterY + RoadHeight * 0.5f;
                float cityRoadHeight = cityRoadTop - cityRoadBottom;
                CreateContinuousCityUnderfill((CourseLeft + CourseRight) * 0.5f,
                    cityRoadBottom + cityRoadHeight * 0.5f,
                    CourseRight - CourseLeft + 4f, cityRoadHeight);
            }

            if (levelNumber <= IntroLevelCount)
            {
                int pattern = Mathf.Abs(levelNumber - 1) % 5;
                Road(-8.25f, 2.2f);
                BuildPattern(pattern, 0f);
                BuildPattern((pattern + 2) % 5, SectionOffset);
                BuildPattern((pattern + 4) % 5, SectionOffset * 2f);
                BuildPattern((pattern + 1) % 5, SectionOffset * 3f);
                Road(21.55f + SectionOffset * 2f, 2.9f);
            }
            else
            {
                BuildVariedCourse(levelNumber);
            }
            if (cyberTheme && cyberObstacleCount == 0)
                TryPlaceIntroCyberObstacle();
            BuildModeChallenge(levelNumber);
            BuildDogAndTreeChallenge(levelNumber);
        }

        void BuildVariedCourse(int levelNumber)
        {
            // On güvenli bölge, çukurların doğma/hedef alanına ya da birbirine
            // yaklaşmasını önler. Bölüm numarası hem bölge seçimini hem ölçüleri
            // sabitler; yeniden denemede yol değişmez.
            float[] gapLanes = { -2.7f, 1.5f, 5.8f, 10.2f, 14.8f,
                19.4f, 24.2f, 29.4f, 35.2f, 41.1f };
            var selected = new List<int> { layoutRandom.Next(0, 3), layoutRandom.Next(6, 10) };
            int gapCount = 4 + layoutRandom.Next(0, 3);
            while (selected.Count < gapCount)
            {
                int lane = layoutRandom.Next(gapLanes.Length);
                if (!selected.Contains(lane))
                    selected.Add(lane);
            }
            selected.Sort();

            var gaps = new List<Vector2>(selected.Count);
            float maxGapWidth = Mathf.Min(1.52f, 1.22f + (levelNumber - IntroLevelCount) * 0.012f);
            foreach (int lane in selected)
            {
                float center = gapLanes[lane] + NextRange(layoutRandom, -0.62f, 0.62f);
                float width = NextRange(layoutRandom, 0.88f, maxGapWidth);
                gaps.Add(new Vector2(center, width));
            }

            float roadStart = CourseLeft;
            foreach (Vector2 gap in gaps)
            {
                float gapLeft = gap.x - gap.y * 0.5f;
                Road((roadStart + gapLeft) * 0.5f, gapLeft - roadStart);
                roadStart = gap.x + gap.y * 0.5f;
            }
            Road((roadStart + CourseRight) * 0.5f, CourseRight - roadStart);

            foreach (Vector2 gap in gaps)
                Gap(gap.x, gap.y);

            // Tümsek yalnız geniş platformlarda oluşur; giriş, bitiş ve çukur
            // kenarları boş kalır. İki tümsek arasında da yürüme payı bulunur.
            int moundCount = 2 + layoutRandom.Next(0, 3);
            var moundPositions = new List<float>();
            for (int i = 0; i < moundCount; i++)
            {
                var candidates = new List<Vector2>();
                foreach (RoadSpan range in roads)
                {
                    float min = Mathf.Max(range.Left + 2.2f, -4.2f);
                    float max = Mathf.Min(range.Right - 2.2f, GoalX - 2.2f);
                    if (max > min)
                        candidates.Add(new Vector2(min, max));
                }
                if (candidates.Count == 0)
                    break;
                bool placed = false;
                for (int attempt = 0; attempt < 12 && !placed; attempt++)
                {
                    Vector2 range = candidates[layoutRandom.Next(candidates.Count)];
                    float x = NextRange(layoutRandom, range.x, range.y);
                    if (moundPositions.Exists(previous => Mathf.Abs(previous - x) < 4.5f))
                        continue;
                    Obstacle(x, NextRange(layoutRandom, 0.92f, 1.10f),
                        NextRange(layoutRandom, 0.90f, 1.07f));
                    moundPositions.Add(x);
                    placed = true;
                }
            }
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
            var span = new RoadSpan(centerX - width * 0.5f, centerX + width * 0.5f,
                RoadCenterY + RoadHeight * 0.5f, RoadCenterY - RoadHeight * 0.5f);
            roads.Add(span);
            var go = new GameObject("Road");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(span.CenterX, span.CenterY);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.size = new Vector2(span.Width, span.Top - span.Bottom);

            if (roadSprite == null)
            {
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = FallbackSprite.WhiteSquare();
                renderer.color = RoadColor;
                renderer.sortingOrder = 1;
                go.transform.localScale = new Vector3(span.Width, span.Top - span.Bottom, 1f);
                collider.size = Vector2.one;
                return;
            }

            int tileCount = Mathf.Max(1, Mathf.CeilToInt(span.Width / 3.1f));
            float tileWidth = span.Width / tileCount;
            if (cyberTheme && roadSprite.name.Contains("cyber_ground_tile"))
            {
                CreateCyberRoad(span);
                return;
            }
            if (roadSprite.name.Contains("new_road_terracot") ||
                roadSprite.name.Contains("forest_ground_underfill"))
            {
                CreateFullDepthRoad(span);
                return;
            }
            CreateRoadUnderfill(span.CenterX, span.Width, span.Top);
            for (int i = 0; i < tileCount; i++)
            {
                float x = span.Left + tileWidth * (i + 0.5f);
                // Bindirme yalnız iç birleşimlerde kalır; ilk ve son karo çukura taşmaz.
                float leftBleed = i > 0 ? 0.09f : 0f;
                float rightBleed = i < tileCount - 1 ? 0.09f : 0f;
                float visualX = x + (rightBleed - leftBleed) * 0.5f;
                float visualWidth = tileWidth + leftBleed + rightBleed;
                // Üst çim/yürüme yüzeyi eski doğru konumunda kalır. Ekranın altına
                // uzanan kalınlık ayrı bir dolgu görseliyle sağlanır.
                bool citySurface = roadSprite != null && roadSprite.name.Contains("city_sidewalk");
                float visualHeight = citySurface ? 0.82f : 3.35f * GameplayScale;
                float visualCenterY = citySurface
                    ? span.Top - visualHeight * 0.5f + 0.03f
                    : span.CenterY - 0.02f;
                Decoration("RoadVisual", roadSprite, new Vector2(visualX, visualCenterY),
                    visualWidth, 1, visualHeight);
            }
        }

        void CreateFullDepthRoad(RoadSpan span)
        {
            const float bottomY = -5.35f;
            float height = span.Top - bottomY;
            var go = new GameObject("RoadVisualFullDepth");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(span.CenterX, bottomY + height * 0.5f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = roadSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 1;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            // Tam derinlikli görsel çukurun içine taşmamalı.
            renderer.size = new Vector2(span.Width, height);
            ApplyRoundedTopCornerMask(go.transform, renderer, span.CenterX, span.Width, height);
        }

        void CreateCyberRoad(RoadSpan span)
        {
            // Gelen PNG'nin üstünde ve altında geniş şeffaf pay var. Yalnızca
            // 669x137 görünür şeridi kırpınca yol collider'ına tam oturur.
            if (croppedCyberRoadSprite == null)
            {
                Texture2D texture = roadSprite.texture;
                Rect rect = texture.width == 669 && texture.height == 373
                    ? new Rect(0f, 119f, 669f, 137f)
                    : new Rect(0f, 0f, texture.width, texture.height);
                croppedCyberRoadSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f),
                    roadSprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                croppedCyberRoadSprite.name = "CyberGroundVisibleStrip";
                generatedRuntimeAssets.Add(croppedCyberRoadSprite);
            }

            CreateRoadUnderfill(span.CenterX, span.Width, span.Top);
            const float visualHeight = 0.82f;
            var go = new GameObject("CyberGroundVisual");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(span.CenterX, span.Top - visualHeight * 0.5f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = croppedCyberRoadSprite;
            renderer.sortingOrder = 1;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(span.Width, visualHeight);
        }

        void ApplyRoundedTopCornerMask(Transform roadTransform, SpriteRenderer roadRenderer,
            float centerX, float width, float height)
        {
            const int textureSize = 512;
            // Bölüm ve yol konumu aynı kaldıkça şekil sabittir; her parçada ve
            // iki köşede farklı, küçük bir aşınma miktarı kullanılır.
            int seed = unchecked(currentLevelNumber * 73856093 ^
                Mathf.RoundToInt(centerX * 100f) * 19349663 ^
                Mathf.RoundToInt(width * 100f) * 83492791);
            var random = new System.Random(seed);
            float leftRadius = Mathf.Lerp(0.05f, 0.23f, (float)random.NextDouble());
            float rightRadius = Mathf.Lerp(0.05f, 0.23f, (float)random.NextDouble());
            float leftPhase = (float)random.NextDouble() * Mathf.PI * 2f;
            float rightPhase = (float)random.NextDouble() * Mathf.PI * 2f;
            float leftRadiusX = Mathf.Max(2f, leftRadius / width * textureSize);
            float leftRadiusY = Mathf.Max(2f, leftRadius / height * textureSize);
            float rightRadiusX = Mathf.Max(2f, rightRadius / width * textureSize);
            float rightRadiusY = Mathf.Max(2f, rightRadius / height * textureSize);
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "RoundedRoadMaskTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    bool visible = true;
                    if (y >= textureSize - leftRadiusY && x < leftRadiusX)
                    {
                        float dx = (x - leftRadiusX) / leftRadiusX;
                        float dy = (y - (textureSize - leftRadiusY)) / leftRadiusY;
                        float irregularity = 0.035f * Mathf.Sin(y * 0.09f + leftPhase);
                        visible = dx * dx + dy * dy <= 1f + irregularity;
                    }
                    else if (y >= textureSize - rightRadiusY && x >= textureSize - rightRadiusX)
                    {
                        float dx = (x - (textureSize - rightRadiusX)) / rightRadiusX;
                        float dy = (y - (textureSize - rightRadiusY)) / rightRadiusY;
                        float irregularity = 0.035f * Mathf.Sin(y * 0.09f + rightPhase);
                        visible = dx * dx + dy * dy <= 1f + irregularity;
                    }
                    pixels[y * textureSize + x] = visible
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f), textureSize, 0, SpriteMeshType.FullRect);
            sprite.name = "RoundedRoadMaskSprite";
            generatedRuntimeAssets.Add(sprite);
            generatedRuntimeAssets.Add(texture);

            var maskObject = new GameObject("RoundedRoadCornerMask");
            maskObject.transform.SetParent(roadTransform, false);
            maskObject.transform.localPosition = Vector3.zero;
            maskObject.transform.localScale = new Vector3(width, height, 1f);
            var mask = maskObject.AddComponent<SpriteMask>();
            mask.sprite = sprite;
            mask.alphaCutoff = 0.45f;
            mask.isCustomRangeActive = true;
            mask.frontSortingOrder = roadRenderer.sortingOrder + 1;
            mask.backSortingOrder = roadRenderer.sortingOrder - 1;
            roadRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        void Obstacle(float centerX, float width, float height)
        {
            if (cyberTheme)
            {
                CreateCyberObstacle(centerX);
                return;
            }
            bool streetMoundPair = moundSprite != null && alternateMoundSprite != null &&
                moundSprite.name.Contains("street_mound") &&
                alternateMoundSprite.name.Contains("street_mound_high");
            bool cityVehiclePair = moundSprite != null && alternateMoundSprite != null &&
                moundSprite.name.Contains("city_") && alternateMoundSprite.name.Contains("city_");
            bool useAlternate = alternateMoundSprite != null && layoutRandom.NextDouble() >= 0.5;
            Sprite obstacleSprite = useAlternate ? alternateMoundSprite : moundSprite;
            bool cityVehicle = cityVehiclePair ||
                (obstacleSprite != null && obstacleSprite.name.Contains("city_"));
            // Cadde temasında boyut varyasyonu yoktur: sadece otomobil/minibüs
            // görseli değişir. Büyük-küçük varyasyonu orman tümseklerine aittir.
            bool isTallVariant = streetMoundPair ? useAlternate :
                !cityVehicle && layoutRandom.NextDouble() >= 0.5;
            // Uzun varyant eskiden 2 kat yüksek ve normal varyantla aynı genişlikteydi.
            // Bu, yamacı gereğinden dikleştiriyor ve çift zıplamayla bile geçilemeyen
            // bir collider tepesi oluşturuyordu. Biraz alçaltıp tabanını genişletiyoruz.
            // Yeni cadde tümsekleri zaten iki ayrı, doğru oranlı PNG'dir. Görseli
            // ikinci kez uzatmak daralmış ve parçalı bir görünüm oluşturuyordu.
            float heightVariant = streetMoundPair ? 1f : isTallVariant ? 1.55f : 1f;
            float widthVariant = streetMoundPair ? 1f : isTallVariant ? 1.15f : 1f;
            float obstacleWidth = width * 1.62f * widthVariant * GameplayScale;
            float obstacleHeight = height * 1.62f * heightVariant * GameplayScale;
            float visualWidth = obstacleWidth * 1.95f;
            // Tümsek görseli bulunduğu fiziksel yol parçasının dışına çıkarsa uç
            // kısmı boşluğun üzerinde asılı görünür. Her iki tarafta küçük bir pay
            // bırakarak görseli gerçek zemin sınırlarına sığdır.
            visualWidth = ClampWidthToContainingRoad(centerX, visualWidth, 0.10f);
            if (visualWidth <= 0f)
                return;
            moundVisualRanges.Add(new Vector2(centerX - visualWidth * 0.5f,
                centerX + visualWidth * 0.5f));
            obstacleWidth = Mathf.Min(obstacleWidth, visualWidth * 0.52f);
            float visualOverlap = (cityVehicle ? 0.27f : obstacleSprite != null && obstacleSprite.name.Contains("forest") ? 0.52f : 0.95f) *
                heightVariant * GameplayScale;
            float colliderHeight = obstacleHeight;
            if (streetMoundPair && obstacleSprite != null && obstacleSprite.bounds.size.x > 0f)
            {
                // Alfa sınırları: küçük 689x145 (altta 99 px boşluk), yüksek
                // 701x318 (altta 17 px boşluk). Görünen toprağın tabanı tam yol
                // yüzeyine gelir; collider yalnızca görünen tümsek yüksekliğidir.
                float spriteScale = visualWidth / obstacleSprite.bounds.size.x;
                float visibleSpriteHeight = (isTallVariant ? 3.18f : 1.45f) * spriteScale;
                visualOverlap = (isTallVariant ? 0.17f : 0.99f) * spriteScale;
                colliderHeight = visibleSpriteHeight;
            }
            else if (obstacleSprite != null && obstacleSprite.bounds.size.x > 0f)
            {
                // Görsel genişlikten ölçeklendiği için, hedef yükseklik oranını korumak
                // adına genişlik artışını dikey ölçekten çıkarıyoruz. Görselin yolun
                // içine gömülen kısmı yürünebilir tepe değildir; collider hesabından
                // çıkarılmazsa kedi görünen tümseğin üstünde havada kalır.
                float spriteHeightScale = heightVariant / widthVariant;
                float visibleRatioHeight = visualWidth * obstacleSprite.bounds.size.y / obstacleSprite.bounds.size.x * spriteHeightScale;
                float visibleHeightAboveRoad = Mathf.Max(0.35f, visibleRatioHeight - visualOverlap);
                colliderHeight = cityVehicle
                    ? visibleHeightAboveRoad * 0.96f
                    : Mathf.Min(obstacleHeight, visibleHeightAboveRoad * 0.90f);
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
            collider.points = cityVehicle
                ? new[]
                {
                    new Vector2(-halfW, -halfH),
                    new Vector2(-halfW * 0.92f, -halfH * 0.20f),
                    new Vector2(-halfW * 0.62f, halfH * 0.72f),
                    new Vector2(-halfW * 0.30f, halfH),
                    new Vector2(halfW * 0.30f, halfH),
                    new Vector2(halfW * 0.72f, halfH * 0.58f),
                    new Vector2(halfW, -halfH)
                }
                : streetMoundPair
                ? new[]
                {
                    new Vector2(-halfW, -halfH),
                    new Vector2(-halfW * 0.86f, -halfH * 0.68f),
                    new Vector2(-halfW * 0.55f, halfH * 0.05f),
                    new Vector2(-halfW * 0.22f, halfH * 0.72f),
                    new Vector2(0f, halfH * 0.95f),
                    new Vector2(halfW * 0.22f, halfH * 0.72f),
                    new Vector2(halfW * 0.55f, halfH * 0.05f),
                    new Vector2(halfW * 0.86f, -halfH * 0.68f),
                    new Vector2(halfW, -halfH)
                }
                : new[]
                {
                    new Vector2(-halfW, -halfH),
                    new Vector2(-halfW * 0.86f, -halfH * 0.62f),
                    // PNG'nin üstündeki şeffaf boşluk fizik yüzeyi değildir.
                    // Eğim ve tepeyi görünen taşların üst çizgisine indir.
                    new Vector2(-halfW * 0.52f, -halfH * 0.20f),
                    new Vector2(-halfW * 0.18f, -halfH * 0.05f),
                    new Vector2(halfW * 0.18f, -halfH * 0.05f),
                    new Vector2(halfW * 0.52f, -halfH * 0.20f),
                    new Vector2(halfW * 0.86f, -halfH * 0.62f),
                    new Vector2(halfW, -halfH)
                };

            if (obstacleSprite != null)
            {
                // Üretilen orman tümseği PNG'sinin altında geniş şeffaf tuval payı var.
                // Görünen yosun/toprak tabanını yolun içine oturtmak için yalnızca bu
                // görsele daha fazla bindirme uygula.
                bool forestMound = obstacleSprite.name.ToLowerInvariant().Contains("forest");
                float forestVisualLift = forestMound ? 0.14f : 0f;
                DecorationBottomAligned("HumpVisual", obstacleSprite, centerX,
                    roadTop - visualOverlap + forestVisualLift,
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

        void TryPlaceIntroCyberObstacle()
        {
            // İlk iki kalıpta tümsek çağrısı yok; Neo modda en az bir obje görünsün.
            float bestWidth = 0f;
            float bestX = 0f;
            foreach (RoadSpan road in roads)
            {
                float left = Mathf.Max(road.Left + 1.3f, 0f);
                float right = Mathf.Min(road.Right - 1.3f, 14f);
                if (right - left <= bestWidth)
                    continue;
                bestWidth = right - left;
                bestX = (left + right) * 0.5f;
            }
            if (bestWidth > 0.8f)
                CreateCyberObstacle(bestX);
        }

        void CreateCyberObstacle(float centerX)
        {
            if (cyberObstacleSprites == null || cyberObstacleSprites.Length == 0)
                return;
            int index = (currentLevelNumber - 1 + cyberObstacleCount) % cyberObstacleSprites.Length;
            if (index >= croppedCyberObstacleSprites.Length || cyberObstacleSprites[index] == null)
                return;

            Sprite sprite = GetCroppedCyberObstacle(index);
            // Neo objeleri yol dekorundan ayrışsın; yüksek modeller hâlâ
            // çift zıplamayla aşılabilecek aralıkta tutulur.
            float targetHeight = index == 2 ? 1.34f : index == 1 ? 1.78f : 1.70f;
            float scale = targetHeight / Mathf.Max(0.01f, sprite.bounds.size.y);
            float visibleWidth = sprite.bounds.size.x * scale;
            visibleWidth = ClampWidthToContainingRoad(centerX, visibleWidth, 0.24f);
            if (visibleWidth < 0.45f)
                return;
            scale = visibleWidth / sprite.bounds.size.x;
            float visibleHeight = sprite.bounds.size.y * scale;
            float roadTop = RoadCenterY + RoadHeight * 0.5f;

            var go = new GameObject($"CyberObstacle_{index + 1}");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(centerX, roadTop + visibleHeight * 0.5f);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(visibleWidth * 0.90f, visibleHeight * 0.90f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(scale, scale, 1f);
            visual.transform.localPosition = new Vector3(0f,
                -sprite.bounds.min.y * scale - visibleHeight * 0.5f, 0f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 3;
            CreateCyberObstacleGlow(go.transform, visibleWidth, visibleHeight, index);
            moundVisualRanges.Add(new Vector2(centerX - visibleWidth * 0.5f,
                centerX + visibleWidth * 0.5f));
            cyberObstacleCount++;
        }

        void CreateCyberObstacleGlow(Transform parent, float width, float height, int index)
        {
            float sideX = width * 0.5f + 0.045f;
            float topY = height * 0.5f + 0.035f;
            float bottomY = -height * 0.5f + 0.045f;
            float frameWidth = width + 0.13f;
            float frameHeight = topY - bottomY;
            var positions = new[]
            {
                new Vector2(-sideX, (topY + bottomY) * 0.5f),
                new Vector2(sideX, (topY + bottomY) * 0.5f),
                new Vector2(0f, topY),
                new Vector2(0f, bottomY)
            };
            var inner = new SpriteRenderer[4];
            var outer = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                bool vertical = i < 2;
                inner[i] = CreateCyberGlowLine(parent, $"NeonEdge_{i}", positions[i],
                    vertical ? new Vector2(0.035f, frameHeight) : new Vector2(frameWidth, 0.035f), 4);
                outer[i] = CreateCyberGlowLine(parent, $"NeonHalo_{i}", positions[i],
                    vertical ? new Vector2(0.16f, frameHeight + 0.14f)
                        : new Vector2(frameWidth + 0.14f, 0.16f), 2);
            }
            parent.gameObject.AddComponent<CyberObstacleGlow>().Configure(inner, outer, index * 0.85f);
        }

        static SpriteRenderer CreateCyberGlowLine(Transform parent, string name, Vector2 position,
            Vector2 size, int sortingOrder)
        {
            var line = new GameObject(name);
            line.transform.SetParent(parent, false);
            line.transform.localPosition = position;
            line.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = line.AddComponent<SpriteRenderer>();
            renderer.sprite = FallbackSprite.WhiteSquare();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        Sprite GetCroppedCyberObstacle(int index)
        {
            if (croppedCyberObstacleSprites[index] != null)
                return croppedCyberObstacleSprites[index];
            Sprite source = cyberObstacleSprites[index];
            Texture2D texture = source.texture;
            Rect rect;
            switch (index)
            {
                case 0: rect = new Rect(13f, 11f, 143f, 217f); break;
                case 1: rect = new Rect(31f, 5f, 137f, 235f); break;
                case 2: rect = new Rect(22f, 11f, 260f, 216f); break;
                default: rect = new Rect(9f, 13f, 139f, 227f); break;
            }
            int[] expectedWidths = { 170, 205, 291, 170 };
            int[] expectedHeights = { 244, 251, 247, 248 };
            if (texture.width != expectedWidths[index] || texture.height != expectedHeights[index])
                return source;

            Sprite cropped = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            cropped.name = $"CyberObstacleVisible_{index + 1}";
            generatedRuntimeAssets.Add(cropped);
            croppedCyberObstacleSprites[index] = cropped;
            return cropped;
        }

        float ClampWidthToContainingRoad(float centerX, float desiredWidth, float edgeInset)
        {
            foreach (RoadSpan road in roads)
            {
                if (!road.Contains(centerX))
                    continue;
                return Mathf.Min(desiredWidth, road.MaxCenteredWidth(centerX, edgeInset));
            }
            return 0f;
        }

        void Gap(float centerX, float width)
        {
            GapSpan gap = ResolvePhysicalGapBounds(centerX, width);
            gaps.Add(gap);
            if (streetGapLeftSprite != null && streetGapRightSprite != null)
            {
                CreateStreetGapEdges(gap);
                return;
            }
            if (gapSprite != null)
            {
                // Üst kırık kenarlar yol hizasında kalır; yalnızca aşağıdaki
                // toprak kesiti büyütülerek çukur daha derin görünür. Merkez
                // konumu yol yüzeyinden ölçeklendiği için büyürken bağlantı kopmaz.
                float roadTop = RoadCenterY + RoadHeight * 0.5f;
                bool cityContainer = gapSprite.name.Contains("city_trash");
                if (cityContainer)
                {
                    // Konteynır kaldırımın üstünde değil, açıklığın içinde durur.
                    // Buraya collider eklenmediği için alan aşılacak çukur olarak kalır.
                    const float cityGapBottom = -5.35f;
                    float cityGapFillTop = roadTop - 1.10f;
                    float cityGapFillHeight = cityGapFillTop - cityGapBottom;
                    if (roadUnderfillSprite == null)
                    {
                        Decoration("CityGapUnderfill", FallbackSprite.WhiteSquare(),
                            new Vector2(centerX, cityGapBottom + cityGapFillHeight * 0.5f),
                            width * GameplayScale, 0, cityGapFillHeight);
                        generatedRoot.GetChild(generatedRoot.childCount - 1)
                            .GetComponent<SpriteRenderer>().color = CityRoadFillColor;
                    }
                    DecorationBottomAligned("TrashContainer", gapSprite, gap.CenterX,
                        roadTop - 1.66f, gap.Width * 2.10f, 2);
                    return;
                }
                // Eski çukur görselinin alt parçaları yeni tam derinlikli zeminin
                // önünde asılı sütun gibi görünmemeli. Geçerli alfa kenar assetleri
                // sağlanana kadar kaplamayı yolun arkasında tutuyoruz.
                DecorationTopAligned("GapVisual", gapSprite, gap.CenterX,
                    roadTop, (gap.Width + 1.95f) * GameplayScale, -2, 5.8f);
            }
        }

        void CreateStreetGapEdges(GapSpan gap)
        {
            float roadTop = RoadCenterY + RoadHeight * 0.5f;
            if (streetGapLeftSprite == streetGapRightSprite &&
                streetGapLeftSprite.name.Contains("street_gap_left (2)"))
            {
                CreateMirroredNaturalGapEdges(gap, roadTop);
                return;
            }
            const float visibleHeight = 2.60f;
            // Alfa ölçüleri: left 490x241, right 567x239. Görünür iç uçlar
            // doğrudan fiziksel çukurun iki sınırına sabitlenir.
            float leftScale = visibleHeight / 2.41f;
            float rightScale = visibleHeight / 2.39f;
            var left = new GameObject("StreetGapLeft");
            left.transform.SetParent(generatedRoot, false);
            left.transform.localScale = new Vector3(leftScale, leftScale, 1f);
            left.transform.position = new Vector2(gap.Left - 1.36f * leftScale,
                roadTop - 1.105f * leftScale);
            var leftRenderer = left.AddComponent<SpriteRenderer>();
            leftRenderer.sprite = streetGapLeftSprite;
            // Uzun yatay bölüm ana yolun arkasında kalır; yalnız uçurum ağzı
            // gerçek boşlukta görünür ve üst üste doku karmaşası oluşturmaz.
            leftRenderer.sortingOrder = 0;

            var right = new GameObject("StreetGapRight");
            right.transform.SetParent(generatedRoot, false);
            right.transform.localScale = new Vector3(rightScale, rightScale, 1f);
            right.transform.position = new Vector2(gap.Right + 2.135f * rightScale,
                roadTop - 1.085f * rightScale);
            var rightRenderer = right.AddComponent<SpriteRenderer>();
            rightRenderer.sprite = streetGapRightSprite;
            rightRenderer.sortingOrder = 0;
        }

        void CreateMirroredNaturalGapEdges(GapSpan gap, float roadTop)
        {
            // 612x408 tuval, görünür alfa 7,85..535,407. Görünen yüksekliği
            // yaklaşık 3 dünya biriminde tutup uçtaki kökü yol yüzeyine hizala.
            const float scale = 3f / 3.23f;
            const float innerEdgeFromPivot = 2.29f;
            const float topFromPivot = 1.19f;

            CreateNaturalGapEdge("StreetGapLeftNatural", gap.Left - innerEdgeFromPivot * scale,
                roadTop - topFromPivot * scale, scale, false);
            CreateNaturalGapEdge("StreetGapRightNatural", gap.Right + innerEdgeFromPivot * scale,
                roadTop - topFromPivot * scale, scale, true);
        }

        void CreateNaturalGapEdge(string objectName, float x, float y, float scale, bool flipX)
        {
            var edge = new GameObject(objectName);
            edge.transform.SetParent(generatedRoot, false);
            edge.transform.position = new Vector2(x, y);
            edge.transform.localScale = new Vector3(scale, scale, 1f);
            var renderer = edge.AddComponent<SpriteRenderer>();
            renderer.sprite = streetGapLeftSprite;
            renderer.flipX = flipX;
            renderer.sortingOrder = 2;
        }

        GapSpan ResolvePhysicalGapBounds(float centerX, float fallbackWidth)
        {
            float leftEdge = centerX - fallbackWidth * 0.5f;
            float rightEdge = centerX + fallbackWidth * 0.5f;
            float bestLeftDistance = float.MaxValue;
            float bestRightDistance = float.MaxValue;
            foreach (RoadSpan road in roads)
            {
                if (road.Right <= centerX)
                {
                    float distance = centerX - road.Right;
                    if (distance < bestLeftDistance)
                    {
                        bestLeftDistance = distance;
                        leftEdge = road.Right;
                    }
                }
                if (road.Left >= centerX)
                {
                    float distance = road.Left - centerX;
                    if (distance < bestRightDistance)
                    {
                        bestRightDistance = distance;
                        rightEdge = road.Left;
                    }
                }
            }
            return new GapSpan(leftEdge, rightEdge);
        }

        void BuildModeChallenge(int levelNumber)
        {
            if (birdFrames == null || birdFrames.Length == 0 || birdFrames[0] == null)
                return;

            // Hareketli engeller başlangıç ekranında görünmez. Her bölümde kuşların
            // sayısı, ilk konumu, uçuş yüksekliği ve hızı deterministik olarak değişir.
            // Böylece yeniden denemede düzen korunur fakat her bölüm aynı hissettirmez.
            var random = new System.Random(levelNumber * 7919 + 173);
            int birdCount = 4 + (levelNumber % 3 == 0 ? 1 : 0);
            float roadTop = RoadCenterY + RoadHeight * 0.5f;
            for (int i = 0; i < birdCount; i++)
            {
                // Kuş bir kez sağdan girip uzun bir mesafe uçar; kısa bir bölgede
                // ışınlanıp tekrar tekrar doğmaz.
                float spawnX = 9.5f + i * ((GoalX - 13f) / (birdCount - 1)) +
                    NextRange(random, 0.4f, 1.8f);
                float height = roadTop + NextRange(random, 1.25f, 2.85f);
                float speed = NextRange(random, 0.72f, 1.48f);
                float width = NextRange(random, 1.00f, 1.38f);
                CreateFlyingBird(i + 1, new Vector2(spawnX, height), -9.5f,
                    speed, width, NextRange(random, 0f, 6.28f));
            }
        }

        static float NextRange(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        void CreateFlyingBird(int index, Vector2 position, float leftBound,
            float speed, float targetWidth, float phase)
        {
            Sprite firstFrame = birdFrames[0];
            var go = new GameObject($"FlyingBird_{index}");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = firstFrame;
            renderer.sortingOrder = 8;
            float scale = firstFrame.bounds.size.x > 0f ? targetWidth / firstFrame.bounds.size.x : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(firstFrame.bounds.size.x * 0.62f,
                firstFrame.bounds.size.y * 0.58f);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            go.AddComponent<FlyingBirdCollectable>().Configure(renderer, birdFrames, speed,
                leftBound, phase, targetWidth);

            var rewardGo = new GameObject("RewardLabel");
            rewardGo.transform.SetParent(go.transform, false);
            rewardGo.transform.localPosition = new Vector3(0f, firstFrame.bounds.extents.y + 0.22f, 0f);
            rewardGo.transform.localScale = new Vector3(1f / scale, 1f / scale, 1f);
            var reward = rewardGo.AddComponent<TextMesh>();
            reward.text = "♥ +3";
            reward.anchor = TextAnchor.MiddleCenter;
            reward.alignment = TextAlignment.Center;
            reward.fontSize = 44;
            reward.characterSize = 0.035f;
            reward.color = new Color(1f, 0.82f, 0.16f, 1f);
            rewardGo.GetComponent<MeshRenderer>().sortingOrder = 10;
        }

        void BuildDogAndTreeChallenge(int levelNumber)
        {
            bool hasDog = dogRunFrames != null && dogRunFrames.Length > 0 && dogRunFrames[0] != null;
            bool hasTree = climbTreeSprite != null;
            if (!hasDog && !hasTree)
                return;

            float roadTop = RoadCenterY + RoadHeight * 0.5f;
            // Aynı bölüm tekrarlandığında aynı sonuç çıkar; farklı bölümlerde
            // ağacın varlığı ve yeri değişir.
            var treeRandom = new System.Random(levelNumber * 104729 + 811);
            // Ağaç çoğu bölümde görünür; her dört bölümden birinde bulunmaz.
            // Böylece mekanik kaybolmuş gibi hissedilmez ama her oyunda da zorunlu değildir.
            bool spawnTree = hasTree && levelNumber % 4 != 0;
            if (spawnTree && TryChooseTreeX(treeRandom, out float treeX))
                CreateClimbableTree(treeX, roadTop);
            if (hasDog)
            {
                // Zorluk seçimi yalnızca bölüm kurulurken okunur; yeniden denemede
                // aynı bölgenin köpek yerleri deterministik kalır.
                var dogRandom = new System.Random(levelNumber * 32531 + 211);
                bool first = TryChooseDogSpawnX(dogRandom, 15f, 25f, out float firstX);
                bool second = TryChooseDogSpawnX(dogRandom, 33f, 42f, out float secondX);
                DifficultyLevel difficulty = GameDifficulty.Selected;
                float speed = difficulty == DifficultyLevel.Easy
                    ? 1.05f + levelNumber % 4 * 0.08f
                    : difficulty == DifficultyLevel.Hard
                        ? 2.25f + levelNumber % 4 * 0.18f
                        : 1.8f + levelNumber % 4 * 0.18f;
                if (first || (difficulty == DifficultyLevel.Easy && second))
                    CreateDog(first ? firstX : secondX, roadTop, CourseLeft - 0.15f, speed, 0);
                if (difficulty != DifficultyLevel.Easy && second)
                    CreateDog(secondX, roadTop, CourseLeft - 0.15f, speed, 1);
                if (difficulty == DifficultyLevel.Hard &&
                    TryChooseDogSpawnX(dogRandom, 27f, 31f, out float thirdX))
                    CreateDog(thirdX, roadTop, CourseLeft - 0.15f, speed, 2);
                if (!first && !second)
                    CreateDog(CourseRight - 0.6f, roadTop, CourseLeft - 0.15f, speed, 0);
            }
        }

        bool TryChooseDogSpawnX(System.Random random, float minX, float maxX, out float spawnX)
        {
            float edgeMargin = cyberTheme ? 1.20f : 1.55f;
            var validRanges = new List<Vector2>();
            foreach (RoadSpan road in roads)
            {
                float left = Mathf.Max(minX, road.Left + edgeMargin);
                float right = Mathf.Min(maxX, road.Right - edgeMargin);
                if (right > left)
                    validRanges.Add(new Vector2(left, right));
            }
            if (validRanges.Count == 0)
            {
                spawnX = 0f;
                return false;
            }
            Vector2 selected = validRanges[random.Next(validRanges.Count)];
            spawnX = NextRange(random, selected.x, selected.y);
            return true;
        }

        bool TryChooseTreeX(System.Random random, out float treeX)
        {
            // Ağaç bir çukura değil gerçek yol collider'ına yerleşsin. Kenarlardan
            // pay bırakmak tırmanma trigger'ının boşluğa taşmasını engeller.
            const float minX = 4.5f;
            float maxX = GoalX - 4.5f;
            const float edgeMargin = 1.05f;
            var validRanges = new List<Vector2>();
            foreach (RoadSpan range in roads)
            {
                float start = Mathf.Max(minX, range.Left + edgeMargin);
                float end = Mathf.Min(maxX, range.Right - edgeMargin);
                if (end > start)
                    validRanges.Add(new Vector2(start, end));
            }

            if (validRanges.Count == 0)
            {
                treeX = 0f;
                return false;
            }

            var chosen = validRanges[random.Next(validRanges.Count)];
            treeX = NextRange(random, chosen.x, chosen.y);
            return true;
        }

        void CreateDog(float x, float roadTop, float leftBound, float speed, int dogIndex)
        {
            Sprite first = dogRunFrames[0];
            var go = new GameObject($"DogObstacle_{dogIndex + 1}");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector3(x, roadTop, 0f);
            var visual = new GameObject("DogVisual");
            visual.transform.SetParent(go.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = first;
            renderer.sortingOrder = 9;
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = cyberTheme ? new Vector2(1.30f, 0.82f) : new Vector2(1.98f, 1.05f);
            collider.offset = new Vector2(0f, collider.size.y * 0.5f);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var dogGaps = new Vector2[gaps.Count];
            for (int i = 0; i < gaps.Count; i++)
                dogGaps[i] = new Vector2(gaps[i].Left, gaps[i].Right);
            go.AddComponent<DogObstacle>().Configure(renderer, dogRunFrames, speed, leftBound,
                CourseRight + 2f,
                roadTop, cyberTheme ? 1.95f : 2.75f,
                cyberTheme ? 0.05f : 0.42f, cyberTheme ? 0.085f : 0.055f,
                dogGaps, currentLevelNumber + dogIndex);
        }

        void CreateClimbableTree(float x, float roadTop)
        {
            var go = new GameObject("ClimbableTree");
            go.transform.SetParent(generatedRoot, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = climbTreeSprite;
            renderer.sortingOrder = 7;
            // Ağacı kök temas noktasını değiştirmeden genel olarak büyüt.
            // Aşağıdaki bottom-alignment hesabı görünen kökü aynı yol hizasında tutar.
            float targetHeight = 6.10f;
            float scale = climbTreeSprite.bounds.size.y > 0f ? targetHeight / climbTreeSprite.bounds.size.y : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            // PNG'nin kök altında şeffaf payı var. Görünen kök ucunu kaldırıma
            // gömerek ağacın havada durmasını önleriz.
            float visibleRootInset = 0.98f * (targetHeight / 5.25f);
            go.transform.position = new Vector3(x,
                roadTop - climbTreeSprite.bounds.min.y * scale - visibleRootInset, 0f);

            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            // Bu collider fiziksel engel değildir; yalnız gövde merkezinde dar
            // bir tırmanma menzili tanımlar. Kedi sağ-sol ile serbestçe geçer.
            trigger.size = new Vector2(climbTreeSprite.bounds.size.x * 0.16f,
                climbTreeSprite.bounds.size.y * 0.82f);
            trigger.offset = new Vector2(0f,
                climbTreeSprite.bounds.min.y + climbTreeSprite.bounds.size.y * 0.46f);
            var perch = new GameObject("ClimbPerch").transform;
            perch.SetParent(go.transform, false);
            // Tepe noktası ağacın görsel sınırını aşmaz.
            perch.localPosition = new Vector3(0f, climbTreeSprite.bounds.max.y * 0.78f, 0f);
            go.AddComponent<ClimbableTree>().Configure(climbingCatFrames, perch);
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
            if (IsContinuousCityUnderfill())
                return;
            // Sokak ve orman platform PNG'lerinin üst kısmında şeffaf pay bulunuyor.
            // Dolguyu collider yüzeyine kadar çıkarmak bu payın arkasından görünerek
            // kaldırımın üstüne taşmış gibi duruyordu; dolgu yalnızca alt kesitte kalır.
            topY -= 0.28f;
            const float bottomY = -5.35f;
            float height = topY - bottomY;
            var go = new GameObject("RoadUnderfill");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(centerX, bottomY + height * 0.5f);
            go.transform.localScale = new Vector3(width, height, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = roadUnderfillSprite != null ? roadUnderfillSprite : FallbackSprite.WhiteSquare();
            bool forest = roadSprite != null && roadSprite.name.ToLowerInvariant().Contains("forest");
            bool city = roadSprite != null && roadSprite.name.Contains("city_sidewalk");
            renderer.color = roadUnderfillSprite != null ? Color.white : cyberTheme
                ? new Color(0.10f, 0.12f, 0.20f, 1f) : city
                ? CityRoadFillColor
                : forest ? new Color(0.115f, 0.095f, 0.065f, 1f) : new Color(0.34f, 0.20f, 0.12f, 1f);
            renderer.sortingOrder = -1;
            if (roadUnderfillSprite != null)
            {
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = new Vector2(width, height);
                go.transform.localScale = Vector3.one;
            }
        }

        void CreateContinuousCityUnderfill(float centerX, float centerY, float width, float height)
        {
            var go = new GameObject("ContinuousCityRoad");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(centerX, centerY);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = roadUnderfillSprite;
            renderer.sortingOrder = -1;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            // Kare dokunun tüm dikey kesiti görünür; yatayda en-boy oranı
            // bozulmadan tekrar eder. Eski 36x4 germe dokuyu eziyordu.
            float spriteHeight = Mathf.Max(0.01f, roadUnderfillSprite.bounds.size.y);
            float scale = height / spriteHeight;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.size = new Vector2(width / scale, spriteHeight);
        }

        bool IsContinuousCityUnderfill()
        {
            return roadUnderfillSprite != null &&
                roadUnderfillSprite.name.ToLowerInvariant().Contains("city");
        }

        // Editör doğrulaması bu veriyi sahne görseline bakmadan kontrol eder.
        public string ValidateCourseGeometry()
        {
            const float tolerance = 0.001f;
            foreach (RoadSpan road in roads)
            {
                if (road.Width <= 0f || road.Top <= road.Bottom)
                    return "Geçersiz yol boyutu.";
            }
            foreach (GapSpan gap in gaps)
            {
                if (gap.Width <= 0f)
                    return "Geçersiz çukur genişliği.";
                foreach (RoadSpan road in roads)
                {
                    if (road.Left < gap.Right - tolerance && road.Right > gap.Left + tolerance)
                        return "Çukur ile yol çakışıyor.";
                }
            }
            foreach (Vector2 mound in moundVisualRanges)
            {
                bool contained = false;
                foreach (RoadSpan road in roads)
                {
                    if (mound.x >= road.Left - tolerance && mound.y <= road.Right + tolerance)
                    {
                        contained = true;
                        break;
                    }
                }
                if (!contained)
                    return "Tümsek görseli yol sınırından taşıyor.";
            }
            return null;
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
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(generatedRoot.gameObject);
                else
#endif
                    Destroy(generatedRoot.gameObject);
            }
            foreach (UnityEngine.Object asset in generatedRuntimeAssets)
            {
                if (asset != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(asset);
                    else
#endif
                        Destroy(asset);
                }
            }
            generatedRuntimeAssets.Clear();
        }
    }
}
