#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PawPath.Core;
using PawPath.Hub;

public static class HomeRoomSwipeValidation
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            HomeSettingsValidation.Run();
            var flow=GameFlow.Instance;var rooms=flow.GetComponentInChildren<HomeRooms>(true);
            flow.EnterHub();HomeEditMode.Close();rooms.SelectRoom(0);Canvas.ForceUpdateCanvases();Physics2D.SyncTransforms();
            Vector2 start=Vector2.zero;bool found=false;
            foreach(var normalized in new[]{new Vector2(.5f,.6f),new Vector2(.35f,.55f),new Vector2(.65f,.6f),new Vector2(.45f,.45f)})
            {
                var point=new Vector2(Screen.width*normalized.x,Screen.height*normalized.y);
                if((bool)Call(rooms,"BeginSwipe",point,7)){start=point;Call(rooms,"EndSwipe");found=true;break;}
            }
            Check(found,"No empty room area accepts swipes");
            Vector2 horizontal=new Vector2(200,0);
            Check((bool)Call(rooms,"BeginSwipe",start,7),"Swipe did not start");Call(rooms,"TrackSwipe",start-horizontal);
            Check(rooms.ActiveRoom==1&&HomeRooms.SuppressWorldTap,"Left swipe did not advance room or suppress ground tap");
            Call(rooms,"TrackSwipe",start+horizontal);Check(rooms.ActiveRoom==1,"One finger switched rooms repeatedly");Call(rooms,"EndSwipe");
            Check(HomeRooms.SuppressWorldTap,"Swipe release can move the selected cat");
            Call(rooms,"BeginSwipe",start,-1);Call(rooms,"TrackSwipe",start+horizontal);Call(rooms,"EndSwipe");
            Check(rooms.ActiveRoom==0,"Right mouse drag did not return to first room");
            Call(rooms,"BeginSwipe",start,7);Call(rooms,"TrackSwipe",start+horizontal);Call(rooms,"EndSwipe");Check(rooms.ActiveRoom==0,"Swiping beyond first room wraps");
            Call(rooms,"BeginSwipe",start,7);Call(rooms,"TrackSwipe",start+Vector2.up*200);Call(rooms,"EndSwipe");Check(rooms.ActiveRoom==0,"Vertical movement changed rooms");
            Call(rooms,"BeginSwipe",start,7);Call(rooms,"TrackSwipe",start+Vector2.right*12);Call(rooms,"EndSwipe");Check(rooms.ActiveRoom==0,"Small tap movement changed rooms");
            HomeEditMode.Toggle();Check(!(bool)Call(rooms,"BeginSwipe",start,7),"Furniture editing accepts room swipe");HomeEditMode.Close();
            var settings=flow.GetComponentsInChildren<Button>(true).First(b=>b.name=="SettingsButton");
            var canvas=settings.GetComponentInParent<Canvas>();var ui=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,settings.transform.position);
            var events=UnityEngine.Object.FindObjectOfType<EventSystem>();
            Check(events!=null,"Event system missing");
            typeof(EventSystem).GetMethod("OnEnable",Hidden).Invoke(events,null);
            foreach(var raycaster in UnityEngine.Object.FindObjectsOfType<GraphicRaycaster>())
                typeof(GraphicRaycaster).GetMethod("OnEnable",Hidden).Invoke(raycaster,null);
            Canvas.ForceUpdateCanvases();
            Check(!(bool)Call(rooms,"BeginSwipe",ui,7),"UI button accepts room swipe");
            var blocker=new GameObject("SwipePettingTarget");var point3=new Vector3(start.x,start.y,Mathf.Abs(Camera.main.transform.position.z));blocker.transform.position=Camera.main.ScreenToWorldPoint(point3);
            blocker.AddComponent<BoxCollider2D>();blocker.AddComponent<CatPettingSystem>();Physics2D.SyncTransforms();
            Check(!(bool)Call(rooms,"BeginSwipe",start,7),"Petting target accepts room swipe");UnityEngine.Object.DestroyImmediate(blocker);
            flow.ShowInfoOverlay();Check(!(bool)Call(rooms,"BeginSwipe",start,7),"Non-home screen accepts room swipe");flow.DismissInfoOverlay();flow.EnterHub();
            rooms.SelectRoom(1);typeof(SaveService).GetField("cache",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            Check(rooms.ActiveRoom==1,"Swiped room does not persist");rooms.SelectRoom(0);
            Debug.Log("HOME_ROOM_SWIPE_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
