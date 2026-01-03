namespace Verve.Gameplay
{
    using UnityEngine;
    
    
    /// <summary>
    /// 无物理运动组件
    /// </summary>
    [RequireComponent(typeof(CharacterController)), DisallowMultipleComponent]
    public class KinematicMovement : MonoBehaviour, IMovementComponent
    {
        private CharacterController m_CC;
        private Vector3 m_VerticalVelocity;
        
        private void Awake()
        {
            m_CC = GetComponent<CharacterController>();
        }
        
        private void Update()
        {
            if (m_CC.isGrounded && m_VerticalVelocity.y < 0)
            {
                m_VerticalVelocity.y = -2f;
            }
            m_VerticalVelocity.y += -Physics.gravity.y * Time.deltaTime;
        
            m_CC.Move(m_VerticalVelocity * Time.deltaTime);
        }
        
        public void Move(Vector3 direction, float speed)
        {
            m_CC.Move(direction * speed * Time.deltaTime); 
        }

        public void Jump(float jumpStrength)
        {
            if (m_CC.isGrounded)
            {
                m_VerticalVelocity.y = Mathf.Sqrt(jumpStrength * -2f * -Physics.gravity.y);
            }
        }

        public bool IsGrounded => m_CC.isGrounded;
        public Vector3 Velocity => m_CC.velocity;
    }
}