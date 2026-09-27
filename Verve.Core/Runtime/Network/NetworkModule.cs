namespace Verve
{
    using System;
    using System.Net.WebSockets;
#if UNITY_WEBGL && !UNITY_EDITOR
    using Connection = BrowserWebSocket;
#else
    using Connection = System.Net.WebSockets.ClientWebSocket;
#endif
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///   <para>网络模块；管理 WebSocket 连接、收发与释放。</para>
    /// </summary>
    [Serializable, GameModule("网络模块，负责管理需要显式生命周期的 WebSocket 长连接。")]
    public sealed class NetworkModule : GameModule
    {
        /// <summary>
        ///   <para>单条接收消息的默认字节上限。</para>
        /// </summary>
        private const int k_DefaultMaxReceiveMessageBytes = 1024 * 1024;

        /// <summary>
        ///   <para>套接字。</para>
        /// </summary>
        private Connection m_Socket;
        /// <summary>
        ///   <para>生命周期取消源。</para>
        /// </summary>
        private readonly CancellationTokenSource m_Lifetime = new();
        /// <summary>
        ///   <para>停止中。</para>
        /// </summary>
        private bool m_Stopping;
        /// <summary>
        ///   <para>接收缓冲区。</para>
        /// </summary>
        private readonly byte[] m_ReceiveBuffer = new byte[8192];
        /// <summary>
        ///   <para>连接操作互斥门。</para>
        /// </summary>
        private readonly SemaphoreSlim m_ConnectionGate = new(1, 1);
        /// <summary>
        ///   <para>发送操作互斥门。</para>
        /// </summary>
        private readonly SemaphoreSlim m_SendGate = new(1, 1);
        /// <summary>
        ///   <para>接收操作互斥门。</para>
        /// </summary>
        private readonly SemaphoreSlim m_ReceiveGate = new(1, 1);
        /// <summary>
        ///   <para>单条接收消息的字节上限。</para>
        /// </summary>
        private int m_MaxReceiveMessageBytes = k_DefaultMaxReceiveMessageBytes;

        /// <summary>
        ///   <para>连接状态。</para>
        /// </summary>
        public NetworkState State => m_Socket?.State switch
        {
            WebSocketState.Connecting => NetworkState.Connecting,
            WebSocketState.Open => NetworkState.Connected,
            WebSocketState.CloseSent or WebSocketState.CloseReceived => NetworkState.Closing,
            _ => NetworkState.Disconnected,
        };

        /// <summary>
        ///   <para>单条 WebSocket 文本消息的最大字节数，默认 1 MB。</para>
        /// </summary>
        public int MaxReceiveMessageBytes
        {
            get => m_MaxReceiveMessageBytes;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Maximum message size must be positive.");
                }

                m_MaxReceiveMessageBytes = value;
#if UNITY_WEBGL && !UNITY_EDITOR
                m_Socket?.SetReceiveLimit(value);
#endif
            }
        }

        /// <summary>
        ///   <para>异步连接。</para>
        /// </summary>
        /// <param name="uri">URI。</param>
        /// <param name="ct">取消令牌。</param>
        public async Task ConnectAsync(Uri uri, CancellationToken ct = default)
        {
            using var operation = CreateOperationCancellation(ct);
            ct = operation.Token;
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (!string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, "wss", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("WebSocket URI must use ws or wss.", nameof(uri));

            await m_ConnectionGate.WaitAsync(ct);
            try
            {
                await DisconnectCoreAsync(ct);
                ct.ThrowIfCancellationRequested();
#if UNITY_WEBGL && !UNITY_EDITOR
                var socket = new Connection(m_MaxReceiveMessageBytes);
#else
                var socket = new Connection();
#endif
                m_Socket = socket;

                try
                {
                    await socket.ConnectAsync(uri, ct);
                    ct.ThrowIfCancellationRequested();
                }
                catch
                {
                    if (ReferenceEquals(m_Socket, socket)) m_Socket = null;
                    socket.Dispose();
                    throw;
                }
            }
            finally
            {
                m_ConnectionGate.Release();
            }
        }

        /// <summary>
        ///   <para>异步发送文本。</para>
        /// </summary>
        /// <param name="message">待发送的文本。</param>
        /// <param name="ct">取消令牌。</param>
        public async Task SendTextAsync(string message, CancellationToken ct = default)
        {
            using var operation = CreateOperationCancellation(ct);
            ct = operation.Token;
            await m_SendGate.WaitAsync(ct);
            try
            {
                var socket = GetSocket();
                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
            }
            finally
            {
                m_SendGate.Release();
            }
        }

        /// <summary>
        ///   <para>异步接收文本。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        public async Task<string> ReceiveTextAsync(CancellationToken ct = default)
        {
            using var operation = CreateOperationCancellation(ct);
            ct = operation.Token;
            var remoteClosed = false;
            string message = null;
            Connection socket = null;
            await m_ReceiveGate.WaitAsync(ct);
            try
            {
                socket = GetSocket();
                using var result = new System.IO.MemoryStream();
                WebSocketReceiveResult received;
                do
                {
                    received = await socket.ReceiveAsync(new ArraySegment<byte>(m_ReceiveBuffer), ct);
                    if (received.MessageType == WebSocketMessageType.Close)
                    {
                        remoteClosed = true;
                        break;
                    }
                    if (received.MessageType != WebSocketMessageType.Text)
                    {
                        throw new InvalidOperationException("Received a non-text WebSocket message.");
                    }
                    if (result.Length + received.Count > m_MaxReceiveMessageBytes)
                    {
                        throw new InvalidOperationException(
                            $"WebSocket message exceeds the configured limit of {m_MaxReceiveMessageBytes} bytes.");
                    }

                    result.Write(m_ReceiveBuffer, 0, received.Count);
                } while (!received.EndOfMessage);

                if (!remoteClosed)
                {
                    message = Encoding.UTF8.GetString(result.GetBuffer(), 0, (int)result.Length);
                }
            }
            catch
            {
                // 不支持的消息、超限或取消后不继续读取已截断的消息流。
                socket?.Abort();
                throw;
            }
            finally
            {
                m_ReceiveGate.Release();
            }

            if (remoteClosed)
            {
                await m_ConnectionGate.WaitAsync(ct);
                try
                {
                    if (ReferenceEquals(m_Socket, socket)) await DisconnectCoreAsync(ct);
                }
                finally { m_ConnectionGate.Release(); }
            }
            // null 表示对端关闭，空字符串仍是一条合法文本消息。
            return message;
        }

        /// <summary>
        ///   <para>异步断开连接。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        public async Task DisconnectAsync(CancellationToken ct = default)
        {
            using var operation = CreateOperationCancellation(ct);
            ct = operation.Token;
            await m_ConnectionGate.WaitAsync(ct);
            try
            {
                await DisconnectCoreAsync(ct);
            }
            finally
            {
                m_ConnectionGate.Release();
            }
        }

        /// <summary>
        ///   <para>异步断开连接。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private async Task DisconnectCoreAsync(CancellationToken ct)
        {
            var socket = m_Socket;
            m_Socket = null;
            if (socket == null) return;
            try
            {
                await m_SendGate.WaitAsync(ct);
                try
                {
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
                }
                finally { m_SendGate.Release(); }
            }
            finally { socket.Dispose(); }
        }

        /// <inheritdoc />
        protected override ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
        {
            StopConnection();
            return default;
        }

        /// <inheritdoc />
        protected override void OnDispose()
        {
            try { StopConnection(); }
            finally { m_Lifetime.Dispose(); }
            // 不释放仍可能被异步 finally 访问的信号量；未使用 AvailableWaitHandle，不持有系统句柄。
        }

        /// <summary>
        ///   <para>终止连接。</para>
        /// </summary>
        private void StopConnection()
        {
            m_Stopping = true;
            try { m_Lifetime.Cancel(); }
            finally
            {
                m_Socket?.Dispose();
                m_Socket = null;
            }
        }

        /// <summary>
        ///   <para>创建请求取消源。</para>
        /// </summary>
        /// <param name="ct">取消令牌。</param>
        private CancellationTokenSource CreateOperationCancellation(CancellationToken ct)
        {
            Game.ThrowIfNotOnMainThread(nameof(NetworkModule));
            if (IsDisposed || m_Stopping) throw new ObjectDisposedException(nameof(NetworkModule));
            return CancellationTokenSource.CreateLinkedTokenSource(ct, m_Lifetime.Token);
        }

        /// <summary>
        ///   <para>获取套接字。</para>
        /// </summary>
        private Connection GetSocket()
        {
            if (m_Socket == null || m_Socket.State != WebSocketState.Open)
                throw new InvalidOperationException("The WebSocket connection is not open.");
            return m_Socket;
        }
    }
}
