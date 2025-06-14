namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    /// 选择节点数据
    /// </summary>
    [Serializable]
    public struct SelectorBTNodeData : INodeData
    {
        /// <summary> 子节点 </summary>
        public IBTNode[] children;
    }
    
    
    /// <summary>
    /// 选择节点
    /// </summary>
    /// <remarks>
    /// 顺序执行子节点，直到某个子节点成功，停止执行剩余子节点
    /// </remarks>
    [CustomBTNode(nameof(SelectorBTNode)), Serializable]
    public struct SelectorBTNode : ICompositeBTNode, IBTNodeResettable
    {
        public SelectorBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        private int m_CurrentChildIndex;
        
        public readonly int CurrentChildIndex => m_CurrentChildIndex;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (ChildCount <= 0) return BTNodeResult.Failed;
            
            for (int i = m_CurrentChildIndex; i < data.children.Length; i++)
            {
                LastResult = this.RunChildNode(ref data.children[i], ref ctx);
                
                if (LastResult == BTNodeResult.Running)
                {
                    m_CurrentChildIndex = i;
                    return BTNodeResult.Running;
                }
                if (LastResult == BTNodeResult.Succeeded)
                {
                    return BTNodeResult.Succeeded;
                }
            }
            return BTNodeResult.Failed;
        }

        #region 可重置节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            m_CurrentChildIndex = 0;
            this.ResetChildrenNode(ref ctx);
        }

        #endregion

        #region 复合节点

        public int ChildCount => data.children?.Length ?? 0;
        
        
        IEnumerable<IBTNode> ICompositeBTNode.GetChildren() => data.children;
        
        IEnumerable<IBTNode> ICompositeBTNode.GetActiveChildren()
        {
            if (m_CurrentChildIndex < ChildCount)
                yield return data.children[m_CurrentChildIndex];
        }

        #endregion
    }
}