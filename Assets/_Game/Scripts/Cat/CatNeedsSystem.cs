using System;
using UnityEngine;
using PawPath.Economy;
using PawPath.Core;

namespace PawPath.Cat
{
    public class CatNeedsSystem : MonoBehaviour
    {
        public static CatNeedsSystem Instance { get; private set; }

        [Header("Günlük Puan Limitleri")]
        public int maxDailyPettingPoints = 100;
        public int foodPoints = 15;
        public int waterPoints = 20;
        public int sleepPoints = 30;

        [Header("Giriş Koşulu")]
        public int requiredEnergyToPlay = 20;

        private int currentDailyPettingPoints = 0;

        const string FeedKey = "DailyCare.Feed";
        const string WaterKey = "DailyCare.Water";
        const string SleepKey = "DailyCare.Sleep";

        public int DailyPettingPoints => PlayerPrefs.GetInt($"DailyPettingPoints.{SelectedCatId}", 0);
        string SelectedCatId => SaveService.Data != null && !string.IsNullOrEmpty(SaveService.Data.selectedCatId)
            ? SaveService.Data.selectedCatId : "mitzi";
        public bool CanFeedToday => !WasUsedToday(CareKey(FeedKey, SelectedCatId));
        public bool CanGiveWaterToday => !WasUsedToday(CareKey(WaterKey, SelectedCatId));
        public bool CanSleepToday => !WasUsedToday(CareKey(SleepKey, SelectedCatId));

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            CheckDailyReset();
        }

        private void CheckDailyReset()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastResetDate = PlayerPrefs.GetString("LastNeedsResetDate", "");

            if (lastResetDate != today)
            {
                currentDailyPettingPoints = 0;
                PlayerPrefs.SetInt("DailyPettingPoints", 0);
                PlayerPrefs.SetString("LastNeedsResetDate", today);
                PlayerPrefs.Save();
            }
            else
            {
                currentDailyPettingPoints = PlayerPrefs.GetInt("DailyPettingPoints", 0);
            }
        }

        public bool TryPetCat(int pointsEarned, string catId = null)
        {
            CheckDailyReset();

            catId = string.IsNullOrEmpty(catId) ? SelectedCatId : catId;
            var needs = GetNeeds(catId);
            string dailyPointsKey = $"DailyPettingPoints.{catId}";
            string dailyDateKey = $"DailyPettingDate.{catId}";
            if (PlayerPrefs.GetString(dailyDateKey, "") != Today())
            {
                PlayerPrefs.SetString(dailyDateKey, Today());
                PlayerPrefs.SetInt(dailyPointsKey, 0);
            }
            currentDailyPettingPoints = PlayerPrefs.GetInt(dailyPointsKey, 0);

            if (currentDailyPettingPoints >= maxDailyPettingPoints)
            {
                return false;
            }

            int allowedPoints = Mathf.Min(pointsEarned, maxDailyPettingPoints - currentDailyPettingPoints);
            currentDailyPettingPoints += allowedPoints;
            PlayerPrefs.SetInt(dailyPointsKey, currentDailyPettingPoints);
            needs.affection = Mathf.Clamp(needs.affection + allowedPoints, 0, 100);
            SaveService.Persist();

            if (CozyEconomyManager.Instance != null)
            {
                CozyEconomyManager.Instance.AddLove(allowedPoints, "Kediyi Okşama");
            }
            return true;
        }

        public bool FeedCat(string catId = null)
        {
            catId = string.IsNullOrEmpty(catId) ? SelectedCatId : catId;
            bool success = TryDailyCare(CareKey(FeedKey, catId), foodPoints, "Mama Verme");
            if (success)
            {
                GetNeeds(catId).hunger = 100;
                SaveService.Persist();
            }
            return success;
        }

        public bool GiveWater(string catId = null)
        {
            catId = string.IsNullOrEmpty(catId) ? SelectedCatId : catId;
            bool success = TryDailyCare(CareKey(WaterKey, catId), waterPoints, "Su Verme");
            if (success)
            {
                GetNeeds(catId).water = 100;
                SaveService.Persist();
            }
            return success;
        }

        public bool PutToSleep()
        {
            return TryDailyCare(CareKey(SleepKey, SelectedCatId), sleepPoints, "Dinlendirme");
        }

        public void RewardInteraction(int points, string reason)
        {
            if (points > 0)
                CozyEconomyManager.Instance?.AddLove(points, reason);
        }

        public bool CanStartLevel()
        {
            if (CozyEconomyManager.Instance == null) return true;
            return CozyEconomyManager.Instance.LovePoints >= requiredEnergyToPlay;
        }

        bool TryDailyCare(string key, int points, string reason)
        {
            CheckDailyReset();
            if (WasUsedToday(key))
                return false;

            PlayerPrefs.SetString(key, Today());
            PlayerPrefs.Save();
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.AddLove(points, reason);
            return true;
        }

        static bool WasUsedToday(string key) => PlayerPrefs.GetString(key, "") == Today();
        static string Today() => DateTime.Now.ToString("yyyy-MM-dd");
        static string CareKey(string baseKey, string catId) => $"{baseKey}.{catId}";

        public SaveService.CatNeedState GetNeeds(string catId)
        {
            if (string.IsNullOrEmpty(catId)) catId = "mitzi";
            var found = SaveService.Data.catNeeds.Find(state => state != null && state.catId == catId);
            if (found != null) return found;
            found = new SaveService.CatNeedState { catId = catId };
            SaveService.Data.catNeeds.Add(found);
            SaveService.Persist();
            return found;
        }
    }
}
