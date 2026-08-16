using System;
using System.Collections.Generic;
using ENP.UnityExtensions.Analytics;
using Firebase.Analytics;
using UnityEngine;

namespace ENP.UnityExtensions.Firebase
{
    public sealed class FirebaseAnalyticsBackend : IAnalyticsBackend, IDisposable
    {
        private const int MaxParametersPerEvent = 25;
        private const int MaxStringValueLength = 100;

        private readonly FirebaseBootstrap _bootstrap;
        private readonly Dictionary<string, string> _pendingUserProperties = new(StringComparer.Ordinal);

        private bool _isCollectionEnabled = true;
        private bool _isSubscribed;
        private string _pendingUserId;

        public FirebaseAnalyticsBackend(FirebaseBootstrap bootstrap)
        {
            _bootstrap = bootstrap;
        }

        public bool IsReady => _bootstrap.IsReady;

        public event Action Initialized;

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
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(isEnabled);
        }

        public void SetUserId(string userId)
        {
            if (!_bootstrap.IsReady)
            {
                _pendingUserId = userId;
                return;
            }

            FirebaseAnalytics.SetUserId(userId);
        }

        public void SetUserProperty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            if (!_bootstrap.IsReady)
            {
                _pendingUserProperties[name] = value;
                return;
            }

            FirebaseAnalytics.SetUserProperty(name, value);
        }

        public void LogEvent(AnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null || !_bootstrap.IsReady)
                return;

            FirebaseAnalytics.LogEvent(analyticsEvent.Name, CreateParameters(analyticsEvent.Parameters));
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
            Initialized?.Invoke();
        }

        private void ApplyPendingState()
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(_isCollectionEnabled);

            if (!string.IsNullOrEmpty(_pendingUserId))
            {
                FirebaseAnalytics.SetUserId(_pendingUserId);
                _pendingUserId = null;
            }

            if (_pendingUserProperties.Count == 0)
                return;

            foreach (var userProperty in _pendingUserProperties)
                FirebaseAnalytics.SetUserProperty(userProperty.Key, userProperty.Value);

            _pendingUserProperties.Clear();
        }

        private static Parameter[] CreateParameters(IReadOnlyList<AnalyticsParam> parameters)
        {
            var count = Mathf.Min(parameters.Count, MaxParametersPerEvent);
            var result = new Parameter[count];

            for (var i = 0; i < count; i++)
                result[i] = CreateParameter(parameters[i]);

            return result;
        }

        private static Parameter CreateParameter(AnalyticsParam parameter)
        {
            switch (parameter.Type)
            {
                case AnalyticsParamType.Long:
                    return new Parameter(parameter.Key, parameter.LongValue);
                case AnalyticsParamType.Double:
                    return new Parameter(parameter.Key, parameter.DoubleValue);
                default:
                    return new Parameter(parameter.Key, Truncate(parameter.StringValue));
            }
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= MaxStringValueLength)
                return value ?? string.Empty;

            return value.Substring(0, MaxStringValueLength);
        }
    }
}
