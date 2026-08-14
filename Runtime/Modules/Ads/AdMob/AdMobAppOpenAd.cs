using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    public sealed class AdMobAppOpenAd
    {
        private const string AppOpenPlacement = "app_open_foreground";
        private const int MinBackgroundDurationSeconds = 20;
        private const int ExpirationHours = 4;
        private const int LoadRetryDelaySeconds = 30;
        private const int ShowSettlingDelayMs = 800;

        private readonly AdAnalyticsService _analytics;
        private readonly AdThrottleService _throttler;
        private readonly IFullscreenAdGate _fullscreenGate;
        private readonly IAdSessionState _sessionState;

        private bool _isInitialized;
        private bool _isLoading;
        private bool _isShowing;
        private bool _isFullscreenGuardHeld;
        private bool _isPendingShowAfterLoad;
        private bool _hasBackgrounded;
        private bool _isInBackground;
        private string _appOpenAdId;
        private AppOpenAd _appOpenAd;
        private DateTime _expireTimeUtc = DateTime.MinValue;
        private DateTime _lastBackgroundTimeUtc = DateTime.MinValue;
        private AppOpenLifecycleListener _lifecycleListener;
        private bool _hasReportedAppOpenShow;

        private bool _isRetryScheduled;
        private int _retryVersion;
        private bool _shouldForceShowAlways;

        public AdMobAppOpenAd(AdAnalyticsService analytics, AdThrottleService throttler,
            IFullscreenAdGate fullscreenGate, IAdSessionState sessionState)
        {
            _analytics = analytics;
            _throttler = throttler;
            _fullscreenGate = fullscreenGate;
            _sessionState = sessionState;
        }

        public void Initialize(string appOpenAdId)
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            _appOpenAdId = appOpenAdId;
            _hasReportedAppOpenShow = false;

            EnsureLifecycleListener();
            LoadAppOpenAd();
        }

        public void SetForceShowAlwaysDebug(bool enabled)
        {
            _shouldForceShowAlways = enabled;
        }

        private bool IsAdAvailable => _appOpenAd != null && _appOpenAd.CanShowAd() && !IsAdExpired;
        private bool IsAdExpired => _appOpenAd != null && DateTime.UtcNow >= _expireTimeUtc;

        private void EnsureLifecycleListener()
        {
            if (_lifecycleListener != null)
            {
                return;
            }

            var go = new GameObject("AdMobAppOpenAdListener");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            _lifecycleListener = go.AddComponent<AppOpenLifecycleListener>();
            _lifecycleListener.Initialize(this);
        }

        private void HandleForeground()
        {
            _isInBackground = false;
            TryShowOnForeground();
        }

        private void HandleBackground()
        {
            _isInBackground = true;
            _hasBackgrounded = true;
            _lastBackgroundTimeUtc = DateTime.UtcNow;
            _isPendingShowAfterLoad = false;
        }

        private void TryShowOnForeground()
        {
            if (!CanAttemptShow())
            {
                EnsureLoaded();
                return;
            }

            if (IsAdAvailable)
            {
                ShowAd();
                return;
            }

            _isPendingShowAfterLoad = true;
            LoadAppOpenAd();
        }

        private bool CanAttemptShow()
        {
            if (!_isInitialized || _isShowing)
            {
                return false;
            }

            if (_sessionState.IsFirstLaunch)
            {
                return false;
            }

            if (_shouldForceShowAlways)
            {
                if (_isInBackground || !_hasBackgrounded)
                {
                    return false;
                }

                return ConsentInformation.CanRequestAds()
                       && !_fullscreenGate.IsFullscreenAdShowing
                       && _throttler.CanShowAppOpenNow();
            }

            if (_isInBackground || !_hasBackgrounded)
            {
                return false;
            }

            var backgroundDuration = DateTime.UtcNow - _lastBackgroundTimeUtc;
            if (backgroundDuration.TotalSeconds < MinBackgroundDurationSeconds)
            {
                return false;
            }

            if (!ConsentInformation.CanRequestAds())
            {
                return false;
            }

            if (_fullscreenGate.IsFullscreenAdShowing)
            {
                return false;
            }

            return _throttler.CanShowAppOpenNow();
        }

        private void EnsureLoaded()
        {
            if (!_isInitialized || _isLoading)
            {
                return;
            }

            if (IsAdExpired)
            {
                DestroyAd();
            }

            if (IsAdAvailable)
            {
                return;
            }

            LoadAppOpenAd();
        }

        private void ShowAd()
        {
            _analytics.LogShowAttempt("app_open", AppOpenPlacement);
            if (!IsAdAvailable)
            {
                _analytics.LogShowFailed("app_open", AppOpenPlacement, "not_ready");
                LoadAppOpenAd();
                return;
            }

            if (!_fullscreenGate.TryAcquireFullscreenAd())
            {
                _analytics.LogShowFailed("app_open", AppOpenPlacement, "fullscreen_guard");
                EnsureLoaded();
                return;
            }

            _isPendingShowAfterLoad = false;
            _isShowing = true;
            _isFullscreenGuardHeld = true;
            ShowAfterSettlingDelay().Forget();
        }

        private async UniTaskVoid ShowAfterSettlingDelay()
        {
            await UniTask.Delay(ShowSettlingDelayMs);

            if (!_isInitialized || _isInBackground || !IsAdAvailable)
            {
                _analytics.LogShowFailed("app_open", AppOpenPlacement, "settling_aborted");
                _isShowing = false;
                ReleaseFullscreenGuard();
                EnsureLoaded();
                return;
            }

            _appOpenAd.Show();
        }

        private void LoadAppOpenAd()
        {
            if (!_isInitialized || _isLoading)
            {
                return;
            }

            if (!ConsentInformation.CanRequestAds())
            {
                return;
            }

            if (string.IsNullOrEmpty(_appOpenAdId))
            {
                return;
            }

            _isLoading = true;
            DestroyAd();

            var adRequest = new AdRequest();
            AppOpenAd.Load(_appOpenAdId, adRequest,
                (AppOpenAd ad, LoadAdError error) =>
                {
                    _isLoading = false;

                    if (error != null || ad == null)
                    {
                        var errorCode = error == null ? "null" : error.GetCode().ToString();
                        var errorDomain = error == null ? "null" : error.GetDomain();

                        Debug.LogError($"AppOpen load failed. code={errorCode}, domain={errorDomain}, message={error?.GetMessage()}, response={error?.GetResponseInfo()}");
                        _analytics.LogLoadFailed("app_open", AppOpenPlacement, "load_error", errorCode, errorDomain, error?.GetMessage());
                        _isPendingShowAfterLoad = false;
                        ScheduleRetry();
                        return;
                    }

                    _analytics.LogLoaded("app_open", AppOpenPlacement);
                    _expireTimeUtc = DateTime.UtcNow.AddHours(ExpirationHours);
                    _appOpenAd = ad;
                    RegisterEventHandlers(ad);

                    if (_isPendingShowAfterLoad)
                    {
                        _isPendingShowAfterLoad = false;
                        TryShowOnForeground();
                    }
                });
        }

        private void DestroyAd()
        {
            if (_appOpenAd == null)
            {
                return;
            }

            _appOpenAd.Destroy();
            _appOpenAd = null;
            _expireTimeUtc = DateTime.MinValue;
        }

        private void RegisterEventHandlers(AppOpenAd ad)
        {
            ad.OnAdImpressionRecorded += () => _analytics.LogImpression("app_open", AppOpenPlacement);
            ad.OnAdClicked += () => _analytics.LogClicked("app_open", AppOpenPlacement);
            ad.OnAdFullScreenContentOpened += () =>
            {
                _hasReportedAppOpenShow = true;
                _analytics.LogShown("app_open", AppOpenPlacement);
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                _analytics.LogClosed("app_open", AppOpenPlacement);
                HandleAdClosed();
            };
            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogError("App open ad failed to open full screen content with error : " + error);
                _analytics.LogShowFailed("app_open", AppOpenPlacement, error == null ? "open_failed" : error.GetCode().ToString());
                HandleAdFailed();
            };
        }

        private void HandleAdClosed()
        {
            _isShowing = false;
            MaybeReportAppOpenShown();
            ReleaseFullscreenGuard();
            DestroyAd();
            LoadAppOpenAd();
        }

        private void HandleAdFailed()
        {
            _isShowing = false;
            MaybeReportAppOpenShown();
            ReleaseFullscreenGuard();
            DestroyAd();
            LoadAppOpenAd();
        }

        private void MaybeReportAppOpenShown()
        {
            if (!_hasReportedAppOpenShow)
            {
                return;
            }

            _hasReportedAppOpenShow = false;
            _throttler.NotifyAdShown(AdType.AppOpen);
        }

        private void ReleaseFullscreenGuard()
        {
            if (!_isFullscreenGuardHeld)
            {
                return;
            }

            _isFullscreenGuardHeld = false;
            _fullscreenGate.ReleaseFullscreenAd();
        }

        private void ScheduleRetry()
        {
            var version = ++_retryVersion;
            if (_isRetryScheduled)
            {
                return;
            }

            _isRetryScheduled = true;
            RetryAfterDelay(version).Forget();
        }

        private async UniTaskVoid RetryAfterDelay(int version)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(LoadRetryDelaySeconds));

            if (!_isInitialized || version != _retryVersion)
            {
                _isRetryScheduled = false;
                return;
            }

            _isRetryScheduled = false;
            LoadAppOpenAd();
        }

        private sealed class AppOpenLifecycleListener : MonoBehaviour
        {
            private AdMobAppOpenAd _owner;
            private bool _shouldUseAppStateEventNotifier;
            private bool _isInBackground;
            private bool _hasNotifierEvent;

            public void Initialize(AdMobAppOpenAd owner)
            {
                _owner = owner;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
                _shouldUseAppStateEventNotifier = true;
#else
                _shouldUseAppStateEventNotifier = false;
#endif
            }

            private void OnDestroy()
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                if (_shouldUseAppStateEventNotifier)
                {
                    AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
                }
#endif
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                if (_shouldUseAppStateEventNotifier && _hasNotifierEvent)
                {
                    return;
                }

                SetBackgroundState(pauseStatus);
            }

            private void OnApplicationFocus(bool hasFocus)
            {
                if (_shouldUseAppStateEventNotifier && _hasNotifierEvent)
                {
                    return;
                }

                SetBackgroundState(!hasFocus);
            }

            private void SetBackgroundState(bool isBackground)
            {
                if (_owner == null || _isInBackground == isBackground)
                {
                    return;
                }

                _isInBackground = isBackground;
                if (isBackground)
                {
                    _owner.HandleBackground();
                }
                else
                {
                    _owner.HandleForeground();
                }
            }

            private void OnAppStateChanged(AppState state)
            {
                _hasNotifierEvent = true;
                if (state == AppState.Foreground)
                {
                    SetBackgroundState(false);
                }
                else if (state == AppState.Background)
                {
                    SetBackgroundState(true);
                }
            }
        }
    }
}
