# if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Loader
{
    using System;
    using UnityEngine;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    
    /// <summary>
    /// 资源加载器子模块
    /// </summary>
    [System.Serializable]
    public sealed partial class ResourcesLoader : VerveUniEx.Loader.AssetLoaderBase
    {
        public override string ModuleName => "Resources";

        private readonly Dictionary<string, UnityEngine.Object> m_LoadedAssets = new Dictionary<string, UnityEngine.Object>();
        
        
        public override TObject LoadAsset<TObject>(string assetPath)
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(TObject)))
            {
                var asset = Resources.Load<UnityEngine.Object>(assetPath);
                m_LoadedAssets[assetPath] = asset;
                return (TObject)Convert.ChangeType(asset, typeof(TObject));
            }

            throw new TypeLoadException($"{typeof(TObject).Name} is not support!");
        }

        public override void UnloadAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            if (m_LoadedAssets.TryGetValue(assetPath, out UnityEngine.Object asset))
            {
                m_LoadedAssets.Remove(assetPath);
                Resources.UnloadAsset(asset);
            }
        }

        public override void UnloadAsset<TObject>(TObject asset)
        {
            if (asset == null) return;
            if (asset is UnityEngine.Object unityAsset)
            {
                Resources.UnloadAsset(unityAsset);
            }
        }

        public override void UnloadAllAsset()
        {
            m_LoadedAssets.Clear();
            Resources.UnloadUnusedAssets();
        }
    }
}
    
#endif