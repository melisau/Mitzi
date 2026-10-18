#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using PawPath.Cat;
using PawPath.Core;
using UnityEditor;
using UnityEngine;
public static class JumpApexValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object obj,string field,object value)=>obj.GetType().GetField(field,Private).SetValue(obj,value);
    static void Call(object obj,string method)=>obj.GetType().GetMethod(method,Private).Invoke(obj,null);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            PriceGrowthValidation.Run();
            foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())canvas.gameObject.SetActive(false);
            foreach(var renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())renderer.gameObject.SetActive(false);
            var catalog=GameFlow.Instance.Catalog;
            string[] ids={"mitzi","komur","tarcin","ada"};
            for(int row=0;row<ids.Length;row++)
            {
                var definition=catalog.GetCat(ids[row]);Check(definition.jumpApexSprite!=null,"Apex missing for "+ids[row]);
                var root=new GameObject("ApexValidation"+row);var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
                var sprite=visual.AddComponent<SpriteRenderer>();visual.AddComponent<Animator>();var cat=root.AddComponent<CatController>();
                Set(cat,"visual",visual.transform);Call(cat,"Awake");cat.ApplyCat(definition);Set(cat,"jumpAnimationPlaying",true);Set(cat,"intentionalJump",true);
                for(int column=0;column<3;column++)
                {
                    float speed=column==0 ? 5:column==1 ? 0:-5;
                    root.GetComponent<Rigidbody2D>().velocity=new Vector2(2,speed);Call(cat,"LateUpdate");
                    Check(sprite.sprite==(column==1 ? definition.jumpApexSprite:definition.jumpFrames[column==0 ? 1:2]),"Air pose does not follow vertical velocity");
                    var preview=new GameObject(ids[row]+"_pose"+column).AddComponent<SpriteRenderer>();preview.sprite=sprite.sprite;
                    preview.transform.localScale=visual.transform.localScale;preview.transform.position=new Vector3(-3.5f+column*3.5f,3f-row*2f)+visual.transform.localPosition;
                }
                var before=visual.transform.localScale;before.x=-Mathf.Abs(before.x);visual.transform.localScale=before;
                root.GetComponent<Rigidbody2D>().velocity=Vector2.zero;Call(cat,"LateUpdate");Check(visual.transform.localScale.x<0,"Apex flips left-facing cat");
                Set(cat,"climbing",true);sprite.sprite=definition.climbFrames[0];Call(cat,"ApplyAirbornePose");Check(sprite.sprite==definition.climbFrames[0],"Apex overwrites climbing");
                UnityEngine.Object.DestroyImmediate(root);
            }
            var camera=Camera.main;camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=5.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.8f,.88f,.85f);
            var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("AnimalArtSources-20261018/jump-apex-comparison.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("JUMP_APEX_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
