using System;
using System.Reflection;
using PawPath.Core;
using PawPath.Drawing;
using PawPath.Gameplay;
using PawPath.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DrawingInkRefundValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
    static void Equal(float actual, float expected, string message) =>
        Check(Mathf.Abs(actual - expected) < .0001f, $"{message}: expected={expected}, actual={actual}");
    static void View(SideScrollCamera follow, bool second) =>
        typeof(SideScrollCamera).GetField("preparationViewIndex", Hidden).SetValue(follow,
            second ? follow.PreparationViewCount - 1 : 0);
    static void Draw(LineDraw draw, params Vector2[] points)
    {
        draw.SetBrushType(draw.CurrentBrush);
        Call(draw, "BeginStroke", points[0]);
        for (int i = 1; i < points.Length; i++) Call(draw, "AppendPoint", points[i]);
        draw.FinishCurrentStroke();
        Physics2D.SyncTransforms();
    }
    static void Erase(LineDraw draw, Vector2 point)
    {
        draw.SetEraser();
        Call(draw, "EraseAt", point);
    }

    public static void Run()
    {
        Check(Application.productName == "MitziValidation", "Use the isolated validation project.");
        var previousMode = GameplayMode.Current;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameplayMode.Select(GameplayPlayMode.Drawing);
            var host = new GameObject("RefundTestCamera");
            var camera = host.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.aspect = 1.5f;
            var follow = host.AddComponent<SideScrollCamera>();
            var draw = host.AddComponent<LineDraw>();
            Call(draw, "Awake");
            var meter = new GameObject("RefundTestMeter").AddComponent<DrawingInkMeter>();
            var fill = new GameObject("RefundTestFill", typeof(RectTransform)).AddComponent<Image>();
            meter.Bind(fill);
            int cycles = 0;

            foreach (PathSurfaceType brush in Enum.GetValues(typeof(PathSurfaceType)))
            {
                draw.ResetInk(20);
                View(follow, false);
                draw.SetBrushType(brush);
                // A bent path has a different smoothed length; refund the charged length.
                Draw(draw, new Vector2(100, 0), new Vector2(101, 0), new Vector2(101, 2));
                Equal(draw.InkLeft, 17, "Drawing did not charge length");
                Equal(draw.AvailableInkForCurrentView(), 7, "First-half charge incorrect");
                Call(meter, "LateUpdate");
                Equal(fill.rectTransform.sizeDelta.x, 190f * 17 / 20, "Meter charge incorrect");
                Erase(draw, new Vector2(101, 1));
                Equal(draw.InkLeft, 20, "Whole-stroke refund missing");
                Equal(draw.AvailableInkForCurrentView(), 10, "First-half refund missing");
                Call(meter, "LateUpdate");
                Equal(fill.rectTransform.sizeDelta.x, 190, "Meter refund incorrect");
                Check(!draw.HasDrawnPath, "Deleted path still counted");
                // One stroke remains throughout: repeated refunds must not drift even when
                // the drawing budget never returns to a completely empty state.
                Draw(draw, new Vector2(110, 0), new Vector2(111.37f, 0));
                float retained = draw.InkLeft;
                for (int cycle = 0; cycle < 100; cycle++)
                {
                    Draw(draw, new Vector2(100, 0), new Vector2(100.73f, 0), new Vector2(100.73f, 1.19f));
                    Erase(draw, new Vector2(100.73f, .6f));
                    Erase(draw, new Vector2(100.73f, .6f));
                    Equal(draw.InkLeft, retained, "Repeated erase created/lost ink");
                    Check(draw.CanDraw, "Refunded ink unusable");
                    cycles++;
                }
                draw.ClearStrokes();
                Equal(draw.InkLeft, 20, "Clear did not refund");
            }

            draw.ResetInk(20);
            View(follow, false);
            Draw(draw, new Vector2(100, 0), new Vector2(115, 0));
            Equal(draw.InkLeft, 10, "First-half cap not applied");
            Equal(draw.AvailableInkForCurrentView(), 0, "First-half cap exceeded");
            Erase(draw, new Vector2(105, 0));
            Equal(draw.AvailableInkForCurrentView(), 10, "Exhausted first-half pool not restored");
            Draw(draw, new Vector2(100, 0), new Vector2(102, 0));
            View(follow, true);
            Draw(draw, new Vector2(110, 0), new Vector2(113, 0));
            View(follow, false);
            Erase(draw, new Vector2(111, 0));
            Equal(draw.InkLeft, 18, "Second-half refund incorrect");
            Equal(draw.AvailableInkForCurrentView(), 8, "Second-half refund changed first-half allowance");
            View(follow, true);
            Erase(draw, new Vector2(101, 0));
            Equal(draw.InkLeft, 20, "First-half path erased from second view did not refund");
            View(follow, false);
            Equal(draw.AvailableInkForCurrentView(), 10, "Cross-view refund lost original pool");

            // A stroke can span both pools if the camera view changes while drawing.
            Call(draw, "BeginStroke", new Vector2(100, 0));
            Call(draw, "AppendPoint", new Vector2(102, 0));
            View(follow, true);
            Call(draw, "AppendPoint", new Vector2(104, 0));
            draw.FinishCurrentStroke();
            Physics2D.SyncTransforms();
            Erase(draw, new Vector2(103, 0));
            View(follow, false);
            Equal(draw.InkLeft, 20, "Mixed-pool stroke total refund incorrect");
            Equal(draw.AvailableInkForCurrentView(), 10, "Mixed-pool first-half refund incorrect");

            // Overlapping collider hits must refund a stroke only once.
            Draw(draw, new Vector2(100, 0), new Vector2(102, 0));
            var stroke = UnityEngine.Object.FindObjectOfType<PathSurface>().gameObject;
            var extra = stroke.AddComponent<EdgeCollider2D>();
            extra.points = stroke.GetComponent<EdgeCollider2D>().points;
            Physics2D.SyncTransforms();
            Erase(draw, new Vector2(101, 0));
            Equal(draw.InkLeft, 20, "Duplicate collider refund incorrect");
            var foreign = new GameObject("UnownedLevelSurface");
            foreign.AddComponent<PathSurface>();
            foreign.AddComponent<BoxCollider2D>().size = Vector2.one;
            Physics2D.SyncTransforms();
            Erase(draw, Vector2.zero);
            Check(foreign != null && foreign.activeSelf, "Eraser removed unowned level geometry");
            Equal(draw.InkLeft, 20, "Unowned surface created ink");

            draw.ResetInk(12);
            Equal(draw.InkLeft, 12, "New level budget incorrect");
            Equal(draw.AvailableInkForCurrentView(), 6, "New level first-half budget incorrect");
            Debug.Log("DRAWING_INK_REFUND_VALIDATION_PASS cycles=" + cycles +
                " brushes=4 meter=pass pools=pass duplicateHits=pass reset=pass");
        }
        finally
        {
            UnityEngine.Object.FindObjectOfType<LineDraw>()?.ClearStrokes();
            GameplayMode.Select(previousMode);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
