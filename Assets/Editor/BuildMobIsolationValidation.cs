using System;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Drawing;
using PawPath.Gameplay;
using PawPath.Levels;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildMobIsolationValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
    static void State(GameFlow flow, GameFlowState state) => Call(flow, "TransitionTo", state);
    static T Mob<T>(string name) where T : Component
    {
        var root = new GameObject(name);
        root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        root.AddComponent<BoxCollider2D>().isTrigger = true;
        var mob = root.AddComponent<T>();
        var gate = root.GetComponent<MobSimulationGate>();
        Check(gate != null, name + " missing simulation gate");
        // Edit-mode validation invokes lifecycle explicitly; play mode does so automatically.
        Call(gate, "OnEnable");
        return mob;
    }

    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project.");
        float previousTimeScale = Time.timeScale;
        var previousSimulation = Physics2D.simulationMode;
        var previousMode = GameplayMode.Current;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics2D.simulationMode = SimulationMode2D.Script;
            GameplayMode.Select(GameplayPlayMode.Drawing);
            var flow = new GameObject("BuildMobTestFlow").AddComponent<GameFlow>();
            Call(flow, "Awake");
            State(flow, GameFlowState.DrawingPreparation);
            var draw = new GameObject("BuildMobTestCamera", typeof(Camera)).AddComponent<LineDraw>();
            Call(draw, "Awake");
            draw.ResetInk(20);
            var cat = new GameObject("BuildMobTestCat").AddComponent<CatController>();
            Call(cat, "Awake");
            cat.PlaceAtSpawn(new Vector2(100, 0));

            var texture = new Texture2D(16, 16);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 16);
            var idle = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 16);
            var dog = Mob<DogObstacle>("BuildDog");
            dog.transform.position = new Vector3(100, 0);
            var visual = new GameObject("DogVisual").AddComponent<SpriteRenderer>();
            visual.transform.SetParent(dog.transform, false);
            visual.sprite = sprite;
            dog.Configure(visual, new[] { sprite }, 2, 80, 120, 0, 2, 0, .1f, null, 1, idle);
            var bird = Mob<FlyingBirdCollectable>("BuildBird");
            bird.transform.position = new Vector3(100, 3);
            var birdVisual = bird.gameObject.AddComponent<SpriteRenderer>(); birdVisual.sprite = sprite;
            bird.Configure(birdVisual, new[] { sprite }, 2, 80, .7f, 1);
            var hazard = Mob<FlyingBirdObstacle>("BuildHazard");
            hazard.transform.position = new Vector3(100, 4);
            var hazardVisual = hazard.gameObject.AddComponent<SpriteRenderer>(); hazardVisual.sprite = sprite;
            hazard.Configure(hazardVisual, new[] { sprite }, 2, 80, 120, .7f);

            // Add a real player path above the dog's feet. Idle terrain resampling used
            // to move the dog onto it even with timeScale == 0.
            Call(draw, "BeginStroke", new Vector2(99, 2));
            Call(draw, "AppendPoint", new Vector2(101, 2));
            draw.FinishCurrentStroke();
            Physics2D.SyncTransforms();
            var path = UnityEngine.Object.FindObjectOfType<PathSurface>();
            var pathCollider = path.GetComponent<EdgeCollider2D>();
            var originalPoints = pathCollider.points;
            Vector3 dogPosition = dog.transform.position, birdPosition = bird.transform.position,
                hazardPosition = hazard.transform.position;
            foreach (GameFlowState state in new[] { GameFlowState.DrawingPreparation, GameFlowState.InfoOverlay,
                GameFlowState.Pause, GameFlowState.RescueMeeting, GameFlowState.GameOver })
            {
                State(flow, state);
                for (int frame = 0; frame < 30; frame++)
                {
                    Call(dog, "Update"); Call(bird, "Update"); Call(hazard, "Update");
                    Call(dog, "OnTriggerEnter2D", cat.GetComponent<Collider2D>());
                    Call(bird, "OnTriggerEnter2D", cat.GetComponent<Collider2D>());
                    // Null is safe only if collision handling returns before touching it.
                    Call(hazard, "OnCollisionEnter2D", new object[] { null });
                    Physics2D.Simulate(.02f);
                }
                Check(dog.transform.position == dogPosition && bird.transform.position == birdPosition &&
                    hazard.transform.position == hazardPosition, "Mob moved outside gameplay: " + state);
                Check(flow.State == state && bird.gameObject.activeSelf, "Mob interaction escaped pause: " + state);
                foreach (var mob in new Component[] { dog, bird, hazard })
                    Check(!mob.GetComponent<Rigidbody2D>().simulated, "Mob physics active in " + state);
                Check(pathCollider.enabled && pathCollider.points.Length == originalPoints.Length,
                    "Build path disabled or changed");
                for (int i = 0; i < originalPoints.Length; i++)
                    Check(pathCollider.points[i] == originalPoints[i], "Build path moved");
            }

            State(flow, GameFlowState.DrawingPreparation);
            draw.SetEraser(); Call(draw, "EraseAt", new Vector2(100, 2));
            Check(Mathf.Abs(draw.InkLeft - 20) < .0001f, "Player erasing blocked by mob suspension");
            draw.SetBrushType(PathSurfaceType.Normal);
            Call(draw, "BeginStroke", new Vector2(105, 1));
            Call(draw, "AppendPoint", new Vector2(107, 1));
            draw.FinishCurrentStroke();
            Check(draw.HasDrawnPath && draw.InkLeft < 20, "Player drawing blocked by mob suspension");

            // Even a solid kinematic mob cannot push a movable part during Build.
            var pusher = new GameObject("SolidMobFixture");
            var pusherBody = pusher.AddComponent<Rigidbody2D>(); pusherBody.bodyType = RigidbodyType2D.Kinematic;
            pusher.AddComponent<BoxCollider2D>();
            var pusherGate = pusher.AddComponent<MobSimulationGate>(); Call(pusherGate, "OnEnable");
            var part = new GameObject("MovablePartFixture"); part.transform.position = new Vector3(1.05f, 0);
            var partBody = part.AddComponent<Rigidbody2D>(); partBody.gravityScale = 0;
            part.AddComponent<BoxCollider2D>();
            pusherBody.velocity = Vector2.right * 3;
            Physics2D.SyncTransforms();
            for (int i = 0; i < 30; i++) Physics2D.Simulate(.02f);
            Check(Mathf.Abs(partBody.position.x - 1.05f) < .001f, "Mob pushed part in Build");
            part.transform.position = new Vector3(1.2f, 0); Physics2D.SyncTransforms();
            Check(Mathf.Abs(partBody.position.x - 1.2f) < .001f, "Player cannot reposition part");
            State(flow, GameFlowState.Gameplay);
            foreach (var mob in new Component[] { dog, bird, hazard })
                Check(mob.GetComponent<Rigidbody2D>().simulated, "Gameplay physics not restored");
            pusherBody.velocity = Vector2.right * 3;
            for (int i = 0; i < 30; i++) Physics2D.Simulate(.02f);
            Check(partBody.position.x > 1.3f, "Gameplay collision rules not restored");
            Call(bird, "OnTriggerEnter2D", cat.GetComponent<Collider2D>());
            Check(!bird.gameObject.activeSelf, "Gameplay bird collection disabled");
            Call(dog, "OnTriggerEnter2D", cat.GetComponent<Collider2D>());
            Check(flow.State == GameFlowState.GameOver, "Gameplay dog contact disabled");

            // Repeated preparation/resume cycles preserve a deliberately disabled body.
            State(flow, GameFlowState.Gameplay);
            pusherBody.simulated = false;
            for (int cycle = 0; cycle < 20; cycle++)
            {
                State(flow, GameFlowState.DrawingPreparation);
                State(flow, GameFlowState.Gameplay);
                Check(!pusherBody.simulated, "Resume enabled an intentionally disabled body");
            }
            draw.ClearStrokes();
            UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(idle);
            UnityEngine.Object.DestroyImmediate(texture);
            Debug.Log("BUILD_MOB_ISOLATION_VALIDATION_PASS states=5 mobs=3 edit=pass physicsPush=pass gameplay=pass cycles=20");
        }
        finally
        {
            UnityEngine.Object.FindObjectOfType<LineDraw>()?.ClearStrokes();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics2D.simulationMode = previousSimulation;
            Time.timeScale = previousTimeScale;
            GameplayMode.Select(previousMode);
        }
    }
}
