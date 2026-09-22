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
        const float SecondSectionOffset = 13.0f;
        static readonly Color RoadColor = new Color(0.72f, 0.54f, 0.38f, 1f);
        static readonly Color ObstacleColor = new Color(0.61f, 0.42f, 0.29f, 1f);
        static readonly Color CityRoadFillColor = new Color(0.70f, 0.66f, 0.60f, 1f);

        Transform generatedRoot;
        Sprite gapSprite;
        Sprite moundSprite;
        Sprite alternateMoundSprite;
        Sprite roadSprite;
        Sprite roadUnderfillSprite;
        Sprite[] birdFrames;
        Sprite[] dogRunFrames;
        Sprite climbTreeSprite;
        Sprite[] climbingCatFrames;
        readonly List<Vector2> roadRanges = new List<Vector2>();

        public float CatSpawnY => RoadCenterY + RoadHeight * 0.5f + 0.40f * GameplayScale;
        public float GoalY => CatSpawnY + 0.15f;
        public float GoalX => 19.35f;

        public void BindVisuals(Sprite gap, Sprite mound, Sprite road, Sprite alternateMound = null,
            Sprite underfill = null, Sprite[] birds = null, Sprite[] dogs = null,
            Sprite tree = null, Sprite[] climbingFrames = null)
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
            roadRanges.Clear();
            generatedRoot = new GameObject("GeneratedCourse").transform;
            generatedRoot.SetParent(transform, false);

            // Cadde asfaltı bütün bölüm boyunca tek görseldir. Parça parça çizmek
            // şeritlerin her kaldırım ve konteynır altında yeniden başlamasına yol açıyordu.
            if (roadUnderfillSprite != null)
            {
                const float cityRoadBottom = -5.35f;
                float cityRoadTop = RoadCenterY + RoadHeight * 0.5f;
                float cityRoadHeight = cityRoadTop - cityRoadBottom;
                Decoration("ContinuousCityRoad", roadUnderfillSprite,
                    new Vector2(6.5f, cityRoadBottom + cityRoadHeight * 0.5f),
                    36f, -1, cityRoadHeight);
            }

            int pattern = Mathf.Abs(levelNumber - 1) % 5;
            // Kameranın ilk ve son ekranında kaldırımın kadraj dışında da devam
            // etmesini sağlar; yalnızca tasarlanmış çukurlar açık kalır.
            Road(-8.25f, 2.2f);
            BuildPattern(pattern, 0f);
            BuildPattern((pattern + 2) % 5, SecondSectionOffset);
            Road(21.55f, 2.9f);
            BuildModeChallenge(levelNumber);
            BuildDogAndTreeChallenge(levelNumber);
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
            roadRanges.Add(new Vector2(centerX - width * 0.5f, centerX + width * 0.5f));
            var go = new GameObject("Road");
            go.transform.SetParent(generatedRoot, false);
            go.transform.position = new Vector2(centerX, RoadCenterY);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
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
                bool citySurface = roadSprite != null && roadSprite.name.Contains("city_sidewalk");
                float visualHeight = citySurface ? 0.82f : 3.35f * GameplayScale;
                float visualCenterY = citySurface
                    ? roadTop - visualHeight * 0.5f + 0.03f
                    : RoadCenterY - 0.02f;
                Decoration("RoadVisual", roadSprite, new Vector2(x, visualCenterY),
                    tileWidth + 0.18f, 1, visualHeight);
            }
        }

        void Obstacle(float centerX, float width, float height)
        {
            Sprite obstacleSprite = alternateMoundSprite != null && Random.value >= 0.5f
                ? alternateMoundSprite : moundSprite;
            bool cityVehicle = obstacleSprite != null && obstacleSprite.name.Contains("city_car");
            // Cadde temasında boyut varyasyonu yoktur: sadece otomobil/minibüs
            // görseli değişir. Büyük-küçük varyasyonu orman tümseklerine aittir.
            bool isTallVariant = !cityVehicle && Random.value >= 0.5f;
            // Uzun varyant eskiden 2 kat yüksek ve normal varyantla aynı genişlikteydi.
            // Bu, yamacı gereğinden dikleştiriyor ve çift zıplamayla bile geçilemeyen
            // bir collider tepesi oluşturuyordu. Biraz alçaltıp tabanını genişletiyoruz.
            float heightVariant = isTallVariant ? 1.55f : 1f;
            float widthVariant = isTallVariant ? 1.15f : 1f;
            float obstacleWidth = width * 1.62f * widthVariant * GameplayScale;
            float obstacleHeight = height * 1.62f * heightVariant * GameplayScale;
            float visualWidth = obstacleWidth * 1.95f;
            float visualOverlap = (cityVehicle ? 0.27f : obstacleSprite != null && obstacleSprite.name.Contains("forest") ? 0.52f : 0.95f) *
                heightVariant * GameplayScale;
            float colliderHeight = obstacleHeight;
            if (obstacleSprite != null && obstacleSprite.bounds.size.x > 0f)
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
                : new[]
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

            if (obstacleSprite != null)
            {
                // Üretilen orman tümseği PNG'sinin altında geniş şeffaf tuval payı var.
                // Görünen yosun/toprak tabanını yolun içine oturtmak için yalnızca bu
                // görsele daha fazla bindirme uygula.
                DecorationBottomAligned("HumpVisual", obstacleSprite, centerX, roadTop - visualOverlap,
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
                    DecorationBottomAligned("TrashContainer", gapSprite, centerX,
                        roadTop - 1.66f, width * 2.10f, 2);
                    return;
                }
                // Görselin en yüksek uçları doğrudan yol yüzeyine sabitlenir; böylece
                // orman çukurunun yan dudakları yükselmez ve arada boşluk kalmaz.
                DecorationTopAligned("GapVisual", gapSprite, centerX,
                    roadTop, (width + 1.95f) * GameplayScale, -2, 5.8f);
            }
        }

        void BuildModeChallenge(int levelNumber)
        {
            if (birdFrames == null || birdFrames.Length == 0 || birdFrames[0] == null)
                return;

            // Hareketli engeller başlangıç ekranında görünmez. Her bölümde kuşların
            // sayısı, ilk konumu, uçuş yüksekliği ve hızı deterministik olarak değişir.
            // Böylece yeniden denemede düzen korunur fakat her bölüm aynı hissettirmez.
            var random = new System.Random(levelNumber * 7919 + 173);
            int birdCount = 2 + (levelNumber % 3 == 0 ? 1 : 0);
            float roadTop = RoadCenterY + RoadHeight * 0.5f;
            for (int i = 0; i < birdCount; i++)
            {
                // Kuş bir kez sağdan girip uzun bir mesafe uçar; kısa bir bölgede
                // ışınlanıp tekrar tekrar doğmaz.
                float spawnX = 11.5f + i * 5.4f + NextRange(random, 0.4f, 1.8f);
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
                leftBound, phase);

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
                CreateDog(22.4f, roadTop, -9.5f,
                    1.8f + levelNumber % 4 * 0.18f);
        }

        bool TryChooseTreeX(System.Random random, out float treeX)
        {
            // Ağaç bir çukura değil gerçek yol collider'ına yerleşsin. Kenarlardan
            // pay bırakmak tırmanma trigger'ının boşluğa taşmasını engeller.
            const float minX = 4.5f;
            const float maxX = 15.5f;
            const float edgeMargin = 1.05f;
            var validRanges = new List<Vector2>();
            foreach (var range in roadRanges)
            {
                float start = Mathf.Max(minX, range.x + edgeMargin);
                float end = Mathf.Min(maxX, range.y - edgeMargin);
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

        void CreateDog(float x, float roadTop, float leftBound, float speed)
        {
            Sprite first = dogRunFrames[0];
            var go = new GameObject("DogObstacle");
            go.transform.SetParent(generatedRoot, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = first;
            renderer.sortingOrder = 9;
            float targetWidth = 2.75f;
            float scale = first.bounds.size.x > 0f ? targetWidth / first.bounds.size.x : 1f;
            go.transform.localScale = new Vector3(-scale, scale, 1f);
            float bottomOffset = -first.bounds.min.y * scale;
            go.transform.position = new Vector3(x, roadTop + bottomOffset, 0f);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(first.bounds.size.x * 0.72f, first.bounds.size.y * 0.62f);
            collider.offset = new Vector2(0f, first.bounds.min.y + first.bounds.size.y * 0.34f);
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            go.AddComponent<DogObstacle>().Configure(renderer, dogRunFrames, speed, leftBound, roadTop);
        }

        void CreateClimbableTree(float x, float roadTop)
        {
            var go = new GameObject("ClimbableTree");
            go.transform.SetParent(generatedRoot, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = climbTreeSprite;
            renderer.sortingOrder = 7;
            float targetHeight = 5.25f;
            float scale = climbTreeSprite.bounds.size.y > 0f ? targetHeight / climbTreeSprite.bounds.size.y : 1f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            // PNG'nin kök altında şeffaf payı var. Görünen kök ucunu kaldırıma
            // gömerek ağacın havada durmasını önleriz.
            const float visibleRootInset = 0.98f;
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
            if (roadUnderfillSprite != null)
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
            renderer.color = roadUnderfillSprite != null ? Color.white : city
                ? CityRoadFillColor
                : forest ? new Color(0.115f, 0.095f, 0.065f, 1f) : new Color(0.34f, 0.20f, 0.12f, 1f);
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
