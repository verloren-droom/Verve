#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Diagnostics;
    using UnityEngine.Networking;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>HTTP 工具；在 Unity 主线程发送请求，失败和取消由任务抛出。</para>
        /// </summary>
        /// <remarks>
        ///   <para>工具创建的请求自动释放；<see cref="SendAsync"/> 借用的请求由调用方释放。</para>
        /// </remarks>
        public static class HttpUtility
        {
            /// <summary>
            ///   <para>发送 GET 请求。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static async Task<string> Get(string url, Dictionary<string, string> headers = null,
                float? timeout = null, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = UnityWebRequest.Get(url);
                return await SendTextAsync(request, headers, timeout, cancellationToken);
            }

            /// <summary>
            ///   <para>发送 JSON POST 请求。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="jsonData">JSON 数据。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="encoding">编码。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static Task<string> PostJson(string url, string jsonData, Dictionary<string, string> headers = null,
                float? timeout = null, Encoding encoding = null, CancellationToken cancellationToken = default)
                => SendUploadAsync(url, "POST", jsonData, "application/json", headers, timeout, encoding, cancellationToken);

            /// <summary>
            ///   <para>发送表单 POST 请求。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="formData">表单数据。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static async Task<string> PostForm(string url, Dictionary<string, string> formData,
                Dictionary<string, string> headers = null, float? timeout = null, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = UnityWebRequest.Post(url, formData);
                return await SendTextAsync(request, headers, timeout, cancellationToken);
            }

            /// <summary>
            ///   <para>发送 PUT 请求。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="content">内容。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="encoding">编码。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static Task<string> Put(string url, string content, Dictionary<string, string> headers = null,
                float? timeout = null, Encoding encoding = null, CancellationToken cancellationToken = default)
                => SendUploadAsync(url, "PUT", content, "text/plain", headers, timeout, encoding, cancellationToken);

            /// <summary>
            ///   <para>发送 DELETE 请求。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static async Task<string> Delete(string url, Dictionary<string, string> headers = null,
                float? timeout = null, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = UnityWebRequest.Delete(url);
                request.downloadHandler = new DownloadHandlerBuffer();
                return await SendTextAsync(request, headers, timeout, cancellationToken);
            }

            /// <summary>
            ///   <para>下载字节。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static async Task<byte[]> DownloadBytes(string url, Dictionary<string, string> headers = null,
                float? timeout = null, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = UnityWebRequest.Get(url);
                SetHeaders(request, headers);
                await SendAsync(request, cancellationToken, timeout);
                return request.downloadHandler.data;
            }

            /// <summary>
            ///   <para>下载完成后原子替换目标；失败或取消时保留原文件并移除临时文件。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="savePath">保存路径。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="progressCallback">进度回调。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static async Task DownloadFile(string url, string savePath, Dictionary<string, string> headers = null,
                float? timeout = null, Action<float> progressCallback = null, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var targetPath = Path.GetFullPath(savePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                var temporaryPath = FileUtility.GetTemporaryFilePath(targetPath);
                try
                {
                    // 先关闭 DownloadHandlerFile，再移动文件，避免 Windows 文件占用。
                    using (var request = new UnityWebRequest(url, "GET"))
                    {
                        request.downloadHandler = new DownloadHandlerFile(temporaryPath);
                        SetHeaders(request, headers);
                        await SendAsync(request, cancellationToken, timeout, progressCallback);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    FileUtility.ReplaceFile(temporaryPath, targetPath);
                }
                finally
                {
                    File.Delete(temporaryPath);
                }
            }

            /// <summary>
            ///   <para>发送 <see cref="UnityWebRequest"/>；失败时中止尚未完成的请求。</para>
            /// </summary>
            /// <param name="request">借用的请求；调用方负责读取响应并释放请求及处理器。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="onProgress">进度回调。</param>
            public static async Task SendAsync(UnityWebRequest request, CancellationToken cancellationToken = default,
                float? timeout = null, Action<float> onProgress = null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ThrowIfNotOnMainThread(nameof(SendAsync));
                if (request == null) throw new ArgumentNullException(nameof(request));
                if (timeout.HasValue && (timeout.Value <= 0 || float.IsNaN(timeout.Value) || float.IsInfinity(timeout.Value)))
                    throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be finite and positive, or null.");

                var timer = timeout.HasValue ? Stopwatch.StartNew() : null;
                var operation = request.SendWebRequest();
                try
                {
                    while (!operation.isDone)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (timer != null && timer.Elapsed.TotalSeconds >= timeout.Value)
                            throw new TimeoutException($"Request timed out: {request.url}");
                        onProgress?.Invoke(operation.progress);
                        await Task.Yield();
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    bool failed = request.result != UnityWebRequest.Result.Success;
                    if (failed)
                        throw new InvalidOperationException($"Request failed ({request.responseCode}): {request.error}; URL: {request.url}");
                    onProgress?.Invoke(1f);
                }
                finally
                {
                    if (!operation.isDone) request.Abort();
                }
            }

            /// <summary>
            ///   <para>上传文本并读取响应；请求与处理器自动释放。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="method">HTTP 方法。</param>
            /// <param name="content">内容。</param>
            /// <param name="contentType">内容类型。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="encoding">编码。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            private static async Task<string> SendUploadAsync(string url, string method, string content, string contentType,
                Dictionary<string, string> headers, float? timeout, Encoding encoding, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytes = (encoding ?? Encoding.UTF8).GetBytes(content);
                using var request = new UnityWebRequest(url, method);
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", contentType);
                return await SendTextAsync(request, headers, timeout, cancellationToken);
            }

            /// <summary>
            ///   <para>异步发送文本。</para>
            /// </summary>
            /// <param name="request">请求。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            private static async Task<string> SendTextAsync(UnityWebRequest request, Dictionary<string, string> headers,
                float? timeout, CancellationToken cancellationToken)
            {
                SetHeaders(request, headers);
                await SendAsync(request, cancellationToken, timeout);
                return request.downloadHandler.text;
            }

            /// <summary>
            ///   <para>设置请求头。</para>
            /// </summary>
            /// <param name="request">请求。</param>
            /// <param name="headers">请求头。</param>
            private static void SetHeaders(UnityWebRequest request, Dictionary<string, string> headers)
            {
                if (headers == null) return;
                foreach (var pair in headers) request.SetRequestHeader(pair.Key, pair.Value);
            }
        }
    }
}

#endif