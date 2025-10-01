namespace Verve.Net
{
    /// <summary>
    /// 网络状态
    /// </summary>
    public enum NetworkState
    {
        /// <summary> 未连接 </summary>
        Disconnected,
        /// <summary> 连接中 </summary>
        Connecting,
        /// <summary> 已连接 </summary>
        Connected,
        /// <summary> 断开中 </summary>
        Disconnecting,
        /// <summary> 重连中 </summary>
        Reconnecting
    }
}