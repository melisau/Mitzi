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
        [Tooltip("Yeni kayıtta ve geliştirici tek-kedi görünümünde kullanılan başlangıç kedisi.")]
        public bool isStarterCat;
        public Color furTint = new Color(0.85f, 0.62f, 0.38f);
        public Sprite portrait;
        public Sprite idleSprite;
        public RuntimeAnimatorController animator;
        [Header("İsteğe Bağlı Animasyon Kareleri")]
        public Sprite[] walkFrames;
        public Sprite[] idleFrames;
        public Sprite[] jumpFrames;
        public Sprite[] sleepFrames;
        [Tooltip("Bu kedi evde yemek, tırmanma ve uyku geçiş karelerini destekler.")]
        public bool useDetailedHomeAnimations;
        public AudioClip purrClip;
        public AudioClip meowClip;
    }
}
