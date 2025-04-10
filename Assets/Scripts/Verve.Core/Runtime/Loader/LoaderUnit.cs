namespace Verve.Loader
{
    using Unit;
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 资源加载单元
    /// </summary>
    [CustomUnit("Loader", -1), System.Serializable]
    public sealed partial class LoaderUnit : UnitBase
    {
        private readonly Dictionary<Type, IAssetLoader> m_Loaders = new Dictionary<Type, IAssetLoader>();

        public override void Startup(UnitRules parent, params object[] args)
        {
            base.Startup(parent, args);
#if UNITY_2018_3_OR_NEWER
            m_Loaders.Add(typeof(AddressablesLoader), new AddressablesLoader());
#endif
#if UNITY_5_3_OR_NEWER
            m_Loaders.Add(typeof(ResourcesLoader), new ResourcesLoader());
            m_Loaders.Add(typeof(AssetBundleLoader), new AssetBundleLoader());
#endif
        }

        public override void Shutdown()
        {
            foreach (var loader in m_Loaders.Values)
            {
                loader.ReleaseAllAsset();
            }
            m_Loaders.Clear();
            base.Shutdown();
        }
        
        public TAssetType LoadAsset<TAssetType>(Type loaderType, string assetPath)
        {
            return m_Loaders[loaderType].LoadAsset<TAssetType>(assetPath);
        }

        public TAssetType LoadAsset<TLoaderType, TAssetType>(string assetPath) where TLoaderType : IAssetLoader => LoadAsset<TAssetType>(typeof(TLoaderType), assetPath);
        
        public async Task<TAssetType> LoadAssetAsync<TAssetType>(Type loaderType, string assetPath)
        {
            return await m_Loaders?[loaderType]?.LoadAssetAsync<TAssetType>(assetPath);
        }
        
        public async Task<TAssetType> LoadAssetsAsync<TLoaderType, TAssetType>(string assetPath) where TLoaderType : IAssetLoader => await LoadAssetAsync<TAssetType>(typeof(TLoaderType), assetPath);

        public IEnumerator LoadAssetAsync<TAssetType>(Type loaderType, string assetPath, Action<TAssetType> onComplete)
        {
            return m_Loaders?[loaderType]?.LoadAssetAsync<TAssetType>(assetPath, onComplete);
        }
        
        public IEnumerator LoadAssetAsync<TLoaderType, TAssetType>(string assetPath, Action<TAssetType> onComplete) where TLoaderType : IAssetLoader => LoadAssetAsync<TAssetType>(typeof(TLoaderType), assetPath, onComplete);
        
        public void ReleaseAsset(Type loaderType, string assetPath)
        {
            m_Loaders?[loaderType]?.ReleaseAsset(assetPath);
        }
        
        public void ReleaseAsset<TLoaderType>(Type loaderType, string assetPath) where TLoaderType : IAssetLoader
        {
            ReleaseAsset(typeof(TLoaderType), assetPath);
        }
        
        public void ReleaseAllAsset(Type loaderType)
        {
            m_Loaders?[loaderType]?.ReleaseAllAsset();
        }
        
        public void ReleaseAllAsset<TLoaderType>(Type loaderType) where TLoaderType : IAssetLoader
        {
            ReleaseAllAsset(typeof(TLoaderType));
        }
    }
}