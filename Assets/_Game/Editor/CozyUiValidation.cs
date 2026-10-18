#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Cat;
using PawPath.Drawing;
using PawPath.Gameplay;
using PawPath.Levels;
using PawPath.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CozyUiValidation
{
    public static void Run()
    {
        HomeFeatureValidation.Run();
        try
        {
            var flow = GameFlow.Instance;
            var camera = Camera.main;
            var drawing = camera.GetComponent<LineDraw>();
            typeof(LineDraw).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(drawing, null);
            var levels = flow.GetComponent<LevelManager>();
            typeof(LevelManager).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(levels, null);
            var cat = flow.GetComponentInChildren<CatController>(true);
            typeof(CatController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cat, null);
            GameplayMode.Select(GameplayPlayMode.Drawing);
            flow.StartNextLevel();
            flow.BeginDrawingPreparation();
            foreach (var care in flow.GetComponentsInChildren<CatNeedsUI>(true))
                typeof(CatNeedsUI).GetMethod("HideForLevel", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(care, null);
            foreach (var node in flow.GetComponentsInChildren<Transform>(true))
                if (node.name == "HomeRoomNavigation") node.gameObject.SetActive(false);
            var hud = flow.GetComponentInChildren<HudView>(true);
            typeof(HudView).GetMethod("OnLevelStarted", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
            flow.BeginDrawingPreparation();
            foreach (var component in flow.GetComponentsInChildren<Transform>(true))
                if (component.name == "BrushTutorial") component.gameObject.SetActive(false);
            typeof(HudView).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
            foreach (var name in new[] { "NormalBrush", "BounceBrush", "IceBrush", "Eraser", "PreviousDrawView", "NextDrawView", "StartDrawingGame" })
            {
                var button = hud.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
                if (button.GetComponentsInChildren<Text>().Any(t => !string.IsNullOrEmpty(t.text)))
                    throw new Exception(name + " still has a visible text label");
                if (button.transform.Find("Icon").GetComponent<Image>().sprite == null)
                    throw new Exception(name + " missing icon");
            }
            foreach (var name in new[] { "NormalBrush", "BounceBrush", "IceBrush", "Eraser" })
            {
                var button = hud.GetComponentsInChildren<Button>(true).Single(b => b.name == name);
                button.onClick.Invoke();
                foreach (var state in hud.GetComponentsInChildren<DrawingToolButton>()) state.Refresh();
                if (!button.transform.Find("SelectionRing").gameObject.activeSelf)
                    throw new Exception(name + " selection did not follow brush");
            }
            drawing.SetBrushType(PathSurfaceType.Normal);
            foreach (var state in hud.GetComponentsInChildren<DrawingToolButton>()) state.Refresh();
            Canvas.ForceUpdateCanvases();
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            File.WriteAllBytes("AnimalArtSources-20261018/drawing-ui.png", image.EncodeToPNG());
            RenderTexture.active = null; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("COZY_UI_VALIDATION_PASS");
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
