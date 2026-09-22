using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
            Build(catalog != null ? catalog : CatalogFactory.CreateRuntime());
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
            root.AddComponent<CozyEconomyManager>();
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
            furnitureSlots[(int)FurnitureSlotType.Rug] = CreateFurnitureSlot("RugSlot", hub.transform, new Vector3(0f, -2.05f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Bed] = CreateFurnitureSlot("BedSlot", hub.transform, new Vector3(3.5f, -1.45f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Bowl] = CreateFurnitureSlot("BowlSlot", hub.transform, new Vector3(-3.4f, -1.65f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Wallpaper] = CreateFurnitureSlot("WallpaperSlot", hub.transform, new Vector3(0f, 0f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Poster] = CreateFurnitureSlot("PosterSlot", hub.transform, new Vector3(2.2f, 0.65f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Water] = CreateFurnitureSlot("WaterSlot", hub.transform, new Vector3(-1.8f, -1.65f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Sand] = CreateFurnitureSlot("SandSlot", hub.transform, new Vector3(1.2f, -1.72f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Tree] = CreateFurnitureSlot("TreeSlot", hub.transform, new Vector3(4.1f, -1.25f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Toy] = CreateFurnitureSlot("ToySlot", hub.transform, new Vector3(0.2f, -1.82f, 0f));
            var furnitureView = hub.AddComponent<HubFurnitureView>();
            furnitureView.Bind(furnitureSlots);

            var spots = new Transform[6];
            var spotPositions = new[]
            {
                new Vector3(-4.25f, -1.55f, 0f),
                new Vector3(-2.10f, -1.25f, 0f),
                new Vector3(0.25f, -1.70f, 0f),
                new Vector3(2.25f, -1.20f, 0f),
                new Vector3(4.15f, -1.45f, 0f),
                new Vector3(1.05f, -1.30f, 0f)
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
            course.BindVisuals(catalog.roadGapSprite, catalog.moundSprite, catalog.roadPlatformSprite);

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
            // Kapı çizgisinden bölümün sağına kadar uzanan geniş WinZone.
            goalCol.size = new Vector2(10f, 12f);
            goalCol.offset = new Vector2(5f, 0f);
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

            var sideScroll = camGo.GetComponent<SideScrollCamera>();
            if (sideScroll == null)
                sideScroll = camGo.AddComponent<SideScrollCamera>();
            // Sokak artık kendi ParallaxBackground bileşeniyle daha yavaş kayar.
            // Kameraya birebir bağlamak görseli ekranda sabit tutuyordu.
            sideScroll.Bind(catGo.transform);

            var canvasGo = CreateCanvas(root.transform);
            var care = BuildCareUi(canvasGo.transform, needs);
            var hud = BuildHud(canvasGo.transform, care);
            var rescue = BuildRescue(canvasGo.transform);
            var levelComplete = BuildLevelComplete(canvasGo.transform, catalog);
            var levelFailure = BuildLevelFailure(canvasGo.transform, catalog);
            var shop = BuildShop(canvasGo.transform, catalog);
            shop.SetActive(false);
            rescue.SetActive(false);
            levelComplete.SetActive(false);
            levelFailure.SetActive(false);

            var hudView = hud.GetComponent<HudView>();
            var shopBtn = FindButton(hud.transform, "ShopButton");
            if (shopBtn != null)
            {
                shopBtn.onClick.RemoveAllListeners();
                shopBtn.onClick.AddListener(() => shop.SetActive(true));
            }

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
                float localY = -0.76f - renderer.sprite.bounds.min.y * scale;
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
            var love = Label(content, "Love", GameText.Love + ": 0", new Vector2(1f, 1f), new Vector2(-505f, -42f));
            var level = Label(content, "Level", GameText.LevelLabel(1), new Vector2(0.5f, 1f), new Vector2(0, -140));
            var ink = Label(content, "Ink", "", new Vector2(1f, 1f), new Vector2(-585f, -55f));
            love.fontSize = 21;
            ink.fontSize = 21;
            love.rectTransform.sizeDelta = new Vector2(220f, 52f);
            ink.rectTransform.sizeDelta = new Vector2(220f, 52f);
            var selected = Label(content, "Selected", "", new Vector2(0.5f, 1f), new Vector2(0, -260));
            selected.gameObject.SetActive(false);
            var play = Button(content, "PlayButton", "Oyna", new Vector2(1f, 1f), new Vector2(-120f, -55f), new Color(0.93f, 0.72f, 0.76f));
            var shop = Button(content, "ShopButton", GameText.Shop, new Vector2(1f, 1f), new Vector2(-330f, -55f), new Color(0.78f, 0.84f, 0.72f));
            StyleCompactTopButton(play, new Vector2(-180f, -62f));
            StyleCompactTopButton(shop, new Vector2(-540f, -62f));
            var editHome = Button(content, "EditHomeButton", "✎", new Vector2(1f, 1f),
                new Vector2(-785f, -62f), new Color(0.78f, 0.72f, 0.64f));
            var flipFurniture = Button(content, "FlipFurnitureButton", "↔", new Vector2(1f, 1f),
                new Vector2(-785f, -178f), new Color(0.78f, 0.72f, 0.64f));
            StyleWarmGameButton(editHome, new Vector2(128f, 104f), 42);
            StyleWarmGameButton(flipFurniture, new Vector2(128f, 104f), 40);
            flipFurniture.gameObject.SetActive(false);
            var quitGame = Button(content, "QuitGameButton", "Oyundan Çık", new Vector2(1f, 1f),
                new Vector2(-960f, -62f), new Color(0.66f, 0.48f, 0.40f));
            StyleWarmGameButton(quitGame, new Vector2(200f, 104f), 25);
            quitGame.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            var musicMute = Button(content, "MusicMuteButton", "♫", new Vector2(0f, 1f),
                new Vector2(52f, -44f), new Color(0.68f, 0.57f, 0.44f, 0.94f));
            var effectsMute = Button(content, "EffectsMuteButton", "SFX", new Vector2(0f, 1f),
                new Vector2(122f, -44f), new Color(0.68f, 0.57f, 0.44f, 0.94f));
            StyleWarmGameButton(musicMute, new Vector2(58f, 50f), 23);
            StyleWarmGameButton(effectsMute, new Vector2(70f, 50f), 15);
            void RefreshAudioButtons()
            {
                var audio = CozyAudioManager.Instance;
                var musicLabel = musicMute.GetComponentInChildren<Text>();
                var effectsLabel = effectsMute.GetComponentInChildren<Text>();
                if (musicLabel != null)
                    musicLabel.text = audio != null && audio.MusicMuted ? "♫×" : "♫";
                if (effectsLabel != null)
                    effectsLabel.text = audio != null && audio.EffectsMuted ? "SFX×" : "SFX";
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

            var modeMenu = new GameObject("PlayModeMenu", typeof(RectTransform));
            modeMenu.transform.SetParent(content, false);
            Stretch(modeMenu.GetComponent<RectTransform>());
            var drawingPlay = Button(modeMenu.transform, "DrawingPlayButton", "Çizerek Oyna", new Vector2(1f, 1f), new Vector2(-150f, -125f), new Color(0.93f, 0.72f, 0.76f));
            var directPlay = Button(modeMenu.transform, "DirectPlayButton", "Tuşlarla Oyna", new Vector2(1f, 1f), new Vector2(-150f, -195f), new Color(0.72f, 0.82f, 0.94f));
            StyleCompactTopButton(drawingPlay, new Vector2(-180f, -180f));
            StyleCompactTopButton(directPlay, new Vector2(-180f, -305f));
            modeMenu.SetActive(false);

            var themeSelectorObject = BuildThemeSelector(content);
            var themeSelector = themeSelectorObject.GetComponent<ThemeSelectionUI>();
            var developerLove = Button(content, "DeveloperLove", "+100", new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Color(0.55f, 0.45f, 0.70f, 0.82f));
            developerLove.GetComponent<RectTransform>().sizeDelta = new Vector2(92f, 50f);
            developerLove.GetComponentInChildren<Text>().fontSize = 18;
            StyleWarmGameButton(developerLove, new Vector2(110f, 52f), 18);
            developerLove.onClick.AddListener(() =>
            {
                if (CozyEconomyManager.Instance != null)
                    CozyEconomyManager.Instance.AddLove(100, "Geliştirici testi");
            });
            developerLove.gameObject.SetActive(Debug.isDebugBuild || Application.isEditor);

            var mitziOnly = Button(content, "DeveloperMitziOnly", "Sadece Mitzi", new Vector2(0f, 0.5f), new Vector2(105f, -68f), new Color(0.55f, 0.45f, 0.70f, 0.82f));
            mitziOnly.GetComponent<RectTransform>().sizeDelta = new Vector2(195f, 50f);
            mitziOnly.GetComponentInChildren<Text>().fontSize = 17;
            StyleWarmGameButton(mitziOnly, new Vector2(195f, 52f), 17);
            void RefreshMitziOnlyLabel()
            {
                var label = mitziOnly.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = SaveService.DeveloperMitziOnly ? "✓ Sadece Mitzi" : "Tüm Kediler";
            }
            RefreshMitziOnlyLabel();
            mitziOnly.onClick.AddListener(() =>
            {
                SaveService.SetDeveloperMitziOnly(!SaveService.DeveloperMitziOnly);
                RefreshMitziOnlyLabel();
                if (CatHouseManager.Instance != null)
                    CatHouseManager.Instance.RefreshResidents();
                var mitzi = GameFlow.Instance != null && GameFlow.Instance.Catalog != null
                    ? GameFlow.Instance.Catalog.GetCat("mitzi") : null;
                if (mitzi != null)
                    GameEvents.PlayableCatChanged(mitzi);
            });
            mitziOnly.gameObject.SetActive(Debug.isDebugBuild || Application.isEditor);

            var resetProgress = Button(content, "DeveloperReset", "Baştan Başla", new Vector2(0f, 0.5f), new Vector2(105f, -132f), new Color(0.72f, 0.38f, 0.38f, 0.88f));
            resetProgress.GetComponent<RectTransform>().sizeDelta = new Vector2(195f, 50f);
            resetProgress.GetComponentInChildren<Text>().fontSize = 17;
            StyleWarmGameButton(resetProgress, new Vector2(195f, 52f), 17);
            resetProgress.onClick.AddListener(() =>
            {
                SaveService.ResetProgressForTesting();
                SaveService.SetDeveloperMitziOnly(true);
                RefreshMitziOnlyLabel();
                if (CozyEconomyManager.Instance != null)
                    CozyEconomyManager.Instance.RefreshUI();
                if (GameFlow.Instance != null)
                    GameFlow.Instance.EnterHub();
                var mitzi = GameFlow.Instance != null && GameFlow.Instance.Catalog != null
                    ? GameFlow.Instance.Catalog.GetCat("mitzi") : null;
                if (mitzi != null)
                    GameEvents.PlayableCatChanged(mitzi);
            });
            resetProgress.gameObject.SetActive(Debug.isDebugBuild || Application.isEditor);
            var restart = Button(content, "RestartButton", "Yeniden", new Vector2(1f, 1f), new Vector2(-125f, -55f), new Color(0.93f, 0.72f, 0.76f));
            restart.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 58f);
            var home = Button(content, "LevelHomeButton", "Eve Dön", new Vector2(0f, 1f), new Vector2(125f, -125f), new Color(0.78f, 0.84f, 0.72f));
            home.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 58f);
            StyleWarmGameButton(restart, new Vector2(190f, 58f), 20);
            StyleWarmGameButton(home, new Vector2(190f, 58f), 20);
            restart.gameObject.SetActive(false);
            home.gameObject.SetActive(false);
            var brushes = BuildBrushToolbar(content);
            brushes.SetActive(false);
            var tutorial = BuildBrushTutorial(content);
            tutorial.SetActive(false);
            var controls = BuildMobileControls(content);
            controls.SetActive(false);
            hud.GetComponent<HudView>().Bind(love, level, ink, selected, play, shop, directPlay,
                restart, home, careUi, brushes, tutorial, controls, modeMenu, drawingPlay, themeSelector,
                editHome, flipFurniture);
            return hud;
        }

        static GameObject BuildThemeSelector(Transform parent)
        {
            var panel = new GameObject("ThemeSelector", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            Stretch(panel.GetComponent<RectTransform>());
            var street = Button(panel.transform, "StreetTheme", "Sokak", new Vector2(1f, 1f), new Vector2(-150f, -125f), new Color(0.83f, 0.68f, 0.52f));
            var forest = Button(panel.transform, "ForestTheme", "Orman", new Vector2(1f, 1f), new Vector2(-150f, -195f), new Color(0.52f, 0.70f, 0.55f));
            var city = Button(panel.transform, "CityTheme", "Cadde", new Vector2(1f, 1f), new Vector2(-150f, -255f), new Color(0.62f, 0.58f, 0.54f));
            StyleCompactTopButton(street, new Vector2(-180f, -180f));
            StyleCompactTopButton(forest, new Vector2(-180f, -305f));
            StyleCompactTopButton(city, new Vector2(-180f, -430f));
            panel.AddComponent<ThemeSelectionUI>().Bind(street, forest, city);
            return panel;
        }

        static GameObject BuildMobileControls(Transform parent)
        {
            var controls = new GameObject("MobileControls", typeof(RectTransform));
            controls.transform.SetParent(parent, false);
            Stretch(controls.GetComponent<RectTransform>());
            AddControlButton(controls.transform, "Left", "◀", new Vector2(0f, 0f), new Vector2(125f, 120f), MobileAction.Left);
            AddControlButton(controls.transform, "Right", "▶", new Vector2(0f, 0f), new Vector2(375f, 120f), MobileAction.Right);
            AddControlButton(controls.transform, "Jump", "ZIPLA", new Vector2(1f, 0f), new Vector2(-375f, 120f), MobileAction.Jump);
            AddControlButton(controls.transform, "Crouch", "EĞİL", new Vector2(1f, 0f), new Vector2(-125f, 120f), MobileAction.Crouch);
            return controls;
        }

        static void AddControlButton(Transform parent, string name, string label, Vector2 anchor,
            Vector2 position, MobileAction action)
        {
            var button = Button(parent, name, label, anchor, position, new Color(0.18f, 0.20f, 0.25f, 0.82f));
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 140f);
            StyleWarmGameButton(button, new Vector2(200f, 140f), 30);
            button.gameObject.AddComponent<MobileControlButton>().Configure(action);
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 30;
                text.color = Color.white;
            }
        }

        static CatNeedsUI BuildCareUi(Transform canvas, CatNeedsSystem needs)
        {
            var panel = Panel("CarePanel", canvas, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
            panel.AddComponent<CanvasGroup>();
            var content = SafeContent(panel.transform);

            var feed = Button(content, "FeedButton", $"Mama +{needs.foodPoints}", new Vector2(0.16f, 0f), new Vector2(0f, 300f), new Color(0.88f, 0.72f, 0.55f));
            var water = Button(content, "WaterButton", $"Su +{needs.waterPoints}", new Vector2(0.16f, 0f), new Vector2(0f, 195f), new Color(0.62f, 0.80f, 0.91f));
            var sleep = Button(content, "SleepButton", $"Uyu +{needs.sleepPoints}", new Vector2(0.16f, 0f), new Vector2(0f, 90f), new Color(0.75f, 0.69f, 0.86f));
            foreach (var button in new[] { feed, water, sleep })
                StyleWarmGameButton(button, new Vector2(420f, 88f), 26);

            var daily = Label(content, "DailyPetting", "Günlük Okşama", new Vector2(0.84f, 0f), new Vector2(0f, 125f));
            daily.fontSize = 20;
            daily.fontStyle = FontStyle.Bold;
            daily.color = new Color(0.10f, 0.075f, 0.055f, 1f);
            daily.alignment = TextAnchor.MiddleCenter;
            daily.rectTransform.sizeDelta = new Vector2(350f, 38f);
            AddTextPanelBackground(daily);
            var warning = Label(content, "EnergyStatus", "", new Vector2(0.84f, 0f), new Vector2(0f, 65f));
            warning.fontSize = 19;
            warning.fontStyle = FontStyle.Bold;
            warning.color = new Color(0.10f, 0.075f, 0.055f, 1f);
            warning.alignment = TextAnchor.MiddleCenter;
            warning.resizeTextForBestFit = true;
            warning.resizeTextMinSize = 16;
            warning.resizeTextMaxSize = 19;
            warning.rectTransform.sizeDelta = new Vector2(430f, 48f);
            warning.rectTransform.anchoredPosition = new Vector2(0f, 78f);
            AddTextPanelBackground(warning);

            var ui = panel.AddComponent<CatNeedsUI>();
            ui.Bind(needs, feed, water, sleep, daily, warning);
            return ui;
        }

        static GameObject BuildBrushToolbar(Transform parent)
        {
            var toolbar = new GameObject("BrushToolbar", typeof(RectTransform));
            toolbar.transform.SetParent(parent, false);
            var rt = toolbar.GetComponent<RectTransform>();
            Stretch(rt);

            AddBrushButton(toolbar.transform, "NormalBrush", "Yol", -340f, new Color(0.17f, 0.15f, 0.14f), PathSurfaceType.Normal);
            AddBrushButton(toolbar.transform, "BounceBrush", "Zıpla", -170f, new Color(0.20f, 0.55f, 0.95f), PathSurfaceType.Bounce);
            AddBrushButton(toolbar.transform, "HazardBrush", "!", 0f, new Color(0.88f, 0.20f, 0.20f), PathSurfaceType.Hazard);
            AddBrushButton(toolbar.transform, "IceBrush", "Buz", 170f, new Color(0.95f, 0.98f, 1f), PathSurfaceType.Ice);
            var eraser = Button(toolbar.transform, "Eraser", "Sil", new Vector2(0.5f, 0f), new Vector2(340f, 68f), new Color(0.72f, 0.68f, 0.65f));
            eraser.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 88f);
            StyleWarmGameButton(eraser, new Vector2(150f, 88f), 24);
            eraser.onClick.AddListener(() =>
            {
                if (LineDraw.Instance != null)
                    LineDraw.Instance.SetEraser();
            });
            return toolbar;
        }

        static void AddBrushButton(Transform parent, string name, string label, float x, Color color, PathSurfaceType type)
        {
            var button = Button(parent, name, label, new Vector2(0.5f, 0f), new Vector2(x, 68f), color);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 88f);
            StyleWarmGameButton(button, new Vector2(150f, 88f), 24);
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 24;
                text.color = type == PathSurfaceType.Normal ? Color.white : new Color(0.18f, 0.16f, 0.17f);
            }
            button.onClick.AddListener(() =>
            {
                if (LineDraw.Instance != null)
                    LineDraw.Instance.SetBrushType(type);
            });
        }

        static GameObject BuildBrushTutorial(Transform parent)
        {
            var panel = Panel("BrushTutorial", parent, Vector2.zero, Vector2.zero, new Color(0.12f, 0.10f, 0.11f, 0.82f));
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(760f, 430f);
            rt.anchoredPosition = Vector2.zero;
            var text = Label(panel.transform, "TutorialText",
                "YOLU TAMAMLAMA\n\nKedi hazır zeminde kendi yürür.\nYalnızca çukurlara köprü, tümseklere rampa çiz.\n\nSiyah: Normal yol   Mavi: Zıplatır\nKırmızı: Tehlike   Beyaz: Kaygan yol\nSilgi: Çizdiğin yolu siler",
                new Vector2(0.5f, 0.5f), Vector2.zero);
            text.rectTransform.sizeDelta = new Vector2(680f, 370f);
            text.fontSize = 27;
            text.color = Color.white;
            panel.GetComponent<Image>().raycastTarget = true;
            panel.AddComponent<Button>();
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
            var invite = Button(content, "Invite", GameText.InviteHome, new Vector2(0.5f, 0.22f), Vector2.zero, new Color(0.93f, 0.72f, 0.76f));
            var screen = panel.AddComponent<RescueScreen>();
            screen.Bind(title, body, invite);
            return panel;
        }

        static GameObject BuildLevelComplete(Transform canvas, PawPathCatalog catalog)
        {
            var panel = Panel("LevelComplete", canvas, Vector2.zero, Vector2.one, new Color(0.98f, 0.93f, 0.84f, 0.68f));
            var content = SafeContent(panel.transform);
            var success = Label(content, "Success", "BAŞARDIN!", new Vector2(0.5f, 0.82f), Vector2.zero);
            success.fontSize = 54;
            success.fontStyle = FontStyle.Bold;
            success.color = new Color(0.54f, 0.30f, 0.22f);

            var title = Label(content, "CompletedLevel", "Bölüm Tamamlandı!", new Vector2(0.5f, 0.72f), Vector2.zero);
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

            var reward = Label(content, "Reward", "+10 Sevgi", new Vector2(0.5f, 0.31f), Vector2.zero);
            reward.fontSize = 30;
            reward.color = new Color(0.78f, 0.20f, 0.38f);

            var next = Button(content, "NextLevel", "Sıradaki Bölüm", new Vector2(0.5f, 0.19f), Vector2.zero, new Color(0.93f, 0.72f, 0.76f));
            next.GetComponent<RectTransform>().sizeDelta = new Vector2(440f, 78f);
            var home = Button(content, "CompletionHome", "Kedi Evine Dön", new Vector2(0.5f, 0.09f), Vector2.zero, new Color(0.78f, 0.84f, 0.72f));
            home.GetComponent<RectTransform>().sizeDelta = new Vector2(440f, 78f);
            StyleWarmGameButton(next, new Vector2(440f, 78f), 27);
            StyleWarmGameButton(home, new Vector2(440f, 78f), 27);

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
            var heading = Label(content, "FailureTitle", "BAŞARISIZ!", new Vector2(0.5f, 0.82f), Vector2.zero);
            heading.fontSize = 58;
            heading.fontStyle = FontStyle.Bold;
            heading.color = new Color(1f, 0.84f, 0.72f);
            var reason = Label(content, "FailureReason", "Kedi yolun dışına düştü.",
                new Vector2(0.5f, 0.70f), Vector2.zero);
            reason.fontSize = 31;
            reason.color = Color.white;
            reason.rectTransform.sizeDelta = new Vector2(900f, 100f);

            var retry = Button(content, "FailureRetry", "Yeniden Dene", new Vector2(0.5f, 0.20f),
                Vector2.zero, new Color(0.72f, 0.48f, 0.34f));
            var home = Button(content, "FailureHome", "Kedi Evine Dön", new Vector2(0.5f, 0.10f),
                Vector2.zero, new Color(0.70f, 0.62f, 0.52f));
            StyleWarmGameButton(retry, new Vector2(420f, 76f), 26);
            StyleWarmGameButton(home, new Vector2(420f, 76f), 26);
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
            t.fontSize = 28;
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

        static void StyleWarmGameButton(Button button, Vector2 size, int fontSize)
        {
            if (button == null)
                return;
            button.GetComponent<RectTransform>().sizeDelta = size;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = RusticUiSpriteFactory.WoodButton();
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
