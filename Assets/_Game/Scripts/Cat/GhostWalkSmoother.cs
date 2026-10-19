using System;
using PawPath.Core;
using PawPath.Data;
using UnityEngine;

namespace PawPath.Cat
{
    /// <summary>Continuous transitions between Ghost's registered walking poses.</summary>
    [DefaultExecutionOrder(350)]
    public sealed class GhostWalkSmoother : MonoBehaviour
    {
        SpriteRenderer source;
        CatDefinition definition;
        Animator animator;
        Sprite previous,current;
        Mesh mesh;
        MeshRenderer blended;
        Material material;
        readonly Vector3[] vertices=new Vector3[4];
        float elapsed;
        bool rendering;
        public float BlendWeight { get; private set; }=1;
        public bool IsRendering => rendering;
        public bool UsesMotion { get; private set; }
        static readonly int Walk=Animator.StringToHash("Walk");

        public static void Bind(SpriteRenderer visual,CatDefinition cat)
        {
            if(visual==null)return;
            var smoother=visual.GetComponent<GhostWalkSmoother>();
            if(smoother==null&&cat!=null&&cat.id=="ghost")smoother=visual.gameObject.AddComponent<GhostWalkSmoother>();
            if(smoother==null)return;
            smoother.Clear();smoother.source=visual;smoother.definition=cat;smoother.animator=visual.GetComponent<Animator>();
        }
        void BuildRenderer()
        {
            if(blended!=null)return;
            var shader=Resources.Load<Shader>("Shaders/GhostWalkBlend");if(shader==null||!shader.isSupported)return;
            material=new Material(shader){name="Ghost walking transition"};
            var holder=new GameObject("GhostWalkTransition",typeof(MeshFilter),typeof(MeshRenderer));holder.transform.SetParent(transform,false);
            holder.layer=gameObject.layer;
            blended=holder.GetComponent<MeshRenderer>();blended.sharedMaterial=material;
            mesh=new Mesh{name="Ghost walking quad"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.triangles=new[]{0,1,2,0,2,3};holder.GetComponent<MeshFilter>().sharedMesh=mesh;
            blended.enabled=false;
        }
        public void Refresh(float seconds)
        {
            bool walking=definition!=null&&definition.id=="ghost"&&source!=null&&source.enabled&&source.gameObject.activeInHierarchy&&animator!=null&&animator.enabled&&animator.speed>0&&
                animator.GetCurrentAnimatorStateInfo(0).shortNameHash==Walk&&GameFlow.Instance!=null&&(GameFlow.Instance.InHub||GameFlow.Instance.State==GameFlowState.Gameplay)&&
                definition.walkFrames!=null&&Array.IndexOf(definition.walkFrames,source.sprite)>=0;
            if(!walking){Clear();return;}
            BuildRenderer();if(blended==null)return;
            if(current==null){previous=current=source.sprite;elapsed=1;}
            else if(current!=source.sprite){previous=current;current=source.sprite;elapsed=0;}
            elapsed+=Mathf.Max(0,seconds);
            float interval=definition.walkCycleSeconds/definition.walkFrames.Length/Mathf.Max(.1f,animator.speed);
            BlendWeight=Mathf.Clamp01(elapsed/Mathf.Max(.001f,interval*.95f));
            bool motion=previous!=current&&GhostWalkMotion.Apply(material,definition,Array.IndexOf(definition.walkFrames,previous),Array.IndexOf(definition.walkFrames,current));
            UsesMotion=motion;
            if(previous!=current&&!motion)BlendWeight=1;
            Apply(previous,"A");Apply(current,"B");material.SetFloat("_UseMotion",motion?1:0);material.SetFloat("_Blend",BlendWeight);material.SetColor("_Tint",source.color);
            var bounds=previous.bounds;bounds.Encapsulate(current.bounds);
            vertices[0]=new Vector3(bounds.min.x,bounds.min.y);vertices[1]=new Vector3(bounds.min.x,bounds.max.y);
            vertices[2]=new Vector3(bounds.max.x,bounds.max.y);vertices[3]=new Vector3(bounds.max.x,bounds.min.y);
            mesh.vertices=vertices;mesh.RecalculateBounds();
            blended.sortingLayerID=source.sortingLayerID;blended.sortingOrder=source.sortingOrder;
            blended.transform.localScale=new Vector3(source.flipX?-1:1,source.flipY?-1:1,1);
            source.forceRenderingOff=true;blended.enabled=true;rendering=true;
        }
        void Apply(Sprite sprite,string key)
        {
            var rect=sprite.textureRect;var bounds=sprite.bounds;
            material.SetTexture("_Texture"+key,sprite.texture);
            material.SetVector("_Rect"+key,new Vector4(rect.x/sprite.texture.width,rect.y/sprite.texture.height,rect.width/sprite.texture.width,rect.height/sprite.texture.height));
            material.SetVector("_Bounds"+key,new Vector4(bounds.min.x,bounds.min.y,bounds.size.x,bounds.size.y));
        }
        void Clear()
        {
            if(rendering&&source!=null)source.forceRenderingOff=false;
            if(blended!=null)blended.enabled=false;
            rendering=false;UsesMotion=false;previous=current=null;elapsed=0;BlendWeight=1;
        }
        void LateUpdate()=>Refresh(Time.deltaTime);
        void OnDisable()=>Clear();
        void OnDestroy(){Clear();if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
