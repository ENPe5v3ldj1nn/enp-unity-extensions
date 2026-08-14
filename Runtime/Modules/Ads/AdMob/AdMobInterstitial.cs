using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    public sealed class AdMobInterstitial
    {
        private const int MinCooldownSeconds = 1;
        private const int MaxBackoffSeconds = 300;
        private const int MaxConsecutiveLoadFailures = 6;
        private const int ShowSettlingDelayMs = 800;

        private readonly AdAnalyticsService _analytics;
        private readonly AdThrottleService _throttler;
        private readonly IFullscreenAdGate _fullscreenGate;

        private bool _isInitialized;
        private bool _isLoading;
        private bool _isShowing;
        private int _backoffSeconds = 1;
        private int _consecutiveLoadFailures;
        private bool _isRetryHalted;
        private DateTime _nextAllowedLoadTime = DateTime.MinValue;
        private string _interstitialId;
        private InterstitialAd _interstitialAd;
        private Action _onAdShowed;
        private bool _hasReportedInterstitialShow;
        private string _currentPlacement = "unknown";
        private bool _isRetryScheduled;
        private int _retryVersion;

        public AdMobInterstitial(AdAnalyticsService analytics, AdThrottleService throttler, IFullscreenAdGate fullscreenGate)
        {
            _analytics = analytics;
            _throttler = throttler;
            _fullscreenGate = fullscreenGate;
        }

        public void Initialize(string interstitialId)
        {
            _isInitialized = true;
            _interstitialId = interstitialId;
            _backoffSeconds = 1;
            _nextAllowedLoadTime = DateTime.MinValue;
            _isLoading = false;
            _isShowing = false;
            _onAdShowed = null;
            _isRetryScheduled = false;
            _retryVersion = 0;
            _consecutiveLoadFailures = 0;
            _isRetryHalted = false;
            _hasReportedInterstitialShow = false;
            DestroyAd();
            EnsureLoaded();
        }

        public bool CanShowNow()
        {
            ResumeLoadingIfHalted();

            if (!_isInitialized || _isShowing)
            {
                EnsureLoaded();
                return false;
            }

            if (!IsAdReady)
            {
                EnsureLoaded();
                return false;
            }

            return true;
        }

        public bool ShowInterstitialAd(Action onAdShowed = null, string placement = "unknown")
        {
            _currentPlacement = string.IsNullOrWhiteSpace(placement) ? "unknown" : placement;
            _analytics.LogShowAttempt("interstitial", _currentPlacement);

            ResumeLoadingIfHalted();

            if (!_isInitialized)
            {
                _analytics.LogShowFailed("interstitial", _currentPlacement, "not_initialized");
                return false;
            }

            if (_isShowing)
            {
                _analytics.LogShowFailed("interstitial", _currentPlacement, "already_showing");
                return false;
            }

            if (!IsAdReady)
            {
                _analytics.LogShowFailed("interstitial", _currentPlacement, "not_ready");
                EnsureLoaded();
                return false;
            }

            if (!_fullscreenGate.TryAcquireFullscreenAd())
            {
                _analytics.LogShowFailed("interstitial", _currentPlacement, "fullscreen_guard");
                return false;
            }

            _onAdShowed = onAdShowed;
            _isShowing = true;
            _hasReportedInterstitialShow = false;
            ShowAfterSettlingDelay().Forget();
            return true;
        }

        public bool ShowEditorMockInterstitial(Action onAdShowed = null, string placement = "unknown")
        {
            _currentPlacement = string.IsNullOrWhiteSpace(placement) ? "unknown" : placement;
            _analytics.LogShowAttempt("interstitial", _currentPlacement);

            if (_isShowing)
            {
                _analytics.LogShowFailed("interstitial", _currentPlacement, "already_showing");
                return false;
            }

            if (!_fullscreenGate.TryAcquireFullscreenAd())
            {
                _analytics.LogShowFailed("interstitial", _currentPlacement, "fullscreen_guard");
                return false;
            }

            _onAdShowed = onAdShowed;
            _isShowing = true;
            _hasReportedInterstitialShow = true;
            RunEditorMockShow().Forget();
            return true;
        }

        public void Preload()
        {
            ResumeLoadingIfHalted();
            EnsureLoaded();
        }

        private bool IsAdReady => _interstitialAd != null && _interstitialAd.CanShowAd();

        private void EnsureLoaded()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_isLoading || _isShowing || IsAdReady)
            {
                return;
            }

            if (DateTime.UtcNow < _nextAllowedLoadTime)
            {
                ScheduleRetry();
                return;
            }

            if (!_throttler.IsInterstitialLoadWorthwhile())
            {
                _analytics.LogLoadSkippedThrottled("interstitial");
                return;
            }

            LoadInterstitialAd();
        }

        private void LoadInterstitialAd()
        {
            _isLoading = true;
            _nextAllowedLoadTime = DateTime.UtcNow.AddSeconds(MinCooldownSeconds);
            DestroyAd();

            var adRequest = new AdRequest();
            InterstitialAd.Load(_interstitialId, adRequest,
                (InterstitialAd ad, LoadAdError error) =>
                {
                    _isLoading = false;
                    if (error != null || ad == null)
                    {
                        var errorCode = error == null ? "null" : error.GetCode().ToString();
                        var errorDomain = error == null ? "null" : error.GetDomain();

                        Debug.LogError($"Interstitial load failed. code={errorCode}, domain={errorDomain}, message={error?.GetMessage()}, response={error?.GetResponseInfo()}");
                        _analytics.LogLoadFailed("interstitial", "cache", "load_error", errorCode, errorDomain, error?.GetMessage());

                        _consecutiveLoadFailures++;
                        if (_consecutiveLoadFailures >= MaxConsecutiveLoadFailures)
                        {
                            _isRetryHalted = true;
                            _analytics.LogRetryStopped("interstitial", "max_consecutive_failures");
                            return;
                        }

                        ScheduleBackoff();
                        ScheduleRetry();
                        return;
                    }

                    _interstitialAd = ad;
                    _analytics.LogLoaded("interstitial", "cache");
                    RegisterEventHandlers(_interstitialAd);
                    RegisterReloadHandler(_interstitialAd);
                    ResetBackoff();
                });
        }

        private void DestroyAd()
        {
            if (_interstitialAd == null)
            {
                return;
            }

            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        private void ScheduleBackoff()
        {
            var delaySeconds = _backoffSeconds < MinCooldownSeconds ? MinCooldownSeconds : _backoffSeconds;
            _nextAllowedLoadTime = DateTime.UtcNow.AddSeconds(delaySeconds);
            _backoffSeconds = Math.Min(_backoffSeconds * 2, MaxBackoffSeconds);
        }

        private void ResetBackoff()
        {
            _backoffSeconds = 1;
            _consecutiveLoadFailures = 0;
            _isRetryHalted = false;
        }

        private void ResumeLoadingIfHalted()
        {
            if (!_isRetryHalted)
            {
                return;
            }

            _isRetryHalted = false;
            _consecutiveLoadFailures = 0;
            EnsureLoaded();
        }

        private async UniTaskVoid ShowAfterSettlingDelay()
        {
            await UniTask.Delay(ShowSettlingDelayMs);

            if (!_isShowing)
            {
                return;
            }

            if (!IsAdReady)
            {
                _analytics.LogShowFailed("interstitial", _currentPlacement, "settling_aborted");
                CompleteShow();
                _fullscreenGate.ReleaseFullscreenAd();
                EnsureLoaded();
                return;
            }

            _interstitialAd.Show();
        }

        private void HandleAdClosedOrFailed()
        {
            MaybeReportInterstitialShown();
            DestroyAd();
            CompleteShow();
            _fullscreenGate.ReleaseFullscreenAd();
            EnsureLoaded();
        }

        private void CompleteShow()
        {
            if (!_isShowing)
            {
                return;
            }

            _isShowing = false;
            var callback = _onAdShowed;
            _onAdShowed = null;
            callback?.Invoke();
        }

        private void RegisterEventHandlers(InterstitialAd interstitialAd)
        {
            interstitialAd.OnAdImpressionRecorded += () => _analytics.LogImpression("interstitial", _currentPlacement);
            interstitialAd.OnAdClicked += () => _analytics.LogClicked("interstitial", _currentPlacement);
            interstitialAd.OnAdFullScreenContentOpened += () =>
            {
                _hasReportedInterstitialShow = true;
                _analytics.LogShown("interstitial", _currentPlacement);
            };
            interstitialAd.OnAdFullScreenContentClosed += () => _analytics.LogClosed("interstitial", _currentPlacement);
            interstitialAd.OnAdFullScreenContentFailed += error =>
                _analytics.LogShowFailed("interstitial", _currentPlacement, error == null ? "open_failed" : error.GetCode().ToString());
        }

        private void RegisterReloadHandler(InterstitialAd interstitialAd)
        {
            interstitialAd.OnAdFullScreenContentClosed += HandleAdClosedOrFailed;
            interstitialAd.OnAdFullScreenContentFailed += _ => HandleAdClosedOrFailed();
        }

        private void MaybeReportInterstitialShown()
        {
            if (!_hasReportedInterstitialShow)
            {
                return;
            }

            _hasReportedInterstitialShow = false;
            _throttler.NotifyAdShown(AdType.Interstitial);
        }

        private async UniTaskVoid RunEditorMockShow()
        {
            _analytics.LogShown("interstitial", _currentPlacement);
            _analytics.LogImpression("interstitial", _currentPlacement);

            await UniTask.Delay(1200);

            _analytics.LogClosed("interstitial", _currentPlacement);

            MaybeReportInterstitialShown();
            CompleteShow();
            _fullscreenGate.ReleaseFullscreenAd();
        }

        private void ScheduleRetry()
        {
            if (_isRetryScheduled)
            {
                return;
            }

            var version = ++_retryVersion;
            _isRetryScheduled = true;
            RetryAfterDelay(version).Forget();
        }

        private async UniTaskVoid RetryAfterDelay(int version)
        {
            while (_isInitialized && version == _retryVersion)
            {
                var delayMs = (int)Math.Ceiling((_nextAllowedLoadTime - DateTime.UtcNow).TotalMilliseconds);
                if (delayMs < 0)
                {
                    delayMs = 0;
                }

                await UniTask.Delay(delayMs);

                if (!_isInitialized || version != _retryVersion)
                {
                    break;
                }

                _isRetryScheduled = false;
                EnsureLoaded();
                break;
            }

            if (version == _retryVersion)
            {
                _isRetryScheduled = false;
            }
        }
    }
}
