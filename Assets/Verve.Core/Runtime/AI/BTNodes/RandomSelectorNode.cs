namespace Verve.AI
{
    using System;
    
    
    /// <summary>
    /// 随机选择节点
    /// </summary>
    [Serializable]
    public struct RandomSelectorNode : IBTNode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
        
        private int m_SelectedIndex;
        private bool m_IsInitialized;

        
        NodeStatus IBTNode.Run(ref Blackboard bb, float deltaTime)
        {
            if (!m_IsInitialized || m_SelectedIndex >= Children.Length)
            {
                m_SelectedIndex = new System.Random().Next(0, Children.Length);
                m_IsInitialized = true;
            }
        
            return Children[m_SelectedIndex].Run(ref bb, deltaTime);
        }

        void IResetableNode.Reset()
        {
            m_SelectedIndex = 0;
            m_IsInitialized = false;
            foreach (var child in Children)
            {
                if (child is IResetableNode resetable)
                    resetable.Reset();
            }
        }
    }

}