#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using PawPath.Core;
using PawPath.UI;
using PawPath.Levels;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class NewCatsAndAreasValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated validation project");
            GhostCatBuilder.Apply();HomeDirectionValidation.Run();AnimalAnimationValidation.BuildAndValidate();DifficultyProgressionValidation.Run();
            var flow=GameFlow.Instance;var data=SaveService.Data;
            data.highestCompletedLevel=0;PlayerPrefs.SetInt("PawPath.SelectedTheme",3);
            Check(ThemeSelectionUI.GetSelectedTheme(1)==0&&!ThemeSelectionUI.TrySelectTheme(1),"New game can enter locked area");
            foreach(int completed in new[]{0,9,10,19,20,29,30,99})
            {
                data.highestCompletedLevel=completed;
                for(int theme=0;theme<4;theme++)Check(ThemeSelectionUI.IsUnlocked(theme)==(completed>=theme*10),"Wrong area boundary");
            }
            foreach(int milestone in new[]{10,20,30})
            {
                data.highestCompletedLevel=milestone-1;data.unlockedCatIds=flow.Catalog.cats.Where(c=>c.unlockAfterLevel<milestone).Select(c=>c.id).ToList();
                ThemeSelectionUI.TrySelectTheme(0);
                var level=flow.GetComponent<LevelManager>();typeof(LevelManager).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(level,null);
                level.BeginCurrentLevel();level.CompleteLevel();
                var greeting=flow.GetComponent<RescueGreeting>();if(greeting!=null&&greeting.IsPlaying)greeting.Advance(RescueGreeting.Duration);
                var notice=UnityEngine.Object.FindObjectOfType<ThemeUnlockNotice>();
                Check(notice!=null&&notice.Theme==milestone/10,"Area notification absent on completion/rescue");
                Check(ThemeSelectionUI.GetSelectedTheme(milestone+1)==0,"Unlock forces a theme change");
                if(milestone==10)RenderNotice(flow);
                var button=notice.GetComponentsInChildren<Button>().Single(b=>b.name==(milestone==20?"ChooseNewArea":"KeepCurrentArea"));button.onClick.Invoke();
                Check(ThemeSelectionUI.GetSelectedTheme(milestone+1)==(milestone==20?2:0),"Area choice ignored");
                level.CompleteLevel();Check(UnityEngine.Object.FindObjectOfType<ThemeUnlockNotice>()==null,"Duplicate area notice on same completion");
                Check(ThemeSelectionUI.TrySelectTheme(0),"Previous area became locked");
            }
            var ghost=flow.Catalog.GetCat("ghost");var boza=flow.Catalog.GetCat("boza");
            Check(ghost.unlockAfterLevel==7&&boza.unlockAfterLevel==20&&flow.Catalog.GetCat("pamuk").unlockAfterLevel==60&&flow.Catalog.GetCat("komur").unlockAfterLevel==70,"Wrong final rescue schedule");
            Check(SaveService.TryReadJson("{\"schemaVersion\":4,\"highestCompletedLevel\":8,\"unlockedCatIds\":[\"mitzi\",\"pamuk\"]}",out var old,out _)&&old.unlockedCatIds.Contains("pamuk"),"Moved rescue removes existing owned cat");
            data.unlockedCatIds=flow.Catalog.cats.Select(c=>c.id).ToList();SaveService.Persist();
            Check(SaveService.TryReadJson(SaveService.ExportJson(),out var saved,out _)&&saved.unlockedCatIds.Contains("ghost")&&saved.unlockedCatIds.Contains("boza"),"New cats do not persist");
            RenderCats(ghost,boza);
            Debug.Log("NEW_CATS_AND_AREAS_VALIDATION_PASS cats=8 rescue=7,20,30,40,50,60,70 areas=10,20,30");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void RenderNotice(GameFlow flow)
    {
        var camera=Camera.main;camera.aspect=16f/9f;
        foreach(var canvas in flow.GetComponentsInChildren<Canvas>(true)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;}
        var target=new RenderTexture(1280,720,24);camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("AnimalArtSources-20261018/area-unlock-preview.png",image.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
    }
    static void RenderCats(params PawPath.Data.CatDefinition[] cats)
    {
        var parent=new GameObject("NewCatPreview");
        for(int row=0;row<cats.Length;row++)
        {
            var cat=cats[row];var frames=new[]{cat.idleSprite,cat.walkFrames[5],cat.jumpFrames[1],cat.crouchWalkFrames[1],cat.sleepFrames[4],cat.climbFrames[3],cat.portrait};
            float scale=2.2f*cat.visualHeightMultiplier/cat.idleSprite.bounds.size.y;
            for(int i=0;i<frames.Length;i++){var sr=new GameObject(cat.id+"_"+i).AddComponent<SpriteRenderer>();sr.gameObject.layer=30;sr.transform.SetParent(parent.transform);sr.sprite=frames[i];sr.transform.position=new Vector3(-9+i*3, row==0?1.5f:-2f);sr.transform.localScale=Vector3.one*scale;}
        }
        var camera=new GameObject("PreviewCamera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6;camera.aspect=16f/9f;camera.transform.position=new Vector3(0,1,-10);camera.cullingMask=1<<30;camera.backgroundColor=new Color(.7f,.75f,.77f);camera.clearFlags=CameraClearFlags.SolidColor;
        var target=new RenderTexture(1600,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/ghost-boza-preview.png",image.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(parent);
    }
}
#endif
