namespace Verve.Input
{
    /// <summary>
    /// 输入设备接口
    /// </summary>
    public interface IInputDevice
    {
        /// <summary> 设备是否已连接 </summary>
        bool IsConnected { get; }
    
        /// <summary> 设备是否启用 </summary>
        bool IsEnabled { get; set; }

    }
    
    
    /// <summary>
    /// 输入设备基类
    /// </summary>
    public abstract class InputDeviceBase : IInputDevice
    {
        public bool IsConnected { get; protected set; }
        public bool IsEnabled { get; set; }
    }
}