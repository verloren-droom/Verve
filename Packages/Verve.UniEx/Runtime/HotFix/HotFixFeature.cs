#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.HotFix
{
    using Verve;


    [System.Serializable]
    public partial class HotFixFeature : Verve.HotFix.HotFixFeature
    {
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new GenericHotFixSubmodule());
            RegisterSubmodule(new HybridCLRHotFixSubmodule());

            base.OnLoad(dependencies);
        }
    }
}

#endif