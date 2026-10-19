#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PawPath.Core;
using PawPath.Gameplay;
using PawPath.UI;
public static class JoystickValidation
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static PointerEventData Pointer(MobileJoystick stick,int id,Vector2 axis)
    {
        var rect=stick.GetComponent<RectTransform>();var raycaster=stick.GetComponentInParent<GraphicRaycaster>();
        Vector3 world=rect.TransformPoint((Vector3)(axis*rect.rect.width*.3f));
        return new PointerEventData(EventSystem.current){pointerId=id,position=RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera,world),pointerPressRaycast=new RaycastResult{module=raycaster}};
    }
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated validation project");
            HudTrimValidation.Run();var flow=GameFlow.Instance;
            var sticks=flow.GetComponentsInChildren<MobileJoystick>().Where(s=>!s.HomeMode).ToArray();Check(sticks.Length==2,"Direct mode must have two active sticks");
            var left=sticks.Single(s=>s.Axis==JoystickAxis.Horizontal);var right=sticks.Single(s=>s.Axis==JoystickAxis.Vertical);
            var oldButtons=flow.GetComponentsInChildren<MobileControlButton>();Check(oldButtons.Length==0,"Direct mode retains old arrow buttons");
            MobileControlState.Reset();Canvas.ForceUpdateCanvases();
            left.OnPointerDown(Pointer(left,11,Vector2.zero));left.OnDrag(Pointer(left,11,Vector2.right*.8f));
            Check(MobileControlState.Horizontal==1,"Left stick does not move right");
            left.OnDrag(Pointer(left,99,Vector2.left));Check(MobileControlState.Horizontal==1,"Foreign finger hijacks movement stick");
            right.OnPointerDown(Pointer(right,22,Vector2.zero));right.OnDrag(Pointer(right,22,Vector2.up*.8f));
            Check(MobileControlState.Horizontal==1&&MobileControlState.ConsumeJump(),"Two fingers cannot move and jump simultaneously");
            right.OnDrag(Pointer(right,22,Vector2.up));Check(!MobileControlState.ConsumeJump(),"Holding up repeats jump");
            right.OnDrag(Pointer(right,22,Vector2.zero));right.OnDrag(Pointer(right,22,Vector2.up*.8f));Check(MobileControlState.ConsumeJump(),"Neutral and up cannot trigger second jump");
            right.OnDrag(Pointer(right,22,Vector2.down*.8f));Check(MobileControlState.CrouchHeld&&MobileControlState.Horizontal==1,"Move and crouch cannot be held together");
            right.OnPointerUp(Pointer(right,99,Vector2.zero));Check(MobileControlState.CrouchHeld,"Foreign finger releases action stick");
            left.OnPointerUp(Pointer(left,11,Vector2.zero));Check(MobileControlState.Horizontal==0&&MobileControlState.CrouchHeld,"Movement release clears other stick");
            right.OnPointerUp(Pointer(right,22,Vector2.zero));Check(!MobileControlState.CrouchHeld,"Crouch stuck after release");
            left.OnPointerDown(Pointer(left,11,Vector2.up));Check(MobileControlState.Horizontal==0,"Vertical move on left stick triggers horizontal input");
            left.OnDrag(Pointer(left,11,Vector2.left*.1f));Check(MobileControlState.Horizontal==0,"Joystick centre has no dead zone");
            left.OnDrag(Pointer(left,11,Vector2.left*4));Check(MobileControlState.Horizontal==-1,"Dragging outside joystick loses control");
            // This batch validator runs outside play mode; dispatch lifecycle callbacks explicitly.
            left.gameObject.SetActive(false);typeof(MobileJoystick).GetMethod("OnDisable",Hidden).Invoke(left,null);
            Check(MobileControlState.Horizontal==0,"Disabled stick leaves movement stuck");left.gameObject.SetActive(true);
            right.OnPointerDown(Pointer(right,22,Vector2.up));right.gameObject.SetActive(false);
            typeof(MobileJoystick).GetMethod("OnDisable",Hidden).Invoke(right,null);
            Check(!MobileControlState.JumpQueued&&!MobileControlState.CrouchHeld,"Disabled action stick leaks queued jump");right.gameObject.SetActive(true);
            flow.SetGameplayPaused(true);left.OnPointerDown(Pointer(left,11,Vector2.right));Check(MobileControlState.Horizontal==0,"Paused game accepts stick input");flow.SetGameplayPaused(false);
            left.Resize(false);float small=left.GetComponent<RectTransform>().sizeDelta.x;left.Resize(true);
            Check(left.GetComponent<RectTransform>().sizeDelta.x>small,"Large setting does not resize stick");
            left.Resize(DisplaySizeSettings.Large);right.Resize(DisplaySizeSettings.Large);
            var demoRoot=new GameObject("JoystickLessonTest",typeof(RectTransform));demoRoot.transform.SetParent(left.transform.parent,false);
            var demo=demoRoot.AddComponent<GameplayTutorialDemo>();demo.Configure(TutorialDemoKind.Move);demo.RenderAt(.8f);
            var knob=demo.transform.Find("DemoControl_0/JoystickThumb").GetComponent<RectTransform>();Check(knob.gameObject.activeInHierarchy&&knob.anchoredPosition.x>0,"Lesson does not show right stick movement");
            demo.RenderAt(3.8f);Check(knob.anchoredPosition.x<0,"Lesson does not show left stick movement");
            demo.Configure(TutorialDemoKind.Jump);demo.RenderAt(.575f);knob=demo.transform.Find("DemoControl_0/JoystickThumb").GetComponent<RectTransform>();Check(knob.anchoredPosition.y>10,"Jump lesson still shows a tap button");
            demo.Configure(TutorialDemoKind.Crouch);demo.RenderAt(2f);knob=demo.transform.Find("DemoControl_0/JoystickThumb").GetComponent<RectTransform>();Check(knob.anchoredPosition.y<0,"Crouch lesson does not hold joystick down");
            UnityEngine.Object.DestroyImmediate(demoRoot);
            var level=flow.GetComponent<PawPath.Levels.LevelManager>();
            typeof(PawPath.Levels.LevelManager).GetMethod("Awake",Hidden).Invoke(level,null);
            flow.StartNextLevel();
            var hud=flow.GetComponentInChildren<HudView>(true);typeof(HudView).GetMethod("OnLevelStarted",Hidden).Invoke(hud,null);
            foreach(var home in flow.GetComponentsInChildren<PawPath.Hub.HomeRooms>(true))
                typeof(PawPath.Hub.HomeRooms).GetMethod("OnDisable",Hidden).Invoke(home,null);
            foreach(var care in flow.GetComponentsInChildren<CatNeedsUI>(true))
                typeof(CatNeedsUI).GetMethod("HideForLevel",Hidden).Invoke(care,null);
            var camera=Camera.main;camera.transform.position=new Vector3(-6f,-.1f,-10);camera.aspect=16f/9f;
            foreach(var canvas in flow.GetComponentsInChildren<Canvas>(true)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;canvas.sortingOrder=200;}
            var target=new RenderTexture(1280,720,24);camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/dual-joystick-preview.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("DUAL_JOYSTICK_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
