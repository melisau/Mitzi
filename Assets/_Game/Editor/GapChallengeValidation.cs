#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Levels;

public static class GapChallengeValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,Hidden).Invoke(obj,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        var previous=Physics2D.simulationMode;
        bool large=DisplaySizeSettings.Large;
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated validation project");
            HomeSettingsValidation.Run();
            var flow=GameFlow.Instance;var level=flow.GetComponent<LevelManager>();
            var course=flow.GetComponentInChildren<LevelCourseBuilder>(true);course.gameObject.SetActive(true);
            Call(level,"Awake");Physics2D.simulationMode=SimulationMode2D.Script;
            int layouts=0,trials=0;
            foreach(var difficulty in new[]{DifficultyLevel.Easy,DifficultyLevel.Medium,DifficultyLevel.Hard})
            {
                GameDifficulty.Select(difficulty);
                for(int theme=0;theme<4;theme++)
                {
                    PlayerPrefs.SetInt("PawPath.SelectedTheme",theme);Call(level,"ApplyLevelTheme",1);
                    foreach(int number in new[]{1,2,3,4,5,6,10,11,20,21,50,100})
                    {
                        course.Build(number);Physics2D.SyncTransforms();
                        Check(course.ValidateCourseGeometry()==null,"Invalid course geometry: "+theme+"/"+number+"/"+difficulty+": "+course.ValidateCourseGeometry());
                        var openings=(IList)typeof(LevelCourseBuilder).GetField("gaps",Hidden).GetValue(course);
                        foreach(object gap in openings)
                        {
                            var type=gap.GetType();float left=(float)type.GetField("Left").GetValue(gap),right=(float)type.GetField("Right").GetValue(gap);
                            Check(right-left>=1.74f,"Narrow gap remains: "+theme+"/"+number);
                            if(difficulty!=DifficultyLevel.Easy || (number!=1&&number!=6&&number!=10))
                            {
                                if(difficulty!=DifficultyLevel.Easy&&number==21)
                                {Check(Traverse(flow,course,left,right,2.55f,3),"Advanced gap cannot be crossed by double jump");trials++;}
                                continue;
                            }
                            // Exercise every easy gap with the actual cat colliders, not only its sprite width.
                            foreach(bool enlarged in new[]{false,true})
                            {
                                DisplaySizeSettings.SetLarge(enlarged);
                                foreach(float speed in new[]{2.55f,2.55f*1.65f})
                                {
                                    Check(!Traverse(flow,course,left,right,speed,0),"Cat walked across an unbridged gap: "+theme+"/"+number+" width="+(right-left)+" speed="+speed+" large="+enlarged);
                                    trials++;
                                }
                            }
                            DisplaySizeSettings.SetLarge(true);
                            Check(Traverse(flow,course,left,right,2.55f,1),"Easy gap cannot be crossed by one well-timed jump: width="+(right-left));
                            Check(Traverse(flow,course,left,right,2.55f,2),"Drawn bridge does not cross easy gap");
                            trials+=2;
                        }
                        layouts++;
                    }
                }
            }
            Debug.Log("GAP_CHALLENGE_VALIDATION_PASS layouts="+layouts+" physicsTrials="+trials);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        finally{Physics2D.simulationMode=previous;DisplaySizeSettings.SetLarge(large);GameDifficulty.SelectAutomatic();}
    }

    // intervention: 0 = walk, 1 = first jump, 2 = bridge, 3 = double jump.
    static bool Traverse(GameFlow flow,LevelCourseBuilder course,float left,float right,float speed,int intervention)
    {
        foreach(var other in UnityEngine.Object.FindObjectsOfType<Rigidbody2D>())other.simulated=false;
        var root=new GameObject("GapPhysicsCat");var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
        visual.AddComponent<SpriteRenderer>();visual.AddComponent<Animator>();
        var cat=root.AddComponent<CatController>();typeof(CatController).GetField("visual",Hidden).SetValue(cat,visual.transform);Call(cat,"Awake");cat.ApplyCat(flow.Catalog.StarterCat);
        var body=root.GetComponent<Rigidbody2D>();body.simulated=true;
        var material=new PhysicsMaterial2D("GapNoFriction"){friction=0,bounciness=0};
        foreach(var collider in root.GetComponentsInChildren<Collider2D>())collider.sharedMaterial=material;
        float top=course.CatSpawnY-.40f*LevelCourseBuilder.GameplayScale;
        cat.PlaceAtSpawn(new Vector2(left-.8f,top+.34f));
        GameObject bridge=null;
        if(intervention==2)
        {
            bridge=new GameObject("DrawnGapBridge");var edge=bridge.AddComponent<EdgeCollider2D>();edge.points=new[]{new Vector2(left-.5f,top+.02f),new Vector2(right+.5f,top+.02f)};edge.sharedMaterial=material;
        }
        Physics2D.SyncTransforms();bool jumped=false,secondJump=false,crossed=false,fell=false;
        try
        {
            for(int step=0;step<400;step++)
            {
                body.velocity=new Vector2(speed,body.velocity.y);
                if((intervention==1||intervention==3)&&!jumped&&body.position.x>=left+.08f)
                {typeof(CatController).GetField("keyboardJumpQueued",Hidden).SetValue(cat,true);Call(cat,"TryJump");jumped=true;}
                else if(intervention==3&&jumped&&!secondJump&&body.velocity.y<=.05f)
                {typeof(CatController).GetField("keyboardJumpQueued",Hidden).SetValue(cat,true);Call(cat,"TryJump");secondJump=true;}
                Physics2D.Simulate(.01f);
                if(body.position.y<top-1.2f){fell=true;break;}
                if(body.position.x>right+.3f&&body.position.y>=top+.20f){crossed=true;break;}
            }
            if(intervention==0)Check(fell,"Unassisted cat stalled above gap instead of falling");
        }
        finally
        {
            if(intervention>0&&!crossed)Debug.Log("Gap traversal failed: width="+(right-left)+" intervention="+intervention+" endX="+(body.position.x-left)+" endY="+(body.position.y-top)+" jumped="+jumped);
            UnityEngine.Object.DestroyImmediate(root);if(bridge!=null)UnityEngine.Object.DestroyImmediate(bridge);UnityEngine.Object.DestroyImmediate(material);
        }
        return crossed;
    }
}
#endif
