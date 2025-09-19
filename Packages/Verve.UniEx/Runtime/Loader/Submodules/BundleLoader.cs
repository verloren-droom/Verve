#if UNITY_5_3_OR_NEWER
    
namespace Verve.UniEx.Loader
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    

    /// <summary>
    /// 资源分包加载
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(LoaderGameFeature), Description = "资源分包加载器")]
    public sealed partial class BundleLoader : LoaderSubmodule
    {
        private readonly Dictionary<string, AssetBundle> m_LoadedBundles = new Dictionary<string, AssetBundle>();

        public override TObject LoadAsset<TObject>(string assetPath)
        {
            throw new NotImplementedException();
        }

        public override void UnloadAsset<TObject>(TObject asset)
        {
            throw new NotImplementedException();
        }
    }
}
    
#endif