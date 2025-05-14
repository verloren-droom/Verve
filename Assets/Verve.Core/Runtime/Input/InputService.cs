namespace Verve.Input
{
    
    using System;
    
    
    /// <summary>
    /// 输入系统基类
    /// </summary>
    public abstract class InputServiceBase : IInputService
    {
        public virtual bool IsValid => false;

        public bool enabled { get; private set; }

        public void Enable()
        {
            enabled = true;
            OnEnable();
        }
        
        public void Disable()
        {
            enabled = false;
            OnDisable();
        }
        
        protected virtual void OnEnable() {}
        protected virtual void OnDisable() {}


        public abstract void AddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed)
            where T : struct;
        public virtual void RemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Performed) {}
        public virtual void RemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed) where T : struct {}
        public virtual void  RemoveAllListener() {}
        public virtual void RebindingAction(string actionName, InputServiceRebinding rebind, Action<bool> onCompleted = null) {}
        public virtual void LoadBindingsFromJson(string json) { if (string.IsNullOrEmpty(json)) return; }
        public virtual string SaveBindingsAsJson() => "NULL";

        public virtual void Dispose()
        {
            RemoveAllListener();
            Disable();
        }
    }

    
    /// <summary>
    /// 输入阶段
    /// </summary>
    public enum InputServicePhase
    {
        Started,
        Performed,
        Canceled
    }

    
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

    
    public struct InputServiceContext<T> where T : struct
    {
        public T value;
        public string actionName;
        public InputServicePhase phase;
        public InputServiceDeviceType deviceType;
        public InputServiceBinding binding;
    }

    
    public struct InputServiceBinding
    {
        public string path;
    }

    
    public struct InputServiceRebinding
    {
        public string path;
        public int bindingIndex;
        public string cancelKey;
        public string filter;
    }
    
}