using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "ENP/Ads/Ads Config")]
    public sealed class AdsConfig : ScriptableObject
    {
        // Стандартні публічні test ad unit id від Google (однакові для всіх застосунків):
        // https://developers.google.com/admob/unity/test-ads
        private const string TestInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";
        private const string TestInterstitialIos = "ca-app-pub-3940256099942544/4411468910";
        private const string TestRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
        private const string TestRewardedIos = "ca-app-pub-3940256099942544/1712485313";
        private const string TestAppOpenAndroid = "ca-app-pub-3940256099942544/9257395921";
        private const string TestAppOpenIos = "ca-app-pub-3940256099942544/5575463023";

        [Header("AdMob — Interstitial")]
        [SerializeField] private string _prodInterstitialAndroid;
        [SerializeField] private string _prodInterstitialIos;

        [Header("AdMob — Rewarded")]
        [SerializeField] private string _prodRewardedAndroid;
        [SerializeField] private string _prodRewardedIos;

        [Header("AdMob — App Open")]
        [SerializeField] private string _prodAppOpenAndroid;
        [SerializeField] private string _prodAppOpenIos;
        [SerializeField] private bool _shouldAutoInitializeAppOpen;

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

        public string ResolveInterstitialAndroid(bool useProduction) =>
            useProduction ? _prodInterstitialAndroid : TestInterstitialAndroid;

        public string ResolveInterstitialIos(bool useProduction) =>
            useProduction ? _prodInterstitialIos : TestInterstitialIos;

        public string ResolveRewardedAndroid(bool useProduction) =>
            useProduction ? _prodRewardedAndroid : TestRewardedAndroid;

        public string ResolveRewardedIos(bool useProduction) =>
            useProduction ? _prodRewardedIos : TestRewardedIos;

        public string ResolveAppOpenAndroid(bool useProduction) =>
            useProduction ? _prodAppOpenAndroid : TestAppOpenAndroid;

        public string ResolveAppOpenIos(bool useProduction) =>
            useProduction ? _prodAppOpenIos : TestAppOpenIos;

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
