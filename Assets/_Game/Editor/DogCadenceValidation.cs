#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PawPath.Data;
using PawPath.Levels;
public static class DogCadenceValidation
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated validation project");
            HomeRoomSwipeValidation.Run();DogTerrainValidation.Run();
            var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            for(int coat=0;coat<2;coat++)
            {
                var root=new GameObject("CadenceDog");root.transform.position=new Vector3(-2000,0);
                var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
                var renderer=visual.AddComponent<SpriteRenderer>();var dog=root.AddComponent<DogObstacle>();
                int previous=0;
                foreach(float speed in new[]{1.13f,2f,3f})
                {
                    var idle=coat==0?catalog.dogIdleSprite:catalog.roboticDogIdleSprite;
                    dog.Configure(renderer,coat==0?catalog.dogRunFrames:catalog.roboticDogWalkFrames,
                        speed,-2010,-1990,0,coat==0?2.75f:1.95f,.15f,1f/16f,null,1,idle);
                    typeof(DogObstacle).GetField("frameTimer",Hidden).SetValue(dog,0f);
                    int steps=0;
                    for(int i=0;i<120;i++)steps+=(int)typeof(DogObstacle).GetMethod("AdvanceRunAnimation",Hidden).Invoke(dog,new object[]{speed/60f});
                    Check(steps>=previous&&steps<=24&&(speed>1.5f||steps<=18),"Cadence does not follow travel speed or remains frantic: "+coat+"/"+speed+" steps="+steps);
                    previous=steps;
                    int held=(int)typeof(DogObstacle).GetField("frameIndex",Hidden).GetValue(dog);
                    for(int i=0;i<120;i++)typeof(DogObstacle).GetMethod("AdvanceRunAnimation",Hidden).Invoke(dog,new object[]{0f});
                    Check((int)typeof(DogObstacle).GetField("frameIndex",Hidden).GetValue(dog)==held,"Dog runs in place without movement");
                    typeof(DogObstacle).GetMethod("ShowIdle",Hidden).Invoke(dog,null);Check(renderer.sprite==idle,"Standing dog uses running pose");
                    Debug.Log("DOG_CADENCE coat="+coat+" speed="+speed+" framesInTwoSeconds="+steps);
                }
                UnityEngine.Object.DestroyImmediate(root);
            }
            Debug.Log("DOG_CADENCE_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
