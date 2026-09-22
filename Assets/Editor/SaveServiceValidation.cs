using System;
using PawPath.Core;
using UnityEditor;
using UnityEngine;

public static class SaveServiceValidation
{
    // Gerçek PlayerPrefs kaydına dokunmadan sürüm geçişi ve bozuk JSON'u sınar.
    [MenuItem("Mitzi/Validate Save Format")]
    public static void Run()
    {
        const string legacy = "{\"lovePoints\":45,\"highestCompletedLevel\":6," +
            "\"selectedCatId\":\"pamuk\",\"unlockedCatIds\":[\"mitzi\",\"pamuk\"]," +
            "\"catNeeds\":[{\"catId\":\"pamuk\",\"hunger\":70,\"water\":80,\"affection\":90}]}";
        Require(SaveService.TryReadJson(legacy, out var migrated, out bool future) && !future,
            "Legacy save could not be migrated.");
        Require(migrated.schemaVersion == SaveService.CurrentSchemaVersion &&
            migrated.highestCompletedLevel == 6 && migrated.selectedCatId == "pamuk" &&
            migrated.catNeeds[0].hunger == 70, "Migration lost progress.");

        string current = JsonUtility.ToJson(migrated);
        Require(SaveService.TryReadJson(current, out var roundTrip, out future) &&
            roundTrip.highestCompletedLevel == 6, "Current save did not round-trip.");
        Require(!SaveService.TryReadJson("{broken", out _, out future) && !future,
            "Corrupt primary save was accepted.");
        Require(SaveService.TryReadJson(current, out var backup, out future) &&
            backup.highestCompletedLevel == 6, "Valid backup was not readable.");

        const string newer = "{\"schemaVersion\":999,\"highestCompletedLevel\":9," +
            "\"unlockedCatIds\":[\"mitzi\"]}";
        Require(!SaveService.TryReadJson(newer, out _, out future) && future,
            "Newer save version was not protected.");
        Debug.Log("Save format validation passed; live PlayerPrefs was not changed.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
