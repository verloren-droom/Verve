#if UNITY_5_3_OR_NEWER

namespace Verve.Loader
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    
    /// <summary>
    ///   <para>热更资源更新器接口</para>
    /// </summary>
    public interface IHotUpdateUpdater
    {
        /// <summary>
        ///   <para>检查更新</para>
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>
        ///   <para>可更新的资源信息列表</para>
        /// </returns>
        Task<IEnumerable<string>> CheckForUpdatesAsync(CancellationToken ct = default);
        
        /// <summary>
        ///   <para>应用更新</para>
        /// </summary>
        /// <param name="updates">要更新的资源信息列表</param>
        /// <param name="onProgress">进度回调函数，参数为进度百分比</param>
        /// <param name="ct">取消令牌</param>
        Task ApplyUpdatesAsync(IEnumerable<string> updates, Action<float> onProgress = null, CancellationToken ct = default);
    }
}

#endif