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
        string[] names =
        {
            "mitzi_sleep_01.png",
            "mitzi_sleep_02.png",
            "mitzi_sleep_03.png",
            "mitzi_sleep_05.png",
            "mitzi_sleep_06.png"
        };
        var frames = new Sprite[names.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            string path = $"{Folder}/{names[i]}";
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
