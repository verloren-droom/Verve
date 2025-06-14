#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using UnityEngine.AI;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 导航网格移动节点数据
    /// </summary>
    [Serializable]
    public struct NavMeshMoveBTNodeData : INodeData
    {
        [Header("基本设置")]
        [Tooltip("到达目标点模式")] public ArrivalActionMode arrivalMode;
        
        [Header("导航设置")]
        [Tooltip("导航代理"), NotNull] public NavMeshAgent agent;

        [Header("目标设置")]
        [Tooltip("目标数组")] public Transform[] targets;
        [Tooltip("循环模式")] public LoopMode loopMode;
    
        [Header("旋转设置")]
        [Tooltip("自动旋转")] public bool autoRotation;
        [Tooltip("旋转速度（度/秒）"), Min(0f)] public float rotationSpeed;
    }


    /// <summary>
    /// 导航网格移动节点
    /// </summary>
    /// <remarks>
    /// 基于NavMeshAgent的智能移动，节点会自动处理寻路逻辑，并自动处理移动状态
    /// </remarks>
    [CustomBTNode(nameof(NavMeshMoveBTNode)), Serializable]
    public struct NavMeshMoveBTNode : IBTNode, IBTNodeResettable, IBTNodeDebuggable, IBTNodePreparable
    {
        [Tooltip("黑板数据键")] public string dataKey;
        [Tooltip("节点数据")] public NavMeshMoveBTNodeData data;
        
        public BTNodeResult LastResult { get; private set; }

        [SerializeField] private int m_CurrentIndex;
        [SerializeField] private bool m_IsMoving;
        [SerializeField] private int m_CurrentDirection;
        
        public readonly int CurrentIndex => m_CurrentIndex;
        public readonly bool IsMoving => m_IsMoving;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (data.targets == null || data.targets.Length == 0 || data.agent == null)
                return BTNodeResult.Failed;
    
            if (!m_IsMoving)
            {
                m_CurrentDirection = m_CurrentDirection == 0 ? 1 : m_CurrentDirection;
                if (!SetNextTarget())
                    return BTNodeResult.Failed;
                
                data.agent.isStopped = false;
                m_IsMoving = true;
            }
    
            if (!data.autoRotation && data.agent.velocity.sqrMagnitude > 0.1f)
            {
                HandleRotation(data.agent.velocity.normalized, ctx.deltaTime);
            }
    
            if (!data.agent.pathPending && data.agent.remainingDistance <= data.agent.stoppingDistance)
            {
                return HandleArrival();
            }
    
            return BTNodeResult.Running;
        }
    
        private bool SetNextTarget()
        {
            if (m_CurrentIndex >= data.targets.Length || data.targets[m_CurrentIndex] == null)
                return false;
    
            return data.agent.SetDestination(data.targets[m_CurrentIndex].position);
        }
    
        private void HandleRotation(Vector3 direction, float deltaTime)
        {
            if (direction == Vector3.zero) return;
    
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            data.agent.transform.rotation = Quaternion.RotateTowards(
                data.agent.transform.rotation,
                targetRotation,
                data.rotationSpeed * deltaTime
            );
        }
    
        private BTNodeResult HandleArrival()
        {
            m_IsMoving = false;
            data.agent.isStopped = true;
            
            switch (data.arrivalMode)
            {
                case ArrivalActionMode.Proceed:
                    m_CurrentIndex += m_CurrentDirection;
    
                    if (m_CurrentIndex >= data.targets.Length || m_CurrentIndex < 0)
                    {
                        switch (data.loopMode)
                        {
                            case LoopMode.None:
                                m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, data.targets.Length - 1);
                                return BTNodeResult.Succeeded;
                            case LoopMode.Repeat:
                                m_CurrentIndex = (m_CurrentIndex + data.targets.Length) % data.targets.Length;
                                break;
                            case LoopMode.PingPong:
                                m_CurrentDirection *= -1;
                                m_CurrentIndex += m_CurrentDirection * 2;
                                break;
                        }
                    }
                    return BTNodeResult.Running;
                case ArrivalActionMode.Keep:
                    switch (data.loopMode)
                    {
                        case LoopMode.None:
                            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex + 1, 0, data.targets.Length - 1);
                            break;
                        case LoopMode.Repeat:
                            m_CurrentIndex = (m_CurrentIndex + 1) % data.targets.Length;
                            break;
                        case LoopMode.PingPong:
                            m_CurrentIndex += m_CurrentDirection;

                            bool shouldReverse = m_CurrentIndex > data.targets.Length - 1 || m_CurrentIndex < 0;
    
                            if (shouldReverse)
                            {
                                m_CurrentDirection *= -1;
                                m_CurrentIndex = m_CurrentIndex < 0 
                                    ? 0 
                                    : data.targets.Length - 1;
                                m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, data.targets.Length - 1);
                            }
                            break;
                    }
                    return BTNodeResult.Succeeded;
            }
            return BTNodeResult.Succeeded;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_IsMoving = false;
            if (data.agent != null)
            {
                data.agent.ResetPath();
                data.agent.velocity = Vector3.zero;
            }
        }

        #endregion
        
        #region 可调试节点

        public bool IsDebug { get; set; }

        #endregion
        
        #region 可准备节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodePreparable.Prepare(ref BTNodeRunContext ctx)
        {
            if (ctx.bb.HasValue(dataKey))
            {
                data = ctx.bb.GetValue<NavMeshMoveBTNodeData>(dataKey);
            }
        }
        
        #endregion
    }
}

#endif