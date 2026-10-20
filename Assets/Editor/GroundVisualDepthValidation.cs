using System;
using System.IO;
using System.Linq;
using PawPath.Core;
using PawPath.Data;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GroundVisualDepthValidation
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Render(Camera camera, SpriteRenderer background, string file)
    {
        int width = Mathf.RoundToInt(720 * camera.aspect);
        background.transform.position = new Vector3(camera.transform.position.x, 0, 1);
        background.transform.localScale = Vector3.one * Mathf.Max(8.9f / background.sprite.bounds.size.y,
            8.9f * camera.aspect / background.sprite.bounds.size.x);
        var rt = new RenderTexture(width, 720, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
        var image = new Texture2D(width, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, 720), 0, 0); image.Apply(); File.WriteAllBytes(file, image.EncodeToPNG());
        RenderTexture.active = null; camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
    }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project");
        var oldDifficulty = GameDifficulty.Selected; bool oldAutomatic = GameDifficulty.IsAutomatic;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameDifficulty.Select(DifficultyLevel.Easy);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var course = new GameObject("GroundDepthCourse").AddComponent<LevelCourseBuilder>();
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 4.45f;
            var background = new GameObject("Background").AddComponent<SpriteRenderer>(); background.sortingOrder = -5;
            string output = Environment.GetEnvironmentVariable("MITZI_GROUND_DEPTH_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-ground-depth");
            Directory.CreateDirectory(output); int layouts = 0, columns = 0, gapChecks = 0;
            foreach (bool forest in new[] { false, true })
            foreach (int level in new[] { 1, 12, 21 })
            foreach (float aspect in new[] { 16f / 9f, 19.5f / 9f, 4f / 3f })
            {
                course.BindVisuals(forest ? catalog.forestGapSprite : catalog.roadGapSprite,
                    forest ? catalog.forestMoundSprite : catalog.moundSprite,
                    forest ? catalog.forestGroundUnderfillSprite : catalog.roadPlatformSprite);
                course.Build(level); Physics2D.SyncTransforms(); camera.aspect = aspect;
                Check(course.ValidateCourseGeometry() == null, "Visual change invalidated course geometry");
                var roads = course.GetComponentsInChildren<BoxCollider2D>().Where(c => c.name == "Road").OrderBy(c => c.bounds.min.x).ToArray();
                var soil = course.GetComponentsInChildren<SpriteRenderer>().Where(r => r.name == "RoadSoilColumn").ToArray();
                Check(roads.Length == soil.Length && soil.Length > 1, "Missing earth beneath road");
                foreach (var road in roads)
                {
                    var fill = soil.Single(s => Mathf.Abs(s.bounds.center.x - road.bounds.center.x) < .001f);
                    Check(Mathf.Abs(fill.bounds.min.x - road.bounds.min.x) < .001f && Mathf.Abs(fill.bounds.max.x - road.bounds.max.x) < .001f,
                        "Soil widens a platform or covers a gap");
                    Check(fill.bounds.min.y < -7.99f && fill.bounds.max.y < road.bounds.max.y - .5f, "Soil depth/top unsafe");
                    Check(fill.GetComponent<Collider2D>() == null && fill.sortingOrder == 0, "Soil changes physics/sorting");
                    Check(Mathf.Abs(road.bounds.max.y - (-1.274f)) < .002f && Mathf.Abs(road.bounds.size.y - 1.152f) < .002f,
                        "Walking height or collider changed");
                    columns++;
                }
                for (int i = 1; i < roads.Length; i++)
                {
                    float left = roads[i - 1].bounds.max.x, right = roads[i].bounds.min.x;
                    if (right - left < .01f) continue;
                    float center = (left + right) * .5f;
                    Check(!soil.Any(s => s.bounds.Contains(new Vector3(center, -4.4f, 0))), "Deep earth bridges a pit"); gapChecks++;
                }
                Check(course.transform.Find("GeneratedCourse/RoadSlabShadow") == null, "Floating slab shadow remains");
                background.sprite = forest ? catalog.forestBackground : catalog.gameplayBackground;
                camera.transform.position = new Vector3(level == 12 ? course.GoalX - 6.15f : 0, 0, -10);
                if (aspect > 1.7f && (level == 1 || level == 12))
                    Render(camera, background, Path.Combine(output, (forest ? "forest" : "street") + "-level-" + level + "-" + Mathf.RoundToInt(aspect * 100) + ".png"));
                layouts++;
            }
            Debug.Log($"GROUND_VISUAL_DEPTH_PASS layouts={layouts} columns={columns} openGaps={gapChecks} walkingHeight=unchanged colliders=unchanged output={output}");
        }
        finally
        {
            GameDifficulty.Select(oldDifficulty); if (oldAutomatic) GameDifficulty.SelectAutomatic();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
