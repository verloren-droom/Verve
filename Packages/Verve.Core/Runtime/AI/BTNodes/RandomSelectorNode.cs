namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    
    
    /// <summary>
    /// 随机选择节点
    /// </summary>
    [Serializable]
    public struct RandomSelectorNode : ICompositeNode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
        
        private int m_SelectedIndex;
        private bool m_IsInitialized;

        public int SelectedIndex => m_SelectedIndex;

        
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (!m_IsInitialized || m_SelectedIndex >= Children.Length)
            {
                m_SelectedIndex = new System.Random().Next(0, Children.Length);
                m_IsInitialized = true;
            }
        
            return Children[m_SelectedIndex].Run(ref ctx);
        }

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_SelectedIndex = 0;
            m_IsInitialized = false;
            foreach (var child in Children)
            {
                if (child is IResetableNode resetable)
                    resetable.Reset(ref ctx);
            }
        }
        
        public int ChildCount => Children?.Length ?? 0;
        IEnumerable<IBTNode> ICompositeNode.GetChildren() => Children;
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren() => new[] { Children[m_SelectedIndex] };
    }
}