#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using System.Diagnostics.CodeAnalysis;

    
    /// <summary>
    /// 两点距离条件节点
    /// </summary>
    [Serializable]
    public struct VectorDistanceConditionNode : IBTNode, IDebuggableNode
    {
        [Serializable]
        public enum Comparison { LessThanOrEqual, GreaterThan }
        
        
        [Tooltip("当前位置")] public Vector3 OwnerPoint;
        [Tooltip("目标位置")] public Vector3 TargetPoint;
        [Tooltip("检测距离")] public float CheckDistance;
        [Tooltip("比较方式")] public Comparison CompareMode;
        
        private float SquaredCheckDistance => CheckDistance * CheckDistance;

        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            var sqrDistance = (TargetPoint - OwnerPoint).sqrMagnitude;
            
            return CompareMode switch
            {
                Comparison.LessThanOrEqual => sqrDistance <= SquaredCheckDistance
                    ? NodeStatus.Success 
                    : NodeStatus.Failure,
                _ => sqrDistance > SquaredCheckDistance
                    ? NodeStatus.Success 
                    : NodeStatus.Failure
            };
        }

        
        #region 调试部分
        
        [NotNull] public GameObject DebugTarget { get; set; }
        public bool IsDebug { get; set; }
        public string NodeName => nameof(VectorDistanceConditionNode);
        
        
        void IDebuggableNode.DrawGizmos(ref NodeDebugContext ctx)
        {
            Gizmos.DrawLine(OwnerPoint, TargetPoint);
            Gizmos.DrawWireSphere(TargetPoint, CheckDistance);
        }

        #endregion
    }
}

#endif