namespace Verve.Input
{
    /// <summary>
    ///   <para>输入设备类型</para>
    /// </summary>
    public enum InputServiceDeviceType
    {
        /// <summary>
        ///   <para>未知设备</para>
        /// </summary>
        Unknown,
        
        /// <summary>
        ///   <para>鼠标</para>
        /// </summary>
        Mouse,
        
        /// <summary>
        ///   <para>键盘</para>
        /// </summary>
        Keyboard,
        
        /// <summary>
        ///   <para>手柄</para>
        /// </summary>
        Gamepad,
        
        /// <summary>
        ///   <para>触摸</para>
        /// </summary>
        Touch,
        
        /// <summary>
        ///   <para>VR控制器</para>
        /// </summary>
        XRController
    }
}