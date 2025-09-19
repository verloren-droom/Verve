namespace Verve.Application
{
    /// <summary>
    /// 应用程序接口
    /// </summary>
    public interface IApplication
    {
        /// <summary> 应用版本号 </summary>
        string Version { get; }
        
        /// <summary> 设备唯一标识 </summary>
        string DeviceId { get; }
        
        /// <summary> 退出应用程序 </summary>
        void QuitApplication();
        
        /// <summary> 重启应用程序 </summary>
        void RestartApplication();
    }
}