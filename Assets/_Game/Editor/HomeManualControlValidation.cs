#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Content;
using PawPath.Gameplay;
using PawPath.Hub;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class HomeManualControlValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        try
        {
            HomeWakeValidation.Run();
            var flow = GameFlow.Instance; flow.EnterHub(); HomeEditMode.Close();
            var interaction = flow.GetComponentInChildren<CatHouseInteraction>(true); Call(interaction, "Awake");
            var resident = flow.GetComponentsInChildren<CatHomeBehaviour>().First();
            SaveService.Data.selectedCatId = resident.Definition.id;
            interaction.Select(resident);
            resident.transform.position = new Vector3(0, HomeRooms.FloorMinY + .2f, 0);
            var controls = flow.GetComponentInChildren<HomeManualControls>(true);
            controls.gameObject.SetActive(true); controls.Refresh();
            var stick = flow.GetComponentsInChildren<MobileJoystick>(true).Single(s => s.HomeMode);
            Check(stick.gameObject.activeSelf, "Home joystick not available for selected cat");
            Canvas.ForceUpdateCanvases();
            var rect = stick.GetComponent<RectTransform>(); var raycaster = stick.GetComponentInParent<GraphicRaycaster>();
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 9, pointerPressRaycast = new RaycastResult { module = raycaster }, position = RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera, rect.TransformPoint(Vector3.right * rect.rect.width * .3f)) };
            MobileControlState.Reset(); stick.OnPointerDown(pointer); controls.Refresh();
            Check(MobileJoystick.SuppressHomeWorldInput, "Home joystick drag can pet or select cats");
            Check(stick.HomeValue.x > .5f && MobileControlState.Horizontal == 0, "Home joystick leaks level input");
            var start = resident.transform.position; Call(resident, "AdvanceManualMovement", .2f);
            Check(resident.transform.position.x > start.x, "Selected resident did not move right");
            stick.OnPointerUp(pointer); controls.Refresh();
            Check(MobileJoystick.SuppressHomeWorldInput, "Home joystick release can send a ground destination");
            Check(resident.CurrentActivity == ResidentActivity.Standing, "Released home joystick keeps walking");
            resident.SetManualInput(Vector2.up); Call(resident, "AdvanceManualMovement", 20f);
            Check(resident.transform.position.y <= HomeRooms.FloorMaxY, "Manual resident can climb wall");
            resident.SetManualInput(Vector2.zero);
            foreach (var slot in new[] { FurnitureSlotType.Bowl, FurnitureSlotType.Water, FurnitureSlotType.Bed })
            {
                var item = flow.Catalog.shopItems.First(i => i.slotType == slot);
                var root = new GameObject("NearbyCareTest"); root.transform.position = resident.transform.position + Vector3.right;
                var use = root.AddComponent<FurnitureInteraction>(); use.Configure(item);
                Check(use.TryNearbyCare(resident, out var action), "Nearby " + slot + " unavailable");
                Check(action == (slot == FurnitureSlotType.Bowl ? CareAction.Eat : slot == FurnitureSlotType.Water ? CareAction.Drink : CareAction.Sleep), "Wrong nearby action");
                controls.Refresh();
                var button = flow.GetComponentsInChildren<Button>(true).Single(b => b.name == "NearbyCareAction");
                Check(button.gameObject.activeSelf, "No explicit nearby action button");
                root.transform.position += Vector3.right * 5;
                Check(!use.TryNearbyCare(resident, out _), "Distant furniture allows manual care");
                UnityEngine.Object.DestroyImmediate(root);
            }
            HomeEditMode.Toggle(); controls.Refresh(); Check(!stick.gameObject.activeSelf, "Joystick remains in furniture edit mode"); HomeEditMode.Close();
            controls.Refresh();
            // Render the actual home UI, including the selected-name badge and contextual action.
            var camera = Camera.main; camera.transform.position = new Vector3(0, .2f, -10); camera.aspect = 16f / 9f;
            foreach (var canvas in flow.GetComponentsInChildren<Canvas>(true)) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5; }
            var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            Directory.CreateDirectory("AnimalArtSources-20261018"); File.WriteAllBytes("AnimalArtSources-20261018/home-joystick-preview.png", image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("HOME_MANUAL_CONTROL_VALIDATION_PASS");
        }
        catch (Exception error) { Debug.LogException(error); UnityEditor.EditorApplication.Exit(1); }
    }
}
#endif
