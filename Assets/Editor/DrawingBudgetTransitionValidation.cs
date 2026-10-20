using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PawPath.Core;
using PawPath.Data;
using PawPath.Drawing;
using PawPath.Gameplay;
using PawPath.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DrawingBudgetTransitionValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Equal(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < .001f, $"{message}: {actual} != {expected}");
    static void Draw(LineDraw draw, float length, Vector2 origin)
    {
        draw.SetBrushType(PathSurfaceType.Normal);
        Call(draw, "BeginStroke", origin); Call(draw, "AppendPoint", origin + Vector2.right * length);
        draw.FinishCurrentStroke(); Physics2D.SyncTransforms();
    }
    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use isolated validation project");
        var oldMode = GameplayMode.Current;
        var oldDifficulty = (DifficultyLevel)PlayerPrefs.GetInt("PawPath.Difficulty", 0);
        bool oldAutomatic = GameDifficulty.IsAutomatic;
        int oldCompleted = -1;
        var evidence = new List<string> { "resolution,difficulty,level,views,budget,view1_after_spend,view2_available,view3_available" };
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameplayMode.Select(GameplayPlayMode.Drawing);
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>("Assets/_Game/Content/PawPathCatalog.asset");
            var flow = new GameObject("BudgetTransitionFlow").AddComponent<GameFlow>(); Call(flow, "Awake"); flow.BindCatalog(catalog);
            oldCompleted = SaveService.Data.highestCompletedLevel;
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 4.45f;
            var follow = camera.gameObject.AddComponent<SideScrollCamera>(); Call(follow, "Awake");
            var draw = camera.gameObject.AddComponent<LineDraw>(); Call(draw, "Awake");
            var course = new GameObject("BudgetCourse").AddComponent<LevelCourseBuilder>();
            var manager = new GameObject("BudgetLevelManager").AddComponent<LevelManager>(); Call(manager, "Awake"); manager.BindCourse(course);
            int cases = 0;
            foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                new Vector2Int(2340, 1080), new Vector2Int(2560, 1600), new Vector2Int(1024, 768) })
            foreach (DifficultyLevel difficulty in Enum.GetValues(typeof(DifficultyLevel)))
            {
                camera.aspect = (float)size.x / size.y; GameDifficulty.Select(difficulty);
                // Real numbered levels 1 -> 2 -> 3, in addition to camera views 1 -> 2 -> 3.
                for (int level = 1; level <= 3; level++)
                {
                    SaveService.Data.highestCompletedLevel = level - 1;
                    Call(flow, "TransitionTo", GameFlowState.Gameplay); manager.BeginCurrentLevel();
                    float factor = difficulty == DifficultyLevel.Easy ? 1.2f : difficulty == DifficultyLevel.Hard ? .9f : 1f;
                    float budget = manager.Current.inkBudget * 2f * factor;
                    Equal(draw.InkLeft, budget, "Numbered level inherited old budget");
                    Check(!draw.HasDrawnPath && follow.PreparationViewIndex == 0, "New level retains strokes/view");
                    Check(follow.PreparationViewCount >= 4 && follow.PreparationViewCount <= 5, "Unexpected supported-screen view count");
                    Equal(draw.AvailableInkForCurrentView(), budget / 2f, "Initial allowance");
                    Draw(draw, budget / 2f, new Vector2(1000, 0));
                    Equal(draw.InkLeft, budget / 2f, "First view spent reserved second half");
                    Equal(draw.AvailableInkForCurrentView(), 0, "First half exceeded");
                    Check(flow.State == GameFlowState.DrawingPreparation, "Half exhaustion started game");
                    follow.MovePreparationView(1);
                    Equal(draw.AvailableInkForCurrentView(), 0, "Second view incorrectly refills");
                    Draw(draw, 1, new Vector2(1100, 0));
                    Equal(draw.InkLeft, budget / 2f, "Blocked view consumed ink");
                    follow.MovePreparationView(1);
                    Equal(draw.AvailableInkForCurrentView(), budget / 2f, "Third view did not unlock reserve");
                    evidence.Add(FormattableString.Invariant($"{size.x}x{size.y},{difficulty},{level},{follow.PreparationViewCount},{budget},0,0,{budget / 2f}"));
                    Draw(draw, budget / 4f, new Vector2(1200, 0));
                    follow.MovePreparationView(-1); follow.MovePreparationView(-1);
                    Equal(draw.InkLeft, budget / 4f, "Backtracking generated ink");
                    Equal(draw.AvailableInkForCurrentView(), 0, "Backtracking bypassed first-half cap");
                    flow.PauseGame(); flow.ResumeGame();
                    Equal(draw.InkLeft, budget / 4f, "Pause resets budget");
                    // Erase the first-area stroke while back in view 1. Its exact pool is refunded.
                    draw.SetEraser(); Call(draw, "EraseAt", new Vector2(1001, 0));
                    Equal(draw.InkLeft, budget * .75f, "Erase refund wrong");
                    Equal(draw.AvailableInkForCurrentView(), budget / 2f, "Erase lost first-half allowance");
                    follow.MovePreparationView(1); follow.MovePreparationView(1);
                    Draw(draw, budget * .75f, new Vector2(1300, 0));
                    Equal(draw.InkLeft, 0, "Full exhaustion failed");
                    Check(flow.State == GameFlowState.Gameplay && !draw.CanDraw, "Full exhaustion did not start game");
                    flow.ShowLevelFailure("Validation"); flow.RetryCurrentLevel();
                    Equal(draw.InkLeft, budget, "Retry did not reset full budget");
                    Equal(draw.AvailableInkForCurrentView(), budget / 2f, "Retry first-half allowance wrong");
                    Check(!draw.HasDrawnPath && follow.PreparationViewIndex == 0, "Retry retains old path/view");
                    Draw(draw, 1f, new Vector2(1400, 0));
                    flow.StartGameplayFromDrawing(true);
                    Equal(draw.InkLeft, budget - 1f, "Early play refills leftover");
                    Check(!draw.CanDraw, "Early play permits drawing");
                    cases++;
                }
            }
            // Generic rule follows camera grouping, not a hard-coded third-view refill.
            int groups = 0;
            foreach (float aspect in new[] { 1f, 1.333333f, 1.777778f, 2.166667f, 3.555556f })
            {
                camera.aspect = aspect; Call(flow, "TransitionTo", GameFlowState.DrawingPreparation);
                follow.ResetView(); draw.ResetInk(20);
                int count = follow.PreparationViewCount;
                for (int i = 0; i < count; i++)
                {
                    Equal(draw.AvailableInkForCurrentView(), i < count / 2 ? 10f : 20f, "View grouping changed");
                    follow.MovePreparationView(1); groups++;
                }
            }
            string output = Environment.GetEnvironmentVariable("MITZI_BUDGET_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "mitzi-budget-transition");
            Directory.CreateDirectory(output); File.WriteAllLines(Path.Combine(output, "transitions.csv"), evidence);
            Debug.Log($"DRAWING_BUDGET_TRANSITION_PASS cases={cases} viewGroupingChecks={groups} levels=1,2,3 views=1,2,3 retry=pause=erase=backtrack=earlyPlay=pass productionBudgetCode=unchanged output={output}");
        }
        finally
        {
            if (oldCompleted >= 0) SaveService.Data.highestCompletedLevel = oldCompleted;
            UnityEngine.Object.FindObjectOfType<LineDraw>()?.ClearStrokes(); GameplayMode.Select(oldMode);
            GameDifficulty.Select(oldDifficulty); if (oldAutomatic) GameDifficulty.SelectAutomatic(); Time.timeScale = 1f;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
