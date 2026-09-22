using System;
using PawPath.Levels;
using UnityEditor;
using UnityEngine;

public static class LevelCourseGeometryValidation
{
    // Test Framework paketi olmayan projede de menüden ya da -executeMethod ile çalışır.
    [MenuItem("Mitzi/Validate Course Geometry")]
    public static void Run()
    {
        var host = new GameObject("CourseGeometryValidation");
        try
        {
            var builder = host.AddComponent<LevelCourseBuilder>();
            for (int level = 1; level <= 100; level++)
            {
                builder.Build(level);
                string error = builder.ValidateCourseGeometry();
                if (error != null)
                    throw new InvalidOperationException($"Bölüm {level}: {error}");
            }
            Debug.Log("Course geometry validation passed: levels 1–100.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }
}
