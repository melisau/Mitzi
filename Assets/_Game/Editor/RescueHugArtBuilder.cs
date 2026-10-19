#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PawPath.Data;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RescueHugArtBuilder
{
    public const string Root="Assets/_Game/Art/AnimalsRealistic/RescueHugs";
    static readonly (string id,string coat)[] Cats={ ("mitzi","silver"),("ghost","ghost"),("boza","boza"),("tarcin","calico"),("pamuk","greywhite"),("ada","greywhite"),("komur","black"),("moka","black") };
    static RescueHugArtBuilder()=>EditorApplication.delayCall+=Apply;
    [MenuItem("Mitzi/Build Rescue Hug Animations")]
    public static void Apply()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");if(catalog==null)return;
        foreach(var coat in Cats.Select(c=>c.coat).Distinct())
        {
            string path=Root+"/"+coat+".png";if(!File.Exists(path))continue;
            var frames=Import(path,out var height,out var contact);
            foreach(var entry in Cats.Where(c=>c.coat==coat))
            {
                var cat=catalog.GetCat(entry.id);if(cat==null)continue;
                cat.rescueHugFrames=frames;cat.rescueHugBodyHeight=height;cat.rescueHugContactX=contact;EditorUtility.SetDirty(cat);
            }
        }
        AssetDatabase.SaveAssets();
    }
    static Sprite[] Import(string path,out float bodyHeight,out float contactX)
    {
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));var pixels=image.GetPixels32();
        int middle=Gutter(image,pixels,false,image.height/2,image.height/12,0,image.width);
        var metadata=new List<SpriteMetaData>();var heights=new List<float>();var contacts=new List<float>();
        for(int row=0;row<2;row++)
        {
            int bottom=row==0?middle:0,top=row==0?image.height:middle;
            int[] edges={0,0,0,image.width};for(int i=1;i<3;i++)edges[i]=Gutter(image,pixels,true,image.width*i/3,image.width/18,bottom,top);
            for(int col=0;col<3;col++)
            {
                int left=edges[col],right=edges[col+1],minX=right,maxX=left,minY=top,maxY=bottom;
                for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)if(pixels[y*image.width+x].a>96){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                if(maxY<=minY)throw new Exception("Empty hug frame "+path);
                float footCenter=0;int count=0;
                for(int y=minY;y<minY+(maxY-minY)/10;y++)for(int x=minX;x<=maxX;x++)if(pixels[y*image.width+x].a>96){footCenter+=x;count++;}
                footCenter=count>0?footCenter/count:(minX+maxX)*.5f;
                int headRight=minX;
                for(int y=minY+(maxY-minY)*3/4;y<=maxY;y++)for(int x=minX;x<=maxX;x++)if(pixels[y*image.width+x].a>96)headRight=Mathf.Max(headRight,x);
                heights.Add((maxY-minY+1)/100f);contacts.Add(Mathf.Max(.1f,(headRight-footCenter)/100f));
                minX=Mathf.Max(left,minX-2);maxX=Mathf.Min(right-1,maxX+2);maxY=Mathf.Min(top-1,maxY+2);
                metadata.Add(new SpriteMetaData{name="hug_"+(metadata.Count+1).ToString("00"),rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2((footCenter-minX)/(maxX-minX+1),0)});
            }
        }
        UnityEngine.Object.DestroyImmediate(image);bodyHeight=(heights[3]+heights[4])*.5f;contactX=(contacts[3]+contacts[4])*.5f;
        var old=importer.spritesheet;
        if(importer.spriteImportMode!=SpriteImportMode.Multiple||old.Length!=6||old.Where((m,i)=>m.rect!=metadata[i].rect||m.pivot!=metadata[i].pivot||m.name!=metadata[i].name).Any())
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=100;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.spritesheet=metadata.ToArray();importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }
    static int Gutter(Texture2D image,Color32[] pixels,bool vertical,int expected,int radius,int low,int high)
    {
        int best=expected,score=int.MaxValue;
        for(int point=Mathf.Max(1,expected-radius);point<Mathf.Min((vertical?image.width:image.height)-1,expected+radius);point++)
        {int count=0;for(int along=low;along<high;along++)if(pixels[(vertical?along:point)*image.width+(vertical?point:along)].a>96)count++;
            int value=count*1000+Mathf.Abs(point-expected);if(value<score){score=value;best=point;}}
        return best;
    }
}
#endif
