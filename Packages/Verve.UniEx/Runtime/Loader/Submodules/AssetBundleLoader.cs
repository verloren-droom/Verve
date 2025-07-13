#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Loader
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    

    /// <summary>
    /// 资源分包加载子模块
    /// </summary>
    public sealed partial class AssetBundleLoader : VerveUniEx.Loader.AssetLoaderBase
    {
        public override string ModuleName => "AssetBundle";

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