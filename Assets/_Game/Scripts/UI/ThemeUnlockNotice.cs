using UnityEngine;
using UnityEngine.UI;
using PawPath.Localization;

namespace PawPath.UI
{
    /// <summary>Appears over either the rescue or completion screen once a new area is earned.</summary>
    public sealed class ThemeUnlockNotice : MonoBehaviour
    {
        public int Theme { get; private set; }
        public static void ShowNewlyUnlocked(int previousLevel,int completedLevel)
        {
            for(int theme=1;theme<4;theme++)
                if(previousLevel<ThemeSelectionUI.UnlockAfter(theme)&&completedLevel>=ThemeSelectionUI.UnlockAfter(theme))Show(theme);
        }
        public static ThemeUnlockNotice Show(int theme)
        {
            var canvas=Object.FindObjectOfType<Canvas>();if(canvas==null)return null;
            var root=new GameObject("ThemeUnlockNotice",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(Image),typeof(ThemeUnlockNotice));
            root.transform.SetParent(canvas.transform,false);var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var overlay=root.GetComponent<Canvas>();overlay.overrideSorting=true;overlay.sortingOrder=500;
            root.GetComponent<Image>().color=new Color(.07f,.04f,.03f,.82f);
            var notice=root.GetComponent<ThemeUnlockNotice>();notice.Theme=theme;
            var card=new GameObject("NewAreaCard",typeof(RectTransform),typeof(Image));card.transform.SetParent(root.transform,false);
            var area=(RectTransform)card.transform;area.anchorMin=area.anchorMax=new Vector2(.5f,.5f);area.sizeDelta=new Vector2(660,390);
            var image=card.GetComponent<Image>();image.sprite=CozyIconFactory.Pill();image.type=Image.Type.Sliced;image.color=new Color(.96f,.89f,.74f);
            Label(card.transform,"Title",GameText.L("Yeni mekan açıldı!","New area unlocked!","Neues Gebiet freigeschaltet!"),new Vector2(0,120),new Vector2(590,65),32);
            Label(card.transform,"AreaName",ThemeSelectionUI.ThemeName(theme),new Vector2(0,55),new Vector2(590,65),36);
            Label(card.transform,"Message",GameText.L("Yeni mekanı seçebilir veya önceki mekanlarda devam edebilirsin.","Choose the new area or keep playing in your previous areas.","Wähle das neue Gebiet oder spiele in deinen bisherigen Gebieten weiter."),new Vector2(0,-12),new Vector2(570,80),23);
            notice.Choice(card.transform,"ChooseNewArea",GameText.L("Yeni mekanı seç","Choose new area","Neues Gebiet wählen"),new Vector2(-153,-120),new Color(.36f,.61f,.35f),true);
            notice.Choice(card.transform,"KeepCurrentArea",GameText.L("Şimdilik burada kal","Stay here for now","Vorerst hier bleiben"),new Vector2(153,-120),new Color(.61f,.45f,.33f),false);
            return notice;
        }
        void Choice(Transform parent,string name,string text,Vector2 position,Color color,bool choose)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(290,72);
            var image=go.GetComponent<Image>();image.sprite=CozyIconFactory.PillButton();image.type=Image.Type.Sliced;image.color=color;
            var button=go.GetComponent<Button>();button.targetGraphic=image;
            var label=Label(go.transform,"Label",text,Vector2.zero,new Vector2(270,65),23);label.color=Color.white;
            button.onClick.AddListener(()=>{if(choose)ThemeSelectionUI.TrySelectTheme(Theme);gameObject.SetActive(false);Destroy(gameObject);});
        }
        static Text Label(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
            var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.25f,.16f,.1f);text.raycastTarget=false;
            return text;
        }
    }
}
