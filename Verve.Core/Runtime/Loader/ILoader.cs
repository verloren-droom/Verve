#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine.SceneManagement;
    
    /// <summary>
    ///   <para>统一资源加载接口；外部只使用 Unity 项目路径，不暴露 <see cref="UnityEngine.AddressableAssets.Addressables"/> key、<see cref="UnityEngine.Resources"/> key 或 Bundle/Asset 组合。</para>
    /// </summary>
    public interface ILoader : IGameModule
    {
        /// <summary>
        ///   <para>按项目路径同步加载资源并返回可释放句柄；句柄必须在 Unity 主线程释放。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="assetPath">资源项目路径，必须以 Assets/ 或 Packages/ 开头。</param>
        AssetLoadHandle<TObject> LoadAsset<TObject>(string assetPath)
            where TObject : UnityEngine.Object;

        /// <summary>
        ///   <para>按项目路径异步加载资源并返回可释放句柄；句柄必须在 Unity 主线程释放。</para>
        /// </summary>
        /// <typeparam name="TObject">资源目标类型。</typeparam>
        /// <param name="assetPath">资源项目路径，必须以 Assets/ 或 Packages/ 开头。</param>
        /// <param name="ct">取消令牌。</param>
        Task<AssetLoadHandle<TObject>> LoadAssetAsync<TObject>(string assetPath, CancellationToken ct = default)
            where TObject : UnityEngine.Object;

        /// <summary>
        ///   <para>异步加载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景项目路径，必须以 Assets/ 或 Packages/ 开头并指向 .unity 文件。</param>
        /// <param name="allowSceneActivation">是否允许加载完成后自动激活场景。</param>
        /// <param name="parameters">场景加载参数。</param>
        /// <param name="onProgress">加载进度回调。</param>
        Task<SceneLoadHandle> LoadSceneAsync(
            string scenePath,
            bool allowSceneActivation = true,
            LoadSceneParameters parameters = default,
            Action<float> onProgress = null);

        /// <summary>
        ///   <para>异步卸载场景。</para>
        /// </summary>
        /// <param name="scenePath">场景项目路径，必须以 Assets/ 或 Packages/ 开头并指向 .unity 文件。</param>
        /// <param name="options">场景卸载参数。</param>
        /// <param name="onProgress">卸载进度回调。</param>
        Task<SceneLoadHandle> UnloadSceneAsync(
            string scenePath,
            UnloadSceneOptions options = UnloadSceneOptions.None,
            Action<float> onProgress = null);
    }

}

#endif
