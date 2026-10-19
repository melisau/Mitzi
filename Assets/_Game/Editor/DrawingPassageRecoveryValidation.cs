#if UNITY_EDITOR
using System;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Gameplay;
using PawPath.Levels;
using UnityEngine;

public static class DrawingPassageRecoveryValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Tick(CatController cat) => typeof(CatController).GetMethod("FixedUpdate", Hidden).Invoke(cat, null);
    public static void Run()
    {
        try
        {
            JoystickValidation.Run();
            var flow = GameFlow.Instance;
            GameplayMode.Select(GameplayPlayMode.Drawing); flow.StartNextLevel(); flow.StartGameplayFromDrawing(true);
            var cat = flow.GetComponentInChildren<CatController>(true);
            typeof(CatController).GetMethod("Awake", Hidden).Invoke(cat, null);
            var body = cat.GetComponent<Rigidbody2D>();
            var previousMode = Physics2D.simulationMode; Physics2D.simulationMode = SimulationMode2D.Script;
            var floor = new GameObject("RecoveryFloor"); floor.transform.position = new Vector3(1000, -.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(20, 1);
            var passageRoot = new GameObject("RecoveryPassage"); passageRoot.transform.position = new Vector3(1002, 0);
            passageRoot.AddComponent<CrouchPassage>().Configure(2, 0, CrouchPassage.PassageClearance);
            var wall = passageRoot.AddComponent<BoxCollider2D>(); wall.size = new Vector2(2, 4); wall.offset = new Vector2(0, CrouchPassage.PassageClearance + 2);
            // Deliberately frictional wall: pushing against it used to hold the cat in midair.
            wall.sharedMaterial = new PhysicsMaterial2D("RecoveryWallFriction") { friction = 1 };
            foreach (float height in new[] { 1f, 2f, 3f })
            {
                cat.PlaceAtSpawn(new Vector2(1000.4f, height)); body.velocity = Vector2.right * 2.55f;
                MobileControlState.Reset(); Physics2D.SyncTransforms();
                bool passed = false;
                for (int frame = 0; frame < 350; frame++)
                {
                    Tick(cat); Physics2D.Simulate(.02f);
                    if (body.position.x > 1003.6f) { passed = true; break; }
                }
                if (!passed) throw new Exception("Drawing cat stuck at passage from height " + height + " position=" + body.position);
            }
            UnityEngine.Object.DestroyImmediate(passageRoot);
            var course = flow.GetComponentInChildren<LevelCourseBuilder>(true);
            int realTrials = 0;
            foreach (bool large in new[] { false, true })
            {
                DisplaySizeSettings.SetLarge(large);
                for (int theme = 0; theme < 4; theme++)
                {
                    course.BindCrouchObstacle(flow.Catalog.CrouchObstacleForTheme(theme), flow.Catalog.CrouchUpperForTheme(theme));
                    foreach (int number in new[] { 6, 7 })
                    {
                        course.Build(number); Physics2D.SyncTransforms();
                        var template = course.GetComponentInChildren<CrouchPassage>();
                        if (template == null) throw new Exception("Missing actual passage fixture");
                        passageRoot = UnityEngine.Object.Instantiate(template.gameObject);
                        passageRoot.transform.position = new Vector3(1002, 0);
                        var real = passageRoot.GetComponent<CrouchPassage>(); real.Configure(template.Width, 0, template.Clearance);
                        foreach (float height in new[] { 1f, 1.6f, 2.4f })
                        {
                            cat.PlaceAtSpawn(new Vector2(real.Left - .1f, height));
                            body.velocity = Vector2.right * 2.55f;
                            MobileControlState.Reset(); Physics2D.SyncTransforms();
                            bool passed = false;
                            for (int frame = 0; frame < 650; frame++)
                            {
                                Tick(cat); Physics2D.Simulate(.02f);
                                if (body.position.x > real.Right + .6f) { passed = true; break; }
                            }
                            if (!passed) throw new Exception("Actual passage stuck theme=" + theme + " level=" + number + " large=" + large + " height=" + height + " position=" + body.position);
                            realTrials++;
                        }
                        UnityEngine.Object.DestroyImmediate(passageRoot);
                    }
                }
            }
            var voidRoot = new GameObject("RecoveryVoidExit");
            var voidPassage = voidRoot.AddComponent<CrouchPassage>(); voidPassage.Configure(2, 0, .62f);
            cat.PlaceAtSpawn(new Vector2(1000, -2)); Physics2D.SyncTransforms();
            typeof(CatController).GetField("recoveringPassage", Hidden).SetValue(cat, voidPassage);
            bool recoveringInVoid = (bool)typeof(CatController).GetMethod("RecoverFromPassageLedge", Hidden).Invoke(cat, new object[] { false });
            if (recoveringInVoid) throw new Exception("Passage recovery suppresses normal falling below the floor");
            UnityEngine.Object.DestroyImmediate(voidRoot);
            Physics2D.simulationMode = previousMode;
            DisplaySizeSettings.SetLarge(false);
            UnityEngine.Object.DestroyImmediate(floor);
            Debug.Log("DRAWING_PASSAGE_RECOVERY_VALIDATION_PASS realTrials=" + realTrials);
        }
        catch (Exception error) { Debug.LogException(error); UnityEditor.EditorApplication.Exit(1); }
    }
}
#endif
