using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Ump.Api;

namespace ENP.UnityExtensions.Ads
{
    public sealed class AdReadinessCoordinator
    {
        private readonly ConsentService _consentService;
        private readonly AdMobService _adMobService;
        private readonly IosAttAuthorizationRequester _attRequester;
        private readonly AdMobConfig _config;

        private bool _isAdsReady;
        private bool _hasCheckedMainMenuThisSession;
        private UniTaskCompletionSource<bool> _adsReadyTcs;

        public AdReadinessCoordinator(ConsentService consentService, AdMobService adMobService,
            IosAttAuthorizationRequester attRequester, AdMobConfig config)
        {
            _consentService = consentService;
            _adMobService = adMobService;
            _attRequester = attRequester;
            _config = config;
        }

        public bool IsAdsReady => _isAdsReady;

        public UniTask<bool> AdsReadyTask => _adsReadyTcs != null ? _adsReadyTcs.Task : UniTask.FromResult(_isAdsReady);

        public UniTask WarmupAtStartupAsync() => _consentService.WarmupAtStartupAsync();

        public UniTask<bool> EnsureAdsReadyAsync(bool allowUi)
        {
            if (_isAdsReady)
            {
                return UniTask.FromResult(true);
            }

            if (_adsReadyTcs != null)
            {
                return _adsReadyTcs.Task;
            }

            _adsReadyTcs = new UniTaskCompletionSource<bool>();
            var tcs = _adsReadyTcs;
            EnsureAdsReadyInternalAsync(allowUi, tcs).Forget();
            return tcs.Task;
        }

        public UniTask TryShowConsentFromMainMenuOncePerSessionAsync()
        {
            if (_hasCheckedMainMenuThisSession)
            {
                return UniTask.CompletedTask;
            }

            _hasCheckedMainMenuThisSession = true;
            return EnsureAdsReadyAsync(true).AsUniTask();
        }

        public void ResetSessionState()
        {
            _isAdsReady = false;
            _hasCheckedMainMenuThisSession = false;
            _consentService.ResetSessionState();

            _adsReadyTcs?.TrySetResult(false);
            _adsReadyTcs = null;
        }

        public void ForgetAdReady()
        {
            _isAdsReady = false;
            _adsReadyTcs?.TrySetResult(false);
            _adsReadyTcs = null;
        }

        private async UniTaskVoid EnsureAdsReadyInternalAsync(bool allowUi, UniTaskCompletionSource<bool> tcs)
        {
            if (_consentService.IsEditorAdsBypassEnabled)
            {
                _isAdsReady = true;

                if (_adsReadyTcs == tcs)
                {
                    _adsReadyTcs = null;
                }

                tcs.TrySetResult(true);
                return;
            }

            var ready = false;

            try
            {
                await _consentService.EnsureConsentFlowAsync(allowUi);

                if (ConsentInformation.CanRequestAds())
                {
                    await _attRequester.RequestIfNeededAsync();
                    ready = await _adMobService.EnsureInitializedAsync();
                    ready = ready && ConsentInformation.CanRequestAds();
                }
            }
            catch (Exception)
            {
                if (ConsentInformation.CanRequestAds())
                {
                    try
                    {
                        ready = await _adMobService.EnsureInitializedAsync();
                        ready = ready && ConsentInformation.CanRequestAds();
                    }
                    catch (Exception)
                    {
                        ready = false;
                    }
                }
            }

            _isAdsReady = ready;

            if (ready && _config.ShouldAutoInitializeAppOpen)
            {
                _adMobService.InitializeAppOpenIfReady();
            }

            if (_adsReadyTcs == tcs)
            {
                _adsReadyTcs = null;
            }

            tcs.TrySetResult(ready);
        }
    }
}
