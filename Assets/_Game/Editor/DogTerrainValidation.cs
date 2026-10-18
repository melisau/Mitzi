#if UNITY_EDITOR
using System;
using System.Reflection;
using PawPath.Data;
using PawPath.Levels;
using UnityEditor;
using UnityEngine;
public static class DogTerrainValidation
{
    public static void Run()
    {
        try
        {
            if(Application.productName!="MitziValidation") throw new Exception("Use isolated project");
            AssetCleanupValidation.Run();
            var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var floor=new GameObject("DogGroundTest"); floor.transform.position=new Vector3(-1000,-.5f);
            floor.AddComponent<BoxCollider2D>().size=new Vector2(20,1);
            var mound=new GameObject("DogHillTest"); mound.transform.position=new Vector3(-1000,0);
            mound.AddComponent<PolygonCollider2D>().points=new[]{new Vector2(-3,0),new Vector2(-1,2),new Vector2(1,2),new Vector2(3,0),new Vector2(3,-1),new Vector2(-3,-1)};
            for(int coat=0;coat<2;coat++)
            {
                var root=new GameObject("TerrainDogTest"); root.transform.position=new Vector3(-1004,0);
                root.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
                var trigger=root.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(1.98f,1.05f); trigger.offset=new Vector2(0,.525f);
                var visual=new GameObject("Visual"); visual.transform.SetParent(root.transform,false);
                var renderer=visual.AddComponent<SpriteRenderer>(); renderer.sprite=coat==0 ? catalog.dogIdleSprite:catalog.roboticDogIdleSprite;
                var dog=root.AddComponent<DogObstacle>();
                dog.Configure(renderer,coat==0 ? catalog.dogRunFrames:catalog.roboticDogWalkFrames,2,-1010,-990,0,2.75f,.22f,1f/16f,new Vector2[0],1,renderer.sprite);
                var align=typeof(DogObstacle).GetMethod("AlignFeetToGround",BindingFlags.Instance|BindingFlags.NonPublic);
                var direction=typeof(DogObstacle).GetField("direction",BindingFlags.Instance|BindingFlags.NonPublic);
                foreach(int facing in new[]{-1,1})
                for(int step=0;step<=32;step++)
                {
                    float x=-4+step*.25f; root.transform.position=new Vector3(-1000+x,0);
                    direction.SetValue(dog,facing); Physics2D.SyncTransforms(); align.Invoke(dog,null); Physics2D.SyncTransforms();
                    float expected=Mathf.Abs(x)<=1 ? 2:Mathf.Abs(x)<3 ? 3-Mathf.Abs(x):0;
                    if(Mathf.Abs(root.transform.position.y-expected)>.02f || Mathf.Abs(trigger.bounds.min.y-expected)>.02f)
                        throw new Exception("Dog or collision trigger went through hill at "+x);
                    Vector3 paw=visual.transform.TransformPoint(new Vector3(0,renderer.sprite.bounds.min.y+.22f/(2.75f/renderer.sprite.bounds.size.x),0));
                    if(Mathf.Abs(paw.y-expected)>.03f) throw new Exception("Dog paws left terrain");
                }
                var jump=typeof(DogObstacle).GetField("jumpingGap",BindingFlags.Instance|BindingFlags.NonPublic);
                root.transform.position=new Vector3(-1000,4); jump.SetValue(dog,true); align.Invoke(dog,null);
                if(Mathf.Abs(root.transform.position.y-4)>.001f) throw new Exception("Gap jump snapped to ground");
                UnityEngine.Object.DestroyImmediate(root);
            }
            UnityEngine.Object.DestroyImmediate(floor); UnityEngine.Object.DestroyImmediate(mound);
            Debug.Log("DOG_TERRAIN_VALIDATION_PASS");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
