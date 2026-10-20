using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Data;
using PawPath.Cat;
using PawPath.Drawing;
using PawPath.Levels;
using PawPath.Hub;
using PawPath.UI;
using PawPath.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PlayStoreCapture
{
    static void Call(object obj,string method) => obj?.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(obj,null);
    static string Folder => Environment.GetEnvironmentVariable("MITZI_STORE_ART") ?? Path.GetFullPath("Builds/Store");
    public static void Run()
    {
        if(Application.productName!="MitziValidation")throw new Exception("Use isolated validation project");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SaveService.ResetFirstPlayForTesting();
        var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
        var flow=RuntimeBootstrap.Build(catalog);
        typeof(GameFlow).GetProperty("Instance").SetValue(null,flow);
        Call(flow.GetComponent<LevelManager>(),"Awake");
        Call(flow.GetComponent<PawPath.Ads.RewardedAdService>(),"Awake");
        Call(Camera.main.GetComponent<LineDraw>(),"Awake");
        Call(flow.GetComponentInChildren<CatController>(true),"Awake");
        foreach(var component in flow.GetComponentsInChildren<MonoBehaviour>(true))Call(component,"OnEnable");
        var camera=Camera.main; camera.aspect=16f/9f;
        foreach(var canvas in flow.GetComponentsInChildren<Canvas>(true))
        {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=200;}
        flow.EnterHub();
        var house=flow.GetComponentInChildren<CatHouseManager>(true);
        foreach(var component in flow.GetComponentsInChildren<PawPath.Ads.RewardedAdButton>(true)){Call(component,"Awake");Call(component,"Refresh");}
        var resident=house.GetComponentInChildren<Animator>();
        if(resident!=null){resident.Play("Sit",0,0);resident.Update(0);}
        Directory.CreateDirectory(Folder);Capture(camera,"home",1920,1080);
        PlayerPrefs.SetInt(GameplayTutorialUI.DirectSeenKey,1);
        PlayerPrefs.SetInt(GameplayTutorialUI.DrawingSeenKey,1);
        GameplayMode.Select(GameplayPlayMode.DirectControl);
        flow.StartNextLevel();
        var cat=CatController.Instance;
        if(cat==null)throw new Exception("Cat not initialized");
        var priorSimulation=Physics2D.simulationMode;Physics2D.simulationMode=SimulationMode2D.Script;
        Physics2D.SyncTransforms();for(int i=0;i<60;i++)Physics2D.Simulate(.02f);Physics2D.simulationMode=priorSimulation;
        Call(cat,"LateUpdate");
        Call(flow.GetComponentInChildren<HudView>(true),"OnLevelStarted");
        Call(flow.GetComponentInChildren<LevelProgressBar>(true),"LateUpdate");
        Capture(camera,"street",1920,1080);
        foreach(var renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())renderer.enabled=false;
        foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())canvas.gameObject.SetActive(false);
        var featureBackground=new GameObject("FeatureBackground").AddComponent<SpriteRenderer>();featureBackground.sprite=catalog.homeBackground;
        featureBackground.transform.localScale=Vector3.one*Mathf.Max(8.9f/catalog.homeBackground.bounds.size.y,8.9f*(1024f/500)/catalog.homeBackground.bounds.size.x);
        featureBackground.sortingOrder=-10;
        var featureCat=new GameObject("FeatureMitzi").AddComponent<SpriteRenderer>();featureCat.sprite=catalog.GetCat("mitzi").idleSprite;
        featureCat.transform.localScale=Vector3.one*(4f/featureCat.sprite.bounds.size.y);featureCat.transform.position=new Vector3(3,-3.3f,0);featureCat.sortingOrder=10;
        var featureCanvas=new GameObject("FeatureCanvas").AddComponent<Canvas>();
        featureCanvas.renderMode=RenderMode.ScreenSpaceCamera;featureCanvas.worldCamera=camera;featureCanvas.planeDistance=1;featureCanvas.sortingOrder=250;
        var scale=featureCanvas.gameObject.AddComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1024,500);
        var title=new GameObject("Title",typeof(RectTransform),typeof(Text));title.transform.SetParent(featureCanvas.transform,false);
        var text=title.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text="Mitzi";text.fontSize=108;text.fontStyle=FontStyle.Bold;text.color=new Color(1,.97f,.87f);text.alignment=TextAnchor.MiddleLeft;
        text.rectTransform.sizeDelta=new Vector2(420,160);text.rectTransform.anchoredPosition=new Vector2(-240,110);
        var outline=title.AddComponent<Outline>();outline.effectColor=new Color(.3f,.18f,.1f);outline.effectDistance=new Vector2(3,-3);
        var subtitle=new GameObject("Subtitle",typeof(RectTransform),typeof(Text));subtitle.transform.SetParent(featureCanvas.transform,false);
        var sub=subtitle.GetComponent<Text>();sub.font=text.font;sub.text="Bir pati dolusu macera";sub.fontSize=30;sub.fontStyle=FontStyle.Bold;sub.color=text.color;sub.alignment=TextAnchor.MiddleLeft;
        sub.rectTransform.sizeDelta=new Vector2(450,70);sub.rectTransform.anchoredPosition=new Vector2(-215,30);
        subtitle.AddComponent<Outline>().effectColor=outline.effectColor;
        cat.transform.position=new Vector3(1.5f,cat.transform.position.y,cat.transform.position.z);
        Physics2D.SyncTransforms();Call(cat,"LateUpdate");
        camera.aspect=1024f/500;Capture(camera,"feature",1024,500);featureCanvas.gameObject.SetActive(false);
        // The store icon is rendered by Unity from the exact shipped artwork.
        foreach(var renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())renderer.enabled=false;
        foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())canvas.gameObject.SetActive(false);
        var icon=new GameObject("StoreIcon").AddComponent<SpriteRenderer>();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/AppIcon/paw_icon.png");
        icon.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100);
        camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=texture.height/200f;camera.aspect=1;
        icon.transform.position=Vector3.zero;Capture(camera,"icon",512,512);
        Debug.Log("PLAY_STORE_CAPTURE_PASS home=1920x1080 street=1920x1080 icon=512x512");
    }
    static void Capture(Camera camera,string name,int width,int height)
    {
        var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();
        RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),image.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
    }
}
