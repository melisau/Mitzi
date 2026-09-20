#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PawPath.Data;

[InitializeOnLoad]
public static class RealisticHomeBackgroundBuilder
{
    const string ImagePath = "Assets/_Game/Art/home_background_gemini.jpg";
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
    const string SessionKey = "PawPath.GeminiHomeBackground.v3";

    static RealisticHomeBackgroundBuilder()
    {
        if (!SessionState.GetBool(SessionKey, false))
            EditorApplication.delayCall += Apply;
    }

    [MenuItem("PawPath/İçerik/Gerçekçi Ev Arka Planını Uygula")]
    public static void Apply()
    {
        SessionState.SetBool(SessionKey, true);
        // Dosya Unity dışından kopyalanmış olabilir. Importer aranmadan önce
        // AssetDatabase'e açıkça tanıtarak ilk domain reload yarışını önle.
        AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceSynchronousImport |
            ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = false;
        importer.SaveAndReimport();

        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        var background = AssetDatabase.LoadAssetAtPath<Sprite>(ImagePath);
        if (catalog == null || background == null)
            return;

        catalog.homeBackground = background;
        catalog.shopBackground = background;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log("Gerçekçi kedi evi arka planı uygulandı.");
    }
}
#endif
