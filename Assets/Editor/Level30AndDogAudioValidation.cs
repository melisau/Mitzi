using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Audio;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Gameplay;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Level30AndDogAudioValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object o, string name) => o.GetType().GetField(name, Hidden).GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Hidden).SetValue(o, value);
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Hidden).Invoke(o, args);
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    static void State(GameFlow flow, GameFlowState state) => Set(flow, "<State>k__BackingField", state);
    static void Render(Camera camera, string path)
    {
        var rt = new RenderTexture(1600, 900, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
        var image = new Texture2D(1600, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG()); RenderTexture.active = null; camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
    }
    static RaycastHit2D Ground(float x, float y) => Physics2D.RaycastAll(new Vector2(x, y + 3), Vector2.down, 10)
        .FirstOrDefault(h => h.collider != null && !h.collider.isTrigger && h.collider.attachedRigidbody == null && h.normal.y > .5f);
    static void TickDog(DogObstacle dog)
    {
        if (dog.transform.position.x < (float)Get(dog, "leftBound") || dog.transform.position.x > (float)Get(dog, "rightBound"))
        { UnityEngine.Object.DestroyImmediate(dog.gameObject); return; }
        Call(dog, "Tick", .02f);
    }
    static float VisibleHeight(SpriteRenderer renderer)
    {
        var sprite = renderer.sprite; var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
        var pixels = texture.GetPixels32(); Rect rect = sprite.rect; int min = (int)rect.yMax, max = (int)rect.y;
        for (int y = (int)rect.y; y < rect.yMax; y++) for (int x = (int)rect.x; x < rect.xMax; x++)
            if (pixels[y * texture.width + x].a > 96) { min = Mathf.Min(min, y); max = Mathf.Max(max, y); }
        UnityEngine.Object.DestroyImmediate(texture); return (max - min + 1) / sprite.pixelsPerUnit * Mathf.Abs(renderer.transform.lossyScale.y);
    }
    public static void RunSuite()
    {
        CrouchControlRuleValidation.Run();
        DogGapAndFurnitureValidation.Run();
        Run();
    }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Isolated project required");
        var simulation = Physics2D.simulationMode; var oldMode = GameplayMode.Current;
        var oldDifficulty = GameDifficulty.Selected; bool automatic = GameDifficulty.IsAutomatic, largeBefore = DisplaySizeSettings.Large;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics2D.simulationMode = SimulationMode2D.Script;
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var flow = new GameObject("Flow").AddComponent<GameFlow>(); Call(flow, "Awake"); flow.BindCatalog(catalog); State(flow, GameFlowState.Gameplay);
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.orthographic = true; camera.orthographicSize = 4.45f; camera.aspect = 16f / 9;
            camera.transform.position = new Vector3(0, 0, -10);
            var course = new GameObject("Level30Course").AddComponent<LevelCourseBuilder>();
            string folder = Environment.GetEnvironmentVariable("MITZI_LEVEL30_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-level30"); Directory.CreateDirectory(folder);
            int routes = 0, passages = 0, patrols = 0;
            foreach (var difficulty in new[] { DifficultyLevel.Easy, DifficultyLevel.Medium, DifficultyLevel.Hard })
            foreach (bool large in new[] { false, true })
            foreach (var mode in new[] { GameplayPlayMode.Drawing, GameplayPlayMode.DirectControl })
            {
                DisplaySizeSettings.SetLarge(large); GameDifficulty.Select(difficulty); GameplayMode.Select(mode); State(flow, GameFlowState.Gameplay);
                course.BindVisuals(catalog.roadGapSprite, catalog.moundSprite, catalog.roadPlatformSprite, catalog.streetMoundHighSprite,
                    dogs: catalog.dogRunFrames, tree: catalog.climbTreeSprite, climbingFrames: catalog.climbingCatFrames);
                course.BindCrouchObstacle(catalog.streetCrouchObstacle, catalog.CrouchUpperForTheme(0)); course.BindDogIdle(catalog.dogIdleSprite); course.Build(30);
                Physics2D.SyncTransforms(); Check(course.ValidateCourseGeometry() == null, "Bad level geometry");
                var roads = course.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "Road").ToArray();
                foreach (var p in course.GetComponentsInChildren<CrouchPassage>())
                {
                    var road = roads.Single(r => r.bounds.min.x < p.Left && r.bounds.max.x > p.Right);
                    Check(p.Left - road.bounds.min.x >= 3.19f && road.bounds.max.x - p.Right >= 3.19f, "Passage has no jump runway"); passages++;
                }
                var catRoot = new GameObject("RouteCat"); var cat = catRoot.AddComponent<CatController>(); Call(cat, "Awake");
                var visual = new GameObject("Visual"); visual.transform.SetParent(catRoot.transform, false); visual.AddComponent<SpriteRenderer>(); visual.AddComponent<Animator>();
                cat.BindVisual(visual.transform, null); cat.ApplyCat(catalog.GetCat("ghost")); cat.PlaceAtSpawn(new Vector2(-6.2f, course.CatSpawnY));
                var body = catRoot.GetComponent<Rigidbody2D>(); body.interpolation = RigidbodyInterpolation2D.None;
                MobileControlState.Reset(); MobileControlState.Set(MobileAction.Right, true);
                int stalled = 0; float lastX = body.position.x;
                var trace = new System.Collections.Generic.List<string>();
                for (int frame = 0; frame < 4500; frame++)
                {
                    Physics2D.SyncTransforms();
                    var circle = catRoot.GetComponent<CircleCollider2D>(); float foot = circle.bounds.min.y;
                    var under = Ground(body.position.x, foot); var ahead = Ground(body.position.x + .65f, foot);
                    bool supported = under.collider != null && Mathf.Abs(under.point.y - foot) < .18f && body.velocity.y <= .15f;
                    bool gapAhead = ahead.collider == null;
                    var edge = Ground(body.position.x + .24f, foot);
                    bool gapAtEdge = edge.collider == null;
                    bool highAhead = ahead.collider != null && ahead.point.y > foot + .2f;
                    var dogs = course.GetComponentsInChildren<DogObstacle>();
                    bool dogAhead = dogs.Any(d => d != null && d.transform.position.x > body.position.x && d.transform.position.x - body.position.x < 3f);
                    bool crouch = CrouchPassage.NeedsCrouch(cat, .45f);
                    MobileControlState.Set(MobileAction.Crouch, mode == GameplayPlayMode.DirectControl && crouch);
                    int remaining = (int)Get(cat, "jumpsRemaining");
                    bool jump = !crouch && ((supported && (gapAtEdge || highAhead || dogAhead)) ||
                        (!supported && remaining == 1 && body.velocity.y < 2f && (gapAhead || highAhead || dogAhead)));
                    if (jump) MobileControlState.Set(MobileAction.Jump, true);
                    trace.Add($"{frame},{body.position.x:F3},{body.position.y:F3},{body.velocity.y:F3},{supported},{crouch},{remaining},{jump},{gapAhead},{highAhead},{dogAhead}");
                    foreach (var dog in dogs) if (dog != null) TickDog(dog);
                    Call(cat, "FixedUpdate"); Physics2D.Simulate(.02f); MobileControlState.Set(MobileAction.Jump, false);
                    if (body.position.x > course.GoalX) break;
                    if (body.position.x - lastX < .001f) stalled++; else stalled = 0; lastX = body.position.x;
                    if (body.position.y <= -3.5f || cat.IsBusy || flow.State != GameFlowState.Gameplay || stalled >= 160)
                        File.WriteAllLines(Path.Combine(folder, "failed-route.csv"), trace);
                    Check(body.position.y > -3.5f && !cat.IsBusy && flow.State == GameFlowState.Gameplay && stalled < 160,
                        $"Level30 route failed {difficulty} large={large} mode={mode} frame={frame} pos={body.position} state={flow.State} stalled={stalled}");
                }
                Check(body.position.x > course.GoalX, "Route did not reach goal"); routes++;
                UnityEngine.Object.DestroyImmediate(catRoot);
                // Long patrol: no dog may leap the same gap more than once.
                foreach (var dog in course.GetComponentsInChildren<DogObstacle>())
                {
                    Set(dog, "activated", true); Set(dog, "turnedToChase", true);
                    var starts = new System.Collections.Generic.Dictionary<int, int>(); bool wasJumping = false;
                    for (int frame = 0; frame < 9000 && dog != null; frame++)
                    {
                        TickDog(dog); if (dog == null) break;
                        bool jumping = (bool)Get(dog, "jumpingGap");
                        if (jumping && !wasJumping)
                        {
                            int launch = Mathf.RoundToInt((float)Get(dog, "jumpStartX") * 100);
                            starts.TryGetValue(launch, out int count); starts[launch] = count + 1;
                            Check(count == 0, "Dog repeats a gap leap forever");
                        }
                        wasJumping = jumping;
                    }
                    patrols++;
                }
            }
            Debug.Log($"LEVEL30_PLAYTHROUGH_PASS routes={routes} safePassages={passages} longPatrols={patrols}");
            ValidateAudioAndRobot(catalog, flow, camera, course, folder);
        }
        finally
        {
            Physics2D.simulationMode = simulation; GameplayMode.Select(oldMode); GameDifficulty.Select(oldDifficulty); if (automatic) GameDifficulty.SelectAutomatic();
            DisplaySizeSettings.SetLarge(largeBefore); MobileControlState.Reset(); Time.timeScale = 1;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
    static void ValidateAudioAndRobot(PawPathCatalog catalog, GameFlow flow, Camera camera, LevelCourseBuilder course, string folder)
    {
        State(flow, GameFlowState.Gameplay);
        var audio = new GameObject("Audio").AddComponent<CozyAudioManager>(); Call(audio, "Awake");
        Check(Resources.Load<AudioClip>("Audio/RobotDogBark").length >= 2 && Resources.Load<AudioClip>("Audio/RobotDogBark").length <= 3, "Robot bark duration");
        var sr = new GameObject("AudibleDog").AddComponent<SpriteRenderer>(); sr.sprite = catalog.dogIdleSprite; camera.transform.position = new Vector3(0, 0, -10);
        sr.transform.position = Vector3.zero;
        Check(CozyAudioManager.DogVisible(sr), "Visible dog rejected");
        foreach (var state in new[] { GameFlowState.DrawingPreparation, GameFlowState.Pause, GameFlowState.MainMenu })
        { State(flow, state); Check(!audio.TryPlayDogBark(sr, false), "Bark outside gameplay"); }
        State(flow, GameFlowState.Gameplay); sr.transform.position = new Vector3(1000, 0);
        Check(!audio.TryPlayDogBark(sr, false), "Offscreen bark"); sr.transform.position = Vector3.zero;
        var source = (AudioSource)Get(audio, "dogBark"); Check(source.volume <= .2f && !source.loop, "Bark loud/looped");
        foreach (bool robotic in new[] { false, true })
        {
            source.Stop(); Set(audio, "nextDogBarkTime", 0d);
            typeof(CozyAudioManager).GetProperty("EffectsMuted").SetValue(audio, false);
            Check(audio.TryPlayDogBark(sr, robotic), "Visible bark not played");
            Check(source.clip.name == (robotic ? "RobotDogBark" : "DogBark"), "Wrong dog sound");
            Check(!audio.TryPlayDogBark(sr, robotic), "Overlapping bark");
            State(flow, GameFlowState.Pause); Call(audio, "Update"); Check(!source.isPlaying, "Pause keeps bark playing"); State(flow, GameFlowState.Gameplay);
        }
        UnityEngine.Object.DestroyImmediate(sr.gameObject);
        foreach (bool large in new[] { false, true })
        {
            DisplaySizeSettings.SetLarge(large);
            course.BindVisuals(null, null, catalog.cyberGroundTile, dogs: catalog.roboticDogWalkFrames, useCyberTheme: true, cyberObstacles: catalog.cyberObstacleSprites);
            course.BindCrouchObstacle(null); course.BindDogIdle(catalog.roboticDogIdleSprite); course.Build(30);
            var dog = course.GetComponentsInChildren<DogObstacle>().First();
            Check((float)Get(dog, "visualWidth") >= 3.6f, "Robot dog remains tiny");
            dog.transform.position = new Vector3(1.8f, -1.274f); Call(dog, "ShowIdle");
            var cat = new GameObject("SizeReference").AddComponent<CatController>(); Call(cat, "Awake");
            var vis = new GameObject("Visual"); vis.transform.SetParent(cat.transform, false); vis.AddComponent<SpriteRenderer>(); vis.AddComponent<Animator>(); cat.BindVisual(vis.transform, null); cat.ApplyCat(catalog.StarterCat); cat.PlaceAtSpawn(new Vector2(-1.8f, -.938f));
            var dogVisual = (SpriteRenderer)Get(dog, "renderer");
            float dogHeight = VisibleHeight(dogVisual);
            foreach (var definition in catalog.cats.Where(c => c != null))
            {
                cat.ApplyCat(definition); Check(dogHeight >= VisibleHeight(vis.GetComponent<SpriteRenderer>()), "Robot dog shorter than " + definition.id);
            }
            cat.ApplyCat(catalog.StarterCat);
            var floorSprite = course.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "CyberGroundVisual").sprite;
            foreach (var r in course.GetComponentsInChildren<SpriteRenderer>()) if (!r.transform.IsChildOf(dog.transform)) r.enabled = false;
            var floor = new GameObject("ComparisonFloor").AddComponent<SpriteRenderer>(); floor.sprite = floorSprite; floor.sortingOrder = 1;
            floor.drawMode = SpriteDrawMode.Tiled; floor.size = new Vector2(18, .82f); floor.transform.position = new Vector3(0, -1.684f);
            camera.transform.position = new Vector3(0, 0, -10); Render(camera, Path.Combine(folder, "robot-dog-scale-" + (large ? "large" : "normal") + ".png"));
            UnityEngine.Object.DestroyImmediate(cat.gameObject); UnityEngine.Object.DestroyImmediate(floor.gameObject);
        }
        Debug.Log("DOG_AUDIO_ROBOT_PASS robotSeconds=2.5 volume=0.2 onScreenOnly=true overlap=false pausedSilent=true robotSize=increased");
    }
}
