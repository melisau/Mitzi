#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using PawPath.Audio;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ClimbPurrValidation
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj,args);
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj,value);
    static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, Private).GetValue(obj);
    static void Check(bool ok, string message) { if(!ok) throw new Exception(message); }

    public static void Run()
    {
        try
        {
            Check(Application.productName == "MitziValidation", "Use the isolated validation project");
            GameAudioBuilder.Apply();
            AnimalAnimationValidation.BuildAndValidate();
            var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            Check(catalog.catPurrSfx != null && catalog.catPurrSfx.length > 1f,"Purr clip not bound");
            catalog.catPurrSfx.LoadAudioData();
            var samples=new float[catalog.catPurrSfx.samples*catalog.catPurrSfx.channels];
            Check(catalog.catPurrSfx.GetData(samples,0),"Cannot decode purr audio");
            double energy=0; foreach(float sample in samples) energy+=sample*sample;
            Check(Math.Sqrt(energy/samples.Length)>0.01,"Purr recording is silent");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var audio=new GameObject("AudioValidation").AddComponent<CozyAudioManager>();
            Call(audio,"Awake"); audio.BindLibrary(catalog);
            if(audio.EffectsMuted) audio.ToggleEffects();
            var pet=new GameObject("PetValidation").AddComponent<CatPettingSystem>();
            pet.Bind(catalog.StarterCat,null);
            Call(pet,"BeginPetting");
            var source=Get<AudioSource>(audio,"purr");
            Check(source.clip==catalog.catPurrSfx && source.loop && !source.mute && source.volume>0f,
                $"Petting purr: clip={source.clip?.name}, expected={catalog.catPurrSfx.name}, loop={source.loop}, mute={source.mute}, volume={source.volume}, default={Get<AudioClip>(audio,"defaultPurr")?.name}");
            Call(pet,"StopPetting");
            Check(!source.isPlaying,"Purr did not stop after petting");
            UnityEngine.Object.DestroyImmediate(pet.gameObject);

            var root=new GameObject("ClimbingCat");
            var visual=new GameObject("Visual"); visual.transform.SetParent(root.transform);
            var renderer=visual.AddComponent<SpriteRenderer>(); visual.AddComponent<Animator>();
            var cat=root.AddComponent<CatController>(); Set(cat,"visual",visual.transform); Call(cat,"Awake");
            cat.ApplyCat(catalog.StarterCat); cat.PlaceAtSpawn(new Vector2(-1f,0f));
            cat.BeginClimb(new Vector2(0f,4.8f),catalog.climbingCatFrames,2.8f);
            Check(Get<Vector2>(cat,"climbTargetPosition").y>4.6f,"Climbing still limited to old height");
            Set(cat,"climbStartTime",Time.time-0.43f); Call(cat,"UpdateClimbing");
            Sprite first=renderer.sprite;
            Set(cat,"climbStartTime",Time.time-0.60f); Call(cat,"UpdateClimbing");
            Check(renderer.sprite!=first,"Climbing paw cycle frozen");
            float firstY=root.transform.position.y;
            Set(cat,"climbStartTime",Time.time-1.6f); Call(cat,"UpdateClimbing");
            Check(root.transform.position.y>firstY+1f,"Cat did not ascend");
            Set(cat,"climbStartTime",Time.time-2.6f); Call(cat,"UpdateClimbing");
            Check(renderer.sprite==catalog.StarterCat.idleSprite,"Top did not switch to perch pose");
            Set(cat,"climbStartTime",Time.time-2.7f); Call(cat,"UpdateClimbing");
            Check(renderer.sprite==catalog.StarterCat.idleSprite,"Climb cycle restarted on top");

            var camera=new GameObject("ClimbPreviewCamera").AddComponent<Camera>();
            camera.orthographic=true; camera.orthographicSize=4.45f;
            camera.transform.position=new Vector3(0f,0f,-10f);
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.68f,.76f,.73f);
            var follow=camera.gameObject.AddComponent<SideScrollCamera>(); Call(follow,"Awake"); follow.Bind(root.transform);
            Check((float)Call(follow,"ClimbingViewY",false)>0.5f,"Camera does not frame higher climb");
            var tree=new GameObject("Tree").AddComponent<SpriteRenderer>(); tree.sprite=catalog.climbTreeSprite;
            if(tree.sprite!=null) { float scale=6.1f/tree.sprite.bounds.size.y; tree.transform.localScale=Vector3.one*scale; tree.transform.position=new Vector3(0f,-tree.sprite.bounds.min.y*scale-1.1f,1f); }
            Directory.CreateDirectory("AnimalArtSources-20261018/climb-preview");
            cat.PlaceAtSpawn(new Vector2(-.65f,0f)); cat.BeginClimb(new Vector2(0f,4.8f),catalog.climbingCatFrames,2.8f);
            for(int i=0;i<72;i++)
            {
                Set(cat,"climbStartTime",Time.time-i/24f); Set(cat,"climbUntil",Time.time+4f);
                Call(cat,"UpdateClimbing");
                Call(cat,"AlignVisualFeet");
                var p=camera.transform.position; p.y=(float)Call(follow,"ClimbingViewY",false); camera.transform.position=p;
                var target=new RenderTexture(640,640,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
                var image=new Texture2D(640,640,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,640,640),0,0); image.Apply();
                File.WriteAllBytes("AnimalArtSources-20261018/climb-preview/"+i.ToString("000")+".png",image.EncodeToPNG());
                RenderTexture.active=null; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            }
            File.WriteAllText("AnimalArtSources-20261018/climb-purr-validation.txt","PASS: twelve upright poses, looping paw strokes, higher ascent, stable top pose, camera framing, non-silent purr, petting fallback binding and stop.");
            Debug.Log("CLIMB_PURR_VALIDATION_PASS");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
