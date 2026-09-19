using System;
using UnityEngine;
using PawPath.Economy;

namespace PawPath.Cat
{
    public class CatNeedsSystem : MonoBehaviour
    {
        [Header("Günlük Puan Limitleri")]
        public int maxDailyPettingPoints = 100;
        public int foodPoints = 15;
        public int waterPoints = 20;
        public int sleepPoints = 30;

        [Header("Giriş Koşulu")]
        public int requiredEnergyToPlay = 20;

        private int currentDailyPettingPoints = 0;

        private void Awake()
        {
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

        public void FeedCat()
        {
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.AddLove(foodPoints, "Mama Verme");
        }

        public void GiveWater()
        {
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.AddLove(waterPoints, "Su Verme");
        }

        public void PutToSleep()
        {
            if (CozyEconomyManager.Instance != null)
                CozyEconomyManager.Instance.AddLove(sleepPoints, "Dinlendirme");
        }

        public bool CanStartLevel()
        {
            if (CozyEconomyManager.Instance == null) return true;
            return CozyEconomyManager.Instance.LovePoints >= requiredEnergyToPlay;
        }
    }
}