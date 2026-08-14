namespace ENP.UnityExtensions.Ads
{
    public interface IFullscreenAdGate
    {
        bool IsFullscreenAdShowing { get; }
        bool TryAcquireFullscreenAd();
        void ReleaseFullscreenAd();
    }
}
