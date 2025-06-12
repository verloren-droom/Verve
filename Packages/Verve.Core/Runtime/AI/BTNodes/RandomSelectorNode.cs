namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;


    [Serializable]
    public struct RandomSelectorNodeData : INodeData
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] Children;
    }
    
    
    /// <summary>
    /// 随机选择节点
    /// </summary>
    [Serializable]
    public struct RandomSelectorNode : ICompositeNode, IResetableNode
    {
        public RandomSelectorNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        private int m_SelectedIndex;
        private bool m_IsInitialized;

        public readonly int SelectedIndex => m_SelectedIndex;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (!m_IsInitialized || m_SelectedIndex >= Data.Children.Length)
            {
                m_SelectedIndex = new System.Random().Next(0, Data.Children.Length);
                m_IsInitialized = true;
            }

            LastStatus = this.RunChildNode(ref Data.Children[m_SelectedIndex], ref ctx);
            return LastStatus;
        }

        #region 可重置节点
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_SelectedIndex = 0;
            m_IsInitialized = false;
            foreach (var child in Data.Children)
            {
                if (child is IResetableNode resetable)
                    resetable.Reset(ref ctx);
            }
        }

        #endregion

        #region 复合节点

        public int ChildCount => Data.Children?.Length ?? 0;
        
        
        IEnumerable<IBTNode> ICompositeNode.GetChildren() => Data.Children;
        
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren() => new[] { Data.Children[m_SelectedIndex] };
        
        #endregion
    }
}