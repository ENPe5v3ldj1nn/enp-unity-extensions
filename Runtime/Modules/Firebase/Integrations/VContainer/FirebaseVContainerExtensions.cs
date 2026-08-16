using ENP.UnityExtensions.Analytics;
using VContainer;

namespace ENP.UnityExtensions.Firebase
{
    public static class FirebaseVContainerExtensions
    {
        public static void RegisterFirebaseAnalyticsBackend(this IContainerBuilder builder)
        {
            builder.Register<FirebaseBootstrap>(Lifetime.Singleton);
            builder.Register<IAnalyticsBackend, FirebaseAnalyticsBackend>(Lifetime.Singleton);
            builder.Register<ICrashReporter, FirebaseCrashReporter>(Lifetime.Singleton);
        }
    }
}
