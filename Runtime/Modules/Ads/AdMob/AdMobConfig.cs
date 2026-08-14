using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    [CreateAssetMenu(fileName = "AdMobConfig", menuName = "ENP/Ads/AdMob Config")]
    public sealed class AdMobConfig : ScriptableObject
    {
        [Header("Interstitial")]
        [SerializeField] private string _testInterstitialAndroid;
        [SerializeField] private string _testInterstitialIos;
        [SerializeField] private string _prodInterstitialAndroid;
        [SerializeField] private string _prodInterstitialIos;

        [Header("Rewarded")]
        [SerializeField] private string _testRewardedAndroid;
        [SerializeField] private string _testRewardedIos;
        [SerializeField] private string _prodRewardedAndroid;
        [SerializeField] private string _prodRewardedIos;

        [Header("App Open")]
        [SerializeField] private string _testAppOpenAndroid;
        [SerializeField] private string _testAppOpenIos;
        [SerializeField] private string _prodAppOpenAndroid;
        [SerializeField] private string _prodAppOpenIos;
        [SerializeField] private bool _shouldAutoInitializeAppOpen;

        [Header("Build")]
        [SerializeField] private bool _shouldUseProductionAdUnitsByDefault;

        public bool ShouldAutoInitializeAppOpen => _shouldAutoInitializeAppOpen;
        public bool ShouldUseProductionAdUnitsByDefault => _shouldUseProductionAdUnitsByDefault;

        public string ResolveInterstitialAndroid(bool useProduction) =>
            useProduction ? _prodInterstitialAndroid : _testInterstitialAndroid;

        public string ResolveInterstitialIos(bool useProduction) =>
            useProduction ? _prodInterstitialIos : _testInterstitialIos;

        public string ResolveRewardedAndroid(bool useProduction) =>
            useProduction ? _prodRewardedAndroid : _testRewardedAndroid;

        public string ResolveRewardedIos(bool useProduction) =>
            useProduction ? _prodRewardedIos : _testRewardedIos;

        public string ResolveAppOpenAndroid(bool useProduction) =>
            useProduction ? _prodAppOpenAndroid : _testAppOpenAndroid;

        public string ResolveAppOpenIos(bool useProduction) =>
            useProduction ? _prodAppOpenIos : _testAppOpenIos;
    }
}
