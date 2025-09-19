namespace Verve.Loader
{
    using System;
    using System.Threading.Tasks;


    /// <summary>
    /// 资源加载器接口
    /// </summary>
    public interface ILoader : IDisposable
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
        /// 卸载资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        void UnloadAsset(string assetPath);
        void UnloadAsset<TObject>(TObject asset);
        /// <summary>
        /// 卸载所有资源
        /// </summary>
        void UnloadAllAsset();
    }
}