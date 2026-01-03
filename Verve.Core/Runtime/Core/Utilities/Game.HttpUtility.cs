namespace Verve
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
    using UnityEngine.Networking;
#else
    using System.Linq;
    using System.Net.Http;
#endif
    
    
    /// <summary>
    ///   <para>游戏入口：工具部分</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>HTTP网络工具类</para>
        /// </summary>
        public static class HttpUtility
        {
            /// <summary>
            ///   <para>发送GET请求</para>
            /// </summary>
            /// <param name="url">请求地址</param>
            /// <param name="headers">请求头</param>
            /// <param name="timeout">超时时间（秒）</param>
            public static async Task<string> Get(string url, Dictionary<string, string> headers = null, float timeout = 5f)
            {
#if UNITY_5_3_OR_NEWER
                var cts = new CancellationTokenSource();
                using var webRequest = UnityWebRequest.Get(url);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                return await SendWebRequest(webRequest, cts, timeout);
#else
                return await SendHttpRequest(HttpMethod.Get, url, null, headers, timeout);
#endif
            }
            
            /// <summary>
            ///   <para>发送POST请求（JSON格式）</para>
            /// </summary>
            /// <param name="url">请求地址</param>
            /// <param name="jsonData">JSON数据</param>
            /// <param name="headers">请求头</param>
            /// <param name="timeout">超时时间（秒）</param>
            public static async Task<string> PostJson(string url, string jsonData, Dictionary<string, string> headers = null, float timeout = 5f, Encoding encoding = null)
            {
#if UNITY_5_3_OR_NEWER
                var cts = new CancellationTokenSource();
                using var webRequest = new UnityWebRequest(url, "POST");
                
                var bodyRaw = (encoding ?? Encoding.UTF8).GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                return await SendWebRequest(webRequest, cts, timeout);
#else
                return await SendHttpRequest(HttpMethod.Post, url, new StringContent(jsonData, (encoding ?? Encoding.UTF8), "application/json"), headers, timeout);
#endif
            }

            /// <summary>
            ///   <para>发送POST请求（表单格式）</para>
            /// </summary>
            /// <param name="url">请求地址</param>
            /// <param name="formData">表单数据</param>
            /// <param name="headers">请求头</param>
            /// <param name="timeout">超时时间（秒）</param>
            public static async Task<string> PostForm(string url, Dictionary<string, string> formData, Dictionary<string, string> headers = null, float timeout = 5f)
            {
#if UNITY_5_3_OR_NEWER
                var cts = new CancellationTokenSource();
                var form = new WWWForm();
                
                if (formData != null)
                {
                    foreach (var kvp in formData)
                    {
                        form.AddField(kvp.Key, kvp.Value);
                    }
                }
                
                using var webRequest = UnityWebRequest.Post(url, form);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                return await SendWebRequest(webRequest, cts, timeout);
#else
                var content = new FormUrlEncodedContent(formData ?? new Dictionary<string, string>());
                return await SendHttpRequest(HttpMethod.Post, url, content, headers, timeout);
#endif
            }

            /// <summary>
            ///   <para>发送PUT请求</para>
            /// </summary>
            /// <param name="url">请求地址</param>
            /// <param name="content">内容</param>
            /// <param name="headers">请求头</param>
            /// <param name="timeout">超时时间（秒）</param>
            /// <param name="encoding">编码</param>
            public static async Task<string> Put(string url, string content, Dictionary<string, string> headers = null, float timeout = 5f, Encoding encoding = null)
            {
#if UNITY_5_3_OR_NEWER
                var cts = new CancellationTokenSource();
                using var webRequest = UnityWebRequest.Put(url, content);
                webRequest.SetRequestHeader("Content-Type", "text/plain");
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                return await SendWebRequest(webRequest, cts, timeout);
#else
                return await SendHttpRequest(HttpMethod.Put, url, new StringContent(content ?? "", encoding ?? Encoding.UTF8), headers, timeout);
#endif
            }

            /// <summary>
            ///   <para>发送DELETE请求</para>
            /// </summary>
            /// <param name="url">请求地址</param>
            /// <param name="headers">请求头</param>
            /// <param name="timeout">超时时间（秒）</param>
            public static async Task<string> Delete(string url, Dictionary<string, string> headers = null, float timeout = 5f)
            {
#if UNITY_5_3_OR_NEWER
                var cts = new CancellationTokenSource();
                using var webRequest = UnityWebRequest.Delete(url);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                return await SendWebRequest(webRequest, cts, timeout);
#else
                return await SendHttpRequest(HttpMethod.Delete, url, null, headers, timeout);
#endif
            }

            /// <summary>
            ///   <para>下载文件到指定路径</para>
            /// </summary>
            /// <param name="url">文件URL</param>
            /// <param name="savePath">保存路径</param>
            /// <param name="headers">请求头</param>
            /// <param name="timeout">超时时间（秒）</param>
            /// <param name="progressCallback">进度回调 (0-1)</param>
            /// <param name="cancellationToken">取消令牌</param>
            public static async Task<bool> DownloadFile(
                string url,
                string savePath,
                Dictionary<string, string> headers = null, 
                float timeout = 60f,
                Action<float> progressCallback = null,
                CancellationToken cancellationToken = default)
            {
                try
                {
                    var directory = Path.GetDirectoryName(savePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

#if UNITY_5_3_OR_NEWER
                    return await DownloadFileUnity(url, savePath, headers, timeout, progressCallback, cancellationToken);
#else
                    return await DownloadFileNet(url, savePath, headers, timeout, progressCallback, cancellationToken);
#endif
                }
                catch (Exception ex)
                {
                    LogError($"Download file exception: {ex.Message} - URL: {url}, SavePath: {savePath}");
                    return false;
                }
            }

#if UNITY_5_3_OR_NEWER
            /// <summary>
            ///   <para>Unity平台下载文件实现</para>
            /// </summary>
            private static async Task<bool> DownloadFileUnity(
                string url,
                string savePath,
                Dictionary<string, string> headers, 
                float timeout,
                Action<float> progressCallback,
                CancellationToken cancellationToken)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                using var webRequest = UnityWebRequest.Get(url);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                webRequest.timeout = (int)timeout;
                webRequest.downloadHandler = new DownloadHandlerFile(savePath);
                webRequest.disposeDownloadHandlerOnDispose = true;
                
                var timeoutCts = new CancellationTokenSource();
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));
                
                try
                {
                    var asyncOperation = webRequest.SendWebRequest();
                    using var timeoutRegistration = timeoutCts.Token.Register(() => cts.Cancel());
                    
                    while (!asyncOperation.isDone)
                    {
                        if (cancellationToken.IsCancellationRequested || cts.Token.IsCancellationRequested)
                        {
                            webRequest.Abort();
                            LogWarning($"Download cancelled: {url}");
                            return false;
                        }
                        
                        progressCallback?.Invoke(asyncOperation.progress);
                        await Task.Yield();
                    }
                    
                    if (cancellationToken.IsCancellationRequested || cts.Token.IsCancellationRequested)
                    {
                        LogWarning($"Download cancelled: {url}");
                        return false;
                    }
                    
                    if (webRequest.result != UnityWebRequest.Result.Success)
                    {
                        LogError($"Download failed: {webRequest.error} - URL: {url}");
                        return false;
                    }
                    
                    progressCallback?.Invoke(1f);
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"Download exception: {ex.Message} - URL: {url}");
                    return false;
                }
                finally
                {
                    timeoutCts?.Dispose();
                    cts?.Dispose();
                }
            }
#else
            /// <summary>
            ///   <para>.NET平台下载文件实现</para>
            /// </summary>
            private static async Task<bool> DownloadFileNet(
                string url,
                string savePath,
                Dictionary<string, string> headers, 
                float timeout,
                Action<float> progressCallback,
                CancellationToken cancellationToken)
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                
                try
                {
                    using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        LogError($"Download failed: {response.StatusCode} - {response.ReasonPhrase} - URL: {url}");
                        return false;
                    }
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var downloadedBytes = 0L;
                    
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                    
                    var buffer = new byte[8192];
                    int bytesRead;
                    
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            LogWarning($"Download cancelled: {url}");
                            return false;
                        }
                        
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        
                        downloadedBytes += bytesRead;
                        if (totalBytes > 0)
                        {
                            var progress = (float)downloadedBytes / totalBytes;
                            progressCallback?.Invoke(progress);
                        }
                    }
                    
                    progressCallback?.Invoke(1f);
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"Download exception: {ex.Message} - URL: {url}");
                    return false;
                }
            }
#endif
            
            /// <summary>
            ///   <para>下载字节数组</para>
            /// </summary>
            public static async Task<byte[]> DownloadBytes(string url, Dictionary<string, string> headers = null, float timeout = 30f)
            {
#if UNITY_5_3_OR_NEWER
                var cts = new CancellationTokenSource();
                using var webRequest = UnityWebRequest.Get(url);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        webRequest.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                await SendWebRequest(webRequest, cts, timeout);
                
                return webRequest.downloadHandler?.data;
#else
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                
                try
                {
                    return await httpClient.GetByteArrayAsync(url);
                }
                catch (Exception ex)
                {
                    LogError($"Download bytes exception: {ex.Message} - URL: {url}");
                    return null;
                }
#endif
            }

#if UNITY_5_3_OR_NEWER
            /// <summary>
            ///   <para>Unity平台发送HTTP请求的通用方法</para>
            /// </summary>
            private static async Task<string> SendWebRequest(UnityWebRequest webRequest, CancellationTokenSource cts, float timeout = 5f)
            {
                webRequest.timeout = (int)timeout;
                
                var timeoutCts = new CancellationTokenSource();
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));
                
                try
                {
                    var asyncOperation = webRequest.SendWebRequest();
                    
                    using var timeoutRegistration = timeoutCts.Token.Register(cts.Cancel);
                    
                    while (!asyncOperation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (cts.Token.IsCancellationRequested)
                    {
                        LogWarning($"HTTP Request canceled: {webRequest.url}");
                        return string.Empty;
                    }
                    
                    if (webRequest.result != UnityWebRequest.Result.Success)
                    {
                        LogError($"HTTP Request failed: {webRequest.error} - URL: {webRequest.url}");
                        return string.Empty;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (ex.CancellationToken == cts.Token || ex.CancellationToken == timeoutCts.Token)
                    {
                        LogWarning($"HTTP Request timeout: {webRequest.url}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"HTTP Request exception: {ex.Message} - URL: {webRequest.url}");
                }
                finally
                {
                    timeoutCts?.Dispose();
                    cts?.Dispose();
                }
                
                return webRequest.downloadHandler.text;
            }
#else
            /// <summary>
            ///   <para>发送HTTP请求的通用方法</para>
            /// </summary>
            private static async Task<string> SendHttpRequest(HttpMethod method, string url, HttpContent content, Dictionary<string, string> headers, float timeout = 5f)
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(timeout);
                
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
                
                try
                {
                    HttpResponseMessage response;
                    
                    switch (method.Method)
                    {
                        case "GET":
                            response = await httpClient.GetAsync(url);
                            break;
                        case "POST":
                            response = await httpClient.PostAsync(url, content);
                            break;
                        case "PUT":
                            response = await httpClient.PutAsync(url, content);
                            break;
                        case "DELETE":
                            response = await httpClient.DeleteAsync(url);
                            break;
                        default:
                            throw new NotSupportedException($"HTTP method {method.Method} is not supported");
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        LogError($"HTTP Request failed: {response.StatusCode} - {response.ReasonPhrase} - URL: {url}");
                        return string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"HTTP Request exception: {ex.Message} - URL: {url}");
                    return string.Empty;
                }
            }
#endif
        }
    }
}