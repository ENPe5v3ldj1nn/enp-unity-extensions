using VContainer;

namespace ENP.UnityExtensions.Ads
{
    public static class AdsVContainerExtensions
    {
        public static void RegisterAdsModule(this IContainerBuilder builder, AdMobConfig adMobConfig,
            ConsentConfig consentConfig, AdThrottleConfig throttleConfig)
        {
            builder.RegisterInstance(adMobConfig);
            builder.RegisterInstance(consentConfig);
            builder.RegisterInstance(throttleConfig);

            builder.Register<AdAnalyticsService>(Lifetime.Singleton);
            builder.Register<AdThrottleService>(Lifetime.Singleton);
            builder.Register<ConsentService>(Lifetime.Singleton);
            builder.Register<AdMobService>(Lifetime.Singleton);
            builder.Register<IosAttAuthorizationRequester>(Lifetime.Singleton);
            builder.Register<AdReadinessCoordinator>(Lifetime.Singleton);
        }
    }
}
