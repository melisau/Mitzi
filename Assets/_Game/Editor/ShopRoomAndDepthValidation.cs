#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Hub;
using PawPath.UI;
using PawPath.Economy;
public static class ShopRoomAndDepthValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Private).Invoke(o,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            HomeSettingsValidation.Run();
            var flow=GameFlow.Instance;var catalog=flow.Catalog;var economy=CozyEconomyManager.Instance;
            var shop=flow.GetComponentInChildren<ShopScreen>(true);shop.gameObject.SetActive(true);
            for(var parent=shop.transform.parent;parent!=null;parent=parent.parent)parent.gameObject.SetActive(true);
            SaveService.Data.activeHomeRoom=0;SaveService.Data.lovePoints=5000;
            Call(shop,"OnDisable");Call(shop,"OnEnable");
            var scroll=shop.GetComponentInChildren<ScrollRect>(true);Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=.28f;
            float before=scroll.verticalNormalizedPosition;
            SaveService.Data.activeHomeRoom=1;
            var item=catalog.GetItem("wall_courtyard");
            var icon=shop.GetComponentsInChildren<Image>().First(i=>i.name=="Icon" && i.sprite==item.placedSprite);
            icon.transform.parent.GetComponentsInChildren<Button>().First(b=>b.name=="BuyButton").onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            Check(Mathf.Abs(scroll.verticalNormalizedPosition-before)<.001f,"Buying jumps shop to the top");
            Check(economy.PlacedCount(item.id,0)==1 && economy.PlacedCount(item.id,1)==0,"Wallpaper applied to wrong opening room");
            GameEvents.LovePointsChanged(economy.ShoppingPoints);GameEvents.ShopChanged();Canvas.ForceUpdateCanvases();
            Check(Mathf.Abs(scroll.verticalNormalizedPosition-before)<.001f,"Repeated refresh loses scroll position");
            Call(shop,"OnDisable");Call(shop,"OnEnable");
            item=catalog.GetItem("wall_blue_room");icon=shop.GetComponentsInChildren<Image>().First(i=>i.name=="Icon" && i.sprite==item.placedSprite);
            icon.transform.parent.GetComponentsInChildren<Button>().First(b=>b.name=="BuyButton").onClick.Invoke();
            Check(economy.PlacedCount(item.id,1)==1 && economy.PlacedCount("wall_courtyard",0)==1,"Reopening shop does not target second room");
            var furniture=flow.GetComponentInChildren<HubFurnitureView>(true);furniture.Refresh();
            Check(HomeRooms.Instance.Background(0).sprite.texture==catalog.homeRoomOptions[1].texture && HomeRooms.Instance.Background(1).sprite.texture==catalog.homeRoomOptions[2].texture,"Wallpaper rendering ignores paid choices");
            SaveService.Persist();SaveService.Load();furniture.Refresh();
            Check(economy.PlacedCount("wall_courtyard",0)==1 && economy.PlacedCount("wall_blue_room",1)==1,"Per-room wallpaper lost after reload");
            foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())canvas.gameObject.SetActive(false);
            foreach(var renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())renderer.gameObject.SetActive(false);
            var back=new GameObject("DepthBack");var front=new GameObject("DepthFront");
            var a=new GameObject("Visual").AddComponent<SpriteRenderer>();a.transform.SetParent(back.transform,false);
            var b=new GameObject("Visual").AddComponent<SpriteRenderer>();b.transform.SetParent(front.transform,false);
            a.sprite=b.sprite=FallbackSprite.WhiteSquare();a.color=Color.red;b.color=Color.blue;
            var ca=back.AddComponent<CatHomeBehaviour>();var cb=front.AddComponent<CatHomeBehaviour>();Call(ca,"Awake");Call(cb,"Awake");ca.BindDefinition(catalog.StarterCat);cb.BindDefinition(catalog.GetCat("komur"));
            front.transform.position=new Vector3(0,0,-.1f);back.transform.position=Vector3.zero;
            for(int i=0;i<120;i++)
            {
                var p=back.transform.position;p.y=i%2==0?.001f:-.001f;back.transform.position=p;
                Call(ca,"UpdateDepthOrder");Call(cb,"UpdateDepthOrder");
                Check(a.sortingOrder==b.sortingOrder && b.spriteSortPoint==SpriteSortPoint.Pivot,"Cat depth changes with Y motion");
            }
            var cam=Camera.main;cam.transform.position=new Vector3(0,0,-10);cam.orthographicSize=2;cam.backgroundColor=Color.black;
            var target=new RenderTexture(64,64,24);cam.targetTexture=target;cam.Render();RenderTexture.active=target;
            var im=new Texture2D(64,64,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,64,64),0,0);im.Apply();var pixel=im.GetPixel(32,32);
            Check(pixel.b>.8f && pixel.r<.2f,"Front Z cat is not visually in front");
            cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(back);UnityEngine.Object.DestroyImmediate(front);
            Debug.Log("SHOP_ROOM_AND_DEPTH_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
