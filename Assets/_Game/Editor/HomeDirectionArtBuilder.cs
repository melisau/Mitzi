#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PawPath.Data;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HomeDirectionArtBuilder
{
    public const string Root = "Assets/_Game/Art/AnimalsRealistic/HomeDirections";
    const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
    static readonly (string id,string coat)[] Cats = {
        ("mitzi","silver"),("tarcin","calico"),("pamuk","greywhite"),("ada","greywhite"),("komur","black"),("moka","black"),("ghost","ghost"),("boza","boza")
    };
    static HomeDirectionArtBuilder() => EditorApplication.delayCall += Apply;
    [MenuItem("Mitzi/Build Home Front And Back Animations")]
    public static void Apply()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
        if (catalog == null) return;
        var available=Cats.Where(c=>catalog.GetCat(c.id)!=null&&File.Exists(Root+"/"+c.coat+".png")).ToArray();
        var packs = new Dictionary<string,Sprite[]>();
        var heights = new Dictionary<string,float[]>();
        foreach (var coat in available.Select(c=>c.coat).Distinct())
        {
            packs[coat] = Import(coat,out var bodyHeights); heights[coat] = bodyHeights;
        }
        foreach (var entry in available)
        {
            var cat = catalog.GetCat(entry.id); if(cat==null)continue;
            cat.homeTowardFrames = packs[entry.coat].Take(6).ToArray();
            cat.homeAwayFrames = packs[entry.coat].Skip(6).Take(6).ToArray();
            cat.homeTowardBodyHeight = Median(heights[entry.coat].Take(6));
            cat.homeAwayBodyHeight = Median(heights[entry.coat].Skip(6));
            if(entry.coat!="ghost"&&entry.coat!="boza")cat.homeVisibleHeightRatio = SideVisibleRatio(entry.coat);
            EditorUtility.SetDirty(cat);
        }
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
    }
    static float Median(IEnumerable<float> values) { var array=values.OrderBy(v=>v).ToArray(); return (array[2]+array[3])*.5f; }

    static Sprite[] Import(string coat,out float[] bodyHeights)
    {
        string path=Root+"/"+coat+".png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null){AssetDatabase.ImportAsset(path);importer=AssetImporter.GetAtPath(path) as TextureImporter;}
        var image=new Texture2D(2,2,TextureFormat.RGBA32,false);image.LoadImage(File.ReadAllBytes(path));
        var pixels=image.GetPixels32();var metadata=new List<SpriteMetaData>();var measured=new List<float>();
        int middle=Gutter(image.width,image.height,pixels,false,image.height/2,image.height/10,0,image.width);
        for(int row=0;row<2;row++)
        {
            int bottom=row==0?middle:0,top=row==0?image.height:middle;
            int[] columns=new int[7];columns[6]=image.width;
            for(int i=1;i<6;i++)columns[i]=Gutter(image.width,image.height,pixels,true,image.width*i/6,image.width/36,bottom,top);
            for(int col=0;col<6;col++)
            {
                int left=columns[col],right=columns[col+1],minX=right,maxX=left,minY=top,maxY=bottom,maxSpan=0;
                var spans=new int[top-bottom];
                for(int y=bottom;y<top;y++)
                {
                    int rowLeft=right,rowRight=left-1;
                    for(int x=left;x<right;x++)if(pixels[y*image.width+x].a>96)
                    {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);rowLeft=Mathf.Min(rowLeft,x);rowRight=Mathf.Max(rowRight,x);}
                    spans[y-bottom]=Mathf.Max(0,rowRight-rowLeft+1);maxSpan=Mathf.Max(maxSpan,spans[y-bottom]);
                }
                if(minY>=maxY)throw new InvalidOperationException("Empty home direction frame "+coat+"/"+row+"/"+col);
                int earTop=minY;
                for(int y=minY;y<=maxY;y++)if(spans[y-bottom]>maxSpan*.55f)earTop=y;
                measured.Add((earTop-minY+1)/100f);
                // Tight rectangles retain all fur and tail pixels; feet and center align between views.
                int pad=2;minX=Mathf.Max(left,minX-pad);maxX=Mathf.Min(right-1,maxX+pad);maxY=Mathf.Min(top-1,maxY+pad);
                float centre=0;int count=0;
                for(int y=minY;y<Mathf.Min(maxY,minY+(top-bottom)/12);y++)
                for(int x=minX;x<=maxX;x++)if(pixels[y*image.width+x].a>96){centre+=x;count++;}
                float pivotX=count>0?(centre/count-minX)/(maxX-minX+1):.5f;
                metadata.Add(new SpriteMetaData{name=(row==0?"toward_":"away_")+(col+1).ToString("00"),
                    rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(pivotX,0)});
            }
        }
        UnityEngine.Object.DestroyImmediate(image);bodyHeights=measured.ToArray();
        var old=importer.spritesheet;
        bool changed=importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Multiple||importer.mipmapEnabled||old.Length!=12||
            old.Where((v,i)=>v.rect!=metadata[i].rect||v.pivot!=metadata[i].pivot||v.name!=metadata[i].name).Any();
        if(changed)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=100;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.spritesheet=metadata.ToArray();importer.SaveAndReimport();
        }
        var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        return Enumerable.Range(1,6).Select(i=>sprites.Single(s=>s.name=="toward_"+i.ToString("00")))
            .Concat(Enumerable.Range(1,6).Select(i=>sprites.Single(s=>s.name=="away_"+i.ToString("00")))).ToArray();
    }
    static int Gutter(int width,int height,Color32[] pixels,bool vertical,int expected,int radius,int low,int high)
    {
        int best=expected,score=int.MaxValue;
        for(int point=Mathf.Max(1,expected-radius);point<=Mathf.Min((vertical?width:height)-1,expected+radius);point++)
        {
            int count=0;for(int along=low;along<high;along++)if(pixels[(vertical?along:point)*width+(vertical?point:along)].a>96)count++;
            int value=count*1000+Mathf.Abs(point-expected);if(value<score){score=value;best=point;}
        }
        return best;
    }
    static float SideVisibleRatio(string coat)
    {
        var image=new Texture2D(2,2,TextureFormat.RGBA32,false);image.LoadImage(File.ReadAllBytes("Assets/_Game/Art/AnimalsRealistic/Cats/"+coat+"/idle_01.png"));
        var pixels=image.GetPixels32();int left=image.width,right=0,bottom=image.height,ear=0;
        for(int y=0;y<image.height;y++)for(int x=0;x<image.width;x++)if(pixels[y*image.width+x].a>96){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);}
        for(int y=0;y<image.height;y++)for(int x=left+(right-left)*2/3;x<=right;x++)if(pixels[y*image.width+x].a>96)ear=Mathf.Max(ear,y);
        float ratio=(ear-bottom+1f)/image.height;UnityEngine.Object.DestroyImmediate(image);return ratio;
    }
}
#endif
