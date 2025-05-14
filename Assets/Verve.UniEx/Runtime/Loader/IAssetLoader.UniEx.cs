namespace VerveUniEx.Loader
{
    
#if UNITY_5_3_OR_NEWER
    using System;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    
    
    public interface IAssetLoader : Verve.Loader.IAssetLoader
    {
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="onComplete">资源加载完成回调函数</param>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <returns></returns>
        IEnumerator LoadAssetAsync<TObject>(string assetPath, Action<AssetLoaderCallbackContext<TObject>> onComplete);
        /// <summary>
        /// 异步场景加载
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="allowSceneActivation"></param>
        /// <param name="parameters">加载场景参数</param>
        /// <param name="onProgress">加载进度回调</param>
        /// <returns></returns>
        Task<SceneLoaderCallbackContext> LoadSceneAsync(string sceneName, bool allowSceneActivation = true, LoadSceneParameters parameters = default, Action<float> onProgress = null);
        /// <summary>
        /// 多线程加载场景
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="onComplete">加载完成回调</param>
        /// <param name="allowSceneActivation"></param>
        /// <param name="parameters"></param>
        /// <param name="onProgress">加载进度回调</param>
        /// <returns></returns>
        IEnumerator LoadSceneAsync(string sceneName, Action<SceneLoaderCallbackContext> onComplete, bool allowSceneActivation = true, LoadSceneParameters parameters = default, Action<float> onProgress = null);
        /// <summary>
        /// 异步卸载场景
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="allowSceneActivation"></param>
        /// <param name="options"></param>
        /// <param name="onProgress">加载进度回调</param>
        /// <returns></returns>
        Task<SceneLoaderCallbackContext> UnloadSceneAsync(string sceneName, bool allowSceneActivation = true, UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null);
        /// <summary>
        /// 多线程卸载场景
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="onComplete">加载完成回调</param>
        /// <param name="allowSceneActivation"></param>
        /// <param name="options"></param>
        /// <param name="onProgress">加载进度回调></param>
        /// <returns></returns>
        IEnumerator UnloadSceneAsync(string sceneName, Action<SceneLoaderCallbackContext> onComplete, bool allowSceneActivation = true, UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null);
    }
#endif
    
}