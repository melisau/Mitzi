using System.IO;
using PawPath.Data;
using UnityEngine;

namespace PawPath.Cat
{
    internal static class GhostWalkMotion
    {
        static Texture2D[] flows;
        static Vector4 bounds;
        static Vector4[] poses;
        public static bool Apply(Material material,CatDefinition cat,int first,int second)
        {
            if(flows==null)Load();
            if(flows==null||first<0||second!=(first+1)%flows.Length||cat.walkFrames.Length!=flows.Length)return false;
            for(int i=0;i<poses.Length;i++)
            {var b=cat.walkFrames[i].bounds;var actual=new Vector4(b.min.x,b.min.y,b.size.x,b.size.y);if((actual-poses[i]).sqrMagnitude>.00001f)return false;}
            material.SetTexture("_Motion",flows[first]);material.SetVector("_MotionBounds",bounds);return true;
        }
        static void Load()
        {
            var data=Resources.Load<TextAsset>("Motion/GhostWalk");if(data==null)return;
            using(var reader=new BinaryReader(new MemoryStream(data.bytes)))
            {
                if(new string(reader.ReadChars(4))!="GWF1")return;
                int width=reader.ReadInt32(),height=reader.ReadInt32(),count=reader.ReadInt32();
                if(width!=192||height!=128||count!=8)return;
                bounds=Vector(reader);poses=new Vector4[count];for(int i=0;i<count;i++)poses[i]=Vector(reader);
                var textures=new Texture2D[count];var format=SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf)?TextureFormat.RGBAHalf:TextureFormat.RGBAFloat;
                for(int i=0;i<count;i++)
                {
                    var colors=new Color[width*height];for(int p=0;p<colors.Length;p++)colors[p]=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                    var texture=new Texture2D(width,height,format,false,true){name="Ghost stride motion "+i,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                    texture.SetPixels(colors);texture.Apply(false,true);textures[i]=texture;
                }
                flows=textures;
            }
        }
        static Vector4 Vector(BinaryReader reader)=>new Vector4(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
    }
}
