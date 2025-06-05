#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    
    
    /// <summary>
    /// 距离条件节点
    /// </summary>
    [Serializable]
    public struct DistanceConditionNode : IBTNode
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
    }
}

#endif