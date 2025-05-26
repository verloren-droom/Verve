#if UNITY_5_3_OR_NEWER && ENABLE_LEGACY_INPUT_MANAGER
    
namespace VerveUniEx.Input
{
    using System;
    using UnityEngine;
    using Verve.Input;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Concurrent;


    /// <summary>
    /// 旧版输入系统（Input Manager） 
    /// </summary>
    [Serializable]
    public sealed partial class InputManagerService : InputServiceBase
    {
        public override bool IsValid => true;

        private Coroutine m_UpdateCoroutine;
        
        [Serializable]
        private struct ActionState
        {
            public bool IsPressed;
            public bool WasPressedThisFrame;
            public bool WasReleasedThisFrame;
            public float LastValue;
            public InputServiceDeviceType LastDeviceType;
        }
        
        private readonly Dictionary<string, ActionState> m_ActionStates = new Dictionary<string, ActionState>();
        private readonly ConcurrentDictionary<string, List<Delegate>> m_ActionCallbacks = new ConcurrentDictionary<string, List<Delegate>>();

        public InputManagerService() { }

        protected override void OnEnable()
        {
            if (m_UpdateCoroutine != null)
            {
                InputManagerRunner.Instance.StopCoroutine(m_UpdateCoroutine);
            }
            m_UpdateCoroutine = InputManagerRunner.Instance.StartCoroutine(Update());
        }

        protected override void OnDisable()
        {
            if (m_UpdateCoroutine != null)
            {
                InputManagerRunner.Instance.StopCoroutine(m_UpdateCoroutine);
                m_UpdateCoroutine = null;
            }
        }

        private IEnumerator Update()
        {
            while (enabled)
            {
                foreach (var actionName in m_ActionCallbacks.Keys)
                {
                    HandleButtonAction(actionName);
                    HandleAxisAction(actionName);
                }
                yield return null;
            }
        }

        protected override void OnAddListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed) where T : struct
        {
            if (!m_ActionCallbacks.ContainsKey(actionName))
            {
                m_ActionCallbacks[actionName] = new List<Delegate>();
            }
            m_ActionCallbacks[actionName].Add(onAction);
        }

        protected override void OnRemoveListener(string actionName, InputServicePhase servicePhase = InputServicePhase.Performed)
        {
            if (m_ActionCallbacks.TryGetValue(actionName, out var delegates))
            {
                delegates.Clear();
                m_ActionCallbacks.TryRemove(actionName, out _);
            }
        }

        protected override void OnRemoveListener<T>(string actionName, Action<InputServiceContext<T>> onAction, InputServicePhase phase = InputServicePhase.Performed) where T : struct
        {
            if (m_ActionCallbacks.TryGetValue(actionName, out var delegates))
            {
                delegates.RemoveAll(d => d.Equals(onAction));
            }
        }

        protected override void OnRemoveAllListener()
        {
            m_ActionCallbacks.Clear();
            m_ActionStates.Clear();
        }
        
        protected override void OnSimulateInputAction<T>(string actionName, T value) where T : struct
        {
            if (m_ActionCallbacks.TryGetValue(actionName, out var delegates))
            {
                var context = new InputServiceContext<T>
                {
                    value = value,
                    actionName = actionName,
                    phase = InputServicePhase.Performed,
                    deviceType = InputServiceDeviceType.Unknown
                };

                foreach (var del in delegates)
                {
                    if (del is Action<InputServiceContext<T>> action)
                    {
                        action(context);
                    }
                }
            }
        }

        private void HandleButtonAction(string actionName)
        {
            bool currentPressed = Input.GetButton(actionName);
            bool currentDown = Input.GetButtonDown(actionName);
            bool currentUp = Input.GetButtonUp(actionName);

            var state = GetOrCreateState<bool>(actionName);

            var phase = InputServicePhase.Performed;
            bool shouldTrigger = false;

            if (currentDown)
            {
                phase = InputServicePhase.Started;
                shouldTrigger = true;
                state.IsPressed = true;
                state.WasPressedThisFrame = true;
            }
            else if (currentUp)
            {
                phase = InputServicePhase.Canceled;
                shouldTrigger = true;
                state.IsPressed = false;
                state.WasReleasedThisFrame = true;
            }
            else if (currentPressed && state.IsPressed)
            {
                phase = InputServicePhase.Performed;
                shouldTrigger = true;
            }

            if (shouldTrigger)
            {
                TriggerCallbacks<bool>(actionName, currentPressed, phase);
            }

            state.WasPressedThisFrame = false;
            state.WasReleasedThisFrame = false;
            m_ActionStates[actionName] = state;
        }

        private void HandleAxisAction(string actionName)
        {
            var currentValue = Input.GetAxis(actionName);
            var state = GetOrCreateState<float>(actionName);
            
            if (Mathf.Abs(currentValue) > 0.0001f)
            {
                TriggerCallbacks<float>(actionName, currentValue, InputServicePhase.Performed);
                state.LastValue = currentValue;
            }
        }
        
        private ActionState GetOrCreateState<T>(string actionName)
        {
            if (!m_ActionStates.TryGetValue(actionName, out var state))
            {
                state = new ActionState();
                m_ActionStates[actionName] = state;
            }
            return state;
        }
        
        private void TriggerCallbacks<T>(string actionName, T value, InputServicePhase phase)where T : struct
        {
            if (m_ActionCallbacks.TryGetValue(actionName, out var delegates))
            {
                var context = new InputServiceContext<T>
                {
                    value = value,
                    actionName = actionName,
                    phase = phase,
                    deviceType = DetectCurrentDevice(),
                };

                for (var i = 0; i < delegates.Count; i++)
                {
                    if (delegates[i] is Action<InputServiceContext<T>> action)
                    {
                        action?.Invoke(context);
                    }
                }
            }
        }
        
        private InputServiceDeviceType DetectCurrentDevice()
        {
            if (Input.GetJoystickNames().Length > 0) return InputServiceDeviceType.Gamepad;
            if (Input.touchCount > 0) return InputServiceDeviceType.Touch;
            if (Input.mousePresent) return InputServiceDeviceType.Mouse;
            return InputServiceDeviceType.Keyboard;
        }
    }

    
    internal class InputManagerRunner : ComponentInstanceBase<InputManagerRunner>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            Instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
    
#endif