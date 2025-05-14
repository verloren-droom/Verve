namespace Verve.Input
{
    
    using Unit;
    using System;
    using System.Collections;
    using System.Threading.Tasks;
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 输入单元
    /// </summary>
    [CustomUnit("Input")]
    public sealed partial class InputUnit : UnitBase 
    {
        private readonly Dictionary<Type, IInputService> m_Inputs = new Dictionary<Type, IInputService>();

        protected override void OnStartup(UnitRules parent, params object[] args)
        {
            base.OnStartup(parent, args);
#if UNITY_5_3_OR_NEWER && ENABLE_LEGACY_INPUT_MANAGER
            m_Inputs.Add(typeof(InputManagerService), new InputManagerService());
#endif
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
            m_Inputs.Add(typeof(InputSystemService), new InputSystemService(args.Length > 0 ? args[0] as PlayerInput : null));
#endif
        }

        public void Enable(Type inputType)
        {
            if (!typeof(IInputService).IsAssignableFrom(inputType))
                throw new InvalidCastException(inputType.Name);
            if (m_Inputs.TryGetValue(inputType, out var input))
            {
                input.Enable();
            }
        }

        public void Enable<TInputService>() where TInputService : IInputService => Enable(typeof(TInputService));

        public void Disable(Type inputType)
        {
            if (!typeof(IInputService).IsAssignableFrom(inputType))
                throw new InvalidCastException(inputType.Name);
            if (m_Inputs.TryGetValue(inputType, out var input))
            {
                input.Disable();
            }
        }

        public void Disable<TInputService>() where TInputService : IInputService => Disable(typeof(TInputService));

        public void AddListener<TInputService, TValue>(string actionName, Action<InputServiceContext<TValue>> onAction,
            InputServicePhase phase = InputServicePhase.Performed) where TInputService : IInputService where TValue : struct
        {
            if (m_Inputs.TryGetValue(typeof(TInputService), out var input))
            {
                input.AddListener(actionName, onAction, phase);
            }
        }

        public void RemoveListener<TInputService, TValue>(string actionName, Action<InputServiceContext<TValue>> onAction,
            InputServicePhase phase = InputServicePhase.Performed)  where TInputService : IInputService where TValue : struct
        {
            if (m_Inputs.TryGetValue(typeof(TInputService), out var input))
            {
                input.RemoveListener(actionName, onAction, phase);
            }
        }

        public void RemoveListener<TInputService>(string actionName, InputServicePhase phase = InputServicePhase.Performed) where TInputService : IInputService
        {
            if (m_Inputs.TryGetValue(typeof(TInputService), out var input))
            {
                input.RemoveListener(actionName, phase);
            }
        }

        public void RebindingAction<TInputService>(string actionName, InputServiceRebinding rebind) where TInputService : IInputService
        {
            if (m_Inputs.TryGetValue(typeof(TInputService), out var input))
            {
                input.RebindingAction(actionName, rebind);
            }
        }

        public void LoadBindingsFromJson<TInputService>(string json) where TInputService : IInputService
        {
            if (m_Inputs.TryGetValue(typeof(TInputService), out var input) && !string.IsNullOrEmpty(json))
            {
                input.LoadBindingsFromJson(json);
            }
        }

        public string SaveBindingsAsJson<TInputService>() where TInputService : IInputService
        {
            if (m_Inputs.TryGetValue(typeof(TInputService), out var input))
            {
                return input.SaveBindingsAsJson();
            }

            throw new ArgumentNullException($"{typeof(TInputService).Name} is not found!");
        }
    }
    
}