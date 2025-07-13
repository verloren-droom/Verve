namespace Verve.Net
{
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 网络功能
    /// </summary>
    [System.Serializable]
    public class NetworkFeature : ModularGameFeature
    {
        protected override void OnLoad(IReadOnlyFeatureDependencies dependencies)
        {
            RegisterSubmodule(new HttpClientSubmodule());
            
            base.OnLoad(dependencies);
        }

        // public virtual async Task ConnectAsync<TNetwork>(string address, int port)
        //     where TNetwork : class, IPersistentConnectionSubmodule =>
        //     await GetSubmodule<TNetwork>().ConnectAsync(address, port);
        //
        // public virtual async Task DisconnectAsync<TNetwork>()
        //     where TNetwork : class, IPersistentConnectionSubmodule =>
        //     await GetSubmodule<TNetwork>().DisconnectAsync();
        //
        // public virtual async Task SendAsync<TNetwork>(byte[] data)
        //     where TNetwork : class, IPersistentConnectionSubmodule =>
        //     await GetSubmodule<TNetwork>().SendAsync(data);
        //
        // public virtual async Task SendAsync<TNetwork>(string message)
        //     where TNetwork : class, IPersistentConnectionSubmodule =>
        //     await GetSubmodule<TNetwork>().SendAsync(System.Text.Encoding.UTF8.GetBytes(message));
        //
        // public virtual async Task<string> RequestAsync<TNetwork>(            
        //     string method,
        //     string url, 
        //     string data = null, 
        //     string contentType = "application/json",
        //     Dictionary<string, string> headers = null)
        // where TNetwork : class, ITransientConnectionSubmodule =>
        //     await GetSubmodule<TNetwork>().RequestAsync(method, url, data, contentType, headers);
        //
        // public string Request<TNetwork>(            
        //     string method,
        //     string url, 
        //     string data = null, 
        //     string contentType = "application/json",
        //     Dictionary<string, string> headers = null)
        //     where TNetwork : class, ITransientConnectionSubmodule =>
        //     GetSubmodule<TNetwork>().Request(method, url, data, contentType, headers);
        //
        // public virtual async Task DownloadFileAsync<TNetwork>(
        //     string url,
        //     string savePath,
        //     string method = "GET",
        //     Dictionary<string, string> headers = null,
        //     System.Action<long, long> progressCallback = null,
        //     string expectedHash = null)
        //     where TNetwork : class, ITransientConnectionSubmodule =>
        //     await GetSubmodule<TNetwork>().DownloadFileAsync(url, savePath, method, headers, progressCallback, expectedHash);
        //
        // public virtual void DownloadFile<TNetwork>(
        //     string url,
        //     string savePath,
        //     string method = "GET",
        //     Dictionary<string, string> headers = null,
        //     System.Action<long, long> progressCallback = null,
        //     string expectedHash = null)
        //     where TNetwork : class, ITransientConnectionSubmodule =>
        //     GetSubmodule<TNetwork>().DownloadFile(url, savePath, method, headers, progressCallback, expectedHash);
        //
        // public string UploadFile<TNetwork>(string url, string filePath, string method = "POST",
        //     Dictionary<string, string> headers = null,
        //     string contentType = "application/octet-stream", System.Action<long, long> progressCallback = null)
        //     where TNetwork : class, ITransientConnectionSubmodule
        //     => GetSubmodule<TNetwork>().UploadFile(url, filePath, method, headers, contentType, progressCallback);
        //
        // public async Task<string> UploadFileAsync<TNetwork>(string url, string filePath, string method = "POST",
        //     Dictionary<string, string> headers = null,
        //     string contentType = "application/octet-stream", System.Action<long, long> progressCallback = null)
        //     where TNetwork : class, ITransientConnectionSubmodule
        //     => await GetSubmodule<TNetwork>().UploadFileAsync(url, filePath, method, headers, contentType, progressCallback);
    }
}