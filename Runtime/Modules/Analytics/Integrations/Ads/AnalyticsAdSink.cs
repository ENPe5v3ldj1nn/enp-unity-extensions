using ENP.UnityExtensions.Ads;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class AnalyticsAdSink : IAdAnalyticsSink
    {
        private const string OfferShownEventName = "ad_offer_shown";
        private const string LoadFailedEventName = "ad_load_failed";
        private const string ClickedEventName = "ad_clicked";
        private const string ShowFailedEventName = "ad_show_failed";
        private const string RewardGrantedEventName = "ad_reward_granted";
        private const string RetryStoppedEventName = "ad_retry_stopped";
        private const string LoadSkippedThrottledEventName = "ad_load_skipped_throttled";
        private const string LoadExpiredEventName = "ad_load_expired";

        private const string AdTypeKey = "ad_type";
        private const string PlacementKey = "placement";
        private const string ReasonKey = "reason";
        private const string ErrorCodeKey = "error_code";
        private const string ErrorDomainKey = "error_domain";
        private const string ErrorMessageKey = "error_message";

        private readonly AnalyticsService _analyticsService;

        public AnalyticsAdSink(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public void LogOfferShown(string adType, string placement)
        {
            _analyticsService.LogEvent(OfferShownEventName,
                new AnalyticsParam(AdTypeKey, adType),
                new AnalyticsParam(PlacementKey, placement));
        }

        public void LogLoadFailed(string adType, string placement, string reason, string errorCode, string errorDomain,
            string errorMessage)
        {
            _analyticsService.LogEvent(LoadFailedEventName,
                new AnalyticsParam(AdTypeKey, adType),
                new AnalyticsParam(PlacementKey, placement),
                new AnalyticsParam(ReasonKey, reason),
                new AnalyticsParam(ErrorCodeKey, errorCode),
                new AnalyticsParam(ErrorDomainKey, errorDomain),
                new AnalyticsParam(ErrorMessageKey, errorMessage));
        }

        public void LogClicked(string adType, string placement)
        {
            _analyticsService.LogEvent(ClickedEventName,
                new AnalyticsParam(AdTypeKey, adType),
                new AnalyticsParam(PlacementKey, placement));
        }

        public void LogShowFailed(string adType, string placement, string reason)
        {
            _analyticsService.LogEvent(ShowFailedEventName,
                new AnalyticsParam(AdTypeKey, adType),
                new AnalyticsParam(PlacementKey, placement),
                new AnalyticsParam(ReasonKey, reason));
        }

        public void LogRewardGranted(string placement)
        {
            _analyticsService.LogEvent(RewardGrantedEventName,
                new AnalyticsParam(PlacementKey, placement));
        }

        public void LogRetryStopped(string adType, string reason)
        {
            _analyticsService.LogEvent(RetryStoppedEventName,
                new AnalyticsParam(AdTypeKey, adType),
                new AnalyticsParam(ReasonKey, reason));
        }

        public void LogLoadSkippedThrottled(string adType)
        {
            _analyticsService.LogEvent(LoadSkippedThrottledEventName,
                new AnalyticsParam(AdTypeKey, adType));
        }

        public void LogLoadExpired(string adType)
        {
            _analyticsService.LogEvent(LoadExpiredEventName,
                new AnalyticsParam(AdTypeKey, adType));
        }
    }
}
