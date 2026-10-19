using UnityEngine;
using PawPath.Audio;
using PawPath.Cat;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Economy;
using PawPath.Season;
using PawPath.UI;
using PawPath.Gameplay;

namespace PawPath.Levels
{
    /// <summary>
    /// Bölüm ilerlemesi, spawn/hedef yerleştirme ve belirlenen bölümlerde kedi kurtarma.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] Transform spawnMarker;
        [SerializeField] Transform goalMarker;
        [SerializeField] SeasonBackdrop seasonBackdrop;
        [SerializeField] LevelCourseBuilder courseBuilder;
        bool completionHandled;
        GameObject waitingRescueCat;

        public LevelDefinition Current { get; private set; }
        public int CurrentTheme { get; private set; }
        public DifficultyLevel CurrentDifficulty { get; private set; }
        public int DisplayLevel => SaveService.Data.highestCompletedLevel + 1;
        /// <summary>İlerleme çubuğu için bölümün başlangıç ve bitiş noktaları.</summary>
        public Vector2 SpawnPosition => spawnMarker != null ? (Vector2)spawnMarker.position : Vector2.zero;
        public Vector2 GoalPosition => goalMarker != null ? (Vector2)goalMarker.position : Vector2.right;

        void Awake()
        {
            Instance = this;
        }

        public void BeginCurrentLevel()
        {
            GetComponent<RescueGreeting>()?.Cancel();
            GameFlow.Instance?.SetGameplayPaused(true);
            completionHandled = false;
            Current = ResolveLevel(DisplayLevel);
            var cameraFollow = Camera.main != null ? Camera.main.GetComponent<SideScrollCamera>() : null;
            if (cameraFollow != null)
            {
                if (courseBuilder != null)
                    cameraFollow.SetCourseGoalX(courseBuilder.GoalX);
                cameraFollow.ResetView();
            }
            ApplyLevelTheme(Current.levelNumber);
            if (courseBuilder != null)
                courseBuilder.Build(Current.levelNumber);
            ApplyLayout(Current);
            PlaceWaitingRescueCat(Current.levelNumber);

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
                LineDraw.Instance.ResetInk(Current.inkBudget * 2f * (CurrentDifficulty == DifficultyLevel.Easy ? 1.2f : CurrentDifficulty == DifficultyLevel.Hard ? .9f : 1f));

            // Hazırlık kilidi HUD olayına bağlı olmamalı; kedi ve fizik ilk kareden
            // itibaren, mürekkep bitene kadar burada bekletilir.
            if (GameplayMode.IsDrawing)
                GameFlow.Instance?.BeginDrawingPreparation();

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
            CurrentTheme = selectedTheme;
            CurrentDifficulty = GameDifficulty.ForTheme(selectedTheme);
            bool forestTheme = selectedTheme == 1;
            bool cityTheme = selectedTheme == 2;
            bool cyberTheme = selectedTheme == 3;
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayThemeMusic(selectedTheme);
            if (courseBuilder != null)
            {
                courseBuilder.BindVisuals(
                    cyberTheme ? null : cityTheme && catalog.cityGapSprite != null ? catalog.cityGapSprite :
                        forestTheme && catalog.forestGapSprite != null ? catalog.forestGapSprite : catalog.roadGapSprite,
                    cyberTheme ? null : cityTheme && catalog.cityCarSprite != null ? catalog.cityCarSprite :
                        forestTheme && catalog.forestMoundSprite != null ? catalog.forestMoundSprite : catalog.moundSprite,
                    cyberTheme && catalog.cyberGroundTile != null ? catalog.cyberGroundTile :
                        cityTheme && catalog.cityPlatformSprite != null ? catalog.cityPlatformSprite :
                        forestTheme && catalog.forestGroundUnderfillSprite != null
                            ? catalog.forestGroundUnderfillSprite : catalog.roadPlatformSprite,
                    cyberTheme ? null : cityTheme ? catalog.cityVanSprite :
                        forestTheme ? null : catalog.streetMoundHighSprite,
                    cityTheme ? catalog.cityRoadSprite :
                        null,
                    cyberTheme ? (catalog.roboticBirdFrames != null && catalog.roboticBirdFrames.Length > 0
                        ? catalog.roboticBirdFrames : catalog.roboticBird != null ? new[] { catalog.roboticBird } : null)
                        : catalog.birdFrames,
                    cyberTheme ? catalog.roboticDogWalkFrames : catalog.dogRunFrames,
                    cyberTheme ? null : catalog.climbTreeSprite,
                    cyberTheme ? null : catalog.climbingCatFrames,
                    forestTheme || cityTheme || cyberTheme ? null : catalog.streetGapLeftSprite,
                    forestTheme || cityTheme || cyberTheme ? null : catalog.streetGapRightSprite,
                    cyberTheme,
                    cyberTheme ? catalog.cyberObstacleSprites : null);
                courseBuilder.BindCrouchObstacle(catalog.CrouchObstacleForTheme(selectedTheme),catalog.CrouchUpperForTheme(selectedTheme));
                courseBuilder.BindDogIdle(cyberTheme ? catalog.roboticDogIdleSprite : catalog.dogIdleSprite);
            }

            if (seasonBackdrop != null)
            {
                var background = cyberTheme && catalog.cyberBackground != null
                    ? catalog.cyberBackground
                    : cityTheme && catalog.cityBackground != null
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

            const int completionReward = 5;
            int completedLevel = Current != null ? Current.levelNumber : DisplayLevel;
            int previousCompletedLevel=SaveService.Data.highestCompletedLevel;
            if (LineDraw.Instance != null)
                LineDraw.Instance.CanDraw = false;

            if (completedLevel > SaveService.Data.highestCompletedLevel)
                GameDifficulty.RecordCompletion(CurrentTheme);
            SaveService.Data.highestCompletedLevel = Mathf.Max(SaveService.Data.highestCompletedLevel, completedLevel);
            SaveService.Persist();
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.AddShoppingPoints(completionReward, "Bölüm Tamamlama");
            if (CozyAudioManager.Instance != null)
                CozyAudioManager.Instance.PlayConfirm();
            GameEvents.LevelCompleted();

            if (!SaveService.DeveloperMitziOnly &&
                CatUnlockService.TryUnlockAfterLevel(SaveService.Data.highestCompletedLevel, out var cat))
            {
                System.Action finish=()=>
                {
                    if(GameFlow.Instance!=null)GameFlow.Instance.ShowRescue(cat);
                    ThemeUnlockNotice.ShowNewlyUnlocked(previousCompletedLevel,completedLevel);
                };
                var greeting=GetComponent<RescueGreeting>()??gameObject.AddComponent<RescueGreeting>();
                if(!greeting.Play(CatController.Instance,cat,waitingRescueCat,finish))finish();
                return;
            }

            if (GameFlow.Instance != null)
                GameFlow.Instance.ShowLevelComplete(completedLevel, completionReward);
            ThemeUnlockNotice.ShowNewlyUnlocked(previousCompletedLevel,completedLevel);
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

        void PlaceWaitingRescueCat(int levelNumber)
        {
            if(waitingRescueCat!=null) { waitingRescueCat.SetActive(false); Destroy(waitingRescueCat); }
            waitingRescueCat=null;
            var cat=CatUnlockService.WaitingAtLevel(levelNumber);
            if(cat==null || cat.idleSprite==null || goalMarker==null)return;
            waitingRescueCat=new GameObject("WaitingRescueCat_"+cat.id);
            waitingRescueCat.transform.SetParent(goalMarker.parent,false);
            var sprite=waitingRescueCat.AddComponent<SpriteRenderer>();sprite.sprite=cat.idleSprite;sprite.sortingOrder=9;
            float scale=2.5f*DisplaySizeSettings.CatScale*cat.visualHeightMultiplier/cat.idleSprite.bounds.size.y;
            waitingRescueCat.transform.localScale=new Vector3(-scale,scale,1f);
            float baseline=cat.idleSprite.bounds.min.y+cat.idleSprite.bounds.size.y*cat.idleSprite.pivot.y/cat.idleSprite.rect.height;
            float floor=courseBuilder!=null ? courseBuilder.CatSpawnY-.48f : goalMarker.position.y-.48f;
            waitingRescueCat.transform.position=new Vector3(goalMarker.position.x-1.1f,floor-baseline*scale,0);
            SoftShadow.Attach(waitingRescueCat,sprite,false,8,.55f,.06f,.32f);
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
