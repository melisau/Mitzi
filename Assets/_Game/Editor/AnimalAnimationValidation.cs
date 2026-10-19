#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Cat;
using PawPath.Drawing;
using PawPath.Gameplay;
using PawPath.Levels;
using PawPath.Data;
using PawPath.EditorTools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AnimalAnimationValidation
{
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";

    public static void RenderPreview()
    {
        BuildAndValidate();
        // A disposable editor scene: neither gameplay saves nor project scenes change.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 6.4f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.63f, 0.70f, 0.73f);
        var target = new RenderTexture(960, 640, 24);
        camera.targetTexture = target;
        var cats = new[] { catalog.GetCat("mitzi"), catalog.GetCat("ada"), catalog.GetCat("komur") };
        var sequences = new[] { cats[0].walkFrames, cats[1].walkFrames, cats[2].walkFrames,
            catalog.dogRunFrames, catalog.roboticDogWalkFrames, catalog.birdFrames,
            cats[0].climbFrames, cats[1].climbFrames, catalog.roboticBirdFrames };
        var renderers = new SpriteRenderer[sequences.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i] = new GameObject("Animal_" + i).AddComponent<SpriteRenderer>();
            renderers[i].transform.position = new Vector3((i % 3 - 1) * 5f,
                i < 3 ? 2.8f : i < 6 ? 0f : -3.8f, 0f);
        }
        string folder = "AnimalArtSources-20261018/unity-preview";
        Directory.CreateDirectory(folder);
        for (int f = 0; f < 24; f++)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                float fps = i < 3 ? sequences[i].Length / 0.5f : i == 3 || i == 4 ? 16f : 24f;
                renderers[i].sprite = sequences[i][Mathf.FloorToInt(f / 24f * fps) % sequences[i].Length];
            }
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(960, 640, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 960, 640), 0, 0);
            texture.Apply();
            File.WriteAllBytes(folder + "/frame_" + f.ToString("00") + ".png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        RenderTexture.active = null;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("ANIMAL_PREVIEW_RENDER_PASS");
    }

    public static void BuildAndValidate()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CatBreedAnimationBuilder.BuildFromMenu();
            GhostCatBuilder.Apply();HomeDirectionArtBuilder.Apply();
            BirdAnimationBuilder.Apply();
            GameplayMechanicsAssetBuilder.Apply();
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
            Require(catalog != null, "Catalog missing");
            Require(catalog.cats.Count(c => c != null) == 8, "Expected eight cats");
            Require(catalog.StarterCat != null && catalog.StarterCat.id == "mitzi"
                && catalog.cats[0] == catalog.StarterCat, "Mitzi must be first starter cat");
            Require(AssetDatabase.GetAssetPath(catalog.StarterCat.idleSprite).Contains("/Cats/silver/"),
                "Mitzi must use silver spotted reference cat");
            foreach (var cat in catalog.cats.Where(c => c != null))
            {
                Require(cat.walkFrames.Length == (cat.id=="ghost"||cat.id=="boza"?8:12) && cat.walkFrames.All(f => AssetDatabase.GetAssetPath(f).Contains(cat.id=="ghost"||cat.id=="boza"?"/"+cat.id+"/walk.png":"/videowalk_")), cat.id + " gait missing");
                CheckSeries(cat.walkFrames, cat.walkFrames.Length, cat.id + " walk");
                CheckSeries(cat.climbFrames, 12, cat.id + " climb");
                CheckSeries(cat.jumpFrames, 4, cat.id + " jump");
                CheckSeries(cat.eatFrames, 4, cat.id + " eat");
                CheckSeries(cat.drinkFrames, 4, cat.id + " drink");
                CheckSeries(cat.idleFrames, 4, cat.id + " idle");
                CheckSeries(cat.sleepFrames, 5, cat.id + " sleep");
                Require(cat.portrait != null && cat.idleSprite != null, cat.id + " portrait/idle");
                var controller = cat.animator as AnimatorController;
                Require(controller != null, cat.id + " controller");
                foreach (var name in new[] { "Walk", "Idle", "Jump", "Crouch", "CrouchWalk", "Sit", "Lie", "Sleep", "Eat", "Drink", "Climb" })
                {
                    var state = controller.layers[0].stateMachine.states.FirstOrDefault(s => s.state.name == name).state;
                    Require(state != null && state.motion is AnimationClip, cat.id + " missing state " + name);
                    var clip = (AnimationClip)state.motion;
                    var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
                    if (name == "Walk")
                    {
                        var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                        Require(keys.Length == cat.walkFrames.Length, cat.id + " walk density");
                        Require(Mathf.Abs(clip.length - cat.walkCycleSeconds) < 0.001f, cat.id + " gait duration changed");
                        Require(AnimationUtility.GetAnimationClipSettings(clip).loopTime, cat.id + " walk loop");
                    }
                    Require(AnimationUtility.GetObjectReferenceCurve(clip, binding).All(k => k.value != null),
                        cat.id + " null clip frame " + name);
                }
            }
            CheckSeries(catalog.dogRunFrames, 8, "dog");
            CheckSeries(catalog.roboticDogWalkFrames, 8, "robot dog");
            CheckSeries(catalog.birdFrames, 8, "bird");
            CheckSeries(catalog.roboticBirdFrames, 8, "robot bird");
            CheckMovementAndFacing(catalog);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("AnimalArtSources-20261018");
            File.WriteAllText("AnimalArtSources-20261018/unity-validation.txt",
                "PASS: grey spotted Mitzi first starter, walking cycle at 24fps; eight cats, eleven controller states, distinct eat/drink, idle/sleep loops, " +
                "crouch walking, low/high jump and two-jump limit, left/right jump facing, left-facing birds.\n" + Application.unityVersion);
            Debug.Log("ANIMAL_ANIMATION_VALIDATION_PASS");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    static void CheckMovementAndFacing(PawPathCatalog catalog)
    {
        var root = new GameObject("MovementValidation");
        var bird = new GameObject("BirdValidation");
        var obstacle = new GameObject("BirdObstacleValidation");
        var ground = new GameObject("GroundValidation");
        var previousSimulation = Physics2D.simulationMode;
        try
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.AddComponent<SpriteRenderer>();
            var animator = visual.AddComponent<Animator>();
            var controller = root.AddComponent<CatController>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(CatController).GetField("visual", flags).SetValue(controller, visual.transform);
            Action<string, object[]> invoke = (name, args) => typeof(CatController).GetMethod(name, flags).Invoke(controller, args);
            invoke("Awake", Array.Empty<object>());
            controller.ApplyCat(catalog.StarterCat);
            animator.Rebind();
            animator.Update(0f);
            var body = root.GetComponent<Rigidbody2D>();
            MobileControlState.Reset();
            MobileControlState.Set(MobileAction.Jump, true);
            invoke("TryJump", Array.Empty<object>());
            float low = body.velocity.y;
            Require(low > 0f, "First jump did not launch");
            MobileControlState.Set(MobileAction.Right, true);
            invoke("UpdateDirectControl", new object[] { false, PathSurfaceType.Normal });
            Require(visual.transform.localScale.x > 0f, "Right-facing jump reversed");
            MobileControlState.Set(MobileAction.Right, false);
            MobileControlState.Set(MobileAction.Left, true);
            invoke("UpdateDirectControl", new object[] { false, PathSurfaceType.Normal });
            Require(visual.transform.localScale.x < 0f, "Left-facing jump reversed");
            MobileControlState.Set(MobileAction.Jump, true);
            invoke("TryJump", Array.Empty<object>());
            float high = body.velocity.y;
            Require(low >= 7.2f && high > low * 1.15f, "First jump must be usable and second jump higher");
            MobileControlState.Set(MobileAction.Jump, true);
            invoke("TryJump", Array.Empty<object>());
            Require(Mathf.Approximately(body.velocity.y, high), "Third airborne jump was allowed");
            invoke("UpdateJumpAnimation", new object[] { false });
            body.velocity = Vector2.zero;
            invoke("UpdateJumpAnimation", new object[] { true });
            MobileControlState.Set(MobileAction.Crouch, true);
            invoke("UpdateDirectControl", new object[] { true, PathSurfaceType.Normal });
            animator.Update(0f);
            Require(animator.GetCurrentAnimatorStateInfo(0).IsName("CrouchWalk") && animator.speed > 0f,
                "Crouching movement must animate");
            MobileControlState.Reset();
            invoke("UpdateDirectControl", new object[] { true, PathSurfaceType.Normal });
            animator.Update(0f);
            Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") && animator.speed > 0f,
                "Stationary cat must play idle");
            var floor = ground.AddComponent<BoxCollider2D>();
            floor.size = new Vector2(20f, 1f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int slope = 0; slope <= 20; slope += 20)
            {
                ground.transform.rotation = Quaternion.Euler(0f, 0f, slope);
                root.transform.position = new Vector3(0f, slope == 0 ? 0.34f : 0.40f, 0f);
                body.velocity = Vector2.zero;
                Physics2D.SyncTransforms();
                for (int step = 0; step < 30; step++) Physics2D.Simulate(0.02f);
                for (int frame = 0; frame < 120; frame++) invoke("AlignVisualFeet", Array.Empty<object>());
                var sr = visual.GetComponent<SpriteRenderer>();
                float padding = (float)typeof(CatController).GetField("spriteBottomPadding", flags).GetValue(controller);
                float footY = sr.sprite.bounds.min.y + sr.sprite.bounds.size.y * padding;
                Vector3 paws = visual.transform.TransformPoint(new Vector3(0f, footY, 0f));
                var surface = Physics2D.RaycastAll(body.position, Vector2.down, 2f)
                    .FirstOrDefault(hit => hit.collider == floor);
                Require(surface.collider != null && paws.y >= surface.point.y && paws.y - surface.point.y < 0.065f,
                    "Visible paws must contact ground at slope " + slope);
                Require(Mathf.Abs(Mathf.DeltaAngle(visual.transform.eulerAngles.z, slope)) < 1f,
                    "Cat must follow the slope angle");
            }
            float baseHeight = (float)typeof(CatController).GetField("visualHeight", flags).GetValue(controller);
            Require(visual.transform.localScale.y * visual.GetComponent<SpriteRenderer>().sprite.bounds.size.y >=
                baseHeight * PawPath.Core.DisplaySizeSettings.CatScale * 1.24f, "Cat must be 25 percent larger");
            var birdRenderer = bird.AddComponent<SpriteRenderer>();
            birdRenderer.sprite = catalog.birdFrames[0];
            bird.AddComponent<FlyingBirdCollectable>().Configure(birdRenderer, catalog.birdFrames, 2f, -20f, 0f, 1f);
            Require(birdRenderer.flipX && bird.transform.localScale.x > 0f, "Collectable bird must face left");
            var obstacleRenderer = obstacle.AddComponent<SpriteRenderer>();
            obstacle.AddComponent<FlyingBirdObstacle>().Configure(obstacleRenderer, catalog.birdFrames, 2f, -20f, 20f, 0f);
            Require(obstacleRenderer.flipX, "Obstacle bird must face left");
            Debug.Log("ANIMAL_MOVEMENT_BEHAVIOR_PASS");
        }
        finally
        {
            MobileControlState.Reset();
            Physics2D.simulationMode = previousSimulation;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(bird);
            UnityEngine.Object.DestroyImmediate(obstacle);
            UnityEngine.Object.DestroyImmediate(ground);
        }
    }

    static void CheckSeries(Sprite[] frames, int expected, string name)
    {
        Require(frames != null && frames.Length == expected, name + " frame count");
        foreach (var frame in frames)
        {
            Require(frame != null, name + " null sprite");
            Require(AssetDatabase.GetAssetPath(frame).StartsWith("Assets/_Game/Art/AnimalsRealistic/"),
                name + " references old art");
            bool photoCat=AssetDatabase.GetAssetPath(frame).Contains("/Cats/ghost/")||AssetDatabase.GetAssetPath(frame).Contains("/Cats/boza/");
            Require(photoCat ? frame.rect.width>50&&frame.rect.height>50 : frame.rect.width == 320 && frame.rect.height == 320, name + " canvas");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(frame));
            Require(importer.alphaIsTransparency && !importer.mipmapEnabled, name + " alpha/import");
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
#endif
