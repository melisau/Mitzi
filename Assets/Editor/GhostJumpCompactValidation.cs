using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GhostJumpCompactValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Hidden).SetValue(obj, value);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Hidden).Invoke(obj, args);
    public static void RunAll()
    {
        Run();
        ProgressionAndGiantPitValidation.Run();
        ProgressionAndGiantPitValidation.RunCompactGoalAndGapRules();
    }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Isolated project required");
        GhostCatBuilder.Apply();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
        var ghost = catalog.GetCat("ghost");
        Check(ghost.jumpFrames.Length == 4 && ghost.jumpFrames.All(s => AssetDatabase.GetAssetPath(s).EndsWith("/ghost/jump-compact.png")), "Compact sheet not connected");
        Check(ghost.jumpApexSprite == null, "Legacy apex can interrupt compact jump");
        float width = ghost.idleSprite.bounds.size.x;
        Check(ghost.jumpFrames.All(s => s.bounds.size.x < width * 1.18f), "Airborne body stretches excessively");
        var png = new Texture2D(2, 2); png.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(ghost.jumpFrames[0])));
        Check(png.GetPixel(0, 0).a == 0 && png.GetPixels32().Count(p => p.a == 0) > png.width * png.height / 3, "Missing alpha");
        UnityEngine.Object.DestroyImmediate(png);
        var root = new GameObject("GhostJumpCat"); var cat = root.AddComponent<CatController>(); Call(cat, "Awake");
        var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>(); var animator = visual.AddComponent<Animator>(); cat.BindVisual(visual.transform, null); cat.ApplyCat(ghost);
        cat.PlaceAtSpawn(new Vector2(0, 4)); animator.Rebind(); animator.Play("Jump", 0, 0); animator.Update(0);
        Set(cat, "intentionalJump", true); Set(cat, "jumpAnimationPlaying", true);
        var body = root.GetComponent<Rigidbody2D>(); var position = body.position;
        var collider = root.GetComponent<CircleCollider2D>(); float radius = collider.radius;
        int stages = 0;
        foreach (float speed in new[] { 7.3f, 5f, 3f, 1f, 0f, -1f, -4f, -8f })
        {
            body.velocity = new Vector2(2.55f, speed); var velocity = body.velocity;
            Call(cat, "ApplyAirbornePose");
            Check(renderer.sprite == ghost.jumpFrames[speed > 4.5f ? 1 : 2], "Wrong live airborne pose");
            var smoother = visual.GetComponent<GhostWalkSmoother>(); smoother.Refresh(0);
            Check(!smoother.IsRendering, "Walk blend overwrites jump");
            Check(body.position == position && body.velocity == velocity && collider.radius == radius, "Visual change altered jump physics");
            stages++;
        }
        UnityEngine.Object.DestroyImmediate(root);
        string folder = Environment.GetEnvironmentVariable("MITZI_PROGRESSION_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-progression");
        Directory.CreateDirectory(folder);
        var camera = new GameObject("GhostJumpCamera").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 4.7f;
        camera.transform.position = new Vector3(0, 1, -10); camera.backgroundColor = new Color(.29f, .38f, .40f); camera.clearFlags = CameraClearFlags.SolidColor;
        var old = AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/AnimalsRealistic/Cats/ghost/jump.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
        for (int row = 0; row < 2; row++)
        for (int column = 0; column < 3; column++)
        {
            var sr = new GameObject((row == 0 ? "Before" : "After") + column).AddComponent<SpriteRenderer>();
            sr.sprite = row == 0 ? old[Mathf.Min(column + 1, 2)] : ghost.jumpFrames[Mathf.Min(column + 1, 2)];
            sr.transform.position = new Vector3((column - 1) * 5.4f, row == 0 ? 1.4f : -2.8f, 0);
        }
        var rt = new RenderTexture(1600, 900, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
        var result = new Texture2D(1600, 900, TextureFormat.RGB24, false); result.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); result.Apply();
        File.WriteAllBytes(Path.Combine(folder, "ghost-airborne-before-after.png"), result.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(result);
        Debug.Log($"GHOST_COMPACT_JUMP_PASS sprites=4 airborneStages={stages} legacyApex=disabled physics=unchanged alpha=preserved");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
