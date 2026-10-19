using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PawPath.Core;
using PawPath.UI;

namespace PawPath.Gameplay
{
    public enum JoystickAxis { Horizontal, Vertical }

    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        JoystickAxis axis;
        RectTransform pad, thumb;
        bool owned, jumpLatched;
        int pointer;
        const float DeadZone=.25f;
        public JoystickAxis Axis => axis;
        public bool HomeMode { get; set; }
        public Vector2 HomeValue { get; private set; }
        static int homePointers, homeReleaseFrame = -1;
        public static bool SuppressHomeWorldInput => homePointers > 0 || homeReleaseFrame == Time.frameCount;
        bool AcceptsInput => GameFlow.Instance == null || (HomeMode
            ? GameFlow.Instance.InHub && !PawPath.Hub.HomeEditMode.Active
            : GameFlow.Instance.State == GameFlowState.Gameplay);

        public static MobileJoystick Create(Transform parent,string name,JoystickAxis axis,Vector2 anchor,bool homeMode=false)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Image));root.transform.SetParent(parent,false);
            var rt=root.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=anchor;
            var background=root.GetComponent<Image>();background.sprite=CozyIconFactory.CircleButton();background.color=new Color(.98f,.94f,.85f,.94f);
            var shadow=root.AddComponent<Shadow>();shadow.effectColor=new Color(.18f,.12f,.09f,.22f);shadow.effectDistance=new Vector2(0,-7);
            var color=axis==JoystickAxis.Horizontal?new Color(.67f,.50f,.37f):new Color(.36f,.63f,.90f);
            Image Part(string part,Sprite sprite,Vector2 min,Vector2 max,Color tint,Transform holder=null)
            {
                var go=new GameObject(part,typeof(RectTransform),typeof(Image));go.transform.SetParent(holder??root.transform,false);
                var image=go.GetComponent<Image>();image.sprite=sprite;image.color=tint;image.raycastTarget=false;
                var rect=image.rectTransform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return image;
            }
            Part("Well",CozyIconFactory.Dot(),Vector2.one*.14f,Vector2.one*.86f,new Color(color.r,color.g,color.b,.18f));
            if(axis==JoystickAxis.Horizontal)
            {
                Part("LeftHint",CozyIconFactory.ArrowLeft(),new Vector2(.07f,.42f),new Vector2(.23f,.58f),color);
                Part("RightHint",CozyIconFactory.ArrowRight(),new Vector2(.77f,.42f),new Vector2(.93f,.58f),color);
            }
            if(axis==JoystickAxis.Vertical || homeMode)
            {
                Part("JumpHint",CozyIconFactory.ArrowUp(),new Vector2(.42f,.77f),new Vector2(.58f,.93f),color);
                Part("CrouchHint",CozyIconFactory.ArrowDown(),new Vector2(.42f,.07f),new Vector2(.58f,.23f),color);
            }
            var knob=Part("Thumb",CozyIconFactory.CircleButton(),Vector2.one*.5f,Vector2.one*.5f,color);
            var knobShadow=knob.gameObject.AddComponent<Shadow>();knobShadow.effectColor=new Color(.2f,.13f,.09f,.22f);knobShadow.effectDistance=new Vector2(0,-5);
            Part("Paw",CozyIconFactory.Paw(),Vector2.one*.28f,Vector2.one*.72f,new Color(1,.97f,.88f),knob.transform);
            var stick=root.AddComponent<MobileJoystick>();stick.axis=axis;stick.pad=rt;stick.thumb=knob.rectTransform;stick.HomeMode=homeMode;
            stick.Resize(PawPath.Core.DisplaySizeSettings.Large);return stick;
        }

        public void Resize(bool large)
        {
            if(pad==null)return;
            float diameter=large?270f:230f;
            pad.sizeDelta=Vector2.one*diameter;
            pad.anchoredPosition=new Vector2((axis==JoystickAxis.Horizontal?1:-1)*(52f+diameter*.5f),44f+diameter*.5f);
            thumb.sizeDelta=Vector2.one*(diameter*.43f);
            Release();
        }

        public void OnPointerDown(PointerEventData data)
        {
            if(owned || data==null || !AcceptsInput)return;
            owned=true;pointer=data.pointerId;jumpLatched=false;
            if(HomeMode)homePointers++;
            ReadPointer(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if(!AcceptsInput)
            {Release();if(axis==JoystickAxis.Vertical)MobileControlState.ClearJump();return;}
            if(owned&&data!=null&&data.pointerId==pointer)ReadPointer(data);
        }
        public void OnPointerUp(PointerEventData data){if(owned&&data!=null&&data.pointerId==pointer)Release();}
        void ReadPointer(PointerEventData data)
        {
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(pad,data.position,data.pressEventCamera,out var local))return;
            float radius=pad.rect.width*.30f;
            if(HomeMode)
            {
                HomeValue=Vector2.ClampMagnitude(local/radius,1f);
                if(HomeValue.magnitude<DeadZone)HomeValue=Vector2.zero;
                thumb.anchoredPosition=HomeValue*radius;
                return;
            }
            ApplyAxis(Mathf.Clamp((axis==JoystickAxis.Horizontal?local.x:local.y)/radius,-1f,1f));
        }
        void ApplyAxis(float value)
        {
            thumb.anchoredPosition=(axis==JoystickAxis.Horizontal?Vector2.right:Vector2.up)*(value*pad.rect.width*.30f);
            if(axis==JoystickAxis.Horizontal)
            {
                MobileControlState.Set(MobileAction.Left,value < -DeadZone);
                MobileControlState.Set(MobileAction.Right,value > DeadZone);
            }
            else
            {
                MobileControlState.Set(MobileAction.Crouch,value < -DeadZone);
                if(value<.12f)jumpLatched=false;
                if(value>.35f&&!jumpLatched){MobileControlState.Set(MobileAction.Jump,true);jumpLatched=true;}
            }
        }
        void Release()
        {
            if(owned && HomeMode){homePointers=Mathf.Max(0,homePointers-1);homeReleaseFrame=Time.frameCount;}
            owned=false;jumpLatched=false;
            if(thumb!=null)thumb.anchoredPosition=Vector2.zero;
            HomeValue=Vector2.zero;
            if(HomeMode)return;
            if(axis==JoystickAxis.Horizontal){MobileControlState.Set(MobileAction.Left,false);MobileControlState.Set(MobileAction.Right,false);}
            else MobileControlState.Set(MobileAction.Crouch,false);
        }
        void OnDisable(){Release();if(axis==JoystickAxis.Vertical)MobileControlState.ClearJump();}
        void OnApplicationFocus(bool focused){if(!focused){Release();if(axis==JoystickAxis.Vertical)MobileControlState.ClearJump();}}
    }
}
