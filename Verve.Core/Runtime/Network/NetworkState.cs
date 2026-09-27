namespace Verve
{
    /// <summary>
    ///   <para>网络连接状态。</para>
    /// </summary>
    public enum NetworkState : byte
    {
        /// <summary>
        ///   <para>未连接。</para>
        /// </summary>
        Disconnected,
        /// <summary>
        ///   <para>连接中。</para>
        /// </summary>
        Connecting,
        /// <summary>
        ///   <para>已连接。</para>
        /// </summary>
        Connected,
        /// <summary>
        ///   <para>关闭中。</para>
        /// </summary>
        Closing,
    }
}
