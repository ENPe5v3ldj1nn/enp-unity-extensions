using System;
using System.Collections.Generic;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class AnalyticsEvent
    {
        public string Name { get; }
        public IReadOnlyList<AnalyticsParam> Parameters { get; }

        public AnalyticsEvent(string name, AnalyticsParam[] parameters)
        {
            Name = name;
            Parameters = parameters ?? Array.Empty<AnalyticsParam>();
        }
    }
}
