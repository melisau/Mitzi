using System;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Gameplay;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CrouchControlRuleValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Hidden).Invoke(obj, args);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Facing(CatController cat, int direction) => typeof(CatController).GetField("facing", Hidden).SetValue(cat, direction);
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project");
        var oldMode = GameplayMode.Current; var oldDifficulty = GameDifficulty.Selected;
        bool oldAutomatic = GameDifficulty.IsAutomatic, oldLarge = DisplaySizeSettings.Large;
        var oldPhysics = Physics2D.simulationMode;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics2D.simulationMode = SimulationMode2D.Script;
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var course = new GameObject("CrouchRuleCourse").AddComponent<LevelCourseBuilder>();
            course.BindVisuals(catalog.roadGapSprite, catalog.moundSprite, catalog.roadPlatformSprite, catalog.streetMoundHighSprite);
            course.BindCrouchObstacle(catalog.streetCrouchObstacle, catalog.CrouchUpperForTheme(0));
            var cat = new GameObject("CrouchRuleCat").AddComponent<CatController>(); Call(cat, "Awake");
            var body = cat.GetComponent<Rigidbody2D>(); body.interpolation = RigidbodyInterpolation2D.None;
            var floor = new GameObject("TraversalFloor"); floor.transform.position = new Vector3(1000, -.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(30, 1);
            int layouts = 0, checks = 0, traversals = 0, manualStops = 0, missing = 0;
            foreach (bool large in new[] { false, true })
            foreach (DifficultyLevel difficulty in Enum.GetValues(typeof(DifficultyLevel)))
            {
                DisplaySizeSettings.SetLarge(large); GameDifficulty.Select(difficulty);
                for (int level = 1; level <= 100; level++)
                {
                    course.Build(level); Physics2D.SyncTransforms(); layouts++;
                    var passage = course.GetComponentInChildren<CrouchPassage>();
                    if (passage == null)
                    {
                        Check(level != 1 || difficulty != DifficultyLevel.Easy,
                            $"First easy level missing box passage large={large} art={catalog.streetCrouchObstacle}");
                        missing++; continue;
                    }
                    string context = $"level={level} difficulty={difficulty} large={large}";
                    float y = passage.FloorY + .35f;
                    GameplayMode.Select(GameplayPlayMode.Drawing); MobileControlState.Reset();
                    cat.PlaceAtSpawn(new Vector2(passage.Left - .6f, y)); Physics2D.SyncTransforms();
                    Check(!cat.IsCrouching && CrouchPassage.NeedsCrouch(cat, .45f), "Bad approach setup: " + context);
                    Call(cat, "FixedUpdate");
                    Check(cat.IsCrouching && !MobileControlState.CrouchHeld, "Drawing requires crouch input: " + context); checks++;
                    cat.transform.position = new Vector3(passage.transform.position.x, y); Physics2D.SyncTransforms();
                    Call(cat, "FixedUpdate"); Check(cat.IsCrouching, "Drawing stands inside: " + context); checks++;
                    cat.transform.position = new Vector3(passage.Right + 1.7f, y); Physics2D.SyncTransforms();
                    Call(cat, "FixedUpdate"); Check(!cat.IsCrouching, "Drawing remains crouched outside: " + context); checks++;
                    GameplayMode.Select(GameplayPlayMode.DirectControl); MobileControlState.Reset();
                    cat.PlaceAtSpawn(new Vector2(passage.Left - .6f, y)); Physics2D.SyncTransforms();
                    Call(cat, "FixedUpdate"); Check(!cat.IsCrouching, "Direct control auto-crouched: " + context); checks++;
                    MobileControlState.Set(MobileAction.Crouch, true); Call(cat, "FixedUpdate");
                    Check(cat.IsCrouching, "Direct crouch input ignored: " + context); checks++;
                    cat.transform.position = new Vector3(passage.transform.position.x, y); Physics2D.SyncTransforms();
                    MobileControlState.Set(MobileAction.Crouch, false); Call(cat, "FixedUpdate");
                    Check(cat.IsCrouching, "Released input expands inside passage: " + context); checks++;
                    cat.transform.position = new Vector3(passage.Right + 1.7f, y); Physics2D.SyncTransforms();
                    Call(cat, "FixedUpdate"); Check(!cat.IsCrouching, "Direct safe crouch persists after exit: " + context); checks++;

                    if (difficulty != DifficultyLevel.Easy || Array.IndexOf(new[] { 1, 2, 6, 7, 25, 100 }, level) < 0) continue;
                    // Copy the actual generated box geometry onto a continuous isolated floor.
                    // This separates the crouch control rule from the following gap/obstacle.
                    var copy = UnityEngine.Object.Instantiate(passage);
                    copy.transform.position = new Vector3(1000, 0); copy.Configure(passage.Width, 0, passage.Clearance);
                    foreach (int direction in new[] { 1, -1 })
                    {
                        GameplayMode.Select(GameplayPlayMode.DirectControl); MobileControlState.Reset();
                        cat.PlaceAtSpawn(new Vector2(1000 - direction * 3.8f, .35f)); Facing(cat, direction);
                        MobileControlState.Set(direction > 0 ? MobileAction.Right : MobileAction.Left, true);
                        Physics2D.SyncTransforms();
                        for (int frame = 0; frame < 120; frame++)
                        {
                            Call(cat, "FixedUpdate"); Check(!cat.IsCrouching, "Direct auto-crouched on collision"); Physics2D.Simulate(.02f);
                        }
                        Check(direction * (body.position.x - 1000) < -copy.Width * .5f, "Direct crossed without crouch"); manualStops++;
                        foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
                        {
                            GameplayMode.Select(mode); MobileControlState.Reset();
                            cat.PlaceAtSpawn(new Vector2(1000 - direction * 3.8f, .35f)); Facing(cat, direction);
                            if (mode == GameplayPlayMode.DirectControl)
                            {
                                MobileControlState.Set(direction > 0 ? MobileAction.Right : MobileAction.Left, true);
                                MobileControlState.Set(MobileAction.Crouch, true);
                            }
                            Physics2D.SyncTransforms(); bool crouchedInside = false, finished = false;
                            for (int frame = 0; frame < 800; frame++)
                            {
                                // Direct input is needed for entry; releasing under the beam is safe.
                                if (mode == GameplayPlayMode.DirectControl && Mathf.Abs(body.position.x - 1000) < .3f)
                                    MobileControlState.Set(MobileAction.Crouch, false);
                                Vector2 before = body.position; Call(cat, "FixedUpdate");
                                if (Mathf.Abs(body.position.x - 1000) < 1.8f)
                                { Check(cat.IsCrouching, "Standing inside traversal: " + context); crouchedInside = true; }
                                Physics2D.Simulate(.02f);
                                Check(Mathf.Abs(body.position.x - before.x) < .15f && !cat.IsBusy, "Teleport/failure during traversal");
                                if (direction * (body.position.x - 1000) > 3.7f)
                                { finished = true; break; }
                            }
                            Check(finished && crouchedInside && !cat.IsCrouching, "Traversal incomplete: " + context + " " + mode + " dir=" + direction + " pos=" + body.position);
                            traversals++;
                        }
                    }
                    UnityEngine.Object.DestroyImmediate(copy.gameObject);
                }
            }
            Debug.Log($"CROUCH_CONTROL_RULE_PASS layouts={layouts} ruleChecks={checks} fullTraversals={traversals} noInputStops={manualStops} absentPassages={missing} movementCode=unchanged");
        }
        finally
        {
            Physics2D.simulationMode = oldPhysics; MobileControlState.Reset(); GameplayMode.Select(oldMode);
            DisplaySizeSettings.SetLarge(oldLarge); GameDifficulty.Select(oldDifficulty);
            if (oldAutomatic) GameDifficulty.SelectAutomatic(); Time.timeScale = 1f;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
