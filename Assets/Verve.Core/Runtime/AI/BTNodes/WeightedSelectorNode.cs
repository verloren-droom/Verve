namespace Verve.AI
{
    using System;
    using System.Linq;
    
    
    /// <summary>
    /// 权重选择节点
    /// </summary>
    [Serializable]
    public struct WeightedSelectorNode : IBTNode, IResetableNode
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
    

        NodeStatus IBTNode.Run(ref Blackboard bb, float deltaTime)
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
                    return validNodes[m_SelectedIndex].Node.Run(ref bb, deltaTime);
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
    
            var status = Nodes[m_SelectedIndex].Node.Run(ref bb, deltaTime);
            if (status != NodeStatus.Running)
                m_IsSelected = false;
    
            return status;
        }
    
        void IResetableNode.Reset()
        {
            m_SelectedIndex = -1;
            m_IsSelected = false;
            foreach (var weightedNode in Nodes)
            {
                if (weightedNode.Node is IResetableNode resetable)
                {
                    resetable.Reset();
                }
            }
        }
    }

}