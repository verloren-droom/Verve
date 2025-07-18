namespace Verve.Net
{
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 网络功能
    /// </summary>
    [System.Serializable]
    public class NetworkFeature : ModularGameFeature
    {
        protected override void OnBeforeSubmodulesLoaded(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new HttpClientSubmodule());
        }
    }
}