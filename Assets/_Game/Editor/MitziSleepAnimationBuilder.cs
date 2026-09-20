#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PawPath.Data;

[InitializeOnLoad]
public static class MitziSleepAnimationBuilder
{
    const string Folder = "Assets/_Game/Art/MitziSleep";
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
    static MitziSleepAnimationBuilder() => EditorApplication.delayCall += Apply;

    [MenuItem("PawPath/İçerik/Mitzi Uyku Animasyonunu Uygula")]
    public static void Apply()
    {
        var frames = new Sprite[2];
        for (int i = 0; i < frames.Length; i++)
        {
            string path = $"{Folder}/mitzi_sleep_{i + 5:00}.png";
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
        var mitzi = catalog != null ? catalog.cats.Find(cat => cat != null && cat.id == "mitzi") : null;
        if (mitzi == null)
            return;
        mitzi.sleepFrames = frames;
        EditorUtility.SetDirty(mitzi);
        AssetDatabase.SaveAssets();
    }
}
#endif
