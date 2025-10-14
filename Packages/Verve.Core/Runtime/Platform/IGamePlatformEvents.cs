namespace Verve
{
    using System;
    
    
    /// <summary>
    /// 游戏平台事件接口
    /// </summary>
    public interface IGamePlatformEvents
    {
        /// <summary> 应用焦点改变事件 </summary>
        event Action<bool> OnApplicationFocus;

        /// <summary> 应用退出事件 </summary>
        event Action OnApplicationQuit;
        
        /// <summary> 低内存警告事件 </summary>
        event Action OnLowMemory;
    }
}