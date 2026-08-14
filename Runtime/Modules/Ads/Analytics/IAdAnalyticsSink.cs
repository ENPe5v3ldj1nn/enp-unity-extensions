namespace ENP.UnityExtensions.Ads
{
    public interface IAdAnalyticsSink
    {
        void LogOfferShown(string adType, string placement);

        void LogLoadFailed(string adType, string placement, string reason, string errorCode, string errorDomain,
            string errorMessage);

        void LogClicked(string adType, string placement);
        void LogShowFailed(string adType, string placement, string reason);
        void LogRewardGranted(string placement);
        void LogRetryStopped(string adType, string reason);
        void LogLoadSkippedThrottled(string adType);
        void LogLoadExpired(string adType);
    }
}
