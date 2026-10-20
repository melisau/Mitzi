using System;
using System.Collections;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Gameplay;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The user chose shared jump/climb input with spatial separation, superseding
// the earlier dedicated-input experiment. Validate that final rule here.
public static class TreeJumpSeparationValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Hidden).Invoke(obj, args);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static float Value(object span, string name) => (float)span.GetType().GetField(name).GetValue(span);
    static float Ground(LevelCourseBuilder course, float x)
    {
        foreach (var hit in Physics2D.RaycastAll(new Vector2(x, 15), Vector2.down, 30))
            if (!hit.collider.isTrigger && hit.collider.transform.IsChildOf(course.transform) &&
                (hit.collider.name == "Road" || hit.collider.name == "Hump")) return hit.point.y;
        throw new Exception("No ground at gap approach " + x);
    }
    public static void RunAll()
    {
        Run();
        TreeRewardAndUnderlayValidation.RunWithInteractionRegression();
    }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project");
        var oldMode = GameplayMode.Current; var oldDifficulty = GameDifficulty.Selected;
        bool oldAutomatic = GameDifficulty.IsAutomatic, oldLarge = DisplaySizeSettings.Large;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var course = new GameObject("TreeGapCourse").AddComponent<LevelCourseBuilder>();
            course.BindVisuals(catalog.forestGapSprite, catalog.forestMoundSprite, catalog.forestGroundUnderfillSprite,
                tree: catalog.climbTreeSprite, climbingFrames: catalog.climbingCatFrames);
            course.BindCrouchObstacle(catalog.forestCrouchObstacle, catalog.CrouchUpperForTheme(1));
            var cat = new GameObject("TreeGapCat").AddComponent<CatController>(); Call(cat, "Awake");
            var body = cat.GetComponent<Rigidbody2D>(); var foot = cat.GetComponent<CircleCollider2D>();
            int layouts = 0, trees = 0, climbInputs = 0, gapJumps = 0, gapDistances = 0;
            foreach (bool large in new[] { false, true })
            foreach (DifficultyLevel difficulty in Enum.GetValues(typeof(DifficultyLevel)))
            {
                DisplaySizeSettings.SetLarge(large); GameDifficulty.Select(difficulty);
                for (int level = 1; level <= 100; level++)
                {
                    course.Build(level); Physics2D.SyncTransforms(); layouts++;
                    var abyss = course.transform.Find("GeneratedCourse/Abyss").GetComponent<SpriteRenderer>();
                    Check(abyss.bounds.max.y < -1.54f, "Abyss starts at/above visible grass");
                    Check(abyss.sprite.texture.GetPixel(0, 127).a < .001f, "Abyss has hard opaque upper boundary");
                    var slab = course.transform.Find("GeneratedCourse").GetComponentInChildren<SpriteRenderer>();
                    foreach (var renderer in course.GetComponentsInChildren<SpriteRenderer>())
                        if (renderer.name == "RoadVisualSlab")
                            Check(Mathf.Abs(renderer.sprite.rect.yMax - (catalog.forestGroundUnderfillSprite.rect.yMax - 18f)) < .01f,
                                "Forest ground still includes transparent top padding");
                    foreach (var tree in course.GetComponentsInChildren<ClimbableTree>())
                    {
                        trees++;
                        var trigger = tree.GetComponent<Collider2D>();
                        var gaps = (IEnumerable)typeof(LevelCourseBuilder).GetField("gaps", Hidden).GetValue(course);
                        foreach (var gap in gaps)
                        {
                            float left = Value(gap, "Left"), right = Value(gap, "Right");
                            float distance = tree.transform.position.x < left ? left - tree.transform.position.x : tree.transform.position.x - right;
                            float freeApproach = distance - trigger.bounds.extents.x - .75f * DisplaySizeSettings.CatScale;
                            Check(freeApproach >= ClimbableTree.GapApproachClearance - .002f,
                                $"Climb range overlaps jump approach level={level} difficulty={difficulty} large={large} clearance={freeApproach}");
                            gapDistances++;
                            foreach (float x in new[] { left - .6f, right + .6f })
                            foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
                            {
                                GameplayMode.Select(mode); cat.PlaceAtSpawn(new Vector2(x, Ground(course, x) + .35f));
                                Physics2D.SyncTransforms(); MobileControlState.Reset();
                                if (CrouchPassage.NeedsCrouch(cat, .45f)) continue; // Separate low-ceiling safety rule.
                                Call(tree, "OnTriggerEnter2D", foot); // Also exercise a stale contact near the gap.
                                MobileControlState.Set(MobileAction.Jump, true); Call(tree, "Update");
                                Check(!cat.IsClimbing && MobileControlState.JumpQueued, "Gap jump stolen by tree");
                                Call(cat, "FixedUpdate");
                                Check(body.velocity.y > 7f && !cat.IsClimbing, "Gap-side jump failed"); gapJumps++;
                            }
                        }
                        foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
                        foreach (bool keyboard in new[] { false, true })
                        {
                            GameplayMode.Select(mode); MobileControlState.Reset();
                            cat.PlaceAtSpawn(new Vector2(tree.transform.position.x, trigger.bounds.min.y + .08f + .35f));
                            Physics2D.SyncTransforms(); Call(tree, "OnTriggerEnter2D", foot);
                            if (keyboard) Call(tree, "HandleJumpToClimb", true);
                            else { MobileControlState.Set(MobileAction.Jump, true); Call(tree, "Update"); }
                            Check(cat.IsClimbing && !MobileControlState.JumpQueued, "Jump no longer climbs at safe trunk"); climbInputs++;
                        }
                    }
                }
            }
            Check(trees > 30 && gapJumps > 100, "Insufficient safe-placement coverage");
            Debug.Log($"TREE_GAP_FINAL_RULE_PASS layouts={layouts} safeTrees={trees} gapDistances={gapDistances} gapJumps={gapJumps} climbInputs={climbInputs} forestSurfacePadding=trimmed abyssTopAlpha=0 clearance={ClimbableTree.GapApproachClearance}");
        }
        finally
        {
            MobileControlState.Reset(); GameplayMode.Select(oldMode); DisplaySizeSettings.SetLarge(oldLarge);
            GameDifficulty.Select(oldDifficulty); if (oldAutomatic) GameDifficulty.SelectAutomatic(); Time.timeScale = 1f;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
