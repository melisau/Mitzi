using UnityEngine;
using PawPath.Audio;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Economy;
using PawPath.Season;
using PawPath.UI;

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
        [SerializeField] LevelCourseBuilder courseBuilder;
        bool completionHandled;

        public LevelDefinition Current { get; private set; }
        public int DisplayLevel => SaveService.Data.highestCompletedLevel + 1;

        void Awake()
        {
            Instance = this;
        }

        public void BeginCurrentLevel()
        {
            completionHandled = false;
            Current = ResolveLevel(DisplayLevel);
            var cameraFollow = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera>() : null;
            if (cameraFollow != null)
                cameraFollow.ResetView();
            ApplyLevelTheme(Current.levelNumber);
            if (courseBuilder != null)
                courseBuilder.Build(Current.levelNumber);
            ApplyLayout(Current);

            var catId = SaveService.Data.selectedCatId;
            var cat = GameFlow.Instance != null && GameFlow.Instance.Catalog != null
                ? GameFlow.Instance.Catalog.GetCat(catId)
                : null;

            if (CatController.Instance != null)
            {
                CatController.Instance.ApplyCat(cat);
                Vector2 spawn = Current.spawnPoint;
                if (courseBuilder != null)
                    spawn.y = courseBuilder.CatSpawnY;
                CatController.Instance.PlaceAtSpawn(spawn);
                CatController.Instance.FlipTowards(Current.goalPoint);
            }

            if (LineDraw.Instance != null)
                LineDraw.Instance.ResetInk(Current.inkBudget);

            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlaySeason(Current.season);

            GameEvents.LevelStarted();
        }

        void ApplyLevelTheme(int levelNumber)
        {
            var catalog = GameFlow.Instance != null ? GameFlow.Instance.Catalog : null;
            if (catalog == null)
                return;

            int selectedTheme = ThemeSelectionUI.GetSelectedTheme(levelNumber);
            bool forestTheme = selectedTheme == 1;
            bool cityTheme = selectedTheme == 2;
            if (courseBuilder != null)
            {
                courseBuilder.BindVisuals(
                    cityTheme && catalog.cityGapSprite != null ? catalog.cityGapSprite :
                        forestTheme && catalog.forestGapSprite != null ? catalog.forestGapSprite : catalog.roadGapSprite,
                    cityTheme && catalog.cityCarSprite != null ? catalog.cityCarSprite :
                        forestTheme && catalog.forestMoundSprite != null ? catalog.forestMoundSprite : catalog.moundSprite,
                    cityTheme && catalog.cityPlatformSprite != null ? catalog.cityPlatformSprite :
                        forestTheme && catalog.forestPlatformSprite != null ? catalog.forestPlatformSprite : catalog.roadPlatformSprite,
                    cityTheme ? catalog.cityVanSprite : null,
                    cityTheme ? catalog.cityRoadSprite : null);
            }

            if (seasonBackdrop != null)
            {
                var background = cityTheme && catalog.cityBackground != null
                    ? catalog.cityBackground
                    : forestTheme && catalog.forestBackground != null ? catalog.forestBackground : catalog.gameplayBackground;
                seasonBackdrop.SetBackground(background);
            }
        }

        public void CompleteLevel()
        {
            if (completionHandled)
                return;
            completionHandled = true;

            const int completionReward = 10;
            int completedLevel = DisplayLevel;
            if (LineDraw.Instance != null)
                LineDraw.Instance.CanDraw = false;

            SaveService.Data.highestCompletedLevel = Mathf.Max(SaveService.Data.highestCompletedLevel, completedLevel);
            SaveService.Persist();
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.AddLove(completionReward, "Bölüm Tamamlama");
            GameEvents.LevelCompleted();

            if (!SaveService.DeveloperMitziOnly &&
                CatUnlockService.TryUnlockAfterLevel(SaveService.Data.highestCompletedLevel, out var cat))
            {
                if (GameFlow.Instance != null)
                    GameFlow.Instance.ShowRescue(cat);
                return;
            }

            if (GameFlow.Instance != null)
                GameFlow.Instance.ShowLevelComplete(completedLevel, completionReward);
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
            {
                Vector2 spawn = level.spawnPoint;
                if (courseBuilder != null)
                    spawn.y = courseBuilder.CatSpawnY;
                spawnMarker.position = spawn;
            }
            if (goalMarker != null)
            {
                Vector2 goal = level.goalPoint;
                if (courseBuilder != null)
                {
                    goal.x = courseBuilder.GoalX;
                    goal.y = courseBuilder.GoalY;
                }
                goalMarker.position = goal;
            }
            if (seasonBackdrop != null)
                seasonBackdrop.Apply(level.season);
        }

        public void Bind(Transform spawn, Transform goal, SeasonBackdrop backdrop)
        {
            spawnMarker = spawn;
            goalMarker = goal;
            seasonBackdrop = backdrop;
        }

        public void BindCourse(LevelCourseBuilder builder) => courseBuilder = builder;
    }
}
