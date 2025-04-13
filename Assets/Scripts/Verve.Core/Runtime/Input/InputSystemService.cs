namespace Verve.Input
{
    
#if UNITY_2019_4_OR_NEWER && ENABLE_INPUT_SYSTEM
    using System;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) && !UNITY_FORCE_INPUTSYSTEM_XR_OFF || PACKAGE_DOCS_GENERATION
    using UnityEngine.InputSystem.XR;
#endif
    
    
    /// <summary>
    /// 新版输入系统（Input System）
    /// </summary>
    public sealed partial class InputSystemService : InputServiceBase
    {
        private readonly PlayerInput m_PlayerInput;
        private readonly InputActionAsset m_ActionAsset;

        public bool IsValid => m_PlayerInput != null;

        private bool m_IsEnable = false;

        public bool IsEnabled
        {
            get => m_IsEnable;
            set
            {
                if (m_IsEnable == value) return;
                m_IsEnable = value;
                if (m_IsEnable)
                {
                    m_ActionAsset?.Enable();   
                }
                else
                {
                    m_ActionAsset?.Disable();
                }
            }
        }

        public InputSystemService(PlayerInput input)
        {
            m_PlayerInput = input ?? Object.FindObjectOfType<PlayerInput>();
            m_ActionAsset = m_PlayerInput?.actions;
        }

        public override float GetAxis(string axisName)
        {
            return IsEnabled ? (m_ActionAsset?[axisName].ReadValue<float>() ?? 0f) : 0.0f;
        }

        public override Vector2 GetMousePosition()
        {
            return IsEnabled ? Mouse.current?.position.ReadValue() ?? Vector2.zero : Vector2.zero;
        }

        public override bool GetButtonDown(string buttonName)
        {
            throw new NotImplementedException();
        }

        public override bool GetButtonUp(string buttonName)
        {
            throw new NotImplementedException();
        }

        public override bool GetButton(string buttonName)
        {
            throw new NotImplementedException();
        }
    }
#endif
}