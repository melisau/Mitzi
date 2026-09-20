using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawPath.Core
{
    /// <summary>
    /// Tüm kalıcı veri tek JSON blob olarak PlayerPrefs'te tutulur.
    /// ScriptableObject tanımları (kedi / ürün kimlikleri) bu kayıta referans verir.
    /// </summary>
    public static class SaveService
    {
        const string Key = "PawPath.Save.v1";
        const string MitziOnlyKey = "PawPath.Developer.MitziOnly";

        [Serializable]
        public class SaveData
        {
            public int lovePoints = 0;
            public int highestCompletedLevel = 0;
            public string selectedCatId = "mitzi";
            public List<string> unlockedCatIds = new List<string> { "mitzi" };
            public List<string> ownedItemIds = new List<string>();
            public List<string> placedItemIds = new List<string>();
        }

        static SaveData cache;

        public static SaveData Data
        {
            get
            {
                if (cache == null)
                    Load();
                return cache;
            }
        }

        public static void Load()
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                cache = new SaveData();
                Persist();
                return;
            }

            try
            {
                cache = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key)) ?? new SaveData();
                if (cache.unlockedCatIds == null || cache.unlockedCatIds.Count == 0)
                    cache.unlockedCatIds = new List<string> { "mitzi" };
                if (cache.ownedItemIds == null)
                    cache.ownedItemIds = new List<string>();
                if (cache.placedItemIds == null)
                    cache.placedItemIds = new List<string>();
                if (string.IsNullOrWhiteSpace(cache.selectedCatId) || !cache.unlockedCatIds.Contains(cache.selectedCatId))
                    cache.selectedCatId = "mitzi";
                cache.lovePoints = Mathf.Max(0, cache.lovePoints);
                cache.highestCompletedLevel = Mathf.Max(0, cache.highestCompletedLevel);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PawPath] Kayıt okunamadı, sıfırlanıyor: {e.Message}");
                cache = new SaveData();
            }
        }

        public static void Persist()
        {
            if (cache == null)
                cache = new SaveData();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(cache));
            PlayerPrefs.Save();
        }

        public static bool HasCat(string id) => Data.unlockedCatIds.Contains(id);

        public static bool DeveloperMitziOnly => PlayerPrefs.GetInt(MitziOnlyKey, 0) == 1;

        public static void SetDeveloperMitziOnly(bool enabled)
        {
            PlayerPrefs.SetInt(MitziOnlyKey, enabled ? 1 : 0);
            if (enabled)
            {
                Data.selectedCatId = "mitzi";
                Persist();
            }
            PlayerPrefs.Save();
        }

        public static void ResetProgressForTesting()
        {
            cache = new SaveData();
            Persist();
            foreach (string dailyKey in new[]
            {
                "DailyCare.Feed", "DailyCare.Water", "DailyCare.Sleep",
                "LastNeedsResetDate", "DailyPettingPoints", "PawPath.SelectedTheme"
            })
                PlayerPrefs.DeleteKey(dailyKey);
            PlayerPrefs.Save();
        }

        public static void UnlockCat(string id)
        {
            if (HasCat(id))
                return;
            Data.unlockedCatIds.Add(id);
            Persist();
        }
    }
}
