using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    public sealed class ConsentService
    {
        private readonly AdsConfig _config;

        private UniTaskCompletionSource<bool> _updateTcs;
        private UniTaskCompletionSource<bool> _formTcs;
        private bool _hasAttemptedConsentUpdate;
        private bool _shouldForceAdsInEditor;

        public ConsentService(AdsConfig config)
        {
            _config = config;
        }

        public bool CanRequestAds => ConsentInformation.CanRequestAds();
        public bool IsEditorAdsBypassEnabled => Application.isEditor && _shouldForceAdsInEditor;

        public void SetEditorAdsEnabled(bool enable)
        {
            _shouldForceAdsInEditor = enable;
        }

        public UniTask WarmupAtStartupAsync()
        {
            if (!IsSupportedPlatform() || IsEditorAdsBypassEnabled)
            {
                return UniTask.CompletedTask;
            }

            return UpdateConsentInfoAsync().AsUniTask();
        }

        public UniTask<bool> EnsureConsentFlowAsync(bool allowUi)
        {
            if (!IsSupportedPlatform())
            {
                return UniTask.FromResult(false);
            }

            if (IsEditorAdsBypassEnabled)
            {
                return UniTask.FromResult(true);
            }

            return EnsureConsentFlowInternalAsync(allowUi);
        }

        public void ShowPrivacyOptions(Action<bool> onClosed)
        {
            if (IsEditorAdsBypassEnabled)
            {
                onClosed?.Invoke(true);
                return;
            }

            if (!IsSupportedPlatform())
            {
                onClosed?.Invoke(false);
                return;
            }

            ConsentForm.ShowPrivacyOptionsForm(error => onClosed?.Invoke(error == null));
        }

        public void ResetSessionState()
        {
            _hasAttemptedConsentUpdate = false;

            _updateTcs?.TrySetResult(false);
            _formTcs?.TrySetResult(false);

            _updateTcs = null;
            _formTcs = null;
        }

        private async UniTask<bool> EnsureConsentFlowInternalAsync(bool allowUi)
        {
            await UpdateConsentInfoAsync();

            if (allowUi)
            {
                await ShowConsentFormIfRequiredAsync();
            }

            return ConsentInformation.CanRequestAds();
        }

        private UniTask<bool> UpdateConsentInfoAsync()
        {
            if (_hasAttemptedConsentUpdate)
            {
                return UniTask.FromResult(true);
            }

            if (_updateTcs != null)
            {
                return _updateTcs.Task;
            }

            _updateTcs = new UniTaskCompletionSource<bool>();
            var tcs = _updateTcs;

            var parameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = _config.TagForUnderAgeOfConsent
            };

#if !ENP_ADS_RELEASE
            parameters.ConsentDebugSettings = BuildDebugSettings();
#endif

            ConsentInformation.Update(parameters, error =>
            {
                _hasAttemptedConsentUpdate = true;

                if (_updateTcs == tcs)
                {
                    _updateTcs = null;
                }

                LogConsentDebug(
                    $"UMP Update: status={ConsentInformation.ConsentStatus}, " +
                    $"canRequest={ConsentInformation.CanRequestAds()}, " +
                    $"error={(error == null ? "none" : error.ToString())}");

                tcs.TrySetResult(error == null);
            });

            return tcs.Task;
        }

        private UniTask<bool> ShowConsentFormIfRequiredAsync()
        {
            if (_formTcs != null)
            {
                return _formTcs.Task;
            }

            _formTcs = new UniTaskCompletionSource<bool>();
            var tcs = _formTcs;

            ConsentForm.LoadAndShowConsentFormIfRequired(error =>
            {
                if (_formTcs == tcs)
                {
                    _formTcs = null;
                }

                LogConsentDebug(
                    $"UMP Form: success={(error == null)}, " +
                    $"error={(error == null ? "none" : error.ToString())}, " +
                    $"canRequest={ConsentInformation.CanRequestAds()}");

                tcs.TrySetResult(error == null);
            });

            return tcs.Task;
        }

        private static void LogConsentDebug(string message)
        {
#if !ENP_ADS_RELEASE
            Debug.Log(message);
#endif
        }

        private bool IsSupportedPlatform()
        {
            if (Application.isEditor)
            {
                return _shouldForceAdsInEditor;
            }

            return Application.platform == RuntimePlatform.Android
                   || Application.platform == RuntimePlatform.IPhonePlayer;
        }

#if !ENP_ADS_RELEASE
        private ConsentDebugSettings BuildDebugSettings()
        {
            var debugSettings = new ConsentDebugSettings();
            if (_config.IsDebugGeographyEea)
            {
                debugSettings.DebugGeography = DebugGeography.EEA;
            }

            if (_config.TestDeviceHashedIds is { Length: > 0 })
            {
                debugSettings.TestDeviceHashedIds = new List<string>(_config.TestDeviceHashedIds);
            }

            return debugSettings;
        }
#endif
    }
}
