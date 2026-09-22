using System;
using PawPath.Cat;
using PawPath.Core;
using UnityEditor;
using UnityEngine;

public static class CatNeedsRulesValidation
{
    [MenuItem("Mitzi/Validate Cat Needs")]
    public static void Run()
    {
        long now = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc).Ticks;
        var first = new SaveService.CatNeedState { catId = "mitzi",
            lastNeedsUpdateUtcTicks = now - TimeSpan.FromHours(2).Ticks };
        var second = new SaveService.CatNeedState { catId = "pamuk",
            lastNeedsUpdateUtcTicks = now };
        Require(CatNeedsRules.ApplyDecay(first, now, 8, 2, 3, 1), "Two-hour decay missing.");
        Require(first.hunger == 92 && first.water == 88 && first.affection == 96,
            "Decay rates are incorrect.");
        Require(!CatNeedsRules.ApplyDecay(first, now, 8, 2, 3, 1),
            "The same elapsed time was charged twice.");
        Require(second.hunger == 100 && second.water == 100 && second.affection == 100,
            "One cat changed another cat's values.");

        var offline = new SaveService.CatNeedState { catId = "ada",
            lastNeedsUpdateUtcTicks = now - TimeSpan.FromDays(3).Ticks };
        CatNeedsRules.ApplyDecay(offline, now, 8, 2, 3, 1);
        Require(offline.hunger == 68 && offline.water == 52 && offline.affection == 84,
            "Offline decay was not capped at eight hours.");
        Require(!CatNeedsRules.ApplyDecay(offline, now, 8, 2, 3, 1),
            "Capped offline time was charged again.");

        var oldSave = new SaveService.CatNeedState { catId = "komur" };
        CatNeedsRules.ApplyDecay(oldSave, now, 8, 2, 3, 1);
        Require(oldSave.hunger == 100 && oldSave.lastNeedsUpdateUtcTicks == now,
            "Migrating an old save reduced needs.");
        Require(CatNeedsRules.CanTravel(first, 20), "Healthy cat was blocked.");
        first.water = 19;
        Require(!CatNeedsRules.CanTravel(first, 20), "Thirsty cat was allowed to travel.");
        Debug.Log("Cat needs validation passed.");
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
