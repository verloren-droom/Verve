#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using System.Linq;
    using UnityEngine;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.CodeAnalysis;
    
    
    /// <summary>
    /// 变换移动节点数据
    /// </summary>
    [Serializable]
    public struct TransformMoveBTNodeData : INodeData
    {
        [Header("基本设置")]
        [Tooltip("当前对象"), NotNull] public Transform owner;
        [Tooltip("移动速度（单位/秒）"), Min(0.1f)] public float moveSpeed;
        [Tooltip("到达目标点模式")] public ArrivalActionMode arrivalMode;
        [Tooltip("忽略的坐标轴")] public AxisFlags ignoreAxes;
        
        [Header("目标设置")]
        [Tooltip("目标数组"), NotNull] public Transform[] targets;
        [Tooltip("最小有效距离"), Range(0.01f, 2f)] public float minValidDistance;
        [Tooltip("循环模式")] public LoopMode loopMode;
    
        [Header("旋转设置")]
        [Tooltip("是否面向移动方向")] public bool faceMovementDirection;
        [Tooltip("旋转速度（度/秒）"), Min(0f)] public float rotationSpeed;
    }
    
    
    /// <summary>
    /// 到达动作模式（仅目标点为多个生效）
    /// </summary>
    [Serializable]
    public enum ArrivalActionMode : byte
    {
        /// <summary> 自动选择下一目标 </summary>
        Proceed,
        /// <summary> 停止移动并保持状态 </summary>
        Keep,
    }
        
        
    /// <summary>
    /// 循环模式（仅目标点为多个生效）
    /// </summary>
    [Serializable]
    public enum LoopMode : byte
    {
        /// <summary> 无循环 </summary>
        None,
        /// <summary> 从头循环 </summary>
        Repeat,
        /// <summary> 往返循环 </summary>
        PingPong,
    }

        
    [Flags, Serializable]
    public enum AxisFlags : byte
    {
        /// <summary> X 轴 </summary>
        X = 1,
        /// <summary> Y 轴 </summary>
        Y = 2,
        /// <summary> Z 轴 </summary>
        Z = 4,
    }

    
    /// <summary>
    /// 变换移动节点
    /// </summary>
    /// <remarks>
    /// 基于Transform的位移控制，适用于非导航网格场景
    /// </remarks>
    [CustomBTNode(nameof(TransformMoveBTNode)), Serializable]
    public struct TransformMoveBTNode : IBTNode, IBTNodeResettable, IBTNodePreparable, IBTNodeDebuggable
    {
        [Tooltip("黑板数据键")] public string dataKey;
        [Tooltip("节点数据")] public TransformMoveBTNodeData data;
        
        public BTNodeResult LastResult { get; private set; }

        [SerializeField] private int m_CurrentIndex;
        [SerializeField] private bool m_IsMoving;
        [SerializeField] private Vector3 m_CurrentTargetPos;
        [SerializeField] private int m_CurrentDirection;
        
        private readonly float SquaredMinValidDistance => data.minValidDistance * data.minValidDistance;
        public readonly int CurrentIndex => m_CurrentIndex;
        public readonly bool IsMoving => m_IsMoving;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (data.targets == null || data.targets.Length <= 0 || !IsValidTarget(data.owner))
                return BTNodeResult.Failed;
            
            if (!m_IsMoving)
            {
                m_CurrentDirection = m_CurrentDirection == 0 ? 1 : m_CurrentDirection;
                if (!GetCurrentTargetPosition(out m_CurrentTargetPos))
                    return BTNodeResult.Failed;
                m_IsMoving = true;
            }
            
            Vector3 moveDirection = ApplyAxisFilter((ApplyAxisFilter(m_CurrentTargetPos, data.owner.position) - data.owner.position).normalized, Vector3.zero);
            
            data.owner.position += moveDirection * (data.moveSpeed * ctx.deltaTime);
            
            HandleRotation(moveDirection, ctx.deltaTime);
        
            if ((data.owner.position - m_CurrentTargetPos).sqrMagnitude <= SquaredMinValidDistance)
            {
                return HandleArrival();;
            }
        
            return BTNodeResult.Running;
        }
        
        private bool IsValidTarget(Transform t) => 
            t != null && t.gameObject.activeInHierarchy;

        private bool GetCurrentTargetPosition(out Vector3 targetPos)
        {
            targetPos = Vector3.zero;
            if (m_CurrentIndex >= data.targets.Length || !IsValidTarget(data.targets[m_CurrentIndex])) return false;
            targetPos = data.targets[m_CurrentIndex].position;
            targetPos = ApplyAxisFilter(targetPos, data.owner.position);
            return true;
        }

        private void HandleRotation(Vector3 moveDirection, float deltaTime)
        {
            if (!data.faceMovementDirection || moveDirection == Vector3.zero) return;
    
            var targetRotation = Quaternion.LookRotation(moveDirection);
            data.owner.rotation = Quaternion.RotateTowards(
                data.owner.rotation,
                targetRotation,
                data.rotationSpeed * deltaTime
            );
        }
    
        private BTNodeResult HandleArrival()
        {
            m_IsMoving = false;

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
        
        private Vector3 ApplyAxisFilter(Vector3 source, Vector3 original)
        {
            Vector3 result = source;
            if ((data.ignoreAxes & AxisFlags.X) != 0) result.x = original.x;
            if ((data.ignoreAxes & AxisFlags.Y) != 0) result.y = original.y;
            if ((data.ignoreAxes & AxisFlags.Z) != 0) result.z = original.z;
            return result;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_IsMoving = false;
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
                data = ctx.bb.GetValue<TransformMoveBTNodeData>(dataKey);
            }
        }
        
        #endregion
    }
}

#endif