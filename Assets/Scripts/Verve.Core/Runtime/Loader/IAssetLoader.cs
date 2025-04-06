namespace Verve.Loader
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    public interface IAssetLoader
    {
        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <returns></returns>
        TObject LoadAsset<TObject>(string assetPath);
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <returns></returns>
        Task<TObject> LoadAssetAsync<TObject>(string assetPath);
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="onComplete">资源加载完成回调函数</param>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <returns></returns>
        IEnumerator LoadAssetAsync<TObject>(string assetPath, Action<TObject> onComplete);
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        void ReleaseAsset(string assetPath);
        void ReleaseAsset<TObject>(TObject asset);
        /// <summary>
        /// 释放所有资源
        /// </summary>
        void ReleaseAllAsset();
    }
}