#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Loader
{
    using System;
    using UnityEngine;
    using System.Collections.Generic;
    

    public sealed partial class AssetBundleLoader : VerveUniEx.Loader.AssetLoaderBase
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