using System;

namespace ENP.UnityExtensions.Analytics
{
    public interface ICrashReporter
    {
        void Initialize();
        void SetCollectionEnabled(bool isEnabled);
        void SetUserId(string userId);
        void SetCustomKey(string key, string value);
        void Log(string message);
        void LogException(Exception exception);
    }
}
