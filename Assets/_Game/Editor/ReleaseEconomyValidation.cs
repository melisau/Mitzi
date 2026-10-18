#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using PawPath.Ads;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Economy;
using PawPath.Hub;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ReleaseEconomyValidation
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run()
    {
        try
        {
            DogTerrainValidation.Run();
            var flow=GameFlow.Instance;
            Check(!flow.GetComponentsInChildren<Button>(true).Any(b=>b.name.StartsWith("Developer")),"Test buttons remain in player UI");
            PlayerPrefs.SetInt("PawPath.Developer.MitziOnly",1);
            Check(!SaveService.DeveloperMitziOnly,"Legacy developer flag still affects game");
            var economy=flow.GetComponent<CozyEconomyManager>(); typeof(CozyEconomyManager).GetProperty("Instance").SetValue(null,economy);
            var needs=flow.GetComponent<CatNeedsSystem>(); typeof(CatNeedsSystem).GetProperty("Instance").SetValue(null,needs);
            var data=SaveService.Data; data.selectedCatId="mitzi"; data.lovePoints=0;
            var state=needs.GetNeeds("mitzi"); state.affection=17;
            var ad=flow.GetComponentInChildren<RewardedAdButton>(true);
            typeof(RewardedAdButton).GetMethod("GrantReward",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ad,null);
            Check(economy.ShoppingPoints==5 && state.affection==17,"Ad changed affection or wrong shopping reward");
            PlayerPrefs.SetString("DailyPettingDate.mitzi",DateTime.Now.ToString("yyyy-MM-dd")); PlayerPrefs.SetInt("DailyPettingPoints.mitzi",0);
            for(int i=0;i<20;i++) needs.TryPetCat(1,"mitzi");
            Check(economy.ShoppingPoints==15 && state.affection==37,"Petting reward cap blocks care or gives excessive coins");
            string interaction="ReleaseEconomyValidation";
            PlayerPrefs.DeleteKey("DailyInteraction."+interaction+".mitzi");
            needs.RewardInteraction(5,interaction); needs.RewardInteraction(5,interaction);
            Check(economy.ShoppingPoints==17,"Repeated furniture taps generate unlimited money");
            var item=flow.Catalog.GetItem("food_bowl"); data.furniture.Clear();
            SaveService.Data.itemPurchases.Clear(); SaveService.Data.itemPurchasesMigrated=true;
            data.lovePoints=item.lovePointCost+10;
            Check(economy.TryBuy(item) && economy.ShoppingPoints==10,"Shop uses wrong wallet");
            Check(economy.SellOne(item) && economy.ShoppingPoints==10+Mathf.CeilToInt(item.lovePointCost*.5f),"Sale refund changed");
            var petRoot=new GameObject("PetFeedbackValidation"); petRoot.transform.position=new Vector3(0,-2.5f);
            var visual=new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(petRoot.transform,false); visual.sprite=flow.Catalog.GetCat("mitzi").idleSprite; visual.transform.localScale=Vector3.one*1.1f; visual.sortingOrder=60;
            var pet=petRoot.AddComponent<CatPettingSystem>(); pet.Bind(flow.Catalog.GetCat("mitzi"),null);
            typeof(CatPettingSystem).GetMethod("PositionLoveFeedback",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(pet,null);
            var text=petRoot.GetComponentInChildren<TextMesh>(true);
            Check(text.transform.position.y>visual.bounds.center.y+.5f && text.GetComponent<MeshRenderer>().sortingOrder>visual.sortingOrder,"Petting feedback behind cat");
            UnityEngine.Object.DestroyImmediate(petRoot);
            SaveService.Persist();
            Debug.Log("RELEASE_ECONOMY_VALIDATION_PASS");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
