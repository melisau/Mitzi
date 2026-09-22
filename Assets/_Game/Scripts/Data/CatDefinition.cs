using UnityEngine;

namespace PawPath.Data
{
    public enum CatPersonality
    {
        Curious,
        Sleepy,
        Brave,
        Shy,
        Playful
    }

    /// <summary>
    /// Koleksiyon kedisi tanımı. Assets/_Game/Content altında asset olarak oluşturulur.
    /// </summary>
    [CreateAssetMenu(menuName = "Paw Path/Cat Definition", fileName = "Cat_")]
    public class CatDefinition : ScriptableObject
    {
        public string id = "mitzi";
        public string displayName = "Mitzi";
        [TextArea(2, 6)] public string bio;
        [TextArea(2, 6)] public string encounterDialogue;
        public CatPersonality personality = CatPersonality.Curious;
        public int unlockAfterLevel = 0;
        public Color furTint = new Color(0.85f, 0.62f, 0.38f);
        public Sprite portrait;
        public Sprite idleSprite;
        public RuntimeAnimatorController animator;
        [Header("İsteğe Bağlı Animasyon Kareleri")]
        public Sprite[] walkFrames;
        public Sprite[] idleFrames;
        public Sprite[] jumpFrames;
        public Sprite[] sleepFrames;
        public AudioClip purrClip;
        public AudioClip meowClip;
    }
}
