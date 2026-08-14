using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "ENP/Ads/Ads Config")]
    public sealed class AdsConfig : ScriptableObject
    {
        [Header("AdMob — Interstitial")]
        [SerializeField] private string _testInterstitialAndroid;
        [SerializeField] private string _testInterstitialIos;
        [SerializeField] private string _prodInterstitialAndroid;
        [SerializeField] private string _prodInterstitialIos;

        [Header("AdMob — Rewarded")]
        [SerializeField] private string _testRewardedAndroid;
        [SerializeField] private string _testRewardedIos;
        [SerializeField] private string _prodRewardedAndroid;
        [SerializeField] private string _prodRewardedIos;

        [Header("AdMob — App Open")]
        [SerializeField] private string _testAppOpenAndroid;
        [SerializeField] private string _testAppOpenIos;
        [SerializeField] private string _prodAppOpenAndroid;
        [SerializeField] private string _prodAppOpenIos;
        [SerializeField] private bool _shouldAutoInitializeAppOpen;

        [Header("AdMob — Build")]
        [SerializeField] private bool _shouldUseProductionAdUnitsByDefault;

        [Header("Consent (UMP)")]
        [SerializeField] private bool _tagForUnderAgeOfConsent;
        [SerializeField] private bool _isDebugGeographyEea = true;
        [SerializeField] private string[] _testDeviceHashedIds = System.Array.Empty<string>();

        [Header("Throttling — Interstitial")]
        [SerializeField] private int _minimumCompletedGamesBeforeFirstInterstitial = 3;
        [SerializeField] private int _maxInterstitialsPerSession = 3;
        [SerializeField] private float _interstitialCooldownSeconds = 180f;
        [SerializeField] private float _antiChainCooldownSeconds = 300f;
        [SerializeField] private float _hourlyWindowMinutes = 60f;
        [SerializeField] private int _hourlyCap = 4;

        [Header("Throttling — App Open")]
        [SerializeField] private float _appOpenMinIntervalBetweenShowsMinutes = 15f;
        [SerializeField] private float _appOpenWindowMinutes = 60f;
        [SerializeField] private int _appOpenMaxShowsPerWindow = 2;

        // AdMob

        public bool ShouldAutoInitializeAppOpen => _shouldAutoInitializeAppOpen;
        public bool ShouldUseProductionAdUnitsByDefault => _shouldUseProductionAdUnitsByDefault;

        public string ResolveInterstitialAndroid(bool useProduction) =>
            useProduction ? _prodInterstitialAndroid : _testInterstitialAndroid;

        public string ResolveInterstitialIos(bool useProduction) =>
            useProduction ? _prodInterstitialIos : _testInterstitialIos;

        public string ResolveRewardedAndroid(bool useProduction) =>
            useProduction ? _prodRewardedAndroid : _testRewardedAndroid;

        public string ResolveRewardedIos(bool useProduction) =>
            useProduction ? _prodRewardedIos : _testRewardedIos;

        public string ResolveAppOpenAndroid(bool useProduction) =>
            useProduction ? _prodAppOpenAndroid : _testAppOpenAndroid;

        public string ResolveAppOpenIos(bool useProduction) =>
            useProduction ? _prodAppOpenIos : _testAppOpenIos;

        // Consent

        // Ім'я властивості навмисно збігається з GoogleMobileAds.Ump.Api.ConsentRequestParameters.TagForUnderAgeOfConsent.
        public bool TagForUnderAgeOfConsent => _tagForUnderAgeOfConsent;
        public bool IsDebugGeographyEea => _isDebugGeographyEea;
        public string[] TestDeviceHashedIds => _testDeviceHashedIds;

        // Throttling

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
