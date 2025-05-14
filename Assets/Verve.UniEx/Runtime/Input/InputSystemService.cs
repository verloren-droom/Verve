namespace VerveUniEx.Input
{
    
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using System;
    using Verve.Input;
    using UnityEngine;
    using System.Linq;
    using UnityEngine.InputSystem;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    using System.Collections.Concurrent;
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF || PACKAGE_DOCS_GENERATION
    using UnityEngine.InputSystem.XR;
#endif
    
    
    /// <summary>
    /// 新版输入系统（Input System）
    /// </summary>
    [Serializable]
    public sealed partial class InputSystemService : InputServiceBase
    {
        public override bool IsValid => m_PlayerInput != null;
        
        private readonly PlayerInput m_PlayerInput;

        private struct CallbackInfo
        {
            public InputAction Action;
            public Delegate OriginalCallback;
            public Action<InputAction.CallbackContext> Wrapper;
            public InputServicePhase Phase;
        }

        private readonly ConcurrentDictionary<string, List<CallbackInfo>> m_ActionCallbacks = 
            new ConcurrentDictionary<string, List<CallbackInfo>>();

        public InputSystemService(PlayerInput input = null)
        {
            m_PlayerInput = input ?? Object.FindObjectOfType<PlayerInput>();
        }

        protected override void OnEnable()
        {
            m_PlayerInput?.actions?.Enable();
            m_PlayerInput?.ActivateInput();
        }

        protected override void OnDisable()
        {
            m_PlayerInput?.DeactivateInput();
            m_PlayerInput?.actions?.Disable();
        }

        public override void AddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed)
        {
            if (!IsValid || string.IsNullOrEmpty(actionName)) return;

            var act = GetInputAction(actionName);
            if (act == null) return;
            if (!act.enabled) act.Enable();

            Action<InputAction.CallbackContext> Wrapper = ctx =>
            {
                if (!enabled) return;
                
                T value = default;
                switch (value)
                {
                    case bool:
                        value = (T)(object)ctx.ReadValueAsButton();
                        break;
                    default:
                        value = ctx.ReadValue<T>();
                        break;
                }

                var context = new InputServiceContext<T>
                {
                    value = value,
                    actionName = actionName,
                    phase = ctx.ToInputServicePhase(), 
                    deviceType = ctx.control.device.ToInputServiceDevice(),
                    binding = new InputServiceBinding()
                    {
                        path = ctx.control.path,
                    }
                };
                if (context.phase == phase) onAction?.Invoke(context);
            };
            
            switch (phase)
            {
                case InputServicePhase.Started:
                    act.started += Wrapper;
                    break;
                case InputServicePhase.Performed:
                    act.performed += Wrapper;
                    break;
                case InputServicePhase.Canceled:
                    act.canceled += Wrapper;
                    break;
                default:
                    act.started += Wrapper;
                    break;
            }
            
            var callbackInfo = new CallbackInfo
            {
                Action = act,
                OriginalCallback = onAction,
                Wrapper = Wrapper,
                Phase = phase
            };

            m_ActionCallbacks.AddOrUpdate(actionName,
                key => new List<CallbackInfo> { callbackInfo },
                (key, list) =>
                {
                    list.Add(callbackInfo);
                    return list;
                });
        }

        public override void RemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed)
        {
            base.RemoveListener(actionName, onAction, phase);
            if (!IsValid || string.IsNullOrEmpty(actionName)) return;

            if (m_ActionCallbacks.TryGetValue(actionName, out var callbacks))
            {
                var targets = callbacks
                    .Where(c => c.Phase == phase && 
                                c.OriginalCallback is Action<InputServiceContext<T>> callback && 
                                callback == onAction)
                    .ToList();

                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    switch (target.Phase)
                    {
                        case InputServicePhase.Started:
                            target.Action.started -= target.Wrapper;
                            break;
                        case InputServicePhase.Performed:
                            target.Action.performed -= target.Wrapper;
                            break;
                        case InputServicePhase.Canceled:
                            target.Action.canceled -= target.Wrapper;
                            break;
                    }
                    callbacks.Remove(target);
                }
                
                if (callbacks.Count == 0)
                {
                    m_ActionCallbacks.TryRemove(actionName, out _);
                }
            }
        }

        public override void RemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Performed)
        {
            base.RemoveListener(actionName, phase);
            if (!IsValid || string.IsNullOrEmpty(actionName)) return;

            if (m_ActionCallbacks.TryGetValue(actionName, out var callbacks))
            {
                var targets = callbacks.Where(c => c.Phase == phase).ToList();

                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    switch (target.Phase)
                    {
                        case InputServicePhase.Started:
                            target.Action.started -= target.Wrapper;
                            break;
                        case InputServicePhase.Performed:
                            target.Action.performed -= target.Wrapper;
                            break;
                        case InputServicePhase.Canceled:
                            target.Action.canceled -= target.Wrapper;
                            break;
                    }
                }

                m_ActionCallbacks.TryRemove(actionName, out _);
            }
        }

        public override void RemoveAllListener()
        {
            base.RemoveAllListener();
            // for (var i = 0; i < m_ActionCallbacks.Count; i++)
            // {
            //     
            // }

            foreach (var actionCallback in m_ActionCallbacks)
            {
                RemoveListener(actionCallback.Key, actionCallback.Value[0].Phase);
            }
        }

        public override void RebindingAction(string actionName, InputServiceRebinding rebind, Action<bool> onCompleted = null)
        {
            var act = GetInputAction(actionName);
            if (act == null) return;
            act.Disable();
            act.ApplyBindingOverride(new InputBinding
            {
                path = rebind.path,
            });
            act.Enable();
            
            // act.PerformInteractiveRebinding(rebind.bindingIndex)
            //     .WithCancelingThrough(rebind.cancelKey)
            //     .OnCancel(operation =>
            //     {
            //         operation?.Dispose();
            //         onCompleted?.Invoke(false);
            //         act.Enable();
            //     })
            //     .OnPotentialMatch(operation =>
            //     {
            //     }).OnComplete(callback =>
            //     {
            //         callback?.Dispose();
            //         onCompleted?.Invoke(true);
            //         act.Enable();
            //     }).Start();
        }
        
        private (string mapName, string actionName) ParseActionName(string fullName)
        {
            var splitIndex = fullName.IndexOf('/');
            if (splitIndex == -1)
            {
                var defaultMap = m_PlayerInput.actions.actionMaps.Count > 0 
                    ? m_PlayerInput.defaultActionMap
                    : "";
                return (defaultMap, fullName);
            }
        
            return (
                fullName[..splitIndex],
                fullName[(splitIndex + 1)..]
            );
        }

        private InputAction GetInputAction(string actionName)
        {
            var (actionMapName, realActionName) = ParseActionName(actionName);
            return m_PlayerInput?.actions?.FindActionMap(actionMapName)?[realActionName];
        }

        public override string SaveBindingsAsJson()
        {
            return m_PlayerInput?.actions?.SaveBindingOverridesAsJson();
        }

        public override void LoadBindingsFromJson(string json)
        {
            m_PlayerInput?.actions?.LoadBindingOverridesFromJson(json);
        }
    }

    public static class InputSystemServiceExtension
    {
        public static InputServicePhase ToInputServicePhase(this InputAction.CallbackContext self)
        {
            switch (self.phase)
            {
                case InputActionPhase.Started: return InputServicePhase.Started;
                case InputActionPhase.Performed: return InputServicePhase.Performed;
                case InputActionPhase.Canceled: return InputServicePhase.Canceled;
                default: return InputServicePhase.Performed;
            }
        }
        
        public static InputServiceDeviceType ToInputServiceDevice(this InputDevice self)
        {
            switch (self.device)
            {
                case Keyboard: return InputServiceDeviceType.Keyboard;
                case Mouse: return InputServiceDeviceType.Mouse;
                case Gamepad: return InputServiceDeviceType.Gamepad;
                case Touchscreen: return InputServiceDeviceType.Touch;
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF || PACKAGE_DOCS_GENERATION
                case XRController: returnInputServiceDevice.XRController;
#endif
                default: return InputServiceDeviceType.Unknown;
            }
        }
    }
#endif
    
}