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
        protected override void OnLoad()
        {
            base.OnLoad();
            RegisterSubmodule(new AssetBundleLoader());
            RegisterSubmodule(new ResourcesLoader());
#if UNITY_2018_3_OR_NEWER
            RegisterSubmodule(new AddressablesLoader());
#endif
        }

        public IEnumerator LoadAssetAsync<TLoaderType, TAssetType>(string assetPath, Action<AssetLoaderCallbackContext<TAssetType>> onComplete) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return GetSubmodule<TLoaderType>().LoadAssetAsync<TAssetType>(assetPath, onComplete);
        }

        public async Task<SceneLoaderCallbackContext> LoadSceneAsync<TLoaderType>(
            string sceneName,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return await GetSubmodule<TLoaderType>().LoadSceneAsync(sceneName, allowSceneActivation, parameters, onProgress);
        }

        public IEnumerator LoadSceneAsync<TLoaderType>(
            string sceneName,
            Action<SceneLoaderCallbackContext> onComplete,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default, 
            Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            yield return GetSubmodule<TLoaderType>().LoadSceneAsync(sceneName, onComplete, allowSceneActivation, parameters, onProgress);
        }

        public async Task<SceneLoaderCallbackContext> UnloadSceneAsync<TLoaderType>(string sceneName, bool allowSceneActivation = true, UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return await GetSubmodule<TLoaderType>().UnloadSceneAsync(sceneName, allowSceneActivation, options, onProgress);
        }

        public IEnumerator UnloadSceneAsync<TLoaderType>(string sceneName, Action<SceneLoaderCallbackContext> onComplete, bool allowSceneActivation = true,
            UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return GetSubmodule<TLoaderType>().UnloadSceneAsync(sceneName, onComplete, allowSceneActivation, options, onProgress);
        }
    }
}

#endif