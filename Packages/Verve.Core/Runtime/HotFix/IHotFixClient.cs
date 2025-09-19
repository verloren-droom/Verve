namespace Verve.HotFix
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    
    /// <summary>
    /// 热更新客户端接口
    /// </summary>
    public interface IHotFixClient
    {
        delegate Task<byte[]> DownloadFile(string url, CancellationToken cancellationToken);
        delegate Task<bool> WriteFile(string path, byte[] bytes, CancellationToken cancellationToken);
        delegate string ComputeHash(byte[] bytes);
        
        /// <summary>
        /// 检查并获取热更清单
        /// </summary>
        /// <param name="version">当前版本</param>
        /// <param name="manifestUrl">清单URL</param>
        /// <param name="downloadManifest">清单下载</param>
        /// <param name="parseManifest">清单解析</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>热更新清单</returns>
        Task<HotFixManifest> CheckForUpdatesAsync(
            string version,
            string manifestUrl,
            DownloadFile downloadManifest,
            Func<byte[], HotFixManifest> parseManifest,
            CancellationToken ct = default);
        
        /// <summary>
        /// 应用热更新
        /// </summary>
        /// <param name="manifest">热更新清单</param>
        /// <param name="downloadAsset">资源下载</param>
        /// <param name="writeFile">文件写入</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="computeHash">计算哈希</param>
        /// <param name="ct">取消令牌</param>
        /// <returns></returns>
        Task<bool> ApplyUpdatesAsync(
            HotFixManifest manifest,
            DownloadFile downloadFile,
            WriteFile writeFile,
            Action<HotFixProgress> progressCallback = null,
            ComputeHash computeHash = null,
            CancellationToken ct = default);
    }
}
