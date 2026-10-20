using System;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Gameplay;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GapEdgeValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(CatController cat, string method, params object[] args) =>
        typeof(CatController).GetMethod(method, Hidden).Invoke(cat, args);
    static void Set(CatController cat, string field, object value) =>
        typeof(CatController).GetField(field, Hidden).SetValue(cat, value);
    static int Jumps(CatController cat) => (int)typeof(CatController).GetField("jumpsRemaining", Hidden).GetValue(cat);
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project.");
        var previousSimulation = Physics2D.simulationMode;
        var previousMode = GameplayMode.Current;
        bool previousLarge = DisplaySizeSettings.Large;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics2D.simulationMode = SimulationMode2D.Script;
            var wall = new GameObject("GapLandingRoad").AddComponent<BoxCollider2D>();
            wall.size = new Vector2(6, 1.152f);
            var material = new PhysicsMaterial2D("GapValidationNoFriction") { friction = 0, bounciness = 0 };
            var root = new GameObject("GapEdgeCat");
            root.AddComponent<Rigidbody2D>();
            root.AddComponent<CircleCollider2D>().sharedMaterial = material;
            var cat = root.AddComponent<CatController>();
            Call(cat, "Awake");
            var body = root.GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.None;
            int trials = 0;
            foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
            foreach (bool large in new[] { false, true })
            foreach (int direction in new[] { 1, -1 })
            {
                GameplayMode.Select(mode);
                DisplaySizeSettings.SetLarge(large);
                wall.transform.position = new Vector3(direction * 3f, -.576f);
                foreach (float height in new[] { -.55f, -.35f, -.15f, 0f, .15f })
                foreach (float approach in new[] { .35f, .5f, .8f })
                {
                    cat.PlaceAtSpawn(new Vector2(-direction * approach, height));
                    Set(cat, "facing", direction);
                    MobileControlState.Reset();
                    MobileControlState.Set(direction > 0 ? MobileAction.Right : MobileAction.Left, true);
                    body.velocity = new Vector2(direction * 2.55f, -1f);
                    Physics2D.SyncTransforms();
                    string context = $"mode={mode} large={large} direction={direction} height={height} approach={approach}";
                    bool fell = false, landed = false;
                    for (int frame = 0; frame < 85; frame++)
                    {
                        Call(cat, "FixedUpdate");
                        Physics2D.Simulate(.02f);
                        if (body.position.y < -2f) { fell = true; break; }
                        if (direction * body.position.x > .8f && body.position.y > .3f)
                        { landed = true; break; }
                    }
                    Check(fell || (height >= 0f && landed), "Cat hangs on road side: " + context + " end=" + body.position);
                    trials++;
                }

                // A wall does not replenish the two airborne jumps. A legal remaining
                // jump can still save a near miss, until the actual fall boundary is crossed.
                cat.PlaceAtSpawn(new Vector2(-direction * .5f, -.35f));
                Set(cat, "facing", direction);
                Set(cat, "jumpsRemaining", 1);
                MobileControlState.Reset();
                MobileControlState.Set(direction > 0 ? MobileAction.Right : MobileAction.Left, true);
                body.velocity = new Vector2(direction * 2.55f, -1f);
                Physics2D.SyncTransforms();
                for (int frame = 0; frame < 3; frame++)
                {
                    Call(cat, "FixedUpdate");
                    Physics2D.Simulate(.02f);
                    Check(Jumps(cat) == 1, "Road side replenished jumps: " + mode);
                }
                MobileControlState.Set(MobileAction.Jump, true);
                Call(cat, "FixedUpdate");
                Check(Jumps(cat) == 0 && body.velocity.y > 8f, "Remaining airborne jump unavailable: " + mode);
                float speed = body.velocity.y;
                MobileControlState.Set(MobileAction.Jump, true);
                Call(cat, "TryJump");
                Check(Jumps(cat) == 0 && body.velocity.y == speed, "Extra wall jump granted: " + mode);
                bool saved = false;
                for (int frame = 0; frame < 100; frame++)
                {
                    Call(cat, "FixedUpdate");
                    Physics2D.Simulate(.02f);
                    if (direction * body.position.x > .8f && body.position.y > .3f)
                    { saved = true; break; }
                    if (body.position.y < -2f) break;
                }
                Check(saved, "Legal airborne jump could not recover: " + mode);

                cat.PlaceAtSpawn(new Vector2(-direction * 2f, -7.6f));
                Physics2D.SyncTransforms();
                MobileControlState.Set(MobileAction.Jump, true);
                Call(cat, "TryJump");
                Check(body.velocity.y == 0f && Jumps(cat) == 2 && !MobileControlState.JumpQueued,
                    "Fall boundary accepted jump: " + mode);
                Set(cat, "keyboardJumpQueued", true);
                Call(cat, "TryJump");
                Check(body.velocity.y == 0f && Jumps(cat) == 2, "Fall boundary accepted keyboard jump");

                cat.PlaceAtSpawn(new Vector2(-direction * 2f, 1f));
                Set(cat, "busy", true);
                MobileControlState.Set(MobileAction.Jump, true);
                Call(cat, "TryJump");
                Check(body.velocity.y == 0f && Jumps(cat) == 2, "Busy rescue accepted jump");
                Set(cat, "busy", false);
            }
            Debug.Log("GAP_EDGE_VALIDATION_PASS approachTrials=" + trials +
                " airborneJump=pass noWallRefill=pass fallBoundary=pass busyRescue=pass");
            UnityEngine.Object.DestroyImmediate(material);
        }
        finally
        {
            MobileControlState.Reset();
            Physics2D.simulationMode = previousSimulation;
            DisplaySizeSettings.SetLarge(previousLarge);
            GameplayMode.Select(previousMode);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
