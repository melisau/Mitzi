#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Data;
using PawPath.Hub;
using UnityEngine;

public static class HomeDirectionValidation
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated validation project");
            GhostCatBuilder.Apply();HomeDirectionArtBuilder.Apply();
            HomeManualControlValidation.Run();
            var flow=GameFlow.Instance;flow.EnterHub();
            var catalog=flow.Catalog;
            Check(catalog.cats.Count==8,"Expected eight cats including Ghost and Boza");
            int tested=0;
            foreach(var cat in catalog.cats)
            {
                Check(cat.homeTowardFrames.Length==6&&cat.homeAwayFrames.Length==6,cat.id+" missing home views");
                Check(cat.homeTowardFrames.Concat(cat.homeAwayFrames).All(s=>s!=null&&AssetDatabasePath(s).StartsWith(HomeDirectionArtBuilder.Root)),cat.id+" missing direction asset");
                Check(cat.walkFrames.All(s=>AssetDatabasePath(s).Contains((cat.id=="ghost"||cat.id=="boza")?"/"+cat.id+"/walk.png":"/videowalk_")),cat.id+" level gait replaced");
                Check(cat.homeVisibleHeightRatio>.1f&&cat.homeVisibleHeightRatio<=1,cat.id+" invalid visible scale");
                var root=Resident(cat);var home=root.GetComponent<CatHomeBehaviour>();var direction=root.GetComponent<HomeDirectionalAnimator>();
                var original=root.transform.Find("Visual").GetComponent<SpriteRenderer>();
                var originalScale=original.transform.localScale;var originalPosition=original.transform.localPosition;
                home.SetManualInput(Vector2.down);Call(home,"AdvanceManualMovement",.1f);direction.Refresh(.12f);
                Check(direction.CurrentView==HomeDirectionalAnimator.View.Toward&&!original.enabled&&direction.DirectionRenderer.enabled,cat.id+" did not face viewer");
                Check(cat.homeTowardFrames.Contains(direction.DirectionRenderer.sprite),cat.id+" uses wrong front coat");
                var first=direction.DirectionRenderer.sprite;float scale=direction.DirectionRenderer.transform.localScale.y;
                direction.Refresh(.2f);Check(direction.DirectionRenderer.sprite!=first,cat.id+" front walking is a still image");
                Check(Mathf.Approximately(scale,direction.DirectionRenderer.transform.localScale.y),cat.id+" front scale pulses");
                home.SetManualInput(Vector2.zero);Call(home,"Update");direction.Refresh(.2f);var idle=direction.DirectionRenderer.sprite;
                direction.Refresh(.2f);Check(idle==direction.DirectionRenderer.sprite&&direction.CurrentView==HomeDirectionalAnimator.View.Toward,cat.id+" does not rest facing viewer");
                home.SetManualInput(Vector2.up);Call(home,"AdvanceManualMovement",.1f);direction.Refresh(.12f);
                Check(direction.CurrentView==HomeDirectionalAnimator.View.Away&&cat.homeAwayFrames.Contains(direction.DirectionRenderer.sprite),cat.id+" did not face away");
                home.SetManualInput(Vector2.right);Call(home,"AdvanceManualMovement",.1f);direction.Refresh(.12f);
                Check(direction.CurrentView==HomeDirectionalAnimator.View.Side&&original.enabled&&!direction.DirectionRenderer.enabled,cat.id+" side walk not restored");
                Check(original.transform.localPosition==originalPosition&&Mathf.Approximately(Mathf.Abs(original.transform.localScale.x),Mathf.Abs(originalScale.x)),cat.id+" side visual mutated");
                home.SetExternalAnimation(true);direction.Refresh(.1f);Check(original.enabled&&!direction.DirectionRenderer.enabled,cat.id+" overrides care animation");home.SetExternalAnimation(false);
                home.SleepFor(10);direction.Refresh(.1f);Check(!direction.DirectionRenderer.enabled,cat.id+" overrides sleep");
                UnityEngine.Object.DestroyImmediate(root);tested++;
            }
            var preview=new GameObject("HomeDirectionPreview");
            for(int i=0;i<catalog.cats.Count;i++)for(int row=0;row<2;row++)
            {
                var root=Resident(catalog.cats[i]);root.transform.SetParent(preview.transform,false);
                root.layer=30;foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=30;
                var home=root.GetComponent<CatHomeBehaviour>();home.SetManualInput(row==0?Vector2.down:Vector2.up);Call(home,"AdvanceManualMovement",.1f);
                root.transform.position=new Vector3(-7f+i*2f,row==0?1f:-2f,0);
                root.GetComponent<HomeDirectionalAnimator>().Refresh(.25f);
            }
            var camera=new GameObject("HomeDirectionPreviewCamera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=3.5f;camera.aspect=16f/9f;
            camera.orthographicSize=4.8f;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.86f,.83f,.75f);
            var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/home-direction-preview.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
            flow.StartNextLevel();foreach(var view in preview.GetComponentsInChildren<HomeDirectionalAnimator>()){view.Refresh(.1f);Check(!view.DirectionRenderer.enabled,"Home view leaks into level");}
            UnityEngine.Object.DestroyImmediate(preview);UnityEngine.Object.DestroyImmediate(camera.gameObject);
            Debug.Log("HOME_DIRECTION_VALIDATION_PASS cats="+tested);
        }
        catch(Exception error){Debug.LogException(error);UnityEditor.EditorApplication.Exit(1);}
    }
    static string AssetDatabasePath(Sprite sprite)=>UnityEditor.AssetDatabase.GetAssetPath(sprite);
    static GameObject Resident(CatDefinition cat)
    {
        var root=new GameObject("DirectionTest_"+cat.id);root.transform.position=new Vector3(0,(HomeRooms.FloorMinY+HomeRooms.FloorMaxY)*.5f,0);
        var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=cat.idleSprite;
        visual.transform.localScale=Vector3.one*(2.75f*cat.visualHeightMultiplier/cat.idleSprite.bounds.size.y);
        var home=root.AddComponent<CatHomeBehaviour>();Call(home,"Awake");home.BindDefinition(cat);
        var view=root.AddComponent<HomeDirectionalAnimator>();view.Bind(cat,renderer,home);return root;
    }
}
#endif
