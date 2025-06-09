using System.Collections.Generic;

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
    /// 变换移动节点（基于Transform的位移控制）
    /// </summary>
    [Serializable]
    public struct TransformMoveNode : IBTNode, IResetableNode, IDebuggableNode
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
        [Tooltip("当前对象")] public Transform Owner;
        [Tooltip("移动速度（单位/秒）"), Min(0.1f)] public float MoveSpeed;
        [Tooltip("到达目标点模式")] public ArrivalActionMode ArrivalMode;
        [Tooltip("忽略的坐标轴")] public AxisFlags IgnoreAxes;
        
        [Header("目标设置")]
        [Tooltip("目标数组")] public Transform[] Targets;
        [Tooltip("最小有效距离"), Range(0.01f, 2f)] public float MinValidDistance;
        [Tooltip("循环模式")] public LoopMode Loop;
    
        [Header("旋转设置")]
        [Tooltip("是否面向移动方向")] public bool FaceMovementDirection;
        [Tooltip("旋转速度（度/秒）"), Min(0f)] public float RotationSpeed;

        private int m_CurrentIndex;
        private bool m_IsMoving;
        private Vector3 m_CurrentTargetPos;
        private int m_CurrentDirection;

        public readonly int CurrentIndex => m_CurrentIndex;
        public readonly bool IsMoving => m_IsMoving;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Targets == null || Targets.Length <= 0 || !IsValidTarget(Owner))
                return NodeStatus.Failure;

            if (!m_IsMoving)
            {
                m_CurrentDirection = m_CurrentDirection == 0 ? 1 : m_CurrentDirection;
                if (!GetCurrentTargetPosition(out m_CurrentTargetPos))
                    return NodeStatus.Failure;
                m_IsMoving = true;
            }
            
            Vector3 moveDirection = ApplyAxisFilter((ApplyAxisFilter(m_CurrentTargetPos, Owner.position) - Owner.position).normalized, Vector3.zero);
            
            Owner.position += moveDirection * (MoveSpeed * ctx.DeltaTime);
            
            HandleRotation(moveDirection, ctx.DeltaTime);
        
            if ((Owner.position - m_CurrentTargetPos).sqrMagnitude <= MinValidDistance * MinValidDistance)
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
            if (m_CurrentIndex >= Targets.Length || !IsValidTarget(Targets[m_CurrentIndex])) return false;
            targetPos = Targets[m_CurrentIndex].position;
            targetPos = ApplyAxisFilter(targetPos, Owner.position);
            return true;
        }

        private void HandleRotation(Vector3 moveDirection, float deltaTime)
        {
            if (!FaceMovementDirection || moveDirection == Vector3.zero) return;
    
            var targetRotation = Quaternion.LookRotation(moveDirection);
            Owner.rotation = Quaternion.RotateTowards(
                Owner.rotation,
                targetRotation,
                RotationSpeed * deltaTime
            );
        }
    
        private NodeStatus HandleArrival()
        {
            m_IsMoving = false;

            switch (ArrivalMode)
            {
                case ArrivalActionMode.Proceed:
                    m_CurrentIndex += m_CurrentDirection;

                    if (m_CurrentIndex >= Targets.Length || m_CurrentIndex < 0)
                    {
                        switch (Loop)
                        {
                            case LoopMode.None:
                                m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, Targets.Length - 1);
                                return NodeStatus.Success;
                            case LoopMode.Repeat:
                                m_CurrentIndex = (m_CurrentIndex + Targets.Length) % Targets.Length;
                                break;
                            case LoopMode.PingPong:
                                m_CurrentDirection *= -1;
                                m_CurrentIndex += m_CurrentDirection * 2;
                                break;
                        }
                    }
                    return NodeStatus.Running;
                case ArrivalActionMode.Keep:
                    switch (Loop)
                    {
                        case LoopMode.None:
                            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex + 1, 0, Targets.Length - 1);
                            break;
                        case LoopMode.Repeat:
                            m_CurrentIndex = (m_CurrentIndex + 1) % Targets.Length;
                            break;
                        case LoopMode.PingPong:
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
        
        private Vector3 ApplyAxisFilter(Vector3 source, Vector3 original)
        {
            Vector3 result = source;
            if ((IgnoreAxes & AxisFlags.X) != 0) result.x = original.x;
            if ((IgnoreAxes & AxisFlags.Y) != 0) result.y = original.y;
            if ((IgnoreAxes & AxisFlags.Z) != 0) result.z = original.z;
            return result;
        }

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_IsMoving = false;
        }

        
        #region 调试部分
        
        [NotNull] public GameObject DebugTarget { get; set; }
        public bool IsDebug { get; set; }
        public string NodeName => nameof(TransformMoveNode);

        
        void IDebuggableNode.DrawGizmos(ref NodeDebugContext ctx)
        {
            
        }

        #endregion
    }
}

#endif