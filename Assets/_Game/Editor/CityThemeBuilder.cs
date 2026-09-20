#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PawPath.Data;

[InitializeOnLoad]
public static class CityThemeBuilder
{
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
    static readonly string[] Paths =
    {
        "Assets/_Game/Art/city_avenue_background_v1.png",
        "Assets/_Game/Art/city_trash_gap_v1.png",
        "Assets/_Game/Art/city_sidewalk_platform_v1.png",
        "Assets/_Game/Art/city_car_small_v1.png",
        "Assets/_Game/Art/city_car_van_v1.png"
    };

    static CityThemeBuilder() => EditorApplication.delayCall += Apply;

    [MenuItem("PawPath/İçerik/Cadde Temasını Uygula")]
    public static void Apply()
    {
        foreach (string path in Paths)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = path.Contains("car_") || path.Contains("trash_gap");
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        if (catalog == null)
            return;
        catalog.cityBackground = AssetDatabase.LoadAssetAtPath<Sprite>(Paths[0]);
        catalog.cityGapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Paths[1]);
        catalog.cityPlatformSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Paths[2]);
        catalog.cityCarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Paths[3]);
        catalog.cityVanSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Paths[4]);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }
}
#endif
