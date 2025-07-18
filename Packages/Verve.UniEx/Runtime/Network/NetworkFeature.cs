#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Net
{
    using Verve;
    
    
    /// <summary>
    /// 网络功能
    /// </summary>
    [System.Serializable]
    public partial class NetworkFeature : Verve.Net.NetworkFeature
    {
        protected override void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new HttpClientSubmodule());
        }
    }
}

#endif