namespace Verve.Input
{
    using System;
    
    
    /// <summary>
    /// 输入系统基类
    /// </summary>
    public abstract class InputSubmodule : IInputSubmodule
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

        public void AddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Started) where T : struct
        {
            if (!enabled || !IsValid || string.IsNullOrEmpty(actionName)) return;
            OnAddListener(actionName, onAction, phase);
        }
        protected abstract void OnAddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed)
            where T : struct;
        
        public void RemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Started)
        {
            if (!enabled || !IsValid || string.IsNullOrEmpty(actionName)) return;
            OnRemoveListener(actionName, phase);
        }
        protected abstract void OnRemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Performed);
        
        public void RemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Started) where T : struct
        {
            if (!enabled || !IsValid || string.IsNullOrEmpty(actionName)) return;
            OnRemoveListener(actionName, phase);
        }
        protected virtual void OnRemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed) where T : struct {}
        
        public void RemoveAllListener()
        {
            if (!enabled || !IsValid) return;
            OnRemoveAllListener();
        }
        protected virtual void OnRemoveAllListener() {}
        
        public virtual void RebindingAction(string actionName, InputServiceRebinding rebind, Action<bool> onCompleted = null) {}
        public virtual void LoadBindingsFromJson(string json) { if (string.IsNullOrEmpty(json)) return; }
        public virtual string SaveBindingsAsJson() => "NULL";
        
        public void SimulateInputAction<T>(string actionName, T value) where T : struct
        {
            if (!enabled || !IsValid || string.IsNullOrEmpty(actionName) || !typeof(T).IsValueType) return;
            OnSimulateInputAction(actionName, value);
        }
        
        protected virtual void OnSimulateInputAction<T>(string actionName, T value) where T : struct {}

        public virtual void Dispose()
        {
            RemoveAllListener();
            Disable();
        }

        public abstract string ModuleName { get; }
        public virtual void OnModuleLoaded(IReadOnlyFeatureDependencies dependencies) { }

        public virtual void OnModuleUnloaded() { }
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