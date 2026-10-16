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
            "mitzi_sleep_03.png"
        };
        var frames = new System.Collections.Generic.List<Sprite>();
        foreach (var name in names)
        {
            string path = $"{Folder}/{name}";
            // Eksik kareyi uyarıyla geçmek yerine sessizce atla (null kare animasyonu bozar).
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                continue;
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
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                frames.Add(sprite);
        }
        if (frames.Count == 0)
            return;

        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        var mitzi = catalog != null ? catalog.cats.Find(cat => cat != null && cat.id == "mitzi") : null;
        if (mitzi == null)
            return;
        var newFrames = frames.ToArray();
        if (mitzi.sleepFrames != null && System.Linq.Enumerable.SequenceEqual(mitzi.sleepFrames, newFrames))
            return;   // değişiklik yoksa kataloğu kirletme
        mitzi.sleepFrames = newFrames;
        EditorUtility.SetDirty(mitzi);
        EditorSave.Save(mitzi, catalog);
    }
}
#endif
