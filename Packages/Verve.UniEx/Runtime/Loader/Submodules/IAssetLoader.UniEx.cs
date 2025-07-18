#if UNITY_5_3_OR_NEWER
    
namespace VerveUniEx.Loader
{
    using System;
    using Verve.Loader;
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;
    
    
    public interface IAssetLoader : Verve.Loader.IAssetLoader
    {
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
        /// 异步卸载场景
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="allowSceneActivation"></param>
        /// <param name="options"></param>
        /// <param name="onProgress">加载进度回调</param>
        /// <returns></returns>
        Task<SceneLoaderCallbackContext> UnloadSceneAsync(string sceneName, bool allowSceneActivation = true, UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null);
    }
}
    
#endif