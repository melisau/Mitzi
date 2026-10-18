#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using PawPath.Data;
using PawPath.Content;
using PawPath.Hub;
using UnityEditor;
using UnityEngine;
public static class AssetCleanupValidation
{
    public static void Run()
    {
        try
        {
            if(Application.productName != "MitziValidation") throw new Exception("Use isolated project");
            AnimatedTutorialValidation.Run();
            var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            foreach(var item in new[]{"item_food_bowl", "item_food_bowl_cream", "item_food_bowl_copper"})
            {
                var data=AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/_Game/Content/"+item+".asset");
                if(data.interactionFrames.Length!=4 || data.interactionFrames.Any(f=>f==null || !AssetDatabase.GetAssetPath(f).Contains("AnimalsRealistic")))
                    throw new Exception("Food fallback references obsolete art: "+item);
            }
            var litter=AssetDatabase.LoadAssetAtPath<ShopItemDefinition>("Assets/_Game/Content/item_cat_sand.asset");
            if(litter.interactionFrames.Length!=0) throw new Exception("Unused litter sprites remain");
            var catRoot=new GameObject("LitterResident");
            var cat=catRoot.AddComponent<CatHomeBehaviour>(); cat.BindDefinition(catalog.GetCat("mitzi"));
            var renderer=new GameObject("Visual").AddComponent<SpriteRenderer>(); renderer.transform.SetParent(catRoot.transform); renderer.sprite=cat.Definition.idleSprite;
            var furniture=new GameObject("LitterFurniture"); furniture.AddComponent<SpriteRenderer>().sprite=litter.placedSprite;
            var interaction=furniture.AddComponent<FurnitureInteraction>();
            var method=typeof(FurnitureInteraction).GetMethod("PlayLitterBoxSequence",BindingFlags.Instance|BindingFlags.NonPublic);
            var sequence=(System.Collections.IEnumerator)method.Invoke(interaction,new object[]{cat});
            if(!sequence.MoveNext() || renderer.sprite==null) throw new Exception("Litter animation needs removed furniture frames");
            ((IDisposable)sequence).Dispose();
            UnityEngine.Object.DestroyImmediate(catRoot); UnityEngine.Object.DestroyImmediate(furniture);
            Debug.Log("ASSET_CLEANUP_VALIDATION_PASS");
        }
        catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
