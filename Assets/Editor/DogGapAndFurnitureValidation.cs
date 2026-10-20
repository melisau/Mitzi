using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Content;
using PawPath.Core;
using PawPath.Data;
using PawPath.Hub;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DogGapAndFurnitureValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object obj, string field) => obj.GetType().GetField(field, Hidden).GetValue(obj);
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Hidden).SetValue(obj, value);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Hidden).Invoke(obj, args);
    static void Check(bool value, string why) { if (!value) throw new Exception(why); }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Isolated project required");
        string primary = PlayerPrefs.GetString("PawPath.Save.v1", ""), backup = PlayerPrefs.GetString("PawPath.Save.v1.backup", "");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var flow = new GameObject("Flow").AddComponent<GameFlow>(); Call(flow, "Awake");
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset"); flow.BindCatalog(catalog);
        var savedCopies = SaveService.Data.furniture;
        try
        {
            typeof(GameFlow).GetField("<State>k__BackingField", Hidden).SetValue(flow, GameFlowState.Gameplay);
            int trials = 0, retreats = 0, jumps = 0, coursePolicies = 0;
            foreach (float width in new[] { 1.75f, 2.3f, 2.6f, 3.2f, 5.5f, 9.1f })
            foreach (int policy in new[] { -1, 0, 1 })
            foreach (int level in new[] { 1, 2 })
            foreach (int direction in new[] { -1, 1 })
            {
                var root = new GameObject("DogRuleTrial");
                var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform, false);
                var sr = visual.AddComponent<SpriteRenderer>(); sr.sprite = catalog.dogIdleSprite;
                const float clearance = 2.75f * .5f + .12f;
                float launch = (direction > 0 ? 0 : width) - direction * clearance;
                root.transform.position = new Vector2(launch - direction * .02f, 0);
                var dog = root.AddComponent<DogObstacle>();
                dog.Configure(sr, catalog.dogRunFrames, 2, -100, 100, 0, 2.75f, .22f, .1f,
                    new[] { new Vector2(0, width) }, level, catalog.dogIdleSprite, policy < 0 ? null : new[] { policy == 1 });
                Set(dog, "direction", direction); Set(dog, "activated", true); Set(dog, "turnedToChase", true);
                Call(dog, "TryStartGapAction", .1f);
                bool expected = width <= 2.601f && policy != 0 && level == 2;
                Check((bool)Get(dog, "jumpingGap") == expected, $"Wrong dog gap action width={width} policy={policy} level={level}");
                Check(Mathf.Abs(root.transform.position.x - launch) < .001f, "Dog steps into pit before decision");
                if (expected)
                {
                    for (int i = 0; i < 30; i++) Call(dog, "AdvanceGapJump", .1f);
                    float destination = (direction > 0 ? width : 0) + direction * clearance;
                    Check(Mathf.Abs(root.transform.position.x - destination) < .001f, "Narrow-gap leap cannot land"); jumps++;
                }
                else
                {
                    Check((bool)Get(dog, "gapRetreatPending"), "Wide-gap dog neither jumps nor retreats");
                    Set(dog, "gapRetreatRemaining", -.01f); Call(dog, "Update");
                    Check((int)Get(dog, "direction") == -direction && !(bool)Get(dog, "jumpingGap"), "Blocked dog does not retreat"); retreats++;
                    if (width > DogObstacle.MaxJumpGapWidth)
                    {
                        Set(dog, "direction", direction); Set(dog, "gapRetreatPending", false);
                        root.transform.position = new Vector2(launch + direction * .2f, 0);
                        Call(dog, "TryStartGapAction", .1f);
                        Check((bool)Get(dog, "gapRetreatPending") && !(bool)Get(dog, "jumpingGap") &&
                            Mathf.Abs(root.transform.position.x - launch) < .001f, "Previous leap bypasses wide-pit stop");
                    }
                }
                trials++; UnityEngine.Object.DestroyImmediate(root);
            }
            var course = new GameObject("CoursePolicy").AddComponent<LevelCourseBuilder>();
            course.BindVisuals(catalog.roadGapSprite, catalog.moundSprite, catalog.roadPlatformSprite, dogs: catalog.dogRunFrames);
            foreach (var difficulty in new[] { DifficultyLevel.Easy, DifficultyLevel.Medium, DifficultyLevel.Hard })
            {
                GameDifficulty.Select(difficulty);
                foreach (int level in new[] { 6, 9, 12, 15, 18, 21, 24, 27, 30 })
                {
                    course.Build(level);
                    foreach (var dog in course.GetComponentsInChildren<DogObstacle>())
                    {
                        var gaps = (Vector2[])Get(dog, "gaps"); var allowed = (bool[])Get(dog, "gapJumpAllowed");
                        Check(allowed != null && gaps.Length == allowed.Length, "Missing generated gap policies");
                        for (int i = 0; i < gaps.Length; i++)
                            Check(allowed[i] == (gaps[i].y - gaps[i].x <= 2.601f), "Generated shelf pit allows dog leap");
                        coursePolicies++;
                    }
                }
            }
            Check(coursePolicies > 0, "No production dogs tested");
            UnityEngine.Object.DestroyImmediate(course.gameObject);
            Debug.Log($"DOG_NARROW_GAP_PASS trials={trials} narrowJumps={jumps} safeRetreats={retreats} generatedDogPolicies={coursePolicies}");

            typeof(GameFlow).GetField("<State>k__BackingField", Hidden).SetValue(flow, GameFlowState.MainMenu);
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 4.45f; camera.aspect = 16f / 9;
            camera.transform.position = new Vector3(0, 0, -10);
            var bg = new GameObject("RoomBackground").AddComponent<SpriteRenderer>(); bg.sprite = catalog.homeBackground; bg.sortingOrder = -5;
            var rooms = new GameObject("Rooms").AddComponent<HomeRooms>(); Call(rooms, "Awake"); rooms.Bind(camera, bg);
            var view = new GameObject("FurnitureView").AddComponent<HubFurnitureView>(); Call(view, "Awake");
            var slots = new Transform[Enum.GetValues(typeof(FurnitureSlotType)).Length];
            for (int i = 0; i < slots.Length; i++) slots[i] = new GameObject("Slot" + i).transform;
            view.Bind(slots); view.BindRoom(bg, catalog.homeBackground);
            var items = catalog.shopItems.Where(i => i != null && i.placedSprite != null && i.slotType != FurnitureSlotType.Wallpaper)
                .GroupBy(i => i.slotType).Select(g => g.First()).ToArray();
            int placements = 0;
            foreach (int room in new[] { 0, 1 })
            foreach (var item in items)
            {
                foreach (Transform child in view.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                SaveService.Data.furniture = new List<SaveService.FurnitureCopy>();
                var copy = new SaveService.FurnitureCopy { instanceId = "placement-trial-" + placements, itemId = item.id, placed = true, roomId = room };
                SaveService.Data.furniture.Add(copy); view.Refresh();
                var furniture = view.GetComponentsInChildren<DraggableFurniture>().Single(f => f.InstanceId == copy.instanceId);
                var bounds = furniture.PlacementBounds;
                if (item.slotType == FurnitureSlotType.Poster)
                    Check(furniture.transform.position.y >= .65f, "Poster moved onto floor");
                else if (item.slotType != FurnitureSlotType.Rug)
                    Check(Mathf.Abs(bounds.min.y - HomeRooms.FloorMaxY) < .002f, "New furniture starts at wall edge: " + item.slotType);
                else Check(bounds.max.y <= HomeRooms.FurnitureBackY + .051f, "Rug enters wall");
                var position = furniture.transform.position; view.Refresh();
                Check(Vector3.Distance(position, furniture.transform.position) < .001f && copy.hasPosition, "Refresh moves saved furniture");
                furniture.transform.position += Vector3.down * .25f; furniture.ClampPlacement();
                Call(furniture, "SavePlacement"); // Drag release persists even inside the valid bounds.
                var moved = furniture.transform.position; view.Refresh();
                Check(Vector3.Distance(moved, furniture.transform.position) < .001f, "Manual placement not retained: " + item.slotType);
                placements++;
            }
            // Render a newly added tree at its default lower floor position.
            foreach (Transform child in view.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            SaveService.Data.furniture = new List<SaveService.FurnitureCopy>();
            var tree = items.First(i => i.slotType == FurnitureSlotType.Tree);
            SaveService.Data.furniture.Add(new SaveService.FurnitureCopy { instanceId = "tree-preview", itemId = tree.id, placed = true, roomId = 0 });
            view.Refresh();
            string folder = Environment.GetEnvironmentVariable("MITZI_PROGRESSION_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-progression"); Directory.CreateDirectory(folder);
            var rt = new RenderTexture(1600, 900, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(folder, "furniture-lower-start.png"), image.EncodeToPNG());
            RenderTexture.active = null; camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log($"FURNITURE_LOWER_START_PASS placements={placements} floorBase={HomeRooms.FloorMaxY} savedAndManualPositions=retained posters=wall");
        }
        finally
        {
            SaveService.Data.furniture = savedCopies;
            if (primary.Length > 0) PlayerPrefs.SetString("PawPath.Save.v1", primary); else PlayerPrefs.DeleteKey("PawPath.Save.v1");
            if (backup.Length > 0) PlayerPrefs.SetString("PawPath.Save.v1.backup", backup); else PlayerPrefs.DeleteKey("PawPath.Save.v1.backup");
            PlayerPrefs.Save(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
