namespace Verve.Net
{
    /// <summary>
    /// 网络客户端接口
    /// </summary>
    public interface INetworkClient
    {
        /// <summary> 网络状态 </summary>
        NetworkState State { get; }
        /// <summary> 连接超时时间（毫秒） </summary>
        int Timeout { get; set; }
    }
}
