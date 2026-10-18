#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Hub;
using PawPath.Levels;
using PawPath.Economy;
using PawPath.Content;

public static class GroundedObstacleValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Private).SetValue(obj,value);
    static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Private).Invoke(obj,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            HomeSettingsValidation.Run();
            RescueMilestoneBuilder.Apply();
            var data=SaveService.Data;
            data.homeRoomStyle0=0;data.homeRoomStyle0Chosen=false;
            Check(HomeRooms.StyleForRoom(0)==3,"Old unselected room does not default blue");
            data.activeHomeRoom=0;HomeRooms.SelectStyle(0);
            Check(HomeRooms.StyleForRoom(0)==0,"Explicit classic choice ignored");
            Check(new SaveService.SaveData().homeRoomStyle0==3,"New game does not default blue");
            var eco=new GameObject("EconomyTest").AddComponent<CozyEconomyManager>();
            var item=ScriptableObject.CreateInstance<ShopItemDefinition>();item.id="grounded-price-test";item.lovePointCost=40;
            data.itemPurchases.RemoveAll(p=>p.itemId==item.id);data.lovePoints=2000;
            foreach(int price in new[]{40,120,360,1080})
            {Check(eco.GetPurchasePrice(item)==price,"Price multiplier is not three");Check(eco.TryBuy(item),"Purchase failed");}
            Check(eco.GetPurchasePrice(item)==3240 && !eco.TryBuy(item),"Insufficient purchase changed price");
            var catalog=GameFlow.Instance.Catalog;
            foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())canvas.gameObject.SetActive(false);
            foreach(var sr in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())sr.gameObject.SetActive(false);
            var root=new GameObject("PassageCat");root.transform.position=new Vector3(1000,.336f);
            var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
            var sprite=visual.AddComponent<SpriteRenderer>();sprite.sortingOrder=10;visual.AddComponent<Animator>();
            var cat=root.AddComponent<CatController>();Set(cat,"visual",visual.transform);Call(cat,"Awake");cat.ApplyCat(catalog.StarterCat);
            var circle=root.GetComponent<CircleCollider2D>();circle.radius=.336f;
            var passage=new GameObject("Passage").AddComponent<CrouchPassage>();passage.transform.position=new Vector3(1000,0);passage.Configure(4.2f,0,CrouchPassage.PassageClearance);
            Check(passage.Clearance<DisplaySizeSettings.CatScale*.7f,"Ceiling too high");
            root.transform.position=new Vector3(1000,.336f);Physics2D.SyncTransforms();
            Check(CrouchPassage.NeedsCrouch(cat),"Cat can stand inside passage");
            var head=root.transform.Find("StandingProfile").GetComponent<BoxCollider2D>();
            Check(head.enabled && head.bounds.max.y>passage.Clearance,"Standing profile fits below beam");
            Call(cat,"SetCrouching",true,true);Physics2D.SyncTransforms();
            Check(!head.enabled && circle.bounds.max.y<passage.Clearance,"Crouch profile does not fit");
            Call(cat,"SetCrouching",false,true);
            Set(cat,"keyboardJumpQueued",true);Set(cat,"jumpsRemaining",2);root.GetComponent<Rigidbody2D>().velocity=Vector2.zero;
            Call(cat,"TryJump");Check(root.GetComponent<Rigidbody2D>().velocity.y>0,"Jump still invisibly disabled inside passage");
            root.transform.position=new Vector3(990,.336f);Set(cat,"keyboardJumpQueued",true);Call(cat,"TryJump");
            Check(root.GetComponent<Rigidbody2D>().velocity.y>0,"Jump disabled outside passage");
            Set(cat,"intentionalJump",false);Set(cat,"jumpAnimationPlaying",false);root.GetComponent<Rigidbody2D>().velocity=Vector2.zero;
            var art=new GameObject("GroundedArt").AddComponent<SpriteRenderer>();art.transform.SetParent(passage.transform,false);
            var ground=new GameObject("Ground");ground.transform.position=new Vector3(1000,-.5f);ground.AddComponent<BoxCollider2D>().size=new Vector2(20,1);
            var groundArt=ground.AddComponent<SpriteRenderer>();groundArt.sprite=PawPath.Hub.FallbackSprite.WhiteSquare();groundArt.drawMode=SpriteDrawMode.Sliced;groundArt.size=new Vector2(20,1);groundArt.sortingOrder=4;groundArt.color=new Color(.33f,.25f,.16f);
            Physics2D.SyncTransforms();
            var cam=Camera.main;cam.transform.position=new Vector3(1000,1.2f,-10);cam.orthographicSize=3;cam.aspect=16f/9f;
            for(int theme=0;theme<4;theme++)
            {
                art.sprite=catalog.CrouchObstacleForTheme(theme);Check(art.sprite!=null && art.sprite.name.Contains("grounded"),"Floating old asset remains");
                CrouchPassage.FitArtwork(art,4.2f,passage.Clearance);
                Check(Mathf.Abs(art.bounds.min.y+.06f)<.01f,"Rear support floats off ground");
                var backdrop=new GameObject("Backdrop").AddComponent<SpriteRenderer>();backdrop.sortingOrder=-5;
                backdrop.sprite=theme==1?catalog.forestBackground:theme==2?catalog.cityBackground:theme==3?catalog.cyberBackground:catalog.gameplayBackground;
                backdrop.transform.position=new Vector3(1000,1.2f,1);
                if(backdrop.sprite!=null)backdrop.transform.localScale=Vector3.one*(6/backdrop.sprite.bounds.size.y);
                sprite.sprite=catalog.StarterCat.crouchWalkFrames[1];root.transform.position=new Vector3(1000,.336f);Call(cat,"AlignVisualFeet");
                Render(cam,"grounded-obstacle-"+theme+".png");UnityEngine.Object.DestroyImmediate(backdrop.gameObject);
            }
            UnityEngine.Object.DestroyImmediate(passage.gameObject);UnityEngine.Object.DestroyImmediate(ground);
            var hill=new GameObject("Hill");hill.transform.position=new Vector3(1000,0);hill.AddComponent<PolygonCollider2D>().points=new[]{new Vector2(-3,-1),new Vector2(-3,0),new Vector2(0,1.8f),new Vector2(3,0),new Vector2(3,-1)};
            sprite.sprite=catalog.StarterCat.walkFrames[4];Set(cat,"intentionalJump",false);Set(cat,"jumpAnimationPlaying",false);
            for(int i=-12;i<=12;i++)
            {
                float x=i*.15f,y=1.8f-Mathf.Abs(x)*.6f;
                root.transform.position=new Vector3(1000+x,y+.39f);Physics2D.SyncTransforms();root.GetComponent<Rigidbody2D>().velocity=new Vector2(2,i<=0?1.2f:-1.2f);
                Call(cat,"UpdateJumpAnimation",false);Call(cat,"ApplyAirbornePose");Call(cat,"AlignVisualFeet");
                Check(sprite.sprite!=catalog.StarterCat.jumpApexSprite && !(bool)typeof(CatController).GetField("jumpAnimationPlaying",Private).GetValue(cat),"Summit changes to jumping pose");
                float baseline=sprite.sprite.bounds.min.y+sprite.sprite.bounds.size.y*.08f;
                Vector3 feet=visual.transform.TransformPoint(new Vector3(0,baseline));Check(feet.y>=y+.02f,"Ascending cat sinks into hill");
            }
            UnityEngine.Object.DestroyImmediate(hill);UnityEngine.Object.DestroyImmediate(root);
            data.unlockedCatIds=new System.Collections.Generic.List<string>{"mitzi"};
            Check(CatUnlockService.WaitingAtLevel(5)==null && CatUnlockService.WaitingAtLevel(7)?.id=="pamuk","Wrong rescue cadence");
            Check(!CatUnlockService.TryUnlockAfterLevel(5,out _) && CatUnlockService.TryUnlockAfterLevel(7,out var rescued) && rescued.id=="pamuk","Rescue does not occur at seven");
            Check(CatUnlockService.WaitingAtLevel(7)==null && !CatUnlockService.TryUnlockAfterLevel(7,out _),"Same cat rescued twice");
            int[] milestones={20,30,40,50};string[] rescueIds={"komur","tarcin","ada","moka"};
            for(int i=0;i<milestones.Length;i++)
            {
                Check(CatUnlockService.WaitingAtLevel(milestones[i])?.id==rescueIds[i],"Later rescue milestone wrong");
                Check(CatUnlockService.TryUnlockAfterLevel(milestones[i],out var next)&&next.id==rescueIds[i],"Later rescue failed");
            }
            data.unlockedCatIds=new System.Collections.Generic.List<string>{"mitzi","pamuk"};
            var level=GameFlow.Instance.GetComponent<LevelManager>();
            Call(level,"PlaceWaitingRescueCat",20);
            var waiting=(GameObject)typeof(LevelManager).GetField("waitingRescueCat",Private).GetValue(level);
            Check(waiting!=null && waiting.name=="WaitingRescueCat_komur","Rescue cat absent at gate");
            var marker=(Transform)typeof(LevelManager).GetField("goalMarker",Private).GetValue(level);
            Check(Mathf.Abs(waiting.transform.position.x-marker.position.x+1.1f)<.01f && waiting.GetComponent<Collider2D>()==null,"Waiting cat blocks gate or is misplaced");
            Check(CatUnlockService.TryUnlockAfterLevel(20,out _),"Gate rescue did not complete");
            Call(level,"PlaceWaitingRescueCat",21);
            Check(typeof(LevelManager).GetField("waitingRescueCat",Private).GetValue(level)==null,"Rescue cat persists in ordinary level");
            Debug.Log("GROUNDED_OBSTACLE_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void Render(Camera cam,string name)
    {
        var target=new RenderTexture(1280,720,24);cam.targetTexture=target;cam.Render();RenderTexture.active=target;
        var im=new Texture2D(1280,720,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1280,720),0,0);im.Apply();
        File.WriteAllBytes("AnimalArtSources-20261018/"+name,im.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(target);
    }
}
#endif
