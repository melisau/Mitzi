using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Gameplay;
using PawPath.Levels;
using PawPath.Localization;
using PawPath.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ProgressionAndGiantPitValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    static object Bootstrap(string name, params object[] args) => typeof(RuntimeBootstrap).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    static void State(GameFlow flow, GameFlowState state) => typeof(GameFlow).GetField("<State>k__BackingField", Hidden).SetValue(flow, state);
    static string output;
    public static void RunCompactGoalAndGapRules()
    {
        Check(Application.productName == "MitziValidation", "Isolated project required");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
        var flow = new GameObject("Flow").AddComponent<GameFlow>(); Call(flow, "Awake"); flow.BindCatalog(catalog);
        var owned = SaveService.Data.unlockedCatIds.ToArray();
        try
        {
            SaveService.Data.unlockedCatIds.Clear(); SaveService.Data.unlockedCatIds.Add("mitzi");
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 4.45f; camera.transform.position = new Vector3(0, 0, -10);
            var background = new GameObject("HomeBackground").AddComponent<SpriteRenderer>();
            background.sprite = catalog.homeBackground; background.sortingOrder = -5;
            var canvasRoot = (GameObject)Bootstrap("CreateCanvas", flow.transform);
            var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 200;
            Bootstrap("BuildCareUi", canvasRoot.transform, null);
            var hud = (GameObject)Bootstrap("BuildHud", canvasRoot.transform, null);
            var goal = hud.GetComponent<NextCatGoalView>(); Call(goal, "OnEnable"); goal.Refresh();
            var card = hud.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "NextCatGoal");
            var balance = hud.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "ShopBalance");
            var needs = canvasRoot.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "NeedsCard");
            var label = card.GetComponentsInChildren<Text>().Single(t => t.name == "Goal");
            Check(label.text == "7" && card.GetComponentsInChildren<Text>().All(t => t.text == "7" || t.text == "?"), "Badge contains sentences or wrong target");
            var first = CatUnlockService.NextLockedCat(); SaveService.Data.unlockedCatIds.Add(first.id); GameEvents.CatUnlocked(first);
            Check(label.text == "20", "Badge does not advance after acquisition");
            string folder = Environment.GetEnvironmentVariable("MITZI_PROGRESSION_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-progression");
            Directory.CreateDirectory(folder); int screens = 0;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(2340, 1080), new Vector2Int(1600, 1200) })
            {
                camera.aspect = (float)size.x / size.y;
                background.transform.localScale = Vector3.one * Mathf.Max(8.9f / background.sprite.bounds.size.y,
                    8.9f * camera.aspect / background.sprite.bounds.size.x);
                var rt = new RenderTexture(size.x, size.y, 24); camera.targetTexture = rt;
                Canvas.ForceUpdateCanvases(); camera.Render();
                var badgeBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRoot.transform, card);
                var balanceBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRoot.transform, balance);
                var needsBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRoot.transform, needs);
                Check(badgeBounds.min.x > balanceBounds.max.x && badgeBounds.min.y > needsBounds.max.y,
                    "Badge overlaps balance or care card " + size);
                Check(card.sizeDelta.x == 166 && card.sizeDelta.y == 76 && label.preferredWidth < label.rectTransform.rect.width,
                    "Badge or target is too large");
                Check(card.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(g => !g.raycastTarget), "Badge steals touch input");
                RenderTexture.active = rt; var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
                File.WriteAllBytes(Path.Combine(folder, "compact-cat-goal-" + size.x + "x" + size.y + ".png"), image.EncodeToPNG());
                RenderTexture.active = null; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); screens++;
            }
            foreach (var state in new[] { GameFlowState.DrawingPreparation, GameFlowState.Gameplay, GameFlowState.Pause })
            { State(flow, state); goal.Refresh(); Check(!card.gameObject.activeSelf, "Badge covers play controls"); }
            State(flow, GameFlowState.MainMenu);
            foreach (var cat in catalog.cats.Where(c => c != null)) SaveService.Data.unlockedCatIds.Add(cat.id);
            goal.Refresh(); Check(!card.gameObject.activeSelf, "Completed collection leaves unnecessary badge");
            var course = new GameObject("GapRuleCheck").AddComponent<LevelCourseBuilder>(); int normal = 0, giant = 0;
            foreach (DifficultyLevel difficulty in Enum.GetValues(typeof(DifficultyLevel)))
            {
                GameDifficulty.Select(difficulty);
                for (int level = 1; level <= 30; level++)
                {
                    course.Build(level);
                    var shelves = course.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "GiantPitShelf").ToArray();
                    Physics2D.SyncTransforms();
                    var gaps = (System.Collections.IEnumerable)typeof(LevelCourseBuilder).GetField("gaps", Hidden).GetValue(course);
                    foreach (var gap in gaps)
                    {
                        var type = gap.GetType(); float left = (float)type.GetField("Left").GetValue(gap), right = (float)type.GetField("Right").GetValue(gap);
                        if (right - left > 2.601f)
                        {
                            int count = shelves.Count(s => s.bounds.min.x > left && s.bounds.max.x < right);
                            Check(count == 1 || count == 3 || count == 4,
                                "Wide pit without valid stepping shelves " + level + " " + difficulty); giant++;
                        }
                        else normal++;
                    }
                }
            }
            Check(normal > 0 && giant > 0, "Missing ordinary or giant pits");
            Debug.Log($"COMPACT_CAT_GOAL_PASS screens={screens} targets=7-to-20 text=number-only overlap=none ordinaryGaps={normal} wideGapsWithShelves={giant}");
        }
        finally
        {
            SaveService.Data.unlockedCatIds.Clear(); SaveService.Data.unlockedCatIds.AddRange(owned);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
    static void Render(Camera camera, string name)
    {
        var rt = new RenderTexture(1600, 900, 24); camera.targetTexture = rt;
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
        File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
    }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Isolated project required");
        output = Environment.GetEnvironmentVariable("MITZI_PROGRESSION_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-progression");
        Directory.CreateDirectory(output);
        string primary = PlayerPrefs.GetString("PawPath.Save.v1", ""), backup = PlayerPrefs.GetString("PawPath.Save.v1.backup", "");
        var simulation = Physics2D.simulationMode;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var flow = new GameObject("Flow").AddComponent<GameFlow>(); Call(flow, "Awake"); flow.BindCatalog(catalog);
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 4.45f; camera.aspect = 16f / 9;
            camera.transform.position = new Vector3(0, 0, -10);
            var canvasRoot = (GameObject)Bootstrap("CreateCanvas", flow.transform);
            var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 200;
            var hud = (GameObject)Bootstrap("BuildHud", canvasRoot.transform, null);
            var goal = hud.GetComponent<NextCatGoalView>(); Call(goal, "OnEnable");
            SaveService.Data.unlockedCatIds.Clear(); SaveService.Data.unlockedCatIds.Add("mitzi");
            var milestones = catalog.cats.Where(c => c != null && c.unlockAfterLevel > 0).OrderBy(c => c.unlockAfterLevel).ToArray();
            Check(milestones[0].unlockAfterLevel == 7, "First cat milestone changed");
            foreach (var cat in milestones)
            {
                goal.Refresh(); var label = hud.GetComponentsInChildren<Text>(true).Single(t => t.name == "Goal");
                Check(CatUnlockService.NextLockedCat() == cat && label.text.Contains(cat.unlockAfterLevel.ToString()), "Wrong next target");
                Check(!label.text.Contains(cat.displayName), "Identity revealed before acquisition");
                SaveService.Data.highestCompletedLevel = 99; goal.Refresh();
                Check(CatUnlockService.NextLockedCat() == cat, "Target advanced without acquisition");
                Check(!CatUnlockService.TryUnlockAfterLevel(cat.unlockAfterLevel - 1, out _), "Early unlock");
                Check(CatUnlockService.TryUnlockAfterLevel(cat.unlockAfterLevel, out var obtained) && obtained == cat, "Wrong unlock order");
            }
            Check(CatUnlockService.NextLockedCat() == null, "Completed collection has target");
            SaveService.Data.unlockedCatIds.Clear(); SaveService.Data.unlockedCatIds.Add("mitzi"); goal.Refresh();
            Render(camera, "next-cat-goal");
            State(flow, GameFlowState.DrawingPreparation); goal.Refresh();
            Check(!hud.GetComponentsInChildren<Transform>(true).Single(t => t.name == "NextCatGoal").gameObject.activeSelf, "Goal covers build controls");
            hud.SetActive(false);
            int guidePages = 0;
            foreach (GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
            {
                typeof(GameText).GetField("language", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, language);
                typeof(GameText).GetField("loaded", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
                Bootstrap("BuildHowToPlay", canvasRoot.transform);
                var guide = canvasRoot.GetComponentsInChildren<HowToPlayUI>(true).Last();
                foreach (int page in new[] { 9, 10 })
                {
                    Call(guide, "Show", page);
                    var demo = guide.GetComponentsInChildren<GameplayTutorialDemo>().Single();
                    Check(demo.Kind == (page == 9 ? TutorialDemoKind.Crouch : TutorialDemoKind.Climb), "Wrong tree example");
                    demo.RenderAt(3.5f);
                    if (page == 10) Check(demo.RewardVisible, "Tree reward not introduced");
                    var body = guide.GetComponentsInChildren<Text>().Single(t => t.name == "Body");
                    Canvas.ForceUpdateCanvases();
                    Check(body.preferredHeight <= body.rectTransform.rect.height + 1, "Guide caption overflows: " + language + " page=" + page + " height=" + body.preferredHeight);
                    if (language == GameLanguage.Turkish) Render(camera, "guide-tree-" + page);
                    guidePages++;
                }
                Call(guide, "Close"); UnityEngine.Object.DestroyImmediate(guide.gameObject);
                var introRoot = new GameObject("Intro", typeof(RectTransform)); introRoot.transform.SetParent(canvasRoot.transform, false);
                var intro = introRoot.AddComponent<GameplayTutorialUI>(); intro.Build();
                foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
                {
                    intro.Open(mode, null);
                    foreach (int page in mode == GameplayPlayMode.Drawing ? new[] { 3, 4 } : new[] { 2, 3 })
                    {
                        intro.ShowPage(page); intro.Demo.RenderAt(3.5f);
                        var caption = intro.GetComponentsInChildren<Text>().Single(t => t.name == "Caption");
                        Canvas.ForceUpdateCanvases();
                        Check(caption.preferredHeight <= caption.rectTransform.rect.height + 1, "Intro caption overflows " + language);
                        guidePages++;
                    }
                }
                UnityEngine.Object.DestroyImmediate(introRoot);
            }
            UnityEngine.Object.DestroyImmediate(canvasRoot); State(flow, GameFlowState.Gameplay);
            var course = new GameObject("Course").AddComponent<LevelCourseBuilder>();
            int layouts = 0, pits = 0, crossings = 0;
            var countsSeen = new System.Collections.Generic.HashSet<int>();
            var renderedVariants = new System.Collections.Generic.HashSet<string>();
            foreach (int theme in new[] { 0, 1, 2, 3 })
            foreach (DifficultyLevel difficulty in Enum.GetValues(typeof(DifficultyLevel)))
            {
                bool forest = theme == 1, city = theme == 2, cyber = theme == 3;
                GameDifficulty.Select(difficulty);
                course.BindVisuals(cyber ? null : city ? catalog.cityGapSprite : forest ? catalog.forestGapSprite : catalog.roadGapSprite,
                    cyber ? null : city ? catalog.cityCarSprite : forest ? catalog.forestMoundSprite : catalog.moundSprite,
                    cyber ? catalog.cyberGroundTile : city ? catalog.cityPlatformSprite : forest ? catalog.forestGroundUnderfillSprite : catalog.roadPlatformSprite,
                    underfill: city ? catalog.cityRoadSprite : null, useCyberTheme: cyber, cyberObstacles: cyber ? catalog.cyberObstacleSprites : null);
                for (int level = 1; level <= 30; level++)
                {
                    course.Build(level); Physics2D.SyncTransforms(); layouts++;
                    Check(course.ValidateCourseGeometry() == null, "Invalid geometry level=" + level);
                    var shelves = course.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "GiantPitShelf").OrderBy(c => c.bounds.center.x).ToArray();
                    bool expected = level >= 6 && level % 3 == 0;
                    Check(expected ? shelves.Length == 1 || shelves.Length == 3 || shelves.Length == 4 : shelves.Length == 0, "Wrong pit progression");
                    if (!expected) continue;
                    pits++;
                    countsSeen.Add(shelves.Length);
                    var signature = shelves.Select(s => new Vector2(s.transform.position.x, s.bounds.max.y)).ToArray();
                    course.Build(level); Physics2D.SyncTransforms();
                    shelves = course.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "GiantPitShelf").OrderBy(c => c.bounds.center.x).ToArray();
                    Check(signature.SequenceEqual(shelves.Select(s => new Vector2(s.transform.position.x, s.bounds.max.y))), "Retry reshuffles shelves");
                    Check(shelves.Length == 1 || shelves.Select(s => Mathf.RoundToInt(s.bounds.max.y * 100)).Distinct().Count() > 1, "Flat staircase");
                    foreach (var shelf in shelves)
                        Check(shelf.usedByEffector && shelf.GetComponent<PlatformEffector2D>().useOneWay && shelf.GetComponent<Rigidbody2D>() == null,
                            "Unstable or solid underside shelf");
                    string variant = theme + "-" + shelves.Length;
                    if (!renderedVariants.Contains(variant))
                    {
                    renderedVariants.Add(variant);
                    float center = (shelves[0].bounds.center.x + shelves[shelves.Length - 1].bounds.center.x) * .5f;
                    var background = new GameObject("Background").AddComponent<SpriteRenderer>();
                    background.sprite = cyber ? catalog.cyberBackground : forest ? catalog.forestBackground : catalog.gameplayBackground;
                    background.sortingOrder = -5; background.transform.position = new Vector3(center, 0, 1);
                    background.transform.localScale = Vector3.one * Mathf.Max(8.9f / background.sprite.bounds.size.y, 8.9f * camera.aspect / background.sprite.bounds.size.x);
                    camera.transform.position = new Vector3(center, 0, -10);
                    Render(camera, (cyber ? "cyber" : city ? "city" : forest ? "forest" : "street") + "-pit-" + shelves.Length + "-shelves");
                    UnityEngine.Object.DestroyImmediate(background.gameObject);
                    }
                    // Exercise real cat movement/ground probes through the complete staircase.
                    foreach (var collider in course.GetComponentsInChildren<Collider2D>())
                        if (collider.name != "Road" && collider.name != "GiantPitShelf") collider.enabled = false;
                    Physics2D.simulationMode = SimulationMode2D.Script;
                    foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
                    foreach (bool large in new[] { false, true })
                    foreach (int direction in new[] { 1, -1 })
                    {
                        GameplayMode.Select(mode); DisplaySizeSettings.SetLarge(large);
                        var catRoot = new GameObject("CrossingCat"); catRoot.AddComponent<Rigidbody2D>(); catRoot.AddComponent<CircleCollider2D>();
                        var cat = catRoot.AddComponent<CatController>(); Call(cat, "Awake");
                        var body = catRoot.GetComponent<Rigidbody2D>(); body.interpolation = RigidbodyInterpolation2D.None;
                        var roads = course.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "Road").ToArray();
                        var first = direction > 0 ? shelves[0] : shelves[shelves.Length - 1]; var last = direction > 0 ? shelves[shelves.Length - 1] : shelves[0];
                        var entry = roads.Where(c => direction > 0 ? c.bounds.max.x < first.bounds.min.x : c.bounds.min.x > first.bounds.max.x)
                            .OrderBy(c => Mathf.Abs(c.bounds.center.x - first.bounds.center.x)).First();
                        var exit = roads.Where(c => direction > 0 ? c.bounds.min.x > last.bounds.max.x : c.bounds.max.x < last.bounds.min.x)
                            .OrderBy(c => Mathf.Abs(c.bounds.center.x - last.bounds.center.x)).First();
                        float entryEdge = direction > 0 ? entry.bounds.max.x : entry.bounds.min.x;
                        cat.PlaceAtSpawn(new Vector2(entryEdge - direction * .7f, entry.bounds.max.y + .34f));
                        typeof(CatController).GetField("facing", Hidden).SetValue(cat, direction);
                        MobileControlState.Reset(); MobileControlState.Set(direction > 0 ? MobileAction.Right : MobileAction.Left, true);
                        bool passed = false; int landedMask = 0;
                        for (int frame = 0; frame < 700; frame++)
                        {
                            Physics2D.SyncTransforms();
                            var supports = shelves.Concat(new[] { entry, exit }).Where(c => body.position.x >= c.bounds.min.x - .05f && body.position.x <= c.bounds.max.x + .05f &&
                                Mathf.Abs(body.position.y - .336f - c.bounds.max.y) < .15f && body.velocity.y <= .15f).ToArray();
                            foreach (var support in supports)
                            {
                                int index = Array.IndexOf(shelves, support); if (index >= 0) landedMask |= 1 << index;
                                float edge = direction > 0 ? support.bounds.max.x : support.bounds.min.x;
                                if (support != exit && direction * (edge - body.position.x) < .42f)
                                    MobileControlState.Set(MobileAction.Jump, true);
                            }
                            Call(cat, "FixedUpdate"); Physics2D.Simulate(.02f); MobileControlState.Set(MobileAction.Jump, false);
                            if (direction * (body.position.x - (direction > 0 ? exit.bounds.min.x : exit.bounds.max.x)) > .6f && body.position.y > -1.1f) { passed = true; break; }
                            if (body.position.y < -3f) break;
                        }
                        Check(passed && landedMask == (1 << shelves.Length) - 1, $"Stair crossing failed level={level} {difficulty} {mode} large={large} direction={direction} pos={body.position} shelves={landedMask}");
                        crossings++; UnityEngine.Object.DestroyImmediate(catRoot);
                    }
                    Physics2D.simulationMode = simulation;
                }
            }
            Check(countsSeen.SetEquals(new[] { 1, 3, 4 }), "Missing shelf-count variety");
            Debug.Log($"PROGRESSION_GIANT_PIT_PASS catMilestones={milestones.Length} guidePages={guidePages} layouts={layouts} giantPits={pits} completeCrossings={crossings} shelfCounts=1,3,4 deterministicRetries=true output={output}");
        }
        finally
        {
            Physics2D.simulationMode = simulation; Time.timeScale = 1; MobileControlState.Reset();
            if (primary.Length > 0) PlayerPrefs.SetString("PawPath.Save.v1", primary); else PlayerPrefs.DeleteKey("PawPath.Save.v1");
            if (backup.Length > 0) PlayerPrefs.SetString("PawPath.Save.v1.backup", backup); else PlayerPrefs.DeleteKey("PawPath.Save.v1.backup");
            PlayerPrefs.Save(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
