using PawPath.Audio;
using PawPath.Economy;
using PawPath.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace PawPath.Ads
{
    /// <summary>
    /// Kedi evindeki "Reklam izle, Alışveriş puanı kazan" butonu. Reklam olduğu açıkça yazılır,
    /// tamamen isteğe bağlıdır ve günlük sınırı vardır.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RewardedAdButton : MonoBehaviour
    {
        Button button;
        Text label;

        void Awake()
        {
            button = GetComponent<Button>();
            label = GetComponentInChildren<Text>();
            button.onClick.AddListener(OnClicked);
        }

        void OnEnable()
        {
            RewardedAdService.StateChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            RewardedAdService.StateChanged -= Refresh;
        }

        void OnClicked()
        {
            var ads = RewardedAdService.Instance;
            if (ads == null)
                return;

            bool shown = ads.TryShow(GrantReward);

            if (!shown)
                CozyAudioManager.Instance?.PlayError();
            Refresh();
        }

        void GrantReward()
        {
            CozyEconomyManager.Instance?.AddShoppingPoints(AdsConfig.RewardShoppingPoints, "Ödüllü reklam");
            CozyAudioManager.Instance?.PlayShopBuy();
        }

        void Refresh()
        {
            if (button == null)
                return;

            var ads = RewardedAdService.Instance;
            int left = ads != null ? Mathf.Max(0, AdsConfig.DailyRewardedLimit - ads.WatchedToday) : 0;

            string text;
            bool interactable;
            if (ads == null || left == 0)
            {
                text = GameText.L("Bugünlük reklam bitti", "No more ads today", "Heute keine Werbung mehr");
                interactable = false;
            }
            else if (!ads.IsReady)
            {
                text = GameText.L("Reklam hazırlanıyor…", "Ad loading…", "Werbung lädt…");
                interactable = false;
            }
            else
            {
                text = GameText.L($"Reklam izle: +{AdsConfig.RewardShoppingPoints} para ({left})", $"Watch ad: +{AdsConfig.RewardShoppingPoints} coins ({left})", $"Werbung ansehen: +{AdsConfig.RewardShoppingPoints} Münzen ({left})");
                interactable = true;
            }

            button.interactable = interactable;
            if (label != null)
                label.text = text;
        }
    }
}
