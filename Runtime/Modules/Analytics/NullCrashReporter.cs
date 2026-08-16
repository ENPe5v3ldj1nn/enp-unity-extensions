using System;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class NullCrashReporter : ICrashReporter
    {
        public void Initialize()
        {
        }

        public void SetCollectionEnabled(bool isEnabled)
        {
        }

        public void SetUserId(string userId)
        {
        }

        public void SetCustomKey(string key, string value)
        {
        }

        public void Log(string message)
        {
        }

        public void LogException(Exception exception)
        {
        }
    }
}
