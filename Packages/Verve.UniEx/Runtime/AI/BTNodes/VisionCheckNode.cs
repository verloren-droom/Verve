#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using System.Diagnostics.CodeAnalysis;

    
    /// <summary>
    /// 视角检测节点（基于物理射线的范围检测）
    /// </summary>
    [Serializable]
    public struct VisionCheckNode : IBTNode, IDebuggableNode
    {
        [Tooltip("当前位置")] public Transform Owner;
        [Tooltip("目标位置")] public Transform Target;
        
        [Tooltip("视野角度（0-360度）"), Range(0, 360)] public float FieldOfViewAngle;
        [Tooltip("视线起始高度偏移（米）"), Min(0)] public float EyeHeight;
        [Tooltip("可视距离（米）"), Min(0)] public float SightDistance;
        [Tooltip("障碍物层级掩码")] public LayerMask ObstacleMask;
        
        [SerializeField, Tooltip("最后触摸的点")]
        private Vector3 m_LastHitPoint;
        
        private float HalfFov => FieldOfViewAngle * 0.5f;
        private float SquaredSightDistance => SightDistance * SightDistance;
        
        public bool LastCheckResult { get; private set; }
        public Vector3 LastHitPoint => m_LastHitPoint;


        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            LastCheckResult = CheckVisibility(out m_LastHitPoint);
            return LastCheckResult ? NodeStatus.Success : NodeStatus.Failure;
        }
        
        private bool CheckVisibility(out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            if (!Owner || !Target) return false;

            Vector3 eyePosition = Owner.position + Vector3.up * EyeHeight;
            Vector3 targetPosition = Target.position + Vector3.up * EyeHeight;
            Vector3 direction = targetPosition - eyePosition;

            // 距离检查
            if (direction.sqrMagnitude > SquaredSightDistance) return false;

            // 角度检查
            float angle = Vector3.Angle(Owner.forward, direction.normalized);
            if (angle > HalfFov) return false;

            // 障碍物检查
            if (Physics.Raycast(eyePosition, direction, out var hit, SightDistance, ObstacleMask))
            {
                hitPoint = hit.point;
                return hit.transform == Target;
            }

            hitPoint = targetPosition;
            return true;
        }

        
        #region 调试部分
        
        [NotNull] public GameObject DebugTarget { get; set; }
        public string NodeName => nameof(VisionCheckNode);
        public bool IsDebug { get; set; }

        
        void IDebuggableNode.DrawGizmos(ref NodeDebugContext ctx)
        {
            if (!Owner || !Target) return;
            
            Vector3 origin = Owner.position + Vector3.up * EyeHeight;
            float halfFov = FieldOfViewAngle * 0.5f;
        
            Quaternion leftRay = Quaternion.AngleAxis(-halfFov, Vector3.up);
            Quaternion rightRay = Quaternion.AngleAxis(halfFov, Vector3.up);
        
            Vector3 leftDir = leftRay * Owner.forward * SightDistance;
            Vector3 rightDir = rightRay * Owner.forward * SightDistance;
        
            Gizmos.DrawRay(origin, leftDir);
            Gizmos.DrawRay(origin, rightDir);
            Gizmos.DrawLine(origin + leftDir, origin + rightDir);
            
            Gizmos.color = LastCheckResult ? Color.green : Color.red;
            Gizmos.DrawLine(Owner.position + Vector3.up * EyeHeight, 
                LastCheckResult ? Target.position : m_LastHitPoint);
        }
        
        #endregion
    }
}

#endif