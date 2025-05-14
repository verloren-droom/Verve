namespace VerveUniEx.Loader
{
    
#if UNITY_5_3_OR_NEWER
    using System;
    using Verve.Unit;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    
    
    [CustomUnit("Loader", -1), System.Serializable]
    public partial class LoaderUnit : Verve.Loader.LoaderUnit
    {
        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
            Register(new AssetBundleLoader());
            Register(new ResourcesLoader());
#if UNITY_2018_3_OR_NEWER
            Register(new AddressablesLoader());
#endif
        }
        
        public IEnumerator LoadAssetAsync<TLoaderType, TAssetType>(string assetPath, Action<AssetLoaderCallbackContext<TAssetType>> onComplete) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return Resolve<TLoaderType>()?.LoadAssetAsync<TAssetType>(assetPath, onComplete);
        }

        public async Task<SceneLoaderCallbackContext> LoadSceneAsync<TLoaderType>(
            string sceneName,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return await Resolve<TLoaderType>()?.LoadSceneAsync(sceneName, allowSceneActivation, parameters, onProgress);
        }

        public IEnumerator LoadSceneAsync<TLoaderType>(
            string sceneName,
            Action<SceneLoaderCallbackContext> onComplete,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default, 
            Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return Resolve<TLoaderType>()?.LoadSceneAsync(sceneName, onComplete, allowSceneActivation, parameters, onProgress);
        }

        public async Task<SceneLoaderCallbackContext> UnloadSceneAsync<TLoaderType>(string sceneName, bool allowSceneActivation = true, UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return await Resolve<TLoaderType>()?.UnloadSceneAsync(sceneName, allowSceneActivation, options, onProgress);
        }

        public IEnumerator UnloadSceneAsync<TLoaderType>(string sceneName, Action<SceneLoaderCallbackContext> onComplete, bool allowSceneActivation = true,
            UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null) where TLoaderType : class, VerveUniEx.Loader.IAssetLoader
        {
            return Resolve<TLoaderType>()?.UnloadSceneAsync(sceneName, onComplete, allowSceneActivation, options, onProgress);
        }
    }
#endif
    
}