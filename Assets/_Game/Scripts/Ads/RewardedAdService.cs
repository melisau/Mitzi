using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace PawPath.Ads
{
    /// <summary>
    /// Ödüllü reklamları yükler ve gösterir.
    ///
    /// Oyunun hedef kitlesi çocukları da kapsadığı için (Google Play Families politikası)
    /// her istek çocuğa yönelik işaretlenir: kişiselleştirilmiş reklam ve reklam kimliği
    /// kullanılmaz, yalnız "G" (her yaşa uygun) içerik istenir. Yalnızca oyuncunun kendi
    /// isteğiyle açılan ödüllü reklam vardır; oyunu kesen geçiş/banner reklamı yoktur.
    /// </summary>
    public class RewardedAdService : MonoBehaviour
    {
        const string DailyCountKeyPrefix = "PawPath.Ads.RewardedCount.";

        public static RewardedAdService Instance { get; private set; }

        /// <summary>Hazır olma veya günlük sayaç durumu değişince tetiklenir (UI yenilemesi için).</summary>
        public static event Action StateChanged;

        RewardedAd rewardedAd;
        bool initialized;
        bool loading;
        float retryAt;
        int failedLoads;

        public bool IsReady => rewardedAd != null && rewardedAd.CanShowAd();

        public int WatchedToday => PlayerPrefs.GetInt(TodayKey, 0);

        public bool DailyLimitReached => WatchedToday >= AdsConfig.DailyRewardedLimit;

        static string TodayKey => DailyCountKeyPrefix + DateTime.Now.ToString("yyyy-MM-dd");

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            rewardedAd?.Destroy();
            rewardedAd = null;
        }

        void Start()
        {
            if (!AdsConfig.RuntimeAdvertisingEnabled) { StateChanged?.Invoke(); return; }
            // Families politikası: tüm istekler çocuğa yönelik (çocuk yönelimli + rıza yaşı altı
            // işaretlerinin yeni karşılığı) ve en fazla G içerik.
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                AgeRestrictedTreatment = AgeRestrictedTreatment.Child,
                MaxAdContentRating = MaxAdContentRating.G
            });

            MobileAds.Initialize(_ => OnMainThread(() =>
            {
                initialized = true;
                LoadAd();
            }));
        }

        void Update()
        {
            // Yükleme başarısız olduysa artan aralıklarla yeniden dene (en fazla 2 dk).
            if (initialized && !loading && rewardedAd == null && retryAt > 0f && Time.unscaledTime >= retryAt)
                LoadAd();
        }

        void LoadAd()
        {
            if (loading || IsReady)
                return;

            loading = true;
            retryAt = 0f;
            RewardedAd.Load(AdsConfig.RewardedAdUnitId, new AdRequest(), (ad, error) => OnMainThread(() =>
            {
                loading = false;
                if (error != null || ad == null)
                {
                    failedLoads++;
                    retryAt = Time.unscaledTime + Mathf.Min(120f, 5f * Mathf.Pow(2f, failedLoads - 1));
                    Debug.LogWarning($"[PawPath] Ödüllü reklam yüklenemedi: {error?.GetMessage()}");
                    StateChanged?.Invoke();
                    return;
                }

                failedLoads = 0;
                rewardedAd = ad;
                rewardedAd.OnAdFullScreenContentClosed += () => OnMainThread(OnAdFinished);
                rewardedAd.OnAdFullScreenContentFailed += _ => OnMainThread(OnAdFinished);
                StateChanged?.Invoke();
            }));
        }

        /// <summary>
        /// Reklamı gösterir. Oyuncu reklamı sonuna kadar izlerse <paramref name="onRewarded"/> çağrılır.
        /// </summary>
        public bool TryShow(Action onRewarded)
        {
            if (DailyLimitReached || !IsReady)
                return false;

            rewardedAd.Show(_ => OnMainThread(() =>
            {
                PlayerPrefs.SetInt(TodayKey, WatchedToday + 1);
                PlayerPrefs.Save();
                onRewarded?.Invoke();
                StateChanged?.Invoke();
            }));
            return true;
        }

        /// <summary>SDK geri çağrıları başka iş parçacığında gelebilir; Unity API'si ana iş parçacığı ister.</summary>
        static void OnMainThread(Action action) => MobileAdsEventExecutor.ExecuteInUpdate(action);

        void OnAdFinished()
        {
            // Her reklam tek kullanımlık; kapanınca bir sonrakini hazırla.
            rewardedAd?.Destroy();
            rewardedAd = null;
            StateChanged?.Invoke();
            if (!DailyLimitReached)
                LoadAd();
        }
    }
}
