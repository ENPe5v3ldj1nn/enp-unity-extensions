using VContainer;

namespace ENP.UnityExtensions.Analytics
{
    public static class AnalyticsVContainerExtensions
    {
        public static void RegisterAnalyticsModule(this IContainerBuilder builder)
        {
            builder.Register<AnalyticsSessionCounter>(Lifetime.Singleton);
            builder.Register<IAnalyticsCommonParamsProvider, AppVersionCommonParamsProvider>(Lifetime.Singleton);
            builder.Register<IAnalyticsCommonParamsProvider, SessionNumberCommonParamsProvider>(Lifetime.Singleton);
            builder.Register<AnalyticsService>(Lifetime.Singleton);
        }

        public static void RegisterNullAnalyticsBackend(this IContainerBuilder builder)
        {
            builder.Register<IAnalyticsBackend, NullAnalyticsBackend>(Lifetime.Singleton);
            builder.Register<ICrashReporter, NullCrashReporter>(Lifetime.Singleton);
        }
    }
}
