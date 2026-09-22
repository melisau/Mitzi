using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawPath.Core
{
    /// <summary>
    /// Kayıt PlayerPrefs'te JSON + son sağlam yedek olarak tutulur.
    /// ScriptableObject tanımları (kedi / ürün kimlikleri) bu kayıta referans verir.
    /// </summary>
    public static class SaveService
    {
        const string Key = "PawPath.Save.v1";
        const string BackupKey = "PawPath.Save.v1.backup";
        const string CorruptPrimaryKey = "PawPath.Save.v1.corrupt";
        const string CorruptBackupKey = "PawPath.Save.v1.backup.corrupt";
        const string MitziOnlyKey = "PawPath.Developer.MitziOnly";
        public const int CurrentSchemaVersion = 2;

        [Serializable]
        public class CatNeedState
        {
            public string catId;
            public int hunger = 100;
            public int water = 100;
            public int affection = 100;
            // Sıfır: eski kayıttan geçiş; geçmişe dönük bakım cezası uygulanmaz.
            public long lastNeedsUpdateUtcTicks;
        }

        [Serializable]
        public class SaveData
        {
            public int schemaVersion = CurrentSchemaVersion;
            public int lovePoints = 0;
            public int highestCompletedLevel = 0;
            public string selectedCatId = "mitzi";
            public List<string> unlockedCatIds = new List<string> { "mitzi" };
            public List<string> ownedItemIds = new List<string>();
            public List<string> placedItemIds = new List<string>();
            public List<CatNeedState> catNeeds = new List<CatNeedState>();
        }

        static SaveData cache;
        static bool readOnlyFutureSave;

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
            string primaryJson = PlayerPrefs.GetString(Key, "");
            string backupJson = PlayerPrefs.GetString(BackupKey, "");
            readOnlyFutureSave = false;

            if (TryReadJson(primaryJson, out cache, out bool futurePrimary))
            {
                // Eski sürüm ve eksik alanlar yedeklenerek yeni şemaya yazılır.
                if (!string.Equals(primaryJson, JsonUtility.ToJson(cache), StringComparison.Ordinal))
                    Persist();
                else if (!TryReadJson(backupJson, out _, out bool futureBackupOnLoad) &&
                    !futureBackupOnLoad)
                {
                    if (!string.IsNullOrEmpty(backupJson))
                        PlayerPrefs.SetString(CorruptBackupKey, backupJson);
                    PlayerPrefs.SetString(BackupKey, primaryJson);
                    PlayerPrefs.Save();
                }
                return;
            }

            bool backupValid = TryReadJson(backupJson, out SaveData backup, out bool futureBackup);
            if (futurePrimary || futureBackup)
            {
                // Daha yeni bir sürümün kaydını eski oyun sürümü asla ezmez.
                readOnlyFutureSave = true;
                cache = backupValid ? backup : new SaveData();
                Debug.LogWarning("[PawPath] Daha yeni kayıt sürümü bulundu. Kayıt salt okunur açıldı; ilerleme üzerine yazılmayacak.");
                return;
            }

            if (backupValid)
            {
                if (!string.IsNullOrEmpty(primaryJson))
                    PlayerPrefs.SetString(CorruptPrimaryKey, primaryJson);
                cache = backup;
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(cache));
                PlayerPrefs.Save();
                Debug.LogWarning("[PawPath] Ana kayıt okunamadı; son sağlam yedekten geri yüklendi.");
                return;
            }

            if (!string.IsNullOrEmpty(primaryJson))
                PlayerPrefs.SetString(CorruptPrimaryKey, primaryJson);
            if (!string.IsNullOrEmpty(backupJson))
                PlayerPrefs.SetString(CorruptBackupKey, backupJson);
            cache = new SaveData();
            Persist();
            if (!string.IsNullOrEmpty(primaryJson) || !string.IsNullOrEmpty(backupJson))
                Debug.LogError("[PawPath] Ana kayıt ve yedek okunamadı. Ham veriler .corrupt anahtarlarında korundu.");
        }

        public static void Persist()
        {
            if (readOnlyFutureSave)
                return;
            if (cache == null)
                cache = new SaveData();
            Normalize(cache);
            string nextJson = JsonUtility.ToJson(cache);
            string previousJson = PlayerPrefs.GetString(Key, "");
            string existingBackup = PlayerPrefs.GetString(BackupKey, "");
            TryReadJson(existingBackup, out _, out bool futureBackup);
            if (!futureBackup && TryReadJson(previousJson, out _, out _))
                PlayerPrefs.SetString(BackupKey, previousJson);
            else if (!futureBackup && !TryReadJson(existingBackup, out _, out _))
                PlayerPrefs.SetString(BackupKey, nextJson);
            PlayerPrefs.SetString(Key, nextJson);
            PlayerPrefs.Save();
        }

        // İçe/dışa aktarma arayüzünün güvenle kullanabileceği sürümlü JSON sınırı.
        public static string ExportJson()
        {
            SaveData data = Data;
            return readOnlyFutureSave
                ? PlayerPrefs.GetString(Key, "") : JsonUtility.ToJson(data, true);
        }

        public static bool TryImportJson(string json)
        {
            if (!TryReadJson(json, out SaveData imported, out _))
                return false;
            cache = imported;
            readOnlyFutureSave = false;
            Persist();
            return true;
        }

        public static bool TryReadJson(string json, out SaveData data, out bool futureVersion)
        {
            data = null;
            futureVersion = false;
            if (string.IsNullOrWhiteSpace(json) ||
                json.IndexOf("\"highestCompletedLevel\"", StringComparison.Ordinal) < 0 ||
                json.IndexOf("\"unlockedCatIds\"", StringComparison.Ordinal) < 0)
                return false;

            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                    return false;
                bool hasVersion = json.IndexOf("\"schemaVersion\"", StringComparison.Ordinal) >= 0;
                if (!hasVersion)
                    data.schemaVersion = 1;
                if (data.schemaVersion < 0)
                {
                    data = null;
                    return false;
                }
                if (data.schemaVersion > CurrentSchemaVersion)
                {
                    futureVersion = true;
                    data = null;
                    return false;
                }
                Normalize(data);
                return true;
            }
            catch (Exception)
            {
                data = null;
                return false;
            }
        }

        static void Normalize(SaveData data)
        {
            data.schemaVersion = CurrentSchemaVersion;
            data.lovePoints = Mathf.Max(0, data.lovePoints);
            data.highestCompletedLevel = Mathf.Max(0, data.highestCompletedLevel);
            if (data.unlockedCatIds == null)
                data.unlockedCatIds = new List<string>();
            if (!data.unlockedCatIds.Contains("mitzi"))
                data.unlockedCatIds.Insert(0, "mitzi");
            if (data.ownedItemIds == null)
                data.ownedItemIds = new List<string>();
            if (data.placedItemIds == null)
                data.placedItemIds = new List<string>();
            if (data.catNeeds == null)
                data.catNeeds = new List<CatNeedState>();
            if (string.IsNullOrWhiteSpace(data.selectedCatId) ||
                !data.unlockedCatIds.Contains(data.selectedCatId))
                data.selectedCatId = "mitzi";
            foreach (CatNeedState need in data.catNeeds)
            {
                if (need == null)
                    continue;
                need.hunger = Mathf.Clamp(need.hunger, 0, 100);
                need.water = Mathf.Clamp(need.water, 0, 100);
                need.affection = Mathf.Clamp(need.affection, 0, 100);
                need.lastNeedsUpdateUtcTicks = Math.Max(0, need.lastNeedsUpdateUtcTicks);
            }
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
            var catsToReset = new List<string>(Data.unlockedCatIds);
            readOnlyFutureSave = false;
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(BackupKey);
            PlayerPrefs.DeleteKey(CorruptPrimaryKey);
            PlayerPrefs.DeleteKey(CorruptBackupKey);
            foreach (string catId in catsToReset)
            {
                PlayerPrefs.DeleteKey($"DailyCare.Feed.{catId}");
                PlayerPrefs.DeleteKey($"DailyCare.Water.{catId}");
                PlayerPrefs.DeleteKey($"DailyCare.Sleep.{catId}");
                PlayerPrefs.DeleteKey($"DailyPettingPoints.{catId}");
                PlayerPrefs.DeleteKey($"DailyPettingDate.{catId}");
            }
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
