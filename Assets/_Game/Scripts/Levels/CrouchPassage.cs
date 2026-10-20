using System.Collections.Generic;
using UnityEngine;
using PawPath.Cat;
using PawPath.Core;
using PawPath.UI;
using PawPath.Localization;
namespace PawPath.Levels
{
    /// <summary>An open-ended low ceiling. Rear artwork never blocks either entrance.</summary>
    public sealed class CrouchPassage : MonoBehaviour
    {
        static readonly List<CrouchPassage> active=new List<CrouchPassage>();
        public float Width { get; private set; }
        public float FloorY { get; private set; }
        public float Clearance { get; private set; }
        GameObject crouchHint;
        public float Left => transform.position.x-Width*.5f;
        public float Right => transform.position.x+Width*.5f;
        public static float PassageClearance => .62f * DisplaySizeSettings.CatScale;
        public const float UpperHeight = 3.9f;
        public const float TreeSafetyDistance = 12f;
        public const float JumpExitRunway = 3.2f;
        public const float DogExitRunway = 2f;
        public static void FitUpperArtwork(SpriteRenderer renderer,float width,float clearance)
        {
            var size=renderer.sprite.bounds.size;
            float scale=UpperHeight/size.y;
            renderer.transform.localScale=Vector3.one*scale;
            renderer.transform.localPosition=new Vector3(renderer.sprite.name.Contains("forest") ? .2f : 0f,clearance+.25f-renderer.sprite.bounds.min.y*scale,0);
        }
        // Measured underside of each beam in its complete rear-support artwork.
        public static float ArtUndersideFraction(Sprite sprite)
        {
            string name = sprite != null ? sprite.name : "";
            return name.Contains("forest") ? .24f : name.Contains("cyber") ? .27f : .30f;
        }
        public static void FitArtwork(SpriteRenderer renderer, float width, float clearance)
        {
            var size = renderer.sprite.bounds.size;
            float xScale = width / size.x;
            float yScale = (clearance + .06f) / (size.y * ArtUndersideFraction(renderer.sprite));
            renderer.transform.localScale = new Vector3(xScale, yScale, 1f);
            renderer.transform.localPosition = new Vector3(0f, -.06f - renderer.sprite.bounds.min.y * yScale, 0f);
            renderer.sortingOrder = 3;
        }
        public void Configure(float width,float floor,float clearance)
        {
            Width=width;FloorY=floor;Clearance=clearance;if(!active.Contains(this))active.Add(this);
            if(crouchHint==null)
            {
                crouchHint=ObstacleActionHint.Create(transform,false,GameText.L("Altından geç","Go under","Darunter durch"));
            }
        }
        void Update()
        {
            var cat=CatController.Instance;
            bool near=cat!=null && !cat.IsBusy && !cat.IsClimbing &&
                (GameFlow.Instance==null || GameFlow.Instance.State==GameFlowState.Gameplay) &&
                Mathf.Abs(cat.transform.position.x-transform.position.x)<Width*.5f+3f;
            if(crouchHint==null)return;
            crouchHint.SetActive(near);
            if(near)crouchHint.transform.localPosition=new Vector3(cat.transform.position.x<transform.position.x ? -Width*.5f-.6f : Width*.5f+.6f,Clearance+.65f+Mathf.Sin(Time.unscaledTime*4f)*.06f,0);
        }
        void OnEnable(){if(!active.Contains(this))active.Add(this);}
        void OnDisable()=>active.Remove(this);
        void OnDestroy()=>active.Remove(this);
        bool NearHeight(CatController cat)
        {
            var collider=cat.GetComponent<Collider2D>();
            return collider!=null && collider.bounds.min.y<FloorY+Clearance+.04f && collider.bounds.max.y>FloorY-.1f;
        }
        public static bool NeedsCrouch(CatController cat,float extra=0f)
        {
            float reach=.75f*DisplaySizeSettings.CatScale+extra;
            foreach(var passage in active)
                if(passage!=null && passage.isActiveAndEnabled && passage.NearHeight(cat) && cat.transform.position.x>passage.Left-reach && cat.transform.position.x<passage.Right+reach)return true;
            return false;
        }
        public static bool BlocksDog(float x,float step,float reach,out float stop)
        {
            reach += DogExitRunway;
            bool blocked=false;float nearest=float.PositiveInfinity;stop=x;
            foreach(var p in active)
            {
                if(p==null || !p.isActiveAndEnabled)continue;
                float left=p.Left-reach,right=p.Right+reach;
                float boundary=step>0?left:right;
                bool crossing=step>0&&x<=left+.02f&&x+step>=left||step<0&&x>=right-.02f&&x+step<=right;
                bool inside=x>left&&x<right;
                bool deeper=inside&&(step>0&&x<(left+right)*.5f||step<0&&x>=(left+right)*.5f);
                if(!(crossing||deeper))continue;
                float distance=Mathf.Abs(boundary-x);if(distance<nearest){nearest=distance;stop=boundary;blocked=true;}
            }
            return blocked;
        }
        public static float SafeDogSpawnX(float x,float reach)
        {
            reach += DogExitRunway;
            foreach(var p in active)
            {
                if(p==null||!p.isActiveAndEnabled)continue;
                float left=p.Left-reach-.02f,right=p.Right+reach+.02f;
                if(x>left&&x<right)x=x<(left+right)*.5f?left:right;
            }
            return x;
        }
        public static bool ContainsX(float x)
        {foreach(var p in active)if(p!=null && p.isActiveAndEnabled && x>p.Left-1f && x<p.Right+1f)return true;return false;}
    }
}
