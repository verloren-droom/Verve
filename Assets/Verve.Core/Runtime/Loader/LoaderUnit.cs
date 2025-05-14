namespace Verve.Loader
{
    
    using Unit;
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
    using UnityEngine.SceneManagement;
#endif
    
    
    /// <summary>
    /// 资源加载单元
    /// </summary>
    [CustomUnit("Loader", -1), System.Serializable]
    public sealed partial class LoaderUnit : UnitBase
    {
        private readonly Dictionary<Type, IAssetLoader> m_Loaders = new Dictionary<Type, IAssetLoader>();

        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
#if UNITY_2018_3_OR_NEWER
            m_Loaders.Add(typeof(AddressablesLoader), new AddressablesLoader());
#endif
#if UNITY_5_3_OR_NEWER
            m_Loaders.Add(typeof(ResourcesLoader), new ResourcesLoader());
            m_Loaders.Add(typeof(AssetBundleLoader), new AssetBundleLoader());
#endif
        }

        protected override void OnShutdown()
        {
            foreach (var loader in m_Loaders.Values)
            {
                loader.UnloadAllAsset();
            }
            m_Loaders.Clear();
            base.OnShutdown();
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

        public IEnumerator LoadAssetAsync<TAssetType>(Type loaderType, string assetPath, Action<AssetLoaderCallbackContext<TAssetType>> onComplete)
        {
            return m_Loaders?[loaderType]?.LoadAssetAsync<TAssetType>(assetPath, onComplete);
        }
        
        public IEnumerator LoadAssetAsync<TLoaderType, TAssetType>(string assetPath, Action<AssetLoaderCallbackContext<TAssetType>> onComplete) where TLoaderType : IAssetLoader => LoadAssetAsync<TAssetType>(typeof(TLoaderType), assetPath, onComplete);
        
        public void UnloadAsset(Type loaderType, string assetPath)
        {
            m_Loaders?[loaderType]?.UnloadAsset(assetPath);
        }
        
        public void UnloadAsset<TLoaderType>(Type loaderType, string assetPath) where TLoaderType : IAssetLoader
        {
            UnloadAsset(typeof(TLoaderType), assetPath);
        }
        
        public void UnloadAllAsset(Type loaderType)
        {
            m_Loaders?[loaderType]?.UnloadAllAsset();
        }
        
        public void UnloadAllAsset<TLoaderType>(Type loaderType) where TLoaderType : IAssetLoader
        {
            UnloadAllAsset(typeof(TLoaderType));
        }
        
#if UNITY_5_3_OR_NEWER
        public async Task<SceneLoaderCallbackContext> LoadSceneAsync<TLoaderType>(
            string sceneName,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null) where TLoaderType : IAssetLoader
        {
            return await m_Loaders?[typeof(TLoaderType)]?.LoadSceneAsync(sceneName, allowSceneActivation, parameters, onProgress);
        }

        public IEnumerator LoadSceneAsync<TLoaderType>(
            string sceneName,
            Action<SceneLoaderCallbackContext> onComplete,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default, 
            Action<float> onProgress = null) where TLoaderType : IAssetLoader
        {
            return m_Loaders?[typeof(TLoaderType)]?.LoadSceneAsync(sceneName, onComplete, allowSceneActivation, parameters, onProgress);
        }

        public async Task<SceneLoaderCallbackContext> UnloadSceneAsync<TLoaderType>(string sceneName, bool allowSceneActivation = true, UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null) where TLoaderType : IAssetLoader
        {
            return await m_Loaders?[typeof(TLoaderType)]?.UnloadSceneAsync(sceneName, allowSceneActivation, options, onProgress);
        }

        public IEnumerator UnloadSceneAsync<TLoaderType>(string sceneName, Action<SceneLoaderCallbackContext> onComplete, bool allowSceneActivation = true,
            UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null) where TLoaderType : IAssetLoader
        {
            return m_Loaders?[typeof(TLoaderType)]?.UnloadSceneAsync(sceneName, onComplete, allowSceneActivation, options, onProgress);
        }
#endif
    }
    
}