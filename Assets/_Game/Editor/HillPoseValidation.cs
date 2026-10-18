#if UNITY_EDITOR
using System;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using UnityEditor;
using UnityEngine;
public static class HillPoseValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
    static object Get(object o,string field)=>o.GetType().GetField(field,Private).GetValue(o);
    static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            HudTrimValidation.Run();
            var ground=new GameObject("HillPoseGround");ground.transform.position=new Vector3(1000,-.5f);ground.AddComponent<BoxCollider2D>().size=new Vector2(10,1);
            var root=new GameObject("HillPoseCat");root.transform.position=new Vector3(1000,.336f);
            var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);var sprite=visual.AddComponent<SpriteRenderer>();visual.AddComponent<Animator>();
            var cat=root.AddComponent<CatController>();root.GetComponent<CircleCollider2D>().radius=.336f;Set(cat,"visual",visual.transform);Call(cat,"Awake");cat.ApplyCat(GameFlow.Instance.Catalog.StarterCat);
            var definition=cat.Definition;var body=root.GetComponent<Rigidbody2D>();Physics2D.SyncTransforms();
            Set(cat,"jumpAnimationPlaying",true);Set(cat,"intentionalJump",false);sprite.sprite=definition.jumpApexSprite;
            body.velocity=new Vector2(2,1.1f);Call(cat,"UpdateJumpAnimation",true);Call(cat,"ApplyAirbornePose");
            Check(!(bool)Get(cat,"jumpAnimationPlaying") && sprite.sprite!=definition.jumpApexSprite,"Grounded hill walking shows apex");
            root.transform.position=new Vector3(1000,5);Physics2D.SyncTransforms();body.velocity=Vector2.zero;
            Set(cat,"jumpAnimationPlaying",true);Set(cat,"intentionalJump",false);Call(cat,"ApplyAirbornePose");
            Check(sprite.sprite!=definition.jumpApexSprite,"Accidental terrain separation curls cat");
            Set(cat,"intentionalJump",true);Call(cat,"ApplyAirbornePose");Check(sprite.sprite==definition.jumpApexSprite,"Actual jump lost apex pose");
            root.transform.position=new Vector3(1000,.336f);Physics2D.SyncTransforms();body.velocity=new Vector2(2,1.1f);Set(cat,"jumpLeftGround",true);Call(cat,"UpdateJumpAnimation",true);Call(cat,"ApplyAirbornePose");
            Check(!(bool)Get(cat,"intentionalJump") && sprite.sprite!=definition.jumpApexSprite,"Uphill landing retains jump pose");
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(ground);
            Debug.Log("HILL_POSE_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
