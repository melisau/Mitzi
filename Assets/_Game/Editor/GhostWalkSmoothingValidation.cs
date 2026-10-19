#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using PawPath.Cat;
using PawPath.Core;
using UnityEditor;
using UnityEngine;

public static class GhostWalkSmoothingValidation
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated project");HomeSettingsValidation.Run();
            var flow=GameFlow.Instance;var ghost=flow.Catalog.GetCat("ghost");var boza=flow.Catalog.GetCat("boza");
            var root=new GameObject("GhostSmoothTest");root.layer=30;
            var source=root.AddComponent<SpriteRenderer>();source.sprite=ghost.walkFrames[0];
            var animator=root.AddComponent<Animator>();animator.runtimeAnimatorController=ghost.animator;animator.Rebind();animator.Play("Walk",0,0);animator.Update(0);
            GhostWalkSmoother.Bind(source,ghost);var smoother=root.GetComponent<GhostWalkSmoother>();smoother.Refresh(0);
            Check(smoother.IsRendering&&source.enabled&&source.forceRenderingOff,"Ghost blend renderer did not take over");
            var position=source.transform.position;var scale=source.transform.localScale;
            var camera=new GameObject("GhostSmoothCamera").AddComponent<Camera>();camera.cullingMask=1<<30;camera.orthographic=true;camera.orthographicSize=2.2f;camera.aspect=16f/9f;
            camera.transform.position=new Vector3(source.bounds.center.x,1.7f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            source.sprite=ghost.walkFrames[1];smoother.Refresh(0);Check(smoother.BlendWeight==0,"Frame change starts as a hard cut");
            var before=Render(camera);float interval=ghost.walkCycleSeconds/ghost.walkFrames.Length;
            smoother.Refresh(interval*.475f);Check(Mathf.Abs(smoother.BlendWeight-.5f)<.001f&&smoother.UsesMotion,"Midpoint has no matched paw motion");var middle=Render(camera);
            smoother.Refresh(interval*.525f);Check(smoother.BlendWeight==1,"Frame never finishes transition");var after=Render(camera);
            var a=before.GetPixels32();var m=middle.GetPixels32();var b=after.GetPixels32();int opaque=0,different=0;
            for(int i=0;i<a.Length;i++)
            {
                float worldY=camera.transform.position.y+((i/426+.5f)/240f*2-1)*camera.orthographicSize;
                // Paws change occlusion during a stride; check opacity in the stationary upper body.
                if(worldY>source.bounds.max.y*.6f&&a[i].a>250&&b[i].a>250){opaque++;Check(m[i].a>248,"Blending makes torso transparent");}
                if(m[i].a!=a[i].a&&m[i].a!=b[i].a)different++;
            }
            Check(opaque>500&&different>100,"No visible intermediate motion rendered");
            Check(source.transform.position==position&&source.transform.localScale==scale,"Smoothing changes physical alignment or body size");
            var cycle=new Texture2D(1278,240*8,TextureFormat.RGBA32,false);
            for(int frame=0;frame<8;frame++)
            {
                source.enabled=false;smoother.Refresh(0);source.enabled=true;
                source.sprite=ghost.walkFrames[frame];smoother.Refresh(0);
                source.sprite=ghost.walkFrames[(frame+1)%8];smoother.Refresh(0);
                for(int sample=0;sample<3;sample++)
                {
                    if(sample>0)smoother.Refresh(interval*.475f);
                    Check(smoother.UsesMotion,"Missing stride motion at frame "+frame);
                    var pose=Render(camera);cycle.SetPixels(sample*426,(7-frame)*240,426,240,pose.GetPixels());UnityEngine.Object.DestroyImmediate(pose);
                }
            }
            cycle.Apply();Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/ghost-walk-cycle.png",cycle.EncodeToPNG());UnityEngine.Object.DestroyImmediate(cycle);
            source.sprite=ghost.walkFrames[7];smoother.Refresh(interval);source.sprite=ghost.walkFrames[0];smoother.Refresh(0);Check(smoother.BlendWeight==0,"Loop seam hard-cuts");
            source.enabled=false;smoother.Refresh(0);Check(!smoother.IsRendering&&!source.forceRenderingOff&&!source.enabled,"Front/rear view cannot hide side cat");source.enabled=true;
            source.sprite=ghost.jumpFrames[1];smoother.Refresh(0);Check(!smoother.IsRendering,"Jump is replaced by walk overlay");
            source.sprite=ghost.walkFrames[0];smoother.Refresh(0);flow.BeginRescueMeeting();source.enabled=false;smoother.Refresh(0);Check(!smoother.IsRendering&&!source.enabled&&!source.forceRenderingOff,"Walk overlay leaks into hug");flow.EnterHub();source.enabled=true;
            GhostWalkSmoother.Bind(source,boza);source.sprite=boza.walkFrames[0];smoother.Refresh(interval);Check(!smoother.IsRendering&&!source.forceRenderingOff,"Boza gait changed");
            var shader=Resources.Load<Shader>("Shaders/GhostWalkBlend");Check(shader!=null&&shader.isSupported&&!ShaderUtil.GetShaderMessages(shader).Any(msg=>msg.severity.ToString()=="Error"),"Walk shader failed compilation");
            var sheet=new Texture2D(1280,240,TextureFormat.RGBA32,false);sheet.SetPixels(0,0,426,240,before.GetPixels());sheet.SetPixels(426,0,426,240,middle.GetPixels());sheet.SetPixels(852,0,426,240,after.GetPixels());sheet.Apply();Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/ghost-walk-interpolation.png",sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(before);UnityEngine.Object.DestroyImmediate(middle);UnityEngine.Object.DestroyImmediate(after);UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(root);
            Debug.Log("GHOST_WALK_SMOOTHING_PASS intermediate_pixels="+different+" opaque_body_pixels="+opaque+" boza=unchanged loop=blended care=restored");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static Texture2D Render(Camera camera)
    {
        var target=new RenderTexture(426,240,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(426,240,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,426,240),0,0);image.Apply();RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(target);return image;
    }
}
#endif
