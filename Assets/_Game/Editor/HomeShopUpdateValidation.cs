#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Hub;
using PawPath.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class HomeShopUpdateValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,Private).Invoke(obj,args);
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            ReleaseEconomyValidation.Run();
            var flow=GameFlow.Instance; flow.EnterHub();
            var rooms=flow.GetComponentInChildren<HomeRooms>(true);
            typeof(HomeRooms).GetProperty("Instance").SetValue(null,rooms);
            UnityEngine.Random.InitState(321);
            var cats=new CatHomeBehaviour[6];
            for(int i=0;i<cats.Length;i++)
            {
                var root=new GameObject("RoamingTest"+i);root.transform.position=new Vector3(i-3,-2.5f);
                var visual=new GameObject("Visual");visual.transform.SetParent(root.transform,false);
                visual.AddComponent<SpriteRenderer>().sprite=flow.Catalog.cats[i].idleSprite;
                cats[i]=root.AddComponent<CatHomeBehaviour>();cats[i].BindDefinition(flow.Catalog.cats[i]);Call(cats[i],"Awake");
                typeof(CatHomeBehaviour).GetField("nextRoomVisit",Private).SetValue(cats[i],-1f);
            }
            int crossings=0,choices=0;
            var final=typeof(CatHomeBehaviour).GetField("finalPosition",Private);
            for(int cycle=0;cycle<40;cycle++) foreach(var cat in cats)
            {
                Call(cat,"BeginWalk");choices++;
                if(((Vector2)final.GetValue(cat)).x>rooms.RoomWidth*.5f)crossings++;
                Check(cats.Count(c=>c.CurrentActivity==ResidentActivity.Walking && ((Vector2)final.GetValue(c)).x>rooms.RoomWidth*.5f)<=1,"Residents migrate together");
            }
            Check(crossings<choices*.25f,"Residents choose distant room too often");
            cats[0].WalkDirectTo(new Vector2(rooms.Offset(1),-2.5f));
            Check(((Vector2)final.GetValue(cats[0])).x>rooms.RoomWidth*.5f,"Explicit cross-room command blocked");
            var tree=new GameObject("ClimbTest");tree.AddComponent<SpriteRenderer>().sprite=flow.Catalog.GetItem("cat_tree").placedSprite;
            var interaction=tree.AddComponent<FurnitureInteraction>();
            var cat0=cats[0];var start=cat0.transform.position;
            var sequence=(System.Collections.IEnumerator)Call(interaction,"PlaySmallHomeClimb",cat0,cat0.Definition.climbFrames);
            Check(sequence.MoveNext(),"Tree climb did not start");
            Check((bool)typeof(CatHomeBehaviour).GetField("externalAnimation",Private).GetValue(cat0),"Roaming overrides climb animation");
            var elapsed=sequence.GetType().GetFields(Private|BindingFlags.Public).Single(f=>f.Name.Contains("elapsed"));
            elapsed.SetValue(sequence,1.14f);Check(sequence.MoveNext(),"Climb ended too early");
            Check(cat0.transform.position.y>start.y+.5f,"Climb barely rises");
            elapsed.SetValue(sequence,2.31f);Check(!sequence.MoveNext(),"Climb did not finish");
            Check(Vector3.Distance(cat0.transform.position,start)<.001f && !(bool)typeof(CatHomeBehaviour).GetField("externalAnimation",Private).GetValue(cat0),"Climb did not restore resident");
            foreach(var cat in cats)UnityEngine.Object.DestroyImmediate(cat.gameObject);UnityEngine.Object.DestroyImmediate(tree);
            var economy=CozyEconomyManager.Instance;SaveService.Data.lovePoints=200;
            SaveService.Data.itemPurchases.Clear(); SaveService.Data.itemPurchasesMigrated=true;
            SaveService.Data.furniture.Clear();var item=flow.Catalog.GetItem("item_rug");economy.TryBuy(item);
            SaveService.Data.furniture.Add(new SaveService.FurnitureCopy{instanceId="shop-test-storage",itemId=item.id,placed=false});
            var shop=flow.GetComponentInChildren<ShopScreen>(true);shop.gameObject.SetActive(true);
            for(Transform parent=shop.transform.parent;parent!=null;parent=parent.parent)parent.gameObject.SetActive(true);
            shop.transform.SetAsLastSibling();shop.Rebuild();
            Canvas.ForceUpdateCanvases();
            var buy=shop.GetComponentsInChildren<Button>(true).First(b=>b.name=="BuyButton");
            var card=buy.transform.parent;
            var buttons=new[]{buy,card.Find("PlaceStoredButton").GetComponent<Button>(),card.Find("SellOneButton").GetComponent<Button>()};
            for(int i=0;i<buttons.Length;i++)
            {
                var rect=buttons[i].GetComponent<RectTransform>();Check(Mathf.Abs(rect.anchoredPosition.y+130)<.01f,"Shop actions on separate rows");
                if(i>0){var previous=buttons[i-1].GetComponent<RectTransform>();Check(rect.anchoredPosition.x-rect.sizeDelta.x*.5f>previous.anchoredPosition.x+previous.sizeDelta.x*.5f,"Shop action buttons overlap");}
                Check(Mathf.Abs(rect.anchoredPosition.x)+rect.sizeDelta.x*.5f<=125,"Shop action outside card");
            }
            var camera=Camera.main;camera.orthographicSize=5f;camera.transform.position=new Vector3(0,0,-10);
            foreach(var canvas in flow.GetComponentsInChildren<Canvas>(true)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;canvas.sortingOrder=200;}
            Canvas.ForceUpdateCanvases();var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("AnimalArtSources-20261018/shop-inline-actions.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("HOME_SHOP_UPDATE_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
