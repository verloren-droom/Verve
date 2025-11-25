namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>游戏平台事件接口</para>
    /// </summary>
    public interface IGamePlatformEvents
    {
        /// <summary>
        ///   <para>应用焦点改变事件</para>
        /// </summary>
        event Action<bool> OnApplicationFocus;

        /// <summary>
        ///   <para>应用退出事件</para>
        /// </summary>
        event Action OnApplicationQuit;
        
        /// <summary>
        ///   <para>低内存警告事件</para>
        /// </summary>
        event Action OnLowMemory;
    }
}