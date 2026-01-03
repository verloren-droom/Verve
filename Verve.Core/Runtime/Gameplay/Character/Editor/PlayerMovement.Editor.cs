#if UNITY_EDITOR

namespace Verve.Gameplay
{
    using UnityEngine;
    
    
    [RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider)), DisallowMultipleComponent]
    public partial class PlayerMovement
    {
        private void OnDrawGizmosSelected()
        {
            if (m_Collider == null) return;
            
            Vector3 spherePosition = transform.position + m_Collider.center;
            spherePosition.y -= m_Collider.height / 2 - m_Collider.radius;
            
            Gizmos.color = m_IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(spherePosition, m_Collider.radius * 0.9f);
            Gizmos.DrawLine(spherePosition, spherePosition + Vector3.down * m_GroundCheckDistance);
        }
    }
}

#endif