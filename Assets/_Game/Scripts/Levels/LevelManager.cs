using UnityEngine;
using PawPath.Audio;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Season;

namespace PawPath.Levels
{
    /// <summary>
    /// Bölüm ilerlemesi, spawn/hedef yerleştirme ve her 5. bölümde kedi kurtarma.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] Transform spawnMarker;
        [SerializeField] Transform goalMarker;
        [SerializeField] SeasonBackdrop seasonBackdrop;

        public LevelDefinition Current { get; private set; }
        public int DisplayLevel => SaveService.Data.highestCompletedLevel + 1;

        void Awake()
        {
            Instance = this;
        }

        public void BeginCurrentLevel()
        {
            Current = ResolveLevel(DisplayLevel);
            ApplyLayout(Current);

            var catId = SaveService.Data.selectedCatId;
            var cat = GameFlow.Instance != null && GameFlow.Instance.Catalog != null
                ? GameFlow.Instance.Catalog.GetCat(catId)
                : null;

            if (CatController.Instance != null)
            {
                CatController.Instance.ApplyCat(cat);
                CatController.Instance.PlaceAtSpawn(Current.spawnPoint);
                CatController.Instance.FlipTowards(Current.goalPoint);
            }

            if (LineDraw.Instance != null)
                LineDraw.Instance.ResetInk(Current.inkBudget);

            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlaySeason(Current.season);

            GameEvents.LevelStarted();
        }

        public void CompleteLevel()
        {
            if (LineDraw.Instance != null)
                LineDraw.Instance.CanDraw = false;

            SaveService.Data.highestCompletedLevel = Mathf.Max(SaveService.Data.highestCompletedLevel, DisplayLevel);
            SaveService.Persist();
            GameEvents.LevelCompleted();

            if (CatUnlockService.TryUnlockAfterLevel(SaveService.Data.highestCompletedLevel, out var cat))
            {
                if (GameFlow.Instance != null)
                    GameFlow.Instance.ShowRescue(cat);
                return;
            }

            if (GameFlow.Instance != null)
                GameFlow.Instance.EnterHub();
        }

        LevelDefinition ResolveLevel(int number)
        {
            var catalog = GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
            if (catalog != null && catalog.levels != null)
            {
                var found = catalog.levels.Find(l => l != null && l.levelNumber == number);
                if (found != null)
                    return found;
            }

            return FallbackLevel(number);
        }

        static LevelDefinition FallbackLevel(int number)
        {
            var so = ScriptableObject.CreateInstance<LevelDefinition>();
            so.levelNumber = number;
            so.season = (SeasonId)((number - 1) / 5 % 3);
            so.spawnPoint = new Vector2(-6.2f, -1.1f);
            so.goalPoint = new Vector2(6.3f, -0.2f + (number % 4) * 0.45f);
            so.inkBudget = Mathf.Clamp(16f + number * 0.4f, 16f, 28f);
            so.fallY = -7.5f;
            return so;
        }

        void ApplyLayout(LevelDefinition level)
        {
            if (spawnMarker != null)
                spawnMarker.position = level.spawnPoint;
            if (goalMarker != null)
                goalMarker.position = level.goalPoint;
            if (seasonBackdrop != null)
                seasonBackdrop.Apply(level.season);
        }

        public void Bind(Transform spawn, Transform goal, SeasonBackdrop backdrop)
        {
            spawnMarker = spawn;
            goalMarker = goal;
            seasonBackdrop = backdrop;
        }
    }
}
