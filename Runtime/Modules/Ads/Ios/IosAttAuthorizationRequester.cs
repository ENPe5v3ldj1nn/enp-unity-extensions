using System;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    public sealed class IosAttAuthorizationRequester
    {
        private const int NotDeterminedStatus = 0;
        private const int AuthorizedStatus = 3;
        private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

        private readonly string _label;

        private UniTaskCompletionSource<bool> _requestTcs;
        private bool _requestFinished;

        public IosAttAuthorizationRequester(string label = null)
        {
            _label = string.IsNullOrWhiteSpace(label) ? Application.productName : label;
        }

        public async UniTask RequestIfNeededAsync()
        {
            if (!IsSupportedPlatform() || _requestFinished)
            {
                return;
            }

            if (_requestTcs != null)
            {
                await _requestTcs.Task;
                return;
            }

            _requestTcs = new UniTaskCompletionSource<bool>();

            try
            {
                var status = GetAuthorizationStatus();
                if (status != NotDeterminedStatus)
                {
                    _requestFinished = true;
                    return;
                }

                await UniTask.Delay(RequestDelay);
                RequestAuthorization();

                var finishedIndex = await UniTask.WhenAny(
                    UniTask.WaitUntil(() => GetAuthorizationStatus() != NotDeterminedStatus),
                    UniTask.Delay(RequestTimeout));

                if (finishedIndex == 1)
                {
                    Debug.LogWarning($"[Ads][ATT][{_label}] Tracking authorization request timed out.");
                }

                _requestFinished = true;
            }
            finally
            {
                _requestTcs.TrySetResult(true);
                _requestTcs = null;
            }
        }

        private static bool IsSupportedPlatform()
        {
            return !Application.isEditor && Application.platform == RuntimePlatform.IPhonePlayer;
        }

        private static int GetAuthorizationStatus()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return NeuroDashTrackingAuthorizationGetStatus();
#else
            return AuthorizedStatus;
#endif
        }

        private static void RequestAuthorization()
        {
#if UNITY_IOS && !UNITY_EDITOR
            NeuroDashTrackingAuthorizationRequest();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int NeuroDashTrackingAuthorizationGetStatus();

        [DllImport("__Internal")]
        private static extern void NeuroDashTrackingAuthorizationRequest();
#endif
    }
}
