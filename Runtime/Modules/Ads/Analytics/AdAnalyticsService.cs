using System;
using System.Collections.Generic;
using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    public sealed class AdAnalyticsService
    {
        private readonly IAdAnalyticsSink _sink;
        private readonly HashSet<string> _reportedLoadFailureFingerprints = new(StringComparer.Ordinal);

        public AdAnalyticsService(IAdAnalyticsSink sink)
        {
            _sink = sink;
        }

        public void LogOfferShown(string adType, string placement)
        {
            _sink.LogOfferShown(adType, placement);
        }

        public void LogShowAttempt(string adType, string placement)
        {
            LogDebug($"[Ads][Analytics] show_attempt ad_type={adType} placement={placement}");
        }

        public void LogLoaded(string adType, string placement)
        {
            LogDebug($"[Ads][Analytics] loaded ad_type={adType} placement={placement}");
        }

        public void LogLoadFailed(string adType, string placement, string reason, string errorCode,
            string errorDomain, string errorMessage = null)
        {
            var safeErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "null" : errorCode;
            var safeErrorDomain = string.IsNullOrWhiteSpace(errorDomain) ? "null" : errorDomain;
            var failureClass = ClassifyLoadFailure(safeErrorCode, safeErrorDomain, errorMessage);
            var fingerprint = $"{adType}|{failureClass}|{safeErrorCode}|{safeErrorDomain}";

            if (!_reportedLoadFailureFingerprints.Add(fingerprint))
            {
                LogDebug($"[Ads][Analytics] load_failed_dedup ad_type={adType} placement={placement} class={failureClass} code={safeErrorCode} domain={safeErrorDomain}");
                return;
            }

            LogDebug($"[Ads][Analytics] load_failed ad_type={adType} placement={placement} reason={reason} class={failureClass} code={safeErrorCode} domain={safeErrorDomain}");
            _sink.LogLoadFailed(adType, placement, failureClass, safeErrorCode, safeErrorDomain, errorMessage);
        }

        public void LogShown(string adType, string placement)
        {
            LogDebug($"[Ads][Analytics] shown ad_type={adType} placement={placement}");
        }

        public void LogImpression(string adType, string placement)
        {
            LogDebug($"[Ads][Analytics] impression ad_type={adType} placement={placement}");
        }

        public void LogClicked(string adType, string placement)
        {
            _sink.LogClicked(adType, placement);
        }

        public void LogClosed(string adType, string placement)
        {
            LogDebug($"[Ads][Analytics] closed ad_type={adType} placement={placement}");
        }

        public void LogShowFailed(string adType, string placement, string reason)
        {
            _sink.LogShowFailed(adType, placement, reason);
        }

        public void LogRewardGranted(string placement)
        {
            _sink.LogRewardGranted(placement);
        }

        public void LogRetryStopped(string adType, string reason)
        {
            LogDebug($"[Ads][Analytics] retry_stopped ad_type={adType} reason={reason}");
            _sink.LogRetryStopped(adType, reason);
        }

        public void LogLoadSkippedThrottled(string adType)
        {
            LogDebug($"[Ads][Analytics] load_skipped_throttled ad_type={adType}");
            _sink.LogLoadSkippedThrottled(adType);
        }

        public void LogLoadExpired(string adType)
        {
            LogDebug($"[Ads][Analytics] loaded_expired ad_type={adType}");
            _sink.LogLoadExpired(adType);
        }

        private static string ClassifyLoadFailure(string errorCode, string errorDomain, string errorMessage)
        {
            var message = string.IsNullOrWhiteSpace(errorMessage) ? string.Empty : errorMessage.Trim().ToLowerInvariant();
            var domain = string.IsNullOrWhiteSpace(errorDomain) ? string.Empty : errorDomain.Trim().ToLowerInvariant();

            if (message.Contains("did not return an ad") || message.Contains("no fill") || message.Contains("mediation no fill"))
            {
                return "no_fill";
            }

            if (message.Contains("mediation waterfall") || message.Contains("mediation adapter"))
            {
                return "mediation_failure";
            }

            if (message.Contains("unable to resolve host") || message.Contains("connect") || message.Contains("network"))
            {
                return "network";
            }

            if (message.Contains("timed out"))
            {
                return "timeout";
            }

            if (message.Contains("ad unit id") || message.Contains("request type"))
            {
                return "invalid_request";
            }

            if (message.Contains("403"))
            {
                return "forbidden";
            }

            if (errorCode == "9")
            {
                return "no_fill";
            }

            if (errorCode == "0")
            {
                return domain.Contains("google.android.gms.ads") ? "internal_error" : "sdk_error";
            }

            if (domain.Contains("google.android.gms.ads"))
            {
                return "sdk_error";
            }

            return "other";
        }

        private static void LogDebug(string message)
        {
#if !ENP_ADS_RELEASE
            Debug.Log(message);
#endif
        }
    }
}
