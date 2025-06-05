#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using UnityEngine.AI;


    /// <summary>
    /// 导航网格移动节点（基于NavMeshAgent的智能移动）
    /// </summary>
    [Serializable]
    public struct NavMeshMoveNode : IBTNode, IResetableNode
    {
        [Header("基本设置")]
        [Tooltip("到达目标点模式")] public TransformMoveNode.ArrivalActionMode ArrivalMode;
        
        [Header("导航设置")]
        [Tooltip("导航代理")] public NavMeshAgent Agent;

        [Header("目标设置")]
        [Tooltip("目标数组")] public Transform[] Targets;
        [Tooltip("循环模式")] public TransformMoveNode.LoopMode Loop;
    
        [Header("旋转设置")]
        [Tooltip("自动旋转")] public bool AutoRotation;
        [Tooltip("旋转速度（度/秒）"), Min(0f)] public float RotationSpeed;
    
        private int m_CurrentIndex;
        private bool m_IsMoving;
        private int m_CurrentDirection;
        
        public int CurrentIndex => m_CurrentIndex;
        public bool IsMoving => m_IsMoving;
        

        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Targets == null || Targets.Length == 0 || Agent == null)
                return NodeStatus.Failure;
    
            if (!m_IsMoving)
            {
                m_CurrentDirection = m_CurrentDirection == 0 ? 1 : m_CurrentDirection;
                if (!SetNextTarget())
                    return NodeStatus.Failure;
                
                Agent.isStopped = false;
                m_IsMoving = true;
            }
    
            if (!AutoRotation && Agent.velocity.sqrMagnitude > 0.1f)
            {
                HandleRotation(Agent.velocity.normalized, ctx.DeltaTime);
            }
    
            if (!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
            {
                return HandleArrival();
            }
    
            return NodeStatus.Running;
        }
    
        private bool SetNextTarget()
        {
            if (m_CurrentIndex >= Targets.Length || Targets[m_CurrentIndex] == null)
                return false;
    
            return Agent.SetDestination(Targets[m_CurrentIndex].position);
        }
    
        private void HandleRotation(Vector3 direction, float deltaTime)
        {
            if (direction == Vector3.zero) return;
    
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Agent.transform.rotation = Quaternion.RotateTowards(
                Agent.transform.rotation,
                targetRotation,
                RotationSpeed * deltaTime
            );
        }
    
        private NodeStatus HandleArrival()
        {
            m_IsMoving = false;
            Agent.isStopped = true;
            
            switch (ArrivalMode)
            {
                case TransformMoveNode.ArrivalActionMode.Proceed:
                    m_CurrentIndex += m_CurrentDirection;
    
                    if (m_CurrentIndex >= Targets.Length || m_CurrentIndex < 0)
                    {
                        switch (Loop)
                        {
                            case TransformMoveNode.LoopMode.None:
                                m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, Targets.Length - 1);
                                return NodeStatus.Success;
                            case TransformMoveNode.LoopMode.Repeat:
                                m_CurrentIndex = (m_CurrentIndex + Targets.Length) % Targets.Length;
                                break;
                            case TransformMoveNode.LoopMode.PingPong:
                                m_CurrentDirection *= -1;
                                m_CurrentIndex += m_CurrentDirection * 2;
                                break;
                        }
                    }
                    return NodeStatus.Running;
                case TransformMoveNode.ArrivalActionMode.Keep:
                    switch (Loop)
                    {
                        case TransformMoveNode.LoopMode.None:
                            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex + 1, 0, Targets.Length - 1);
                            break;
                        case TransformMoveNode.LoopMode.Repeat:
                            m_CurrentIndex = (m_CurrentIndex + 1) % Targets.Length;
                            break;
                        case TransformMoveNode.LoopMode.PingPong:
                            m_CurrentIndex += m_CurrentDirection;

                            bool shouldReverse = m_CurrentIndex > Targets.Length - 1 || m_CurrentIndex < 0;
    
                            if (shouldReverse)
                            {
                                m_CurrentDirection *= -1;
                                m_CurrentIndex = m_CurrentIndex < 0 
                                    ? 0 
                                    : Targets.Length - 1;
                                m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, Targets.Length - 1);
                            }
                            break;
                    }
                    return NodeStatus.Success;
            }
            return NodeStatus.Success;
        }
    
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_IsMoving = false;
            if (Agent != null)
            {
                Agent.ResetPath();
                Agent.velocity = Vector3.zero;
            }
        }
    }
}

#endif