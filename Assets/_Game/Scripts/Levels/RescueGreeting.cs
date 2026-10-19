using System;
using System.Collections.Generic;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.UI;
using UnityEngine;

namespace PawPath.Levels
{
    /// <summary>A short unscaled greeting over the frozen finish area, using both cats' own coats.</summary>
    public sealed class RescueGreeting : MonoBehaviour
    {
        public const float Duration=3.1f;
        public bool IsPlaying { get; private set; }
        public CatDefinition RescuerDefinition => rescuer?.cat;
        public CatDefinition RescuedDefinition => rescued?.cat;
        public SpriteRenderer RescuerRenderer => rescuer?.renderer;
        public SpriteRenderer RescuedRenderer => rescued?.renderer;
        sealed class Actor
        {
            public CatDefinition cat;
            public SpriteRenderer renderer;
            public Vector3 start, destination;
            public float facing, normalScale, hugScale;
        }
        Actor rescuer,rescued;
        GameObject stage;
        readonly List<(SpriteRenderer renderer,bool enabled)> originals=new List<(SpriteRenderer,bool)>();
        readonly List<SpriteRenderer> hearts=new List<SpriteRenderer>();
        Action finished;
        float elapsed,floor;

        public bool Play(CatController player,CatDefinition found,GameObject waiting,Action onFinished)
        {
            Cancel();
            if(player==null||waiting==null||!HasArt(player.Definition)||!HasArt(found)||GameFlow.Instance==null)return false;
            var waitingVisual=waiting.GetComponent<SpriteRenderer>();if(waitingVisual==null)return false;
            var waitingCat=found.idleSprite;
            float baseline=waitingCat.bounds.min.y+waitingCat.bounds.size.y*waitingCat.pivot.y/waitingCat.rect.height;
            floor=waiting.transform.position.y+baseline*Mathf.Abs(waiting.transform.localScale.y);
            stage=new GameObject("RescueGreetingStage");stage.transform.SetParent(transform,false);
            bool playerLeft=player.transform.position.x<=waiting.transform.position.x;
            rescuer=CreateActor(player.Definition,player.transform.position.x,playerLeft?1:-1,30);
            rescued=CreateActor(found,waiting.transform.position.x,playerLeft?-1:1,31);
            // Meet at a common shoulder height: the taller cat bends down to its friend.
            float hugHeight=Mathf.Min(player.Definition.rescueHugBodyHeight*rescuer.hugScale,found.rescueHugBodyHeight*rescued.hugScale);
            rescuer.hugScale=hugHeight/player.Definition.rescueHugBodyHeight;rescued.hugScale=hugHeight/found.rescueHugBodyHeight;
            float center=(player.transform.position.x+waiting.transform.position.x)*.5f;
            float separation=Mathf.Max(.3f,(player.Definition.rescueHugContactX*rescuer.hugScale+found.rescueHugContactX*rescued.hugScale)*.72f-.07f*DisplaySizeSettings.CatScale);
            rescuer.destination=new Vector3(center-rescuer.facing*separation*.5f,floor,0);
            rescued.destination=new Vector3(center-rescued.facing*separation*.5f,floor,0);
            HideOriginals(player.gameObject);HideOriginals(waiting);
            for(int i=0;i<3;i++)
            {
                var heart=new GameObject("GreetingHeart_"+i).AddComponent<SpriteRenderer>();heart.transform.SetParent(stage.transform,false);
                heart.sprite=CozyIconFactory.Heart();heart.sortingOrder=40;heart.color=new Color(1f,.42f,.57f,0);hearts.Add(heart);
            }
            elapsed=0;finished=onFinished;IsPlaying=true;
            GameFlow.Instance.BeginRescueMeeting();RefreshPose();return true;
        }
        static bool HasArt(CatDefinition cat)=>cat!=null&&cat.idleSprite!=null&&cat.rescueHugFrames!=null&&cat.rescueHugFrames.Length==6&&Array.TrueForAll(cat.rescueHugFrames,s=>s!=null)&&cat.rescueHugBodyHeight>.01f;
        Actor CreateActor(CatDefinition cat,float x,float facing,int order)
        {
            var renderer=new GameObject("GreetingCat_"+cat.id).AddComponent<SpriteRenderer>();renderer.transform.SetParent(stage.transform,false);renderer.sortingOrder=order;renderer.spriteSortPoint=SpriteSortPoint.Pivot;
            float normal=2.5f*DisplaySizeSettings.CatScale*cat.visualHeightMultiplier/cat.idleSprite.bounds.size.y;
            float visible=cat.idleSprite.bounds.size.y*normal*cat.homeVisibleHeightRatio;
            return new Actor{cat=cat,renderer=renderer,start=new Vector3(x,floor,0),facing=facing,normalScale=normal,hugScale=visible*1.5f/cat.rescueHugBodyHeight};
        }
        void HideOriginals(GameObject root)
        {
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)){originals.Add((renderer,renderer.enabled));renderer.enabled=false;}
        }
        public void Advance(float seconds)
        {
            if(!IsPlaying)return;
            if(GameFlow.Instance==null||GameFlow.Instance.State!=GameFlowState.RescueMeeting){Cancel();return;}
            elapsed+=Mathf.Max(0,seconds);RefreshPose();
            if(elapsed<Duration)return;
            var callback=finished;Cancel();callback?.Invoke();
        }
        void RefreshPose()
        {
            Pose(rescuer);Pose(rescued);
            float center=(rescuer.destination.x+rescued.destination.x)*.5f;
            float height=Mathf.Max(rescuer.cat.rescueHugBodyHeight*rescuer.hugScale,rescued.cat.rescueHugBodyHeight*rescued.hugScale);
            float heartTime=Mathf.Clamp01((elapsed-1.2f)/1.6f);
            for(int i=0;i<hearts.Count;i++)
            {
                var heart=hearts[i];float size=(.22f+i*.045f)*DisplaySizeSettings.CatScale;
                float scale=size/heart.sprite.bounds.size.y;heart.transform.localScale=new Vector3(scale,scale,1);
                heart.transform.position=new Vector3(center+(i-1)*.3f,floor+height+.08f+heartTime*.65f+i*.1f,0);
                var color=heart.color;color.a=elapsed<1.2f||elapsed>2.9f?0:Mathf.Sin(heartTime*Mathf.PI);heart.color=color;
            }
        }
        void Pose(Actor actor)
        {
            bool walking=elapsed<.65f;
            var frames=actor.cat.walkFrames;
            var frame=walking&&frames!=null&&frames.Length>0?frames[Mathf.FloorToInt(elapsed*frames.Length/actor.cat.walkCycleSeconds)%frames.Length]:actor.cat.idleSprite;
            float scale=actor.normalScale;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.65f));var position=Vector3.Lerp(actor.start,actor.destination,t);
            float padding=actor.cat.idleSprite.pivot.y/actor.cat.idleSprite.rect.height;
            if(!walking)
            {
                int index=elapsed<.95f?0:elapsed<1.15f?1:elapsed<1.35f?2:elapsed<2.45f?3+(Mathf.FloorToInt((elapsed-1.35f)/.35f)%2):elapsed<2.7f?5:0;
                frame=actor.cat.rescueHugFrames[index];scale=actor.hugScale;padding=0;
                if(elapsed>=1.35f&&elapsed<2.45f)position.x+=actor.facing*Mathf.Sin((elapsed-1.35f)*Mathf.PI*2)*.018f;
            }
            actor.renderer.sprite=frame;actor.renderer.transform.localScale=new Vector3(scale*actor.facing,scale,1);
            position.y=floor-(frame.bounds.min.y+frame.bounds.size.y*padding)*scale;actor.renderer.transform.position=position;
        }
        public void Cancel()
        {
            IsPlaying=false;finished=null;
            foreach(var original in originals)if(original.renderer!=null)original.renderer.enabled=original.enabled;
            originals.Clear();hearts.Clear();
            if(stage!=null){stage.SetActive(false);Destroy(stage);}stage=null;rescuer=null;rescued=null;
        }
        void Update()=>Advance(Time.unscaledDeltaTime);
        void OnDisable()=>Cancel();
    }
}
