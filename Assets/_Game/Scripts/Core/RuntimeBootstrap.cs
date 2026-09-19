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
            cam.orthographicSize = 5f;
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
            root.AddComponent<CozyAudioManager>();
            var needs = root.AddComponent<CatNeedsSystem>();
            var levels = root.AddComponent<LevelManager>();

            var hub = new GameObject("Hub");
            hub.transform.SetParent(root.transform);
            var house = hub.AddComponent<CatHouseManager>();

            var hubBg = CreateQuad("HubRoom", hub.transform, new Vector3(0f, 0.2f, 1f), new Vector3(14f, 8f, 1f), new Color(0.90f, 0.82f, 0.74f));
            hubBg.sortingOrder = -2;
            if (catalog.homeBackground != null)
            {
                hubBg.sprite = catalog.homeBackground;
                hubBg.color = Color.white;
                FitSpriteToCamera(hubBg, cam);
            }

            var furnitureSlots = new Transform[4];
            furnitureSlots[(int)FurnitureSlotType.Rug] = CreateFurnitureSlot("RugSlot", hub.transform, new Vector3(0f, -2.05f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Bed] = CreateFurnitureSlot("BedSlot", hub.transform, new Vector3(3.5f, -1.45f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Bowl] = CreateFurnitureSlot("BowlSlot", hub.transform, new Vector3(-3.4f, -1.65f, 0f));
            furnitureSlots[(int)FurnitureSlotType.Wallpaper] = CreateFurnitureSlot("WallpaperSlot", hub.transform, new Vector3(0f, 0f, 0f));
            var furnitureView = hub.AddComponent<HubFurnitureView>();
            furnitureView.Bind(furnitureSlots);

            var spots = new Transform[6];
            for (int i = 0; i < spots.Length; i++)
            {
                var s = new GameObject($"Spot_{i}");
                s.transform.SetParent(hub.transform);
                s.transform.position = new Vector3(-3.5f + i * 1.4f, -1.1f, 0f);
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
            }
            var course = levelRoot.AddComponent<LevelCourseBuilder>();

            var season = levelRoot.AddComponent<SeasonBackdrop>();
            season.Bind(sky, landscapeGround, null);

            var spawn = new GameObject("Spawn");
            spawn.transform.SetParent(levelRoot.transform);
            spawn.transform.position = new Vector3(-6.2f, -1.15f, 0f);

            var goal = new GameObject("Goal");
            goal.transform.SetParent(levelRoot.transform);
            goal.transform.position = new Vector3(6.25f, 0.2f, 0f);
            var goalVis = CreateQuad("GoalPad", goal.transform, Vector3.zero, new Vector3(1.2f, 0.25f, 1f), new Color(0.96f, 0.88f, 0.64f));
            goalVis.sortingOrder = 2;
            var goalCol = goal.AddComponent<BoxCollider2D>();
            goalCol.isTrigger = true;
            goalCol.size = new Vector2(1.3f, 1.6f);
            goal.AddComponent<GoalTrigger>();

            levels.Bind(spawn.transform, goal.transform, season);
            levels.BindCourse(course);

            var catGo = new GameObject("PlayableCat");
            catGo.transform.SetParent(levelRoot.transform);
            catGo.transform.position = spawn.transform.position;
            var rb = catGo.AddComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            var circle = catGo.AddComponent<CircleCollider2D>();
            circle.radius = 0.28f;
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
            sideScroll.Bind(catGo.transform, sky.transform, landscapeGround.transform);

            var canvasGo = CreateCanvas(root.transform);
            var care = BuildCareUi(canvasGo.transform, needs);
            var hud = BuildHud(canvasGo.transform, care);
            var rescue = BuildRescue(canvasGo.transform);
            var levelComplete = BuildLevelComplete(canvasGo.transform);
            var shop = BuildShop(canvasGo.transform, catalog);
            shop.SetActive(false);
            rescue.SetActive(false);
            levelComplete.SetActive(false);

            var hudView = hud.GetComponent<HudView>();
            var shopBtn = FindButton(hud.transform, "ShopButton");
            if (shopBtn != null)
            {
                shopBtn.onClick.RemoveAllListeners();
                shopBtn.onClick.AddListener(() => shop.SetActive(true));
            }

            flow.BindCatalog(catalog);
            flow.BindRoots(hub, levelRoot, hud, rescue, levelComplete);
            hub.SetActive(true);
            levelRoot.SetActive(false);
            return flow;
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
            scaler.referenceResolution = new Vector2(1080, 1920);
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
            var love = Label(hud.transform, "Love", GameText.Love + ": 0", new Vector2(0.5f, 1f), new Vector2(0, -80));
            var level = Label(hud.transform, "Level", GameText.LevelLabel(1), new Vector2(0.5f, 1f), new Vector2(0, -140));
            var ink = Label(hud.transform, "Ink", "", new Vector2(0.5f, 1f), new Vector2(0, -200));
            var selected = Label(hud.transform, "Selected", GameText.PlayingAs + ": Mitzi", new Vector2(0.5f, 1f), new Vector2(0, -260));
            var play = Button(hud.transform, "PlayButton", GameText.Play, new Vector2(0.5f, 0f), new Vector2(0, 180), new Color(0.93f, 0.72f, 0.76f));
            var shop = Button(hud.transform, "ShopButton", GameText.Shop, new Vector2(0.5f, 0f), new Vector2(0, 90), new Color(0.78f, 0.84f, 0.72f));
            var restart = Button(hud.transform, "RestartButton", "Yeniden", new Vector2(1f, 1f), new Vector2(-125f, -55f), new Color(0.93f, 0.72f, 0.76f));
            restart.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 58f);
            var home = Button(hud.transform, "LevelHomeButton", "Eve Dön", new Vector2(0f, 1f), new Vector2(125f, -55f), new Color(0.78f, 0.84f, 0.72f));
            home.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 58f);
            restart.gameObject.SetActive(false);
            home.gameObject.SetActive(false);
            var brushes = BuildBrushToolbar(hud.transform);
            brushes.SetActive(false);
            var tutorial = BuildBrushTutorial(hud.transform);
            tutorial.SetActive(false);
            hud.GetComponent<HudView>().Bind(love, level, ink, selected, play, shop, restart, home, careUi, brushes, tutorial);
            return hud;
        }

        static CatNeedsUI BuildCareUi(Transform canvas, CatNeedsSystem needs)
        {
            var panel = Panel("CarePanel", canvas, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
            panel.AddComponent<CanvasGroup>();

            var feed = Button(panel.transform, "FeedButton", $"Mama +{needs.foodPoints}", new Vector2(0.16f, 0f), new Vector2(0f, 205f), new Color(0.88f, 0.72f, 0.55f));
            var water = Button(panel.transform, "WaterButton", $"Su +{needs.waterPoints}", new Vector2(0.16f, 0f), new Vector2(0f, 130f), new Color(0.62f, 0.80f, 0.91f));
            var sleep = Button(panel.transform, "SleepButton", $"Uyu +{needs.sleepPoints}", new Vector2(0.16f, 0f), new Vector2(0f, 55f), new Color(0.75f, 0.69f, 0.86f));
            foreach (var button in new[] { feed, water, sleep })
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 58f);

            var daily = Label(panel.transform, "DailyPetting", "Günlük Okşama", new Vector2(0.84f, 0f), new Vector2(0f, 125f));
            daily.fontSize = 20;
            daily.rectTransform.sizeDelta = new Vector2(330f, 48f);
            var warning = Label(panel.transform, "EnergyStatus", "", new Vector2(0.84f, 0f), new Vector2(0f, 65f));
            warning.fontSize = 19;
            warning.rectTransform.sizeDelta = new Vector2(360f, 70f);

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

            AddBrushButton(toolbar.transform, "NormalBrush", "Yol", -220f, new Color(0.17f, 0.15f, 0.14f), PathSurfaceType.Normal);
            AddBrushButton(toolbar.transform, "BounceBrush", "Zıpla", -110f, new Color(0.20f, 0.55f, 0.95f), PathSurfaceType.Bounce);
            AddBrushButton(toolbar.transform, "HazardBrush", "!", 0f, new Color(0.88f, 0.20f, 0.20f), PathSurfaceType.Hazard);
            AddBrushButton(toolbar.transform, "IceBrush", "Buz", 110f, new Color(0.95f, 0.98f, 1f), PathSurfaceType.Ice);
            var eraser = Button(toolbar.transform, "Eraser", "Sil", new Vector2(0.5f, 0f), new Vector2(220f, 46f), new Color(0.72f, 0.68f, 0.65f));
            eraser.GetComponent<RectTransform>().sizeDelta = new Vector2(92f, 58f);
            eraser.onClick.AddListener(() =>
            {
                if (LineDraw.Instance != null)
                    LineDraw.Instance.SetEraser();
            });
            return toolbar;
        }

        static void AddBrushButton(Transform parent, string name, string label, float x, Color color, PathSurfaceType type)
        {
            var button = Button(parent, name, label, new Vector2(0.5f, 0f), new Vector2(x, 46f), color);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(92f, 58f);
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 17;
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
            panel.GetComponent<Image>().raycastTarget = false;
            return panel;
        }

        static GameObject BuildRescue(Transform canvas)
        {
            var panel = Panel("Rescue", canvas, Vector2.zero, Vector2.zero, new Color(0.98f, 0.94f, 0.90f, 0.96f));
            var title = Label(panel.transform, "Title", "", new Vector2(0.5f, 0.62f), Vector2.zero);
            title.fontSize = 42;
            var body = Label(panel.transform, "Body", "", new Vector2(0.5f, 0.48f), Vector2.zero);
            body.rectTransform.sizeDelta = new Vector2(780, 320);
            var invite = Button(panel.transform, "Invite", GameText.InviteHome, new Vector2(0.5f, 0.22f), Vector2.zero, new Color(0.93f, 0.72f, 0.76f));
            var screen = panel.AddComponent<RescueScreen>();
            screen.Bind(title, body, invite);
            return panel;
        }

        static GameObject BuildLevelComplete(Transform canvas)
        {
            var panel = Panel("LevelComplete", canvas, Vector2.zero, Vector2.one, new Color(0.98f, 0.93f, 0.84f, 0.98f));
            var success = Label(panel.transform, "Success", "BAŞARDIN!", new Vector2(0.5f, 0.70f), Vector2.zero);
            success.fontSize = 54;
            success.fontStyle = FontStyle.Bold;
            success.color = new Color(0.54f, 0.30f, 0.22f);

            var title = Label(panel.transform, "CompletedLevel", "Bölüm Tamamlandı!", new Vector2(0.5f, 0.58f), Vector2.zero);
            title.fontSize = 36;
            var reward = Label(panel.transform, "Reward", "+10 Sevgi", new Vector2(0.5f, 0.48f), Vector2.zero);
            reward.fontSize = 30;
            reward.color = new Color(0.78f, 0.20f, 0.38f);

            var next = Button(panel.transform, "NextLevel", "Sıradaki Bölüm", new Vector2(0.5f, 0.32f), Vector2.zero, new Color(0.93f, 0.72f, 0.76f));
            next.GetComponent<RectTransform>().sizeDelta = new Vector2(440f, 78f);
            var home = Button(panel.transform, "CompletionHome", "Kedi Evine Dön", new Vector2(0.5f, 0.22f), Vector2.zero, new Color(0.78f, 0.84f, 0.72f));
            home.GetComponent<RectTransform>().sizeDelta = new Vector2(440f, 78f);

            var screen = panel.AddComponent<LevelCompleteUI>();
            screen.Bind(title, reward, next, home);
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
            Label(panel.transform, "Title", GameText.Shop, new Vector2(0.5f, 0.92f), Vector2.zero).fontSize = 40;
            var list = new GameObject("List", typeof(RectTransform));
            list.transform.SetParent(panel.transform, false);
            var rt = list.GetComponent<RectTransform>();
            Stretch(rt);
            rt.offsetMin = new Vector2(60, 160);
            rt.offsetMax = new Vector2(-60, -160);
            var close = Button(panel.transform, "Close", GameText.House, new Vector2(0.5f, 0.08f), Vector2.zero, new Color(0.85f, 0.78f, 0.90f));
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
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(420, 72);
            go.GetComponent<Image>().color = color;
            var label = Label(go.transform, "Text", text, new Vector2(0.5f, 0.5f), Vector2.zero);
            label.rectTransform.sizeDelta = new Vector2(400, 64);
            return go.GetComponent<Button>();
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
            var t = root.Find(name);
            return t != null ? t.GetComponent<Button>() : null;
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
