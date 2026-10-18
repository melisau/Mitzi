#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Content;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Hub;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HomeFeatureValidation
{
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void SetInstance<T>(T component) => typeof(T).GetProperty("Instance").SetValue(null, component);
    static void Seed(SaveService.SaveData data) => typeof(SaveService).GetField("cache", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, data);

    public static void Run()
    {
        try
        {
            Require(Application.productName == "MitziValidation", "Use isolated validation project; live saves must not be touched");
            SaveServiceValidation.Run();
            AnimalAnimationValidation.BuildAndValidate();
            const string legacy = "{\"schemaVersion\":2,\"lovePoints\":4256,\"highestCompletedLevel\":162,\"selectedCatId\":\"mitzi\",\"unlockedCatIds\":[\"mitzi\",\"ada\"],\"ownedItemIds\":[\"cat_poster\",\"food_bowl\"],\"placedItemIds\":[\"cat_poster\"]}";
            Require(SaveService.TryReadJson(legacy, out var migrated, out bool future) && !future, "Legacy migration failed");
            Require(migrated.lovePoints == 4256 && migrated.highestCompletedLevel == 162 && migrated.furniture.Count == 2 && migrated.furniture.Count(f => f.placed) == 1, "Migration lost money, progress or items");
            Require(SaveService.TryReadJson(JsonUtility.ToJson(migrated), out var roundTrip, out _) && roundTrip.furniture.Count == 2, "Migration duplicated items");

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var posters = catalog.shopItems.Where(i => i != null && i.slotType == FurnitureSlotType.Poster).Take(2).ToArray();
            Require(posters.Length == 2, "Poster test fixtures missing");
            var data = new SaveService.SaveData { lovePoints = 10000, furnitureInventoryMigrated = true };
            data.unlockedCatIds = catalog.cats.Select(c => c.id).ToList(); Seed(data);
            var root = new GameObject("EconomyTest");
            var flow = root.AddComponent<GameFlow>(); SetInstance(flow); flow.BindCatalog(catalog);
            var economy = root.AddComponent<CozyEconomyManager>(); SetInstance(economy);
            Require(economy.TryBuy(posters[0]) && economy.TryBuy(posters[0]) && economy.TryBuy(posters[1]), "Multiple poster purchases failed");
            Require(data.furniture.Count == 3 && data.furniture.All(f => f.placed) && data.furniture.Select(f => f.instanceId).Distinct().Count() == 3, "Copies replaced one another");
            data.activeHomeRoom = 1;
            Require(economy.TryBuy(posters[0]) && economy.PlacedCount(posters[0].id, 1) == 1, "Second-room purchase failed");
            string firstId = data.furniture[0].instanceId;
            Require(economy.Store(firstId) && economy.StoredCount(posters[0].id) == 1, "Store copy failed");
            Require(economy.Place(posters[0]) && data.furniture[0].roomId == 1, "Place stored copy in second room failed");
            var moved = data.furniture.Find(f => f.itemId == posters[1].id);
            Require(economy.MoveToRoom(moved.instanceId, 1) && moved.roomId == 1, "Move between rooms failed");
            moved.x = -2.1f; moved.y = -2.6f; moved.hasPosition = true; moved.flipped = true;
            SaveService.Persist();
            Require(SaveService.TryReadJson(SaveService.ExportJson(), out var loaded, out _) && loaded.furniture.Find(f => f.instanceId == moved.instanceId).flipped, "Per-copy placement did not persist");
            int before = economy.OwnedCount(posters[0].id);
            Require(economy.SellOne(posters[0]) && economy.OwnedCount(posters[0].id) == before - 1, "Sell removed wrong quantity");
            data.lovePoints = 0; int count = data.furniture.Count;
            Require(!economy.TryBuy(posters[0]) && data.furniture.Count == count, "Unaffordable purchase allowed");
            data.lovePoints = 10000;
            UnityEngine.Object.DestroyImmediate(root);

            // Render the real runtime UI and room objects, without running ads or changing live saves.
            var previewCamera = new GameObject("Main Camera").AddComponent<Camera>();
            previewCamera.tag="MainCamera"; previewCamera.aspect=16f/9f;
            var game = RuntimeBootstrap.Build(catalog); SetInstance(game);
            economy = game.GetComponent<CozyEconomyManager>(); SetInstance(economy);
            var rooms = game.GetComponentInChildren<HomeRooms>(true); SetInstance(rooms);
            game.EnterHub();
            var view = game.GetComponentInChildren<HubFurnitureView>(true); SetInstance(view);
            var house = game.GetComponentInChildren<CatHouseManager>(true); SetInstance(house);
            data.activeHomeRoom = 0;
            economy.TryBuy(posters[0]); economy.TryBuy(posters[1]);
            foreach(var item in catalog.shopItems.Where(i => i != null && (i.slotType == FurnitureSlotType.Bed || i.slotType == FurnitureSlotType.Bowl || i.slotType == FurnitureSlotType.Water)).GroupBy(i => i.slotType).Select(g => g.First())) economy.TryBuy(item);
            view.Refresh(); house.RefreshResidents();
            foreach(var drag in view.GetComponentsInChildren<DraggableFurniture>()) typeof(DraggableFurniture).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(drag, null);
            Require(view.GetComponentsInChildren<DraggableFurniture>().Length == data.furniture.Count(f => f.placed), "Placed copies not rendered independently");
            foreach(var cat in house.GetComponentsInChildren<CatHomeBehaviour>())
            {
                Require(cat.transform.position.y <= HomeRooms.FloorMaxY, "Home cat floating above floor band");
                cat.WalkDirectTo(new Vector2(rooms.Offset(1), -2.4f));
                var final = (Vector2)typeof(CatHomeBehaviour).GetField("finalPosition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(cat);
                Require(final.x > rooms.RoomWidth * 0.5f, "Cat cannot walk into second room");
            }
            var camera = Camera.main;
            camera.aspect=4f/3f;
            typeof(HomeRooms).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rooms,null);
            Require(Mathf.Abs(rooms.RoomWidth-camera.orthographicSize*2f*camera.aspect)<0.01f,"Room widths do not follow viewport resize");
            camera.aspect=16f/9f;
            typeof(HomeRooms).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(rooms,null);
            foreach(var canvas in game.GetComponentsInChildren<Canvas>(true)) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5f; canvas.sortingOrder=200; }
            for(int room=0;room<2;room++)
            {
                rooms.SelectRoom(room);
                camera.transform.position = new Vector3(rooms.Offset(room), camera.transform.position.y, camera.transform.position.z);
                Canvas.ForceUpdateCanvases(); Render(camera, "home-room-"+(room+1)+".png");
            }
            var shop=game.GetComponentInChildren<PawPath.UI.ShopScreen>(true);
            shop.gameObject.SetActive(true); shop.Rebuild(); Canvas.ForceUpdateCanvases();
            Require(shop.GetComponentsInChildren<UnityEngine.UI.Button>().Count(b => b.name=="SellOneButton") == catalog.shopItems.Count(i=>i!=null), "Shop must expose one-copy sale for each product");
            Render(camera,"home-shop.png"); shop.gameObject.SetActive(false);
            var dogGo = new GameObject("IdleDogTest");
            var dogRenderer = dogGo.AddComponent<SpriteRenderer>();
            var dog = dogGo.AddComponent<DogObstacle>();
            dog.Configure(dogRenderer, catalog.dogRunFrames, 2f, -20f, 20f, 0f, 2.75f, 2.75f*0.08f, 1f/16f, null, 1, catalog.dogIdleSprite);
            Require(dogRenderer.sprite == catalog.dogIdleSprite, "Waiting dog uses running pose");
            float feet = dogRenderer.sprite.bounds.min.y + dogRenderer.sprite.bounds.size.y * 0.08f;
            Require(Mathf.Abs(dogRenderer.transform.TransformPoint(new Vector3(0f,feet,0f)).y) < 0.02f, "Idle dog feet float");
            File.WriteAllText("AnimalArtSources-20261018/home-validation.txt", "PASS: save migration, duplicate/different posters, room purchases, storage, room transfer, per-copy persistence, single-copy sale, insufficient funds, independent rendered objects, lowered residents, cross-room destinations, idle dog grounding.");
            Debug.Log("HOME_FEATURE_VALIDATION_PASS");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    static void Render(Camera camera, string name)
    {
        Directory.CreateDirectory("AnimalArtSources-20261018");
        var target = new RenderTexture(1280,720,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
        var image = new Texture2D(1280,720,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        File.WriteAllBytes("AnimalArtSources-20261018/"+name,image.EncodeToPNG());
        RenderTexture.active=null; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
    }
}
#endif
