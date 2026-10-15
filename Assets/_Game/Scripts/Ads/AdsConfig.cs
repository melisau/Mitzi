namespace PawPath.Ads
{
    /// <summary>
    /// AdMob kimlikleri. Şu an Google'ın herkese açık TEST kimlikleri kullanılıyor:
    /// test reklamı gösterir, gelir getirmez. AdMob hesabı açılınca:
    ///  1. Uygulama kimliğini (ca-app-pub-XXXX~YYYY) Assets > Google Mobile Ads > Settings'e gir.
    ///  2. Ödüllü reklam birimi kimliğini (ca-app-pub-XXXX/ZZZZ) aşağıdaki ProductionRewarded alanlarına yaz.
    /// Development Build'lerde her zaman test kimlikleri kullanılır; böylece kendi reklamına
    /// tıklayıp hesabın askıya alınma riski olmaz.
    /// </summary>
    public static class AdsConfig
    {
        // Google'ın resmi test ödüllü reklam birimleri.
        const string TestRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
        const string TestRewardedIos = "ca-app-pub-3940256099942544/1712485313";

        // TODO(yayın): AdMob'daki gerçek ödüllü reklam birimi kimlikleri.
        const string ProductionRewardedAndroid = "";
        const string ProductionRewardedIos = "";

        /// <summary>Bir ödüllü reklamın verdiği Sevgi puanı.</summary>
        public const int RewardLovePoints = 20;

        /// <summary>Günde izlenebilecek ödüllü reklam sayısı (çocuk kitlesi için düşük tutuldu).</summary>
        public const int DailyRewardedLimit = 3;

        public static string RewardedAdUnitId
        {
            get
            {
#if UNITY_ANDROID
                return Pick(ProductionRewardedAndroid, TestRewardedAndroid);
#elif UNITY_IPHONE
                return Pick(ProductionRewardedIos, TestRewardedIos);
#else
                return "unused";
#endif
            }
        }

        public static bool UsingTestAds =>
            UnityEngine.Debug.isDebugBuild || RewardedAdUnitId.Contains("3940256099942544");

        static string Pick(string production, string test) =>
            UnityEngine.Debug.isDebugBuild || string.IsNullOrEmpty(production) ? test : production;
    }
}
