#if UNITY_EDITOR
using UnityEditor;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif
using UnityEngine;
[InitializeOnLoad]
// Silver Egyptian Mau paw artwork with black pads; shared by all launcher icon formats.
public static class AppIconBuilder
{
    static AppIconBuilder()=>EditorApplication.delayCall+=Apply;
    [MenuItem("Mitzi/Apply Paw App Icon")]
    public static void Apply()
    {
        const string path="Assets/_Game/Art/AppIcon/paw_icon_adaptive.png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)return;
        if(importer.textureType!=TextureImporterType.Default || importer.maxTextureSize<2048 || importer.mipmapEnabled)
        { importer.textureType=TextureImporterType.Default;importer.maxTextureSize=2048;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport(); }
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{texture});
#if UNITY_ANDROID
        foreach(var kind in new[]{AndroidPlatformIconKind.Legacy,AndroidPlatformIconKind.Round,AndroidPlatformIconKind.Adaptive})
        {
            var icons=PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android,kind);
            foreach(var icon in icons)
            {
                for(int layer=0;layer<icon.maxLayerCount;layer++)icon.SetTexture(texture,layer);
            }
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android,kind,icons);
        }
#endif
    }
}
#endif
