#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Platform
{
    /// <summary>
    /// 平台功能
    /// </summary>
    public partial class PlatformFeature : Verve.Platform.PlatformFeature
    {
        protected override void OnLoad(Verve.IReadOnlyFeatureDependencies dependencies)
        {
            base.OnLoad(dependencies);
            
            m_Platform = new GenericPlatformSubmodule();
            m_Platform.OnModuleLoaded(dependencies);
        }
    }
}

#endif