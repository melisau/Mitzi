using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PawPath.Ads;
using PawPath.Audio;
using PawPath.Cat;
using PawPath.Content;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Economy;
using PawPath.Hub;
using PawPath.Levels;
using PawPath.Localization;
using PawPath.Season;
using PawPath.UI;
using PawPath.Gameplay;

namespace PawPath.Core
{
    /// <summary>
    /// Boş sahnede bile tek bileşenle oynanabilir iskeleti kurar.
    /// Unity menüsü: Paw Path / Build Starter Scene aynı hiyerarşiyi kalıcı sahnede üretir.
    /// </summary>
    public class RuntimeBootstrap : MonoBehaviour
    {
        [SerializeField] PawPathCatalog catalog;

        void Awake()
        {
            if (FindObjectOfType<GameFlow>() != null)
                return;

            // Starter sahnenin eski sürümlerinden kalmış statik HUD, runtime'da
            // yeniden kurulan arayüzün üzerinde isimsiz "Button"lar gösteriyordu.
            foreach (var staleHud in FindObjectsOfType<HudView>(true))
            {
                if (staleHud != null)
                    staleHud.gameObject.SetActive(false);
            }
            if (catalog == null)
                Debug.LogError("[PawPath] RuntimeBootstrap'a katalog atanmamış; yedek katalog kullanılıyor (dükkan eşyaları görselsiz olur).");
            else
                WarnIfCatalogLooksReset(catalog);
            Build(catalog != null ? catalog : CatalogFactory.CreateRuntime());
        }

        /// <summary>
        /// "Paw Path > Build Starter Scene" eskiden kataloğu kod içi yedekle değiştiriyordu:
        /// görselsiz 4 eşya kalıyordu. Böyle bir katalog fark edilirse konsolda açıkça uyar.
        /// </summary>
        static void WarnIfCatalogLooksReset(PawPathCatalog value)
        {
            int missing = 0, total = 0;
            if (value.shopItems != null)
                foreach (var item in value.shopItems)
                {
                    if (item == null) continue;
                    total++;
                    if (item.placedSprite == null) missing++;
                }
            if (total == 0 || missing > 0)
                Debug.LogError($"[PawPath] Dükkan kataloğu eksik görünüyor ({total} eşya, {missing} tanesi görselsiz). " +
                    "PawPathCatalog.asset sıfırlanmış olabilir: dosyadaki yerel değişiklikleri geri al (git) veya " +
                    "'python3 tools/build_shop_catalog.py' çalıştır.");
        }

        public static GameFlow Build(PawPathCatalog catalog)
        {
            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            var cam = camGo.GetComponent<Camera>();
            if (cam == null)
                cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            // Mobil yatay ekranda kedi ve engeller daha rahat okunabilsin.
            cam.orthographicSize = 4.45f;
            cam.backgroundColor = new Color(0.93f, 0.88f, 0.84f);
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();
            if (camGo.GetComponent<LineDraw>() == null)
                camGo.AddComponent<LineDraw>();

            var root = new GameObject("PawPath");
            var flow = root.AddComponent<GameFlow>();
            // Diğer bileşenler (dükkan, eşyalar) GameFlow.Instance.Catalog'u okur; kurulumun
            // ortasında bir hata olsa bile yanlış (yedek) katalogda kalınmasın diye ilk iş bağla.
            flow.BindCatalog(catalog);
            root.AddComponent<CozyEconomyManager>();
            root.AddComponent<RewardedAdService>();
            var audio = root.AddComponent<CozyAudioManager>();
            audio.BindLibrary(catalog);
            var needs = root.AddComponent<CatNeedsSystem>();
            var levels = root.AddComponent<LevelManager>();

            var hub = new GameObject("Hub");
            hub.transform.SetParent(root.transform);
            var house = hub.AddComponent<CatHouseManager>();
            hub.AddComponent<CatHouseInteraction>();

            var hubBg = CreateQuad("HubRoom", hub.transform, new Vector3(0f, 0.2f, 1f), new Vector3(14f, 8f, 1f), new Color(0.90f, 0.82f, 0.74f));
            hubBg.sortingOrder = -2;
            if (catalog.homeBackground != null)
            {
                hubBg.sprite = catalog.homeBackground;
                hubBg.color = Color.white;
                FitSpriteToCamera(hubBg, cam);
            }

            var furnitureSlots = new Transform[9];
            // Varsayılan yerleşim: zemin çizgisi ~y=-1.25 (eşyalar bunun altında, tahtanın üstünde durur); sol alttaki bakım düğmeleri ve
            // pencerenin önü boş bırakılır. Oyuncu Evi Düzenle ile her eşyayı taşıyabilir.
            furnitureSlots[(int)FurnitureSlotType.Rug] = CreateFurnitureSlot("RugSlot", hub.transform, new Vector3(0.9f, -2.75f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Bed] = CreateFurnitureSlot("BedSlot", hub.transform, new Vector3(2.9f, -1.45f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Bowl] = CreateFurnitureSlot("BowlSlot", hub.transform, new Vector3(0.0f, -1.72f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Wallpaper] = CreateFurnitureSlot("WallpaperSlot", hub.transform, new Vector3(0f, 0f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Poster] = CreateFurnitureSlot("PosterSlot", hub.transform, new Vector3(1.1f, 1.4f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Water] = CreateFurnitureSlot("WaterSlot", hub.transform, new Vector3(0.95f, -1.82f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Sand] = CreateFurnitureSlot("SandSlot", hub.transform, new Vector3(-1.9f, -0.70f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Tree] = CreateFurnitureSlot("TreeSlot", hub.transform, new Vector3(5.3f, 0.27f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Toy] = CreateFurnitureSlot("ToySlot", hub.transform, new Vector3(1.8f, -2.35f, 0f));
            var furnitureView = hub.AddComponent<HubFurnitureView>();
            furnitureView.Bind(furnitureSlots);
            furnitureView.BindRoom(hubBg, catalog.homeBackground);
            var homeRooms = hub.AddComponent<HomeRooms>();
            homeRooms.Bind(cam, hubBg);

            var spots = new Transform[8];
            var spotPositions = new[]
            {
                // Kedilerin dinlendiği noktalar: zeminde, alt ortadaki bakım düğmelerinin üstünde.
                new Vector3(-3.10f, -2.95f, 0f),
                new Vector3(-0.90f, -3.20f, 0f),
                new Vector3(0.55f, -3.30f, 0f),
                new Vector3(2.60f, -3.25f, 0f),
                new Vector3(4.35f, -3.05f, 0f),
                new Vector3(-2.20f, -3.30f, 0f),
                new Vector3(3.40f, -2.30f, 0f),
                new Vector3(-4.30f, -2.30f, 0f)
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var s = new GameObject($"Spot_{i}");
                s.transform.SetParent(hub.transform);
                s.transform.position = spotPositions[i];
                spots[i] = s.transform;
            }
            house.Bind(spots, null, null);

            var levelRoot = new GameObject("Level");
            levelRoot.transform.SetParent(root.transform);

            var sky = CreateQuad("Sky", levelRoot.transform, new Vector3(0f, 1.4f, 2f), new Vector3(18f, 8f, 1f), new Color(0.78f, 0.90f, 0.86f));
            sky.sortingOrder = -5;
            var landscapeGround = CreateQuad("LandscapeGround", levelRoot.transform, new Vector3(0f, -3.35f, 1.5f), new Vector3(18f, 3.2f, 1f), new Color(0.72f, 0.84f, 0.62f));
            landscapeGround.sortingOrder = -4;
            if (catalog.gameplayBackground != null)
            {
                sky.sprite = catalog.gameplayBackground;
                sky.color = Color.white;
                FitSpriteToCamera(sky, cam);
                landscapeGround.enabled = false;
                var parallax = sky.gameObject.AddComponent<ParallaxBackground>();
                parallax.Bind(cam, sky, 0.32f);
            }
            var course = levelRoot.AddComponent<LevelCourseBuilder>();
            course.BindVisuals(catalog.roadGapSprite, catalog.moundSprite, catalog.roadPlatformSprite,
                catalog.streetMoundHighSprite,
                streetGapLeft: catalog.streetGapLeftSprite,
                streetGapRight: catalog.streetGapRightSprite);

            var season = levelRoot.AddComponent<SeasonBackdrop>();
            season.Bind(sky, landscapeGround, null);

            var spawn = new GameObject("Spawn");
            spawn.transform.SetParent(levelRoot.transform);
            spawn.transform.position = new Vector3(-6.2f, -1.15f, 0f);

            var goal = new GameObject("Goal");
            goal.transform.SetParent(levelRoot.transform);
            goal.transform.position = new Vector3(6.25f, 0.2f, 0f);
            var goalCol = goal.AddComponent<BoxCollider2D>();
            goalCol.isTrigger = true;
            // Geniş yatay WinZone korunur; altı artık çukurun derinine uzanmaz.
            goalCol.size = new Vector2(10f, 5.35f);
            goalCol.offset = new Vector2(5f, 2.025f);
            goal.AddComponent<GoalTrigger>();
            BuildFinishGate(goal.transform, catalog.finishPortalSprite);

            levels.Bind(spawn.transform, goal.transform, season);
            levels.BindCourse(course);

            var catGo = new GameObject("PlayableCat");
            catGo.transform.SetParent(levelRoot.transform);
            catGo.transform.position = spawn.transform.position;
            var rb = catGo.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            var circle = catGo.AddComponent<CircleCollider2D>();
            circle.radius = 0.336f;
            // Eğimli tümseklerde collider sürtünmesi kedinin yatay hareketini
            // sıfırlamasın; yürüyüş kontrolü hızı zaten CatController'da belirliyor.
            circle.sharedMaterial = new PhysicsMaterial2D("CatMovementNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            var vis = new GameObject("Visual");
            vis.transform.SetParent(catGo.transform);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale = Vector3.one;
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = FallbackSprite.WhiteCircle();
            sr.sortingOrder = 10;
            vis.AddComponent<Animator>();
            var controller = catGo.AddComponent<CatController>();
            controller.BindVisual(vis.transform, null);
            // Yere yansıyan yumuşak gölge: kediyi sahneye oturtur, zıplarken küçülür.
            SoftShadow.Attach(catGo, sr, true, 9);

            var sideScroll = camGo.GetComponent<SideScrollCamera>();
            if (sideScroll == null)
                sideScroll = camGo.AddComponent<SideScrollCamera>();
            // Sokak artık kendi ParallaxBackground bileşeniyle daha yavaş kayar.
            // Kameraya birebir bağlamak görseli ekranda sabit tutuyordu.
            sideScroll.Bind(catGo.transform);

            var canvasGo = CreateCanvas(root.transform);
            var care = BuildCareUi(canvasGo.transform, needs);
            var hud = BuildHud(canvasGo.transform, care);
            homeRooms.BuildControls(hud.transform);
            var rescue = BuildRescue(canvasGo.transform);
            var levelComplete = BuildLevelComplete(canvasGo.transform, catalog);
            var levelFailure = BuildLevelFailure(canvasGo.transform, catalog);
            var shop = BuildShop(canvasGo.transform, catalog);
            shop.SetActive(false);
            // En üstte: ilk açılışta gösterilen, Ayarlar'dan da açılan "Nasıl Oynanır".
            BuildHowToPlay(canvasGo.transform);
            rescue.SetActive(false);
            levelComplete.SetActive(false);
            levelFailure.SetActive(false);

            var hudView = hud.GetComponent<HudView>();
            var shopBtn = FindButton(hud.transform, "ShopButton");
            if (shopBtn != null)
            {
                shopBtn.onClick.RemoveAllListeners();
                shopBtn.onClick.AddListener(() => shop.SetActive(true));
                care.BindShopHint(shopBtn);
            }
            ApplyTypeScale(canvasGo.transform);

            flow.BindCatalog(catalog);
            flow.BindRoots(hub, levelRoot, hud, rescue, levelComplete, levelFailure);
            hub.SetActive(true);
            levelRoot.SetActive(false);
            return flow;
        }

        static void BuildFinishGate(Transform goal, Sprite portalSprite)
        {
            var portal = new GameObject("FinishPortal");
            portal.transform.SetParent(goal, false);
            portal.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            var renderer = portal.AddComponent<SpriteRenderer>();
            renderer.sprite = portalSprite != null ? portalSprite : FallbackSprite.WhiteCircle();
            renderer.color = portalSprite != null
                ? new Color(1f, 1f, 1f, 0.68f)
                : new Color(1f, 0.82f, 0.32f, 0.68f);
            renderer.sortingOrder = 8;
            if (renderer.sprite != null && renderer.sprite.bounds.size.y > 0f)
            {
                float scale = 5.65f / renderer.sprite.bounds.size.y;
                portal.transform.localScale = new Vector3(scale, scale, 1f);
                // Goal merkezi kedi yüksekliğindedir; portalın sprite alt sınırını
                // kaldırım yüzeyine biraz gömerek havada kalmasını önle.
                // Görselin altındaki şeffaf pay nedeniyle kapı hâlâ havada
                // görünüyordu; yalnız görseli biraz daha zemine göm.
                float localY = -1.02f - renderer.sprite.bounds.min.y * scale;
                portal.transform.localPosition = new Vector3(0f, localY, 0f);
            }
        }

        static SpriteRenderer CreateQuad(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FallbackSprite.WhiteSquare();
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Simple;
            return sr;
        }

        static GameObject CreateCanvas(Transform parent)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return go;
        }

        static GameObject BuildHud(Transform canvas, CatNeedsUI careUi)
        {
            var hud = Panel("HUD", canvas, new Vector2(0, 0), new Vector2(0, 0), new Color(1, 1, 1, 0));
            hud.AddComponent<HudView>();
            var content = SafeContent(hud.transform);
            // ── ÜST ŞERİT ─────────────────────────────────────────────
            // Sol: sade bakiye sayacı (para ikonu + sayı). Tıklanabilir görünmez.
            var balanceRoot = new GameObject("ShopBalance", typeof(RectTransform), typeof(Image));
            balanceRoot.transform.SetParent(content, false);
            var balanceRt = balanceRoot.GetComponent<RectTransform>();
            balanceRt.anchorMin = balanceRt.anchorMax = new Vector2(0f, 1f);
            balanceRt.anchoredPosition = new Vector2(EdgeMargin + 130f, -(EdgeMargin + 44f));
            balanceRt.sizeDelta = new Vector2(260f, 88f);
            var balanceBg = balanceRoot.GetComponent<Image>();
            balanceBg.sprite = CozyIconFactory.Pill();
            balanceBg.type = Image.Type.Sliced;
            balanceBg.color = new Color(0.20f, 0.13f, 0.09f, 0.72f);
            balanceBg.raycastTarget = false;
            var coinIcon = IconImage(balanceRoot.transform, "Coin", CozyIconFactory.Coin(), Color.white,
                new Vector2(0f, 0.5f), new Vector2(46f, 0f), 76f);
            coinIcon.raycastTarget = false;
            var love = Label(balanceRoot.transform, "Value", "0", new Vector2(0.5f, 0.5f), new Vector2(38f, 0f));
            love.rectTransform.sizeDelta = new Vector2(170f, 70f);
            love.fontSize = FontButton;
            love.fontStyle = FontStyle.Bold;
            love.color = new Color(1f, 0.95f, 0.82f);
            AddTextShadow(love);

            var nextCatCard = new GameObject("NextCatGoal", typeof(RectTransform), typeof(Image));
            nextCatCard.transform.SetParent(content, false);
            var nextCatRect = nextCatCard.GetComponent<RectTransform>();
            nextCatRect.anchorMin = nextCatRect.anchorMax = new Vector2(0, 1);
            // Share the balance row instead of covering the care card below it.
            nextCatRect.anchoredPosition = new Vector2(EdgeMargin + 359, -(EdgeMargin + 44));
            nextCatRect.sizeDelta = new Vector2(166, 76);
            var nextCatBackground = nextCatCard.GetComponent<Image>();
            nextCatBackground.sprite = CozyIconFactory.Pill();
            nextCatBackground.type = Image.Type.Sliced;
            nextCatBackground.color = new Color(.20f, .13f, .09f, .72f);
            nextCatBackground.raycastTarget = false;
            var mysteryCat = IconImage(nextCatCard.transform, "MysteryCat", CozyIconFactory.MysteryCat(),
                new Color(1f, .95f, .82f), new Vector2(0, .5f), new Vector2(34, 0), 54);
            mysteryCat.raycastTarget = false;
            var nextCatSymbol = Label(nextCatCard.transform, "Mystery", "?", new Vector2(0, .5f), new Vector2(34, -2));
            nextCatSymbol.rectTransform.sizeDelta = new Vector2(36, 42);
            nextCatSymbol.fontSize = 30; nextCatSymbol.fontStyle = FontStyle.Bold;
            nextCatSymbol.raycastTarget = false;
            nextCatSymbol.color = new Color(.27f, .17f, .11f);
            var targetFlag = IconImage(nextCatCard.transform, "TargetLevel", CozyIconFactory.FinishFlag(),
                new Color(.98f, .76f, .30f), new Vector2(0, .5f), new Vector2(82, 0), 30);
            targetFlag.raycastTarget = false;
            var nextCatText = Label(nextCatCard.transform, "Goal", "", new Vector2(0, .5f), new Vector2(128, 0));
            nextCatText.rectTransform.sizeDelta = new Vector2(66, 60);
            nextCatText.fontSize = 34; nextCatText.fontStyle = FontStyle.Bold;
            nextCatText.alignment = TextAnchor.MiddleCenter;
            nextCatText.color = new Color(1f, .95f, .82f);
            nextCatText.raycastTarget = false;
            hud.AddComponent<NextCatGoalView>().Bind(nextCatCard, nextCatText, nextCatSymbol);

            // Orta: bölüm rozeti (havada asılı yazı yerine küçük bir hap).
            var levelBadge = new GameObject("LevelBadge", typeof(RectTransform), typeof(Image));
            levelBadge.transform.SetParent(content, false);
            var levelBadgeRt = levelBadge.GetComponent<RectTransform>();
            levelBadgeRt.anchorMin = levelBadgeRt.anchorMax = new Vector2(0.5f, 1f);
            levelBadgeRt.anchoredPosition = new Vector2(0f, -(EdgeMargin + 36f));
            levelBadgeRt.sizeDelta = new Vector2(250f, 68f);
            var levelBadgeBg = levelBadge.GetComponent<Image>();
            levelBadgeBg.sprite = CozyIconFactory.Pill();
            levelBadgeBg.type = Image.Type.Sliced;
            levelBadgeBg.color = new Color(0.20f, 0.13f, 0.09f, 0.55f);
            levelBadgeBg.raycastTarget = false;
            var level = Label(levelBadge.transform, "Level", GameText.LevelLabel(1), new Vector2(0.5f, 0.5f), Vector2.zero);
            level.rectTransform.sizeDelta = new Vector2(240f, 60f);
            level.fontSize = FontBody;
            level.fontStyle = FontStyle.Bold;
            level.color = new Color(1f, 0.95f, 0.82f);

            // Bölüm ilerleme çubuğu (yalnızca bölüm içinde görünür).
            BuildProgressBar(content, -(EdgeMargin + 36f) - 34f - 14f - 26f);

            var ink = Label(content, "Ink", "", new Vector2(0f, 1f), new Vector2(150f, -185f));
            ink.fontSize = FontBody;
            ink.rectTransform.sizeDelta = new Vector2(160f, 52f);
            var inkIcon = IconImage(ink.transform, "Pencil", CozyIconFactory.Pencil(), UtilityColor,
                new Vector2(0.5f, 0.5f), new Vector2(-105f, 0f), 40f);
            inkIcon.raycastTarget = false;
            var inkTrack = IconImage(ink.transform, "InkTrack", CozyIconFactory.Pill(),
                new Color(0.25f, 0.18f, 0.13f, 0.35f), new Vector2(0.5f, 0.5f), new Vector2(-15f, -36f), 190f);
            inkTrack.type = Image.Type.Sliced;
            inkTrack.rectTransform.sizeDelta = new Vector2(190f, 16f);
            inkTrack.raycastTarget = false;
            var inkFill = IconImage(inkTrack.transform, "Remaining", CozyIconFactory.Pill(), DrawModeColor,
                new Vector2(0f, 0.5f), Vector2.zero, 190f);
            inkFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            inkFill.rectTransform.sizeDelta = new Vector2(190f, 16f);
            inkFill.type = Image.Type.Sliced;
            inkFill.raycastTarget = false;
            ink.gameObject.AddComponent<DrawingInkMeter>().Bind(inkFill);
            ink.gameObject.SetActive(false);
            var selected = Label(content, "Selected", "", new Vector2(0.5f, 1f), new Vector2(0, -260));
            selected.gameObject.SetActive(false);

            // Sağ: [✎] [Dükkan] [OYNA] [⚙]  — Oyna ana aksiyon: büyük, yeşil, nabızlı.
            var settingsButton = RoundIconButton(content, "SettingsButton", CozyIconFactory.Gear(),
                UtilityColor, new Vector2(1f, 1f),
                new Vector2(-(EdgeMargin + 52f), -(EdgeMargin + 52f)), 104f, 0.62f);
            // Oyna ve Dükkan, yuvarlak düğmelerle aynı görsel dilde hap düğmeler.
            var play = Button(content, "PlayButton", GameText.L("OYNA", "PLAY", "SPIELEN"), new Vector2(1f, 1f), new Vector2(PlayButtonX, -(EdgeMargin + 52f)), Color.white);
            StylePillButton(play, new Vector2(300f, 104f), PlayColor, CozyIconFactory.Play(), FontTitle, true);
            var shop = Button(content, "ShopButton", GameText.Shop, new Vector2(1f, 1f), new Vector2(-594f, -(EdgeMargin + 52f)), Color.white);
            StylePillButton(shop, new Vector2(248f, 104f), UtilityColor, CozyIconFactory.Storefront(), FontButton, false);
            var editHome = RoundIconButton(content, "EditHomeButton", CozyIconFactory.Pencil(),
                HudView.EditIdleColor, new Vector2(1f, 1f), new Vector2(-790f, -(EdgeMargin + 52f)), 104f, 0.52f);
            var flipFurniture = RoundIconButton(content, "FlipFurnitureButton", null,
                HudView.EditIdleColor, new Vector2(1f, 1f), new Vector2(-790f, -(EdgeMargin + 52f) - 118f), 104f, 0.52f);
            var flipLabel = Label(flipFurniture.transform, "Text", "↔", new Vector2(0.5f, 0.5f), Vector2.zero);
            flipLabel.rectTransform.sizeDelta = new Vector2(100f, 100f);
            flipLabel.fontSize = FontTitle;
            flipLabel.fontStyle = FontStyle.Bold;
            flipLabel.color = Color.white;
            flipFurniture.gameObject.SetActive(false);

            // ── AYARLAR PANELİ (ebeveyn alanı: görünüm, ses, geliştirici) ─────
            var settingsPanel = Panel("DisplaySettings", content, Vector2.zero, Vector2.one,
                new Color(0.12f, 0.08f, 0.06f, 0.90f));
            var settingsContent = SafeContent(settingsPanel.transform);
            var settingsTitle = Label(settingsContent, "SettingsTitle", GameText.Settings,
                new Vector2(0.5f, 0.90f), Vector2.zero);
            settingsTitle.color = Color.white;
            settingsTitle.fontSize = FontTitle;
            settingsTitle.fontStyle = FontStyle.Bold;

            SettingsSectionLabel(settingsContent, "SizeSection", GameText.L("Görünüm Boyutu", "Display Size", "Anzeigegröße"), 0.80f);
            var normalSize = Button(settingsContent, "NormalSize", GameText.L("Normal", "Normal", "Normal"),
                new Vector2(0.5f, 0.72f), new Vector2(-220f, 0f), Color.white);
            var largeSize = Button(settingsContent, "LargeSize", GameText.L("Büyük", "Large", "Groß"),
                new Vector2(0.5f, 0.72f), new Vector2(220f, 0f), Color.white);
            StylePillButton(normalSize, new Vector2(330f, 96f), UtilityColor, null, FontButton, false);
            StylePillButton(largeSize, new Vector2(330f, 96f), UtilityColor, null, FontButton, false);
            void RefreshSizeLabels()
            {
                normalSize.GetComponent<Image>().color = !DisplaySizeSettings.Large ? PlayColor : UtilityColor;
                largeSize.GetComponent<Image>().color = DisplaySizeSettings.Large ? PlayColor : UtilityColor;
                normalSize.GetComponentInChildren<Text>().text = GameText.Checked(GameText.L("Normal", "Normal", "Normal"), !DisplaySizeSettings.Large);
                largeSize.GetComponentInChildren<Text>().text = GameText.Checked(GameText.L("Büyük", "Large", "Groß"), DisplaySizeSettings.Large);
            }
            void SetSize(bool large)
            {
                DisplaySizeSettings.SetLarge(large);
                RefreshSizeLabels();
                foreach (var control in hud.GetComponentsInChildren<MobileControlButton>(true))
                    ApplyControlLayout(control.GetComponent<RectTransform>(), control.name, large);
                foreach (var stick in hud.GetComponentsInChildren<MobileJoystick>(true)) stick.Resize(large);
                var drawingStart = FindButton(hud.transform, "StartDrawingGame");
                ApplyDrawingStartLayout(drawingStart != null ? drawingStart.GetComponent<RectTransform>() : null, large);
                if (CatHouseManager.Instance != null)
                    CatHouseManager.Instance.RefreshResidents();
            }
            normalSize.onClick.AddListener(() => SetSize(false));
            largeSize.onClick.AddListener(() => SetSize(true));

            // Müzik ve efekt düğmeleri artık ayarların içinde.
            SettingsSectionLabel(settingsContent, "AudioSection", GameText.L("Ses", "Sound", "Ton"), 0.63f);
            var musicMute = Button(settingsContent, "MusicMuteButton", GameText.L("Müzik", "Music", "Musik"),
                new Vector2(0.5f, 0.55f), new Vector2(-220f, 0f), Color.white);
            var effectsMute = Button(settingsContent, "EffectsMuteButton", GameText.L("Efektler", "Effects", "Effekte"),
                new Vector2(0.5f, 0.55f), new Vector2(220f, 0f), Color.white);
            StylePillButton(musicMute, new Vector2(330f, 96f), UtilityColor, null, FontButton, false);
            StylePillButton(effectsMute, new Vector2(330f, 96f), UtilityColor, null, FontButton, false);
            void RefreshAudioButtons()
            {
                var audio = CozyAudioManager.Instance;
                musicMute.GetComponent<Image>().color = audio != null && audio.MusicMuted ? UtilityColor : PlayColor;
                effectsMute.GetComponent<Image>().color = audio != null && audio.EffectsMuted ? UtilityColor : PlayColor;
                var musicLabel = musicMute.GetComponentInChildren<Text>();
                var effectsLabel = effectsMute.GetComponentInChildren<Text>();
                if (musicLabel != null)
                    musicLabel.text = audio != null && audio.MusicMuted ? GameText.L("♫ Müzik: Kapalı", "♫ Music: Off", "♫ Musik: Aus") : GameText.L("♫ Müzik: Açık", "♫ Music: On", "♫ Musik: An");
                if (effectsLabel != null)
                    effectsLabel.text = audio != null && audio.EffectsMuted ? GameText.L("Efektler: Kapalı", "Effects: Off", "Effekte: Aus") : GameText.L("Efektler: Açık", "Effects: On", "Effekte: An");
            }
            musicMute.onClick.AddListener(() =>
            {
                CozyAudioManager.Instance?.ToggleMusic();
                RefreshAudioButtons();
            });
            effectsMute.onClick.AddListener(() =>
            {
                CozyAudioManager.Instance?.ToggleEffects();
                RefreshAudioButtons();
            });
            RefreshAudioButtons();

            // Dil: her dil kendi adıyla; seçilince sahne yeniden kurulur (ilerleme korunur).
            SettingsSectionLabel(settingsContent, "LanguageSection", GameText.L("Dil", "Language", "Sprache"), 0.46f);
            var languageOptions = new (GameLanguage lang, string name, string objectName)[]
            {
                (GameLanguage.Turkish, "Türkçe", "TurkishLanguage"),
                (GameLanguage.English, "English", "EnglishLanguage"),
                (GameLanguage.German, "Deutsch", "GermanLanguage"),
            };
            for (int i = 0; i < languageOptions.Length; i++)
            {
                var option = languageOptions[i];
                bool current = GameText.Language == option.lang;
                var languageButton = Button(settingsContent, option.objectName,
                    GameText.Checked(option.name, current),
                    new Vector2(0.5f, 0.38f), new Vector2((i - 1) * 340f, 0f), Color.white);
                StylePillButton(languageButton, new Vector2(310f, 92f), current ? PlayColor : UtilityColor, null, FontButton, false);
                languageButton.onClick.AddListener(() => GameText.SetLanguage(option.lang));
            }

            var howToPlay = Button(settingsContent, "HowToPlayButton", GameText.L("Nasıl Oynanır?", "How to Play", "So wird gespielt"),
                new Vector2(0.5f, 0.08f), new Vector2(-340f, 0f), Color.white);
            StylePillButton(howToPlay, new Vector2(380f, 104f), WaterColor, CozyIconFactory.HappyCatFace(), FontButton, false);
            howToPlay.onClick.AddListener(() =>
            {
                settingsPanel.SetActive(false);
                HowToPlayUI.Open();
            });
            var closeSettings = Button(settingsContent, "CloseSettings", GameText.Close,
                new Vector2(0.5f, 0.08f), new Vector2(20f, 0f), Color.white);
            StylePillButton(closeSettings, new Vector2(280f, 104f), UtilityColor, null, FontButton, false);
            closeSettings.onClick.AddListener(() => settingsPanel.SetActive(false));
            var quitGame = Button(settingsContent, "QuitGame", GameText.L("Oyundan Çık", "Quit Game", "Spiel beenden"),
                new Vector2(0.5f, 0.08f), new Vector2(350f, 0f), Color.white);
            StylePillButton(quitGame, new Vector2(340f, 104f), HardColor, null, FontButton, false);
            quitGame.onClick.AddListener(() =>
            {
                SaveService.Persist();
                PlayerPrefs.Save();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            var privacyPanel = BuildPrivacyDialog(content);
            var privacyButton = Button(settingsContent,"PrivacyPolicyButton",GameText.L("Gizlilik", "Privacy", "Datenschutz"),
                new Vector2(.5f,.23f),Vector2.zero,Color.white);
            StylePillButton(privacyButton,new Vector2(410f,82f),UtilityColor,null,FontButton,false);
            privacyButton.onClick.AddListener(()=>{settingsPanel.SetActive(false);privacyPanel.transform.SetAsLastSibling();privacyPanel.SetActive(true);});
            privacyPanel.transform.Find("ClosePrivacyPolicy").GetComponent<Button>().onClick.AddListener(()=>{privacyPanel.SetActive(false);settingsPanel.SetActive(true);settingsPanel.transform.SetAsLastSibling();});
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            privacyButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(-245f,0f);
            var resetFirstPlay = Button(settingsContent, "ResetFirstPlay",
                GameText.L("İlk Oynayış Testi", "Test First Play", "Ersten Start testen"),
                new Vector2(0.5f, 0.23f), new Vector2(245f,0f), Color.white);
            StylePillButton(resetFirstPlay, new Vector2(410f, 82f), UtilityColor, null, FontButton, false);
            var resetConfirm = Panel("ResetFirstPlayConfirmation", settingsPanel.transform,
                Vector2.zero, Vector2.one, new Color(.10f, .07f, .05f, .96f));
            var resetMessage = Label(resetConfirm.transform, "ResetMessage",
                GameText.L("Kayıt, puanlar ve eşyalar silinecek.\nİlk açılıştan başlansın mı?",
                    "Progress, points and items will be erased.\nStart from the first launch?",
                    "Fortschritt, Punkte und Gegenstände werden gelöscht.\nBeim ersten Start beginnen?"),
                new Vector2(.5f, .58f), Vector2.zero);
            resetMessage.fontSize = 36;
            resetMessage.color = Color.white;
            resetMessage.alignment = TextAnchor.MiddleCenter;
            resetMessage.rectTransform.sizeDelta = new Vector2(1050f, 180f);
            var cancelReset = Button(resetConfirm.transform, "CancelFirstPlayReset",
                GameText.L("Vazgeç", "Cancel", "Abbrechen"), new Vector2(.5f, .34f), new Vector2(-220f, 0f), Color.white);
            StylePillButton(cancelReset, new Vector2(340f, 100f), UtilityColor, null, FontButton, false);
            cancelReset.onClick.AddListener(() => resetConfirm.SetActive(false));
            var confirmReset = Button(resetConfirm.transform, "ConfirmFirstPlayReset",
                GameText.L("Sıfırla ve Başlat", "Reset and Start", "Zurücksetzen und starten"),
                new Vector2(.5f, .34f), new Vector2(220f, 0f), Color.white);
            StylePillButton(confirmReset, new Vector2(410f, 100f), HardColor, null, FontButton, false);
            confirmReset.onClick.AddListener(() =>
            {
                confirmReset.interactable = false;
                SaveService.ResetFirstPlayForTesting();
                GameText.ReloadDefaultLanguageForTesting();
                GameplayMode.Select(GameplayPlayMode.Drawing);
                Time.timeScale = 1f;
                MobileControlState.Reset();
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            });
            resetFirstPlay.onClick.AddListener(() => resetConfirm.SetActive(true));
            resetConfirm.SetActive(false);
#endif
            settingsButton.onClick.AddListener(() =>
            {
                RefreshSizeLabels();
                RefreshAudioButtons();
                settingsPanel.transform.SetAsLastSibling();
                settingsPanel.SetActive(true);
            });
            settingsPanel.SetActive(false);

            var modeMenu = new GameObject("PlayModeMenu", typeof(RectTransform));
            modeMenu.transform.SetParent(content, false);
            Stretch(modeMenu.GetComponent<RectTransform>());
            var drawingPlay = Button(modeMenu.transform, "DrawingPlayButton", GameText.L("Çizerek Oyna", "Draw to Play", "Zeichnen & Spielen"), new Vector2(1f, 1f), new Vector2(-150f, -125f), new Color(0.93f, 0.72f, 0.76f));
            var directPlay = Button(modeMenu.transform, "DirectPlayButton", GameText.L("Tuşlarla Oyna", "Play with Buttons", "Mit Tasten spielen"), new Vector2(1f, 1f), new Vector2(-150f, -195f), new Color(0.72f, 0.82f, 0.94f));
            // Oyna'nın altındaki seçenekler de aynı hap stilinde ve ikonlu.
            StyleMenuPill(drawingPlay, 0, DrawModeColor, CozyIconFactory.Pencil());
            StyleMenuPill(directPlay, 1, WaterColor, CozyIconFactory.Gamepad());
            var automaticDifficulty = Button(modeMenu.transform, "AutomaticDifficulty", GameText.L("Otomatik", "Auto", "Automatisch"),
                new Vector2(.5f,.32f), new Vector2(-315f,0),Color.white);
            StylePillButton(automaticDifficulty,new Vector2(200f,96f),WaterColor,null,FontBody,false);
            var difficultyHint = Label(modeMenu.transform,"DifficultyHint",
                GameText.L("Her mekân: 1–10 kolay · 11–20 orta · 21+ zor", "Each setting: 1–10 easy · 11–20 medium · 21+ hard", "Je Umgebung: 1–10 leicht · 11–20 mittel · ab 21 schwer"),
                new Vector2(.5f,.23f),Vector2.zero);
            difficultyHint.fontSize=25;
            var easyDifficulty = Button(modeMenu.transform, "EasyDifficulty", GameText.L("Kolay", "Easy", "Leicht"),
                new Vector2(0.5f, 0.32f), new Vector2(-105f, 0f), new Color(0.52f, 0.70f, 0.55f));
            var mediumDifficulty = Button(modeMenu.transform, "MediumDifficulty", GameText.L("Orta", "Medium", "Mittel"),
                new Vector2(0.5f, 0.32f), new Vector2(105f,0f), new Color(0.83f, 0.68f, 0.52f));
            var hardDifficulty = Button(modeMenu.transform, "HardDifficulty", GameText.L("Zor", "Hard", "Schwer"),
                new Vector2(0.5f, 0.32f), new Vector2(315f, 0f), new Color(0.72f, 0.48f, 0.34f));
            StylePillButton(easyDifficulty, new Vector2(200f, 96f), PlayColor, null, FontBody, false);
            StylePillButton(mediumDifficulty, new Vector2(200f, 96f), FoodColor, null, FontBody, false);
            StylePillButton(hardDifficulty, new Vector2(200f, 96f), HardColor, null, FontBody, false);
            void RefreshDifficultyLabels()
            {
                var selectedDifficulty = GameDifficulty.Selected;
                bool automatic = GameDifficulty.IsAutomatic;
                difficultyHint.text = automatic
                    ? GameText.L("Her mekân: 1–10 kolay · 11–20 orta · 21+ zor", "Each setting: 1–10 easy · 11–20 medium · 21+ hard", "Je Umgebung: 1–10 leicht · 11–20 mittel · ab 21 schwer")
                    : GameText.L("Seçtiğin zorluk sabit kalır.", "Your chosen difficulty stays fixed.", "Die gewählte Schwierigkeit bleibt gleich.");
                automaticDifficulty.GetComponentInChildren<Text>().text=GameText.Checked(GameText.L("Otomatik", "Auto", "Automatisch"),automatic);
                easyDifficulty.GetComponentInChildren<Text>().text =
                    GameText.Checked(GameText.L("Kolay", "Easy", "Leicht"), !automatic && selectedDifficulty == DifficultyLevel.Easy);
                mediumDifficulty.GetComponentInChildren<Text>().text =
                    GameText.Checked(GameText.L("Orta", "Medium", "Mittel"), !automatic && selectedDifficulty == DifficultyLevel.Medium);
                hardDifficulty.GetComponentInChildren<Text>().text =
                    GameText.Checked(GameText.L("Zor", "Hard", "Schwer"), !automatic && selectedDifficulty == DifficultyLevel.Hard);
            }
            automaticDifficulty.onClick.AddListener(() => { GameDifficulty.SelectAutomatic(); RefreshDifficultyLabels(); });
            play.onClick.AddListener(RefreshDifficultyLabels);
            easyDifficulty.onClick.AddListener(() => { GameDifficulty.Select(DifficultyLevel.Easy); RefreshDifficultyLabels(); });
            mediumDifficulty.onClick.AddListener(() => { GameDifficulty.Select(DifficultyLevel.Medium); RefreshDifficultyLabels(); });
            hardDifficulty.onClick.AddListener(() => { GameDifficulty.Select(DifficultyLevel.Hard); RefreshDifficultyLabels(); });
            RefreshDifficultyLabels();
            modeMenu.SetActive(false);

            var themeSelectorObject = BuildThemeSelector(content);
            var themeSelector = themeSelectorObject.GetComponent<ThemeSelectionUI>();
            var brushes = BuildBrushToolbar(content);
            brushes.SetActive(false);
            var previousDrawView = RoundIconButton(content, "PreviousDrawView", CozyIconFactory.ArrowLeft(),
                WaterColor, new Vector2(0.5f, 1f), new Vector2(-145f, -165f), 96f, 0.52f);
            var startDrawingGame = RoundIconButton(content, "StartDrawingGame", CozyIconFactory.Play(),
                PlayColor, new Vector2(1f, 0f), Vector2.zero, 160f, 0.57f);
            ApplyDrawingStartLayout(startDrawingGame.GetComponent<RectTransform>(), DisplaySizeSettings.Large);
            var nextDrawView = RoundIconButton(content, "NextDrawView", CozyIconFactory.ArrowRight(),
                WaterColor, new Vector2(0.5f, 1f), new Vector2(145f, -165f), 96f, 0.52f);
            previousDrawView.gameObject.AddComponent<DrawingToolButton>();
            nextDrawView.gameObject.AddComponent<DrawingToolButton>();
            previousDrawView.gameObject.SetActive(false);
            startDrawingGame.gameObject.SetActive(false);
            nextDrawView.gameObject.SetActive(false);
            var tutorial = BuildBrushTutorial(content);
            tutorial.SetActive(false);
            var controls = BuildMobileControls(content);
            controls.SetActive(false);
            BuildPauseMenu(hud, content);
            hud.GetComponent<HudView>().Bind(love, level, ink, selected, play, shop, directPlay,
                null, null, careUi, brushes, tutorial, controls, modeMenu, drawingPlay, themeSelector,
                editHome, flipFurniture, settingsButton, settingsPanel,
                previousDrawView, nextDrawView, startDrawingGame, null);
            hud.GetComponent<HudView>().BindBalanceRoot(balanceRoot);
            BuildMitziPhone(hud, content, canvas);
            return hud;
        }

        static void BuildMitziPhone(GameObject hud, Transform content, Transform canvas)
        {
            var launcher = RoundIconButton(content, "MitziPhone", CozyIconFactory.MitziPhone(), new Color(.96f,.85f,.68f),
                new Vector2(0,1), new Vector2(EdgeMargin + 498, -(EdgeMargin + 44)), 88, 1.05f);
            launcher.transform.Find("Icon").localRotation=Quaternion.Euler(0,0,-9);
            var launcherName=Label(launcher.transform,"PhoneOwner","Mitzi",new Vector2(.5f,.5f),new Vector2(0,-54));
            launcherName.rectTransform.sizeDelta=new Vector2(94,26);launcherName.fontSize=FontSmall;launcherName.fontStyle=FontStyle.Bold;
            launcherName.color=new Color(1,.97f,.87f);launcherName.raycastTarget=false;AddTextShadow(launcherName);
            var overlay = Panel("MitziPhoneOverlay", canvas, Vector2.zero, Vector2.one, new Color(.10f,.07f,.05f,.70f));
            var safe = SafeContent(overlay.transform);
            var device = new GameObject("PhoneDevice", typeof(RectTransform), typeof(Image)); device.transform.SetParent(safe,false);
            device.GetComponent<RectTransform>().sizeDelta = new Vector2(500,860);
            var frame = device.GetComponent<Image>(); frame.sprite = CozyIconFactory.Pill(); frame.type = Image.Type.Sliced; frame.color = new Color(.30f,.20f,.15f);
            var screen = new GameObject("PhoneScreen", typeof(RectTransform), typeof(Image)); screen.transform.SetParent(device.transform,false);
            screen.GetComponent<RectTransform>().sizeDelta = new Vector2(464,816);
            var screenImage = screen.GetComponent<Image>(); screenImage.sprite = CozyIconFactory.Pill(); screenImage.type = Image.Type.Sliced; screenImage.color = new Color(1f,.97f,.90f);
            var notch = IconImage(screen.transform,"Speaker",CozyIconFactory.Pill(),new Color(.30f,.20f,.15f),new Vector2(.5f,.5f),new Vector2(0,382),10);
            notch.rectTransform.sizeDelta = new Vector2(64,9); notch.raycastTarget = false;
            var heading = Label(screen.transform,"PhoneTitle",GameText.L("Mitzi’nin telefonu","Mitzi’s phone","Mitzis Handy"),new Vector2(.5f,.5f),new Vector2(0,335));
            heading.rectTransform.sizeDelta = new Vector2(380,52); heading.fontSize = FontButton; heading.fontStyle = FontStyle.Bold;
            var avatarBack = IconImage(screen.transform,"AvatarCircle",CozyIconFactory.Dot(),new Color(.91f,.48f,.58f),new Vector2(.5f,.5f),new Vector2(-145,245),100);
            avatarBack.raycastTarget = false;
            avatarBack.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var mitzi = GameFlow.Instance != null ? GameFlow.Instance.Catalog?.GetCat("mitzi") : null;
            var avatar = IconImage(avatarBack.transform,"MitziAvatar",mitzi != null && mitzi.portrait != null ? mitzi.portrait : CozyIconFactory.HappyCatFace(),Color.white,new Vector2(.5f,.5f),Vector2.zero,92);
            avatar.preserveAspect = true; avatar.raycastTarget = false;
            var name = Label(screen.transform,"AccountName",MitziPhoneUI.AccountName,new Vector2(.5f,.5f),new Vector2(45,265));
            name.rectTransform.sizeDelta = new Vector2(280,44); name.fontSize = FontBody; name.fontStyle = FontStyle.Bold;
            var handle = Label(screen.transform,"AccountHandle",MitziPhoneUI.AccountHandle,new Vector2(.5f,.5f),new Vector2(45,220));
            handle.rectTransform.sizeDelta = new Vector2(280,44); handle.fontSize = FontBody;
            var description = Label(screen.transform,"AccountDescription",GameText.L("Mitzi’nin albümü","Mitzi’s album","Mitzis Album"),new Vector2(.5f,.5f),new Vector2(0,165));
            description.rectTransform.sizeDelta = new Vector2(360,42); description.fontSize = FontBody;
            var photos=new Image[4];
            for(int i=0;i<4;i++)
            {
                photos[i]=IconImage(screen.transform,"Photo_"+i,null,new Color(.93f,.88f,.80f),new Vector2(.5f,.5f),
                    new Vector2((i%2==0?-1:1)*95,50-(i/2)*190),180);
                photos[i].raycastTarget=false;photos[i].preserveAspect=true;
            }
            var open = Button(screen.transform,"OpenMitziInstagram",GameText.L("Instagram’da aç","Open in Instagram","In Instagram öffnen"),new Vector2(.5f,.5f),new Vector2(0,-295),LoveColor);
            StylePillButton(open,new Vector2(390,84),LoveColor,CozyIconFactory.Camera(),FontBody,false);
            var close = RoundIconButton(device.transform,"ClosePhone",CozyIconFactory.Close(),UtilityColor,new Vector2(.5f,.5f),new Vector2(220,400),64,.5f);
            var homeBar = IconImage(screen.transform,"HomeBar",CozyIconFactory.Pill(),new Color(.30f,.20f,.15f,.45f),new Vector2(.5f,.5f),new Vector2(0,-382),8);
            homeBar.rectTransform.sizeDelta = new Vector2(90,8); homeBar.raycastTarget = false;
            var phone=hud.AddComponent<MitziPhoneUI>();phone.BindAlbum(avatar,photos);phone.Bind(overlay,launcher,close,open);
            BuildPhoneParentGate(phone,screen.transform);
        }

        static void BuildPhoneParentGate(MitziPhoneUI phone, Transform screen)
        {
            var gate=Panel("PhoneParentGate",screen,Vector2.zero,Vector2.one,new Color(1,.97f,.90f));
            var title=Label(gate.transform,"ParentTitle",GameText.L("Yetişkinler için","For grown-ups","Für Erwachsene"),new Vector2(.5f,.5f),new Vector2(0,270));
            title.rectTransform.sizeDelta=new Vector2(390,70);title.fontSize=FontButton;title.fontStyle=FontStyle.Bold;
            var instruction=Label(gate.transform,"ParentInstruction",GameText.L("Instagram’a geçmeden önce\nsonucu yazın.","Enter the answer before\nopening Instagram.","Vor dem Öffnen von Instagram\ndas Ergebnis eingeben."),new Vector2(.5f,.5f),new Vector2(0,175));
            instruction.rectTransform.sizeDelta=new Vector2(380,105);instruction.fontSize=FontBody;
            var question=Label(gate.transform,"ParentChallenge","",new Vector2(.5f,.5f),new Vector2(0,60));
            question.rectTransform.sizeDelta=new Vector2(370,75);question.fontSize=FontTitle;question.fontStyle=FontStyle.Bold;
            var inputRoot=new GameObject("ParentAnswer",typeof(RectTransform),typeof(Image),typeof(InputField));inputRoot.transform.SetParent(gate.transform,false);
            var inputRect=inputRoot.GetComponent<RectTransform>();inputRect.sizeDelta=new Vector2(280,75);inputRect.anchoredPosition=new Vector2(0,-40);
            var background=inputRoot.GetComponent<Image>();background.sprite=CozyIconFactory.Pill();background.type=Image.Type.Sliced;background.color=new Color(.95f,.88f,.76f);
            var inputText=Label(inputRoot.transform,"AnswerText","",new Vector2(.5f,.5f),Vector2.zero);inputText.rectTransform.sizeDelta=new Vector2(240,65);inputText.fontSize=FontButton;inputText.supportRichText=false;
            var input=inputRoot.GetComponent<InputField>();input.textComponent=inputText;input.contentType=InputField.ContentType.IntegerNumber;input.characterLimit=4;input.lineType=InputField.LineType.SingleLine;
            var status=Label(gate.transform,"ParentStatus","",new Vector2(.5f,.5f),new Vector2(0,-135));status.rectTransform.sizeDelta=new Vector2(380,95);status.fontSize=FontBody;
            var verify=Button(gate.transform,"VerifyParent",GameText.L("Doğrula","Verify","Bestätigen"),new Vector2(.5f,.5f),new Vector2(0,-240),PlayColor);
            StylePillButton(verify,new Vector2(370,84),PlayColor,null,FontBody,false);
            var cancel=Button(gate.transform,"CancelParent",GameText.L("Albümüne dön","Back to album","Zurück zum Album"),new Vector2(.5f,.5f),new Vector2(0,-335),UtilityColor);
            StylePillButton(cancel,new Vector2(370,70),UtilityColor,null,FontBody,false);
            phone.BindParentGate(gate,question,input,status,verify,cancel,verify.GetComponentInChildren<Text>());
        }

        /// <summary>
        /// "Nasıl Oynanır" ekranı: krem kart, büyük ikonlu çizim alanı, başlık,
        /// ebeveyn için kısa açıklama, sayfa noktaları ve ileri/geri/başla/atla düğmeleri.
        /// </summary>
        static void BuildHowToPlay(Transform canvas)
        {
            var holder = new GameObject("HowToPlay", typeof(RectTransform));
            holder.transform.SetParent(canvas, false);
            Stretch(holder.GetComponent<RectTransform>());

            var panel = Panel("HowToPlayPanel", holder.transform, Vector2.zero, Vector2.one,
                new Color(0.10f, 0.07f, 0.05f, 0.82f));
            var content = SafeContent(panel.transform);

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(content, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(1180f, 720f);
            var cardImage = card.GetComponent<Image>();
            cardImage.sprite = CozyIconFactory.Pill();
            cardImage.type = Image.Type.Sliced;
            cardImage.pixelsPerUnitMultiplier = 0.5f;
            cardImage.color = new Color(1f, 0.97f, 0.90f, 0.98f);
            var cardShadow = card.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.30f);
            cardShadow.effectDistance = new Vector2(0f, -10f);

            var title = Label(card.transform, "Title", "", new Vector2(0.5f, 0.5f), new Vector2(0f, 270f));
            title.rectTransform.sizeDelta = new Vector2(900f, 80f);
            title.fontSize = FontTitle;
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.36f, 0.22f, 0.14f);

            var art = new GameObject("Art", typeof(RectTransform));
            art.transform.SetParent(card.transform, false);
            var artRt = art.GetComponent<RectTransform>();
            artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.anchoredPosition = new Vector2(0f, 60f);
            artRt.sizeDelta = new Vector2(1000f, 240f);

            var body = Label(card.transform, "Body", "", new Vector2(0.5f, 0.5f), new Vector2(0f, -130f));
            body.rectTransform.sizeDelta = new Vector2(980f, 110f);
            body.fontSize = FontButton;
            body.color = new Color(0.30f, 0.22f, 0.17f);

            int pageCount = HowToPlayUI.PageCount;
            var dots = new Image[pageCount];
            for (int i = 0; i < pageCount; i++)
            {
                dots[i] = IconImage(card.transform, "Dot_" + i, CozyIconFactory.CircleButton(), Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2((i - (pageCount - 1) * 0.5f) * 40f, -250f), 24f);
                dots[i].raycastTarget = false;
            }

            var previous = RoundIconButton(card.transform, "Previous", CozyIconFactory.ArrowLeft(), UtilityColor,
                new Vector2(0.5f, 0.5f), new Vector2(-470f, -250f), 112f, 0.5f);
            var next = RoundIconButton(card.transform, "Next", CozyIconFactory.ArrowRight(), PlayColor,
                new Vector2(0.5f, 0.5f), new Vector2(470f, -250f), 124f, 0.5f);
            var nextPulse = next.gameObject.AddComponent<UiPulse>();
            nextPulse.Configure(0.05f, 3.2f);
            var start = Button(card.transform, "StartPlaying", GameText.L("Başla!", "Start!", "Los!"), new Vector2(0.5f, 0.5f),
                new Vector2(400f, -250f), Color.white);
            StylePillButton(start, new Vector2(300f, 112f), PlayColor, CozyIconFactory.Play(), FontTitle, true);
            var skip = Button(card.transform, "Skip", GameText.L("Atla", "Skip", "Überspringen"), new Vector2(0.5f, 0.5f),
                new Vector2(470f, 290f), Color.white);
            StylePillButton(skip, new Vector2(160f, 72f), UtilityColor, null, FontBody, false);

            holder.AddComponent<HowToPlayUI>().Bind(panel, title, body, artRt, dots, previous, next, start, skip);
        }

        static GameObject BuildThemeSelector(Transform parent)
        {
            var panel = new GameObject("ThemeSelector", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>());
            var street = Button(panel.transform, "StreetTheme", GameText.L("Sokak", "Street", "Straße"), new Vector2(1f, 1f), new Vector2(-150f, -125f), new Color(0.83f, 0.68f, 0.52f));
            var forest = Button(panel.transform, "ForestTheme", GameText.L("Orman", "Forest", "Wald"), new Vector2(1f, 1f), new Vector2(-150f, -195f), new Color(0.52f, 0.70f, 0.55f));
            var city = Button(panel.transform, "CityTheme", GameText.L("Cadde", "City", "Stadt"), new Vector2(1f, 1f), new Vector2(-150f, -255f), new Color(0.62f, 0.58f, 0.54f));
            var cyber = Button(panel.transform, "CyberTheme", GameText.L("Neo Teknoloji", "Neo Tech", "Neo-Tech"), new Vector2(1f, 1f), new Vector2(-150f, -315f), new Color(0.35f, 0.28f, 0.55f));
            StyleMenuPill(street, 0, StreetColor, CozyIconFactory.House());
            StyleMenuPill(forest, 1, ForestColor, CozyIconFactory.Tree());
            StyleMenuPill(city, 2, CityColor, CozyIconFactory.Buildings());
            StyleMenuPill(cyber, 3, CyberColor, CozyIconFactory.Bolt());
            panel.AddComponent<ThemeSelectionUI>().Bind(street, forest, city, cyber);
            return panel;
        }

        static GameObject BuildPrivacyDialog(Transform parent)
        {
            var panel=Panel("PrivacyPolicyPanel",parent,Vector2.zero,Vector2.one,new Color(.98f,.96f,.90f,1f));
            var title=Label(panel.transform,"PrivacyTitle",GameText.L("Gizlilik Politikası", "Privacy Policy", "Datenschutzerklärung"),new Vector2(.5f,.89f),Vector2.zero);
            title.fontSize=44;title.fontStyle=FontStyle.Bold;
            var viewport=new GameObject("PolicyViewport",typeof(RectTransform),typeof(Image),typeof(Mask));viewport.transform.SetParent(panel.transform,false);
            var rect=viewport.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.08f,.20f);rect.anchorMax=new Vector2(.92f,.80f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            viewport.GetComponent<Image>().color=Color.white;viewport.GetComponent<Mask>().showMaskGraphic=true;
            var textObject=new GameObject("PolicyText",typeof(RectTransform),typeof(Text),typeof(ContentSizeFitter));textObject.transform.SetParent(viewport.transform,false);
            var textRect=textObject.GetComponent<RectTransform>();textRect.anchorMin=new Vector2(0,1);textRect.anchorMax=Vector2.one;textRect.pivot=new Vector2(.5f,1);textRect.anchoredPosition=Vector2.zero;textRect.sizeDelta=new Vector2(-48,0);
            var text=textObject.GetComponent<Text>();text.font=UiFont();text.fontSize=28;text.lineSpacing=1.15f;text.alignment=TextAnchor.UpperLeft;text.color=new Color(.16f,.22f,.22f);text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
            text.text=Resources.Load<TextAsset>("PrivacyPolicy")?.text ?? "Melisa Uyar — melisauyar5225@gmail.com";
            textObject.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=panel.AddComponent<ScrollRect>();scroll.viewport=rect;scroll.content=textRect;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            var close=Button(panel.transform,"ClosePrivacyPolicy",GameText.Close,new Vector2(.5f,.09f),Vector2.zero,Color.white);
            StylePillButton(close,new Vector2(300f,100f),UtilityColor,null,FontButton,false);
            close.GetComponent<RectTransform>().anchoredPosition=new Vector2(-220f,0);
            var web=Button(panel.transform,"OpenPrivacyPolicyWebsite",GameText.L("Web'de aç", "Open website", "Webseite öffnen"),new Vector2(.5f,.09f),new Vector2(180f,0),Color.white);
            StylePillButton(web,new Vector2(400f,100f),UtilityColor,null,FontButton,false);
            string policyUrl=ReleaseConfiguration.Current?.privacyPolicyUrl;
            web.interactable=System.Uri.TryCreate(policyUrl,System.UriKind.Absolute,out var policyUri)&&policyUri.Scheme=="https";
            web.onClick.AddListener(()=>Application.OpenURL(policyUrl));
            panel.SetActive(false);return panel;
        }

        static GameObject BuildMobileControls(Transform parent)
        {
            var controls = new GameObject("MobileControls", typeof(RectTransform));
            controls.transform.SetParent(parent, false);
            Stretch(controls.GetComponent<RectTransform>());
            MobileJoystick.Create(controls.transform,"MoveJoystick",JoystickAxis.Horizontal,new Vector2(0f,0f));
            MobileJoystick.Create(controls.transform,"ActionJoystick",JoystickAxis.Vertical,new Vector2(1f,0f));
            return controls;
        }

        /// <summary>
        /// Yuvarlak, yarı saydam kontrol düğmeleri. Zıpla en çok kullanılan tuş olduğu
        /// için başparmağın doğal durduğu sağ alt köşede ve biraz daha büyük.
        /// </summary>
        static void ApplyControlLayout(RectTransform rt, string name, bool large)
        {
            if (rt == null)
                return;
            float d = large ? 190f : 160f;
            float jump = large ? 210f : 180f;
            const float gap = 30f;
            float size = d;
            Vector2 position;
            switch (name)
            {
                case "Left": position = new Vector2(EdgeMargin + d * 0.5f, EdgeMargin + d * 0.5f); break;
                case "Right": position = new Vector2(EdgeMargin + d * 1.5f + gap, EdgeMargin + d * 0.5f); break;
                case "Jump":
                    size = jump;
                    position = new Vector2(-(EdgeMargin + jump * 0.5f), EdgeMargin + jump * 0.5f);
                    break;
                case "Crouch": position = new Vector2(-(EdgeMargin + jump + gap + d * 0.5f), EdgeMargin + d * 0.5f); break;
                default: return;
            }
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(size, size);
        }

        static void ApplyDrawingStartLayout(RectTransform rect, bool large)
        {
            if (rect == null) return;
            // Same safe-area parent and edge clearance as the right gameplay stick.
            // Keep this action in the bottom control strip, outside the central road.
            float diameter = large ? 176f : 160f;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.sizeDelta = Vector2.one * diameter;
            rect.anchoredPosition = new Vector2(-(52f + diameter * .5f), 44f + diameter * .5f);
            var icon = rect.Find("Icon") as RectTransform;
            if (icon != null)
            {
                icon.anchorMin = Vector2.one * .215f;
                icon.anchorMax = Vector2.one * .785f;
                icon.offsetMin = icon.offsetMax = Vector2.zero;
            }
        }

        static readonly Color ControlColor = new Color(0.24f, 0.18f, 0.14f, 0.50f);

        static void AddControlButton(Transform parent, string name, Sprite icon, Vector2 anchor, MobileAction action)
        {
            var button = RoundIconButton(parent, name, icon, ControlColor, anchor, Vector2.zero, 160f, 0.50f);
            ApplyControlLayout(button.GetComponent<RectTransform>(), name, DisplaySizeSettings.Large);
            // İkon düğme boyutuyla birlikte ölçeklensin.
            var iconRt = button.transform.Find("Icon") as RectTransform;
            if (iconRt != null)
            {
                iconRt.anchorMin = new Vector2(0.25f, 0.25f);
                iconRt.anchorMax = new Vector2(0.75f, 0.75f);
                iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
            }
            var shadow = button.GetComponent<Shadow>();
            if (shadow != null)
                shadow.effectColor = new Color(0f, 0f, 0f, 0.15f);
            button.gameObject.AddComponent<MobileControlButton>().Configure(action);
        }

        /// <summary>
        /// Bölüm içi tek duraklat düğmesi + menüsü (devam, yeniden başla, eve dön).
        /// Eski "Eve Dön", "Yeniden" ve "Oyundan Çık" köşe düğmelerinin yerini alır.
        /// </summary>
        static void BuildPauseMenu(GameObject hud, Transform content)
        {
            var pauseButton = RoundIconButton(content, "PauseButton", CozyIconFactory.Pause(), UtilityColor,
                new Vector2(1f, 1f), new Vector2(-(EdgeMargin + 52f), -(EdgeMargin + 52f)), 104f, 0.50f);
            pauseButton.gameObject.SetActive(false);

            var panel = Panel("PauseMenu", content, Vector2.zero, Vector2.one, new Color(0.10f, 0.07f, 0.05f, 0.72f));
            var restart = RoundIconButton(panel.transform, "PauseRestart", CozyIconFactory.Restart(), WaterColor,
                new Vector2(0.5f, 0.5f), new Vector2(-280f, 0f), 170f, 0.52f);
            var resume = RoundIconButton(panel.transform, "PauseResume", CozyIconFactory.Play(), PlayColor,
                new Vector2(0.5f, 0.5f), Vector2.zero, 230f, 0.46f);
            var home = RoundIconButton(panel.transform, "PauseHome", CozyIconFactory.House(), UtilityColor,
                new Vector2(0.5f, 0.5f), new Vector2(280f, 0f), 170f, 0.52f);
            // Devam en büyük ve nabızlı: çocuk menüyü açınca en kolay yol oyuna geri dönmek.
            var resumePulse = resume.gameObject.AddComponent<UiPulse>();
            resumePulse.Configure(0.05f, 3.2f);
            // Play üçgeni görsel olarak sağa ağır; ortalamak için hafif sola kaydır.
            var resumeIcon = resume.transform.Find("Icon") as RectTransform;
            if (resumeIcon != null)
                resumeIcon.anchoredPosition = new Vector2(-6f, 2f);
            panel.SetActive(false);

            hud.AddComponent<PauseMenu>().Bind(pauseButton, panel, resume, restart, home);
        }

        static CatNeedsUI BuildCareUi(Transform canvas, CatNeedsSystem needs)
        {
            var panel = Panel("CarePanel", canvas, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
            panel.AddComponent<CanvasGroup>();
            var content = SafeContent(panel.transform);

            // ── DUYGU BALONU: seçili kedinin başının üstünde, yazısız ─────────
            var bubbleGo = new GameObject("CatMoodBubble", typeof(RectTransform), typeof(Image));
            bubbleGo.transform.SetParent(content, false);
            var bubbleRt = bubbleGo.GetComponent<RectTransform>();
            bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRt.pivot = new Vector2(0.5f, 0f);
            bubbleRt.sizeDelta = new Vector2(124f, 136f);
            var bubbleImage = bubbleGo.GetComponent<Image>();
            bubbleImage.sprite = CozyIconFactory.SpeechBubble();
            bubbleImage.raycastTarget = false;
            var bubbleShadow = bubbleGo.AddComponent<Shadow>();
            bubbleShadow.effectColor = new Color(0.25f, 0.15f, 0.08f, 0.30f);
            bubbleShadow.effectDistance = new Vector2(0f, -5f);
            var bubbleIcon = IconImage(bubbleGo.transform, "MoodIcon", CozyIconFactory.HappyCatFace(), CatFaceColor,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), 74f);
            bubbleIcon.raycastTarget = false;
            bubbleGo.AddComponent<CatMoodBubble>().Bind(content as RectTransform, bubbleIcon, needs,
                FoodColor, WaterColor, LoveColor, SleepColor, CatFaceColor);

            // ── ALT ŞERİT: ortalanmış yuvarlak bakım düğmeleri (mama, su, uyku) ──
            // Çocuk parmağı için geniş dokunma alanı (tipik telefonda ≈60 pt) ve aralarında boşluk.
            const float careDiameter = 150f;
            const float careSpacing = 195f;
            float careY = EdgeMargin + careDiameter * 0.5f;
            var feed = RoundIconButton(content, "FeedButton", CozyIconFactory.FoodBowl(), FoodColor,
                new Vector2(0.5f, 0f), new Vector2(-careSpacing, careY), careDiameter, 0.58f);
            var water = RoundIconButton(content, "WaterButton", CozyIconFactory.WaterDrop(), WaterColor,
                new Vector2(0.5f, 0f), new Vector2(0f, careY), careDiameter, 0.56f);
            var sleep = RoundIconButton(content, "SleepButton", CozyIconFactory.Moon(), SleepColor,
                new Vector2(0.5f, 0f), new Vector2(careSpacing, careY), careDiameter, 0.56f);
            foreach (var careButton in new[] { feed, water, sleep })
            {
                var pulse = careButton.gameObject.AddComponent<UiPulse>();
                pulse.Configure(0.08f, 4.2f);
                pulse.Pulsing = false;
                careButton.gameObject.AddComponent<UiShake>();
            }

            // ── DURUM KARTI (sol üst, bakiyenin altında) ─────────────────
            // Yazı ve sayı yok: kaplar dolup boşalır, günlük okşama patilerle dolar.
            var card = new GameObject("NeedsCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(content, false);
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0f, 1f);
            cardRt.pivot = new Vector2(0f, 1f);
            cardRt.anchoredPosition = new Vector2(EdgeMargin, -(EdgeMargin + 88f + 16f));
            cardRt.sizeDelta = new Vector2(330f, 184f);
            var cardBg = card.GetComponent<Image>();
            cardBg.sprite = CozyIconFactory.Pill();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(1f, 0.97f, 0.90f, 0.82f);
            cardBg.raycastTarget = false;

            var foodGauge = NeedGaugeView(card.transform, "FoodGauge", CozyIconFactory.FoodBowl(), FoodColor, new Vector2(60f, -64f));
            var waterGauge = NeedGaugeView(card.transform, "WaterGauge", CozyIconFactory.WaterDrop(), WaterColor, new Vector2(165f, -64f));
            var loveGauge = NeedGaugeView(card.transform, "LoveGauge", CozyIconFactory.Heart(), LoveColor, new Vector2(270f, -64f));

            var paws = new Image[5];
            for (int i = 0; i < paws.Length; i++)
            {
                paws[i] = IconImage(card.transform, $"PettingPaw_{i}", CozyIconFactory.Paw(), Color.white,
                    new Vector2(0f, 1f), new Vector2(165f + (i - 2) * 50f, -148f), 40f);
                paws[i].raycastTarget = false;
            }

            // Ebeveyn için kısa, kendiliğinden kaybolan bilgi (ör. "önce mama kabı al").
            var toast = Label(content, "CareToast", "", new Vector2(0.5f, 0f), new Vector2(0f, careY + careDiameter * 0.5f + 50f));
            toast.rectTransform.sizeDelta = new Vector2(900f, 60f);
            toast.fontSize = FontBody;
            toast.fontStyle = FontStyle.Bold;
            toast.color = Color.white;
            var toastOutline = toast.gameObject.AddComponent<Outline>();
            toastOutline.effectColor = new Color(0.12f, 0.07f, 0.04f, 0.85f);
            toastOutline.effectDistance = new Vector2(2f, -2f);

            // ── İSTEĞE BAĞLI ÖDÜLLÜ REKLAM: sağ alt köşe, yalnız kedi evinde ──
            // Çocuk kitlesi: yalnız oyuncu isteyince açılır, günde sınırlı (RewardedAdButton).
            var rewardedAd = Button(content, "RewardedAdButton", "", new Vector2(1f, 0f),
                new Vector2(-(EdgeMargin + 230f), careY), Color.white);
            StylePillButton(rewardedAd, new Vector2(440f, 104f), LoveColor, CozyIconFactory.Play(), FontBody, false);
            rewardedAd.gameObject.AddComponent<RewardedAdButton>();

            var ui = panel.AddComponent<CatNeedsUI>();
            var homeStick = MobileJoystick.Create(content, "HomeJoystick", JoystickAxis.Horizontal, new Vector2(0,0), true);
            var homeName = Label(content, "ControlledCatName", "", new Vector2(0,0), new Vector2(170,330));
            homeName.fontSize = 28;
            homeName.fontStyle = FontStyle.Bold;
            var homeNameOutline = homeName.gameObject.AddComponent<Outline>();
            homeNameOutline.effectColor = new Color(1f,.97f,.90f,1f);
            homeNameOutline.effectDistance = new Vector2(2,2);
            homeName.rectTransform.sizeDelta = new Vector2(300,50);
            homeName.raycastTarget = false;
            var nearbyAction = Button(content, "NearbyCareAction", "", new Vector2(1,0), new Vector2(-245,300), FoodColor);
            StylePillButton(nearbyAction, new Vector2(300,92), FoodColor, null, FontBody, false);
            content.gameObject.AddComponent<HomeManualControls>().Bind(homeStick, nearbyAction, homeName);
            ui.Bind(needs, feed, water, sleep, foodGauge, waterGauge, loveGauge, paws, toast);
            return ui;
        }

        static NeedGauge NeedGaugeView(Transform parent, string name, Sprite icon, Color color, Vector2 position)
        {
            const float size = 92f;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(size, size);

            // Arkada koyu, yarı saydam "boş kap"; önde değer kadar dolan renkli ikon.
            var back = IconImage(go.transform, "Empty", icon, new Color(0.25f, 0.18f, 0.13f, 0.30f),
                new Vector2(0.5f, 0.5f), Vector2.zero, size);
            back.raycastTarget = false;
            var fill = IconImage(go.transform, "Fill", icon, color, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 1f;

            var shake = go.AddComponent<UiShake>();
            var gauge = go.AddComponent<NeedGauge>();
            gauge.Bind(fill, color, shake);
            return gauge;
        }

        static GameObject BuildBrushToolbar(Transform parent)
        {
            var toolbar = new GameObject("BrushToolbar", typeof(RectTransform));
            toolbar.transform.SetParent(parent, false);
            var rt = toolbar.GetComponent<RectTransform>();
            Stretch(rt);

            AddBrushButton(toolbar.transform, "NormalBrush", CozyIconFactory.Pencil(), -198f, UtilityColor, PathSurfaceType.Normal);
            AddBrushButton(toolbar.transform, "BounceBrush", CozyIconFactory.ArrowUp(), -66f, WaterColor, PathSurfaceType.Bounce);
            AddBrushButton(toolbar.transform, "IceBrush", CozyIconFactory.Snowflake(), 66f, new Color(0.40f, 0.70f, 0.75f), PathSurfaceType.Ice);
            var eraser = RoundIconButton(toolbar.transform, "Eraser", CozyIconFactory.Eraser(), LoveColor,
                new Vector2(0.5f, 0f), new Vector2(198f, 80f), 104f, 0.56f);
            AddDrawingToolSelection(eraser, PathSurfaceType.Normal, true);
            eraser.onClick.AddListener(() =>
            {
                if (LineDraw.Instance != null)
                    LineDraw.Instance.SetEraser();
            });
            return toolbar;
        }

        static void AddBrushButton(Transform parent, string name, Sprite icon, float x, Color color, PathSurfaceType type)
        {
            var button = RoundIconButton(parent, name, icon, color,
                new Vector2(0.5f, 0f), new Vector2(x, 80f), 104f, 0.56f);
            AddDrawingToolSelection(button, type, false);
            button.onClick.AddListener(() =>
            {
                if (LineDraw.Instance != null)
                    LineDraw.Instance.SetBrushType(type);
            });
        }

        static void AddDrawingToolSelection(Button button, PathSurfaceType type, bool eraser)
        {
            var ring = IconImage(button.transform, "SelectionRing", CozyIconFactory.Ring(),
                new Color(1f, 0.93f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, 134f);
            ring.raycastTarget = false;
            button.gameObject.AddComponent<DrawingToolButton>().Bind(type, eraser, ring.gameObject);
        }

        static GameObject BuildBrushTutorial(Transform parent)
        {
            var panel = new GameObject("BrushTutorial", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.AddComponent<GameplayTutorialUI>().Build();
            return panel;
        }

        static GameObject BuildRescue(Transform canvas)
        {
            var panel = Panel("Rescue", canvas, Vector2.zero, Vector2.zero, new Color(0.98f, 0.94f, 0.90f, 0.96f));
            var content = SafeContent(panel.transform);
            var title = Label(content, "Title", "", new Vector2(0.5f, 0.62f), Vector2.zero);
            title.fontSize = 42;
            var body = Label(content, "Body", "", new Vector2(0.5f, 0.48f), Vector2.zero);
            body.rectTransform.sizeDelta = new Vector2(780, 320);
            var invite = Button(content, "Invite", GameText.InviteHome, new Vector2(0.5f, 0.22f), Vector2.zero, Color.white);
            // Ana aksiyon: kalp ikonlu, nabız atan hap düğme.
            StylePillButton(invite, new Vector2(440f, 104f), LoveColor, CozyIconFactory.Heart(), FontButton, true);
            var screen = panel.AddComponent<RescueScreen>();
            screen.Bind(title, body, invite);
            return panel;
        }

        static GameObject BuildLevelComplete(Transform canvas, PawPathCatalog catalog)
        {
            var panel = Panel("LevelComplete", canvas, Vector2.zero, Vector2.one,
                catalog != null && catalog.completionBackground != null
                    ? Color.black
                    : new Color(0.98f, 0.93f, 0.84f, 0.68f));
            if (catalog != null && catalog.completionBackground != null)
            {
                var backgroundGo = new GameObject("CompletionBackground", typeof(RectTransform),
                    typeof(Image), typeof(AspectRatioFitter));
                backgroundGo.transform.SetParent(panel.transform, false);
                var backgroundRt = backgroundGo.GetComponent<RectTransform>();
                backgroundRt.anchorMin = backgroundRt.anchorMax = new Vector2(0.5f, 0.5f);
                backgroundRt.anchoredPosition = Vector2.zero;
                var background = backgroundGo.GetComponent<Image>();
                background.sprite = catalog.completionBackground;
                background.color = Color.white;
                background.raycastTarget = false;
                var fitter = backgroundGo.GetComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = catalog.completionBackground.rect.width /
                    catalog.completionBackground.rect.height;
            }
            var content = SafeContent(panel.transform);
            var success = Label(content, "Success", GameText.L("BAŞARDIN!", "YOU DID IT!", "GESCHAFFT!"), new Vector2(0.5f, 0.82f), Vector2.zero);
            success.fontSize = 54;
            success.fontStyle = FontStyle.Bold;
            success.color = new Color(0.54f, 0.30f, 0.22f);

            var title = Label(content, "CompletedLevel", GameText.LevelCompleted, new Vector2(0.5f, 0.72f), Vector2.zero);
            title.fontSize = 36;
            var faceGo = new GameObject("MitziCelebration", typeof(RectTransform), typeof(Image));
            faceGo.transform.SetParent(content, false);
            var faceRt = faceGo.GetComponent<RectTransform>();
            faceRt.anchorMin = faceRt.anchorMax = new Vector2(0.5f, 0.51f);
            faceRt.sizeDelta = new Vector2(380f, 310f);
            faceRt.anchoredPosition = Vector2.zero;
            var faceImage = faceGo.GetComponent<Image>();
            faceImage.preserveAspect = true;
            faceImage.raycastTarget = false;

            var reward = Label(content, "Reward", GameText.L("+10 Sevgi", "+10 Love", "+10 Liebe"), new Vector2(0.5f, 0.31f), Vector2.zero);
            reward.fontSize = 30;
            reward.color = new Color(0.78f, 0.20f, 0.38f);

            // Sonuç düğmeleri yan yana ve ikonlu: solda eve dön, sağda büyük ve
            // nabız atan "devam" (ana aksiyon).
            var next = Button(content, "NextLevel", GameText.L("Sıradaki", "Next", "Weiter"), new Vector2(0.5f, 0.13f), new Vector2(200f, 0f), Color.white);
            var home = Button(content, "CompletionHome", GameText.Continue, new Vector2(0.5f, 0.13f), new Vector2(-210f, 0f), Color.white);
            StylePillButton(next, new Vector2(380f, 112f), PlayColor, CozyIconFactory.Play(), FontTitle, true);
            StylePillButton(home, new Vector2(320f, 104f), UtilityColor, CozyIconFactory.House(), FontButton, false);

            var screen = panel.AddComponent<LevelCompleteUI>();
            screen.Bind(title, reward, next, home, faceImage, catalog != null ? catalog.completionFaces : null);
            return panel;
        }

        static GameObject BuildLevelFailure(Transform canvas, PawPathCatalog catalog)
        {
            var panel = Panel("LevelFailure", canvas, Vector2.zero, Vector2.one, Color.black);
            if (catalog != null && catalog.failureBackground != null)
            {
                var backgroundGo = new GameObject("FailureBackground", typeof(RectTransform),
                    typeof(Image), typeof(AspectRatioFitter));
                backgroundGo.transform.SetParent(panel.transform, false);
                var backgroundRt = backgroundGo.GetComponent<RectTransform>();
                backgroundRt.anchorMin = backgroundRt.anchorMax = new Vector2(0.5f, 0.5f);
                backgroundRt.anchoredPosition = Vector2.zero;
                var background = backgroundGo.GetComponent<Image>();
                background.sprite = catalog.failureBackground;
                background.color = Color.white;
                background.raycastTarget = false;
                var fitter = backgroundGo.GetComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = catalog.failureBackground.rect.width / catalog.failureBackground.rect.height;
            }

            var shade = Panel("FailureShade", panel.transform, Vector2.zero, Vector2.one,
                new Color(0.08f, 0.01f, 0.01f, 0.34f));
            var content = SafeContent(shade.transform);
            var heading = Label(content, "FailureTitle", GameText.L("KAYBETTİN!", "TRY AGAIN!", "VERSUCH ES NOCHMAL!"), new Vector2(0.5f, 0.82f), Vector2.zero);
            heading.fontSize = 58;
            heading.fontStyle = FontStyle.Bold;
            heading.color = new Color(1f, 0.84f, 0.72f);
            var reason = Label(content, "FailureReason", GameText.L("Kedi yolun dışına düştü.", "Your cat fell off the path.", "Deine Katze ist vom Weg gefallen."),
                new Vector2(0.5f, 0.70f), Vector2.zero);
            reason.fontSize = 31;
            reason.color = Color.white;
            reason.rectTransform.sizeDelta = new Vector2(900f, 100f);

            // Yan yana, ikonlu: solda eve dön, sağda büyük ve nabız atan "tekrar dene".
            var retry = Button(content, "FailureRetry", GameText.L("Tekrar Dene", "Try Again", "Nochmal versuchen"), new Vector2(0.5f, 0.13f),
                new Vector2(200f, 0f), Color.white);
            var home = Button(content, "FailureHome", GameText.Continue, new Vector2(0.5f, 0.13f),
                new Vector2(-210f, 0f), Color.white);
            StylePillButton(retry, new Vector2(380f, 112f), WaterColor, CozyIconFactory.Restart(), FontButton, true);
            StylePillButton(home, new Vector2(320f, 104f), UtilityColor, CozyIconFactory.House(), FontButton, false);
            var screen = panel.AddComponent<LevelFailureUI>();
            screen.Bind(reason, retry, home);
            return panel;
        }

        static GameObject BuildShop(Transform canvas, PawPathCatalog catalog)
        {
            var panel = Panel("Shop", canvas, Vector2.zero, Vector2.zero, new Color(0.96f, 0.93f, 0.88f, 0.97f));
            if (catalog != null && catalog.shopBackground != null)
            {
                var background = panel.GetComponent<Image>();
                background.sprite = catalog.shopBackground;
                background.color = Color.white;
                background.preserveAspect = false;
                var backgroundRect = panel.GetComponent<RectTransform>();
                backgroundRect.offsetMin = new Vector2(-24f, -24f);
                backgroundRect.offsetMax = new Vector2(24f, 24f);
            }
            var content = SafeContent(panel.transform);
            Label(content, "Title", GameText.Shop, new Vector2(0.5f, 0.92f), Vector2.zero).fontSize = 40;
            var viewport = new GameObject("ShopViewport", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect));
            viewport.transform.SetParent(content, false);
            var viewportRt = viewport.GetComponent<RectTransform>();
            Stretch(viewportRt);
            viewportRt.offsetMin = new Vector2(60, 160);
            viewportRt.offsetMax = new Vector2(-60, -160);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var list = new GameObject("List", typeof(RectTransform));
            list.transform.SetParent(viewport.transform, false);
            var rt = list.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.content = rt;
            scroll.viewport = viewportRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var close = Button(content, "Close", GameText.House, new Vector2(0.5f, 0.08f), Vector2.zero, new Color(0.85f, 0.78f, 0.90f));
            var shop = panel.AddComponent<ShopScreen>();
            shop.Bind(list.transform, close);
            return panel;
        }

        static void FitSpriteToCamera(SpriteRenderer renderer, Camera camera)
        {
            if (renderer == null || renderer.sprite == null || camera == null)
                return;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float height = camera.orthographicSize * 2f;
            float width = height * camera.aspect;
            float coverScale = Mathf.Max(width / spriteSize.x, height / spriteSize.y) * 1.12f;
            renderer.transform.localScale = new Vector3(coverScale, coverScale, 1f);
            renderer.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, 1f);
        }

        static Transform CreateFurnitureSlot(string name, Transform parent, Vector3 position)
        {
            var slot = new GameObject(name);
            slot.transform.SetParent(parent);
            slot.transform.position = position;
            return slot.transform;
        }

        static GameObject Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            var image = go.GetComponent<Image>();
            image.color = color;
            // Tamamen şeffaf HUD panelleri yalnızca kapsayıcıdır. Raycast açık
            // kalırsa bütün ekranı kapatıp dünyada yol çizilmesini engeller.
            image.raycastTarget = color.a > 0.001f;
            return go;
        }

        static Transform SafeContent(Transform parent)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go.transform;
        }

        static Text Label(Transform parent, string name, string text, Vector2 anchor, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(900, 70);
            var t = go.AddComponent<Text>();
            t.text = text;
            t.font = UiFont();
            t.fontSize = FontBody;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.35f, 0.28f, 0.32f);
            t.raycastTarget = false;
            return t;
        }

        static Button Button(Transform parent, string name, string text, Vector2 anchor, Vector2 offset, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.AddComponent<UiButtonSound>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(420, 72);
            go.GetComponent<Image>().color = color;
            var label = Label(go.transform, "Text", text, new Vector2(0.5f, 0.5f), Vector2.zero);
            label.rectTransform.sizeDelta = new Vector2(400, 64);
            return go.GetComponent<Button>();
        }

        // ── Ortak görünüm sabitleri ─────────────────────────────────
        // Güvenli alanın (çentik/köşe) içinde ek kenar boşluğu.
        const float EdgeMargin = 32f;
        const float PlayButtonX = -306f;

        // Tek yazı tipi ailesi, dört boyut kademesi.
        public const int FontSmall = 20;
        public const int FontBody = 26;
        public const int FontButton = 32;
        public const int FontTitle = 48;

        // Tek palet: oda ahşabıyla uyumlu, biraz yumuşatılmış doygunluk.
        static readonly Color FoodColor = new Color(0.90f, 0.69f, 0.33f);   // bal sarısı
        static readonly Color WaterColor = new Color(0.36f, 0.63f, 0.90f);  // gök mavisi
        static readonly Color SleepColor = new Color(0.58f, 0.50f, 0.82f);  // lavanta
        static readonly Color LoveColor = new Color(0.90f, 0.42f, 0.58f);   // gül pembesi
        static readonly Color PlayColor = new Color(0.45f, 0.72f, 0.38f);   // yaprak yeşili
        static readonly Color UtilityColor = new Color(0.66f, 0.49f, 0.37f); // karamel
        static readonly Color CatFaceColor = new Color(0.63f, 0.43f, 0.31f);
        static readonly Color DrawModeColor = new Color(0.30f, 0.62f, 0.60f);   // deniz yeşili
        static readonly Color HardColor = new Color(0.84f, 0.47f, 0.42f);       // mercan
        static readonly Color StreetColor = new Color(0.84f, 0.53f, 0.47f);     // kiremit
        static readonly Color ForestColor = new Color(0.36f, 0.58f, 0.38f);     // orman yeşili
        static readonly Color CityColor = new Color(0.47f, 0.54f, 0.66f);       // arduvaz
        static readonly Color CyberColor = new Color(0.52f, 0.40f, 0.78f);      // neon mor

        /// <summary>Sahnedeki tüm metinleri dört boyut kademesinden en yakınına oturtur.</summary>
        static void ApplyTypeScale(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = SnapFontSize(text.fontSize);
                if (text.resizeTextForBestFit)
                    text.resizeTextMaxSize = SnapFontSize(text.resizeTextMaxSize);
            }
        }

        static int SnapFontSize(int size)
        {
            if (size <= 22) return FontSmall;
            if (size <= 28) return FontBody;
            if (size <= 40) return FontButton;
            return FontTitle;
        }

        static Image IconImage(Transform parent, string name, Sprite sprite, Color color,
            Vector2 anchor, Vector2 position, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        /// <summary>Yuvarlak, ikonlu, gölgeli düğme. icon null ise üzerine metin eklenebilir.</summary>
        static Button RoundIconButton(Transform parent, string name, Sprite icon, Color color,
            Vector2 anchor, Vector2 position, float diameter, float iconScale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.AddComponent<UiButtonSound>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(diameter, diameter);
            var background = go.GetComponent<Image>();
            background.sprite = CozyIconFactory.CircleButton();
            background.color = color;
            background.preserveAspect = true;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -6f);

            if (icon != null)
            {
                var iconImage = IconImage(go.transform, "Icon", icon, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), diameter * iconScale);
                iconImage.raycastTarget = false;
                var iconShadow = iconImage.gameObject.AddComponent<Shadow>();
                iconShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
                iconShadow.effectDistance = new Vector2(0f, -3f);
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.90f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            // Soluklaştırma CanvasGroup ile yapılır; renk tonu değişmesin.
            colors.disabledColor = Color.white;
            button.colors = colors;
            return button;
        }

        /// <summary>
        /// Hap biçimli ikon+yazı düğmesi; yuvarlak düğmelerle aynı halka, degrade ve
        /// gölgeyi kullanır. pulse=true ana aksiyona hafif nabız animasyonu ekler.
        /// </summary>
        static void StylePillButton(Button button, Vector2 size, Color color, Sprite icon, int fontSize, bool pulse)
        {
            button.GetComponent<RectTransform>().sizeDelta = size;
            var image = button.GetComponent<Image>();
            image.sprite = CozyIconFactory.PillButton();
            image.type = Image.Type.Sliced;
            image.color = color;
            var shadow = button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
            shadow.effectDistance = new Vector2(0f, -6f);

            const float iconSize = 50f;
            float iconX = -size.x * 0.5f + size.y * 0.5f + 8f;
            if (icon != null)
            {
                var iconImage = IconImage(button.transform, "Icon", icon, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(iconX, 1f), iconSize);
                iconImage.raycastTarget = false;
                var iconShadow = iconImage.gameObject.AddComponent<Shadow>();
                iconShadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
                iconShadow.effectDistance = new Vector2(0f, -3f);
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                float textLeft = icon != null ? iconX + iconSize * 0.5f + 6f : -size.x * 0.5f + 24f;
                float textRight = size.x * 0.5f - 28f;
                label.rectTransform.sizeDelta = new Vector2(textRight - textLeft, size.y - 16f);
                label.rectTransform.anchoredPosition = new Vector2((textLeft + textRight) * 0.5f, 2f);
                label.fontSize = fontSize;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                AddTextShadow(label);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.96f, 0.90f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            if (pulse)
            {
                var pulseAnim = button.gameObject.AddComponent<UiPulse>();
                pulseAnim.Configure(0.05f, 3.2f);
            }
        }

        static void AddTextShadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.10f, 0.06f, 0.03f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -2f);
        }

        static Text SettingsSectionLabel(Transform parent, string name, string text, float anchorY)
        {
            var label = Label(parent, name, text, new Vector2(0.5f, anchorY), Vector2.zero);
            label.fontSize = FontBody;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(1f, 0.86f, 0.62f);
            return label;
        }

        static void BuildProgressBar(Transform parent, float centerY)
        {
            var root = new GameObject("LevelProgress", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, centerY);
            rootRt.sizeDelta = new Vector2(620f, 56f);
            var rootBg = root.GetComponent<Image>();
            rootBg.sprite = CozyIconFactory.Pill();
            rootBg.type = Image.Type.Sliced;
            rootBg.color = new Color(0.20f, 0.13f, 0.09f, 0.55f);
            rootBg.raycastTarget = false;

            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(root.transform, false);
            var trackRt = track.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 0.5f);
            trackRt.anchorMax = new Vector2(1f, 0.5f);
            trackRt.offsetMin = new Vector2(40f, -8f);
            trackRt.offsetMax = new Vector2(-80f, 8f);
            var trackImage = track.GetComponent<Image>();
            trackImage.sprite = CozyIconFactory.Pill();
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color(1f, 1f, 1f, 0.35f);
            trackImage.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(16f, 16f);
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = CozyIconFactory.Pill();
            fillImage.type = Image.Type.Sliced;
            fillImage.color = PlayColor;
            fillImage.raycastTarget = false;

            var flag = IconImage(root.transform, "FinishFlag", CozyIconFactory.FinishFlag(), new Color(1f, 0.96f, 0.88f),
                new Vector2(1f, 0.5f), new Vector2(-44f, 2f), 46f);
            flag.raycastTarget = false;

            var marker = new GameObject("CatMarker", typeof(RectTransform), typeof(Image));
            marker.transform.SetParent(track.transform, false);
            var markerRt = marker.GetComponent<RectTransform>();
            markerRt.anchorMin = markerRt.anchorMax = new Vector2(0f, 0.5f);
            markerRt.sizeDelta = new Vector2(54f, 54f);
            var markerImage = marker.GetComponent<Image>();
            markerImage.sprite = CozyIconFactory.CircleButton();
            markerImage.color = new Color(1f, 0.97f, 0.92f);
            markerImage.raycastTarget = false;
            var face = IconImage(marker.transform, "Face", CozyIconFactory.HappyCatFace(), CatFaceColor,
                new Vector2(0.5f, 0.5f), Vector2.zero, 38f);
            face.raycastTarget = false;

            root.AddComponent<LevelProgressBar>().Bind(trackRt, fillRt, markerRt);
        }

        /// <summary>Oyna altındaki açılır listenin index'inci satırı: ikonlu hap düğme.</summary>
        static void StyleMenuPill(Button button, int index, Color color, Sprite icon)
        {
            button.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(PlayButtonX, -(EdgeMargin + 104f) - 64f - index * 112f);
            StylePillButton(button, new Vector2(340f, 96f), color, icon, FontBody, false);
        }

        static void StyleCompactTopButton(Button button, Vector2 position)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            StyleWarmGameButton(button, new Vector2(340f, 104f), 32);
        }

        static void StyleCompactMenuButton(Button button, Vector2 position)
        {
            StyleCompactTopButton(button, position);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 104f);
            var image = button.GetComponent<Image>();
            image.sprite = RusticUiSpriteFactory.ParchmentPanel();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var menuLabel = button.GetComponentInChildren<Text>();
            if (menuLabel != null)
                menuLabel.color = new Color(0.16f, 0.11f, 0.075f, 1f);
        }

        static void StyleWarmGameButton(Button button, Vector2 size, int fontSize, bool ornate = false)
        {
            if (button == null)
                return;
            button.GetComponent<RectTransform>().sizeDelta = size;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                // Altın yaprak süsü yalnızca ana düğmelerde; diğerleri sade ahşap.
                image.sprite = ornate ? RusticUiSpriteFactory.WoodButton() : RusticUiSpriteFactory.WoodButtonPlain();
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            if (button.GetComponent<Outline>() == null)
            {
                var outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.92f, 0.80f, 0.63f, 0.55f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = fontSize;
                label.color = new Color(1f, 0.96f, 0.88f, 1f);
                label.fontStyle = FontStyle.Bold;
                // Uzun çeviriler (özellikle Almanca) düğmeye sığmazsa yazı otomatik küçülür.
                label.resizeTextForBestFit = true;
                label.resizeTextMaxSize = fontSize;
                label.resizeTextMinSize = Mathf.Max(12, Mathf.RoundToInt(fontSize * 0.6f));
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f, 1f);
            colors.pressedColor = new Color(0.82f, 0.70f, 0.56f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.78f, 0.72f, 0.65f, 0.72f);
            button.colors = colors;
        }

        static void AddTextPanelBackground(Text text)
        {
            var background = new GameObject(text.name + "Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(text.transform.parent, false);
            var rt = background.GetComponent<RectTransform>();
            rt.anchorMin = text.rectTransform.anchorMin;
            rt.anchorMax = text.rectTransform.anchorMax;
            rt.pivot = text.rectTransform.pivot;
            rt.anchoredPosition = text.rectTransform.anchoredPosition;
            rt.sizeDelta = text.rectTransform.sizeDelta;
            var image = background.GetComponent<Image>();
            image.sprite = RusticUiSpriteFactory.ParchmentPanel();
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.84f);
            image.raycastTarget = false;
            var outline = background.AddComponent<Outline>();
            outline.effectColor = new Color(0.92f, 0.80f, 0.63f, 0.48f);
            outline.effectDistance = new Vector2(1f, -1f);
            background.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Button FindButton(Transform root, string name)
        {
            if (root == null)
                return null;

            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button != null && button.name == name)
                    return button;
            }
            return null;
        }

        static Font UiFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
