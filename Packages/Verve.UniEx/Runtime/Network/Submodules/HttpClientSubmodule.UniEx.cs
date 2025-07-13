#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Net
{
    using Verve;
    using System;
    using Verve.Net;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using UnityEngine.Networking;
    using System.Collections.Generic;
    
    
    [System.Serializable]
    public partial class HttpClientSubmodule : ITransientConnectionSubmodule
    {
        public string ModuleName => "HttpClient";
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies) { }
        public virtual void OnModuleUnloaded() { }
        
        public NetworkState State { get; private set; } = NetworkState.Disconnected;
        public int Timeout { get; set; } = 30;
        
        public Dictionary<string, string> DefaultHeader { get; set; } = new Dictionary<string, string>()
        {
            { "User-Agent", "VerveGameEngine/Unity" }
        };
        
        public CookieContainer Cookie { get; } = new CookieContainer();

        public async Task<string> RequestAsync(
            string method,
            string url, 
            string data = null, 
            string contentType = "application/json",
            Dictionary<string, string> headers = null)
        {
            using var request = CreateUnityWebRequest(method, url, data, contentType, headers);
            await SendRequestAsync(request);
            return ProcessResponse(request);
        }

        public string Request(
            string method,
            string url, 
            string data = null, 
            string contentType = "application/json",
            Dictionary<string, string> headers = null)
        {
            return RequestAsync(method, url, data, contentType, headers).GetAwaiter().GetResult();
        }

        public async Task DownloadFileAsync(
            string url,
            string savePath,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null)
        {
            using var request = UnityWebRequest.Get(url);
            request.timeout = Timeout;
            request.method = method;
            request.downloadHandler = new DownloadHandlerFile(savePath);
            SetupRequestHeaders(request, headers);
            
            if (progressCallback != null)
            {
                request.SendWebRequest().completed += (op) => 
                {
                    progressCallback?.Invoke(
                        (long)request.downloadedBytes, 
                        (long)request.downloadHandler.data.Length
                    );
                };
            }
            
            await SendRequestAsync(request);
            
            if (!string.IsNullOrEmpty(expectedHash) && 
                !ValidateFileHash(savePath, expectedHash))
            {
                File.Delete(savePath);
                throw new InvalidDataException("File hash mismatch");
            }
        }
        
        public async Task<byte[]> DownloadFileToMemoryAsync(
            string url,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null)
        {
            using var request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Timeout
            };

            SetupRequestHeaders(request, headers);
            State = NetworkState.Connecting;
            var operation = request.SendWebRequest();

            ulong lastReportedBytes = 0;
            while (!operation.isDone)
            {
                await Task.Yield();
                if (progressCallback != null && request.downloadedBytes != lastReportedBytes)
                {
                    progressCallback?.Invoke((long)request.downloadedBytes, (long)request.downloadHandler.data.Length);
                    lastReportedBytes = (ulong)request.downloadedBytes;
                }
            }

            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                State = NetworkState.Disconnected;
                throw new WebException($"HTTP error: {request.error} ({request.responseCode})");
            }

            State = NetworkState.Connected;
            byte[] data = request.downloadHandler.data;


            if (!string.IsNullOrEmpty(expectedHash))
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                byte[] hashBytes = md5.ComputeHash(data);
                string actualHash = BitConverter.ToString(hashBytes).Replace("-", "");
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"File hash mismatch: {actualHash} vs {expectedHash}");
                }
            }

            return data;
        }

        public void DownloadFile(
            string url,
            string savePath,
            string method = "GET",
            Dictionary<string, string> headers = null,
            System.Action<long, long> progressCallback = null,
            string expectedHash = null)
        {
            DownloadFileAsync(url, savePath, method, headers, progressCallback, expectedHash)
                .GetAwaiter().GetResult();
        }

        public async Task<string> UploadFileAsync(
            string url,
            string filePath,
            string method = "POST",
            Dictionary<string, string> headers = null,
            string contentType = "application/octet-stream",
            System.Action<long, long> progressCallback = null)
        {
            using var request = new UnityWebRequest(url, method);
            request.uploadHandler = new UploadHandlerFile(filePath);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", contentType);
            SetupRequestHeaders(request, headers);
            
            if (progressCallback != null)
            {
                request.SendWebRequest().completed += (op) => 
                {
                    progressCallback?.Invoke(
                        (long)request.uploadedBytes, 
                        (long)request.uploadHandler.data.Length
                    );
                };
            }
            
            await SendRequestAsync(request);
            return request.downloadHandler.text;
        }

        public string UploadFile(
            string url,
            string filePath,
            string method = "POST",
            Dictionary<string, string> headers = null,
            string contentType = "application/octet-stream",
            System.Action<long, long> progressCallback = null)
        {
            return UploadFileAsync(url, filePath, method, headers, contentType, progressCallback)
                .GetAwaiter().GetResult();
        }

        private UnityWebRequest CreateUnityWebRequest(
            string method,
            string url,
            string data,
            string contentType,
            Dictionary<string, string> headers)
        {
            var request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Timeout
            };

            if (!string.IsNullOrEmpty(data))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", contentType);
            }

            SetupRequestHeaders(request, headers);
            return request;
        }

        private void SetupRequestHeaders(UnityWebRequest request, Dictionary<string, string> headers)
        {
            foreach (var header in DefaultHeader)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        private async Task SendRequestAsync(UnityWebRequest request)
        {
            State = NetworkState.Connecting;
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                State = NetworkState.Disconnected;
                throw new WebException($"HTTP error: {request.error} ({request.responseCode})");
            }

            State = NetworkState.Connected;
        }

        private string ProcessResponse(UnityWebRequest request)
        {
            return request.downloadHandler?.text;
        }

        private bool ValidateFileHash(string path, string expectedHash)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = System.IO.File.OpenRead(path);
            var hashBytes = md5.ComputeHash(stream);
            var actualHash = BitConverter.ToString(hashBytes).Replace("-", "");
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}

#endif