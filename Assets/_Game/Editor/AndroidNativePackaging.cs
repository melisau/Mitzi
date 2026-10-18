#if UNITY_ANDROID && UNITY_EDITOR
using System.IO;
using UnityEditor.Android;
public sealed class AndroidNativePackaging : IPostGenerateGradleAndroidProject
{
    public int callbackOrder=>999;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        foreach(string file in new[]{Path.Combine(path,"build.gradle"),Path.Combine(path,"..","launcher","build.gradle")})
        {
            if(!File.Exists(file))continue;
            var text=File.ReadAllText(file);
            if(!text.Contains("MITZI_NATIVE_EXTRACTION"))File.AppendAllText(file,"\n// MITZI_NATIVE_EXTRACTION: compressed libs avoid 4 KB ZIP alignment on AGP 7.4.\nandroid { packagingOptions { jniLibs { useLegacyPackaging = true } } }\n");
        }
    }
}
#endif
