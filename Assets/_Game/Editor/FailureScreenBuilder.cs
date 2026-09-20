#if UNITY_EDITOR
using UnityEditor;
using PawPath.Data;

[InitializeOnLoad]
public static class FailureScreenBuilder
{
    const string ImagePath = "Assets/_Game/Art/failure_screen_background_v1.png";
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";

    static FailureScreenBuilder() => EditorApplication.delayCall += Apply;

    [MenuItem("PawPath/İçerik/Başarısızlık Ekranını Uygula")]
    public static void Apply()
    {
        AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        if (catalog == null)
            return;
        catalog.failureBackground = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(ImagePath);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }
}
#endif
