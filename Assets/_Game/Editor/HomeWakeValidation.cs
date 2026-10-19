#if UNITY_EDITOR
using System;
using System.Reflection;
using PawPath.Core;
using PawPath.Hub;
using UnityEngine;

public static class HomeWakeValidation
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Call(object target, string name) => target.GetType().GetMethod(name, Hidden).Invoke(target, null);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static void Run()
    {
        try
        {
            HomeSettingsValidation.Run();
            GameFlow.Instance.EnterHub();
            int tested = 0;
            foreach (var cat in GameFlow.Instance.Catalog.cats)
            {
                if (cat.sleepFrames == null || cat.sleepFrames.Length == 0) continue;
                var root = new GameObject("WakeTest_" + cat.id);
                var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform);
                var renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = cat.walkFrames[0];
                var resident = root.AddComponent<CatHomeBehaviour>(); Call(resident, "Awake");
                var sleep = root.AddComponent<MitziSleepAnimator>(); sleep.Bind(cat, renderer, resident);
                foreach (bool direct in new[] { false, true })
                {
                    resident.SleepFor(10); Call(sleep, "Update");
                    Set(sleep, "transitionTime", 20f); Call(sleep, "Update");
                    Vector3 origin = root.transform.position;
                    if (direct) resident.WalkDirectTo((Vector2)origin + Vector2.right);
                    else resident.WalkTo((Vector2)origin + Vector2.right);
                    Check(sleep.BlocksWalking, cat.id + " can slide before sleep Update");
                    Call(resident, "Update");
                    Check(root.transform.position == origin, cat.id + " moved while curled");
                    Call(sleep, "Update");
                    Check(sleep.BlocksWalking, cat.id + " can move during wake frames");
                    Set(sleep, "transitionTime", cat.sleepFrames.Length * .18f + .1f);
                    Call(sleep, "Update");
                    Check(!sleep.BlocksWalking && renderer.sprite == cat.walkFrames[0], cat.id + " never wakes to walk");
                    Check(resident.CurrentActivity == ResidentActivity.Walking, "Wake lost destination");
                }
                UnityEngine.Object.DestroyImmediate(root); tested++;
            }
            Check(tested > 0, "No sleep animation fixtures");
            Debug.Log("HOME_WAKE_VALIDATION_PASS cats=" + tested);
        }
        catch (Exception error) { Debug.LogException(error); UnityEditor.EditorApplication.Exit(1); }
    }
}
#endif
