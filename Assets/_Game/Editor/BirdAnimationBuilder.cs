#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PawPath.Data;

[InitializeOnLoad]
public static class BirdAnimationBuilder
{
    const string Folder = "Assets/_Game/Art/Bird";
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
    static BirdAnimationBuilder() => EditorApplication.delayCall += Apply;

    [MenuItem("PawPath/İçerik/Kuş Animasyonunu Uygula")]
    public static void Apply()
    {
        var frames = new Sprite[4];
        for (int i = 0; i < frames.Length; i++)
        {
            string path = $"{Folder}/bird_fly_{i + 1:00}.png";
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
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        if (catalog == null) return;
        catalog.birdFrames = frames;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }
}
#endif
