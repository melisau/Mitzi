#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Gameplay;
using PawPath.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class HudTrimValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Call(object o,string name)=>o.GetType().GetMethod(name,Private).Invoke(o,null);
    public static void Run()
    {
        try
        {
            if(Application.productName!="MitziValidation")throw new Exception("Use isolated project");
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.aspect=16f/9f;
            var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var flow=RuntimeBootstrap.Build(catalog);typeof(GameFlow).GetProperty("Instance").SetValue(null,flow);
            Call(camera.GetComponent<LineDraw>(),"Awake");
            PlayerPrefs.SetInt(GameplayTutorialUI.DirectSeenKey,1);PlayerPrefs.SetInt(GameplayTutorialUI.DrawingSeenKey,1);
            var hud=flow.GetComponentInChildren<HudView>(true);
            var ink=(Text)typeof(HudView).GetField("inkLabel",Private).GetValue(hud);
            GameplayMode.Select(GameplayPlayMode.Drawing);flow.StartNextLevel();Call(hud,"OnLevelStarted");Call(hud,"Update");
            if(!ink.gameObject.activeSelf)throw new Exception("Drawing preparation lost its ink meter");
            GameplayMode.Select(GameplayPlayMode.DirectControl);flow.StartNextLevel();Call(hud,"OnLevelStarted");
            ink.gameObject.SetActive(true);Call(hud,"Update");
            if(ink.gameObject.activeSelf)throw new Exception("Direct controls still show drawing meter");
            var first=catalog.shopItems.First(i=>i!=null);
            SaveService.Data.furniture.Add(new SaveService.FurnitureCopy{instanceId="trim-preview",itemId=first.id,placed=true});
            var shop=flow.GetComponentInChildren<ShopScreen>(true);shop.Rebuild();
            foreach(var button in shop.GetComponentsInChildren<Button>(true).Where(b=>b.name=="SellOneButton"))
            {
                if(button.transform.Find("MoneyReceived")!=null)throw new Exception("White sell arrow remains");
                if(Mathf.Abs(button.transform.Find("Icon").GetComponent<RectTransform>().anchoredPosition.x)>.01f)throw new Exception("Sell coin is off-centre");
            }
            Debug.Log("HUD_TRIM_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
