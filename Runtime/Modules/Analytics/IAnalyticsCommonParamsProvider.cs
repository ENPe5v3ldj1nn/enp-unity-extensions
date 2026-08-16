using System.Collections.Generic;

namespace ENP.UnityExtensions.Analytics
{
    public interface IAnalyticsCommonParamsProvider
    {
        void AppendParams(IList<AnalyticsParam> destination);
    }
}
