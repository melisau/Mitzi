#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Levels;
using PawPath.UI;
using UnityEditor;
using UnityEngine;

public static class RescueGreetingValidation
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Hidden).Invoke(obj,args);
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Hidden).SetValue(obj,value);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void ValidateDogOnly()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated project");TestDogs(AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset"));
            var root=new GameObject("MeetingFailureGuard");var flow=root.AddComponent<GameFlow>();flow.BeginRescueMeeting();flow.ShowLevelFailure("Late hazard contact");
            Check(flow.State==GameFlowState.RescueMeeting,"Late hazard interrupts completed rescue");flow.EnterHub();UnityEngine.Object.DestroyImmediate(root);
            Debug.Log("DOG_PASSAGE_BOUNDARIES_AND_MEETING_GUARD_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated project");
            GhostCatBuilder.Apply();RescueHugArtBuilder.Apply();NewCatsAndAreasValidation.Run();
            var flow=GameFlow.Instance;var catalog=flow.Catalog;
            foreach(var cat in catalog.cats)Check(cat.rescueHugFrames.Length==6&&cat.rescueHugFrames.All(s=>s!=null)&&cat.rescueHugContactX>0,"Missing own hug poses "+cat.id);
            foreach(string id in new[]{"ghost","boza"})
            {
                var cat=catalog.GetCat(id);Check(cat.walkFrames.Length==8&&cat.walkCycleSeconds==.8f,id+" old fast gait");
                var clip=(cat.animator as UnityEditor.Animations.AnimatorController).layers[0].stateMachine.states.Single(s=>s.state.name=="Walk").state.motion as AnimationClip;
                Check(Mathf.Abs(clip.length-.8f)<.001f&&Mathf.Approximately(clip.frameRate,10),id+" incorrect gait cadence");
                Check(cat.walkFrames.Select(s=>s.pixelsPerUnit).Distinct().Count()==1,id+" scale pulses between gait frames");
            }
            var ghost=catalog.GetCat("ghost");Check(AssetDatabase.GetAssetPath(ghost.portrait).EndsWith("/ghost/portrait.png"),"Victory portrait not replaced");
            Check(ghost.jumpFrames.All(s=>AssetDatabase.GetAssetPath(s).EndsWith("/ghost/jump.png")),"Old clipped jump used");
            Check(ghost.visualHeightMultiplier*ghost.homeVisibleHeightRatio<catalog.StarterCat.homeVisibleHeightRatio*.8f,"Ghost remains oversized");
            var player=flow.GetComponentInChildren<CatController>(true);Call(player,"Awake");
            var meeting=flow.GetComponent<RescueGreeting>()??flow.gameObject.AddComponent<RescueGreeting>();
            int pairs=0;
            foreach(var first in catalog.cats)foreach(var second in catalog.cats.Where(c=>c!=first))
            {
                player.ApplyCat(first);player.transform.position=new Vector3(2,0,0);
                var waiting=new GameObject("WaitingTest");var sr=waiting.AddComponent<SpriteRenderer>();sr.sprite=second.idleSprite;waiting.transform.position=Vector3.zero;
                int finished=0;Check(meeting.Play(player,second,waiting,()=>{finished++;flow.ShowRescue(second);}),"Greeting failed "+first.id+"/"+second.id);
                Check(flow.State==GameFlowState.RescueMeeting&&Time.timeScale==0&&flow.GameplayPaused,"World not frozen for hug");
                meeting.Advance(1.7f);
                Check(first.rescueHugFrames.Contains(meeting.RescuerRenderer.sprite)&&second.rescueHugFrames.Contains(meeting.RescuedRenderer.sprite),"Wrong coat at hug");
                Check(Mathf.Sign(meeting.RescuerRenderer.transform.localScale.x)!=Mathf.Sign(meeting.RescuedRenderer.transform.localScale.x),"Cats face same way");
                Check(Mathf.Abs(meeting.RescuerRenderer.bounds.min.y)<.01f&&Mathf.Abs(meeting.RescuedRenderer.bounds.min.y)<.01f,"Hug feet float or sink");
                meeting.Advance(2f);meeting.Advance(10f);Check(finished==1&&flow.State==GameFlowState.Rescue,"Greeting did not finish once");
                Check(sr.enabled,"Waiting renderer not restored");UnityEngine.Object.DestroyImmediate(waiting);pairs++;
            }
            var level=flow.GetComponent<LevelManager>();SaveService.Data.highestCompletedLevel=6;SaveService.Data.unlockedCatIds=new System.Collections.Generic.List<string>{"mitzi"};SaveService.Data.selectedCatId="mitzi";
            Call(level,"Awake");level.BeginCurrentLevel();player.transform.position=level.GoalPosition;level.CompleteLevel();
            Check(meeting.IsPlaying&&SaveService.HasCat("ghost")&&flow.State==GameFlowState.RescueMeeting,"Real rescue skips hug");
            int coins=SaveService.Data.lovePoints;level.CompleteLevel();Check(SaveService.Data.lovePoints==coins,"Hug grants duplicate reward");
            meeting.Advance(1.7f);Render(meeting,"rescue-hug-preview.png");meeting.Advance(2f);Check(flow.State==GameFlowState.Rescue,"Real rescue never presents invite screen");
            TestDogs(catalog);
            Debug.Log("RESCUE_GREETING_AND_GHOST_VALIDATION_PASS pairs="+pairs+" dog=ground,airborne,spawn,nearest gait=8@10fps");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void TestDogs(PawPathCatalog catalog)
    {
        foreach(var old in UnityEngine.Object.FindObjectsOfType<CrouchPassage>())old.gameObject.SetActive(false);
        var root=new GameObject("DogPassageTest");root.transform.position=new Vector3(500,0);var passage=root.AddComponent<CrouchPassage>();passage.Configure(3,0,.62f);
        var go=new GameObject("DogTest");var visual=new GameObject("Visual").AddComponent<SpriteRenderer>();visual.transform.SetParent(go.transform,false);visual.sprite=catalog.dogRunFrames[0];var dog=go.AddComponent<DogObstacle>();
        go.transform.position=new Vector3(500,0);dog.Configure(visual,catalog.dogRunFrames,2,490,510,0,3,.05f,.1f,null,1,catalog.dogIdleSprite);
        float reach=1.62f;Check(go.transform.position.x<passage.Left-reach||go.transform.position.x>passage.Right+reach,"Dog spawns inside passage");
        foreach(int direction in new[]{-1,1})
        {
            float start=direction>0?passage.Left-reach-1:passage.Right+reach+1;go.transform.position=new Vector3(start,0);Set(dog,"direction",direction);Set(dog,"jumpingGap",false);
            Check((bool)Call(dog,"StopAtCrouchPassage",2f),"Running dog crosses low beam");Check(Mathf.Abs(go.transform.position.x-(direction>0?passage.Left-reach:passage.Right+reach))<.001f,"Dog not stopped before nose hits beam");
            go.transform.position=new Vector3(start,1);Set(dog,"jumpingGap",true);Set(dog,"jumpStartX",start);Set(dog,"jumpEndX",start+direction*10);Set(dog,"jumpDuration",1f);Set(dog,"jumpElapsed",0f);
            Check((bool)Call(dog,"StopAtCrouchPassage",1f)&&!(bool)typeof(DogObstacle).GetField("jumpingGap",Hidden).GetValue(dog),"Gap-jumping dog passes through obstacle");
        }
        var farther=new GameObject("FartherPassage");farther.transform.position=new Vector3(520,0);farther.AddComponent<CrouchPassage>().Configure(3,0,.62f);
        Check(CrouchPassage.BlocksDog(530,-40,reach,out float nearest)&&Mathf.Abs(nearest-(521.5f+reach))<.001f,"Dog skips closest passage on a large step");
        Check(!CrouchPassage.BlocksDog(passage.Right+reach,.1f,reach,out _),"Stopped dog cannot retreat away from passage");
        UnityEngine.Object.DestroyImmediate(farther);UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(root);
    }
    static void Render(RescueGreeting greeting,string name)
    {
        var cam=Camera.main;var before=cam.transform.position;float size=cam.orthographicSize;
        var a=greeting.RescuerRenderer.bounds;var b=greeting.RescuedRenderer.bounds;
        cam.transform.position=new Vector3((a.center.x+b.center.x)*.5f,(a.min.y+Mathf.Max(a.max.y,b.max.y))*.5f,-10);cam.orthographicSize=2.2f;cam.aspect=16f/9f;
        var target=new RenderTexture(1280,720,24);cam.targetTexture=target;cam.Render();RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/"+name,image.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;cam.transform.position=before;cam.orthographicSize=size;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
    }
}
#endif
