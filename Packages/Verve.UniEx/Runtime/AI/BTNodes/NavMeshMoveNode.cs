#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using UnityEngine.AI;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct NavMeshMoveNodeData : INodeData
    {
        [Header("基本设置")]
        [Tooltip("到达目标点模式")] public TransformMoveNodeData.ArrivalActionMode ArrivalMode;
        
        [Header("导航设置")]
        [Tooltip("导航代理")] public NavMeshAgent Agent;

        [Header("目标设置")]
        [Tooltip("目标数组")] public Transform[] Targets;
        [Tooltip("循环模式")] public TransformMoveNodeData.LoopMode Loop;
    
        [Header("旋转设置")]
        [Tooltip("自动旋转")] public bool AutoRotation;
        [Tooltip("旋转速度（度/秒）"), Min(0f)] public float RotationSpeed;
    }


    /// <summary>
    /// 导航网格移动节点（基于NavMeshAgent的智能移动）
    /// </summary>
    [Serializable]
    public struct NavMeshMoveNode : IBTNode, IResetableNode, IDebuggableNode, IPreparableNode
    {
        public string Key;
        public NavMeshMoveNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        [SerializeField] private int m_CurrentIndex;
        [SerializeField] private bool m_IsMoving;
        [SerializeField] private int m_CurrentDirection;
        
        public readonly int CurrentIndex => m_CurrentIndex;
        public readonly bool IsMoving => m_IsMoving;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Data.Targets == null || Data.Targets.Length == 0 || Data.Agent == null)
                return NodeStatus.Failure;
    
            if (!m_IsMoving)
            {
                m_CurrentDirection = m_CurrentDirection == 0 ? 1 : m_CurrentDirection;
                if (!SetNextTarget())
                    return NodeStatus.Failure;
                
                Data.Agent.isStopped = false;
                m_IsMoving = true;
            }
    
            if (!Data.AutoRotation && Data.Agent.velocity.sqrMagnitude > 0.1f)
            {
                HandleRotation(Data.Agent.velocity.normalized, ctx.DeltaTime);
            }
    
            if (!Data.Agent.pathPending && Data.Agent.remainingDistance <= Data.Agent.stoppingDistance)
            {
                return HandleArrival();
            }
    
            return NodeStatus.Running;
        }
    
        private bool SetNextTarget()
        {
            if (m_CurrentIndex >= Data.Targets.Length || Data.Targets[m_CurrentIndex] == null)
                return false;
    
            return Data.Agent.SetDestination(Data.Targets[m_CurrentIndex].position);
        }
    
        private void HandleRotation(Vector3 direction, float deltaTime)
        {
            if (direction == Vector3.zero) return;
    
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Data.Agent.transform.rotation = Quaternion.RotateTowards(
                Data.Agent.transform.rotation,
                targetRotation,
                Data.RotationSpeed * deltaTime
            );
        }
    
        private NodeStatus HandleArrival()
        {
            m_IsMoving = false;
            Data.Agent.isStopped = true;
            
            switch (Data.ArrivalMode)
            {
                case TransformMoveNodeData.ArrivalActionMode.Proceed:
                    m_CurrentIndex += m_CurrentDirection;
    
                    if (m_CurrentIndex >= Data.Targets.Length || m_CurrentIndex < 0)
                    {
                        switch (Data.Loop)
                        {
                            case TransformMoveNodeData.LoopMode.None:
                                m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, Data.Targets.Length - 1);
                                return NodeStatus.Success;
                            case TransformMoveNodeData.LoopMode.Repeat:
                                m_CurrentIndex = (m_CurrentIndex + Data.Targets.Length) % Data.Targets.Length;
                                break;
                            case TransformMoveNodeData.LoopMode.PingPong:
                                m_CurrentDirection *= -1;
                                m_CurrentIndex += m_CurrentDirection * 2;
                                break;
                        }
                    }
                    return NodeStatus.Running;
                case TransformMoveNodeData.ArrivalActionMode.Keep:
                    switch (Data.Loop)
                    {
                        case TransformMoveNodeData.LoopMode.None:
                            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex + 1, 0, Data.Targets.Length - 1);
                            break;
                        case TransformMoveNodeData.LoopMode.Repeat:
                            m_CurrentIndex = (m_CurrentIndex + 1) % Data.Targets.Length;
                            break;
                        case TransformMoveNodeData.LoopMode.PingPong:
                            m_CurrentIndex += m_CurrentDirection;

                            bool shouldReverse = m_CurrentIndex > Data.Targets.Length - 1 || m_CurrentIndex < 0;
    
                            if (shouldReverse)
                            {
                                m_CurrentDirection *= -1;
                                m_CurrentIndex = m_CurrentIndex < 0 
                                    ? 0 
                                    : Data.Targets.Length - 1;
                                m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, Data.Targets.Length - 1);
                            }
                            break;
                    }
                    return NodeStatus.Success;
            }
            return NodeStatus.Success;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_IsMoving = false;
            if (Data.Agent != null)
            {
                Data.Agent.ResetPath();
                Data.Agent.velocity = Vector3.zero;
            }
        }

        #endregion
        
        #region 可调试节点

        [NotNull] public GameObject DebugTarget { get; set; }
        public bool IsDebug { get; set; }
        public string NodeName => nameof(NavMeshMoveNode);

        
        void IDebuggableNode.DrawGizmos(ref NodeDebugContext ctx)
        {
            
        }

        #endregion
        
        #region 可准备节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IPreparableNode.Prepare(ref NodeRunContext ctx)
        {
            if (ctx.BB.HasValue(Key))
            {
                Data = ctx.BB.GetValue<NavMeshMoveNodeData>(Key);
            }
        }
        
        #endregion
    }
}

#endif