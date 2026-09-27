using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Verve.Acc.Net
{
    /// <summary>
    /// QUIC 端点；统一拥有全部连接，释放端点即可关闭子连接。
    /// </summary>
    public sealed class AccEndpoint : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Slice
        {
            public IntPtr Data;
            public UIntPtr Length;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Config
        {
            public uint Version;
            public uint Server;
            public Slice Bind;
            public Slice Cert;
            public Slice Key;
            public Slice Ca;
            public uint MaxStateBytes;
            public uint MaxFrame;
            public uint Queue;
            public uint Connections;
            public uint FrameRate;
            public ulong ByteRate;
            public uint Timeout;
        }

        /// <summary>
        /// 暂时固定托管数组，保证 native 调用期间指针有效。
        /// </summary>
        private sealed class Pin : IDisposable
        {
            private GCHandle m_Handle;

            public Slice Value { get; }

            public Pin(byte[] value)
            {
                if (value == null || value.Length == 0)
                    return;

                m_Handle = GCHandle.Alloc(value, GCHandleType.Pinned);
                Value = new Slice
                {
                    Data = m_Handle.AddrOfPinnedObject(),
                    Length = (UIntPtr)value.Length,
                };
            }

            public void Dispose()
            {
                if (m_Handle.IsAllocated)
                    m_Handle.Free();
            }
        }

        private const string Library = "verve_acc_net";
        private const CallingConvention Convention = CallingConvention.Cdecl;
        private const int Ok = 0;
        private const int Empty = 1;
        private const int Full = 2;
        private const int BufferTooSmall = 3;
        private const int Timeout = 4;

        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_endpoint_create(in Config config, out ulong endpoint);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_endpoint_reload(ulong endpoint, Slice toml);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_endpoint_accept(ulong endpoint, uint timeout, out ulong peer);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_endpoint_connect(ulong endpoint, Slice address, Slice name, out ulong peer);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_endpoint_port(ulong endpoint, out ushort port);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_endpoint_destroy(ulong endpoint);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_peer_destroy(ulong endpoint, ulong peer);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_peer_status(ulong endpoint, ulong peer);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_peer_send(ulong endpoint, ulong peer, Slice packet);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern int verve_acc_peer_receive(
            ulong endpoint, ulong peer, IntPtr buffer, UIntPtr capacity, out UIntPtr length);
        [DllImport(Library, CallingConvention = Convention)]
        private static extern UIntPtr verve_acc_last_error([Out] byte[] buffer, UIntPtr capacity);

        private readonly ulong m_Handle;
        private bool m_Disposed;

        /// <summary>Rust 端点的资源上限；创建后固定。</summary>
        public sealed class Limits
        {
            /// <summary>单帧最大字节数。</summary>
            public uint MaxFrame = 65536;
            /// <summary>单批复制状态上限。</summary>
            public uint MaxStateBytes = 8388608;
            /// <summary>每个连接的排队帧数。</summary>
            public uint QueueFrames = 32;
            /// <summary>端点最大连接数。</summary>
            public uint MaxConnections = 128;
            /// <summary>每个连接每秒最大帧数。</summary>
            public uint FramesPerSecond = 2048;
            /// <summary>每个连接每秒最大字节数。</summary>
            public ulong BytesPerSecond = 16777216;
            /// <summary>连接和帧操作超时（毫秒）。</summary>
            public uint TimeoutMilliseconds = 10000;
        }

        /// <summary>创建服务端或客户端端点。</summary>
        /// <param name="server">是否创建服务端。</param>
        /// <param name="bindAddress">绑定地址，例如 <c>127.0.0.1:9000</c>。</param>
        /// <param name="certificate">DER 证书。</param>
        /// <param name="privateKey">DER 私钥。</param>
        /// <param name="ca">DER 信任根；服务端传入时启用双向 TLS。</param>
        /// <param name="limits">资源上限；为空时使用默认值。</param>
        public AccEndpoint(bool server, string bindAddress, byte[] certificate, byte[] privateKey, byte[] ca, Limits limits = null)
        {
            if (string.IsNullOrWhiteSpace(bindAddress))
                throw new ArgumentException("Bind address cannot be empty.", nameof(bindAddress));

            limits ??= new Limits();
            using var bind = new Pin(Encoding.UTF8.GetBytes(bindAddress));
            using var cert = new Pin(certificate);
            using var key = new Pin(privateKey);
            using var authority = new Pin(ca);
            var config = new Config
            {
                Version = 2,
                Server = server ? 1u : 0u,
                Bind = bind.Value,
                Cert = cert.Value,
                Key = key.Value,
                Ca = authority.Value,
                MaxStateBytes = limits.MaxStateBytes,
                MaxFrame = limits.MaxFrame,
                Queue = limits.QueueFrames,
                Connections = limits.MaxConnections,
                FrameRate = limits.FramesPerSecond,
                ByteRate = limits.BytesPerSecond,
                Timeout = limits.TimeoutMilliseconds,
            };

            Check(verve_acc_endpoint_create(in config, out m_Handle));
        }

        /// <summary>当前监听端口。</summary>
        public ushort Port
        {
            get
            {
                CheckAlive();
                Check(verve_acc_endpoint_port(m_Handle, out var port));
                return port;
            }
        }

        /// <summary>重载 TOML 接收速率；固定参数变化或无效输入会报错。</summary>
        /// <param name="toml">完整配置文本；省略项使用默认值。</param>
        public void Reload(string toml)
        {
            CheckAlive();
            using var input = new Pin(Encoding.UTF8.GetBytes(toml));
            Check(verve_acc_endpoint_reload(m_Handle, input.Value));
        }

        /// <summary>等待连接；超时返回 <see langword="null"/>。</summary>
        /// <param name="timeoutMilliseconds">等待时长。</param>
        public Peer Accept(uint timeoutMilliseconds = 10000)
        {
            CheckAlive();
            var status = verve_acc_endpoint_accept(m_Handle, timeoutMilliseconds, out var peer);
            if (status == Timeout)
                return null;

            Check(status);
            return new Peer(this, peer);
        }

        /// <summary>连接服务端并校验证书名称。</summary>
        /// <param name="address">服务端地址。</param>
        /// <param name="serverName">证书中的服务端名称。</param>
        public Peer Connect(string address, string serverName)
        {
            CheckAlive();
            using var remote = new Pin(Encoding.UTF8.GetBytes(address));
            using var name = new Pin(Encoding.UTF8.GetBytes(serverName));
            Check(verve_acc_endpoint_connect(m_Handle, remote.Value, name.Value, out var peer));
            return new Peer(this, peer);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;
            Check(verve_acc_endpoint_destroy(m_Handle));
        }

        private void CheckAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AccEndpoint));
        }

        private static void Check(int status)
        {
            if (status == Ok)
                return;

            var length = checked((int)verve_acc_last_error(null, UIntPtr.Zero).ToUInt64());
            var error = new byte[length];
            verve_acc_last_error(error, (UIntPtr)length);
            throw new IOException(length == 0 ? $"ACC status {status}" : Encoding.UTF8.GetString(error));
        }

        /// <summary>端点拥有的连接；释放端点后连接不可继续使用。</summary>
        public sealed class Peer : IDisposable
        {
            private readonly AccEndpoint m_Owner;
            private readonly ulong m_Handle;
            private bool m_Disposed;

            internal Peer(AccEndpoint owner, ulong handle)
            {
                m_Owner = owner;
                m_Handle = handle;
            }

            /// <summary>检查连接是否仍然可用。</summary>
            public void CheckConnection()
            {
                CheckAlive();
                Check(verve_acc_peer_status(m_Owner.m_Handle, m_Handle));
            }

            /// <summary>非阻塞发送；队列满返回 <see langword="false"/>。</summary>
            /// <param name="packet">完整 ACC 数据包。</param>
            public bool TrySend(byte[] packet)
            {
                CheckAlive();
                using var data = new Pin(packet);
                var status = verve_acc_peer_send(m_Owner.m_Handle, m_Handle, data.Value);
                if (status == Full)
                    return false;

                Check(status);
                return true;
            }

            /// <summary>非阻塞接收；空队列返回零，缓冲区不足时保留当前帧。</summary>
            /// <param name="buffer">接收缓冲区。</param>
            /// <returns>接收的字节数。</returns>
            public int Receive(byte[] buffer)
            {
                CheckAlive();
                using var data = new Pin(buffer);
                var status = verve_acc_peer_receive(
                    m_Owner.m_Handle, m_Handle, data.Value.Data, data.Value.Length, out var length);
                if (status == Empty)
                    return 0;
                if (status == BufferTooSmall)
                    throw new ArgumentException($"Receive buffer needs {length.ToUInt64()} bytes.", nameof(buffer));

                Check(status);
                return checked((int)length.ToUInt64());
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (m_Disposed)
                    return;

                m_Disposed = true;
                if (!m_Owner.m_Disposed)
                    Check(verve_acc_peer_destroy(m_Owner.m_Handle, m_Handle));
            }

            private void CheckAlive()
            {
                m_Owner.CheckAlive();
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(Peer));
            }
        }
    }
}
