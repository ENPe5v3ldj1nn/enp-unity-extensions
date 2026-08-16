using System;
using System.Collections.Generic;
using ENP.UnityExtensions.Runtime;
using UnityEngine;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class PendingAnalyticsQueue
    {
        private const string StorageDirectory = "Analytics";
        private const string StorageFileName = "pending_events.json";
        private const int MaxPendingEvents = 200;

        private readonly List<AnalyticsEvent> _events = new();

        public IReadOnlyList<AnalyticsEvent> Events => _events;

        public void Restore()
        {
            _events.Clear();

            PendingEventsData data;
            try
            {
                data = Storage.Load<PendingEventsData>(StorageDirectory, StorageFileName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Analytics] Failed to read pending events: {exception.Message}");
                Clear();
                return;
            }

            if (data?.Events == null)
                return;

            foreach (var eventData in data.Events)
            {
                var restored = CreateEvent(eventData);
                if (restored != null)
                    _events.Add(restored);
            }
        }

        public void Enqueue(AnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent == null)
                return;

            if (_events.Count >= MaxPendingEvents)
                _events.RemoveAt(0);

            _events.Add(analyticsEvent);
            Persist();
        }

        public void Clear()
        {
            _events.Clear();
            Persist();
        }

        private void Persist()
        {
            var data = new PendingEventsData();
            foreach (var analyticsEvent in _events)
                data.Events.Add(CreateEventData(analyticsEvent));

            try
            {
                Storage.Save(StorageDirectory, StorageFileName, data);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Analytics] Failed to persist pending events: {exception.Message}");
            }
        }

        private static PendingEventData CreateEventData(AnalyticsEvent analyticsEvent)
        {
            var eventData = new PendingEventData { Name = analyticsEvent.Name };
            var parameters = analyticsEvent.Parameters;

            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                eventData.Parameters.Add(new PendingParamData
                {
                    Key = parameter.Key,
                    Type = parameter.Type,
                    StringValue = parameter.StringValue,
                    LongValue = parameter.LongValue,
                    DoubleValue = parameter.DoubleValue
                });
            }

            return eventData;
        }

        private static AnalyticsEvent CreateEvent(PendingEventData eventData)
        {
            if (eventData == null || string.IsNullOrWhiteSpace(eventData.Name))
                return null;

            var sourceParameters = eventData.Parameters;
            if (sourceParameters == null || sourceParameters.Count == 0)
                return new AnalyticsEvent(eventData.Name, Array.Empty<AnalyticsParam>());

            var parameters = new List<AnalyticsParam>(sourceParameters.Count);
            foreach (var paramData in sourceParameters)
            {
                if (paramData == null || string.IsNullOrWhiteSpace(paramData.Key))
                    continue;

                parameters.Add(CreateParam(paramData));
            }

            return new AnalyticsEvent(eventData.Name, parameters.ToArray());
        }

        private static AnalyticsParam CreateParam(PendingParamData paramData)
        {
            switch (paramData.Type)
            {
                case AnalyticsParamType.Long:
                    return new AnalyticsParam(paramData.Key, paramData.LongValue);
                case AnalyticsParamType.Double:
                    return new AnalyticsParam(paramData.Key, paramData.DoubleValue);
                default:
                    return new AnalyticsParam(paramData.Key, paramData.StringValue);
            }
        }

        private sealed class PendingEventsData
        {
            public List<PendingEventData> Events = new();
        }

        private sealed class PendingEventData
        {
            public string Name;
            public List<PendingParamData> Parameters = new();
        }

        private sealed class PendingParamData
        {
            public string Key;
            public AnalyticsParamType Type;
            public string StringValue;
            public long LongValue;
            public double DoubleValue;
        }
    }
}
