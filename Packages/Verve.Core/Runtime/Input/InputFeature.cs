namespace Verve.Input
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 输入功能
    /// </summary>
    [System.Serializable]
    public class InputFeature : ModularGameFeature
    {
        public void Enable(Type inputType)
        {
            if (!typeof(IInputSubmodule).IsAssignableFrom(inputType))
                throw new InvalidCastException(inputType.Name);
            ((IInputSubmodule)GetSubmodule(inputType))?.Enable();
        }
        
        public void Enable<TInputService>() where TInputService : IInputSubmodule => Enable(typeof(TInputService));
        
        public void Disable(Type inputType)
        {
            if (!typeof(IInputSubmodule).IsAssignableFrom(inputType))
                throw new InvalidCastException(inputType.Name);
            ((IInputSubmodule)GetSubmodule(inputType))?.Disable();

        }
        
        public void Disable<TInputService>() where TInputService : IInputSubmodule => Disable(typeof(TInputService));
        
        public void AddListener<TInputService, TValue>(string actionName, Action<InputServiceContext<TValue>> onAction,
            InputServicePhase phase = InputServicePhase.Performed) where TInputService : class, IInputSubmodule where TValue : struct
        {
            GetSubmodule<TInputService>()?.AddListener(actionName, onAction, phase);
        }
        
        public void RemoveListener<TInputService, TValue>(string actionName, Action<InputServiceContext<TValue>> onAction,
            InputServicePhase phase = InputServicePhase.Performed)  where TInputService : class, IInputSubmodule where TValue : struct
        {
            GetSubmodule<TInputService>()?.RemoveListener(actionName, onAction, phase);
        }
        
        public void RemoveListener<TInputService>(string actionName, InputServicePhase phase = InputServicePhase.Performed) where TInputService : class, IInputSubmodule
        {
            GetSubmodule<TInputService>()?.RemoveListener(actionName, phase);
        }
        
        public void RebindingAction<TInputService>(string actionName, InputServiceRebinding rebind) where TInputService : class, IInputSubmodule
        {
            GetSubmodule<TInputService>()?.RebindingAction(actionName, rebind);
        }
        
        public void LoadBindingsFromJson<TInputService>(string json) where TInputService : class, IInputSubmodule
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }
            GetSubmodule<TInputService>()?.LoadBindingsFromJson(json);
        }
        
        public string SaveBindingsAsJson<TInputService>() where TInputService : class, IInputSubmodule
        {
            return GetSubmodule<TInputService>()?.SaveBindingsAsJson();
        }
        
        public void SimulateInputAction<TInputService, TValue>(string actionName, TValue value) where TInputService : class, IInputSubmodule where TValue : struct
        {
            GetSubmodule<TInputService>()?.SimulateInputAction(actionName, value);
        }
    }
}