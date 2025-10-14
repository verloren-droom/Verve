namespace Verve.Net
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 网络短连接接口
    /// </summary>
    public interface ITransientConnection : INetworkClient
    {
        /// <summary> 发送请求并获取响应 </summary>
        Task<string> RequestAsync(
            string method,
            string url, 
            string data = null, 
            string contentType = "application/json",
            Dictionary<string, string> headers = null,
            CancellationToken ct = default);
        
        string Request(
            string method,
            string url, 
            string data = null, 
            string contentType = "application/json",
            Dictionary<string, string> headers = null);

        /// <summary> 异步下载文件 </summary>
        /// <param name="url">网址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>
        Task DownloadFileAsync(
            string url,
            string savePath,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null,
            Func<byte[], string> computeHash = null,
            CancellationToken ct = default);
        
        /// <summary> 异步下载文件并保存为内存 </summary>
        Task<byte[]> DownloadFileToMemoryAsync(
            string url,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null,
            Func<byte[], string> computeHash = null,
            CancellationToken ct = default);
        
        /// <summary> 下载文件 </summary>
        /// <param name="url">网址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="progressCallback"></param>
        void DownloadFile(
            string url,
            string savePath,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null,
            Func<byte[], string> computeHash = null);
        
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="contentType"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>
        string UploadFile(
            string url,
            string filePath,
            string method = "POST",
            Dictionary<string, string> headers = null,
            string contentType = "application/octet-stream",
            System.Action<long, long> progressCallback = null);

        /// <summary>
        /// 异步上传文件
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <param name="contentType"></param>
        /// <param name="progressCallback"></param>
        /// <returns></returns>
        Task<string> UploadFileAsync(
            string url,
            string filePath,
            string method = "POST",
            Dictionary<string, string> headers = null,
            string contentType = "application/octet-stream",
            System.Action<long, long> progressCallback = null,
            CancellationToken ct = default);
        
        /// <summary> 默认请求头 </summary>
        Dictionary<string, string> DefaultHeader { get; }
        /// <summary> Cookie容器 </summary>
        CookieContainer Cookie { get; }
    }
}