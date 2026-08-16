using System;
using System.Collections.Generic;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class AnalyticsService : IDisposable
    {
        private const string ScreenViewEventName = "screen_view";
        private const string ScreenNameKey = "screen_name";
        private const string ScreenClassKey = "screen_class";

        private readonly IAnalyticsBackend _backend;
        private readonly AnalyticsSessionCounter _sessionCounter;
        private readonly IReadOnlyList<IAnalyticsCommonParamsProvider> _commonParamsProviders;
        private readonly PendingAnalyticsQueue _pendingQueue = new();
        private readonly List<AnalyticsParam> _eventParamsBuffer = new();
        private readonly List<AnalyticsParam> _commonParamsBuffer = new();

        private bool _isSessionStarted;

        public AnalyticsService(IAnalyticsBackend backend, AnalyticsSessionCounter sessionCounter,
            IReadOnlyList<IAnalyticsCommonParamsProvider> commonParamsProviders)
        {
            _backend = backend;
            _sessionCounter = sessionCounter;
            _commonParamsProviders = commonParamsProviders ?? Array.Empty<IAnalyticsCommonParamsProvider>();
        }

        public int SessionNumber => _sessionCounter.SessionNumber;

        public void BeginSession()
        {
            if (_isSessionStarted)
                return;

            _isSessionStarted = true;
            _sessionCounter.BeginSession();
            _pendingQueue.Restore();

            _backend.Initialized += OnBackendInitialized;
            _backend.Initialize();

            if (_backend.IsReady)
                FlushPendingEvents();
        }

        public void SetCollectionEnabled(bool isEnabled)
        {
            _backend.SetCollectionEnabled(isEnabled);
        }

        public void SetUserId(string userId)
        {
            _backend.SetUserId(userId);
        }

        public void SetUserProperty(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            _backend.SetUserProperty(name, value);
        }

        public void LogEvent(string eventName)
        {
            LogEvent(eventName, Array.Empty<AnalyticsParam>());
        }

        public void LogEvent(string eventName, params AnalyticsParam[] parameters)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return;

            var analyticsEvent = CreateEvent(eventName, parameters);

            if (_backend.IsReady)
            {
                _backend.LogEvent(analyticsEvent);
                return;
            }

            _pendingQueue.Enqueue(analyticsEvent);
        }

        public void LogScreenView(string screenName, string screenClass = null)
        {
            if (string.IsNullOrWhiteSpace(screenName))
                return;

            LogEvent(ScreenViewEventName,
                new AnalyticsParam(ScreenNameKey, screenName),
                new AnalyticsParam(ScreenClassKey, string.IsNullOrWhiteSpace(screenClass) ? screenName : screenClass));
        }

        public void Dispose()
        {
            _backend.Initialized -= OnBackendInitialized;
        }

        private void OnBackendInitialized()
        {
            _backend.Initialized -= OnBackendInitialized;
            FlushPendingEvents();
        }

        private void FlushPendingEvents()
        {
            var pendingEvents = _pendingQueue.Events;
            if (pendingEvents.Count == 0)
                return;

            for (var i = 0; i < pendingEvents.Count; i++)
                _backend.LogEvent(pendingEvents[i]);

            _pendingQueue.Clear();
        }

        private AnalyticsEvent CreateEvent(string eventName, AnalyticsParam[] parameters)
        {
            _eventParamsBuffer.Clear();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.IsValid)
                        _eventParamsBuffer.Add(parameter);
                }
            }

            AppendCommonParams();

            return new AnalyticsEvent(eventName, _eventParamsBuffer.ToArray());
        }

        private void AppendCommonParams()
        {
            for (var i = 0; i < _commonParamsProviders.Count; i++)
            {
                _commonParamsBuffer.Clear();
                _commonParamsProviders[i].AppendParams(_commonParamsBuffer);

                foreach (var parameter in _commonParamsBuffer)
                {
                    if (parameter.IsValid && !ContainsKey(parameter.Key))
                        _eventParamsBuffer.Add(parameter);
                }
            }
        }

        private bool ContainsKey(string key)
        {
            foreach (var parameter in _eventParamsBuffer)
            {
                if (string.Equals(parameter.Key, key, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
