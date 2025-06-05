#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    
    
    /// <summary>
    /// 视角检测节点
    /// </summary>
    [Serializable]
    public struct VisionCheckNode : IBTNode
    {
        [Tooltip("当前位置")] public Transform Owner;
        [Tooltip("目标位置")] public Transform Target;
        
        [Tooltip("视野角度（0-360度）"), Range(0, 360)] public float FieldOfViewAngle;
        [Tooltip("视线起始高度偏移（米）"), Min(0)] public float EyeHeight;
        [Tooltip("可视距离（米）")] public float SightDistance;
        [Tooltip("障碍物层级掩码")] public LayerMask ObstacleMask;
        
        private float HalfFov => FieldOfViewAngle * 0.5f;
        private float SquaredSightDistance => SightDistance * SightDistance;
        

        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            Vector3 eyePosition = Owner.position + Vector3.up * EyeHeight;
            Vector3 targetPosition = Target.position + Vector3.up * EyeHeight;
            Vector3 directionToTarget = targetPosition - eyePosition;
            
            if (directionToTarget.sqrMagnitude > SquaredSightDistance)
                return NodeStatus.Failure;

            float angle = Vector3.Angle(Owner.forward, directionToTarget.normalized);
            if (angle > HalfFov)
                return NodeStatus.Failure;

            if (Physics.Raycast(eyePosition, directionToTarget.normalized, 
                    out RaycastHit hit, SightDistance, ObstacleMask))
            {
                return hit.transform == Target 
                    ? NodeStatus.Success 
                    : NodeStatus.Failure;
            }

            return NodeStatus.Success;
        }
    }
}

#endif