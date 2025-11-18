#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace Verve.UniEx.Samples
{
    using MVC;
    using Verve.MVC;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    
    
    /// <summary>
    ///   <para>虚拟摇杆</para>
    /// </summary>
    [AddComponentMenu("Verve/Samples/VirtualJoystick")]
    public class VirtualJoystick : ViewBase, IController
    {
        [SerializeField, Tooltip("虚拟摇杆类型")] private VirtualJoystickType m_JoystickType = VirtualJoystickType.Fixed;
        [SerializeField, Tooltip("最远拖拽距离"), Min(0)] private float m_MaxDistance = 120;
        [SerializeField, Tooltip("可触摸范围")] private Image m_TouchScope;
        [SerializeField, Tooltip("虚拟摇杆背景")] private Image m_BK;
        [SerializeField, Tooltip("虚拟摇杆控制杆")] private Image m_Control;
        
        public override IActivity GetActivity() => VirtualJoystickActivity.Instance;
        
        /// <summary>
        ///   <para>虚拟摇杆类型</para>
        /// </summary>
        [System.Serializable]
        public enum VirtualJoystickType
        {
            /// <summary>
            ///   <para>固定</para>
            /// </summary>
            [Tooltip("固定")] Fixed,
            /// <summary>
            ///   <para>触发</para>
            /// </summary>
            [Tooltip("触发")] Trigger,
            /// <summary>
            ///   <para>跟随</para>
            /// </summary>
            [Tooltip("跟随")] Follow,
        }

        private VirtualJoystickModel m_JoystickModel;

        /// <summary>
        ///   <para>最远拖拽距离</para>
        /// </summary>
        public float MaxDistance { get => m_MaxDistance; set => m_MaxDistance = Mathf.Max(0, value); }

        private Vector2 m_OrigPos = Vector2.zero;

        protected override void OnOpening(params object[] args)
        {
            m_JoystickModel = this.GetModel<VirtualJoystickModel>();
            m_OrigPos = m_BK.transform.localPosition;
            AddEventTrigger(m_TouchScope, EventTriggerType.PointerDown, OnPointerDown);
            AddEventTrigger(m_TouchScope, EventTriggerType.PointerUp, OnPointerUp);
            AddEventTrigger(m_TouchScope, EventTriggerType.Drag, OnDrag);

            m_JoystickModel.JoystickType.Value = m_JoystickType;
            m_JoystickModel.JoystickType.AddListener((_, _) =>
            {
                m_JoystickType = m_JoystickModel.JoystickType.Value;
            });
            
            m_JoystickModel.MaxDirection.Value = m_MaxDistance;
            m_JoystickModel.MaxDirection.AddListener((_, _) =>
            {
                m_MaxDistance = m_JoystickModel.MaxDirection.Value;
            });

            switch (m_JoystickType)
            {
                case VirtualJoystickType.Fixed:
                    m_BK.gameObject.SetActive(true);
                    break;
                case VirtualJoystickType.Trigger:
                    m_BK.gameObject.SetActive(false);
                    break;
                case VirtualJoystickType.Follow:
                    break;
            }
        }
        
        protected override void OnClosing()
        {
            RemoveEventTrigger(m_TouchScope, EventTriggerType.PointerDown, OnPointerDown);
            RemoveEventTrigger(m_TouchScope, EventTriggerType.PointerUp, OnPointerUp);
            RemoveEventTrigger(m_TouchScope, EventTriggerType.Drag, OnDrag);
        }

        private void OnPointerDown(BaseEventData data)
        {
            m_BK.gameObject.SetActive(true);
            
            switch (m_JoystickType)
            {
                case VirtualJoystickType.Fixed:
                    m_BK.transform.localPosition = m_OrigPos;
                    break;
                case VirtualJoystickType.Trigger:
                case VirtualJoystickType.Follow:
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(m_TouchScope.rectTransform, (data as PointerEventData).position, (data as PointerEventData).pressEventCamera, out var localPos);
                    m_BK.transform.localPosition = localPos;
                    break;
            }
        }
        
        private void OnPointerUp(BaseEventData data)
        {
            switch (m_JoystickType)
            {
                case VirtualJoystickType.Fixed:
                    break;
                case VirtualJoystickType.Trigger:
                case VirtualJoystickType.Follow:
                    m_BK.gameObject.SetActive(false);
                    break;
            }
            m_Control.transform.localPosition = Vector3.zero;
            m_JoystickModel.Direction.Value = Vector2.zero;
        }
        
        private void OnDrag(BaseEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(m_BK.rectTransform, (data as PointerEventData).position, (data as PointerEventData).pressEventCamera, out var localPos);
            m_Control.transform.localPosition = localPos;
            if (localPos.magnitude > m_MaxDistance)
            {
                if (m_JoystickType == VirtualJoystickType.Follow)
                {
                    m_BK.transform.localPosition +=
                        (Vector3)(localPos.normalized * (localPos.magnitude - m_MaxDistance));
                }
                m_Control.transform.localPosition = localPos.normalized * m_MaxDistance;
            }

            m_JoystickModel.Direction.Value = m_Control.transform.localPosition.normalized;
        }

        private void Update()
        {
#if UNITY_EDITOR

            if (Input.GetKeyDown(KeyCode.Alpha1))
                m_JoystickType = VirtualJoystickType.Fixed;
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                m_JoystickType = VirtualJoystickType.Trigger;
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                m_JoystickType = VirtualJoystickType.Follow;
#endif
        }

        private void OnGUI()
        {
#if UNITY_EDITOR
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.red;
            style.fontSize = 30;
            style.fontStyle = FontStyle.Bold;
    
            GUILayout.BeginVertical();
            GUILayout.Space(20);
            GUILayout.Label($"Joystick Type (Press 1/2/3): {m_JoystickType}", style);
            GUILayout.Label($"Max Distance: {m_MaxDistance}", style);
            GUILayout.EndVertical();
#endif
        }
    }

    
    /// <summary>
    ///   <para>虚拟摇杆数据</para>
    /// </summary>
    public class VirtualJoystickModel : ModelBase
    {
        /// <summary>
        ///   <para>移动方向向量</para>
        /// </summary>
        public readonly PropertyProxy<Vector2> Direction = new PropertyProxy<Vector2>(Vector2.zero);
        /// <summary>
        ///   <para>虚拟摇杆类型</para>
        /// </summary>
        public readonly PropertyProxy<VirtualJoystick.VirtualJoystickType> JoystickType = new PropertyProxy<VirtualJoystick.VirtualJoystickType>(VirtualJoystick.VirtualJoystickType.Fixed);
        /// <summary>
        ///   <para>最远拖拽距离</para>
        /// </summary>
        public readonly PropertyProxy<float> MaxDirection = new PropertyProxy<float>(100);
    }
}

#endif