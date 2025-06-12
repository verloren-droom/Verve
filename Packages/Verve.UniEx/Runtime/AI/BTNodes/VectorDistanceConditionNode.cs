#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct VectorDistanceConditionNodeData : INodeData
    {
        [Serializable]
        public enum Comparison { LessThanOrEqual, GreaterThan }
        
        
        [Tooltip("当前位置")] public Vector3 OwnerPoint;
        [Tooltip("目标位置")] public Vector3 TargetPoint;
        [Tooltip("检测距离")] public float CheckDistance;
        [Tooltip("比较方式")] public Comparison CompareMode;
    }

    
    /// <summary>
    /// 两点距离条件节点
    /// </summary>
    [Serializable]
    public struct VectorDistanceConditionNode : IBTNode, IPreparableNode, IDebuggableNode
    {
        public string Key;
        public VectorDistanceConditionNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        private float SquaredCheckDistance => Data.CheckDistance * Data.CheckDistance;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            var sqrDistance = (Data.TargetPoint - Data.OwnerPoint).sqrMagnitude;
            
            return Data.CompareMode switch
            {
                VectorDistanceConditionNodeData.Comparison.LessThanOrEqual => sqrDistance <= SquaredCheckDistance
                    ? NodeStatus.Success 
                    : NodeStatus.Failure,
                _ => sqrDistance > SquaredCheckDistance
                    ? NodeStatus.Success 
                    : NodeStatus.Failure
            };
        }
        
        #region 可调试节点
        
        [NotNull] public GameObject DebugTarget { get; set; }
        public bool IsDebug { get; set; }
        public string NodeName => nameof(VectorDistanceConditionNode);
        
        
        void IDebuggableNode.DrawGizmos(ref NodeDebugContext ctx)
        {
            Gizmos.DrawLine(Data.OwnerPoint, Data.TargetPoint);
            Gizmos.DrawWireSphere(Data.TargetPoint, Data.CheckDistance);
        }

        #endregion
        
        #region 可准备节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IPreparableNode.Prepare(ref NodeRunContext ctx)
        {
            if (ctx.BB.HasValue(Key))
            {
                Data = ctx.BB.GetValue<VectorDistanceConditionNodeData>(Key);
            }
        }
        
        #endregion
    }
}

#endif