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
        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
        }

        public void Enable(Type inputType)
        {
            if (!typeof(IInputService).IsAssignableFrom(inputType))
                throw new InvalidCastException(inputType.Name);
            Resolve(inputType)?.Enable();
        }

        public void Enable<TInputService>() where TInputService : IInputService => Enable(typeof(TInputService));

        public void Disable(Type inputType)
        {
            if (!typeof(IInputService).IsAssignableFrom(inputType))
                throw new InvalidCastException(inputType.Name);
            Resolve(inputType)?.Disable();
        }

        public void Disable<TInputService>() where TInputService : IInputService => Disable(typeof(TInputService));

        public void AddListener<TInputService, TValue>(string actionName, Action<InputServiceContext<TValue>> onAction,
            InputServicePhase phase = InputServicePhase.Performed) where TInputService : class, IInputService where TValue : struct
        {
            Resolve<TInputService>()?.AddListener(actionName, onAction, phase);
        }

        public void RemoveListener<TInputService, TValue>(string actionName, Action<InputServiceContext<TValue>> onAction,
            InputServicePhase phase = InputServicePhase.Performed)  where TInputService : class, IInputService where TValue : struct
        {
            Resolve<TInputService>()?.RemoveListener(actionName, onAction, phase);
        }

        public void RemoveListener<TInputService>(string actionName, InputServicePhase phase = InputServicePhase.Performed) where TInputService : class, IInputService
        {
            Resolve<TInputService>()?.RemoveListener(actionName, phase);
        }

        public void RebindingAction<TInputService>(string actionName, InputServiceRebinding rebind) where TInputService : class, IInputService
        {
            Resolve<TInputService>()?.RebindingAction(actionName, rebind);
        }

        public void LoadBindingsFromJson<TInputService>(string json) where TInputService : class, IInputService
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            Resolve<TInputService>()?.LoadBindingsFromJson(json);
        }

        public string SaveBindingsAsJson<TInputService>() where TInputService : class, IInputService
        {
            return Resolve<TInputService>()?.SaveBindingsAsJson();
        }
    }
    
}