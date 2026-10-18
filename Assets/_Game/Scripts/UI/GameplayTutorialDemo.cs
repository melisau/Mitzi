using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.UI
{
    public enum TutorialDemoKind { DrawPath, DrawTools, DrawViews, Move, Jump, Crouch, Climb, DogStomp, DogContact, BirdReward }

    /// <summary>A paused-game-safe miniature showing the action and its result.</summary>
    public sealed class GameplayTutorialDemo : MonoBehaviour
    {
        public TutorialDemoKind Kind { get; private set; }
        public Vector2 CatPosition => cat.rectTransform.anchoredPosition;
        public int VisiblePathSegments { get; private set; }
        CatDefinition definition;
        Image cat, finger, tapRing, dog, bird, feedback;
        Text reward;
        public bool DogVisible => dog != null && dog.gameObject.activeSelf;
        public bool BirdVisible => bird != null && bird.gameObject.activeSelf;
        public bool RewardVisible => reward != null && reward.gameObject.activeSelf;
        Image[] path;
        Image[] controls;
        Image[] viewDots;
        float started;
        float catSize;
        static readonly Color Brown = new Color(.66f,.49f,.37f);
        static readonly Color Blue = new Color(.36f,.63f,.90f);
        static readonly Color Green = new Color(.45f,.72f,.38f);
        static readonly Color Pink = new Color(.90f,.42f,.58f);
        static readonly Color Ice = new Color(.40f,.70f,.75f);
        const float Floor = -35f;

        public void Configure(TutorialDemoKind kind, CatDefinition catDefinition = null)
        {
            Kind=kind;
            definition=catDefinition ?? GameFlow.Instance?.Catalog?.StarterCat;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            var rect=GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(850f,330f);
            var background=GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            background.sprite=CozyIconFactory.Pill(); background.type=Image.Type.Sliced;
            background.color=new Color(.85f,.93f,.89f); background.raycastTarget=false;
            if(GetComponent<RectMask2D>()==null) gameObject.AddComponent<RectMask2D>();
            Art("Cloud",CozyIconFactory.Cloud(),new Vector2(-280f,118f),new Vector2(110f,55f),new Color(1f,1f,1f,.65f));
            Art("Cloud",CozyIconFactory.Cloud(),new Vector2(270f,125f),new Vector2(150f,60f),new Color(1f,1f,1f,.5f));
            bool drawing=kind==TutorialDemoKind.DrawPath || kind==TutorialDemoKind.DrawTools || kind==TutorialDemoKind.DrawViews;
            if(drawing)
            {
                Platform(-295f,260f,Floor); Platform(295f,260f,Floor);
            }
            else Platform(0f,820f,kind==TutorialDemoKind.Climb ? -80f : kind==TutorialDemoKind.Jump ? -60f : Floor);
            path=new Image[64];
            for(int i=0;i<path.Length;i++)
            {
                Vector2 a=PathPoint(i/(float)path.Length), b=PathPoint((i+1f)/path.Length);
                path[i]=Art("Path_"+i,CozyIconFactory.Dot(),(a+b)*.5f,new Vector2(Vector2.Distance(a,b)+10f,12f),Brown);
                path[i].rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
            }
            if(kind==TutorialDemoKind.Crouch)
            {
                var catalog=GameFlow.Instance?.Catalog;
                int theme=ThemeSelectionUI.GetSelectedTheme(PawPath.Core.SaveService.Data.highestCompletedLevel+1);
                var obstacle=catalog!=null ? catalog.CrouchObstacleForTheme(theme):null;
                if(obstacle!=null)
                {
                    float height=58f/PawPath.Levels.CrouchPassage.ArtUndersideFraction(obstacle);
                    var beam=Art("CrouchObstacle",obstacle,new Vector2(0,Floor-4f),new Vector2(320,height),Color.white);
                    beam.rectTransform.pivot=new Vector2(.5f,0);
                    var upper=catalog.CrouchUpperForTheme(theme);
                    if(upper!=null)
                    {
                        var crop=new GameObject("UpperSceneryCrop",typeof(RectTransform),typeof(RectMask2D));crop.transform.SetParent(transform,false);
                        var cropRect=crop.GetComponent<RectTransform>();cropRect.anchoredPosition=new Vector2(0,93f);cropRect.sizeDelta=new Vector2(330,140);
                        var pile=Art("VisibleUpperObstruction",upper,new Vector2(theme==1 ? 15f:0f,-70f),new Vector2(240f*upper.bounds.size.x/upper.bounds.size.y,240f),Color.white,crop.transform);
                        pile.rectTransform.pivot=new Vector2(.5f,0);
                    }
                }
            }
            if(kind==TutorialDemoKind.Jump)
            {
                var mound=Art("RealMound",GameFlow.Instance?.Catalog?.moundSprite,new Vector2(0,-60-28.05f),new Vector2(200,100),Color.white);
                mound.rectTransform.pivot=new Vector2(.5f,0f);
            }
            if(kind==TutorialDemoKind.Climb)
            {
                var tree=Art("Tree",GameFlow.Instance?.Catalog?.climbTreeSprite ?? CozyIconFactory.Tree(),new Vector2(15,-80),new Vector2(290,245),Green);
                tree.rectTransform.pivot=new Vector2(.5f,0); tree.preserveAspect=true;
                if(GameFlow.Instance?.Catalog?.climbTreeSprite!=null) tree.color=Color.white;
            }
            bool encounter=kind==TutorialDemoKind.DogStomp || kind==TutorialDemoKind.DogContact || kind==TutorialDemoKind.BirdReward;
            catSize=kind==TutorialDemoKind.Climb ? 170f : encounter ? 180f : kind==TutorialDemoKind.Jump || kind==TutorialDemoKind.DrawTools ? 205f : 230f;
            dog=Art("DemoDog",GameFlow.Instance?.Catalog?.dogIdleSprite,new Vector2(75,Floor),new Vector2(190,190),Color.white);
            dog.rectTransform.pivot=new Vector2(.5f,.08f); dog.preserveAspect=true;
            bird=Art("DemoBird",Frame(GameFlow.Instance?.Catalog?.birdFrames,0,24),new Vector2(90,55),new Vector2(100,100),Color.white); bird.preserveAspect=true;
            feedback=Art("Result",CozyIconFactory.Star(),new Vector2(90,45),Vector2.one*60,new Color(1f,.76f,.24f));
            var rewardObject=new GameObject("BirdPoints",typeof(RectTransform),typeof(Text)); rewardObject.transform.SetParent(transform,false);
            reward=rewardObject.GetComponent<Text>(); reward.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); reward.fontSize=38; reward.fontStyle=FontStyle.Bold;
            reward.alignment=TextAnchor.MiddleCenter; reward.color=Green; reward.text="+3"; reward.raycastTarget=false;
            reward.rectTransform.sizeDelta=new Vector2(130,60); reward.rectTransform.anchoredPosition=new Vector2(90,95);
            cat=Art("Cat",definition?.idleSprite ?? CozyIconFactory.HappyCatFace(),Vector2.zero,Vector2.one*catSize,Color.white);
            cat.rectTransform.pivot=new Vector2(.5f,.08f); cat.preserveAspect=true;
            viewDots=new Image[2];
            for(int i=0;i<2;i++) viewDots[i]=Art("ViewDot_"+i,CozyIconFactory.Dot(),new Vector2(i==0 ? -13 : 13,139),Vector2.one*13,Green);
            controls=new Image[4];
            for(int i=0;i<controls.Length;i++)
            {
                controls[i]=Art("DemoControl_"+i,CozyIconFactory.CircleButton(),Vector2.zero,Vector2.one*66,Blue);
                var icon=Art("Icon",CozyIconFactory.ArrowRight(),Vector2.zero,Vector2.one*35,Color.white,controls[i].transform);
            }
            tapRing=Art("TouchRing",CozyIconFactory.Ring(),Vector2.zero,Vector2.one*80,new Color(1f,.83f,.36f));
            finger=Art("Finger",CozyIconFactory.TouchFinger(),Vector2.zero,Vector2.one*55,new Color(1f,.77f,.60f));
            finger.rectTransform.pivot=new Vector2(.5f,.92f);
            var shadow=finger.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(.3f,.2f,.14f,.55f); shadow.effectDistance=new Vector2(2,-2);
            started=Time.unscaledTime;
            RenderAt(0f);
        }

        void Update() => RenderAt(Time.unscaledTime-started);

        public void RenderAt(float elapsed)
        {
            if(cat==null) return;
            foreach(var control in controls) { control.gameObject.SetActive(false); control.rectTransform.localScale=Vector3.one; }
            foreach(var dot in viewDots) dot.gameObject.SetActive(Kind==TutorialDemoKind.DrawViews);
            finger.gameObject.SetActive(false); tapRing.gameObject.SetActive(false);
            cat.rectTransform.localScale=Vector3.one; cat.color=Color.white;
            dog.gameObject.SetActive(false); bird.gameObject.SetActive(false); feedback.gameObject.SetActive(false); reward.gameObject.SetActive(false);
            ShowPath(0,Brown);
            if(Kind==TutorialDemoKind.DrawPath || Kind==TutorialDemoKind.DrawViews) Draw(elapsed);
            else if(Kind==TutorialDemoKind.DrawTools) Tools(elapsed);
            else if(Kind==TutorialDemoKind.Move) Move(elapsed);
            else if(Kind==TutorialDemoKind.Jump) Jump(elapsed);
            else if(Kind==TutorialDemoKind.Crouch) Crouch(elapsed);
            else if(Kind==TutorialDemoKind.Climb) Climb(elapsed);
            else Encounter(elapsed);
        }

        void Draw(float elapsed)
        {
            bool views=Kind==TutorialDemoKind.DrawViews;
            float t=elapsed%(views ? 9f : 6f);
            Control(0,CozyIconFactory.Play(),Green,new Vector2(310,-112));
            if(views) Control(1,CozyIconFactory.ArrowRight(),Blue,new Vector2(215,-112));
            float drawStart=views && t>=3f ? 3f : .5f;
            float drawEnd=drawStart+1.9f;
            float playStart=views ? 5.2f : 2.7f;
            float walkStart=playStart+.5f;
            float progress=Mathf.Clamp01((t-drawStart)/1.9f);
            if(views && t>=2.4f && t<3f) progress=1f;
            ShowPath(progress,Brown);
            if(t>=drawStart && t<drawEnd) Touch(PathPoint(progress),false);
            if(views && t>=2.4f && t<3f) Press(1,t-2.4f);
            if(t>=playStart && t<walkStart) Press(0,t-playStart);
            float walk=Mathf.Clamp01((t-walkStart)/2.4f);
            float x=Mathf.Lerp(-300,300,walk);
            float y=Mathf.Abs(x)<170 ? PathPoint((x+170)/340).y : Floor;
            Pose(x,y,walk>0 && walk<1 ? Frame(definition?.walkFrames,t,24) : definition?.idleSprite);
            for(int i=0;i<2;i++) viewDots[i].color=new Color(Green.r,Green.g,Green.b,(views && (t>=3 ? i==1 : i==0)) ? 1f:.25f);
        }

        void Tools(float elapsed)
        {
            float t=elapsed%9f; int phase=Mathf.FloorToInt(t/3f); float local=t-phase*3f;
            Control(0,CozyIconFactory.ArrowUp(),Blue,new Vector2(-100,-112));
            Control(1,CozyIconFactory.Snowflake(),Ice,new Vector2(0,-112));
            Control(2,CozyIconFactory.Eraser(),Pink,new Vector2(100,-112));
            if(local<.55f) Press(phase,local);
            float travel=Mathf.Clamp01((local-.55f)/1.9f);
            ShowPath(phase==2 ? 1-travel : 1,phase==0 ? Blue : Ice);
            if(phase==2)
            {
                Touch(PathPoint(1-travel),false);
                Pose(-295,Floor,definition?.idleSprite);
            }
            else
            {
                float x=Mathf.Lerp(-295,295,travel);
                float y=Mathf.Abs(x)<170 ? PathPoint((x+170)/340).y : Floor;
                if(phase==0) y+=Mathf.Sin(travel*Mathf.PI)*60f;
                Pose(x,y,phase==0 ? JumpPose(travel) : definition?.idleSprite);
            }
        }

        void Move(float elapsed)
        {
            float t=elapsed%6f; bool right=t<3f; float progress=(t%3f)/3f;
            Control(0,CozyIconFactory.ArrowLeft(),Brown,new Vector2(-300,-112));
            Control(1,CozyIconFactory.ArrowRight(),Blue,new Vector2(300,-112));
            Press(right ? 1 : 0,t%1f);
            Pose(Mathf.Lerp(right ? -280 : 280,right ? 280 : -280,progress),Floor,Frame(definition?.walkFrames,t,24),right);
        }

        void Jump(float elapsed)
        {
            float t=elapsed%7f; bool high=t>=3.4f; float start=high ? 3.4f:.4f;
            float local=t-start; float duration=high ? 2.3f:1.8f;
            Control(0,CozyIconFactory.ArrowUp(),Blue,new Vector2(0,-112));
            if(local>=0 && local<.35f) Press(0,local);
            if(high && local>=.35f && local<.7f) Press(0,local-.35f);
            for(int i=0;i<2;i++)
            {
                viewDots[i].gameObject.SetActive(i==0 || high);
                viewDots[i].rectTransform.anchoredPosition=new Vector2(i==0 ? -60:60,-112);
                viewDots[i].color=Green;
            }
            float progress=Mathf.Clamp01((local-.15f)/duration);
            float height=(high ? 82f:47f)*Mathf.Sin(progress*Mathf.PI);
            Pose(Mathf.Lerp(-240,240,progress),-60+height,progress>0 && progress<1 ? JumpPose(progress) : definition?.idleSprite);
        }

        void Crouch(float elapsed)
        {
            float t=elapsed%5.5f; float progress=Mathf.Clamp01((t-.5f)/4f);
            Control(0,CozyIconFactory.ArrowDown(),Blue,new Vector2(300,-112));
            float x=Mathf.Lerp(-300,300,progress); bool low=x>-245 && x<245;
            if(low) Press(0,t%1f);
            Pose(x,Floor,low ? Frame(definition?.crouchWalkFrames,t,8) ?? Frame(definition?.jumpFrames,0,1) : Frame(definition?.walkFrames,t,24));
        }

        void Climb(float elapsed)
        {
            float t=elapsed%6f;
            Control(0,CozyIconFactory.ArrowUp(),Blue,new Vector2(300,-112));
            if(t<.65f) Press(0,t);
            float rise=Mathf.Clamp01((t-.65f)/2.4f);
            Pose(-35,Mathf.Lerp(-80,5,rise),t<3.1f ? Frame(definition?.climbFrames,t,24) : definition?.idleSprite);
        }

        void Encounter(float elapsed)
        {
            float t=elapsed%6f;
            if(Kind==TutorialDemoKind.DogStomp)
            {
                Control(0,CozyIconFactory.ArrowUp(),Blue,new Vector2(-280,-112));
                if(t<.5f) Press(0,t);
                dog.gameObject.SetActive(t<2.5f);
                dog.color=new Color(1,1,1,Mathf.Clamp01((2.5f-t)/.3f));
                dog.rectTransform.localScale=new Vector3(-1,1,1)*(t<2.2f ? 1f:Mathf.Max(.1f,1-(t-2.2f)*2));
                float x,y;
                if(t<1.6f) { float u=Mathf.Clamp01((t-.5f)/1.1f); x=Mathf.Lerp(-230,75,u); y=Floor+Mathf.Sin(u*Mathf.PI*.5f)*85; }
                else if(t<2.2f) { x=75; y=Floor+Mathf.Lerp(85,52,(t-1.6f)/.6f); }
                else { float u=Mathf.Clamp01((t-2.2f)/1.6f); x=Mathf.Lerp(75,280,u); y=Floor+Mathf.Lerp(52,0,u)+Mathf.Sin(u*Mathf.PI)*50; }
                Pose(x,y,t>.5f && t<3.8f ? Frame(definition?.jumpFrames,t,3):definition?.idleSprite);
                feedback.sprite=CozyIconFactory.Star(); feedback.color=new Color(1,.76f,.24f);
                feedback.gameObject.SetActive(t>=2.2f && t<3.2f);
                feedback.rectTransform.anchoredPosition=new Vector2(75,Floor+45+(t-2.2f)*35);
            }
            else if(Kind==TutorialDemoKind.DogContact)
            {
                dog.gameObject.SetActive(true); dog.color=Color.white; dog.rectTransform.localScale=new Vector3(-1,1,1);
                float u=Mathf.Clamp01(t/1.8f);
                Pose(Mathf.Lerp(-260,-30,u)+(t>1.8f && t<2.3f ? Mathf.Sin(t*55)*4:0),Floor,u<1 ? Frame(definition?.walkFrames,t,24):definition?.idleSprite);
                Control(0,u<1 ? CozyIconFactory.ArrowRight():CozyIconFactory.ArrowLeft(),u<1 ? Blue:Pink,new Vector2(-280,-112));
                if(u<1) Press(0,t%1f);
                feedback.sprite=CozyIconFactory.Close(); feedback.color=Pink; feedback.rectTransform.anchoredPosition=new Vector2(5,110);
                feedback.gameObject.SetActive(t>=1.8f);
            }
            else
            {
                float u=Mathf.Clamp01((t-.5f)/2f); bool collected=t>=2.15f;
                bird.gameObject.SetActive(!collected);
                bird.sprite=Frame(GameFlow.Instance?.Catalog?.birdFrames,t,24); bird.rectTransform.localScale=new Vector3(-1,1,1);
                bird.rectTransform.anchoredPosition=new Vector2(90,55+Mathf.Sin(t*4)*5);
                float y=Floor+Mathf.Sin(u*Mathf.PI)*95;
                Pose(Mathf.Lerp(-240,90,u),y,u>0 && u<1 ? JumpPose(u):definition?.idleSprite);
                reward.gameObject.SetActive(collected); reward.rectTransform.anchoredPosition=new Vector2(90,85+Mathf.Min(25,(t-2.15f)*15));
                feedback.sprite=CozyIconFactory.Star(); feedback.color=new Color(1,.76f,.24f); feedback.rectTransform.anchoredPosition=new Vector2(40,100); feedback.gameObject.SetActive(collected);
                Control(0,CozyIconFactory.ArrowUp(),Blue,new Vector2(-280,-112)); if(t<.5f) Press(0,t);
            }
        }

        void Pose(float x,float y,Sprite sprite,bool right=true)
        {
            cat.sprite=sprite ?? definition?.idleSprite ?? CozyIconFactory.HappyCatFace();
            cat.rectTransform.pivot=new Vector2(.5f,definition!=null && cat.sprite==definition.jumpApexSprite ? .22f:.08f);
            cat.rectTransform.sizeDelta=Vector2.one*(catSize*100f/cat.sprite.pixelsPerUnit);
            cat.rectTransform.anchoredPosition=new Vector2(x,y);
            cat.rectTransform.localScale=new Vector3(right ? 1:-1,1,1);
        }
        Sprite JumpPose(float progress) => definition!=null && definition.jumpApexSprite!=null && Mathf.Abs(progress-.5f)<.13f
            ? definition.jumpApexSprite : Frame(definition?.jumpFrames,progress,3);
        Sprite Frame(Sprite[] frames,float time,float fps) => frames!=null && frames.Length>0 ? frames[Mathf.FloorToInt(time*fps)%frames.Length] : definition?.idleSprite;
        Vector2 PathPoint(float t) => new Vector2(Mathf.Lerp(-170,170,t),Floor+Mathf.Sin(t*Mathf.PI)*25f);
        void ShowPath(float progress,Color color)
        {
            VisiblePathSegments=Mathf.Clamp(Mathf.FloorToInt(progress*path.Length),0,path.Length);
            for(int i=0;i<path.Length;i++) { path[i].gameObject.SetActive(i<VisiblePathSegments); path[i].color=color; }
        }
        void Platform(float x,float width,float top)
        {
            var road=GameFlow.Instance?.Catalog?.roadPlatformSprite;
            int count=Mathf.Max(1,Mathf.CeilToInt(width/160f));
            float tileWidth=width/count;
            for(int i=0;i<count;i++)
            {
                var image=Art("Platform_"+i,road ?? CozyIconFactory.Pill(),new Vector2(x-width*.5f+tileWidth*(i+.5f),top-30),new Vector2(tileWidth+.5f,60),road!=null ? Color.white:Brown);
                if(road==null) image.type=Image.Type.Sliced;
            }
        }
        void Control(int index,Sprite sprite,Color color,Vector2 position)
        {
            var control=controls[index]; control.gameObject.SetActive(true); control.color=color;
            control.rectTransform.anchoredPosition=position;
            control.transform.GetChild(0).GetComponent<Image>().sprite=sprite;
        }
        void Press(int index,float phase)
        {
            var control=controls[index]; control.rectTransform.localScale=Vector3.one*(.94f+.03f*Mathf.Sin(phase*12));
            Touch(control.rectTransform.anchoredPosition,true);
            tapRing.rectTransform.localScale=Vector3.one*(1f+.12f*Mathf.Sin(phase*8));
        }
        void Touch(Vector2 point,bool tap)
        {
            finger.gameObject.SetActive(true); finger.rectTransform.anchoredPosition=point;
            tapRing.gameObject.SetActive(tap); tapRing.rectTransform.anchoredPosition=point;
        }
        Image Art(string name,Sprite sprite,Vector2 position,Vector2 size,Color color,Transform parent=null)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent ?? transform,false);
            var image=go.GetComponent<Image>(); image.sprite=sprite; image.color=color; image.raycastTarget=false;
            image.rectTransform.anchoredPosition=position; image.rectTransform.sizeDelta=size; return image;
        }
    }
}
