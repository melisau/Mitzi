using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PawPath.Core;
using PawPath.Data;
using PawPath.Localization;
using PawPath.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MitziPhoneValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Bootstrap(string name, params object[] args) => typeof(RuntimeBootstrap).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,args);
    static object Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,Hidden).Invoke(obj,args);
    static void Check(bool ok,string why) { if(!ok) throw new Exception(why); }
    public static void Run()
    {
        Check(Application.productName=="MitziValidation","Isolated project required");
        var oldLanguage = GameText.Language;
        try
        {
            MitziPhonePhotoImporter.Apply();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var catalog=AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var flow=new GameObject("Flow").AddComponent<GameFlow>();Call(flow,"Awake");flow.BindCatalog(catalog);
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=4.45f;camera.transform.position=new Vector3(0,0,-10);
            var background=new GameObject("HomeBackground").AddComponent<SpriteRenderer>();background.sprite=catalog.homeBackground;background.sortingOrder=-5;
            var uri=new Uri(MitziPhoneUI.InstagramUrl);
            Check(uri.Scheme=="https" && uri.Host=="www.instagram.com" && uri.AbsolutePath=="/egyptianmaumitzi/","Wrong real account URL");
            string folder=Environment.GetEnvironmentVariable("MITZI_LEVEL30_OUTPUT")??Path.Combine(Path.GetTempPath(),"mitzi-level30");Directory.CreateDirectory(folder);
            int screens=0;
            foreach(GameLanguage language in Enum.GetValues(typeof(GameLanguage)))
            {
                typeof(GameText).GetField("language",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,language);
                typeof(GameText).GetField("loaded",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);
                var canvasRoot=(GameObject)Bootstrap("CreateCanvas",flow.transform);
                var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=200;
                var hud=(GameObject)Bootstrap("BuildHud",canvasRoot.transform,null);
                var phone=hud.GetComponent<MitziPhoneUI>();Call(phone,"OnEnable");
                var launcher=hud.GetComponentsInChildren<Button>(true).Single(b=>b.name=="MitziPhone");
                Check(!phone.IsOpen && !phone.AlbumLoaded && launcher.gameObject.activeSelf,"Phone or photos loaded automatically");launcher.onClick.Invoke();Check(phone.IsOpen && phone.AlbumLoaded,"Phone icon does not open album");
                var panel=canvasRoot.transform.Find("MitziPhoneOverlay");
                var photos=panel.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Photo_")).ToArray();
                Check(photos.Length==4 && photos.All(i=>i.sprite!=null) && photos.Select(i=>i.sprite).Distinct().Count()==4,"Four real photos missing");
                var avatar=panel.GetComponentsInChildren<Image>().Single(i=>i.name=="MitziAvatar");
                Check(avatar.sprite!=null && avatar.sprite.name=="mitzi_profile" && !photos.Any(i=>i.sprite==avatar.sprite),"Dedicated user profile photo missing");
                Check(photos.Select(i=>i.sprite.texture).Distinct().Count()==1 && photos[0].sprite.texture.width<=1024 && photos[0].sprite.texture.mipmapCount==1,"Album not a compact shared atlas");
                Debug.Log("MITZI_ALBUM_MEMORY textureBytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(photos[0].sprite.texture));
                var originalPhotos=photos.Select(i=>i.sprite).ToArray();
                Check(panel.GetComponentsInChildren<Text>().Any(t=>t.text=="Mitzi Tosuncuk")&&panel.GetComponentsInChildren<Text>().Any(t=>t.text=="@egyptianmaumitzi"),"Wrong profile identity");
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(2340,1080),new Vector2Int(1600,1200)})
                {
                    camera.aspect=(float)size.x/size.y;background.transform.localScale=Vector3.one*Mathf.Max(8.9f/background.sprite.bounds.size.y,8.9f*camera.aspect/background.sprite.bounds.size.x);
                    var rt=new RenderTexture(size.x,size.y,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();
                    var device=panel.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="PhoneDevice");
                    var corners=new Vector3[4];device.GetWorldCorners(corners);
                    foreach(var point in corners){var p=camera.WorldToViewportPoint(point);Check(p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1,"Phone escapes screen "+size);}
                    foreach(var text in panel.GetComponentsInChildren<Text>())Check(text.preferredHeight<=text.rectTransform.rect.height+1,"Phone text overflows "+language+" "+text.name);
                    var badge=hud.GetComponentsInChildren<RectTransform>(true).Single(t=>t.name=="NextCatGoal");
                    var phoneBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRoot.transform,launcher.transform);
                    var badgeBounds=RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRoot.transform,badge);
                    Check(phoneBounds.min.x>badgeBounds.max.x,"Phone icon covers cat goal");
                    if(language==GameLanguage.Turkish&&size.x==1280)
                    {
                        RenderTexture.active=rt;var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                        File.WriteAllBytes(Path.Combine(folder,"mitzi-phone-profile.png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=null;
                    }
                    camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);screens++;
                }
                panel.GetComponentsInChildren<Button>().Single(b=>b.name=="ClosePhone").onClick.Invoke();Check(!phone.IsOpen,"Close failed");
                if(language==GameLanguage.Turkish) Capture(camera,folder,"mitzi-phone-launcher",1280,720);
                phone.Open();Check(photos.Select(i=>i.sprite).SequenceEqual(originalPhotos),"Reopen replaces/reloads album sprites");
                var openInstagram=panel.GetComponentsInChildren<Button>(true).Single(b=>b.name=="OpenMitziInstagram");
                openInstagram.onClick.Invoke();Check(phone.IsParentGateOpen && !phone.ParentApproved,"External button bypasses parent gate");
                var answer=panel.GetComponentsInChildren<InputField>().Single();answer.text="0";
                Check(!phone.SubmitParentAnswer() && !phone.ParentApproved,"Incorrect answer accepted");
                var challenge=panel.GetComponentsInChildren<Text>().Single(t=>t.name=="ParentChallenge");
                var values=System.Text.RegularExpressions.Regex.Matches(challenge.text,@"\d+").Cast<System.Text.RegularExpressions.Match>().Select(m=>int.Parse(m.Value)).ToArray();
                Check(values.Length==2 && values.All(v=>v>=12),"Adult challenge too simple or malformed");answer.text=(values[0]*values[1]).ToString();
                Check(phone.SubmitParentAnswer() && phone.ParentApproved && phone.IsParentGateOpen,"Correct answer did not unlock explicit continue");
                foreach(var text in panel.GetComponentsInChildren<Text>())Check(text.preferredHeight<=text.rectTransform.rect.height+1,"Parent gate text overflows "+language+" "+text.name);
                panel.GetComponentsInChildren<Button>().Single(b=>b.name=="CancelParent").onClick.Invoke();
                Check(!phone.ParentApproved && !phone.IsParentGateOpen,"Cancel retains external-link approval");
                phone.OpenInstagram();Check(!phone.ParentApproved && phone.IsParentGateOpen,"Approval reused");phone.Close();
                Check(!phone.ParentApproved && !phone.IsParentGateOpen && !phone.SubmitParentAnswer(),"Closed phone permits approval");
                foreach(var state in new[]{GameFlowState.Gameplay,GameFlowState.DrawingPreparation,GameFlowState.Pause})
                {
                    phone.Open();phone.OpenInstagram();Call(flow,"TransitionTo",state);Check(!phone.IsOpen&&!phone.IsParentGateOpen&&!phone.ParentApproved&&!launcher.gameObject.activeSelf,"Phone covers gameplay or retains parent approval");
                    Call(flow,"TransitionTo",GameFlowState.MainMenu);Check(launcher.gameObject.activeSelf,"Phone lost on return home");
                }
                UnityEngine.Object.DestroyImmediate(canvasRoot);
            }
            Debug.Log($"MITZI_PHONE_PASS localizedScreens={screens} account=egyptianmaumitzi realPhotos=4 sharedAtlasMax=1024 lazyLoad=true parentGate=wrong-correct-cancel-close-state-passed externalUrl=validated-not-launched");
        }
        finally
        {
            typeof(GameText).GetField("language",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,oldLanguage);
            Time.timeScale=1;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
    static void Capture(Camera camera,string folder,string name,int width,int height)
    {
        var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
    }
}
