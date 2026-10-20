using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Gameplay;
using PawPath.Hub;
using PawPath.Levels;
using PawPath.UI;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MobileUxValidation
{
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static object Call(object target, string method, params object[] args) =>
        (target is Type type ? type : target.GetType()).GetMethod(method, Hidden)
            .Invoke(target is Type ? null : target, args);
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    static Rect ScreenRect(RectTransform rect, Camera camera)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project.");
        var oldMode = GameplayMode.Current;
        bool oldLarge = DisplaySizeSettings.Large;
        float oldTime = Time.timeScale;
        int oldTutorial = PlayerPrefs.GetInt(GameplayTutorialUI.DrawingSeenKey, 0);
        RenderTexture target = null;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameplayMode.Select(GameplayPlayMode.Drawing);
            PlayerPrefs.SetInt(GameplayTutorialUI.DrawingSeenKey, 1);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var flow = new GameObject("MobileUxFlow").AddComponent<GameFlow>();
            Call(flow, "Awake"); flow.BindCatalog(catalog);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 4.45f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.gameObject.AddComponent<SideScrollCamera>();
            var draw = camera.gameObject.AddComponent<LineDraw>(); Call(draw, "Awake"); draw.ResetInk(20);
            var canvasRoot = (GameObject)Call(typeof(RuntimeBootstrap), "CreateCanvas", flow.transform);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            canvasRoot.GetComponent<CanvasScaler>().enabled = false;
            var hudRoot = (GameObject)Call(typeof(RuntimeBootstrap), "BuildHud", canvasRoot.transform, null);
            var hud = hudRoot.GetComponent<HudView>();
            var events = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (EventSystem.current == null) Call(events, "OnEnable");
            Call(canvasRoot.GetComponent<GraphicRaycaster>(), "OnEnable");
            Call(flow, "TransitionTo", GameFlowState.DrawingPreparation);
            Call(hud, "OnLevelStarted"); Call(hud, "Update");
            var safe = hudRoot.transform.Find("SafeArea").GetComponent<RectTransform>();
            safe.GetComponent<SafeAreaFitter>().enabled = false;
            var start = hudRoot.GetComponentsInChildren<Button>(true).Single(b => b.name == "StartDrawingGame");
            var startRect = start.GetComponent<RectTransform>();
            var background = new GameObject("GameplayBackground").AddComponent<SpriteRenderer>();
            background.sprite = catalog.gameplayBackground; background.sortingOrder = -5;
            var course = new GameObject("PreviewCourse").AddComponent<LevelCourseBuilder>();
            course.BindVisuals(catalog.roadGapSprite, catalog.moundSprite, catalog.roadPlatformSprite,
                catalog.streetMoundHighSprite, streetGapLeft: catalog.streetGapLeftSprite, streetGapRight: catalog.streetGapRightSprite);
            course.Build(1);
            string output = Environment.GetEnvironmentVariable("MITZI_UX_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-mobile-ux");
            Directory.CreateDirectory(output);
            int cases = 0;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                new Vector2Int(2340, 1080), new Vector2Int(2560, 1600), new Vector2Int(1024, 768) })
            foreach (int cutout in new[] { 0, 1, 2, 3 })
            foreach (bool large in new[] { false, true })
            {
                if (target != null) UnityEngine.Object.DestroyImmediate(target);
                target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target;
                camera.aspect = (float)size.x / size.y;
                canvas.scaleFactor = Mathf.Sqrt((size.x / 1920f) * (size.y / 1080f));
                // Both landscape orientations: left/right notch, symmetric insets, and no cutout.
                float left = cutout == 1 ? .07f : cutout == 3 ? .045f : 0f;
                float right = cutout == 2 ? .07f : cutout == 3 ? .045f : 0f;
                float bottom = cutout == 0 ? 0f : .035f;
                Rect safePixels = new Rect(size.x * left, size.y * bottom,
                    size.x * (1f - left - right), size.y * (1f - bottom));
                safe.anchorMin = new Vector2(safePixels.xMin / size.x, safePixels.yMin / size.y);
                safe.anchorMax = new Vector2(safePixels.xMax / size.x, safePixels.yMax / size.y);
                safe.offsetMin = safe.offsetMax = Vector2.zero;
                Call(typeof(RuntimeBootstrap), "ApplyDrawingStartLayout", startRect, large);
                Canvas.ForceUpdateCanvases();
                // Populate CanvasRenderer depths before edit-mode GraphicRaycaster checks.
                camera.Render();
                Rect bounds = ScreenRect(startRect, camera);
                Check(bounds.xMin >= safePixels.xMin && bounds.xMax <= safePixels.xMax &&
                    bounds.yMin >= safePixels.yMin && bounds.yMax <= safePixels.yMax, "Start button outside safe area");
                Check(bounds.center.x > safePixels.xMin + safePixels.width * .75f &&
                    bounds.yMax < safePixels.yMin + safePixels.height * .3f, "Start button outside right-thumb control strip");
                foreach (var other in hudRoot.GetComponentsInChildren<Button>())
                {
                    if (other == start || !other.gameObject.activeInHierarchy) continue;
                    Check(!bounds.Overlaps(ScreenRect(other.GetComponent<RectTransform>(), camera)),
                        "Start button overlaps " + other.name);
                }
                // Real UI raycasting must hit the new button; the central drawing area stays free.
                var hits = new List<RaycastResult>();
                var pointer = new PointerEventData(EventSystem.current) { position = bounds.center };
                EventSystem.current.RaycastAll(pointer, hits);
                Check(hits.Any(hit => hit.gameObject == start.gameObject),
                    $"Start button cannot receive touch size={size} bounds={bounds} screen={Screen.width}x{Screen.height} pixelRect={camera.pixelRect} depth={start.image.depth} graphics={GraphicRegistry.GetGraphicsForCanvas(canvas).Count} raycasters={RaycasterManager.GetRaycasters().Count} hits={string.Join(",", hits.Select(h => h.gameObject.name))}");
                pointer.position = new Vector2(size.x * .5f, size.y * .4f); hits.Clear();
                EventSystem.current.RaycastAll(pointer, hits);
                Check(!hits.Any(hit => hit.gameObject.GetComponentInParent<Button>() != null), "Drawing area blocked by controls");
                if (large && (size.x == 1280 || size.x == 2340) && cutout == 3)
                {
                    background.transform.localScale = Vector3.one * Mathf.Max(8.9f / background.sprite.bounds.size.y,
                        8.9f * camera.aspect / background.sprite.bounds.size.x);
                    camera.Render(); RenderTexture.active = target;
                    var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
                    File.WriteAllBytes(Path.Combine(output, "build-start-" + size.x + ".png"), image.EncodeToPNG());
                    RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image);
                }
                cases++;
            }
            start.onClick.Invoke();
            Check(flow.State == GameFlowState.Gameplay && !draw.CanDraw, "Moved button cannot start the game");
            Call(hud, "Update"); Check(!start.gameObject.activeSelf, "Start button blocks gameplay joystick");

            Call(typeof(PettingHaptics), "Reset");
            int pulses = 0; double last = double.NegativeInfinity;
            for (int i = 0; i < 1000; i++)
            {
                double now = i * .01;
                bool accepted = (bool)Call(typeof(PettingHaptics), "ReservePulse", now);
                if (!accepted) continue;
                Check(now - last >= PettingHaptics.MinimumIntervalSeconds - 1e-8, "Haptic pulses overlap");
                last = now; pulses++;
            }
            Check(pulses == 7, "Unexpected petting haptic repeat rate");
            Call(typeof(PettingHaptics), "Reset");
            Check((bool)Call(typeof(PettingHaptics), "ReservePulse", 0d), "Initial haptic missing");
            var petA = new GameObject("PetA").AddComponent<CatPettingSystem>();
            var petB = new GameObject("PetB").AddComponent<CatPettingSystem>();
            for (int i = 0; i < 10; i++)
            {
                Call(i % 2 == 0 ? petA : petB, "BeginPetting");
                Call(i % 2 == 0 ? petA : petB, "StopPetting");
                Check(!(bool)Call(typeof(PettingHaptics), "ReservePulse", .1d + i * .1d), "Switching cats bypasses cooldown");
            }
            Check((bool)Call(typeof(PettingHaptics), "ReservePulse", 1.5d), "Haptic did not recover after cooldown");
            Debug.Log($"MOBILE_UX_VALIDATION_PASS layouts={cases} buttonClick=pass raycasts=pass hapticCalls=1000 pulses={pulses} output={output}");
        }
        finally
        {
            RenderTexture.active = null;
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.FindObjectOfType<LineDraw>()?.ClearStrokes();
            Call(typeof(PettingHaptics), "Reset");
            PlayerPrefs.SetInt(GameplayTutorialUI.DrawingSeenKey, oldTutorial);
            GameplayMode.Select(oldMode); DisplaySizeSettings.SetLarge(oldLarge); Time.timeScale = oldTime;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    public static void CompileAndroid()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project.");
        string output = Path.Combine(Path.GetTempPath(), "mitzi-mobile-ux-android-scripts");
        Directory.CreateDirectory(output);
        var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings {
            target = BuildTarget.Android, group = BuildTargetGroup.Android,
            options = ScriptCompilationOptions.DevelopmentBuild }, output);
        Check(result.assemblies != null && result.assemblies.Count > 0, "Android player script compilation failed");
        Debug.Log("MOBILE_UX_ANDROID_SCRIPTS_PASS assemblies=" + result.assemblies.Count);
    }
}
