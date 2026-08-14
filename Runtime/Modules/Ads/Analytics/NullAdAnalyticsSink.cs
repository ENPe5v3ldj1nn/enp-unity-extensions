namespace ENP.UnityExtensions.Ads
{
    public sealed class NullAdAnalyticsSink : IAdAnalyticsSink
    {
        public void LogOfferShown(string adType, string placement)
        {
        }

        public void LogLoadFailed(string adType, string placement, string reason, string errorCode,
            string errorDomain, string errorMessage)
        {
        }

        public void LogClicked(string adType, string placement)
        {
        }

        public void LogShowFailed(string adType, string placement, string reason)
        {
        }

        public void LogRewardGranted(string placement)
        {
        }

        public void LogRetryStopped(string adType, string reason)
        {
        }

        public void LogLoadSkippedThrottled(string adType)
        {
        }

        public void LogLoadExpired(string adType)
        {
        }
    }
}
