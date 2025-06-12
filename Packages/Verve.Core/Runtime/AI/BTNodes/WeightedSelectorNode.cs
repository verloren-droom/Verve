namespace Verve.AI
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct WeightedNode
    {
        public double Weight;
        public IBTNode Node;
    }

    
    [Serializable]
    public struct WeightedSelectorNodeData : INodeData
    {
        /// <summary> 权重节点 </summary>
        public WeightedNode[] Children;
    }

    
    /// <summary>
    /// 权重选择节点
    /// </summary>
    [Serializable]
    public struct WeightedSelectorNode : ICompositeNode, IResetableNode, IPreparableNode
    {
        public string Key;
        public WeightedSelectorNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        private int m_SelectedIndex;
        private bool m_IsSelected;
        
        public readonly int SelectedIndex => m_SelectedIndex;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Data.Children == null || Data.Children.Length == 0) 
                return NodeStatus.Failure;
    
            if (!m_IsSelected)
            {
                double totalWeight = 0;
                for (int i = 0; i < Data.Children.Length; i++)
                {
                    if (Data.Children[i].Node == null) continue;
                    totalWeight += Math.Max(0, Data.Children[i].Weight);
                }
                
                System.Random random = new System.Random();
    
                if (totalWeight <= 0)
                {
                    var validNodes = Data.Children.Where(n => n.Node != null).ToArray();
                    if (validNodes.Length == 0) return NodeStatus.Failure;
                    
                    m_SelectedIndex = random.Next(validNodes.Length);
                    m_IsSelected = true;
                    return this.RunChildNode(ref validNodes[m_SelectedIndex].Node, ref ctx);
                }
    
                var randomValue = random.NextDouble() * totalWeight;
                double cumulativeWeight = 0f;
                
                for (int i = 0; i < Data.Children.Length; i++)
                {
                    if (Data.Children[i].Node == null) continue;
                    
                    double currentWeight = Math.Max(0, Data.Children[i].Weight);
                    cumulativeWeight += currentWeight;
                    
                    if (randomValue <= cumulativeWeight)
                    {
                        m_SelectedIndex = i;
                        break;
                    }
                }
                m_IsSelected = true;
            }
    
            if (Data.Children[m_SelectedIndex].Node == null)
            {
                m_IsSelected = false;
                return NodeStatus.Failure;
            }
    
            LastStatus = this.RunChildNode(ref Data.Children[m_SelectedIndex].Node, ref ctx);
            if (LastStatus != NodeStatus.Running)
                m_IsSelected = false;
    
            return LastStatus;
        }

        #region 可重置节点

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_SelectedIndex = -1;
            m_IsSelected = false;
            foreach (var weightedNode in Data.Children)
            {
                if (weightedNode.Node is IResetableNode resetable)
                {
                    resetable.Reset(ref ctx);
                }
            }
        }

        #endregion

        #region 复合节点

        public int ChildCount => Data.Children?.Length ?? 0;

        
        IEnumerable<IBTNode> ICompositeNode.GetChildren() => Data.Children.Select(x => x.Node);
        
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren()
        {
            if (m_SelectedIndex < ChildCount)
                yield return Data.Children[m_SelectedIndex].Node;
        }

        #endregion
        
        #region 可准备节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IPreparableNode.Prepare(ref NodeRunContext ctx)
        {
            if (ctx.BB.HasValue(Key))
            {
                Data = ctx.BB.GetValue<WeightedSelectorNodeData>(Key);
            }
        }
        
        #endregion
    }
}