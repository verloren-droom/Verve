namespace Verve.AI
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct WeightedBTNodeItem
    {
        public double weight;
        public IBTNode child;
    }

    
    /// <summary>
    /// 权重选择节点数据
    /// </summary>
    [Serializable]
    public struct WeightedSelectorBTNodeData : INodeData
    {
        /// <summary> 权重节点 </summary>
        public WeightedBTNodeItem[] children;
    }

    
    /// <summary>
    /// 权重选择节点
    /// </summary>
    /// <remarks>
    /// 遍历所有子节点，并计算每个子节点的权重，然后根据权重进行选择
    /// </remarks>
    [CustomBTNode(nameof(WeightedSelectorBTNode)), Serializable]
    public struct WeightedSelectorBTNode : ICompositeBTNode, IBTNodeResettable, IBTNodePreparable
    {
        /// <summary> 黑板数据键 </summary>
        public string dataKey;
        public WeightedSelectorBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        private int m_SelectedIndex;
        private bool m_IsSelected;
        
        public readonly int SelectedIndex => m_SelectedIndex;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (data.children == null || data.children.Length == 0) 
                return BTNodeResult.Failed;
    
            if (!m_IsSelected)
            {
                double totalWeight = 0;
                for (int i = 0; i < data.children.Length; i++)
                {
                    if (data.children[i].child == null) continue;
                    totalWeight += Math.Max(0, data.children[i].weight);
                }
                
                System.Random random = new System.Random();
    
                if (totalWeight <= 0)
                {
                    var validNodes = data.children.Where(n => n.child != null).ToArray();
                    if (validNodes.Length == 0) return BTNodeResult.Failed;
                    
                    m_SelectedIndex = random.Next(validNodes.Length);
                    m_IsSelected = true;
                    return this.RunChildNode(ref validNodes[m_SelectedIndex].child, ref ctx);
                }
    
                var randomValue = random.NextDouble() * totalWeight;
                double cumulativeWeight = 0f;
                
                for (int i = 0; i < data.children.Length; i++)
                {
                    if (data.children[i].child == null) continue;
                    
                    double currentWeight = Math.Max(0, data.children[i].weight);
                    cumulativeWeight += currentWeight;
                    
                    if (randomValue <= cumulativeWeight)
                    {
                        m_SelectedIndex = i;
                        break;
                    }
                }
                m_IsSelected = true;
            }
    
            if (data.children[m_SelectedIndex].child == null)
            {
                m_IsSelected = false;
                return BTNodeResult.Failed;
            }
    
            LastResult = this.RunChildNode(ref data.children[m_SelectedIndex].child, ref ctx);
            if (LastResult != BTNodeResult.Running)
                m_IsSelected = false;
    
            return LastResult;
        }

        #region 可重置节点

        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_SelectedIndex = -1;
            m_IsSelected = false;
            this.ResetChildrenNode(ref ctx);
        }

        #endregion

        #region 复合节点

        public int ChildCount => data.children?.Length ?? 0;

        
        IEnumerable<IBTNode> ICompositeBTNode.GetChildren() => data.children.Select(x => x.child);
        
        IEnumerable<IBTNode> ICompositeBTNode.GetActiveChildren()
        {
            if (m_SelectedIndex < ChildCount)
                yield return data.children[m_SelectedIndex].child;
        }

        #endregion
        
        #region 可准备节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodePreparable.Prepare(ref BTNodeRunContext ctx)
        {
            if (ctx.bb.HasValue(dataKey))
            {
                data = ctx.bb.GetValue<WeightedSelectorBTNodeData>(dataKey);
            }
        }
        
        #endregion
    }
}