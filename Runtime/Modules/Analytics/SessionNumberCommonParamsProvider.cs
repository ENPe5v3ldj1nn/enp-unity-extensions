using System.Collections.Generic;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class SessionNumberCommonParamsProvider : IAnalyticsCommonParamsProvider
    {
        private const string SessionNumberKey = "session_number";

        private readonly AnalyticsSessionCounter _sessionCounter;

        public SessionNumberCommonParamsProvider(AnalyticsSessionCounter sessionCounter)
        {
            _sessionCounter = sessionCounter;
        }

        public void AppendParams(IList<AnalyticsParam> destination)
        {
            destination.Add(new AnalyticsParam(SessionNumberKey, _sessionCounter.SessionNumber));
        }
    }
}
