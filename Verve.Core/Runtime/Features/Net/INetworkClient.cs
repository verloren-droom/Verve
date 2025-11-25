namespace Verve.Net
{
    /// <summary>
    ///   <para>网络客户端接口</para>
    /// </summary>
    public interface INetworkClient
    {
        /// <summary>
        ///   <para>网络状态</para>
        /// </summary>
        NetworkState State { get; }
        
        /// <summary>
        ///   <para>连接超时时间（毫秒）</para>
        /// </summary>
        int Timeout { get; set; }
    }
}
