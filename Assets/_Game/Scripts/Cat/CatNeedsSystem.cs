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

        [Header("Kedi Bakımı")]
        [Range(0, 100)] public int minimumNeedToPlay = 20;
        [Min(0)] public int hungerLossPerHalfHour = 2;
        [Min(0)] public int waterLossPerHalfHour = 3;
        [Min(0)] public int affectionLossPerHalfHour = 1;
        [Min(1)] public int maximumOfflineHours = 8;

        private int currentDailyPettingPoints = 0;
        float nextNeedsRefresh;
        public event Action NeedsChanged;

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
            RefreshAllNeeds();
        }

        void Update()
        {
            if (Time.unscaledTime < nextNeedsRefresh)
                return;
            nextNeedsRefresh = Time.unscaledTime + 60f;
            RefreshAllNeeds();
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused)
                RefreshAllNeeds();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused)
                RefreshAllNeeds();
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
            // Günlük Sevgi ödülü sınırlı; mama kabı ise kediyi her kullanımda besleyebilir.
            TryDailyCare(CareKey(FeedKey, catId), foodPoints, "Mama Verme");
            GetNeeds(catId).hunger = 100;
            SaveService.Persist();
            NeedsChanged?.Invoke();
            return true;
        }

        public bool GiveWater(string catId = null)
        {
            catId = string.IsNullOrEmpty(catId) ? SelectedCatId : catId;
            TryDailyCare(CareKey(WaterKey, catId), waterPoints, "Su Verme");
            GetNeeds(catId).water = 100;
            SaveService.Persist();
            NeedsChanged?.Invoke();
            return true;
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
            return CatNeedsRules.CanTravel(GetNeeds(SelectedCatId), minimumNeedToPlay);
        }

        public string TravelStatus()
        {
            var needs = GetNeeds(SelectedCatId);
            string values = $"Mama {needs.hunger} · Su {needs.water} · Sevgi {needs.affection}";
            bool ready = CatNeedsRules.CanTravel(needs, minimumNeedToPlay);
            return ready
                ? $"Kedin yola hazır\n{values}"
                : $"Bakım gerekli (her biri en az {minimumNeedToPlay})\n{values}";
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
            if (found == null)
            {
                found = new SaveService.CatNeedState { catId = catId,
                    lastNeedsUpdateUtcTicks = DateTime.UtcNow.Ticks };
                SaveService.Data.catNeeds.Add(found);
                SaveService.Persist();
                return found;
            }
            if (ApplyDecay(found, DateTime.UtcNow.Ticks))
                SaveService.Persist();
            return found;
        }

        void RefreshAllNeeds()
        {
            bool changed = false;
            long now = DateTime.UtcNow.Ticks;
            foreach (string catId in SaveService.Data.unlockedCatIds)
            {
                if (string.IsNullOrEmpty(catId) ||
                    SaveService.Data.catNeeds.Exists(state => state != null && state.catId == catId))
                    continue;
                SaveService.Data.catNeeds.Add(new SaveService.CatNeedState
                {
                    catId = catId,
                    lastNeedsUpdateUtcTicks = now
                });
                changed = true;
            }
            foreach (var needs in SaveService.Data.catNeeds)
                if (needs != null)
                    changed |= ApplyDecay(needs, now);
            if (!changed)
                return;
            SaveService.Persist();
            NeedsChanged?.Invoke();
        }

        bool ApplyDecay(SaveService.CatNeedState needs, long now)
        {
            return CatNeedsRules.ApplyDecay(needs, now, maximumOfflineHours,
                hungerLossPerHalfHour, waterLossPerHalfHour, affectionLossPerHalfHour);
        }
    }

    /// <summary>Kayda veya Unity sahnesine dokunmadan sınanabilen bakım kuralları.</summary>
    public static class CatNeedsRules
    {
        public static bool CanTravel(SaveService.CatNeedState needs, int minimum)
        {
            return needs != null && needs.hunger >= minimum &&
                needs.water >= minimum && needs.affection >= minimum;
        }

        public static bool ApplyDecay(SaveService.CatNeedState needs, long now,
            int maximumOfflineHours, int hungerPerHalfHour, int waterPerHalfHour,
            int affectionPerHalfHour)
        {
            if (needs == null)
                return false;
            long previous = needs.lastNeedsUpdateUtcTicks;
            if (previous <= 0 || previous > now)
            {
                needs.lastNeedsUpdateUtcTicks = now;
                return true;
            }

            const long interval = TimeSpan.TicksPerMinute * 30;
            long elapsed = now - previous;
            long periods = elapsed / interval;
            if (periods == 0)
                return false;
            long maximumPeriods = Math.Max(1, maximumOfflineHours) * 2L;
            long appliedPeriods = Math.Min(periods, maximumPeriods);
            needs.hunger = Mathf.Max(0, needs.hunger - (int)Math.Min(100L, appliedPeriods * Math.Max(0, hungerPerHalfHour)));
            needs.water = Mathf.Max(0, needs.water - (int)Math.Min(100L, appliedPeriods * Math.Max(0, waterPerHalfHour)));
            needs.affection = Mathf.Max(0, needs.affection - (int)Math.Min(100L, appliedPeriods * Math.Max(0, affectionPerHalfHour)));
            // Uzun çevrimdışı aradan kalan süre sonraki açılışta tekrar düşülmez.
            needs.lastNeedsUpdateUtcTicks = periods > maximumPeriods
                ? now : previous + periods * interval;
            return true;
        }
    }
}
