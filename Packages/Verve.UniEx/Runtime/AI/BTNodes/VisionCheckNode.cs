#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using System.Linq;
    using System.Diagnostics.CodeAnalysis;
    
    
    [Serializable]
    public struct VisionCheckNodeData : INodeData
    {
        [Tooltip("检测源位置"), NotNull] public Transform Owner;
        
        [Tooltip("目标位置（优先级最高）")] public Transform[] Targets;
        [Tooltip("目标检测层级")] public LayerMask TargetLayerMask;
        [Tooltip("目标标签白名单（当目标位置为空时生效）"), TagSelector] public string[] TargetTags;
        [Tooltip("检测到目标结果的键（Transform类型）")] public string ResultTargetKey;

        [Tooltip("视野角度（0-360度）"), Range(0, 360)] public float FieldOfViewAngle;
        [Tooltip("视线起始高度偏移（米）"), Min(0)] public float EyeHeight;
        [Tooltip("最大可视/检测距离（米）"), Min(0)] public float SightDistance;
        [Tooltip("检测间隔（秒）"), Min(0)] public float DetectionInterval;
    }

    
    /// <summary>
    /// 视角检测节点（带视线追踪和视锥体检测，基于物理射线的范围检测）
    /// </summary>
    [Serializable]
    public struct VisionCheckNode : IBTNode, IResetableNode, IPreparableNode, IDebuggableNode
    {
        public string Key;
        public VisionCheckNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        [SerializeField, Tooltip("累计时间")] private float m_ElapsedTime;
        [SerializeField, Tooltip("最后检测到的目标")] private Transform m_LastDetectedTarget;
        [SerializeField, Tooltip("最后是否检测到的目标")] private bool m_LastCheckResult;
        [SerializeField, Tooltip("最后检测到的点")] private Vector3? m_LastHitPoint;
        
        private Collider[] m_ColliderBuffer;
        private readonly float HalfFov => Data.FieldOfViewAngle * 0.5f;
        private readonly float SquaredSightDistance => Data.SightDistance * Data.SightDistance;


        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Data.Owner == null) return NodeStatus.Failure;
            
            m_ElapsedTime += ctx.DeltaTime;
            if (m_ElapsedTime < Data.DetectionInterval) return NodeStatus.Running;
            m_ElapsedTime = 0f;
            
            m_LastCheckResult = CheckPredefinedTargets(out var hitPoint);
            if (!m_LastCheckResult)
            {
                m_LastCheckResult = CheckDynamicTargets(out hitPoint);
            }

            if (m_LastCheckResult && m_LastDetectedTarget != null)
            {
                m_LastHitPoint = hitPoint;
                ctx.BB.SetValue<Transform>(Data.ResultTargetKey, m_LastDetectedTarget);
            }
            
            return m_LastCheckResult ? NodeStatus.Success : NodeStatus.Failure;
        }

        private bool CheckPredefinedTargets(out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            if (Data.Owner == null || Data.Targets == null || Data.Targets.Length <= 0) return false;

            var ownerPos = Data.Owner.position;
            var orderedTargets = Data.Targets
                .Where(t => t != null)
                .OrderBy(t => (t.position - ownerPos).sqrMagnitude);
            
            foreach (var target in orderedTargets)
            {
                Vector3 eyePos = Data.Owner.position + Data.Owner.up * Data.EyeHeight;
                Vector3 targetPos = target.position + Data.Owner.up * Data.EyeHeight;
                Vector3 direction = targetPos - eyePos;

                if (!(direction.sqrMagnitude <= SquaredSightDistance
                     && (Data.FieldOfViewAngle >= 360 || Vector3.Angle(Data.Owner.forward, direction) <= Data.FieldOfViewAngle)))
                {
                    continue;
                }

                m_LastDetectedTarget = target;
                return true;
            }
    
            return false;
        }

        private bool CheckDynamicTargets(out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            if (Data.Owner == null || Data.Targets != null && Data.Targets.Length > 0) return false;
        
            Vector3 eyePosition = Data.Owner.position + Data.Owner.up * Data.EyeHeight;
            m_ColliderBuffer ??= new Collider[32];
            
            int count = Physics.OverlapSphereNonAlloc(
                eyePosition, 
                Data.SightDistance, 
                m_ColliderBuffer, 
                Data.TargetLayerMask
            );
        
            Transform nearestValidTarget = null;
            float minSqrDistance = float.MaxValue;
        
            for (int i = 0; i < count; i++)
            {
                var collider = m_ColliderBuffer[i];
                if (collider == null) continue;
        
                Transform target = collider.transform;
                
                if (Data.TargetTags != null && Data.TargetTags.Length > 0 && !Data.TargetTags.Contains(target.tag))
                    continue;
        
                Vector3 toTarget = target.position - eyePosition;
                float sqrDistance = toTarget.sqrMagnitude;
                
                if (sqrDistance > SquaredSightDistance) continue;
                
                if (Data.FieldOfViewAngle < 360 && Vector3.Angle(Data.Owner.forward, toTarget) > Data.FieldOfViewAngle)
                    continue;
        
                if (Physics.Raycast(eyePosition, toTarget.normalized, 
                    out RaycastHit hit, Data.SightDistance, Data.TargetLayerMask))
                {
                    if (hit.transform == target)
                    {
                        nearestValidTarget = target;
                        minSqrDistance = sqrDistance;
                        hitPoint = hit.point;
                    }
                }
            }
        
            if (nearestValidTarget != null)
            {
                m_LastDetectedTarget = nearestValidTarget;
                return true;
            }
            
            return false;
        }
        
        #region 可重置节点

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_ElapsedTime = 0;
            m_LastHitPoint = null;
            m_LastCheckResult = false;
            m_LastDetectedTarget = null;
            // ctx.BB.RemoveValue(ResultTargetKey);
        }

        #endregion
        
        #region 可准备节点

        void IPreparableNode.Prepare(ref NodeRunContext ctx)
        {
            if (ctx.BB.HasValue(Key))
            {
                Data = ctx.BB.GetValue<VisionCheckNodeData>(Key);
            }
        }
        
        #endregion

        #region 可调试节点
        
        [NotNull] public GameObject DebugTarget { get; set; }
        public string NodeName => nameof(VisionCheckNode);
        public bool IsDebug { get; set; }

        
        void IDebuggableNode.DrawGizmos(ref NodeDebugContext ctx)
        {
            if (!Data.Owner) return;
            
            Gizmos.color = m_LastCheckResult ? Color.green : Color.red;
            
            Vector3 origin = Data.Owner.position + Vector3.up * Data.EyeHeight;
        
            Quaternion leftRay = Quaternion.AngleAxis(-HalfFov, Vector3.up);
            Quaternion rightRay = Quaternion.AngleAxis(HalfFov, Vector3.up);
        
            Vector3 leftDir = leftRay * Data.Owner.forward * Data.SightDistance;
            Vector3 rightDir = rightRay * Data.Owner.forward * Data.SightDistance;
        
            Gizmos.DrawRay(origin, leftDir);
            Gizmos.DrawRay(origin, rightDir);
            Gizmos.DrawRay(origin, (leftDir + rightDir));
            Gizmos.DrawWireSphere(origin, Data.SightDistance);

            if (m_LastDetectedTarget)
            {
                Gizmos.DrawLine(origin, m_LastDetectedTarget.position + Vector3.up * Data.EyeHeight);
            }
            else if (Data.Targets != null)
            {
                foreach (var target in Data.Targets.Where(t => t != null))
                {
                    Gizmos.DrawLine(origin, target.position + Vector3.up * Data.EyeHeight);
                }
            }
            
            if (m_LastHitPoint.HasValue)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(m_LastHitPoint.Value, 0.2f);
                Gizmos.DrawWireSphere(m_LastHitPoint.Value, 0.3f);
            }
        }
        
        #endregion
    }
}

#endif