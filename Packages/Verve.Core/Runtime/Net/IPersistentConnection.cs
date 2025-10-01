namespace Verve.Net
{
    using System.Threading.Tasks;
    
    
    /// <summary>
    /// 网络长连接接口
    /// </summary>
    public interface IPersistentConnection : INetworkClient
    {
        void Connect(string address, int port);
        Task ConnectAsync(string address, int port);
        void Disconnect();
        Task DisconnectAsync();
        void Send(byte[] data);
        Task SendAsync(byte[] data);
    }
}