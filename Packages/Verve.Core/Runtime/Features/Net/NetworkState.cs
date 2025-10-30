namespace Verve.Net
{
    /// <summary>
    ///   <para>网络状态</para>
    /// </summary>
    public enum NetworkState
    {
        /// <summary>
        ///   <para>未连接</para>
        /// </summary>
        Disconnected,
        
        /// <summary>
        ///   <para>连接中</para>
        /// </summary>
        Connecting,
        
        /// <summary>
        ///   <para>已连接</para>
        /// </summary>
        Connected,
        
        /// <summary>
        ///   <para>断开中</para>
        /// </summary>
        Disconnecting,
        
        /// <summary>
        ///   <para>重连中</para>
        /// </summary>
        Reconnecting
    }
}