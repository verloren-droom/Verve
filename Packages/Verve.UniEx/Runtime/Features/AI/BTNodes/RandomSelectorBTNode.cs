namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;


    /// <summary>
    /// 随机选择节点数据
    /// </summary>
    [Serializable]
    public struct RandomSelectorBTNodeData : INodeData
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] children;
    }
    
    
    /// <summary>
    /// 随机选择节点
    /// </summary>
    /// <remarks>
    /// 随机选择一个子节点运行
    /// </remarks>
    [CustomBTNode(nameof(RandomSelectorBTNode)), Serializable]
    public struct RandomSelectorBTNode : ICompositeBTNode, IBTNodeResettable
    {
        public RandomSelectorBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        private int m_SelectedIndex;
        private bool m_IsInitialized;

        public readonly int SelectedIndex => m_SelectedIndex;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (!m_IsInitialized || m_SelectedIndex >= data.children.Length)
            {
                m_SelectedIndex = new System.Random().Next(0, data.children.Length);
                m_IsInitialized = true;
            }

            LastResult = this.RunChildNode(ref data.children[m_SelectedIndex], ref ctx);
            return LastResult;
        }

        #region 可重置节点
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_SelectedIndex = 0;
            m_IsInitialized = false;
            this.ResetChildrenNode(ref ctx);
        }

        #endregion

        #region 复合节点

        public int ChildCount => data.children?.Length ?? 0;
        
        
        IEnumerable<IBTNode> ICompositeBTNode.GetChildren() => data.children;
        
        IEnumerable<IBTNode> ICompositeBTNode.GetActiveChildren() => new[] { data.children[m_SelectedIndex] };
        
        #endregion
    }
}