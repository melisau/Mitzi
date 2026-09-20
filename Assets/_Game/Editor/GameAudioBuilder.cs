#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PawPath.Data;

[InitializeOnLoad]
public static class GameAudioBuilder
{
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";

    static GameAudioBuilder() => EditorApplication.delayCall += Apply;

    [MenuItem("PawPath/İçerik/Oyun Seslerini Uygula")]
    public static void Apply()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        if (catalog == null)
            return;

        catalog.homeMusic = Clip("home_loop", "ogg");
        catalog.streetMusic = Clip("street_loop", "ogg");
        catalog.forestMusic = Clip("forest_loop", "ogg");
        catalog.cityMusic = Clip("city_loop", "ogg");
        catalog.catStepSfx = Clip("cat_step", "wav");
        catalog.catMeowSfx = new[]
        {
            Clip("cat_meow", "wav"), Clip("cat_meow2", "wav"),
            Clip("cat_meow3", "wav"), Clip("cat_meow4", "wav")
        };
        catalog.catHurtSfx = Clip("cat_hurt", "wav");
        catalog.catFallSfx = Clip("cat_fall", "wav");
        catalog.catRescueSfx = Clip("cat_resque", "wav");
        catalog.errorSfx = Clip("error-notification", "wav");
        catalog.uiClickSfx = Clip("ui_click", "wav");
        catalog.uiConfirmSfx = Clip("ui_confirm", "wav");
        catalog.shopBuySfx = Clip("ui_shop_buy", "wav");
        catalog.shopSellSfx = Clip("ui_shop_sell", "wav");
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    static AudioClip Clip(string fileName, string preferredExtension)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/{fileName}.{preferredExtension}");
    }
}
#endif
