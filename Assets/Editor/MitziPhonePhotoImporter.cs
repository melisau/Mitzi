using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MitziPhonePhotoImporter
{
    const string Path = "Assets/_Game/Resources/MitziPhone/mitzi-instagram-photos.png";
    static MitziPhonePhotoImporter() => EditorApplication.delayCall += Apply;
    public static void Apply()
    {
        ApplyProfilePhoto();
        var importer = AssetImporter.GetAtPath(Path) as TextureImporter;
        if (importer == null) return;
        // Lossless source remains untouched. Unity metadata selects four square
        // thumbnails from the user's screenshot; no generated/replaced cat faces.
        int[] left = { 7, 314, 624, 932 }, widths = { 306, 309, 307, 311 };
        var sprites = new SpriteMetaData[4];
        for (int i=0;i<4;i++) sprites[i]=new SpriteMetaData {
            name="mitzi_photo_"+(i+1).ToString("00"), rect=new Rect(left[i],(405-widths[i])*.5f,widths[i],widths[i]),
            alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f };
        var old=importer.spritesheet;
        bool changed=importer.spriteImportMode!=SpriteImportMode.Multiple || importer.maxTextureSize!=1024 || importer.mipmapEnabled || importer.isReadable ||
            old.Length!=4 || old.Where((s,i)=>s.name!=sprites[i].name || s.rect!=sprites[i].rect).Any();
        if (!changed) return;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritesheet=sprites;importer.maxTextureSize=1024;importer.mipmapEnabled=false;importer.isReadable=false;
        importer.textureCompression=TextureImporterCompression.Compressed;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }
    static void ApplyProfilePhoto()
    {
        var importer=AssetImporter.GetAtPath("Assets/_Game/Resources/MitziPhone/mitzi-profile-photo.png") as TextureImporter;
        if(importer==null)return;
        // Select the circular portrait from the supplied screenshot, excluding its outer background.
        var rect=new Rect(29,18,150,150);
        var old=importer.spritesheet;
        if(importer.textureType==TextureImporterType.Sprite && importer.spriteImportMode==SpriteImportMode.Multiple &&
            old.Length==1 && old[0].rect==rect && importer.maxTextureSize==256 && !importer.mipmapEnabled)return;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
        importer.spritesheet=new[]{new SpriteMetaData{name="mitzi_profile",rect=rect,alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f}};
        importer.maxTextureSize=256;importer.mipmapEnabled=false;importer.isReadable=false;
        importer.textureCompression=TextureImporterCompression.Compressed;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }
}
