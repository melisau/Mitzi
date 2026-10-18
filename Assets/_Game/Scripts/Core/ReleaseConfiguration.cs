using UnityEngine;
namespace PawPath.Core
{
    [CreateAssetMenu(menuName="Mitzi/Release Configuration")]
    public sealed class ReleaseConfiguration : ScriptableObject
    {
        public string developerName="Mitzi";
        public string contactEmail="melisauyar5225@gmail.com";
        public string privacyPolicyUrl="";
        public bool privacyPolicyReviewed;
        public bool advertisingEnabled;
        public string androidAppId="";
        public string androidRewardedAdUnitId="";
        public string iosRewardedAdUnitId="";
        public static ReleaseConfiguration Current => Resources.Load<ReleaseConfiguration>("ReleaseConfiguration");
    }
}
