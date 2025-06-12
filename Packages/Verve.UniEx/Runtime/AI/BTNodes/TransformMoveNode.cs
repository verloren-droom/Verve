#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using System.Linq;
    using UnityEngine;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.CodeAnalysis;
    
    
    [Serializable]
    public struct TransformMoveNodeData : INodeData
    { 
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

        
        [Header("基本设置")]
        [Tooltip("当前对象"), NotNull] public Transform Owner;
        [Tooltip("移动速度（单位/秒）"), Min(0.1f)] public float MoveSpeed;
        [Tooltip("到达目标点模式")] public ArrivalActionMode ArrivalMode;
        [Tooltip("忽略的坐标轴")] public AxisFlags IgnoreAxes;
        
        [Header("目标设置")]
        [Tooltip("目标数组"), NotNull] public Transform[] Targets;
        [Tooltip("最小有效距离"), Range(0.01f, 2f)] public float MinValidDistance;
        [Tooltip("循环模式")] public LoopMode Loop;
    
        [Header("旋转设置")]
        [Tooltip("是否面向移动方向")] public bool FaceMovementDirection;
        [Tooltip("旋转速度（度/秒）"), Min(0f)] public float RotationSpeed;
    }

    
    /// <summary>
    /// 变换移动节点（基于Transform的位移控制）
    /// </summary>
    [Serializable]
    public struct TransformMoveNode : IBTNode, IResetableNode, IPreparableNode, IDebuggableNode
    {
        public string Key;
        public TransformMoveNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        [SerializeField] private int m_CurrentIndex;
        [SerializeField] private bool m_IsMoving;
        [SerializeField] private Vector3 m_CurrentTargetPos;
        [SerializeField] private int m_CurrentDirection;
        
        private float SquaredMinValidDistance => Data.MinValidDistance * Data.MinValidDistance;

        public readonly int CurrentIndex => m_CurrentIndex;
        public readonly bool IsMoving => m_IsMoving;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Data.Targets == null || Data.Targets.Length <= 0 || !IsValidTarget(Data.Owner))
                return NodeStatus.Failure;
            
            if (!m_IsMoving)
            {
                m_CurrentDirection = m_CurrentDirection == 0 ? 1 : m_CurrentDirection;
                if (!GetCurrentTargetPosition(out m_CurrentTargetPos))
                    return NodeStatus.Failure;
                m_IsMoving = true;
            }
            
            Vector3 moveDirection = ApplyAxisFilter((ApplyAxisFilter(m_CurrentTargetPos, Data.Owner.position) - Data.Owner.position).normalized, Vector3.zero);
            
            Data.Owner.position += moveDirection * (Data.MoveSpeed * ctx.DeltaTime);
            
            HandleRotation(moveDirection, ctx.DeltaTime);
        
            if ((Data.Owner.position - m_CurrentTargetPos).sqrMagnitude <= SquaredMinValidDistance)
            {
                return HandleArrival();;
            }
        
            return NodeStatus.Running;
        }
        
        private bool IsValidTarget(Transform t) => 
            t != null && t.gameObject.activeInHierarchy;

        private bool GetCurrentTargetPosition(out Vector3 targetPos)
        {
            targetPos = Vector3.zero;
            if (m_CurrentIndex >= Data.Targets.Length || !IsValidTarget(Data.Targets[m_CurrentIndex])) return false;
            targetPos = Data.Targets[m_CurrentIndex].position;
            targetPos = ApplyAxisFilter(targetPos, Data.Owner.position);
            return true;
        }

        private void HandleRotation(Vector3 moveDirection, float deltaTime)
        {
            if (!Data.FaceMovementDirection || moveDirection == Vector3.zero) return;
    
            var targetRotation = Quaternion.LookRotation(moveDirection);
            Data.Owner.rotation = Quaternion.RotateTowards(
                Data.Owner.rotation,
                targetRotation,
                Data.RotationSpeed * deltaTime
            );
        }
    
        private NodeStatus HandleArrival()
        {
            m_IsMoving = false;

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
        
        private Vector3 ApplyAxisFilter(Vector3 source, Vector3 original)
        {
            Vector3 result = source;
            if ((Data.IgnoreAxes & TransformMoveNodeData.AxisFlags.X) != 0) result.x = original.x;
            if ((Data.IgnoreAxes & TransformMoveNodeData.AxisFlags.Y) != 0) result.y = original.y;
            if ((Data.IgnoreAxes & TransformMoveNodeData.AxisFlags.Z) != 0) result.z = original.z;
            return result;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_IsMoving = false;
        }

        #endregion
        
        #region 可调试节点
        
        [NotNull] public GameObject DebugTarget { get; set; }
        public bool IsDebug { get; set; }
        public string NodeName => nameof(TransformMoveNode);
        
        
        void IDebuggableNode.DrawGizmos(ref NodeDebugContext ctx)
        {
            if (!Data.Owner) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(Data.Owner.position, Data.Targets[m_CurrentIndex].position);
            
            foreach (var target in Data.Targets)
            {
                Gizmos.color  = Color.magenta;
                Gizmos.DrawSphere(target.position, 0.1f);
                Gizmos.DrawWireSphere(target.position, 0.2f);
            }
        }

        #endregion
        
        #region 可准备节点
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IPreparableNode.Prepare(ref NodeRunContext ctx)
        {
            if (ctx.BB.HasValue(Key))
            {
                Data = ctx.BB.GetValue<TransformMoveNodeData>(Key);
            }
        }
        
        #endregion
    }
}

#endif