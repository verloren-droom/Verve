#if UNITY_5_3_OR_NEWER

namespace VerveUniEx.AI
{
    using System;
    using Verve.AI;
    using UnityEngine;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 两点距离条件节点数据
    /// </summary>
    [Serializable]
    public struct VectorDistanceConditionBTNodeData : INodeData
    {
        [Serializable]
        public enum Comparison { LessThanOrEqual, GreaterThan }
        
        
        [Tooltip("当前位置")] public Vector3 ownerPoint;
        [Tooltip("目标位置")] public Vector3 targetPoint;
        [Tooltip("检测距离")] public float checkDistance;
        [Tooltip("比较方式")] public Comparison compareMode;
    }

    
    /// <summary>
    /// 两点距离条件节点
    /// </summary>
    /// <remarks>
    /// 检测两个点之间的距离，并返回结果
    /// </remarks>
    [CustomBTNode(nameof(VectorDistanceConditionBTNode)), Serializable]
    public struct VectorDistanceConditionBTNode : IBTNode, IBTNodePreparable, IBTNodeDebuggable
    {
        [Tooltip("黑板数据键")] public string dataKey;
        [Tooltip("节点数据")] public VectorDistanceConditionBTNodeData data;
        
        public BTNodeResult LastResult { get; private set; }

        private readonly float SquaredCheckDistance => data.checkDistance * data.checkDistance;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            var sqrDistance = (data.targetPoint - data.ownerPoint).sqrMagnitude;
            
            return data.compareMode switch
            {
                VectorDistanceConditionBTNodeData.Comparison.LessThanOrEqual => sqrDistance <= SquaredCheckDistance
                    ? BTNodeResult.Succeeded 
                    : BTNodeResult.Failed,
                _ => sqrDistance > SquaredCheckDistance
                    ? BTNodeResult.Succeeded 
                    : BTNodeResult.Failed
            };
        }
        
        #region 可调试节点
        
        public bool IsDebug { get; set; }

        #endregion
        
        #region 可准备节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodePreparable.Prepare(ref BTNodeRunContext ctx)
        {
            if (ctx.bb.HasValue(dataKey))
            {
                data = ctx.bb.GetValue<VectorDistanceConditionBTNodeData>(dataKey);
            }
        }
        
        #endregion
    }
}

#endif