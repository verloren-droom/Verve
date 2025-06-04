namespace Verve.Input
{
    using Unit;
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 输入单元
    /// </summary>
    [CustomUnit("Input")]
    public partial class InputUnit : UnitBase<IInputService>
    {
        public void Enable(Type inputType)
        {
            GetService(inputType)?.Enable();
        }

        public void Enable<TInputService>() where TInputService : IInputService => Enable(typeof(TInputService));

        public void Disable(Type inputType)
        {
            if (!typeof(IInputService).IsAssignableFrom(inputType))
                throw new InvalidCastException(inputType.Name);
            GetService(inputType)?.Disable();
        }

        public void Disable<TInputService>() where TInputService : IInputService => Disable(typeof(TInputService));

        public void AddListener<TInputService, TValue>(string actionName, Action<InputServiceContext<TValue>> onAction,
            InputServicePhase phase = InputServicePhase.Performed) where TInputService : class, IInputService where TValue : struct
        {
            GetService<TInputService>()?.AddListener(actionName, onAction, phase);
        }

        public void RemoveListener<TInputService, TValue>(string actionName, Action<InputServiceContext<TValue>> onAction,
            InputServicePhase phase = InputServicePhase.Performed)  where TInputService : class, IInputService where TValue : struct
        {
            GetService<TInputService>()?.RemoveListener(actionName, onAction, phase);
        }

        public void RemoveListener<TInputService>(string actionName, InputServicePhase phase = InputServicePhase.Performed) where TInputService : class, IInputService
        {
            GetService<TInputService>()?.RemoveListener(actionName, phase);
        }

        public void RebindingAction<TInputService>(string actionName, InputServiceRebinding rebind) where TInputService : class, IInputService
        {
            GetService<TInputService>()?.RebindingAction(actionName, rebind);
        }

        public void LoadBindingsFromJson<TInputService>(string json) where TInputService : class, IInputService
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            GetService<TInputService>()?.LoadBindingsFromJson(json);
        }

        public string SaveBindingsAsJson<TInputService>() where TInputService : class, IInputService
        {
            return GetService<TInputService>()?.SaveBindingsAsJson();
        }
        
        public void SimulateInputAction<TInputService, TValue>(string actionName, TValue value) where TInputService : class, IInputService where TValue : struct
        {
            GetService<TInputService>()?.SimulateInputAction(actionName, value);
        }
    }
}