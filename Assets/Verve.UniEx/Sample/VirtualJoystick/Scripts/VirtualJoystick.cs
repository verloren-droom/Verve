#if VERVE_UNIEX_0_0_1_OR_NEWER

namespace VerveUniEx.Sample
{
    using Verve.MVC;
    using Verve.Event;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using ViewBase = VerveUniEx.MVC.ViewBase;
    
    
    /// <summary>
    /// 虚拟摇杆
    /// </summary>
    public class VirtualJoystick : ViewBase, IController
    {
        [SerializeField, Tooltip("虚拟摇杆类型")] private VirtualJoystickType m_JoystickType = VirtualJoystickType.Fixed;
        [SerializeField, Tooltip("最远拖拽距离"), Min(0)] private float m_MaxDistance = 120;
        [SerializeField, Tooltip("可触摸范围")] private Image m_TouchScope;
        [SerializeField, Tooltip("虚拟摇杆背景")] private Image m_BK;
        [SerializeField, Tooltip("虚拟摇杆控制杆")] private Image m_Control;
        
        
        [System.Serializable]
        public enum VirtualJoystickType
        {
            /// <summary>
            /// 固定
            /// </summary>
            Fixed,
            /// <summary>
            /// 触发
            /// </summary>
            Trigger,
            /// <summary>
            /// 跟随
            /// </summary>
            Follow,
        }

        private VirtualJoystickModel m_JoystickModel;

        /// <summary>
        /// 最远距离
        /// </summary>
        public float MaxDistance { get => m_MaxDistance; set => m_MaxDistance = Mathf.Max(0, value); }

        private Vector2 m_OrigPos = Vector2.zero;

        
        private void Awake()
        {
            m_JoystickModel = this.GetModel<VirtualJoystickModel>();
            m_OrigPos = m_BK.transform.localPosition;
        }

        private void Start()
        {
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
    
        public override IActivity Activity { get; set; } = VirtualJoystickActivity.Instance;
    }

    
    public class VirtualJoystickModel : ModelBase
    {
        /// <summary>
        /// 移动方向向量
        /// </summary>
        public readonly PropertyProxy<Vector2> Direction = new PropertyProxy<Vector2>(Vector2.zero);
        /// <summary>
        /// 虚拟摇杆类型
        /// </summary>
        public readonly PropertyProxy<VirtualJoystick.VirtualJoystickType> JoystickType = new PropertyProxy<VirtualJoystick.VirtualJoystickType>(VirtualJoystick.VirtualJoystickType.Fixed);
        /// <summary>
        /// 最远拖拽距离
        /// </summary>
        public readonly PropertyProxy<float> MaxDirection = new PropertyProxy<float>(100);
    }
    
    
    public class VirtualJoystickActivity : ActivityBase<VirtualJoystickActivity>
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();

            RegisterModel<VirtualJoystickModel>();
        }
    }
}

#endif