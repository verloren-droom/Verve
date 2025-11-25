namespace Verve.Net
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>网络短连接接口</para>
    /// </summary>
    public interface ITransientConnection : INetworkClient
    {
        /// <summary>
        ///   <para>异步发送请求并获取响应</para>
        /// </summary>
        /// <param name="method">请求方法</param>
        /// <param name="url">网址</param>
        /// <param name="data">数据</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="headers">请求头</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>
        ///   <para>响应数据</para>
        /// </returns>
        Task<string> RequestAsync(
            string method,
            string url, 
            string data = null, 
            string contentType = "application/json",
            Dictionary<string, string> headers = null,
            CancellationToken ct = default);
        
        /// <summary>
        ///  <para>发送请求并获取响应</para>
        /// </summary>
        /// <param name="method">请求方法</param>
        /// <param name="url">网址</param>
        /// <param name="data">数据</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="headers">请求头</param>
        /// <returns>
        ///   <para>响应数据</para>
        /// </returns>
        string Request(
            string method,
            string url, 
            string data = null, 
            string contentType = "application/json",
            Dictionary<string, string> headers = null);

        /// <summary>
        ///   <para>异步下载文件</para>
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="method">请求方法</param>
        /// <param name="headers">请求头</param>
        /// <param name="progressCallback">进度回调</param>
        Task DownloadFileAsync(
            string url,
            string savePath,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null,
            Func<byte[], string> computeHash = null,
            CancellationToken ct = default);
        
        /// <summary>
        ///   <para>异步下载文件并保存为内存</para>
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="method">请求方法</param>
        /// <param name="headers">请求头</param>
        /// <param name="progressCallback">进度回调</param>
        Task<byte[]> DownloadFileToMemoryAsync(
            string url,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null,
            Func<byte[], string> computeHash = null,
            CancellationToken ct = default);
        
        /// <summary>
        ///   <para>下载文件</para>
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="method">请求方法</param>
        /// <param name="headers">请求头</param>
        /// <param name="progressCallback">进度回调</param>
        void DownloadFile(
            string url,
            string savePath,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null,
            Func<byte[], string> computeHash = null);
        
        /// <summary>
        ///   <para>上传文件</para>
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="filePath">本地文件路径</param>
        /// <param name="method">请求方法</param>
        /// <param name="headers">请求头</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns></returns>
        string UploadFile(
            string url,
            string filePath,
            string method = "POST",
            Dictionary<string, string> headers = null,
            string contentType = "application/octet-stream",
            System.Action<long, long> progressCallback = null);

        /// <summary>
        ///   <para>异步上传文件</para>
        /// </summary>
        /// <param name="url">网址</param>
        /// <param name="filePath">本地文件路径</param>
        /// <param name="method">请求方法</param>
        /// <param name="headers">请求头</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="ct">取消令牌</param>
        Task<string> UploadFileAsync(
            string url,
            string filePath,
            string method = "POST",
            Dictionary<string, string> headers = null,
            string contentType = "application/octet-stream",
            System.Action<long, long> progressCallback = null,
            CancellationToken ct = default);
        
        /// <summary>
        ///   <para>默认请求头</para>
        /// </summary>
        Dictionary<string, string> DefaultHeader { get; }
        
        /// <summary>
        ///   <para>Cookie容器</para>
        /// </summary>
        CookieContainer Cookie { get; }
    }
}