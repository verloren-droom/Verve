namespace Verve.UniEx.Loader
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    
    /// <summary>
    /// 热更资源更新器接口
    /// </summary>
    public interface IHotUpdateUpdater
    {
        /// <summary>
        /// 检查更新
        /// </summary>
        /// <param name="ct">取消令牌</param>
        /// <returns>可更新的资源信息列表</returns>
        Task<IEnumerable<string>> CheckForUpdatesAsync(CancellationToken ct = default);
        
        /// <summary>
        /// 应用更新
        /// </summary>
        /// <param name="updates">要更新的资源信息列表</param>
        /// <param name="onProgress">进度回调函数，参数为进度百分比</param>
        /// <param name="ct">取消令牌</param>
        Task ApplyUpdatesAsync(IEnumerable<string> updates, Action<float> onProgress = null, CancellationToken ct = default);
    }
}