namespace Verve.Net
{
    using System;
    using System.IO;
    using System.Net;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    

    /// <summary>
    /// Http客户端网络子模块
    /// </summary>
    [System.Serializable]
    public partial class HttpClientSubmodule : ITransientConnectionSubmodule
    {
        public string ModuleName => "HttpNetwork";
        public void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies) { }
        public void OnModuleUnloaded() { }


        public NetworkState State { get; protected set; } = NetworkState.Disconnected;
        public int Timeout { get; set; } = 10000;

        public Dictionary<string, string> DefaultHeader { get; set; } = new Dictionary<string, string>()
        {
            { "User-Agent", "VerveGameEngine/1.0" }
        };
        public CookieContainer Cookie { get; protected set; } = new CookieContainer();

        protected static bool CanHaveBody(string method)
        {
            return method.ToUpperInvariant() switch
            {
                "POST" => true,
                "PUT" => true,
                "PATCH" => true,
                "DELETE" => true,
                _ => false
            };
        }

        public virtual async Task<string> RequestAsync(string method, string url, string data = null, string contentType = "application/json",
            Dictionary<string, string> headers = null)
        {
            if (string.IsNullOrWhiteSpace(method))
                throw new System.ArgumentException("HTTP method cannot be null or empty", nameof(method));

            var request = CreateRequest(method, url, contentType, headers);
            
            if (!string.IsNullOrEmpty(data) && CanHaveBody(method))
            {
                using (var stream = await request.GetRequestStreamAsync())
                {
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                return await ReadResponseAsync(response);
            }
        }
        
        public virtual string Request(string method, string url, string data = null, string contentType = "application/json",
            Dictionary<string, string> headers = null)
        {
            var task = RequestAsync(method, url, data, contentType, headers);
            task.Wait(Timeout);
            return task.Result;
        }
        
        private async Task<string> ReadResponseAsync(HttpWebResponse response)
        {
            using (var stream = response.GetResponseStream())
            {
                if (stream == null) return null;
                
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
        
        /// <summary>
        /// 下载文件到指定路径
        /// </summary>
        public virtual async Task DownloadFileAsync(
            string url, 
            string savePath,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null)
        {
            var request = CreateRequest(method, url, null, headers);

            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null) 
                    throw new WebException("Response stream is null");
                
                long totalBytes = response.ContentLength;
                long receivedBytes = 0;
                byte[] buffer = new byte[8192];
                
                
                using (var fileStream = File.Create(savePath))
                {
                    int bytesRead;
                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        receivedBytes += bytesRead;
                        
                        progressCallback?.Invoke(receivedBytes, totalBytes);
                    }
                }
            }
            if (!string.IsNullOrEmpty(expectedHash))
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                await using var stream = File.OpenRead(savePath);
                var hashBytes = md5.ComputeHash(stream);
                var actualHash = BitConverter.ToString(hashBytes).Replace("-", "");
            
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"File hash mismatch: {actualHash} vs {expectedHash}");
            }
        }
        
        public async Task<byte[]> DownloadFileToMemoryAsync(
            string url,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null)
        {
            throw new NotImplementedException();
        }


        public virtual void DownloadFile(
            string url,
            string savePath,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null)
        {
            var task = DownloadFileAsync(url, savePath, method, headers, progressCallback, expectedHash);
            task.Wait(Timeout);
        }
        
        public string UploadFile(string url, string filePath, string method = "POST", Dictionary<string, string> headers = null,
            string contentType = "application/octet-stream", Action<long, long> progressCallback = null)
        {
            var task = UploadFileAsync(url, filePath, method, headers, contentType, progressCallback);
            task.Wait(Timeout);
            return task.Result;
        }
        
        public async Task<string> UploadFileAsync(string url, string filePath, string method = "POST", Dictionary<string, string> headers = null,
            string contentType = "application/octet-stream", Action<long, long> progressCallback = null)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var request = CreateRequest(method, url, contentType, headers);
            request.ContentType = contentType;
        
            using (var fileStream = File.OpenRead(filePath))
            {
                request.ContentLength = fileStream.Length;
            
                using (var requestStream = await request.GetRequestStreamAsync())
                {
                    byte[] buffer = new byte[8192];
                    long totalBytes = fileStream.Length;
                    long sentBytes = 0;
                    int bytesRead;
                
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await requestStream.WriteAsync(buffer, 0, bytesRead);
                        sentBytes += bytesRead;
                        progressCallback?.Invoke(sentBytes, totalBytes);
                    }
                }
            }
        
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                return await ReadResponseAsync(response);
            }
        }

        private HttpWebRequest CreateRequest(
            string method, 
            string url, 
            string contentType,
            Dictionary<string, string> customHeaders)
        {
            var request = WebRequest.Create(url);
            if (request is not HttpWebRequest httpRequest)
            {
                throw new InvalidOperationException($"URL '{url}' 不支持 HTTP 请求。只支持 HTTP/HTTPS 协议");
            }
            httpRequest.Method = method.ToUpperInvariant().Trim();
            httpRequest.Timeout = Timeout;
            httpRequest.CookieContainer = Cookie;
            httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            
            if (!string.IsNullOrEmpty(contentType) && CanHaveBody(method))
            {
                httpRequest.ContentType = contentType;
            }

            // var allHeaders = DefaultHeader
            //     .Concat(customHeaders ?? new Dictionary<string, string>())
            //     .GroupBy(p => p.Key)
            //     .ToDictionary(g => g.Key, g => g.First().Value);

            return httpRequest;
        }
    }
}