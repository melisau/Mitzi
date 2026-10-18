#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Data;
using PawPath.Hub;
using PawPath.UI;

public static class HomeSettingsValidation
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        try
        {
            Check(Application.productName == "MitziValidation", "Use isolated validation project");
            HomeRoomOptionsBuilder.Apply();
            CrouchObstacleArtBuilder.Apply();
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            Check(catalog.homeRoomOptions.Length == 3 && catalog.homeRoomOptions.All(s => s != null), "Room sprites missing");
            var flow = RuntimeBootstrap.Build(catalog);
            typeof(GameFlow).GetProperty("Instance").SetValue(null, flow);
            var rooms = flow.GetComponentInChildren<HomeRooms>(true);
            typeof(HomeRooms).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rooms, null);
            var furniture = flow.GetComponentInChildren<HubFurnitureView>(true);
            var buttons = flow.GetComponentsInChildren<Button>(true);
            var settings = buttons.First(b => b.name == "CloseSettings").transform.parent.parent.gameObject;
            buttons.First(b => b.name == "SettingsButton").onClick.Invoke();
            foreach (string name in new[] { "NormalSize", "LargeSize", "MusicMuteButton", "EffectsMuteButton", "QuitGame" })
            {
                var b = buttons.First(v => v.name == name);
                Check(b.image.sprite == CozyIconFactory.PillButton(), name + " has old box style");
            }
            Check(buttons.First(b => b.name == "QuitGame").onClick.GetPersistentEventCount() == 0, "Unexpected serialized quit action");
            SaveService.Data.activeHomeRoom = 0;
            Check(!buttons.Any(b=>b.name.StartsWith("RoomStyle_")),"Wallpaper selection remains in settings");
            var economy=flow.GetComponent<PawPath.Economy.CozyEconomyManager>();
            typeof(PawPath.Economy.CozyEconomyManager).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(economy,null);
            SaveService.Data.lovePoints=5000;SaveService.Data.itemPurchases.Clear();SaveService.Data.furniture.RemoveAll(f=>catalog.GetItem(f.itemId)?.slotType==PawPath.Content.FurnitureSlotType.Wallpaper);
            Check(economy.TryBuy(catalog.GetItem("wall_garden")),"Garden wallpaper purchase failed");furniture.Refresh();
            Check(rooms.Background(0).sprite.texture == catalog.homeRoomOptions[0].texture, "Wrong room texture");
            SaveService.Data.activeHomeRoom = 1;
            Check(economy.TryBuy(catalog.GetItem("wall_blue_room")),"Blue wallpaper purchase failed");furniture.Refresh();
            Check(rooms.Background(0).sprite.texture==catalog.homeRoomOptions[0].texture && rooms.Background(1).sprite.texture==catalog.homeRoomOptions[2].texture,"Rooms share wallpaper");
            typeof(SaveService).GetField("cache", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            furniture.Refresh();
            Check(rooms.Background(0).sprite.texture==catalog.homeRoomOptions[0].texture && rooms.Background(1).sprite.texture==catalog.homeRoomOptions[2].texture,"Purchased wallpapers lost after reload");
            SaveService.Data.activeHomeRoom = 0;
            buttons.First(b => b.name == "SettingsButton").onClick.Invoke();
            var cam = Camera.main;
            cam.aspect = 16f / 9f;
            foreach (var canvas in flow.GetComponentsInChildren<Canvas>(true))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 5;
            }
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(1280, 720, 24); cam.targetTexture = target; cam.Render(); RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
            Directory.CreateDirectory("AnimalArtSources-20261018"); File.WriteAllBytes("AnimalArtSources-20261018/settings-room-preview.png", image.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            var resetButton = buttons.First(b => b.name == "ResetFirstPlay");
            var confirmation = buttons.First(b => b.name == "CancelFirstPlayReset").transform.parent.gameObject;
            Check(!confirmation.activeSelf, "Reset confirmation starts visible");
            int priorBalance = SaveService.Data.lovePoints;
            resetButton.onClick.Invoke();
            Check(confirmation.activeSelf && SaveService.Data.lovePoints == priorBalance, "Reset happened without confirmation");
            buttons.First(b => b.name == "CancelFirstPlayReset").onClick.Invoke();
            Check(!confirmation.activeSelf && SaveService.Data.lovePoints == priorBalance, "Cancel erased progress");
            PlayerPrefs.SetInt(GameplayTutorialUI.DrawingSeenKey, 1);
            PlayerPrefs.SetInt(GameplayTutorialUI.DirectSeenKey, 1);
            PlayerPrefs.SetInt("PawPath.HowToPlaySeen.v1", 1);
            SaveService.ResetFirstPlayForTesting();
            Check(SaveService.Data.highestCompletedLevel == 0 && SaveService.Data.lovePoints == 0, "First play progress not reset");
            Check(SaveService.Data.unlockedCatIds.SequenceEqual(new[] { "mitzi" }) && SaveService.Data.furniture.Count == 0 && SaveService.Data.itemPurchases.Count == 0, "First play inventory not reset");
            Check(!GameplayTutorialUI.HasSeen(PawPath.Gameplay.GameplayPlayMode.Drawing) && !GameplayTutorialUI.HasSeen(PawPath.Gameplay.GameplayPlayMode.DirectControl), "Lessons were not reset");
            Check(!PlayerPrefs.HasKey("PawPath.HowToPlaySeen.v1"), "First play flag remains");
            typeof(SaveService).GetField("cache", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
            Check(SaveService.Data.lovePoints == 0 && SaveService.Data.unlockedCatIds.Count == 1, "Fresh save did not persist");
            Debug.Log("FIRST_PLAY_RESET_VALIDATION_PASS");
            Debug.Log("HOME_SETTINGS_VALIDATION_PASS");
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
