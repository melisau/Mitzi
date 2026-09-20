#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PawPath.Content;
using PawPath.Data;

[InitializeOnLoad]
public static class ShopFurnitureBuilder
{
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
    const string SessionKey = "PawPath.ShopFurnitureBuilder.v1";

    struct ItemData
    {
        public string id, name, description, imagePath, assetPath;
        public int cost;
        public FurnitureSlotType slot;

        public ItemData(string id, string name, string description, int cost,
            FurnitureSlotType slot, string imagePath)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.cost = cost;
            this.slot = slot;
            this.imagePath = imagePath;
            assetPath = $"Assets/_Game/Content/item_{id}.asset";
        }
    }

    static readonly ItemData[] Items =
    {
        new ItemData("cat_poster", "Kedi Posteri", "Duvara asılan neşeli kedi posteri.", 30,
            FurnitureSlotType.Poster, "Assets/cat_poster.jpg"),
        new ItemData("cat_water", "Su Kabı", "Kedilerin su içmek için kullanacağı su kabı.", 25,
            FurnitureSlotType.Water, "Assets/cat_water.jpg"),
        new ItemData("cat_sand", "Kedi Kumu", "Kediler için temiz ve rahat kum alanı.", 35,
            FurnitureSlotType.Sand, "Assets/cat_sand.jpg"),
        new ItemData("cat_tree", "Kedi Ağacı", "Tırmanmak, dinlenmek ve evi seyretmek için.", 80,
            FurnitureSlotType.Tree, "Assets/cat_tree.jpg")
    };

    static ShopFurnitureBuilder()
    {
        if (!SessionState.GetBool(SessionKey, false))
            EditorApplication.delayCall += Build;
    }

    [MenuItem("PawPath/İçerik/Yeni Dükkan Eşyalarını Yenile")]
    public static void Build()
    {
        SessionState.SetBool(SessionKey, true);
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning("PawPathCatalog bulunamadı; yeni dükkan eşyaları oluşturulamadı.");
            return;
        }

        if (catalog.shopItems == null)
            catalog.shopItems = new List<ShopItemDefinition>();

        foreach (var data in Items)
        {
            PrepareSprite(data.imagePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(data.imagePath);
            var item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(data.assetPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ShopItemDefinition>();
                AssetDatabase.CreateAsset(item, data.assetPath);
            }

            item.id = data.id;
            item.displayName = data.name;
            item.description = data.description;
            item.lovePointCost = data.cost;
            item.slotType = data.slot;
            item.icon = sprite;
            item.placedSprite = sprite;
            EditorUtility.SetDirty(item);

            catalog.shopItems.RemoveAll(existing => existing != null && existing.id == data.id && existing != item);
            if (!catalog.shopItems.Contains(item))
                catalog.shopItems.Add(item);
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Poster, su kabı, kedi kumu ve kedi ağacı dükkana eklendi.");
    }

    static void PrepareSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        bool changed = importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        if (changed)
            importer.SaveAndReimport();
    }
}
#endif
