namespace Verve.Net
{
    using System.Net;
    using System.Threading.Tasks;
    using System.Collections.Generic;


    /// <summary>
    /// 网络状态
    /// </summary>
    public enum NetworkState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
        Reconnecting
    }

    
    /// <summary>
    /// 网络子模块接口
    /// </summary>
    public interface INetworkSubmodule : IGameFeatureSubmodule
    {
        NetworkState State { get; }
        /// <summary> 连接超时时间（毫秒） </summary>
        int Timeout { get; set; }
    }

    
    /// <summary>
    /// 长连接子模块接口
    /// </summary>
    public interface IPersistentConnectionSubmodule : INetworkSubmodule
    {
        void Connect(string address, int port);
        Task ConnectAsync(string address, int port);
        void Disconnect();
        Task DisconnectAsync();
        void Send(byte[] data);
        Task SendAsync(byte[] data);
    }
    
    
    /// <summary>
    /// 短连接子模块接口
    /// </summary>
    public interface ITransientConnectionSubmodule : INetworkSubmodule
    {
        /// <summary> 发送请求并获取响应 </summary>
        Task<string> RequestAsync(
            string method,
            string url, 
            string data = null, 
            string contentType = "application/json",
            Dictionary<string, string> headers = null);
        
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
            System.Action<long, long> progressCallback = null, string expectedHash = null);

        /// <summary> 异步下载文件并保存为内存 </summary>
        Task<byte[]> DownloadFileToMemoryAsync(
            string url,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null);
        
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
            System.Action<long, long> progressCallback = null, string expectedHash = null);
        
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
            System.Action<long, long> progressCallback = null);
        
        /// <summary> 默认请求头 </summary>
        Dictionary<string, string> DefaultHeader { get; }
        /// <summary> Cookie容器 </summary>
        CookieContainer Cookie { get; }
    }
}
