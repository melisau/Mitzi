#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PawPath.Data;

[InitializeOnLoad]
public static class HomeRoomOptionsBuilder
{
    static HomeRoomOptionsBuilder() => EditorApplication.delayCall += Apply;
    [MenuItem("PawPath/İçerik/Oda Seçeneklerini Uygula")]
    public static void Apply()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
        if (catalog == null) return;
        var names = new[] { "01_bahce", "02_avlu", "03_mavi" };
        var options = new Sprite[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            string path = "Assets/_Game/Art/HomeRoomOptions/" + names[i] + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || importer.maxTextureSize != 2048)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            options[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        catalog.homeRoomOptions = options;
        string[] ids={"wall_garden","wall_courtyard","wall_blue_room"};
        string[] labels={"Bahçe Manzaralı Oda","Avlu Manzaralı Oda","Mavi Oda"};
        for(int i=0;i<ids.Length;i++)
        {
            string path="Assets/_Game/Content/item_"+ids[i]+".asset";
            var item=AssetDatabase.LoadAssetAtPath<PawPath.Content.ShopItemDefinition>(path);
            if(item==null) { item=ScriptableObject.CreateInstance<PawPath.Content.ShopItemDefinition>();AssetDatabase.CreateAsset(item,path); }
            item.id=ids[i];item.displayName=labels[i];item.lovePointCost=60;
            item.slotType=PawPath.Content.FurnitureSlotType.Wallpaper;item.icon=options[i];item.placedSprite=options[i];
            if(!catalog.shopItems.Contains(item))catalog.shopItems.Add(item);
            EditorUtility.SetDirty(item);EditorSave.Save(item);
        }
        EditorUtility.SetDirty(catalog);
        EditorSave.Save(catalog);
    }
}
#endif
