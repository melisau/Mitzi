using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace PawPath.Core
{
    /// <summary>One brief, soft system tick; never falls back to a long generic vibration.</summary>
    public static class PettingHaptics
    {
        public const double MinimumIntervalSeconds = 1.5d;
        static double nextPulseTime = double.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => nextPulseTime = double.NegativeInfinity;

        // Shared across all cats: changing cats or restarting a stroke cannot stack pulses.
        static bool ReservePulse(double now)
        {
            if (now < nextPulseTime) return false;
            nextPulseTime = now + MinimumIntervalSeconds;
            return true;
        }

        public static void Pulse()
        {
            if (!ReservePulse(Time.unscaledTimeAsDouble)) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    // Acquire fresh wrappers on the UI thread; the scheduling call's
                    // wrappers have already been disposed by the time this runs.
                    try
                    {
                        using var currentPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                        using var currentActivity = currentPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                        if (!currentActivity.Call<bool>("hasWindowFocus")) return;
                        using var window = currentActivity.Call<AndroidJavaObject>("getWindow");
                        using var view = window.Call<AndroidJavaObject>("getDecorView");
                        // HapticFeedbackConstants.CLOCK_TICK, flags=0: short system tick,
                        // respects the device's haptic settings and needs no new permission.
                        view.Call<bool>("performHapticFeedback", 4, 0);
                    }
                    catch (AndroidJavaException) { /* Unsupported feedback stays silent. */ }
                }));
            }
            catch (AndroidJavaException) { /* No full-strength fallback. */ }
#elif UNITY_IOS && !UNITY_EDITOR
            MitziPettingLightImpact();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void MitziPettingLightImpact();
#endif
    }
}
