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
            var levels = root.AddComponent<LevelManager>();

            var hub = new GameObject("Hub");
            hub.transform.SetParent(root.transform);
            var house = hub.AddComponent<CatHouseManager>();

            var hubBg = CreateQuad("HubRoom", hub.transform, new Vector3(0f, 0.2f, 1f), new Vector3(14f, 8f, 1f), new Color(0.90f, 0.82f, 0.74f));
            hubBg.sortingOrder = -2;

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
            var ground = CreateQuad("StartPlatform", levelRoot.transform, new Vector3(-5.2f, -1.7f, 0f), new Vector3(4.4f, 0.35f, 1f), new Color(0.76f, 0.62f, 0.48f));
            ground.sortingOrder = 1;
            var groundCol = ground.gameObject.AddComponent<BoxCollider2D>();
            groundCol.size = Vector2.one;

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
            var bubbleGo = new GameObject("Bubble");
            bubbleGo.transform.SetParent(catGo.transform);
            bubbleGo.transform.localScale = Vector3.one * 1.8f;
            var bubble = bubbleGo.AddComponent<SpriteRenderer>();
            bubble.sprite = FallbackSprite.WhiteCircle();
            bubble.color = new Color(1f, 0.75f, 0.86f, 0.55f);
            bubble.sortingOrder = 9;
            bubble.enabled = false;
            var controller = catGo.AddComponent<CatController>();
            controller.BindVisual(vis.transform, bubble);

            var sideScroll = camGo.GetComponent<SideScrollCamera>();
            if (sideScroll == null)
                sideScroll = camGo.AddComponent<SideScrollCamera>();
            sideScroll.Bind(catGo.transform, sky.transform, landscapeGround.transform);

            var canvasGo = CreateCanvas(root.transform);
            var hud = BuildHud(canvasGo.transform);
            var rescue = BuildRescue(canvasGo.transform);
            var shop = BuildShop(canvasGo.transform);
            shop.SetActive(false);
            rescue.SetActive(false);

            var hudView = hud.GetComponent<HudView>();
            var shopBtn = FindButton(hud.transform, "ShopButton");
            if (shopBtn != null)
            {
                shopBtn.onClick.RemoveAllListeners();
                shopBtn.onClick.AddListener(() => shop.SetActive(true));
            }

            flow.BindCatalog(catalog);
            flow.BindRoots(hub, levelRoot, hud, rescue);
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

        static GameObject BuildHud(Transform canvas)
        {
            var hud = Panel("HUD", canvas, new Vector2(0, 0), new Vector2(0, 0), new Color(1, 1, 1, 0));
            hud.AddComponent<HudView>();
            var love = Label(hud.transform, "Love", GameText.Love + ": 0", new Vector2(0.5f, 1f), new Vector2(0, -80));
            var level = Label(hud.transform, "Level", GameText.LevelLabel(1), new Vector2(0.5f, 1f), new Vector2(0, -140));
            var ink = Label(hud.transform, "Ink", "", new Vector2(0.5f, 1f), new Vector2(0, -200));
            var selected = Label(hud.transform, "Selected", GameText.PlayingAs + ": Mitzi", new Vector2(0.5f, 1f), new Vector2(0, -260));
            var play = Button(hud.transform, "PlayButton", GameText.Play, new Vector2(0.5f, 0f), new Vector2(0, 180), new Color(0.93f, 0.72f, 0.76f));
            var shop = Button(hud.transform, "ShopButton", GameText.Shop, new Vector2(0.5f, 0f), new Vector2(0, 90), new Color(0.78f, 0.84f, 0.72f));
            hud.GetComponent<HudView>().Bind(love, level, ink, selected, play, shop);
            return hud;
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

        static GameObject BuildShop(Transform canvas)
        {
            var panel = Panel("Shop", canvas, Vector2.zero, Vector2.zero, new Color(0.96f, 0.93f, 0.88f, 0.97f));
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

        static GameObject Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            go.GetComponent<Image>().color = color;
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
