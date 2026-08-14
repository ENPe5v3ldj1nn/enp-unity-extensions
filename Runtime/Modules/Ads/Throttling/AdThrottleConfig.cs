using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    [CreateAssetMenu(fileName = "AdThrottleConfig", menuName = "ENP/Ads/Ad Throttle Config")]
    public sealed class AdThrottleConfig : ScriptableObject
    {
        [SerializeField] private int _minimumCompletedGamesBeforeFirstInterstitial = 3;
        [SerializeField] private int _maxInterstitialsPerSession = 3;
        [SerializeField] private float _interstitialCooldownSeconds = 180f;
        [SerializeField] private float _antiChainCooldownSeconds = 300f;
        [SerializeField] private float _hourlyWindowMinutes = 60f;
        [SerializeField] private int _hourlyCap = 4;
        [SerializeField] private float _appOpenMinIntervalBetweenShowsMinutes = 15f;
        [SerializeField] private float _appOpenWindowMinutes = 60f;
        [SerializeField] private int _appOpenMaxShowsPerWindow = 2;

        public int MinimumCompletedGamesBeforeFirstInterstitial => _minimumCompletedGamesBeforeFirstInterstitial;
        public int MaxInterstitialsPerSession => _maxInterstitialsPerSession;
        public float InterstitialCooldownSeconds => _interstitialCooldownSeconds;
        public float AntiChainCooldownSeconds => _antiChainCooldownSeconds;
        public float HourlyWindowMinutes => _hourlyWindowMinutes;
        public int HourlyCap => _hourlyCap;
        public float AppOpenMinIntervalBetweenShowsMinutes => _appOpenMinIntervalBetweenShowsMinutes;
        public float AppOpenWindowMinutes => _appOpenWindowMinutes;
        public int AppOpenMaxShowsPerWindow => _appOpenMaxShowsPerWindow;
    }
}
