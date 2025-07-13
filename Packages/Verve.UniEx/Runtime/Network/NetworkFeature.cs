#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Net
{
    using Verve;
    
    
    [System.Serializable]
    public partial class NetworkFeature : Verve.Net.NetworkFeature
    {
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new HttpClientSubmodule());
            
            base.OnLoad(dependencies);
        }
    }
}

#endif