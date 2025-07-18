#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Loader
{
    using System;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    
    
    /// <summary>
    /// 加载器功能
    /// </summary>
    [Serializable]
    public partial class LoaderFeature : Verve.Loader.LoaderFeature
    {
        protected override void OnBeforeSubmodulesLoaded(Verve.IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new AssetBundleLoader());
            RegisterSubmodule(new ResourcesLoader());
#if UNITY_2018_3_OR_NEWER
            RegisterSubmodule(new AddressablesLoader());
#endif
        }
    }
}

#endif