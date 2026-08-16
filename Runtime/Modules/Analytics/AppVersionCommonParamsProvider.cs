using System.Collections.Generic;
using UnityEngine;

namespace ENP.UnityExtensions.Analytics
{
    public sealed class AppVersionCommonParamsProvider : IAnalyticsCommonParamsProvider
    {
        private const string AppVersionKey = "app_version";

        private readonly AnalyticsParam _appVersion;

        public AppVersionCommonParamsProvider()
        {
            _appVersion = new AnalyticsParam(AppVersionKey, Application.version);
        }

        public void AppendParams(IList<AnalyticsParam> destination)
        {
            destination.Add(_appVersion);
        }
    }
}
