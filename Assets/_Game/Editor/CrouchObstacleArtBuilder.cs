#if UNITY_EDITOR
using PawPath.Data;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class CrouchObstacleArtBuilder
{
    static CrouchObstacleArtBuilder()=>EditorApplication.delayCall+=Apply;
    [MenuItem("PawPath/İçerik/Eğilme Engellerini Uygula")]
    public static void Apply()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");if(catalog==null)return;
        catalog.streetCrouchObstacle=Load("street");catalog.forestCrouchObstacle=Load("forest");catalog.cityCrouchObstacle=Load("city");catalog.cyberCrouchObstacle=Load("cyber");
        catalog.crouchUpperObstacles=new[]{Load("street",true),Load("forest",true),Load("city",true),Load("cyber",true)};
        EditorUtility.SetDirty(catalog);EditorSave.Save(catalog);
    }
    static Sprite Load(string theme,bool upper=false)
    {
        string path="Assets/_Game/Art/CrouchObstacles/"+theme+(upper ? "_crouch_upper.png" : "_crouch_grounded.png");
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)return null;
        if(importer.textureType!=TextureImporterType.Sprite || importer.spritePivot!=new Vector2(.5f,0) || importer.spritePixelsPerUnit!=100f)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2(.5f,0);importer.SetTextureSettings(settings);importer.spritePixelsPerUnit=100;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
