namespace ENP.UnityExtensions.Ads
{
    public interface IAdSessionState
    {
        bool IsFirstLaunch { get; }
        bool IsFirstInstallSession { get; }
    }
}
