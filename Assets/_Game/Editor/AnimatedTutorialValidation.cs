#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Gameplay;
using PawPath.UI;
using PawPath.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AnimatedTutorialValidation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,Private).Invoke(obj,args);
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static Button Button(GameplayTutorialUI ui,string name) => ui.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use the isolated project");
            AnimalAnimationValidation.BuildAndValidate();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.aspect=16f/9f;
            var flow=RuntimeBootstrap.Build(catalog);
            typeof(GameFlow).GetProperty("Instance").SetValue(null,flow);
            var drawing=camera.GetComponent<LineDraw>(); Call(drawing,"Awake");
            var hud=flow.GetComponentInChildren<HudView>(true);
            var tutorial=flow.GetComponentInChildren<GameplayTutorialUI>(true);
            foreach(var canvas in flow.GetComponentsInChildren<Canvas>(true))
            { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=5f; canvas.sortingOrder=200; }
            PlayerPrefs.DeleteKey(GameplayTutorialUI.DrawingSeenKey); PlayerPrefs.DeleteKey(GameplayTutorialUI.DirectSeenKey);
            GameplayMode.Select(GameplayPlayMode.Drawing); flow.StartNextLevel(); Call(hud,"OnLevelStarted");
            Check(flow.State==GameFlowState.InfoOverlay && Time.timeScale==0f && tutorial.gameObject.activeSelf,"Drawing intro did not pause gameplay");
            Check(!GameplayTutorialUI.HasSeen(GameplayPlayMode.Drawing) && tutorial.PageCount==6,"Drawing marked seen before close");
            tutorial.Demo.RenderAt(.9f); int before=tutorial.Demo.VisiblePathSegments;
            tutorial.Demo.RenderAt(2f); Check(tutorial.Demo.VisiblePathSegments>before,"Finger does not draw a path");
            tutorial.Demo.RenderAt(1.6f); Render(camera,"tutorial-draw.png");
            Button(tutorial,"NextLesson").onClick.Invoke();
            tutorial.Demo.RenderAt(2f); Render(camera,"tutorial-tools.png");
            Button(tutorial,"NextLesson").onClick.Invoke();
            Check(tutorial.PageIndex==2 && Button(tutorial,"NextLesson").gameObject.activeSelf,"Drawing page navigation failed");
            tutorial.Demo.RenderAt(6.8f); Render(camera,"tutorial-views.png");
            CheckSharedLessons(tutorial,camera);
            Button(tutorial,"TryIt").onClick.Invoke();
            Check(GameplayTutorialUI.HasSeen(GameplayPlayMode.Drawing) && !GameplayTutorialUI.HasSeen(GameplayPlayMode.DirectControl),"Modes share a seen flag");
            Check(flow.State==GameFlowState.DrawingPreparation,"Drawing intro resumed the wrong state");
            Check(!(bool)Call(hud,"ShowGameplayTutorialOnce"),"Completed drawing lesson appeared again");

            GameplayMode.Select(GameplayPlayMode.DirectControl); flow.StartNextLevel(); Call(hud,"OnLevelStarted");
            Check(flow.State==GameFlowState.InfoOverlay && tutorial.PageCount==7,"Direct-control intro missing");
            tutorial.Demo.RenderAt(.5f); var start=tutorial.Demo.CatPosition;
            tutorial.Demo.RenderAt(1.5f); Check(tutorial.Demo.CatPosition.x>start.x,"Move example is static while paused");
            Render(camera,"tutorial-move.png");
            Button(tutorial,"NextLesson").onClick.Invoke();
            var mound=tutorial.Demo.transform.Find("RealMound").GetComponent<Image>();
            Check(mound.sprite!=null,"Tutorial mound sprite is null; catalog="+flow.Catalog.moundSprite+" direct="+AssetDatabase.LoadAssetAtPath<Sprite>("Assets/street_mound.png"));
            tutorial.Demo.RenderAt(1.45f); float low=tutorial.Demo.CatPosition.y;
            tutorial.Demo.RenderAt(4.7f); Check(tutorial.Demo.CatPosition.y>low+25f,"Double tap does not jump higher");
            Render(camera,"tutorial-jump.png");
            Button(tutorial,"NextLesson").onClick.Invoke(); tutorial.Demo.RenderAt(2.3f); Render(camera,"tutorial-crouch.png");
            Button(tutorial,"NextLesson").onClick.Invoke(); tutorial.Demo.RenderAt(1.8f); Render(camera,"tutorial-climb.png");
            CheckSharedLessons(tutorial,camera);
            Button(tutorial,"TryIt").onClick.Invoke();
            Check(flow.State==GameFlowState.Gameplay && Time.timeScale==1f,"Direct lesson did not resume gameplay");
            Check(!(bool)Call(hud,"ShowGameplayTutorialOnce"),"Completed direct lesson appeared again");
            var guide=flow.GetComponentInChildren<HowToPlayUI>(true);
            Call(guide,"Show",4);
            Check(guide.GetComponentsInChildren<GameplayTutorialDemo>().Single().Kind==TutorialDemoKind.DrawPath,"Settings drawing guide is static");
            guide.GetComponentsInChildren<GameplayTutorialDemo>().Single().RenderAt(1.7f); Render(camera,"how-to-draw.png");
            Call(guide,"Show",5);
            Check(guide.GetComponentsInChildren<GameplayTutorialDemo>().Single().Kind==TutorialDemoKind.Move,"Settings controls guide is static");
            guide.GetComponentsInChildren<GameplayTutorialDemo>().Single().RenderAt(1.7f); Render(camera,"how-to-controls.png");
            Call(guide,"Close");
            CheckCareScale(catalog);
            RenderSizes(catalog,camera);
            Debug.Log("ANIMATED_TUTORIAL_VALIDATION_PASS");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        finally { Time.timeScale=1f; }
    }
    static void CheckSharedLessons(GameplayTutorialUI tutorial,Camera camera)
    {
        Button(tutorial,"NextLesson").onClick.Invoke();
        Check(tutorial.Demo.Kind==TutorialDemoKind.DogStomp,"Dog stomp lesson missing");
        tutorial.Demo.RenderAt(1.8f); Check(tutorial.Demo.DogVisible,"Dog must exist before stomp"); Render(camera,"tutorial-stomp-before.png");
        tutorial.Demo.RenderAt(2.8f); Check(!tutorial.Demo.DogVisible,"Dog does not disappear after stomp"); Render(camera,"tutorial-stomp-after.png");
        Button(tutorial,"NextLesson").onClick.Invoke();
        Check(tutorial.Demo.Kind==TutorialDemoKind.DogContact,"Dog danger lesson missing");
        tutorial.Demo.RenderAt(2.1f); Render(camera,"tutorial-dog-contact.png");
        Button(tutorial,"NextLesson").onClick.Invoke();
        Check(tutorial.Demo.Kind==TutorialDemoKind.BirdReward,"Bird reward lesson missing");
        tutorial.Demo.RenderAt(1f); Check(tutorial.Demo.BirdVisible && !tutorial.Demo.RewardVisible,"Bird reward shown too early");
        tutorial.Demo.RenderAt(2.7f); Check(!tutorial.Demo.BirdVisible && tutorial.Demo.RewardVisible,"Bird collection reward missing"); Render(camera,"tutorial-bird-reward.png");
        Check(Button(tutorial,"TryIt").gameObject.activeSelf,"Final lesson button missing");
    }
    static void CheckCareScale(PawPathCatalog catalog)
    {
        var root=new GameObject("CareScaleValidation");
        var home=root.AddComponent<CatHomeBehaviour>(); home.BindDefinition(catalog.GetCat("komur"));
        var visual=new GameObject("Visual"); visual.transform.SetParent(root.transform);
        var renderer=visual.AddComponent<SpriteRenderer>(); renderer.sprite=home.Definition.walkFrames[5];
        float before=renderer.sprite.bounds.min.y+renderer.sprite.bounds.size.y*.08f;
        var item=new GameObject("FoodValidation").AddComponent<FurnitureInteraction>();
        var sequence=(System.Collections.IEnumerator)Call(item,"PlayInteractionFrames",home,home.Definition.eatFrames,2f);
        Check(sequence.MoveNext(),"Care animation did not begin");
        float after=visual.transform.localPosition.y+(renderer.sprite.bounds.min.y+renderer.sprite.bounds.size.y*.08f)*visual.transform.localScale.y;
        Check(Mathf.Abs(visual.transform.localScale.y-1f)<.001f && Mathf.Abs(after-before)<.01f,"Walking calibration changed feeding size or paw baseline");
        ((IDisposable)sequence).Dispose();
        UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(item.gameObject);
    }
    static void RenderSizes(PawPathCatalog catalog,Camera camera)
    {
        foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>()) canvas.gameObject.SetActive(false);
        foreach(var sr in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>()) sr.gameObject.SetActive(false);
        camera.transform.position=new Vector3(0,0,-10); camera.orthographicSize=5.5f;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.81f,.87f,.87f);
        string[] ids={"mitzi","komur","tarcin","ada"};
        for(int i=0;i<ids.Length;i++)
        {
            var definition=catalog.GetCat(ids[i]);
            Check(definition.crouchWalkFrames!=null && definition.crouchWalkFrames.Length==4,"Crouch demo frames missing");
            for(int j=0;j<2;j++)
            {
                var image=new GameObject(ids[i]+(j==0 ? "_idle":"_walk")).AddComponent<SpriteRenderer>();
                image.sprite=j==0 ? definition.idleSprite:definition.walkFrames[5];
                image.transform.position=new Vector3(j==0 ? -2.3f:2.3f,3f-i*2.1f,0);
            }
        }
        Render(camera,"idle-walk-size-comparison.png");
    }
    static void Render(Camera camera,string file)
    {
        Canvas.ForceUpdateCanvases();
        Directory.CreateDirectory("AnimalArtSources-20261018");
        var target=new RenderTexture(1280,720,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        File.WriteAllBytes("AnimalArtSources-20261018/"+file,image.EncodeToPNG());
        RenderTexture.active=null; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
    }
}
#endif
