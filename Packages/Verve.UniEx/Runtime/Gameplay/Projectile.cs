#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Gameplay
{
    using System;
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    
    /// <summary>
    /// 发射物
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public partial class Projectile : MonoBehaviour
    {
        [SerializeField, Tooltip("发射速度"), Min(0.0f)] private float m_Speed = 100.0f;
        [SerializeField, Tooltip("伤害值"), Min(0)] private float m_Damage = 1.0f;
        [SerializeField, Tooltip("碰撞检测半径"), Min(0.01f)] private float m_Radius = 0.01f;
        [SerializeField, Tooltip("碰撞层")] private LayerMask m_CollisionMask = ~0;
        [SerializeField, Tooltip("留存时间(秒,0=永久)"), Min(0)] private float m_Lifetime = 0.0f;
        [SerializeField, Tooltip("忽略发射者")] private bool m_IgnoreOwner = true;

        
        /// <summary> 命中事件 </summary>
        public event Action<RaycastHit> OnHit;
        /// <summary> 发射事件 </summary>
        public event Action OnFire;
        /// <summary> 销毁事件  </summary>
        public event Action OnDestroyed;
        

        private bool m_IsActive;
        private Rigidbody m_Rigidbody;
        private GameObject m_Owner;
        private Vector3 m_LastPosition;
        private RaycastHit[] m_Hits = new RaycastHit[10];

        
        protected virtual void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        public virtual void ResetState()
        {
            m_IsActive = false;
            m_Owner = null;
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }

        protected virtual void FixedUpdate()
        {
            if (!m_IsActive) return;
            
            if (m_LastPosition != transform.position)
            {
                Vector3 moveDirection = transform.position - m_LastPosition;
                float moveDistance = moveDirection.magnitude;
                
                if (moveDistance > 0.01f && 
                    Physics.SphereCastNonAlloc(
                        m_LastPosition, 
                        m_Radius, 
                        moveDirection.normalized, 
                        m_Hits,
                        moveDistance, 
                        m_CollisionMask, 
                        QueryTriggerInteraction.Ignore) > 0)
                {
                    for (var i = 0; i < m_Hits.Length; i++)
                    {
                        if (m_Hits[i].collider == null) continue;
                
                        GameObject hitObject = m_Hits[i].collider.gameObject;
                        if (hitObject == gameObject || (m_IgnoreOwner && hitObject == m_Owner)) continue;
                
                        Hit(m_Hits[i]);
                    }
                }
            }
            m_LastPosition = transform.position;
        }

        protected virtual void Hit(RaycastHit ray)
        {
            if (ray.transform.TryGetComponent<Health>(out var health))
            {
                health.CurrentHealth -= m_Damage;
            }
            
            OnHit?.Invoke(ray);
            m_IsActive = false;
        }

        public virtual IEnumerator Fire(Vector3 direction, GameObject owner = null)
        {
            ResetState();
            m_Owner = owner;
            m_IsActive = true;
            m_LastPosition = transform.position;
            
            m_Rigidbody.velocity = direction.normalized * m_Speed;
            OnFire?.Invoke();
            if (m_Lifetime > 0)
            {
                yield return new WaitForSeconds(m_Lifetime);
                m_IsActive = false;
                OnDestroyed?.Invoke();
            }
        }
    }
}

#endif