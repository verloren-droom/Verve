#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    
namespace Verve.Input
{
    using System;
    using UnityEngine;
    using System.Linq;
    using System.Collections;
    using UnityEngine.InputSystem;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    using System.Collections.Concurrent;
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF || PACKAGE_DOCS_GENERATION
    using UnityEngine.InputSystem.XR;
#endif
    
    
    /// <summary>
    ///   <para>新版输入系统（Input System）</para>
    /// </summary>
    [Serializable, GameFeatureSubmodule(typeof(InputGameFeature), Description = "新版输入系统（Input System）")]
    public sealed partial class InputSystemSubmodule : InputSubmodule
    {
        [SerializeField, Tooltip("玩家输入")] private PlayerInput m_PlayerInput;

        public override bool IsValid => m_PlayerInput != null;

        private bool m_IsEnabled = true;

        public override bool IsEnabled
        {
            get => m_IsEnabled;
            set
            {
                if (m_IsEnabled == value) return;
                if (value)
                {
                    m_PlayerInput?.actions?.Enable();
                    m_PlayerInput?.ActivateInput();
                }
                else
                {
                    m_PlayerInput?.DeactivateInput();
                    m_PlayerInput?.actions?.Disable();
                }
                m_IsEnabled = value;
            }
        }

        private struct CallbackInfo
        {
            public InputAction Action;
            public Delegate OriginalCallback;
            public Action<InputAction.CallbackContext> Wrapper;
            public InputServicePhase Phase;
        }

        private readonly ConcurrentDictionary<string, List<CallbackInfo>> m_ActionCallbacks = 
            new ConcurrentDictionary<string, List<CallbackInfo>>();
        

        protected override void OnShutdown()
        {
            if (Application.isPlaying && m_PlayerInput != null && m_PlayerInput.gameObject != null)
            {
                m_PlayerInput?.DeactivateInput();
                m_PlayerInput?.actions?.Disable();
            }
        }

        /// <summary>
        ///   <para>绑定玩家输入</para>
        /// </summary>
        /// <param name="playerInput">玩家输入</param>
        /// <param name="enable">是否启用</param>
        public void BindPlayerInput(PlayerInput playerInput, bool enable = true)
        {
            m_PlayerInput = playerInput;
            if (enable && m_PlayerInput != null)
            {
                m_PlayerInput?.actions?.Enable();
                m_PlayerInput?.ActivateInput();
            }
        }

        protected override void OnAddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed)
        {
            var act = GetInputAction(actionName);
            if (act == null) return;
            if (!act.enabled) act.Enable();

            Action<InputAction.CallbackContext> Wrapper = ctx =>
            {
                if (!IsEnabled) return;
                
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

        protected override void OnRemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed)
        {
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

        protected override void OnRemoveListener(string actionName, InputServicePhase phase = InputServicePhase.Performed)
        {
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

        protected override void OnRemoveAllListener()
        {
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

        protected override void OnSimulateInputAction<T>(string actionName, T value)
        {
            var action = GetInputAction(actionName);
            if (action == null) return;
            
            // TODO: Need to implement InputSystem Input simulation
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
}
    
#endif