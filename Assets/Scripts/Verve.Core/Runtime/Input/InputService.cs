using System;
using UnityEngine;

namespace Verve.Input
{
    
    /// <summary>
    /// 输入系统接口
    /// </summary>
    public interface IInputHandle
    {
        /// <summary> 是否有效 </summary>
        bool IsValid { get; }
        /// <summary> 是否启用 </summary>
        bool IsEnabled { get; set; }

        /// <summary> 获取虚拟轴值 </summary>
        float GetAxis(string axisName);
        
        Vector2 GetMousePosition();
        bool GetButtonDown(string buttonName);
        bool GetButtonUp(string buttonName);
        bool GetButton(string buttonName);
    }
    
    
    /// <summary>
    /// 输入系统基类
    /// </summary>
    public abstract class InputServiceBase : IInputHandle
    {
        public bool IsValid { get; }
        public bool IsEnabled { get; set; }

        public abstract float GetAxis(string axisName);
        public abstract Vector2 GetMousePosition();
        public abstract bool GetButtonDown(string buttonName);
        public abstract bool GetButtonUp(string buttonName);
        public abstract bool GetButton(string buttonName);
    }

    /// <summary>
    /// 输入设备类型枚举
    /// </summary>
    public enum InputDeviceType
    {
        KeyboardMouse,
        Gamepad,
        TouchScreen,
        XRController
    }

    /// <summary>
    /// 输入事件数据
    /// </summary>
    public struct InputEvent
    {
        public InputEventType EventType;
        public KeyCode Key;
        public Vector2 Position;
        public float AxisValue;
    }

    /// <summary>
    /// 输入事件类型
    /// </summary>
    public enum InputEventType
    {
        ButtonDown,
        ButtonUp,
        AxisChanged,
        TouchBegan,
        TouchEnded
    }
}