namespace Verve.AI
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    
    /// <summary>
    /// 权重选择节点
    /// </summary>
    [Serializable]
    public struct WeightedSelectorNode : ICompositeNode, IResetableNode
    {
        [Serializable]
        public struct WeightedNode
        {
            public double Weight;
            public IBTNode Node;
        }
    
        
        public WeightedNode[] Nodes;
        
        private int m_SelectedIndex;
        private bool m_IsSelected;
        
        public readonly int SelectedIndex => m_SelectedIndex;


        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Nodes == null || Nodes.Length == 0) 
                return NodeStatus.Failure;
    
            if (!m_IsSelected)
            {
                double totalWeight = 0;
                for (int i = 0; i < Nodes.Length; i++)
                {
                    if (Nodes[i].Node == null) continue;
                    totalWeight += Math.Max(0, Nodes[i].Weight);
                }
                
                System.Random random = new System.Random();
    
                if (totalWeight <= 0)
                {
                    var validNodes = Nodes.Where(n => n.Node != null).ToArray();
                    if (validNodes.Length == 0) return NodeStatus.Failure;
                    
                    m_SelectedIndex = random.Next(validNodes.Length);
                    m_IsSelected = true;
                    return validNodes[m_SelectedIndex].Node.Run(ref ctx);
                }
    
                var randomValue = random.NextDouble() * totalWeight;
                double cumulativeWeight = 0f;
                
                for (int i = 0; i < Nodes.Length; i++)
                {
                    if (Nodes[i].Node == null) continue;
                    
                    double currentWeight = Math.Max(0, Nodes[i].Weight);
                    cumulativeWeight += currentWeight;
                    
                    if (randomValue <= cumulativeWeight)
                    {
                        m_SelectedIndex = i;
                        break;
                    }
                }
                m_IsSelected = true;
            }
    
            if (Nodes[m_SelectedIndex].Node == null)
            {
                m_IsSelected = false;
                return NodeStatus.Failure;
            }
    
            var status = Nodes[m_SelectedIndex].Node.Run(ref ctx);
            if (status != NodeStatus.Running)
                m_IsSelected = false;
    
            return status;
        }
    
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_SelectedIndex = -1;
            m_IsSelected = false;
            foreach (var weightedNode in Nodes)
            {
                if (weightedNode.Node is IResetableNode resetable)
                {
                    resetable.Reset(ref ctx);
                }
            }
        }
        
        public int ChildCount => Nodes?.Length ?? 0;

        IEnumerable<IBTNode> ICompositeNode.GetChildren() => Nodes.Select(x => x.Node);
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren()
        {
            if (m_SelectedIndex < ChildCount)
                yield return Nodes[m_SelectedIndex].Node;
        }
    }
}