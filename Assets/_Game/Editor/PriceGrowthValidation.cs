#if UNITY_EDITOR
using System;
using System.Linq;
using PawPath.Core;
using PawPath.Content;
using PawPath.Economy;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class PriceGrowthValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            if(Application.productName!="MitziValidation")throw new Exception("Use isolated project");
            HomeShopUpdateValidation.Run(); SaveServiceValidation.Run();
            var economy=CozyEconomyManager.Instance;
            var shop=GameFlow.Instance.GetComponentInChildren<PawPath.UI.ShopScreen>(true);
            var buy=shop.GetComponentsInChildren<Button>(true).First(b=>b.name=="BuyButton");
            var first=GameFlow.Instance.Catalog.shopItems[0];
            Check(buy.GetComponentInChildren<Text>().text==economy.GetPurchasePrice(first).ToString(),"Shop shows base price instead of next purchase price");
            var item=ScriptableObject.CreateInstance<ShopItemDefinition>();item.id="price-growth-test";item.lovePointCost=40;
            SaveService.Data.itemPurchases.RemoveAll(p=>p.itemId==item.id);SaveService.Data.furniture.RemoveAll(f=>f.itemId==item.id);SaveService.Data.lovePoints=2000;
            foreach(int expected in new[]{40,120,360,1080})
            {
                Check(economy.GetPurchasePrice(item)==expected,"Wrong exponential price");
                int before=economy.ShoppingPoints;Check(economy.TryBuy(item),"Purchase failed");
                Check(economy.ShoppingPoints==before-expected,"Incorrect wallet debit");
            }
            Check(economy.GetPurchasePrice(item)==3240 && !economy.TryBuy(item) && economy.PurchaseCount(item.id)==4,"Failed purchase changes history");
            Check(economy.SellOne(item) && economy.GetPurchasePrice(item)==3240,"Selling resets price growth");
            SaveService.Persist();SaveService.Load();Check(economy.GetPurchasePrice(item)==3240,"Reload loses purchase history");
            var other=ScriptableObject.CreateInstance<ShopItemDefinition>();other.id="different-price-test";other.lovePointCost=40;
            Check(economy.GetPurchasePrice(other)==40,"One item's purchases affect another");
            SaveService.Data.itemPurchases.Find(p=>p.itemId==item.id).count=100;
            SaveService.Data.lovePoints=int.MaxValue;
            Check(economy.GetPurchasePrice(item)==long.MaxValue && !economy.CanAfford(item),"Price overflow creates cheap items");
            string old="{\"schemaVersion\":3,\"highestCompletedLevel\":0,\"unlockedCatIds\":[\"mitzi\"],\"lovePoints\":200,\"furnitureInventoryMigrated\":true,\"furniture\":[{\"instanceId\":\"a\",\"itemId\":\"bed\"},{\"instanceId\":\"b\",\"itemId\":\"bed\"}]}";
            Check(SaveService.TryReadJson(old,out var migrated,out bool future) && !future && migrated.itemPurchases.Single(p=>p.itemId=="bed").count==2,"Legacy copies not migrated to price history");
            Check(SaveService.TryReadJson(JsonUtility.ToJson(migrated),out var again,out future) && again.itemPurchases.Single(p=>p.itemId=="bed").count==2,"Migration counts copies twice");
            UnityEngine.Object.DestroyImmediate(item);UnityEngine.Object.DestroyImmediate(other);
            Debug.Log("PRICE_GROWTH_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
