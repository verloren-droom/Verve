#if UNITY_5_3_OR_NEWER

namespace Verve.Gameplay
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    /// 生命组件 - 用于描述一个对象的生命值
    /// </summary>
    public partial class Health : MonoBehaviour
    {
        [SerializeField, Tooltip("最大健康值")] private float m_MaxHealth = 0;
        [SerializeField, Tooltip("最小健康值")] private float m_MinHealth = 0;
        [SerializeField, Tooltip("当前健康值")] private float m_CurrentHealth = 0;
        [SerializeField, Tooltip("是否死亡"), ReadOnly] private bool m_IsDead = false;
        
        public event Action OnDead;
        public event Action<float> OnHealthChanged;
        
        /// <summary> 是否无敌 </summary>
        public bool Invincible { get; set; }
        /// <summary> 是否死亡 </summary>
        public bool IsDead => m_IsDead;

        public float MaxHealth
        {
            get => m_MaxHealth;
            set => m_MaxHealth = Mathf.Max(value, m_MinHealth);
        }
        
        public float MinHealth
        {
            get => m_MinHealth;
            set => m_MinHealth = Mathf.Min(value, m_MaxHealth);
        }
        
        public float CurrentHealth
        {
            get => m_CurrentHealth;
            set
            {
                if (m_CurrentHealth == value || Invincible) return;
                
                m_CurrentHealth = Mathf.Clamp(value, m_MinHealth, m_MaxHealth);
                OnHealthChanged?.Invoke(m_CurrentHealth);
                if (m_CurrentHealth <= m_MinHealth)
                {
                    m_IsDead = true;
                    OnDead?.Invoke();
                }
            }
        }

        private void Awake()
        {
            m_CurrentHealth = m_MaxHealth;
        }
    }
}

#endif