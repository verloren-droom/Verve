#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Gameplay
{
    using UnityEngine;
    
    
    /// <summary>
    /// 玩家移动
    /// </summary>
    public partial class PlayerMovement : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField, Tooltip("移动速度")] private float m_MoveSpeed = 5f;
        [SerializeField, Tooltip("跳跃力")] private float m_JumpForce = 7f;
        [SerializeField, Tooltip("检查地面距离")] private float m_GroundCheckDistance = 0.1f;
        
        private IMovementComponent m_MovementComponent;
        private CapsuleCollider m_Collider;
        private bool m_IsGrounded;


        private void Awake()
        {
            m_Collider = GetComponent<CapsuleCollider>();
            m_MovementComponent = GetComponent<IMovementComponent>();
        }

        /// <summary>
        /// 移动玩家
        /// </summary>
        public void Move(Vector3 direction) => m_MovementComponent.Move(direction, m_MoveSpeed);

        /// <summary>
        /// 跳跃
        /// </summary>
        public void Jump() => m_MovementComponent.Jump(m_JumpForce);
    }
}

#endif