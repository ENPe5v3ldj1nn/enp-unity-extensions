using System;

namespace ENP.UnityExtensions.Analytics
{
    public interface IAnalyticsBackend
    {
        bool IsReady { get; }

        event Action Initialized;

        void Initialize();
        void SetCollectionEnabled(bool isEnabled);
        void SetUserId(string userId);
        void SetUserProperty(string name, string value);
        void LogEvent(AnalyticsEvent analyticsEvent);
    }
}
