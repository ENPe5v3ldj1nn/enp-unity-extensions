using System;
using System.Collections.Generic;
using ENP.UnityExtensions.Analytics;
using Firebase.Crashlytics;

namespace ENP.UnityExtensions.Firebase
{
    public sealed class FirebaseCrashReporter : ICrashReporter, IDisposable
    {
        private readonly FirebaseBootstrap _bootstrap;
        private readonly Dictionary<string, string> _pendingCustomKeys = new(StringComparer.Ordinal);

        private bool _isCollectionEnabled = true;
        private bool _isSubscribed;
        private string _pendingUserId;

        public FirebaseCrashReporter(FirebaseBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
        }

        public void Initialize()
        {
            if (_bootstrap.IsReady)
            {
                ApplyPendingState();
                return;
            }

            if (!_isSubscribed)
            {
                _isSubscribed = true;
                _bootstrap.Initialized += OnBootstrapInitialized;
            }

            _bootstrap.Initialize();
        }

        public void SetCollectionEnabled(bool isEnabled)
        {
            _isCollectionEnabled = isEnabled;

            if (_bootstrap.IsReady)
                Crashlytics.IsCrashlyticsCollectionEnabled = isEnabled;
        }

        public void SetUserId(string userId)
        {
            if (!_bootstrap.IsReady)
            {
                _pendingUserId = userId;
                return;
            }

            Crashlytics.SetUserId(userId);
        }

        public void SetCustomKey(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!_bootstrap.IsReady)
            {
                _pendingCustomKeys[key] = value;
                return;
            }

            Crashlytics.SetCustomKey(key, value ?? string.Empty);
        }

        public void Log(string message)
        {
            if (!_bootstrap.IsReady || string.IsNullOrEmpty(message))
                return;

            Crashlytics.Log(message);
        }

        public void LogException(Exception exception)
        {
            if (!_bootstrap.IsReady || exception == null)
                return;

            Crashlytics.LogException(exception);
        }

        public void Dispose()
        {
            if (!_isSubscribed)
                return;

            _isSubscribed = false;
            _bootstrap.Initialized -= OnBootstrapInitialized;
        }

        private void OnBootstrapInitialized()
        {
            Dispose();
            ApplyPendingState();
        }

        private void ApplyPendingState()
        {
            Crashlytics.IsCrashlyticsCollectionEnabled = _isCollectionEnabled;

            if (!string.IsNullOrEmpty(_pendingUserId))
            {
                Crashlytics.SetUserId(_pendingUserId);
                _pendingUserId = null;
            }

            if (_pendingCustomKeys.Count == 0)
                return;

            foreach (var customKey in _pendingCustomKeys)
                Crashlytics.SetCustomKey(customKey.Key, customKey.Value ?? string.Empty);

            _pendingCustomKeys.Clear();
        }
    }
}
