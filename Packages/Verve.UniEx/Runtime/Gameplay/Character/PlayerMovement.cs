#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.Gameplay
{
    using UnityEngine;
    
    
    public partial class PlayerMovement : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float m_MoveSpeed = 5f;
        [SerializeField] private float m_JumpForce = 7f;
        [SerializeField] private float m_GroundCheckDistance = 0.1f;
        
        [Header("物理设置")]
        [SerializeField] private float m_GroundDrag = 5f;
        [SerializeField] private float m_AirDrag = 0.5f;
        
        private Rigidbody m_Rigidbody;
        private CapsuleCollider m_Collider;
        private bool m_IsGrounded;
        private float m_OriginalDrag;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Collider = GetComponent<CapsuleCollider>();
            m_OriginalDrag = m_Rigidbody.drag;
        }

        private void FixedUpdate()
        {
            CheckGround();
            ApplyDrag();
            ApplyGravity();
        }

        /// <summary>
        /// 移动玩家
        /// </summary>
        public void Move(Vector3 direction)
        {
            if (m_IsGrounded)
            {
                Vector3 moveForce = direction * m_MoveSpeed;
                m_Rigidbody.AddForce(moveForce, ForceMode.Acceleration);
            }
            else
            {
                Vector3 moveForce = direction * (m_MoveSpeed * 0.5f);
                m_Rigidbody.AddForce(moveForce, ForceMode.Acceleration);
            }
        }

        /// <summary>
        /// 跳跃
        /// </summary>
        public void Jump()
        {
            if (m_IsGrounded)
            {
                m_Rigidbody.AddForce(Vector3.up * m_JumpForce, ForceMode.Impulse);
                m_IsGrounded = false;
            }
        }

        /// <summary>
        /// 检查是否在地面
        /// </summary>
        private void CheckGround()
        {
            Vector3 spherePosition = transform.position + m_Collider.center;
            spherePosition.y -= m_Collider.height / 2 - m_Collider.radius;
            
            m_IsGrounded = Physics.SphereCast(
                spherePosition,
                m_Collider.radius * 0.9f,
                Vector3.down,
                out RaycastHit hit,
                m_GroundCheckDistance,
                ~LayerMask.GetMask("Player")
            );
        }

        /// <summary>
        /// 应用阻力
        /// </summary>
        private void ApplyDrag()
        {
            m_Rigidbody.drag = m_IsGrounded ? m_GroundDrag : m_AirDrag;
        }

        /// <summary>
        /// 应用额外重力
        /// </summary>
        private void ApplyGravity()
        {
            if (!m_IsGrounded)
            {
                m_Rigidbody.AddForce(Physics.gravity * 1.5f, ForceMode.Acceleration);
            }
        }
    }
}

#endif