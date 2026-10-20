using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Cat;
using PawPath.Data;
using PawPath.Gameplay;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ForestTreeGroundingValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    static float GroundAt(LevelCourseBuilder course, float x)
    {
        return Physics2D.RaycastAll(new Vector2(x, 15), Vector2.down, 30)
            .First(hit => !hit.collider.isTrigger && hit.collider.transform.IsChildOf(course.transform) &&
                (hit.collider.name == "Road" || hit.collider.name == "Hump")).point.y;
    }

    static void ValidateTree(LevelCourseBuilder course, ClimbableTree tree)
    {
        Physics2D.SyncTransforms();
        var renderer = tree.GetComponent<SpriteRenderer>();
        var sprite = renderer.sprite;
        // Measured root crown, rather than the hanging tips or transparent canvas edge.
        float crownY = (sprite.rect.height * .20f - sprite.pivot.y) / sprite.pixelsPerUnit;
        float contactY = tree.transform.TransformPoint(new Vector3(0, crownY)).y;
        float ground = GroundAt(course, tree.transform.position.x);
        Check(Mathf.Abs(contactY - (ground - .03f)) < .002f,
            $"Tree root not on local surface: x={tree.transform.position.x} root={contactY} ground={ground}");
        Check(renderer.sortingOrder < 1 && renderer.sortingOrder > -5, "Roots render in front of terrain");
        var trigger = tree.GetComponent<BoxCollider2D>();
        Check(trigger.isTrigger && Mathf.Abs(trigger.bounds.min.y - (ground - .08f)) < .002f,
            "Climb trigger does not begin at the visible trunk base");
        Check(trigger.OverlapPoint(new Vector2(tree.transform.position.x, ground + .336f)),
            "Cat cannot reach grounded climb trigger");
        var perch = tree.transform.Find("ClimbPerch");
        Check(perch != null && perch.position.y < renderer.bounds.max.y && perch.position.y > ground + 1.4f,
            "Climb perch detached from tree artwork");
    }

    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project.");
        int oldDifficulty = PlayerPrefs.GetInt("PawPath.Difficulty", 0);
        bool oldAutomatic = GameDifficulty.IsAutomatic;
        var oldMode = GameplayMode.Current;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var course = new GameObject("ForestTreeCourse").AddComponent<LevelCourseBuilder>();
            course.BindVisuals(catalog.forestGapSprite, catalog.forestMoundSprite,
                catalog.forestGroundUnderfillSprite, tree: catalog.climbTreeSprite,
                climbingFrames: catalog.climbingCatFrames);
            course.BindCrouchObstacle(catalog.forestCrouchObstacle, catalog.CrouchUpperForTheme(1));
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 4.45f; // RuntimeBootstrap gameplay camera settings.
            camera.aspect = 16f / 9f;
            var background = new GameObject("ForestBackground").AddComponent<SpriteRenderer>();
            background.sprite = catalog.forestBackground;
            background.sortingOrder = -5;
            background.transform.localScale = Vector3.one * (8.9f / background.sprite.bounds.size.y);
            string output = Environment.GetEnvironmentVariable("MITZI_TREE_OUTPUT") ??
                Path.Combine(Path.GetTempPath(), "mitzi-forest-tree-grounding");
            Directory.CreateDirectory(output);
            int trees = 0, layouts = 0, climbs = 0;
            var cat = new GameObject("TreeClimbValidationCat").AddComponent<CatController>();
            Call(cat, "Awake");
            foreach (DifficultyLevel difficulty in Enum.GetValues(typeof(DifficultyLevel)))
            {
                GameDifficulty.Select(difficulty);
                for (int level = 1; level <= 100; level++)
                {
                    course.Build(level);
                    Check(course.ValidateCourseGeometry() == null, "Course geometry invalid at " + level);
                    foreach (var tree in course.GetComponentsInChildren<ClimbableTree>())
                    {
                        ValidateTree(course, tree);
                        trees++;
                        // Exercise the actual trigger and climb input once per difficulty.
                        if (climbs == (int)difficulty)
                        {
                            float ground = GroundAt(course, tree.transform.position.x);
                            cat.PlaceAtSpawn(new Vector2(tree.transform.position.x, ground + .34f));
                            Physics2D.SyncTransforms();
                            Call(tree, "OnTriggerEnter2D", cat.GetComponent<CircleCollider2D>());
                            Call(tree, "Update");
                            MobileControlState.Reset(); tree.RequestClimb();
                            Check(cat.IsClimbing, "Grounded cat cannot climb tree");
                            climbs++;
                        }
                        if (difficulty == DifficultyLevel.Easy && (level == 1 || level == 21))
                            Render(camera, background, tree.transform.position.x,
                                Path.Combine(output, "forest-level-" + level + ".png"));
                    }
                    layouts++;
                }
            }
            Check(trees > 100 && climbs == 3, "Insufficient tree/climb coverage");

            // Use the actual road and hill builders at several elevations. A higher
            // player path and unrelated level object must not become the tree's support.
            course.Build(1);
            foreach (var tree in course.GetComponentsInChildren<ClimbableTree>())
                tree.gameObject.SetActive(false);
            var generated = course.transform.Find("GeneratedCourse");
            float roadTop = course.CatSpawnY - .40f * LevelCourseBuilder.GameplayScale;
            int elevationTrials = 0;
            foreach (float elevation in new[] { -3f, -1.274f, 0f, 1.5f })
            foreach (bool hill in new[] { false, true })
            {
                float x = 1000 + elevationTrials * 20;
                var before = generated.Cast<Transform>().ToArray();
                Call(course, "Road", x, 8f);
                if (hill) Call(course, "Obstacle", x, 1.08f, 1.05f);
                var group = new GameObject("ElevatedTerrain").transform;
                group.SetParent(generated, false);
                foreach (Transform child in generated.Cast<Transform>().ToArray())
                    if (child != group && !before.Contains(child)) child.SetParent(group, true);
                group.position += Vector3.up * (elevation - roadTop);
                var stray = new GameObject("OldPlayerPath");
                stray.transform.position = new Vector3(x, elevation + 3f);
                stray.AddComponent<EdgeCollider2D>().points = new[] { new Vector2(-3, 0), new Vector2(3, 0) };
                Call(course, "CreateClimbableTree", x, roadTop);
                var placed = generated.GetComponentsInChildren<ClimbableTree>().Last();
                ValidateTree(course, placed);
                if (elevation == -1.274f)
                {
                    Render(camera, background, x, Path.Combine(output, hill ? "forest-hill.png" : "forest-flat.png"));
                    if (hill)
                    {
                        camera.aspect = 19.5f / 9f;
                        Render(camera, background, x, Path.Combine(output, "forest-hill-wide.png"));
                        camera.aspect = 16f / 9f;
                    }
                }
                UnityEngine.Object.DestroyImmediate(stray);
                elevationTrials++;
            }
            Debug.Log($"FOREST_TREE_GROUNDING_VALIDATION_PASS layouts={layouts} trees={trees} " +
                $"elevations={elevationTrials} climbs={climbs} camera=16:9,19.5:9 output={output}");
        }
        finally
        {
            MobileControlState.Reset();
            GameplayMode.Select(oldMode);
            GameDifficulty.Select((DifficultyLevel)oldDifficulty);
            if (oldAutomatic) GameDifficulty.SelectAutomatic();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    static void Render(Camera camera, SpriteRenderer background, float x, string file)
    {
        camera.transform.position = new Vector3(x, 0, -10);
        background.transform.position = new Vector3(x, 0, 1);
        int height = 720, width = Mathf.RoundToInt(height * camera.aspect);
        // Extend the same background artwork to cover the wider gameplay viewport.
        background.transform.localScale = Vector3.one * Mathf.Max(8.9f / background.sprite.bounds.size.y,
            8.9f * camera.aspect / background.sprite.bounds.size.x);
        var target = new RenderTexture(width, height, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(file, image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(target);
    }
}
