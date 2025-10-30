#if UNITY_5_3_OR_NEWER
    
namespace Verve.UniEx.Loader
{
    using System;
    using Verve.Loader;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    
    
    /// <summary>
    ///   <para>加载子模块接口</para>
    /// </summary>
    public interface ILoaderSubmodule : ILoader
    {
        /// <summary>
        ///   <para>异步场景加载</para>
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="allowSceneActivation"></param>
        /// <param name="parameters">加载场景参数</param>
        /// <param name="onProgress">加载进度回调</param>
        /// <returns>
        ///   <para>异步场景加载回调</para>
        /// </returns>
        Task<SceneLoaderCallbackContext> LoadSceneAsync(string sceneName, bool allowSceneActivation = true, LoadSceneParameters parameters = default, Action<float> onProgress = null);
        
        /// <summary>
        ///   <para>异步卸载场景</para>
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="allowSceneActivation"></param>
        /// <param name="options"></param>
        /// <param name="onProgress">加载进度回调</param>
        /// <returns>
        ///   <para>异步场景加载回调</para>
        /// </returns>
        Task<SceneLoaderCallbackContext> UnloadSceneAsync(string sceneName, bool allowSceneActivation = true, UnloadSceneOptions options = UnloadSceneOptions.None, Action<float> onProgress = null);
    }
}
    
#endif