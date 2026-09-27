#if UNITY_WEBGL && !UNITY_EDITOR

namespace Verve
{
    using System;
    using System.Net.WebSockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///   <para>浏览器 WebSocket 适配；由网络模块拥有，使用主线程轮询等待浏览器事件。</para>
    /// </summary>
    internal sealed class BrowserWebSocket : DisposableObject
    {
        /// <summary>
        ///   <para>浏览器连接标识；零表示尚未连接或已经释放。</para>
        /// </summary>
        private int m_Id;
        /// <summary>
        ///   <para>连接释放后的状态。</para>
        /// </summary>
        private WebSocketState m_FinalState = WebSocketState.None;
        /// <summary>
        ///   <para>接收队列字节上限。</para>
        /// </summary>
        private readonly int m_MaxBufferedBytes;

        /// <summary>
        ///   <para>连接状态。</para>
        /// </summary>
        internal WebSocketState State => m_Id == 0 ? m_FinalState : (WebSocketState)VerveWebSocketState(m_Id);

        /// <summary>
        ///   <para>创建浏览器连接。</para>
        /// </summary>
        /// <param name="maxBufferedBytes">接收队列字节上限。</param>
        internal BrowserWebSocket(int maxBufferedBytes) => m_MaxBufferedBytes = maxBufferedBytes;

        /// <summary>
        ///   <para>连接服务器。</para>
        /// </summary>
        /// <param name="uri">服务器地址。</param>
        /// <param name="ct">取消令牌。</param>
        internal async Task ConnectAsync(Uri uri, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            m_Id = VerveWebSocketCreate(uri.AbsoluteUri, m_MaxBufferedBytes);
            while (State == WebSocketState.Connecting)
            {
                CheckOperation(ct);
                await Task.Yield();
            }
            CheckOperation(ct);
            if (State != WebSocketState.Open) throw new WebSocketException("WebSocket closed before connecting.");
        }

        /// <summary>
        ///   <para>发送完整文本消息。</para>
        /// </summary>
        /// <param name="buffer">UTF-8 消息。</param>
        /// <param name="messageType">消息类型。</param>
        /// <param name="endOfMessage">是否为完整消息。</param>
        /// <param name="ct">取消令牌。</param>
        internal Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken ct)
        {
            CheckOperation(ct);
            if (messageType != WebSocketMessageType.Text || !endOfMessage)
                throw new NotSupportedException("The browser adapter sends complete text messages.");
            VerveWebSocketSend(m_Id, buffer.Array, buffer.Offset, buffer.Count);
            CheckOperation(ct);
            return Task.CompletedTask;
        }

        /// <summary>
        ///   <para>接收文本消息片段。</para>
        /// </summary>
        /// <param name="buffer">接收缓冲区。</param>
        /// <param name="ct">取消令牌。</param>
        internal async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken ct)
        {
            while (true)
            {
                CheckOperation(ct);
                // -1 表示暂无消息；零长度仍是一条完整消息。
                int remaining = VerveWebSocketPeek(m_Id);
                if (remaining >= 0)
                {
                    int count = Math.Min(buffer.Count, remaining);
                    VerveWebSocketReceive(m_Id, buffer.Array, buffer.Offset, count);
                    return new WebSocketReceiveResult(count, WebSocketMessageType.Text, count == remaining);
                }
                if (State == WebSocketState.CloseReceived || State == WebSocketState.Closed)
                    return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
                await Task.Yield();
            }
        }

        /// <summary>
        ///   <para>发送关闭帧。</para>
        /// </summary>
        /// <param name="status">关闭状态。</param>
        /// <param name="description">关闭原因。</param>
        /// <param name="ct">取消令牌。</param>
        internal Task CloseOutputAsync(WebSocketCloseStatus status, string description, CancellationToken ct)
        {
            CheckOperation(ct);
            VerveWebSocketClose(m_Id, (int)status, description);
            CheckOperation(ct);
            return Task.CompletedTask;
        }

        /// <summary>
        ///   <para>同步模块的接收上限。</para>
        /// </summary>
        /// <param name="limit">字节上限。</param>
        internal void SetReceiveLimit(int limit)
        {
            if (m_Id != 0) VerveWebSocketSetLimit(m_Id, limit);
        }

        /// <summary>
        ///   <para>中止连接并释放浏览器队列。</para>
        /// </summary>
        internal void Abort()
        {
            Dispose();
            m_FinalState = WebSocketState.Aborted;
        }

        /// <inheritdoc />
        protected override void OnDispose()
        {
            if (m_Id != 0) VerveWebSocketRelease(m_Id);
            m_Id = 0;
            m_FinalState = WebSocketState.Closed;
        }

        /// <summary>
        ///   <para>检查取消、释放与浏览器错误。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private void CheckOperation(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var error = VerveWebSocketError(m_Id);
            if (!string.IsNullOrEmpty(error)) throw new WebSocketException(error);
        }

        /// <summary><para>创建浏览器连接。</para></summary>
        /// <param name="uri">地址。</param>
        /// <param name="maxBufferedBytes">缓存字节上限。</param>
        [DllImport("__Internal")] private static extern int VerveWebSocketCreate(string uri, int maxBufferedBytes);
        /// <summary><para>读取连接状态。</para></summary>
        /// <param name="id">连接标识。</param>
        [DllImport("__Internal")] private static extern int VerveWebSocketState(int id);
        /// <summary><para>读取浏览器错误。</para></summary>
        /// <param name="id">连接标识。</param>
        [DllImport("__Internal")] private static extern string VerveWebSocketError(int id);
        /// <summary><para>发送 UTF-8 文本。</para></summary>
        /// <param name="id">连接标识。</param>
        /// <param name="buffer">数据。</param>
        /// <param name="offset">起始位置。</param>
        /// <param name="count">字节数。</param>
        [DllImport("__Internal")] private static extern void VerveWebSocketSend(int id, byte[] buffer, int offset, int count);
        /// <summary><para>读取下一条消息的剩余长度。</para></summary>
        /// <param name="id">连接标识。</param>
        [DllImport("__Internal")] private static extern int VerveWebSocketPeek(int id);
        /// <summary><para>读取下一段消息。</para></summary>
        /// <param name="id">连接标识。</param>
        /// <param name="buffer">缓冲区。</param>
        /// <param name="offset">起始位置。</param>
        /// <param name="count">字节数。</param>
        [DllImport("__Internal")] private static extern void VerveWebSocketReceive(int id, [Out] byte[] buffer, int offset, int count);
        /// <summary><para>发送关闭帧。</para></summary>
        /// <param name="id">连接标识。</param>
        /// <param name="status">关闭状态。</param>
        /// <param name="description">关闭原因。</param>
        [DllImport("__Internal")] private static extern void VerveWebSocketClose(int id, int status, string description);
        /// <summary><para>释放连接及缓存。</para></summary>
        /// <param name="id">连接标识。</param>
        [DllImport("__Internal")] private static extern void VerveWebSocketRelease(int id);
        /// <summary><para>更新接收上限。</para></summary>
        /// <param name="id">连接标识。</param>
        /// <param name="limit">字节上限。</param>
        [DllImport("__Internal")] private static extern void VerveWebSocketSetLimit(int id, int limit);
    }
}

#endif
