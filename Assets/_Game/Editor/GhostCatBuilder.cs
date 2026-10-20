#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PawPath.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class GhostCatBuilder
{
    public const string Root="Assets/_Game/Art/AnimalsRealistic/Cats";
    static readonly string[] Sheets={"home","walk","action","care","climb"};
    static GhostCatBuilder()=>EditorApplication.delayCall+=Apply;
    [MenuItem("Mitzi/Build Ghost Cat")]
    public static void Apply()
    {
        HomeDirectionArtBuilder.Apply();
        Build("ghost","Ghost",7,"Bembeyaz pofuduk tüylü, bir gözü mavi, diğeri kehribar yeşili bir Van kedisi.");
        Build("boza","Boza",20,"Beyaz göğüslü, yeşil gözlü, turuncu ve kahverengi tekir desenli sıcacık bir dost.");
        RescueMilestoneBuilder.Apply();HomeDirectionArtBuilder.Apply();
    }
    static void Build(string id,string displayName,int milestone,string bio)
    {
        string root=Root+"/"+id;
        if(Sheets.Any(s=>!File.Exists(root+"/"+s+".png")))return;
        var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
        if(catalog==null)return;
        var packs=Sheets.ToDictionary(s=>s,s=>Import(root,s));
        var cat=catalog.GetCat(id);
        if(cat==null){cat=ScriptableObject.CreateInstance<CatDefinition>();cat.name="Cat_"+displayName;cat.id=id;AssetDatabase.AddObjectToAsset(cat,catalog);catalog.cats.Add(cat);}
        cat.displayName=displayName;cat.unlockAfterLevel=milestone;cat.isStarterCat=false;cat.furTint=Color.white;cat.personality=CatPersonality.Curious;
        cat.bio=bio;
        cat.encounterDialogue=displayName+" kapının yanında seni bekliyor. Artık onun da sıcacık bir evi var.";
        cat.walkFrames=packs["walk"];cat.idleFrames=packs["home"].Take(4).ToArray();cat.idleSprite=cat.idleFrames[0];
        cat.portrait=packs["action"][10];cat.jumpFrames=packs["action"].Take(4).ToArray();cat.jumpApexSprite=packs["action"][4];
        cat.crouchWalkFrames=packs["action"].Skip(5).Take(4).ToArray();cat.sleepFrames=new[]{packs["home"][6]}.Concat(packs["home"].Skip(8).Take(4)).ToArray();
        cat.climbFrames=packs["climb"];cat.eatFrames=packs["care"].Take(4).ToArray();cat.drinkFrames=packs["care"].Skip(4).Take(4).ToArray();cat.useDetailedHomeAnimations=true;
        cat.homeVisibleHeightRatio=VisibleHeightRatio(root,cat.idleSprite);
        cat.visualHeightMultiplier=catalog.StarterCat.homeVisibleHeightRatio/cat.homeVisibleHeightRatio*(id=="ghost"?.78f:1f);
        cat.walkCycleSeconds=.8f;
        if(id=="ghost"&&File.Exists(root+"/jump.png"))cat.jumpFrames=Import(root,"jump");
        if(id=="ghost"&&File.Exists(root+"/jump-compact.png"))
        {
            cat.jumpFrames=Import(root,"jump-compact");
            // This sheet already includes a tucked airborne pose. Do not insert the
            // legacy action-sheet apex, which has a different scale/foot baseline.
            cat.jumpApexSprite=null;
        }
        if(id=="ghost"&&File.Exists(root+"/portrait.png"))
        {
            var portrait=(TextureImporter)AssetImporter.GetAtPath(root+"/portrait.png");
            if(portrait==null){AssetDatabase.ImportAsset(root+"/portrait.png");portrait=(TextureImporter)AssetImporter.GetAtPath(root+"/portrait.png");}
            if(portrait.textureType!=TextureImporterType.Sprite||portrait.spriteImportMode!=SpriteImportMode.Single||portrait.mipmapEnabled)
            {portrait.textureType=TextureImporterType.Sprite;portrait.spriteImportMode=SpriteImportMode.Single;portrait.alphaIsTransparency=true;portrait.mipmapEnabled=false;portrait.maxTextureSize=2048;portrait.SaveAndReimport();}
            cat.portrait=AssetDatabase.LoadAssetAtPath<Sprite>(root+"/portrait.png");
        }
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(root+"/"+id+".controller")??AnimatorController.CreateAnimatorControllerAtPath(root+"/"+id+".controller");
        var machine=controller.layers[0].stateMachine;
        State(root,machine,"Walk",cat.walkFrames,cat.walkFrames.Length/cat.walkCycleSeconds,true);State(root,machine,"Idle",cat.idleFrames,2,true);State(root,machine,"Jump",cat.jumpFrames.Take(3).ToArray(),12,false);
        State(root,machine,"Crouch",new[]{packs["action"][id=="boza"?9:8]},1,false);State(root,machine,"CrouchWalk",cat.crouchWalkFrames,8,true);
        State(root,machine,"Sit",packs["home"].Skip(4).Take(2).ToArray(),4,false);State(root,machine,"Lie",new[]{packs["home"][6]},1,false);
        State(root,machine,"Sleep",cat.sleepFrames.Skip(1).ToArray(),2,true);State(root,machine,"Eat",cat.eatFrames,8,true);State(root,machine,"Drink",cat.drinkFrames,8,true);State(root,machine,"Climb",cat.climbFrames,24,true);
        machine.defaultState=machine.states.First(s=>s.state.name=="Walk").state;
        cat.animator=controller;catalog.completionFaces=catalog.cats.Select(c=>c.portrait).Where(s=>s!=null).ToArray();
        EditorUtility.SetDirty(cat);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(machine);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
    }
    static void State(string root,AnimatorStateMachine machine,string name,Sprite[] frames,float fps,bool loop)
    {
        string path=root+"/"+name.ToLowerInvariant()+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
        clip.frameRate=fps;var keys=new List<ObjectReferenceKeyframe>();
        for(int i=0;i<frames.Length;i++)keys.Add(new ObjectReferenceKeyframe{time=i/fps,value=frames[i]});
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys.ToArray());
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;if(loop){settings.startTime=0;settings.stopTime=frames.Length/fps;}AnimationUtility.SetAnimationClipSettings(clip,settings);
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);state.motion=clip;EditorUtility.SetDirty(state);EditorUtility.SetDirty(clip);
    }
    static float VisibleHeightRatio(string root,Sprite sprite)
    {
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(root+"/home.png"));var pixels=image.GetPixels32();var rect=sprite.rect;int ear=(int)rect.y;
        for(int y=(int)rect.y;y<(int)rect.yMax;y++)for(int x=(int)(rect.x+rect.width*.67f);x<(int)rect.xMax;x++)if(pixels[y*image.width+x].a>96)ear=Mathf.Max(ear,y);
        UnityEngine.Object.DestroyImmediate(image);return (ear-rect.y+1)/rect.height;
    }
    static Sprite[] Import(string root,string sheet)
    {
        string path=root+"/"+sheet+".png";AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));var pixels=image.GetPixels32();
        bool jumpSheet=sheet=="jump"||sheet=="jump-compact";
        int rowCount=sheet=="walk"||jumpSheet?2:3;
        var rows=new int[rowCount+1];rows[rowCount]=image.height;
        for(int i=1;i<rowCount;i++)rows[i]=Gutter(image,pixels,false,image.height*i/rowCount,image.height/12,0,image.width);
        var metadata=new List<SpriteMetaData>();
        for(int row=0;row<rowCount;row++)
        {
            int bottom=rows[rowCount-1-row],top=rows[rowCount-row];
            // Boza's action rows contain four jumps, five apex/crouch poses and three final poses.
            int count=jumpSheet?2:root.EndsWith("/boza")&&sheet=="action"?(row==1?5:row==2?3:4):4;
            var columns=new int[count+1];columns[count]=image.width;
            for(int i=1;i<count;i++)columns[i]=Gutter(image,pixels,true,image.width*i/count,image.width/20,bottom,top);
            for(int col=0;col<count;col++)
            {
                int left=columns[col],right=columns[col+1],minX=right,maxX=left,minY=top,maxY=bottom;
                for(int y=bottom;y<top;y++)for(int x=left;x<right;x++)if(pixels[y*image.width+x].a>96)
                {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                if(maxY<=minY)throw new Exception("Empty Ghost frame "+sheet+"/"+row+"/"+col);
                minX=Mathf.Max(left,minX-2);maxX=Mathf.Min(right-1,maxX+2);maxY=Mathf.Min(top-1,maxY+2);
                metadata.Add(new SpriteMetaData{name=sheet+"_"+(metadata.Count+1).ToString("00"),rect=new Rect(minX,minY,maxX-minX+1,maxY-minY+1),alignment=(int)SpriteAlignment.Custom,pivot=sheet=="climb"||sheet=="care"?new Vector2(.5f,.5f):new Vector2(.5f,0)});
            }
        }
        float ppu=100;
        if(sheet=="walk"||jumpSheet)
        {
            var idle=AssetDatabase.LoadAllAssetsAtPath(root+"/home.png").OfType<Sprite>().OrderBy(s=>s.name).First();
            var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(root+"/home.png"));
            float idleHeight=BodyHeight(idle.rect,source.GetPixels32(),source.width);
            var heights=metadata.Select(m=>BodyHeight(m.rect,pixels,image.width)).OrderBy(h=>h).ToArray();
            ppu=jumpSheet?100*metadata[0].rect.width/idle.rect.width:100*((heights[(heights.Length-1)/2]+heights[heights.Length/2])*.5f)/idleHeight;
            if(sheet=="walk")
            {
                float referenceHead=HeadCenter(idle.rect,source.GetPixels32(),source.width)-idle.rect.x-idle.pivot.x;
                for(int i=0;i<metadata.Count;i++)
                {var m=metadata[i];float head=HeadCenter(m.rect,pixels,image.width);m.pivot=new Vector2((head-referenceHead*ppu/100-m.rect.x)/m.rect.width,0);metadata[i]=m;}
            }
            UnityEngine.Object.DestroyImmediate(source);
        }
        UnityEngine.Object.DestroyImmediate(image);
        var old=importer.spritesheet;bool changed=importer.spriteImportMode!=SpriteImportMode.Multiple||Mathf.Abs(importer.spritePixelsPerUnit-ppu)>.01f||old.Length!=metadata.Count||old.Where((m,i)=>m.rect!=metadata[i].rect||m.pivot!=metadata[i].pivot||m.name!=metadata[i].name).Any();
        if(changed)
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=ppu;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.spritesheet=metadata.ToArray();importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }
    static float BodyHeight(Rect rect,Color32[] pixels,int width)
    {
        int ear=(int)rect.y;
        for(int y=(int)rect.y;y<(int)rect.yMax;y++)for(int x=(int)(rect.x+rect.width*.67f);x<(int)rect.xMax;x++)if(pixels[y*width+x].a>96)ear=Mathf.Max(ear,y);
        return ear-rect.y+1;
    }
    static float HeadCenter(Rect rect,Color32[] pixels,int width)
    {
        float ear=BodyHeight(rect,pixels,width)+rect.y-1,total=0;int count=0;
        for(int y=(int)(rect.y+(ear-rect.y)*.7f);y<=ear;y++)for(int x=(int)(rect.x+rect.width*.67f);x<rect.xMax;x++)if(pixels[y*width+x].a>96){total+=x;count++;}
        return count>0?total/count:rect.center.x;
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
