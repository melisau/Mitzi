using System;
using PawPath.Levels;
using UnityEditor;
using UnityEngine;

public static class GoalTriggerValidation
{
    [MenuItem("Mitzi/Validate Goal Bounds")]
    public static void Run()
    {
        Vector2 goal = new Vector2(19.35f, -0.65f);
        Require(GoalTrigger.IsWithinFinishBounds(new Vector2(19.4f, -0.8f), goal,
            0.05f, -0.65f, 4.7f), "Cat at gate height should finish.");
        Require(!GoalTrigger.IsWithinFinishBounds(new Vector2(20f, -3f), goal,
            0.05f, -0.65f, 4.7f), "Falling cat below gate must not finish.");
        Require(!GoalTrigger.IsWithinFinishBounds(new Vector2(20f, 5f), goal,
            0.05f, -0.65f, 4.7f), "Cat above gate must not finish.");
        Require(!GoalTrigger.IsWithinFinishBounds(new Vector2(19f, -0.8f), goal,
            0.05f, -0.65f, 4.7f), "Cat before gate must not finish.");
        Debug.Log("Goal bounds validation passed.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
