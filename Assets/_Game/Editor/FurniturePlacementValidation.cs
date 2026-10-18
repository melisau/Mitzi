#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PawPath.Core;
using PawPath.Hub;
using PawPath.Content;
using PawPath.Economy;

public static class FurniturePlacementValidation
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        try
        {
            Check(Application.productName == "MitziValidation", "Use isolated validation project");
            var previewCamera = new GameObject("Main Camera").AddComponent<Camera>();
            previewCamera.tag = "MainCamera"; previewCamera.aspect = 16f/9f;
            HomeSettingsValidation.Run();
            var flow = GameFlow.Instance;
            var view = flow.GetComponentInChildren<HubFurnitureView>(true);
            var rooms = flow.GetComponentInChildren<HomeRooms>(true);
            var economy = flow.GetComponent<CozyEconomyManager>();
            typeof(CozyEconomyManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(economy, null);
            SaveService.Data.lovePoints = 1000000;
            SaveService.Data.furniture.Clear();
            var types = new[] { FurnitureSlotType.Sand, FurnitureSlotType.Bowl, FurnitureSlotType.Water, FurnitureSlotType.Bed, FurnitureSlotType.Rug, FurnitureSlotType.Poster };
            foreach (var type in types)
            {
                var item = flow.Catalog.shopItems.First(i => i != null && i.slotType == type);
                Check(economy.TryBuy(item, 0), "Purchase failed: " + type);
                view.Refresh();
            }
            var placed = view.GetComponentsInChildren<DraggableFurniture>();
            Check(placed.Length == types.Length, "Starter furniture did not fit");
            foreach (var item in placed)
            {
                var b = item.PlacementBounds;
                var room = rooms.RoomBounds(item.RoomId);
                Check(b.min.x >= room.x && b.max.x <= room.y, "Furniture outside room");
                if (item.SlotType != FurnitureSlotType.Poster)
                    Check(b.min.y <= HomeRooms.FurnitureBackY + .06f && b.min.y >= -4.36f, "Furniture floating/under floor: " + item.SlotType);
                foreach (var other in placed)
                {
                    if (other == item || !item.SharesPlacementLayer(other)) continue;
                    var o = other.PlacementBounds;
                    Check(!(b.min.x < o.max.x && b.max.x > o.min.x && b.min.y < o.max.y && b.max.y > o.min.y), "Initial furniture overlap");
                }
            }
            var locations = SaveService.Data.furniture.ToDictionary(f => f.instanceId, f => new Vector2(f.x,f.y));
            view.Refresh();
            foreach (var copy in SaveService.Data.furniture)
                Check(Vector2.Distance(locations[copy.instanceId], new Vector2(copy.x,copy.y)) < .001f, "Refresh rearranged furniture");
            var bowl = placed.First(d => d.SlotType == FurnitureSlotType.Bowl);
            bowl.transform.position = new Vector3(100,100,0); bowl.ClampPlacement();
            Check(bowl.PlacementBounds.min.y <= HomeRooms.FurnitureBackY && bowl.PlacementBounds.max.x <= rooms.RoomBounds(0).y, "Drag escaped floor/room limits");
            // An old wall-level saved object is corrected immediately, before rendering.
            var bowlSave = SaveService.Data.furniture.First(f => f.instanceId == bowl.InstanceId);
            bowlSave.y = 2f; view.Refresh();
            Check(bowl.PlacementBounds.min.y <= HomeRooms.FurnitureBackY, "Old high placement not corrected");
            var food = flow.Catalog.shopItems.First(i=>i!=null && i.slotType == FurnitureSlotType.Bowl);
            Check(economy.TryBuy(food,1), "Second-room purchase failed");view.Refresh();
            Check(view.GetComponentsInChildren<DraggableFurniture>().Any(d=>d.RoomId==1 && d.SlotType==FurnitureSlotType.Bowl), "Second room placement failed");
            // Repeated purchases can overflow to storage but must never overlap or disappear.
            for(int i=0;i<15;i++)
            {
                SaveService.Data.lovePoints = int.MaxValue;
                var poster = flow.Catalog.shopItems.First(p=>p!=null && p.slotType==FurnitureSlotType.Poster);
                SaveService.Data.itemPurchases.Clear();
                Check(economy.TryBuy(poster,0), "Capacity test purchase failed");view.Refresh();
            }
            Check(SaveService.Data.furniture.Any(f=>!f.placed), "Full room did not store overflow");
            Check(SaveService.Data.furniture.Count==types.Length+16, "Overflow lost purchased copies");
            SaveService.Persist();
            Check(SaveService.TryReadJson(SaveService.ExportJson(),out var reloaded,out _) && reloaded.furniture.Count==SaveService.Data.furniture.Count, "Layout save did not round trip");
            SaveService.Data.lovePoints = 50;
            var camera = Camera.main; camera.aspect = 16f/9f;camera.transform.position=new Vector3(0,0,-10);
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/furniture-floor-preview.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("FURNITURE_PLACEMENT_VALIDATION_PASS");
        }
        catch(Exception error){ Debug.LogException(error);EditorApplication.Exit(1); }
    }
}
#endif
