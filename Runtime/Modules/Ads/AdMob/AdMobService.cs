using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    public sealed class AdMobService : IFullscreenAdGate
    {
        private readonly AdsConfig _config;
        private readonly ConsentService _consentService;
        private readonly AdMobInterstitial _interstitial;
        private readonly AdMobRewarded _rewarded;
        private readonly AdMobAppOpenAd _appOpen;

        private bool _isInitialized;
        private bool _isInitializing;
        private bool _isAppOpenInitialized;
        private int _fullscreenAdCount;
        private bool _shouldUseProductionAdUnits;
        private UniTaskCompletionSource<bool> _initializeTcs;

        public AdMobService(AdsConfig config, ConsentService consentService, AdAnalyticsService analytics,
            AdThrottleService throttler, IAdSessionState sessionState)
        {
            _config = config;
            _consentService = consentService;
            _shouldUseProductionAdUnits = config.ShouldUseProductionAdUnitsByDefault;

            _interstitial = new AdMobInterstitial(analytics, throttler, this);
            _rewarded = new AdMobRewarded(analytics, throttler, this);
            _appOpen = new AdMobAppOpenAd(analytics, throttler, this, sessionState);
        }

        public bool IsInitialized => _isInitialized;
        public bool IsRewardedReady => _rewarded.IsReady;
        public bool IsFullscreenAdShowing => _fullscreenAdCount > 0;

        public UniTask<bool> EnsureInitializedAsync()
        {
            if (_isInitialized)
            {
                return UniTask.FromResult(true);
            }

            if (_initializeTcs != null)
            {
                return _initializeTcs.Task;
            }

            _initializeTcs = new UniTaskCompletionSource<bool>();
            Initialize();
            return _initializeTcs.Task;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                _initializeTcs?.TrySetResult(true);
                _initializeTcs = null;
                return;
            }

            if (_isInitializing)
            {
                return;
            }

            _isInitializing = true;

            var interstitialId = GetPlatformId(_config.ResolveInterstitialAndroid, _config.ResolveInterstitialIos);
            var rewardedId = GetPlatformId(_config.ResolveRewardedAndroid, _config.ResolveRewardedIos);

            MobileAds.Initialize(_ =>
            {
                _isInitializing = false;
                _isInitialized = true;

                _interstitial.Initialize(interstitialId);
                _rewarded.Initialize(rewardedId);

                _initializeTcs?.TrySetResult(true);
                _initializeTcs = null;
            });
        }

        public bool ShowInterstitialAd(Action onShow = null, string placement = "unknown")
        {
            if (_consentService.IsEditorAdsBypassEnabled)
            {
                return _interstitial.ShowEditorMockInterstitial(onShow, placement);
            }

            return _interstitial.ShowInterstitialAd(onShow, placement);
        }

        public bool CanShowInterstitialNow()
        {
            if (_consentService.IsEditorAdsBypassEnabled)
            {
                return true;
            }

            if (!_isInitialized || IsFullscreenAdShowing)
            {
                return false;
            }

            return _interstitial.CanShowNow();
        }

        public bool ShowRewardedAd(Action onShowed = null, Action onReward = null, string placement = "unknown") =>
            _rewarded.ShowRewardedAd(onShowed, onReward, placement);

        public void PreloadInterstitial()
        {
            if (_consentService.IsEditorAdsBypassEnabled || !_isInitialized)
            {
                return;
            }

            _interstitial.Preload();
        }

        public void PreloadRewarded()
        {
            if (_consentService.IsEditorAdsBypassEnabled || !_isInitialized)
            {
                return;
            }

            _rewarded.Warmup();
        }

        public void SetUseProductionAdUnits(bool enabled)
        {
            _shouldUseProductionAdUnits = enabled;
        }

        public void SetAppOpenForceShowAlwaysDebug(bool enabled)
        {
            _appOpen.SetForceShowAlwaysDebug(enabled);
        }

        public void InitializeAppOpenIfReady()
        {
            if (!_isInitialized || _isAppOpenInitialized)
            {
                return;
            }

            if (!GoogleMobileAds.Ump.Api.ConsentInformation.CanRequestAds())
            {
                return;
            }

            var appOpenId = GetPlatformId(_config.ResolveAppOpenAndroid, _config.ResolveAppOpenIos);
            if (string.IsNullOrEmpty(appOpenId))
            {
                return;
            }

            _appOpen.Initialize(appOpenId);
            _isAppOpenInitialized = true;
        }

        public bool TryAcquireFullscreenAd()
        {
            if (_fullscreenAdCount > 0)
            {
                return false;
            }

            _fullscreenAdCount++;
            return true;
        }

        public void ReleaseFullscreenAd()
        {
            if (_fullscreenAdCount <= 0)
            {
                _fullscreenAdCount = 0;
                return;
            }

            _fullscreenAdCount--;
        }

        private string GetPlatformId(Func<bool, string> androidSelector, Func<bool, string> iosSelector)
        {
#if UNITY_ANDROID
            return androidSelector(_shouldUseProductionAdUnits);
#elif UNITY_IOS
            return iosSelector(_shouldUseProductionAdUnits);
#else
            return androidSelector(_shouldUseProductionAdUnits);
#endif
        }
    }
}
