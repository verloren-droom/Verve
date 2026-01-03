#if UNITY_5_3_OR_NEWER

namespace Verve.Input
{
    using System;
    
    
    /// <summary>
    ///   <para>输入子模块基类</para>
    /// </summary>
    public abstract class InputSubmodule : TickableGameFeatureSubmodule<InputGameFeatureComponent>, IInput
    {
        public virtual bool IsValid => false;

        public void AddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Started) where T : struct
        {
            if (!IsEnabled || !IsValid || string.IsNullOrEmpty(actionName)) return;
            OnAddListener(actionName, onAction, phase);
        }
        protected abstract void OnAddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed)
            where T : struct;
        
        public void RemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Started)
        {
            if (!IsEnabled || !IsValid || string.IsNullOrEmpty(actionName)) return;
            OnRemoveListener(actionName, phase);
        }
        protected abstract void OnRemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Performed);
        
        public void RemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Started) where T : struct
        {
            if (!IsEnabled || !IsValid || string.IsNullOrEmpty(actionName)) return;
            OnRemoveListener(actionName, phase);
        }
        protected virtual void OnRemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed) where T : struct {}
        
        public void RemoveAllListener()
        {
            if (!IsEnabled || !IsValid) return;
            OnRemoveAllListener();
        }
        protected virtual void OnRemoveAllListener() {}
        
        public virtual void RebindingAction(string actionName, InputServiceRebinding rebind, Action<bool> onCompleted = null) {}
        public virtual void LoadBindingsFromJson(string json) { if (string.IsNullOrEmpty(json)) return; }
        public virtual string SaveBindingsAsJson() => "NULL";
        
        public void SimulateInputAction<T>(string actionName, T value) where T : struct
        {
            if (!IsEnabled || !IsValid || string.IsNullOrEmpty(actionName) || !typeof(T).IsValueType) return;
            OnSimulateInputAction(actionName, value);
        }
        
        protected virtual void OnSimulateInputAction<T>(string actionName, T value) where T : struct {}

        public virtual void Dispose()
        {
            RemoveAllListener();
        }
    }
}

#endif