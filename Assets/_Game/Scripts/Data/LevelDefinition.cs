using UnityEngine;

namespace PawPath.Data
{
    public enum SeasonId
    {
        Summer,
        Autumn,
        Winter
    }

    [CreateAssetMenu(menuName = "Paw Path/Level Definition", fileName = "Level_")]
    public class LevelDefinition : ScriptableObject
    {
        public int levelNumber = 1;
        public SeasonId season = SeasonId.Summer;
        [Tooltip("Başlangıç platformunun dünya konumu.")]
        public Vector2 spawnPoint = new Vector2(-6.2f, -1.2f);
        [Tooltip("Hedefin dünya konumu.")]
        public Vector2 goalPoint = new Vector2(6.4f, 1.1f);
        [Tooltip("Çizilebilir mürekkep uzunluğu (dünya birimi).")]
        public float inkBudget = 18f;
        [Tooltip("Bu yüksekliğin altına düşülürse baloncuk kurtarması başlar.")]
        public float fallY = -7.5f;
    }
}
