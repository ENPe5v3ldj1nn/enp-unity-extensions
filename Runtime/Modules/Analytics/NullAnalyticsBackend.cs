using System;
using System.Text;
using UnityEngine;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class NullAnalyticsBackend : IAnalyticsBackend
    {
        public bool IsReady => true;

        public event Action Initialized
        {
            add { }
            remove { }
        }

        public void Initialize()
        {
        }

        public void SetCollectionEnabled(bool isEnabled)
        {
        }

        public void SetUserId(string userId)
        {
        }

        public void SetUserProperty(string name, string value)
        {
        }

        public void LogEvent(AnalyticsEvent analyticsEvent)
        {
#if UNITY_EDITOR
            if (analyticsEvent == null)
                return;

            Debug.Log(Describe(analyticsEvent));
#endif
        }

#if UNITY_EDITOR
        private static string Describe(AnalyticsEvent analyticsEvent)
        {
            var builder = new StringBuilder("[Analytics] ").Append(analyticsEvent.Name);
            var parameters = analyticsEvent.Parameters;

            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                builder.Append(' ').Append(parameter.Key).Append('=');

                switch (parameter.Type)
                {
                    case AnalyticsParamType.Long:
                        builder.Append(parameter.LongValue);
                        break;
                    case AnalyticsParamType.Double:
                        builder.Append(parameter.DoubleValue);
                        break;
                    default:
                        builder.Append(parameter.StringValue);
                        break;
                }
            }

            return builder.ToString();
        }
#endif
    }
}
