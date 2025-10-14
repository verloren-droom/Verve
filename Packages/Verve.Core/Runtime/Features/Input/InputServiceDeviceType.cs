namespace Verve.Input
{
    /// <summary>
    /// 输入设备类型
    /// </summary>
    public enum InputServiceDeviceType
    {
        Unknown,
        /// <summary>
        /// 鼠标
        /// </summary>
        Mouse,
        /// <summary>
        /// 键盘
        /// </summary>
        Keyboard,
        /// <summary>
        /// 手柄
        /// </summary>
        Gamepad,
        /// <summary>
        /// 触摸
        /// </summary>
        Touch,
        XRController
    }
}