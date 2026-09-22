#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PawPath.Content;
using PawPath.Data;

[InitializeOnLoad]
public static class GameplayMechanicsAssetBuilder
{
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
    const string FoodItemPath = "Assets/_Game/Content/item_food_bowl.asset";
    // v2 köpek karelerini eski Default Texture durumundan zorla Sprite'a geçirir.
    const string SessionKey = "PawPath.GameplayMechanicsAssets.v2";

    static GameplayMechanicsAssetBuilder()
    {
        if (!SessionState.GetBool(SessionKey, false))
            EditorApplication.delayCall += Apply;
    }

    [MenuItem("PawPath/İçerik/Köpek, Ağaç ve Etkileşim Görsellerini Uygula")]
    public static void Apply()
    {
        SessionState.SetBool(SessionKey, true);
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        if (catalog == null) return;

        catalog.dogRunFrames = LoadSeries("Assets/dog_run ({0}).png", 14);
        catalog.climbingCatFrames = LoadSeries("Assets/climbing_cat ({0}).png", 4);
        catalog.climbTreeSprite = LoadSprite("Assets/climb_tree.png");

        var food = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(FoodItemPath);
        if (food != null)
        {
            food.interactionType = ItemInteractionType.Eating;
            food.interactionFrames = LoadSeries("Assets/cat_eating ({0}).png", 4);
            EditorUtility.SetDirty(food);
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("Köpek koşusu, tırmanma ve Mitzi mama yeme kareleri bağlandı.");
    }

    static Sprite[] LoadSeries(string pattern, int count)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            frames[i] = LoadSprite(string.Format(pattern, i + 1));
        return frames;
    }

    static Sprite LoadSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
