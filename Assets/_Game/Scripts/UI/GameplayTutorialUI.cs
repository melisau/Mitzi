using System;
using UnityEngine;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.Gameplay;
using PawPath.Localization;

namespace PawPath.UI
{
    /// <summary>Separate first-play lessons; only the user's close action marks them seen.</summary>
    public sealed class GameplayTutorialUI : MonoBehaviour
    {
        public const string DrawingSeenKey = "PawPath.AnimatedTutorial.Drawing.v4.Trees";
        public const string DirectSeenKey = "PawPath.AnimatedTutorial.Direct.v4.Trees";
        public GameplayTutorialDemo Demo { get; private set; }
        public int PageIndex { get; private set; }
        public int PageCount => mode==GameplayPlayMode.Drawing ? 8:7;
        Text title, caption;
        Button previous, next, finish;
        Image[] dots;
        GameplayPlayMode mode;
        Action dismissed;
        bool open;
        static readonly Color Brown=new Color(.66f,.49f,.37f);
        static readonly Color Blue=new Color(.36f,.63f,.90f);
        static readonly Color Green=new Color(.45f,.72f,.38f);

        public static bool HasSeen(GameplayPlayMode value) => PlayerPrefs.GetInt(value==GameplayPlayMode.Drawing ? DrawingSeenKey:DirectSeenKey,0)==1;

        public void Build()
        {
            var overlay=gameObject.AddComponent<Image>(); overlay.color=new Color(.10f,.07f,.05f,.82f); overlay.raycastTarget=true;
            var card=new GameObject("LessonCard",typeof(RectTransform),typeof(Image)); card.transform.SetParent(transform,false);
            var rect=card.GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(1180,800);
            var image=card.GetComponent<Image>(); image.sprite=CozyIconFactory.Pill(); image.type=Image.Type.Sliced; image.color=new Color(1f,.97f,.90f);
            var shadow=card.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.3f); shadow.effectDistance=new Vector2(0,-10);
            title=Label(card.transform,"Title",new Vector2(0,315),new Vector2(850,65),42);
            title.fontStyle=FontStyle.Bold;
            caption=Label(card.transform,"Caption",new Vector2(0,-205),new Vector2(1000,90),28);
            var stage=new GameObject("AnimatedExample",typeof(RectTransform)); stage.transform.SetParent(card.transform,false);
            stage.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,45);
            Demo=stage.AddComponent<GameplayTutorialDemo>();
            dots=new Image[8];
            for(int i=0;i<dots.Length;i++) dots[i]=Icon(card.transform,"PageDot_"+i,CozyIconFactory.Dot(),new Vector2(0,-265),18,Green);
            previous=Button(card.transform,"PreviousLesson",CozyIconFactory.ArrowLeft(),new Vector2(-420,-315),92,Brown);
            next=Button(card.transform,"NextLesson",CozyIconFactory.ArrowRight(),new Vector2(420,-315),92,Blue);
            finish=Button(card.transform,"TryIt",CozyIconFactory.Play(),new Vector2(0,-315),110,Green);
            var close=Button(card.transform,"CloseLesson",CozyIconFactory.Close(),new Vector2(515,315),64,Brown);
            previous.onClick.AddListener(()=>ShowPage(PageIndex-1)); next.onClick.AddListener(()=>ShowPage(PageIndex+1));
            finish.onClick.AddListener(Close); close.onClick.AddListener(Close);
            gameObject.SetActive(false);
        }

        public void Open(GameplayPlayMode value,Action onDismissed)
        {
            mode=value; dismissed=onDismissed; open=true;
            transform.SetAsLastSibling(); gameObject.SetActive(true); ShowPage(0);
        }

        public void ShowPage(int index)
        {
            PageIndex=Mathf.Clamp(index,0,PageCount-1);
            TutorialDemoKind kind;
            int sharedPage=PageIndex-(mode==GameplayPlayMode.Drawing ? 5:4);
            if(mode==GameplayPlayMode.Drawing && (PageIndex==3 || PageIndex==4))
            {
                bool climb=PageIndex==4;
                kind=climb ? TutorialDemoKind.Climb:TutorialDemoKind.Crouch;
                title.text=climb ? GameText.L("Ağaç: tırman","Tree: climb","Baum: klettern"):
                    GameText.L("Ağaç: altından geç","Tree: go under","Baum: darunter durch");
                caption.text=climb ? GameText.L("Yeşil ↑ gövdede zıplama tuşuna bas. Tepede köpekten korun ve +5 puan al.",
                    "At the green ↑ trunk, press jump. Stay safe from dogs at the top and earn +5 points.",
                    "Am grünen ↑ Stamm springen drücken. Oben vor Hunden sicher sein und +5 Punkte erhalten."):
                    GameText.L("Mavi ↓ alçak dal: kedi otomatik eğilip altından geçer. Bu ağaca tırmanılmaz.",
                    "Blue ↓ low branch: your cat automatically crouches and goes under. This tree cannot be climbed.",
                    "Blauer ↓ Ast: Die Katze duckt sich automatisch und läuft darunter durch. Nicht zum Klettern.");
            }
            else if(sharedPage>=0)
            {
                kind=sharedPage==0 ? TutorialDemoKind.DogStomp:sharedPage==1 ? TutorialDemoKind.DogContact:TutorialDemoKind.BirdReward;
                title.text=sharedPage==0 ? GameText.L("Üstüne zıpla","Jump on top","Spring darauf"):
                    sharedPage==1 ? GameText.L("Dokunma!","Avoid touching!","Nicht berühren!"):GameText.L("Kuşu yakala","Catch the bird","Fang den Vogel");
                caption.text=sharedPage==0 ? GameText.L("Köpeğin üstüne in. Puf, kaybolur!","Land on the dog. Poof, it disappears!","Lande auf dem Hund. Puff, er verschwindet!"):
                    sharedPage==1 ? GameText.L("Köpeğe yandan dokunursan kaybedersin.","Touching the dog's side ends the round.","Berührst du den Hund seitlich, ist die Runde vorbei."):
                    GameText.L("Kuşları topla. +3 puan kazan!","Collect birds. Earn +3 points!","Sammle Vögel. Bekomme +3 Punkte!");
            }
            else if(mode==GameplayPlayMode.Drawing)
            {
                kind=PageIndex==0 ? TutorialDemoKind.DrawPath:PageIndex==1 ? TutorialDemoKind.DrawTools:TutorialDemoKind.DrawViews;
                title.text=PageIndex==0 ? GameText.L("Yol çiz","Draw a path","Zeichne einen Weg"):
                    PageIndex==1 ? GameText.L("Araçları dene","Try the tools","Probiere die Werkzeuge"):GameText.L("Sonra başlat","Then press play","Dann starte");
                caption.text=PageIndex==0 ? GameText.L("Parmağınla boşluğu birleştir.","Connect the gap with your finger.","Verbinde die Lücke mit deinem Finger."):
                    PageIndex==1 ? GameText.L("Mavi zıplatır. Buz kaydırır. Silgi siler.","Blue bounces. Ice slides. The eraser removes.","Blau federt. Eis rutscht. Der Radierer löscht."):
                    GameText.L("Yeşil okla başlat. Sağ pati yukarı: zıpla. Yeşil işaretli gövdede aynı tuşla tırman.","Tap green play. Right paw up: jump. At a green-marked trunk, use the same control to climb.","Tippe auf Grün zum Starten. Rechte Pfote hoch: springen. Am grün markierten Stamm damit klettern.");
            }
            else
            {
                kind=PageIndex==0 ? TutorialDemoKind.Move:PageIndex==1 ? TutorialDemoKind.Jump:PageIndex==2 ? TutorialDemoKind.Crouch:TutorialDemoKind.Climb;
                title.text=PageIndex==0 ? GameText.L("Yürü","Walk","Laufen"):PageIndex==1 ? GameText.L("Zıpla","Jump","Springen"):
                    PageIndex==2 ? GameText.L("Ağaç: altından geç","Tree: go under","Baum: darunter durch"):GameText.L("Ağaç: tırman","Tree: climb","Baum: klettern");
                caption.text=PageIndex==0 ? GameText.L("Soldaki patiyi sağa veya sola it.","Push the left paw right or left.","Schiebe die linke Pfote nach rechts oder links."):
                    PageIndex==1 ? GameText.L("Sağ patiyi yukarı it. Bırakıp yine it: çift zıpla!","Push the right paw up. Release and push again: double jump!","Rechte Pfote nach oben. Loslassen und erneut: Doppelsprung!"):
                    PageIndex==2 ? GameText.L("Mavi ↓ engel: sağ patiyi aşağıda tut, altından geç. Bu engele tırmanılmaz.","Blue ↓ obstacle: hold the right paw down and go under. This obstacle cannot be climbed.","Blaues ↓ Hindernis: rechte Pfote unten halten und darunter durch. Dieses Hindernis ist nicht zum Klettern."):
                    GameText.L("Yeşil işaretli gövdede zıplama tuşuna basarak tırman. Tepede +5 puan. Çukur yanında aynı tuş zıplatır.","At the green-marked trunk, press jump to climb. At the top: +5 points. By a gap, the same control jumps.","Am grün markierten Stamm mit der Sprungtaste klettern. Oben: +5 Punkte. An Lücken springst du damit.");
            }
            Demo.Configure(kind,null,true,useForestPassage:kind==TutorialDemoKind.Crouch,
                autoCrouch:mode==GameplayPlayMode.Drawing && kind==TutorialDemoKind.Crouch);
            for(int i=0;i<dots.Length;i++)
            {
                dots[i].gameObject.SetActive(i<PageCount);
                dots[i].rectTransform.anchoredPosition=new Vector2((i-(PageCount-1)*.5f)*32,-265);
                dots[i].color=new Color(Green.r,Green.g,Green.b,i==PageIndex ? 1f:.25f);
            }
            previous.gameObject.SetActive(PageIndex>0);
            next.gameObject.SetActive(PageIndex<PageCount-1);
            finish.gameObject.SetActive(PageIndex==PageCount-1);
        }

        public void Close()
        {
            if(!open) return;
            open=false;
            PlayerPrefs.SetInt(mode==GameplayPlayMode.Drawing ? DrawingSeenKey:DirectSeenKey,1); PlayerPrefs.Save();
            gameObject.SetActive(false);
            var callback=dismissed; dismissed=null; callback?.Invoke();
        }

        static Text Label(Transform parent,string name,Vector2 position,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            var text=go.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=fontSize; text.color=new Color(.36f,.22f,.14f); text.alignment=TextAnchor.MiddleCenter;
            text.raycastTarget=false; text.rectTransform.anchoredPosition=position; text.rectTransform.sizeDelta=size; return text;
        }
        static Image Icon(Transform parent,string name,Sprite sprite,Vector2 position,float size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>(); image.sprite=sprite; image.color=color; image.raycastTarget=false;
            image.rectTransform.anchoredPosition=position; image.rectTransform.sizeDelta=Vector2.one*size; return image;
        }
        static Button Button(Transform parent,string name,Sprite sprite,Vector2 position,float size,Color color)
        {
            var image=Icon(parent,name,CozyIconFactory.CircleButton(),position,size,color); image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            image.gameObject.AddComponent<UiButtonSound>();
            Icon(image.transform,"Icon",sprite,Vector2.zero,size*.55f,Color.white);
            var shadow=image.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.25f); shadow.effectDistance=new Vector2(0,-5);
            return button;
        }
    }
}
