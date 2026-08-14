using UnityEngine;

namespace ENP.UnityExtensions.Ads
{
    [CreateAssetMenu(fileName = "ConsentConfig", menuName = "ENP/Ads/Consent Config")]
    public sealed class ConsentConfig : ScriptableObject
    {
        [SerializeField] private bool _tagForUnderAgeOfConsent;
        [SerializeField] private bool _isDebugGeographyEea = true;
        [SerializeField] private string[] _testDeviceHashedIds = System.Array.Empty<string>();

        // Ім'я поля/властивості навмисно збігається з GoogleMobileAds.Ump.Api.ConsentRequestParameters.TagForUnderAgeOfConsent.
        public bool TagForUnderAgeOfConsent => _tagForUnderAgeOfConsent;
        public bool IsDebugGeographyEea => _isDebugGeographyEea;
        public string[] TestDeviceHashedIds => _testDeviceHashedIds;
    }
}
