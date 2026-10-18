#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Levels;
public static class GroundedCourseVisualValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,Private).Invoke(obj,args);
    public static void Run()
    {
        try
        {
            GroundedObstacleValidation.Run();
            var flow=GameFlow.Instance;var catalog=flow.Catalog;var level=flow.GetComponent<LevelManager>();
            var course=flow.GetComponentInChildren<LevelCourseBuilder>(true);course.gameObject.SetActive(true);
            var root=new GameObject("CoursePreviewCat");var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
            var sprite=visual.AddComponent<SpriteRenderer>();sprite.sortingOrder=10;visual.AddComponent<Animator>();
            var cat=root.AddComponent<CatController>();typeof(CatController).GetField("visual",Private).SetValue(cat,visual.transform);Call(cat,"Awake");cat.ApplyCat(catalog.StarterCat);
            var cam=Camera.main;cam.orthographicSize=3;cam.aspect=16f/9f;
            var backdrop=new GameObject("CourseBackdrop").AddComponent<SpriteRenderer>();backdrop.sortingOrder=-5;
            for(int theme=0;theme<4;theme++)
            {
                PlayerPrefs.SetInt("PawPath.SelectedTheme",theme);Call(level,"ApplyLevelTheme",170);course.Build(170);Physics2D.SyncTransforms();
                var passage=course.GetComponentsInChildren<CrouchPassage>().FirstOrDefault();if(passage==null)throw new Exception("Course missing passage in theme "+theme);
                cam.transform.position=new Vector3(passage.transform.position.x,passage.FloorY+1.2f,-10);
                backdrop.sprite=theme==1?catalog.forestBackground:theme==2?catalog.cityBackground:theme==3?catalog.cyberBackground:catalog.gameplayBackground;
                backdrop.transform.localScale=Vector3.one*(6/backdrop.sprite.bounds.size.y);backdrop.transform.position=new Vector3(cam.transform.position.x,cam.transform.position.y,1);
                sprite.sprite=catalog.StarterCat.crouchWalkFrames[1];root.transform.position=new Vector3(passage.transform.position.x,passage.FloorY+.336f);Physics2D.SyncTransforms();Call(cat,"AlignVisualFeet");
                Render(cam,"course-passage-"+theme+".png");
                if(theme==0)
                {
                    var hill=course.GetComponentsInChildren<PolygonCollider2D>().FirstOrDefault(c=>c.name=="Hump");
                    if(hill!=null)
                    {
                        float x=hill.bounds.center.x-hill.bounds.size.x*.23f;
                        var hit=Physics2D.RaycastAll(new Vector2(x,hill.bounds.max.y+3),Vector2.down,10).First(h=>h.collider==hill);
                        root.transform.position=new Vector3(x,hit.point.y+.40f);sprite.sprite=catalog.StarterCat.walkFrames[4];Physics2D.SyncTransforms();root.GetComponent<Rigidbody2D>().velocity=new Vector2(2,1.2f);
                        for(int i=0;i<120;i++)Call(cat,"AlignVisualFeet");
                        cam.transform.position=new Vector3(hill.bounds.center.x,hill.bounds.center.y+1,-10);backdrop.transform.position=new Vector3(cam.transform.position.x,cam.transform.position.y,1);
                        Render(cam,"course-hill-contact.png");
                    }
                }
            }
            var marker=(Transform)typeof(LevelManager).GetField("goalMarker",Private).GetValue(level);
            marker.position=new Vector3(course.GoalX,course.GoalY);
            Call(level,"PlaceWaitingRescueCat",20);
            cam.transform.position=new Vector3(course.GoalX-1,course.CatSpawnY+.7f,-10);backdrop.transform.position=new Vector3(cam.transform.position.x,cam.transform.position.y,1);
            root.transform.position=new Vector3(course.GoalX-3.2f,course.CatSpawnY);sprite.sprite=catalog.StarterCat.walkFrames[4];Physics2D.SyncTransforms();Call(cat,"AlignVisualFeet");
            Render(cam,"waiting-rescue-cat.png");
            Debug.Log("GROUNDED_COURSE_VISUAL_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    static void Render(Camera cam,string name)
    {
        var target=new RenderTexture(1280,720,24);cam.targetTexture=target;cam.Render();RenderTexture.active=target;
        var im=new Texture2D(1280,720,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1280,720),0,0);im.Apply();File.WriteAllBytes("AnimalArtSources-20261018/"+name,im.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(target);
    }
}
#endif
