using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Gameplay;
using PawPath.Levels;
using PawPath.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TreeRewardAndUnderlayValidation
{
    public static void RunWithInteractionRegression()
    {
        Run();
        ForestInteractionUxValidation.Run();
    }
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Hidden).Invoke(obj, args);
    static void Field(object obj, string name, object value) => obj.GetType().GetField(name, Hidden).SetValue(obj, value);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Render(Camera camera, SpriteRenderer background, float x, string file)
    {
        camera.transform.position = new Vector3(x, 0, -10); background.transform.position = new Vector3(x, 0, 1);
        background.transform.localScale = Vector3.one * Mathf.Max(8.9f / background.sprite.bounds.size.y,
            8.9f * camera.aspect / background.sprite.bounds.size.x);
        int width = Mathf.RoundToInt(720 * camera.aspect);
        var target = new RenderTexture(width, 720, 24); camera.targetTexture = target; camera.Render();
        RenderTexture.active = target; var png = new Texture2D(width, 720, TextureFormat.RGB24, false);
        png.ReadPixels(new Rect(0, 0, width, 720), 0, 0); png.Apply(); File.WriteAllBytes(file, png.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(png); UnityEngine.Object.DestroyImmediate(target);
    }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project");
        var oldMode = GameplayMode.Current; var oldDifficulty = GameDifficulty.Selected;
        bool oldAutomatic = GameDifficulty.IsAutomatic;
        string[] saveKeys = { "PawPath.Save.v1", "PawPath.Save.v1.backup" };
        bool[] savedKeys = saveKeys.Select(PlayerPrefs.HasKey).ToArray();
        string[] savedValues = saveKeys.Select(key => PlayerPrefs.GetString(key)).ToArray();
        int oldPoints = -1, events = 0; Action<int> onPoints = value => events++;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var flow = new GameObject("TreeRewardFlow").AddComponent<GameFlow>(); Call(flow, "Awake"); flow.BindCatalog(catalog);
            oldPoints = SaveService.Data.lovePoints;
            var economy = new GameObject("TreeRewardEconomy").AddComponent<CozyEconomyManager>(); Call(economy, "Awake");
            GameEvents.OnLovePointsChanged += onPoints;
            var course = new GameObject("TreeRewardCourse").AddComponent<LevelCourseBuilder>();
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 4.45f; camera.aspect = 16f / 9f;
            var background = new GameObject("Background").AddComponent<SpriteRenderer>(); background.sortingOrder = -5;
            background.sprite = catalog.forestBackground;
            var cat = new GameObject("RewardCat").AddComponent<CatController>(); Call(cat, "Awake");
            var visual = new GameObject("Visual"); visual.transform.SetParent(cat.transform, false);
            visual.AddComponent<SpriteRenderer>(); cat.BindVisual(visual.transform, null); cat.ApplyCat(catalog.cats[0]);
            string output = Environment.GetEnvironmentVariable("MITZI_TREE_REWARD_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-tree-reward-underlay");
            Directory.CreateDirectory(output);
            int rewards = 0;
            foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
            foreach (DifficultyLevel difficulty in Enum.GetValues(typeof(DifficultyLevel)))
            {
                GameplayMode.Select(mode); GameDifficulty.Select(difficulty);
                course.BindVisuals(catalog.forestGapSprite, catalog.forestMoundSprite, catalog.forestGroundUnderfillSprite,
                    tree: catalog.climbTreeSprite, climbingFrames: catalog.climbingCatFrames);
                course.BindCrouchObstacle(catalog.forestCrouchObstacle, catalog.CrouchUpperForTheme(1));
                ClimbableTree tree = null;
                for (int candidateLevel = 1; candidateLevel <= 20 && tree == null; candidateLevel++)
                { course.Build(candidateLevel); tree = course.GetComponentInChildren<ClimbableTree>(); }
                Check(tree != null, "Missing safe tree in first 20 levels");
                var perch = tree.transform.Find("ClimbPerch"); var reward = perch.Find("TreeTopReward");
                Check(reward != null && reward.GetComponentInChildren<TextMesh>().text == "+5", "Reward point missing");
                Check(reward.position.y > perch.position.y && reward.GetComponentInChildren<Collider2D>() == null, "Reward position/physics wrong");
                Check(tree.transform.Find("ClimbHint/CaptionBacking") == null, "Climb label still has solid background");
                Check(tree.transform.Find("ClimbHint/CaptionShadow") != null, "Climb label lost contrast");
                float ground = tree.GetComponent<Collider2D>().bounds.min.y + .08f;
                cat.PlaceAtSpawn(new Vector2(tree.transform.position.x, ground + .34f));
                Physics2D.SyncTransforms(); cat.ApplyCat(catalog.cats[0]); Call(cat, "LateUpdate");
                var foot = cat.GetComponent<CircleCollider2D>(); Call(tree, "OnTriggerEnter2D", foot);
                int before = economy.ShoppingPoints, beforeEvents = events;
                Call(flow, "TransitionTo", GameFlowState.DrawingPreparation);
                MobileControlState.Set(MobileAction.Jump, true); Call(tree, "Update");
                tree.RequestClimb();
                Check(economy.ShoppingPoints == before && !cat.IsClimbing && reward.gameObject.activeSelf, "Build awarded points");
                MobileControlState.Reset(); Call(flow, "TransitionTo", GameFlowState.Gameplay); Call(tree, "Update");
                if (rewards == 0) Render(camera, background, tree.transform.position.x, Path.Combine(output, "tree-reward-before.png"));
                MobileControlState.Reset(); tree.RequestClimb();
                Check(cat.IsClimbing && economy.ShoppingPoints == before, "Reward paid on upward tap");
                // Trigger contacts may end during the ascent; arrival must still award once.
                Call(tree, "OnTriggerExit2D", foot);
                Field(cat, "climbStartTime", Time.time - 1f); Call(cat, "UpdateClimbing"); Call(tree, "Update");
                Check(economy.ShoppingPoints == before && reward.gameObject.activeSelf, "Reward paid halfway up");
                Field(cat, "climbStartTime", Time.time - 4f); Field(cat, "climbUntil", Time.time - 1f); Call(cat, "UpdateClimbing");
                Check(cat.IsPerched, "Cat did not reach perch");
                Call(flow, "TransitionTo", GameFlowState.Pause); Call(tree, "Update");
                Check(economy.ShoppingPoints == before, "Paused game paid reward");
                Call(flow, "TransitionTo", GameFlowState.Gameplay); Call(tree, "Update");
                Check(economy.ShoppingPoints == before + 5 && !reward.gameObject.activeSelf, "Arrival did not award/hide +5");
                Check(events == beforeEvents + 1, "HUD reward event missing/duplicated");
                for (int i = 0; i < 100; i++) Call(tree, "Update");
                MobileControlState.Set(MobileAction.Jump, true); Call(cat, "UpdatePerched");
                cat.PlaceAtSpawn(new Vector2(tree.transform.position.x, ground + .34f)); Physics2D.SyncTransforms();
                Call(tree, "OnTriggerEnter2D", foot); MobileControlState.Reset(); tree.RequestClimb();
                Field(cat, "climbStartTime", Time.time - 4f); Field(cat, "climbUntil", Time.time - 1f); Call(cat, "UpdateClimbing"); Call(tree, "Update");
                tree.gameObject.SetActive(false); tree.gameObject.SetActive(true); Call(tree, "Update");
                Check(economy.ShoppingPoints == before + 5 && events == beforeEvents + 1, "Repeated climb/enable farmed points");
                rewards++; MobileControlState.Reset();
            }
            // Inspect actual runtime theme bindings, including fallback ground fills.
            cat.gameObject.SetActive(false); int layouts = 0, abyssCases = 0;
            foreach (int theme in new[] { 0, 1, 2, 3 })
            foreach (int level in new[] { 1, 10, 21 })
            foreach (float aspect in new[] { 16f / 9f, 19.5f / 9f, 4f / 3f })
            {
                bool forest = theme == 1, city = theme == 2, cyber = theme == 3;
                camera.aspect = aspect;
                course.BindVisuals(cyber ? null : city ? catalog.cityGapSprite : forest ? catalog.forestGapSprite : catalog.roadGapSprite,
                    cyber ? null : city ? catalog.cityCarSprite : forest ? catalog.forestMoundSprite : catalog.moundSprite,
                    cyber ? catalog.cyberGroundTile : city ? catalog.cityPlatformSprite : forest ? catalog.forestGroundUnderfillSprite : catalog.roadPlatformSprite,
                    city ? catalog.cityVanSprite : forest || cyber ? null : catalog.streetMoundHighSprite,
                    city ? catalog.cityRoadSprite : null, useCyberTheme: cyber, cyberObstacles: cyber ? catalog.cyberObstacleSprites : null);
                course.Build(level); Check(course.ValidateCourseGeometry() == null, "Invalid geometry");
                background.sprite = cyber ? catalog.cyberBackground : city ? catalog.cityBackground : forest ? catalog.forestBackground : catalog.gameplayBackground;
                var abyss = course.transform.Find("GeneratedCourse/Abyss");
                Check((abyss != null) == (theme == 0 || theme == 1), "Unexpected abyss theme");
                if (abyss != null)
                {
                    var renderer = abyss.GetComponent<SpriteRenderer>();
                    Check(abyss.GetComponent<Collider2D>() == null && abyss.GetComponent<Rigidbody2D>() == null, "Abyss changes physics");
                    Check(renderer.sortingOrder == -1 && renderer.sortingOrder > background.sortingOrder, "Abyss sorting wrong");
                    Check(renderer.bounds.min.y <= -8f + .001f && renderer.bounds.max.y < -1.27f, "Abyss crosses ground surface");
                    Check(renderer.bounds.min.y < -4.45f, "Bottom edge visible in normal gameplay view");
                    var texture = renderer.sprite.texture;
                    Check(texture.GetPixel(0, 127).a < .001f && Mathf.Abs(texture.GetPixel(0, 0).a - .98f) < .01f, "Missing/generated gradient invalid");
                    foreach (var slab in course.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "RoadVisualSlab"))
                        Check(slab.sortingOrder > renderer.sortingOrder, "Abyss overlays walkable surface");
                    abyssCases++;
                }
                Check(course.GetComponentsInChildren<SpriteRenderer>().All(r => r.sprite != null), "Missing course sprite");
                if (level == 1 && Mathf.Abs(aspect - 16f / 9f) < .001f)
                {
                    Render(camera, background, 0, Path.Combine(output, "theme-" + theme + "-lower-area.png"));
                    if (forest)
                    {
                        abyss.gameObject.SetActive(false);
                        Render(camera, background, 0, Path.Combine(output, "forest-inspection-without-abyss.png"));
                        abyss.gameObject.SetActive(true); // Inspection only; runtime design is retained.
                    }
                }
                layouts++;
            }
            Debug.Log($"TREE_REWARD_UNDERLAY_PASS rewardedTrees={rewards} points={rewards * 5} repeatChecks=600 layouts={layouts} abyssCases={abyssCases} backgrounds=retained output={output}");
        }
        finally
        {
            GameEvents.OnLovePointsChanged -= onPoints;
            if (oldPoints >= 0) SaveService.Data.lovePoints = oldPoints;
            for (int i = 0; i < saveKeys.Length; i++)
                if (savedKeys[i]) PlayerPrefs.SetString(saveKeys[i], savedValues[i]); else PlayerPrefs.DeleteKey(saveKeys[i]);
            PlayerPrefs.Save(); MobileControlState.Reset(); GameplayMode.Select(oldMode);
            GameDifficulty.Select(oldDifficulty); if (oldAutomatic) GameDifficulty.SelectAutomatic(); Time.timeScale = 1f;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
