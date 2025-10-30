#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using System.Linq;
    using System.Diagnostics.CodeAnalysis;
    
    
    /// <summary>
    ///   <para>视角检测节点数据</para>
    /// </summary>
    [Serializable]
    public struct VisionCheckBTNodeData : INodeData
    {
        [Tooltip("检测源位置"), NotNull] public Transform owner;
        
        [Tooltip("目标位置（优先级最高）")] public Transform[] targets;
        [Tooltip("目标检测层级")] public LayerMask targetLayerMask;
        [Tooltip("目标标签白名单（当目标位置为空时生效）"), TagSelector] public string[] targetTags;
        [Tooltip("检测到目标结果的键（Transform类型）")] public string resultTargetKey;

        [Tooltip("视野角度（0-360度）"), Range(0, 360)] public float fieldOfViewAngle;
        [Tooltip("视线起始高度偏移（米）"), Min(0)] public float eyeHeight;
        [Tooltip("最大可视/检测距离（米）"), Min(0)] public float sightDistance;
        [Tooltip("检测间隔（秒）"), Min(0)] public float detectionInterval;
    }

    
    /// <summary>
    ///   <para>视角检测节点</para>
    ///   <para>带视线追踪和视锥体检测，基于物理射线的范围检测</para>
    /// </summary>
    [CustomBTNode(nameof(VisionCheckBTNode)), Serializable]
    public struct VisionCheckBTNode : IBTNode, IBTNodeResettable, IBTNodePreparable, IBTNodeDebuggable
    {
        [Tooltip("黑板数据键")] public string dataKey;
        [Tooltip("节点数据")] public VisionCheckBTNodeData data;
        
        public BTNodeResult LastResult { get; private set; }

        [SerializeField, Tooltip("累计时间")] private float m_ElapsedTime;
        [SerializeField, Tooltip("最后检测到的目标")] private Transform m_LastDetectedTarget;
        [SerializeField, Tooltip("最后是否检测到的目标")] private bool m_LastCheckResult;
        [SerializeField, Tooltip("最后检测到的点")] private Vector3? m_LastHitPoint;
        
        private Collider[] m_ColliderBuffer;
        private readonly float SquaredSightDistance => data.sightDistance * data.sightDistance;
        public readonly float HalfFov => data.fieldOfViewAngle * 0.5f;
        
        public readonly bool LastCheckResult => m_LastCheckResult;
        public readonly Vector3? LastHitPoint => m_LastHitPoint;
        public readonly Transform LastDetectedTarget => m_LastDetectedTarget;


        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (data.owner == null) return BTNodeResult.Failed;
            
            m_ElapsedTime += ctx.deltaTime;
            if (m_ElapsedTime < data.detectionInterval) return BTNodeResult.Running;
            m_ElapsedTime = 0f;
            
            m_LastCheckResult = CheckPredefinedTargets(out var hitPoint);
            if (!m_LastCheckResult)
            {
                m_LastCheckResult = CheckDynamicTargets(out hitPoint);
            }

            if (m_LastCheckResult && m_LastDetectedTarget != null)
            {
                m_LastHitPoint = hitPoint;
                ctx.bb.SetValue<Transform>(data.resultTargetKey, m_LastDetectedTarget);
            }
            
            return m_LastCheckResult ? BTNodeResult.Succeeded : BTNodeResult.Failed;
        }

        private bool CheckPredefinedTargets(out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            if (data.owner == null || data.targets == null || data.targets.Length <= 0) return false;

            var ownerPos = data.owner.position;
            var orderedTargets = data.targets
                .Where(t => t != null)
                .OrderBy(t => (t.position - ownerPos).sqrMagnitude);
            
            foreach (var target in orderedTargets)
            {
                Vector3 eyePos = data.owner.position + data.owner.up * data.eyeHeight;
                Vector3 targetPos = target.position + data.owner.up * data.eyeHeight;
                Vector3 direction = targetPos - eyePos;

                if (!(direction.sqrMagnitude <= SquaredSightDistance
                     && (data.fieldOfViewAngle >= 360 || Vector3.Angle(data.owner.forward, direction) <= data.fieldOfViewAngle)))
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
            if (data.owner == null || data.targets != null && data.targets.Length > 0) return false;
        
            Vector3 eyePosition = data.owner.position + data.owner.up * data.eyeHeight;
            m_ColliderBuffer ??= new Collider[32];
            
            int count = Physics.OverlapSphereNonAlloc(
                eyePosition, 
                data.sightDistance, 
                m_ColliderBuffer, 
                data.targetLayerMask
            );
        
            Transform nearestValidTarget = null;
            float minSqrDistance = float.MaxValue;
        
            for (int i = 0; i < count; i++)
            {
                var collider = m_ColliderBuffer[i];
                if (collider == null) continue;
        
                Transform target = collider.transform;
                
                if (data.targetTags != null && data.targetTags.Length > 0 && !data.targetTags.Contains(target.tag))
                    continue;
        
                Vector3 toTarget = target.position - eyePosition;
                float sqrDistance = toTarget.sqrMagnitude;
                
                if (sqrDistance > SquaredSightDistance) continue;
                
                if (data.fieldOfViewAngle < 360 && Vector3.Angle(data.owner.forward, toTarget) > data.fieldOfViewAngle)
                    continue;
        
                if (Physics.Raycast(eyePosition, toTarget.normalized, 
                    out RaycastHit hit, data.sightDistance, data.targetLayerMask))
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

        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_ElapsedTime = 0;
            m_LastHitPoint = null;
            m_LastCheckResult = false;
            m_LastDetectedTarget = null;
        }

        #endregion

        #region 可调试节点
        
        public bool IsDebug { get; set; }

        #endregion
        
        #region 可准备节点

        void IBTNodePreparable.Prepare(ref BTNodeRunContext ctx)
        {
            if (ctx.bb.HasValue(dataKey))
            {
                data = ctx.bb.GetValue<VisionCheckBTNodeData>(dataKey);
            }
        }
        
        #endregion
    }
}

#endif