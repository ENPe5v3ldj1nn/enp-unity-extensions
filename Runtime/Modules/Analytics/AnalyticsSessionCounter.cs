using System;
using ENP.UnityExtensions.Runtime;
using UnityEngine;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class AnalyticsSessionCounter
    {
        private const string StorageDirectory = "Analytics";
        private const string StorageFileName = "session_counter.json";

        private bool _isSessionStarted;

        public int SessionNumber { get; private set; }

        public void BeginSession()
        {
            if (_isSessionStarted)
                return;

            _isSessionStarted = true;
            SessionNumber = LoadSessionNumber() + 1;
            SaveSessionNumber(SessionNumber);
        }

        private static int LoadSessionNumber()
        {
            try
            {
                var data = Storage.Load<SessionCounterData>(StorageDirectory, StorageFileName);
                return data?.SessionNumber ?? 0;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Analytics] Failed to read session counter: {exception.Message}");
                return 0;
            }
        }

        private static void SaveSessionNumber(int sessionNumber)
        {
            try
            {
                Storage.Save(StorageDirectory, StorageFileName, new SessionCounterData
                {
                    SessionNumber = sessionNumber
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Analytics] Failed to persist session counter: {exception.Message}");
            }
        }

        private sealed class SessionCounterData
        {
            public int SessionNumber;
        }
    }
}
