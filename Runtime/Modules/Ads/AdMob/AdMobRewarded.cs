using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    public sealed class AdMobRewarded
    {
        private const int MinCooldownSeconds = 1;
        private const int MaxBackoffSeconds = 300;
        private const int MaxConsecutiveLoadFailures = 6;

        private readonly AdAnalyticsService _analytics;
        private readonly AdThrottleService _throttler;
        private readonly IFullscreenAdGate _fullscreenGate;

        private bool _isInitialized;
        private bool _isLoading;
        private bool _isShowing;
        private bool _hasUserEarnedReward;
        private int _backoffSeconds = 1;
        private int _consecutiveLoadFailures;
        private bool _isRetryHalted;
        private DateTime _nextAllowedLoadTime = DateTime.MinValue;
        private string _rewardedId;
        private RewardedAd _rewardedAd;
        private Action _onAdShowed;
        private Action _onUserEarnedReward;
        private bool _hasReportedRewardedShow;
        private string _currentPlacement = "unknown";

        private bool _isRetryScheduled;
        private int _retryVersion;

        public AdMobRewarded(AdAnalyticsService analytics, AdThrottleService throttler, IFullscreenAdGate fullscreenGate)
        {
            _analytics = analytics;
            _throttler = throttler;
            _fullscreenGate = fullscreenGate;
        }

        public bool IsReady => _rewardedAd != null && _rewardedAd.CanShowAd();

        public void Initialize(string rewardedId)
        {
            _isInitialized = true;
            _rewardedId = rewardedId;
            _backoffSeconds = 1;
            _nextAllowedLoadTime = DateTime.MinValue;
            _isLoading = false;
            _isShowing = false;
            _hasUserEarnedReward = false;
            _onAdShowed = null;
            _onUserEarnedReward = null;
            _isRetryScheduled = false;
            _retryVersion = 0;
            _consecutiveLoadFailures = 0;
            _isRetryHalted = false;
            _hasReportedRewardedShow = false;
            DestroyAd();
            EnsureLoaded();
        }

        public void Warmup()
        {
            ResumeLoadingIfHalted();
            EnsureLoaded();
        }

        public bool ShowRewardedAd(Action onAdShowed = null, Action onUserEarnedReward = null, string placement = "unknown")
        {
            _currentPlacement = string.IsNullOrWhiteSpace(placement) ? "unknown" : placement;
            _analytics.LogShowAttempt("rewarded", _currentPlacement);

            ResumeLoadingIfHalted();

            if (!_isInitialized)
            {
                Debug.LogWarning("Rewarded ad is not initialized.");
                _analytics.LogShowFailed("rewarded", _currentPlacement, "not_initialized");
                return false;
            }

            if (_isShowing)
            {
                _analytics.LogShowFailed("rewarded", _currentPlacement, "already_showing");
                return false;
            }

            if (!IsReady)
            {
                _analytics.LogShowFailed("rewarded", _currentPlacement, "not_ready");
                return false;
            }

            if (!_fullscreenGate.TryAcquireFullscreenAd())
            {
                _analytics.LogShowFailed("rewarded", _currentPlacement, "fullscreen_guard");
                return false;
            }

            _onAdShowed = onAdShowed;
            _onUserEarnedReward = onUserEarnedReward;
            _hasUserEarnedReward = false;
            _isShowing = true;
            _hasReportedRewardedShow = false;
            _rewardedAd.Show(_ => UniTask.Post(HandleUserEarnedReward));
            return true;
        }

        private void EnsureLoaded()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_isLoading || _isShowing || IsReady)
            {
                return;
            }

            if (DateTime.UtcNow < _nextAllowedLoadTime)
            {
                ScheduleRetry();
                return;
            }

            LoadRewardedAd();
        }

        private void LoadRewardedAd()
        {
            _isLoading = true;
            _nextAllowedLoadTime = DateTime.UtcNow.AddSeconds(MinCooldownSeconds);
            DestroyAd();

            var adRequest = new AdRequest();
            RewardedAd.Load(_rewardedId, adRequest,
                (RewardedAd ad, LoadAdError error) =>
                {
                    _isLoading = false;
                    if (error != null || ad == null)
                    {
                        var errorCode = error == null ? "null" : error.GetCode().ToString();
                        var errorDomain = error == null ? "null" : error.GetDomain();

                        Debug.LogError($"Rewarded load failed. code={errorCode}, domain={errorDomain}, message={error?.GetMessage()}, response={error?.GetResponseInfo()}");
                        _analytics.LogLoadFailed("rewarded", "cache", "load_error", errorCode, errorDomain, error?.GetMessage());

                        _consecutiveLoadFailures++;
                        if (_consecutiveLoadFailures >= MaxConsecutiveLoadFailures)
                        {
                            _isRetryHalted = true;
                            _analytics.LogRetryStopped("rewarded", "max_consecutive_failures");
                            return;
                        }

                        ScheduleBackoff();
                        ScheduleRetry();
                        return;
                    }

                    _rewardedAd = ad;
                    _analytics.LogLoaded("rewarded", "cache");
                    RegisterEventHandlers(_rewardedAd);
                    RegisterReloadHandler(_rewardedAd);
                    ResetBackoff();
                });
        }

        private void DestroyAd()
        {
            if (_rewardedAd == null)
            {
                return;
            }

            _rewardedAd.Destroy();
            _rewardedAd = null;
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

        private void HandleAdClosedOrFailed()
        {
            MaybeReportRewardedShown();
            DestroyAd();
            CompleteShow();
            _fullscreenGate.ReleaseFullscreenAd();
            EnsureLoaded();
        }

        private void HandleUserEarnedReward()
        {
            if (_hasUserEarnedReward)
            {
                return;
            }

            _hasUserEarnedReward = true;
            _analytics.LogRewardGranted(_currentPlacement);
            var callback = _onUserEarnedReward;
            _onUserEarnedReward = null;
            callback?.Invoke();
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

        private void RegisterEventHandlers(RewardedAd rewardedAd)
        {
            rewardedAd.OnAdImpressionRecorded += () => _analytics.LogImpression("rewarded", _currentPlacement);
            rewardedAd.OnAdClicked += () => _analytics.LogClicked("rewarded", _currentPlacement);
            rewardedAd.OnAdFullScreenContentOpened += () =>
            {
                _hasReportedRewardedShow = true;
                _analytics.LogShown("rewarded", _currentPlacement);
            };
            rewardedAd.OnAdFullScreenContentClosed += () => _analytics.LogClosed("rewarded", _currentPlacement);
            rewardedAd.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogError("Rewarded ad failed to open full screen content with error : " + error);
                _analytics.LogShowFailed("rewarded", _currentPlacement, error == null ? "open_failed" : error.GetCode().ToString());
            };
        }

        private void RegisterReloadHandler(RewardedAd rewardedAd)
        {
            rewardedAd.OnAdFullScreenContentClosed += () => UniTask.Post(HandleAdClosedOrFailed);
            rewardedAd.OnAdFullScreenContentFailed += _ => UniTask.Post(HandleAdClosedOrFailed);
        }

        private void MaybeReportRewardedShown()
        {
            if (!_hasReportedRewardedShow)
            {
                return;
            }

            _hasReportedRewardedShow = false;
            _throttler.NotifyAdShown(AdType.Rewarded);
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
