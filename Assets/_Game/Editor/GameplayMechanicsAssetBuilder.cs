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
    const string SessionKey = "PawPath.GameplayMechanicsAssets.realistic.v4.uprightclimb";

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

        catalog.dogRunFrames = LoadSeries("Assets/_Game/Art/AnimalsRealistic/Dog/dog_run_{0:00}.png", 8);
        catalog.dogIdleSprite = LoadSprite("Assets/_Game/Art/AnimalsRealistic/Dog/dog_idle.png");
        catalog.roboticDogIdleSprite = LoadSprite("Assets/_Game/Art/AnimalsRealistic/RobotDog/dog_idle.png");
        catalog.roboticDogWalkFrames = LoadSeries("Assets/_Game/Art/AnimalsRealistic/RobotDog/dog_run_{0:00}.png", 8);
        catalog.roboticBirdFrames = LoadSeries("Assets/_Game/Art/AnimalsRealistic/RobotBird/bird_fly_{0:00}.png", 8);
        catalog.roboticBird = catalog.roboticBirdFrames[0];
        catalog.cyberBackground = LoadSprite("Assets/cyber_neo.jpg");
        catalog.cyberGroundTile = LoadSprite("Assets/cyber_ground_tile.png");
        catalog.cyberObstacleSprites = LoadSeries("Assets/neo_techno_object ({0}).png", 4);
        catalog.climbingCatFrames = LoadSeries("Assets/_Game/Art/AnimalsRealistic/Cats/silver/climb_{0:00}.png", 12);
        catalog.climbTreeSprite = LoadSprite("Assets/climb_tree.png");

        var food = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(FoodItemPath);
        if (food != null)
        {
            food.interactionType = ItemInteractionType.Eating;
            food.interactionFrames = LoadSeries("Assets/_Game/Art/AnimalsRealistic/Cats/silver/eatanim_{0:00}.png", 4);
            EditorUtility.SetDirty(food);
        }

        EditorUtility.SetDirty(catalog);
        EditorSave.Save(catalog, food);
        Debug.Log("Köpek koşusu, robotik köpek/kuş, Neo Teknoloji zemini ve etkileşim kareleri bağlandı.");
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
            importer.alphaIsTransparency = path.EndsWith(".png");
            importer.mipmapEnabled = false;
            importer.filterMode = path.Contains("cyber_neo") ? FilterMode.Point : FilterMode.Bilinear;
            if (path.Contains("cyber_ground_tile"))
                importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
