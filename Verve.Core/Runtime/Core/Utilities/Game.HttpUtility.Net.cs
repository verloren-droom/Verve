#if !UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>游戏入口；工具部分。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>非 Unity 平台的 HTTP 适配。</para>
        /// </summary>
        public static class HttpUtility
        {
            /// <summary>
            ///   <para>共享 HTTP 客户端。</para>
            /// </summary>
            private static readonly HttpClient s_Client = new() { Timeout = Timeout.InfiniteTimeSpan };

            /// <summary>
            ///   <para>发送 GET 请求。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static Task<string> Get(string url, Dictionary<string, string> headers = null,
                float? timeout = null, CancellationToken cancellationToken = default)
                => SendAsync(HttpMethod.Get, url, null, headers, timeout, cancellationToken, ReadTextAsync);

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
                => SendAsync(HttpMethod.Post, url, new StringContent(jsonData, encoding ?? Encoding.UTF8, "application/json"),
                    headers, timeout, cancellationToken, ReadTextAsync);

            /// <summary>
            ///   <para>发送表单 POST 请求。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="formData">表单数据。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static Task<string> PostForm(string url, Dictionary<string, string> formData,
                Dictionary<string, string> headers = null, float? timeout = null, CancellationToken cancellationToken = default)
                => SendAsync(HttpMethod.Post, url, new FormUrlEncodedContent(formData), headers, timeout, cancellationToken, ReadTextAsync);

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
                => SendAsync(HttpMethod.Put, url, new StringContent(content, encoding ?? Encoding.UTF8),
                    headers, timeout, cancellationToken, ReadTextAsync);

            /// <summary>
            ///   <para>发送 DELETE 请求。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static Task<string> Delete(string url, Dictionary<string, string> headers = null,
                float? timeout = null, CancellationToken cancellationToken = default)
                => SendAsync(HttpMethod.Delete, url, null, headers, timeout, cancellationToken, ReadTextAsync);

            /// <summary>
            ///   <para>下载字节。</para>
            /// </summary>
            /// <param name="url">URL。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="cancellationToken">取消令牌。</param>
            public static Task<byte[]> DownloadBytes(string url, Dictionary<string, string> headers = null,
                float? timeout = null, CancellationToken cancellationToken = default)
                => SendAsync(HttpMethod.Get, url, null, headers, timeout, cancellationToken, ReadBytesAsync);

            /// <summary>
            ///   <para>下载文件。</para>
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
                    await SendAsync(HttpMethod.Get, url, null, headers, timeout, cancellationToken, async (response, ct) =>
                    {
                        using var input = await response.Content.ReadAsStreamAsync();
                        using var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, true);
                        var buffer = new byte[8192];
                        long downloaded = 0;
                        long? total = response.Content.Headers.ContentLength;
                        int count;
                        while ((count = await input.ReadAsync(buffer, 0, buffer.Length, ct)) != 0)
                        {
                            await output.WriteAsync(buffer, 0, count, ct);
                            downloaded += count;
                            if (total > 0) progressCallback?.Invoke((float)downloaded / total.Value);
                        }
                        progressCallback?.Invoke(1f);
                        return true;
                    });
                    cancellationToken.ThrowIfCancellationRequested();
                    FileUtility.ReplaceFile(temporaryPath, targetPath);
                }
                finally { File.Delete(temporaryPath); }
            }

            /// <summary>
            ///   <para>异步发送。</para>
            /// </summary>
            /// <param name="method">方法。</param>
            /// <param name="url">URL。</param>
            /// <param name="content">内容。</param>
            /// <param name="headers">请求头。</param>
            /// <param name="timeout">超时时间（秒）；null 表示不限时。</param>
            /// <param name="ct">取消令牌。</param>
            /// <param name="readResponse">响应正文读取委托。</param>
            /// <typeparam name="T">数据类型。</typeparam>
            private static async Task<T> SendAsync<T>(HttpMethod method, string url, HttpContent content,
                Dictionary<string, string> headers, float? timeout, CancellationToken ct,
                Func<HttpResponseMessage, CancellationToken, Task<T>> readResponse)
            {
                using var request = new HttpRequestMessage(method, url) { Content = content };
                ct.ThrowIfCancellationRequested();
                if (timeout.HasValue && (timeout.Value <= 0 || float.IsNaN(timeout.Value) || float.IsInfinity(timeout.Value)))
                    throw new ArgumentOutOfRangeException(nameof(timeout));
                if (headers != null)
                    foreach (var header in headers)
                    {
                        // HttpClient 将内容头与请求头分开存放；UnityWebRequest 使用同一个入口。
                        if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                        {
                            if (content == null) throw new InvalidOperationException("Content headers require request content.");
                            content.Headers.Remove(header.Key);
                            content.Headers.Add(header.Key, header.Value);
                        }
                        else request.Headers.Add(header.Key, header.Value);
                    }
                using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (timeout.HasValue) cancellation.CancelAfter(TimeSpan.FromSeconds(timeout.Value));
                try
                {
                    using var response = await s_Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
                    response.EnsureSuccessStatusCode();
                    var result = await readResponse(response, cancellation.Token);
                    cancellation.Token.ThrowIfCancellationRequested();
                    return result;
                }
                catch (OperationCanceledException error) when (!ct.IsCancellationRequested && cancellation.IsCancellationRequested)
                {
                    throw new TimeoutException($"Request timed out: {url}", error);
                }
            }

            /// <summary>
            ///   <para>异步读取字节。</para>
            /// </summary>
            /// <param name="response">响应。</param>
            /// <param name="ct">取消令牌。</param>
            private static async Task<byte[]> ReadBytesAsync(HttpResponseMessage response, CancellationToken ct)
            {
                using var input = await response.Content.ReadAsStreamAsync();
                using var output = new MemoryStream();
                await input.CopyToAsync(output, 81920, ct);
                return output.ToArray();
            }

            /// <summary>
            ///   <para>异步读取文本。</para>
            /// </summary>
            /// <param name="response">响应。</param>
            /// <param name="ct">取消令牌。</param>
            private static async Task<string> ReadTextAsync(HttpResponseMessage response, CancellationToken ct)
            {
                var charset = response.Content.Headers.ContentType?.CharSet;
                var encoding = charset == null ? Encoding.UTF8 : Encoding.GetEncoding(charset.Trim('"'));
                return encoding.GetString(await ReadBytesAsync(response, ct));
            }
        }
    }
}

#endif