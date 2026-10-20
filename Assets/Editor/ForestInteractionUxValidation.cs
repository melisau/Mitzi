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

public static class ForestInteractionUxValidation
{
    public static void RunWithGrounding()
    {
        Run();
        ForestTreeGroundingValidation.Run();
    }
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static object Call(object target, string name, params object[] args) =>
        (target is Type type ? type : target.GetType()).GetMethod(name, Hidden).Invoke(target is Type ? null : target, args);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
    static void Language(GameLanguage value)
    {
        typeof(GameText).GetField("language", Hidden).SetValue(null, value);
        typeof(GameText).GetField("loaded", Hidden).SetValue(null, true);
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static float Ground(LevelCourseBuilder course, float x) => Physics2D.RaycastAll(new Vector2(x, 15), Vector2.down, 30)
        .First(h => !h.collider.isTrigger && h.collider.transform.IsChildOf(course.transform) &&
            (h.collider.name == "Road" || h.collider.name == "Hump")).point.y;
    static void Render(Camera camera, SpriteRenderer background, float x, string output)
    {
        camera.transform.position = new Vector3(x, 0, -10);
        background.transform.position = new Vector3(x, 0, 1);
        background.transform.localScale = Vector3.one * Mathf.Max(8.9f / background.sprite.bounds.size.y,
            8.9f * camera.aspect / background.sprite.bounds.size.x);
        var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target;
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
        var png = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        png.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); png.Apply();
        File.WriteAllBytes(output, png.EncodeToPNG()); camera.targetTexture = null; RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(png); UnityEngine.Object.DestroyImmediate(target);
    }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project");
        var oldMode = GameplayMode.Current; var oldLanguage = GameText.Language;
        var oldDifficulty = (DifficultyLevel)PlayerPrefs.GetInt("PawPath.Difficulty", 0); bool oldAutomatic = GameDifficulty.IsAutomatic;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Language(GameLanguage.Turkish);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var flow = new GameObject("ForestUxFlow").AddComponent<GameFlow>(); Call(flow, "Awake"); flow.BindCatalog(catalog);
            Call(flow, "TransitionTo", GameFlowState.Gameplay);
            var course = new GameObject("ForestUxCourse").AddComponent<LevelCourseBuilder>();
            course.BindVisuals(catalog.forestGapSprite, catalog.forestMoundSprite, catalog.forestGroundUnderfillSprite,
                dogs: catalog.dogRunFrames, tree: catalog.climbTreeSprite, climbingFrames: catalog.climbingCatFrames);
            course.BindCrouchObstacle(catalog.forestCrouchObstacle, catalog.CrouchUpperForTheme(1));
            var cat = new GameObject("ForestUxCat").AddComponent<CatController>(); Call(cat, "Awake");
            var visual = new GameObject("Visual"); visual.transform.SetParent(cat.transform, false);
            visual.AddComponent<SpriteRenderer>(); cat.BindVisual(visual.transform, null);
            cat.ApplyCat(catalog.cats[0]);
            var foot = cat.GetComponent<CircleCollider2D>();
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 4.45f; camera.aspect = 16f / 9f;
            var background = new GameObject("ForestBackground").AddComponent<SpriteRenderer>();
            background.sprite = catalog.forestBackground; background.sortingOrder = -5;
            string output = Environment.GetEnvironmentVariable("MITZI_FOREST_UX_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-forest-ux");
            Directory.CreateDirectory(output);
            int layouts = 0, climbs = 0, noTrees = 0;
            foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
            foreach (DifficultyLevel difficulty in Enum.GetValues(typeof(DifficultyLevel)))
            {
                GameplayMode.Select(mode); GameDifficulty.Select(difficulty);
                for (int level = 1; level <= 20; level++)
                {
                    course.Build(level); Physics2D.SyncTransforms();
                    Check(course.ValidateCourseGeometry() == null, "Invalid course " + level);
                    var tree = course.GetComponentInChildren<ClimbableTree>();
                    string advice = (string)Call(typeof(DogObstacle), "EscapeAdvice");
                    Check(advice.Contains("tırman") == (tree != null), "Dog advice promises missing climb tree");
                    if (tree == null) noTrees++;
                    var passage = course.GetComponentInChildren<CrouchPassage>();
                    if (passage != null)
                    {
                        Check(passage.GetComponent<ClimbableTree>() == null, "Passage became climbable");
                        Check(passage.transform.Find("CrouchHint/DownArrow") != null, "Crouch arrow missing");
                        cat.PlaceAtSpawn(new Vector2(passage.Left - 1.1f, passage.FloorY + .34f));
                        Physics2D.SyncTransforms(); cat.ApplyCat(catalog.cats[0]); Call(cat, "LateUpdate"); Call(passage, "Update");
                        Check(passage.transform.Find("CrouchHint").gameObject.activeSelf, "Crouch hint missing on approach");
                        if (level == 1 && difficulty == DifficultyLevel.Easy && mode == GameplayPlayMode.DirectControl)
                            Render(camera, background, passage.transform.position.x, Path.Combine(output, "forest-go-under.png"));
                    }
                    if (tree != null)
                    {
                        Check(tree.transform.Find("ClimbHint/UpArrow") != null, "Climb arrow missing");
                        Check(tree.transform.Find("ClimbHint").GetComponentInChildren<Collider2D>(true) == null, "Hint blocks movement");
                        foreach (float approach in new[] { -.2f, 0f, .2f })
                        {
                            cat.PlaceAtSpawn(new Vector2(tree.transform.position.x + approach, Ground(course, tree.transform.position.x + approach) + .34f));
                            Physics2D.SyncTransforms(); cat.ApplyCat(catalog.cats[0]); Call(cat, "LateUpdate"); MobileControlState.Reset();
                            Check(tree.GetComponent<Collider2D>().bounds.Intersects(foot.bounds), "Tree trigger outside cat reach");
                            Call(tree, "OnTriggerEnter2D", foot); Call(tree, "Update");
                            Check(tree.transform.Find("ClimbHint").gameObject.activeSelf, "Climb hint missing");
                            if (level == 1 && approach == 0 && difficulty == DifficultyLevel.Easy && mode == GameplayPlayMode.DirectControl)
                                Render(camera, background, tree.transform.position.x, Path.Combine(output, "forest-climb.png"));
                            // Losing one of the cat's two contacts must not cancel the other.
                            var upper = cat.GetComponentsInChildren<Collider2D>(true).First(c => c != foot);
                            Call(tree, "OnTriggerEnter2D", upper); Call(tree, "OnTriggerExit2D", upper);
                            MobileControlState.Set(MobileAction.Jump, true); Call(tree, "Update");
                            Check(cat.IsClimbing && !MobileControlState.JumpQueued,
                                $"Jump-to-climb failed mode={mode} difficulty={difficulty} level={level} approach={approach} eligible={cat.CanStartClimb} pos={cat.transform.position}");
                            var dog = course.GetComponentInChildren<DogObstacle>();
                            if (dog != null)
                            {
                                Call(dog, "OnTriggerEnter2D", foot);
                                Check(flow.State == GameFlowState.Gameplay, "Dog catches climbing cat");
                            }
                            Set(cat, "climbStartTime", Time.time - 4f); Set(cat, "climbUntil", Time.time - 1f);
                            Call(cat, "UpdateClimbing"); Check(cat.IsClimbing, "Cat cannot remain on tree");
                            Check(cat.transform.position.y > Ground(course, tree.transform.position.x) + 1f, "Climb did not rise");
                            MobileControlState.Set(MobileAction.Jump, true); Call(cat, "UpdatePerched");
                            Check(!cat.IsClimbing, "Cannot leave tree with up input");
                            Call(tree, "OnTriggerExit2D", foot); MobileControlState.Reset(); climbs++;
                        }
                        cat.PlaceAtSpawn(new Vector2(tree.transform.position.x, Ground(course, tree.transform.position.x) + .34f));
                        Physics2D.SyncTransforms(); Call(tree, "OnTriggerEnter2D", foot);
                        Call(flow, "TransitionTo", GameFlowState.DrawingPreparation);
                        MobileControlState.Set(MobileAction.Jump, true); Call(tree, "Update");
                        tree.RequestClimb();
                        Check(!cat.IsClimbing && !tree.transform.Find("ClimbHint").gameObject.activeSelf, "Climbed during Build");
                        MobileControlState.Reset(); Call(flow, "TransitionTo", GameFlowState.Gameplay);
                    }
                    layouts++;
                }
            }
            var canvasObject = (GameObject)Call(typeof(RuntimeBootstrap), "CreateCanvas", flow.transform);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1f; canvas.sortingOrder = 200;
            var lessonObject = new GameObject("ForestLesson", typeof(RectTransform));
            lessonObject.transform.SetParent(canvas.transform, false);
            var lesson = lessonObject.AddComponent<GameplayTutorialUI>(); lesson.Build();
            foreach (GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
            foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
            {
                Language(language); lesson.Open(mode, null);
                foreach (int page in mode == GameplayPlayMode.Drawing ? new[] { 2 } : new[] { 2, 3 })
                {
                    lesson.ShowPage(page); lesson.Demo.RenderAt(1.8f); Canvas.ForceUpdateCanvases();
                    var caption = lesson.GetComponentsInChildren<Text>().Single(t => t.name == "Caption");
                    Check(caption.preferredHeight <= caption.rectTransform.rect.height + 1f,
                        "Instruction clipped for " + language + " " + mode + " " + page);
                    if (language == GameLanguage.Turkish && mode == GameplayPlayMode.DirectControl && page == 3)
                        Render(camera, background, 0f, Path.Combine(output, "forest-climb-lesson.png"));
                }
            }
            Debug.Log($"FOREST_INTERACTION_UX_PASS layouts={layouts} climbs={climbs} noTreeAdvice={noTrees} modes=2 difficulties=3 languages=3 output={output}");
        }
        finally
        {
            MobileControlState.Reset(); GameplayMode.Select(oldMode); GameDifficulty.Select(oldDifficulty);
            if (oldAutomatic) GameDifficulty.SelectAutomatic(); Language(oldLanguage); Time.timeScale = 1;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
