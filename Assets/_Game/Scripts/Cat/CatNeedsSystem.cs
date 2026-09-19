using System;
using UnityEngine;
using PawPath.Economy;

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

        public int DailyPettingPoints => currentDailyPettingPoints;
        public bool CanFeedToday => !WasUsedToday(FeedKey);
        public bool CanGiveWaterToday => !WasUsedToday(WaterKey);
        public bool CanSleepToday => !WasUsedToday(SleepKey);

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

        public bool TryPetCat(int pointsEarned)
        {
            CheckDailyReset();

            if (currentDailyPettingPoints >= maxDailyPettingPoints)
            {
                return false;
            }

            int allowedPoints = Mathf.Min(pointsEarned, maxDailyPettingPoints - currentDailyPettingPoints);
            currentDailyPettingPoints += allowedPoints;
            PlayerPrefs.SetInt("DailyPettingPoints", currentDailyPettingPoints);

            if (CozyEconomyManager.Instance != null)
            {
                CozyEconomyManager.Instance.AddLove(allowedPoints, "Kediyi Okşama");
            }
            return true;
        }

        public bool FeedCat()
        {
            return TryDailyCare(FeedKey, foodPoints, "Mama Verme");
        }

        public bool GiveWater()
        {
            return TryDailyCare(WaterKey, waterPoints, "Su Verme");
        }

        public bool PutToSleep()
        {
            return TryDailyCare(SleepKey, sleepPoints, "Dinlendirme");
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
    }
}
