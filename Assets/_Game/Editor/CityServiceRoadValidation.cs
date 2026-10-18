#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PawPath.Core;
using PawPath.Data;
using PawPath.Cat;
using PawPath.Levels;
public static class CityServiceRoadValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated validation project");
            CityThemeBuilder.Apply();HomeSettingsValidation.Run();
            var catalog=GameFlow.Instance.Catalog;
            Check(catalog.cityPlatformSprite.name=="city_service_road" && catalog.cityGapSprite.name=="city_service_shaft","New city sprites not bound");
            foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())canvas.gameObject.SetActive(false);
            foreach(var renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())renderer.gameObject.SetActive(false);
            var course=new GameObject("CityServiceValidation").AddComponent<LevelCourseBuilder>();
            course.BindVisuals(catalog.cityGapSprite,catalog.cityCarSprite,catalog.cityPlatformSprite,catalog.cityVanSprite,catalog.cityRoadSprite);
            SpriteRenderer selected=null;
            foreach(int number in new[]{1,2,5,17,170,175})
            {
                course.Build(number);Physics2D.SyncTransforms();
                Check(course.ValidateCourseGeometry()==null,"Physical course geometry changed: "+course.ValidateCourseGeometry());
                var sprites=course.GetComponentsInChildren<SpriteRenderer>();
                Check(!sprites.Any(s=>s.name=="TrashContainer" || s.name=="ContinuousCityRoad" || s.name=="RoadUnderfill"),"Old bin or soil covers the gap");
                var roads=course.GetComponentsInChildren<BoxCollider2D>().Where(c=>c.name=="Road").ToArray();
                var visuals=sprites.Where(s=>s.name=="CityServiceRoad").ToArray();
                Check(visuals.Length==roads.Length,"Road visuals do not match physical spans");
                foreach(var visual in visuals)
                {
                    Check(roads.Any(r=>Mathf.Abs(r.bounds.min.x-visual.bounds.min.x)<.003f && Mathf.Abs(r.bounds.max.x-visual.bounds.max.x)<.003f && Mathf.Abs(r.bounds.max.y-visual.bounds.max.y)<.003f),"Road ends or top are misaligned");
                }
                var shafts=sprites.Where(s=>s.name=="CityServiceShaft").ToArray();Check(shafts.Length>0,"No open service shafts");
                foreach(var shaft in shafts)
                {
                    var bounds=shaft.bounds;
                    Check(shaft.GetComponent<Collider2D>()==null,"Decorative shaft blocks falling");
                    Check(Mathf.Abs(bounds.min.y+5.35f)<.003f,"Shaft bottom does not meet foundation");
                    Check(roads.Any(r=>Mathf.Abs(r.bounds.max.x-bounds.min.x)<.003f) && roads.Any(r=>Mathf.Abs(r.bounds.min.x-bounds.max.x)<.003f),"Shaft edges do not meet road cuts");
                    Check(!roads.Any(r=>r.bounds.min.x<bounds.max.x-.003f && r.bounds.max.x>bounds.min.x+.003f),"Road bridges service shaft");
                    var hits=Physics2D.RaycastAll(new Vector2(bounds.center.x,bounds.max.y+.05f),Vector2.down,1);
                    Check(!hits.Any(h=>h.collider.name=="Road"),"Invisible walking surface across shaft");
                }
                selected=shafts[shafts.Length/2];
            }
            var camera=Camera.main;camera.orthographicSize=3;camera.aspect=16f/9f;camera.transform.position=new Vector3(selected.bounds.center.x,selected.bounds.max.y+.8f,-10);
            var back=new GameObject("CityPreviewBackdrop").AddComponent<SpriteRenderer>();back.sprite=catalog.cityBackground;back.sortingOrder=-5;back.transform.localScale=Vector3.one*(6/back.sprite.bounds.size.y);back.transform.position=new Vector3(camera.transform.position.x,camera.transform.position.y,1);
            var root=new GameObject("CityPreviewCat");var visualCat=new GameObject("Visual");visualCat.transform.SetParent(root.transform,false);var catSprite=visualCat.AddComponent<SpriteRenderer>();catSprite.sortingOrder=10;visualCat.AddComponent<Animator>();
            var cat=root.AddComponent<CatController>();const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(CatController).GetField("visual",flags).SetValue(cat,visualCat.transform);typeof(CatController).GetMethod("Awake",flags).Invoke(cat,null);cat.ApplyCat(catalog.StarterCat);
            root.transform.position=new Vector3(selected.bounds.min.x-1.8f,selected.bounds.max.y+.336f);Physics2D.SyncTransforms();typeof(CatController).GetMethod("AlignVisualFeet",flags).Invoke(cat,null);
            var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes("AnimalArtSources-20261018/city-service-road-preview.png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("CITY_SERVICE_ROAD_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
