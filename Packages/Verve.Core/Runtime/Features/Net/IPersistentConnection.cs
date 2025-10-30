namespace Verve.Net
{
    using System.Threading.Tasks;
    
    
    /// <summary>
    ///   <para>网络长连接接口</para>
    /// </summary>
    public interface IPersistentConnection : INetworkClient
    {
        /// <summary>
        ///   <para>连接</para>
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="port">端口</param>
        void Connect(string address, int port);
        
        /// <summary>
        ///   <para>异步连接</para>
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="port">端口</param>
        Task ConnectAsync(string address, int port);
        
        /// <summary>
        ///   <para>断开连接</para>
        /// </summary>
        void Disconnect();
        
        /// <summary>
        ///   <para>异步断开连接</para>
        /// </summary>
        Task DisconnectAsync();
        
        /// <summary>
        ///   <para>发送数据</para>
        /// </summary>
        /// <param name="data">数据</param>
        void Send(byte[] data);
        
        /// <summary>
        ///   <para>异步发送数据</para>
        /// </summary>
        /// <param name="data">数据</param>
        Task SendAsync(byte[] data);
    }
}