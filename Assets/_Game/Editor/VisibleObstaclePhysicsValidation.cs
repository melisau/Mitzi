#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Levels;
public static class VisibleObstaclePhysicsValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        var previous=Physics2D.simulationMode;
        try
        {
            GroundedCourseVisualValidation.Run();
            Physics2D.simulationMode=SimulationMode2D.Script;
            var catalog=GameFlow.Instance.Catalog;
            var course=GameFlow.Instance.GetComponentInChildren<LevelCourseBuilder>(true);
            var level=GameFlow.Instance.GetComponent<LevelManager>();
            int testedTrees=0;
            for(int theme=0;theme<4;theme++)
            {
                PlayerPrefs.SetInt("PawPath.SelectedTheme",theme);Call(level,"ApplyLevelTheme",171);
                for(int number=171;number<178;number+=2)
                {
                    course.Build(number);
                    var passages=course.GetComponentsInChildren<CrouchPassage>();
                    foreach(var tree in course.GetComponentsInChildren<ClimbableTree>())
                    foreach(var passage in passages)
                    {
                        float x=tree.transform.position.x;
                        float distance=x<passage.Left?passage.Left-x:x-passage.Right;
                        Check(distance>=CrouchPassage.TreeSafetyDistance-.01f,"Tree can launch into low passage");testedTrees++;
                    }
                }
            }
            Check(testedTrees>0,"Tree separation test exercised no trees");
            // Conservative ice-speed jump from the highest allowed perch.
            float gravity=9.81f*2.4f, first=7.344f, second=8.784f;
            float maximumHeight=5.2f+first*first/(2*gravity)+second*second/(2*gravity);
            float maximumFlightTime=(first+second)/gravity+Mathf.Sqrt(2*maximumHeight/gravity);
            Check(maximumFlightTime*(2.55f*1.65f)+1.5f<CrouchPassage.TreeSafetyDistance,"Tree safety gap too short even at ice speed");
            var ground=new GameObject("PhysicalPassageGround");ground.transform.position=new Vector3(10000,-.5f);ground.AddComponent<BoxCollider2D>().size=new Vector2(30,1);
            var root=new GameObject("PhysicalPassageCat");var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);visual.AddComponent<SpriteRenderer>();visual.AddComponent<Animator>();
            var cat=root.AddComponent<CatController>();typeof(CatController).GetField("visual",Private).SetValue(cat,visual.transform);Call(cat,"Awake");cat.ApplyCat(catalog.StarterCat);
            var body=root.GetComponent<Rigidbody2D>();
            var material=new PhysicsMaterial2D("ValidationNoFriction"){friction=0,bounciness=0};root.GetComponent<CircleCollider2D>().sharedMaterial=material;root.transform.Find("StandingProfile").GetComponent<BoxCollider2D>().sharedMaterial=material;
            for(int theme=0;theme<4;theme++)
            {
                var holder=new GameObject("PhysicalPassage_"+theme);holder.transform.position=new Vector3(10000,0);
                var passage=holder.AddComponent<CrouchPassage>();passage.Configure(4.2f,0,CrouchPassage.PassageClearance);
                var beam=holder.AddComponent<BoxCollider2D>();beam.size=new Vector2(4.2f,.32f);beam.offset=new Vector2(0,passage.Clearance+.16f);
                var upper=new GameObject("VisibleUpperObstruction");upper.transform.SetParent(holder.transform,false);var sr=upper.AddComponent<SpriteRenderer>();sr.sprite=catalog.CrouchUpperForTheme(theme);
                Check(sr.sprite!=null && sr.sprite.GetPhysicsShapeCount()>0,"Missing upper sprite physics outline "+theme);
                CrouchPassage.FitUpperArtwork(sr,4.2f,passage.Clearance);
                var polygon=upper.AddComponent<PolygonCollider2D>();var points=new List<Vector2>();polygon.pathCount=sr.sprite.GetPhysicsShapeCount();
                for(int i=0;i<polygon.pathCount;i++){sr.sprite.GetPhysicsShape(i,points);polygon.SetPath(i,points);}
                Physics2D.SyncTransforms();
                Check(polygon.bounds.max.y>4f,"Upper object within double jump height");
                foreach(int dir in new[]{1,-1})
                {
                    float start=10000-dir*4.2f;
                    Call(cat,"SetCrouching",false,true);root.transform.position=new Vector3(start,.34f);body.velocity=Vector2.zero;Physics2D.SyncTransforms();
                    for(int frame=0;frame<140;frame++){body.velocity=new Vector2(dir*3f,body.velocity.y);Physics2D.Simulate(.02f);}
                    Check(dir*(body.position.x-10000)<-2f,"Standing cat physically passed beam theme "+theme+" dir "+dir);
                    Call(cat,"SetCrouching",true,true);root.transform.position=new Vector3(start,.35f);body.velocity=Vector2.zero;Physics2D.SyncTransforms();
                    for(int frame=0;frame<160;frame++){body.velocity=new Vector2(dir*3f,body.velocity.y);Physics2D.Simulate(.02f);}
                    Check(dir*(body.position.x-10000)>3f,"Crouching cat blocked theme "+theme+" dir "+dir);
                    Call(cat,"SetCrouching",false,true);root.transform.position=new Vector3(start,.34f);body.velocity=Vector2.zero;Physics2D.SyncTransforms();
                    for(int frame=0;frame<150;frame++)
                    {
                        if(frame==8)body.velocity=new Vector2(body.velocity.x,7.344f);
                        if(frame==24)body.velocity=new Vector2(body.velocity.x,8.784f);
                        body.velocity=new Vector2(dir*4.5f,body.velocity.y);Physics2D.Simulate(.02f);
                    }
                    Check(dir*(body.position.x-10000)<1.5f,"Double jump physically bypassed upper object theme "+theme+" dir "+dir);
                }
                UnityEngine.Object.DestroyImmediate(holder);
            }
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(ground);UnityEngine.Object.DestroyImmediate(material);
            Debug.Log("VISIBLE_OBSTACLE_PHYSICS_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        finally{Physics2D.simulationMode=previous;}
    }
}
#endif
