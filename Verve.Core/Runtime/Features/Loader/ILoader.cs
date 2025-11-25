namespace Verve.Loader
{
    using System;
    using System.Threading.Tasks;


    /// <summary>
    ///   <para>资源加载器接口</para>
    /// </summary>
    public interface ILoader : IDisposable
    {
        /// <summary>
        ///   <para>加载资源</para>
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <returns>
        ///   <para>资源对象</para>
        /// </returns>
        TObject LoadAsset<TObject>(string assetPath);
        
        /// <summary>
        ///   <para>异步加载资源</para>
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <returns>
        ///   <para>资源对象</para>
        /// </returns>
        Task<TObject> LoadAssetAsync<TObject>(string assetPath);
        
        /// <summary>
        ///   <para>卸载资源</para>
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        void UnloadAsset(string assetPath);
        
        /// <summary>
        ///   <para>卸载资源</para>
        /// </summary>
        /// <param name="asset">资源对象</param>
        /// <typeparam name="TObject">资源类型</typeparam>
        void UnloadAsset<TObject>(TObject asset);
        
        /// <summary>
        ///   <para>卸载所有资源</para>
        /// </summary>
        void UnloadAllAsset();
    }
}