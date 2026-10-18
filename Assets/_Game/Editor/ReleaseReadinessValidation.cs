#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
public static class ReleaseReadinessValidation
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            Check(Application.productName=="MitziValidation","Use isolated validation project");
            HomeSettingsValidation.Run();AppIconBuilder.Apply();
            var config=ReleaseConfiguration.Current;
            Check(config!=null && config.developerName=="Mitzi" && config.contactEmail=="melisauyar5225@gmail.com","Developer release configuration missing");
            Check(!config.advertisingEnabled,"Missing production IDs enable ads");
            Check(config.privacyPolicyReviewed && config.privacyPolicyUrl=="https://melisau.github.io/Mitzi/privacy.html","Published privacy URL missing");
            bool blocked=false;
            try{PawPath.Editor.AndroidReleaseBuilder.RequireProductionConfiguration();}catch(BuildFailedException){blocked=true;}
            Check(blocked,"Store build accepted missing production settings");
            ReleaseReadinessGuard.ValidateAndroidConfiguration(false,false);
            foreach(var destination in new[]{new[]{true,false},new[]{false,true}})
            {
                blocked=false;
                try{ReleaseReadinessGuard.ValidateAndroidConfiguration(destination[0],destination[1]);}catch(BuildFailedException){blocked=true;}
                Check(blocked,"Store AAB/export accepted disabled production ads");
            }
            string savedAdUnit=config.androidRewardedAdUnitId;
            try
            {
                config.advertisingEnabled=true;config.androidRewardedAdUnitId="";blocked=false;
                try{ReleaseReadinessGuard.ValidateAndroidConfiguration(false,false);}catch(BuildFailedException){blocked=true;}
                Check(blocked,"Live-ad APK accepted missing ad unit ID");
            }
            finally{config.advertisingEnabled=false;config.androidRewardedAdUnitId=savedAdUnit;}
            Debug.Log("ANDROID_APK_STORE_GUARD_VALIDATION_PASS");
            var flow=GameFlow.Instance;var buttons=flow.GetComponentsInChildren<Button>(true);
            buttons.First(b=>b.name=="SettingsButton").onClick.Invoke();buttons.First(b=>b.name=="PrivacyPolicyButton").onClick.Invoke();
            var close=buttons.First(b=>b.name=="ClosePrivacyPolicy");
            Check(close.transform.parent.gameObject.activeSelf,"Privacy screen did not open");
            Canvas.ForceUpdateCanvases();
            var text=close.transform.parent.GetComponentsInChildren<Text>().First(t=>t.name=="PolicyText");
            Check(text.text.Contains(config.contactEmail)&&text.text.Contains("Google")&&text.text.Contains("SİLME"),"Privacy disclosure incomplete");
            var scroll=close.transform.parent.GetComponent<ScrollRect>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            Check(scroll.content.rect.height>scroll.viewport.rect.height,"Long privacy policy is not scrollable");
            var icons=PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            Check(icons.Length>0&&AssetDatabase.GetAssetPath(icons[0]).EndsWith("paw_icon.png"),"Paw app icon missing");
            foreach(var canvas in flow.GetComponentsInChildren<Canvas>(true))canvas.sortingOrder=200;
            var cam=Camera.main;cam.aspect=16f/9f;Canvas.ForceUpdateCanvases();
            var target=new RenderTexture(1280,720,24);cam.targetTexture=target;cam.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            Directory.CreateDirectory("AnimalArtSources-20261018");File.WriteAllBytes("AnimalArtSources-20261018/privacy-screen-preview.png",image.EncodeToPNG());
            RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
            close.onClick.Invoke();Check(!close.transform.parent.gameObject.activeSelf,"Privacy close failed");
            Debug.Log("RELEASE_READINESS_PRIVACY_ICON_VALIDATION_PASS");
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
#endif
