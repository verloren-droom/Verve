namespace Verve.Gameplay
{
    using UnityEngine;
    using UnityEngine.AI;

    
    /// <summary>
    /// 导航网址运动组件
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent)), DisallowMultipleComponent]
    public class NavMeshMovement : MonoBehaviour, IMovementComponent
    {
        private NavMeshAgent m_Agent;
        
        private void Awake()
        {
            m_Agent = GetComponent<NavMeshAgent>();
        }
        
        public void Move(Vector3 direction, float speed)
        {
            Vector3 targetPosition = m_Agent.gameObject.transform.position + direction * 5f;
            m_Agent.destination = targetPosition;
            m_Agent.speed = speed;
        }

        public void Jump(float jumpStrength) { }

        public bool IsGrounded => !m_Agent.isStopped && m_Agent.isOnNavMesh;
        public Vector3 Velocity => m_Agent.velocity;
    }
}