namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>顺序节点数据</para>
    /// </summary>
    [Serializable]
    public struct SequenceBTNodeData : INodeData
    {
        /// <summary>
        ///   <para>子节点</para>
        /// </summary>
        [NotNull] public IBTNode[] children;
    }
    

    /// <summary>
    ///   <para>顺序节点</para>
    ///   <para>按照顺序执行所有子节点，直到一个子节点返回失败或者完成，或者所有子节点都返回成功或者完成</para>
    /// </summary>
    [CustomBTNode(nameof(SequenceBTNode)), Serializable]
    public struct SequenceBTNode : ICompositeBTNode, IBTNodeResettable
    {
        public SequenceBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        private int m_CurrentChildIndex;

        /// <summary>
        ///   <para>当前节点索引值</para>
        /// </summary>
        public readonly int CurrentChildIndex => m_CurrentChildIndex;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (ChildCount <= 0) return BTNodeResult.Failed;

            while (m_CurrentChildIndex < data.children.Length)
            {
                LastResult = this.RunChildNode(ref data.children[m_CurrentChildIndex], ref ctx);
        
                if (LastResult == BTNodeResult.Running) 
                    return BTNodeResult.Running;
            
                if (LastResult == BTNodeResult.Failed)
                {
                    m_CurrentChildIndex = 0;
                    return BTNodeResult.Failed;
                }
        
                m_CurrentChildIndex++;
            }
    
            m_CurrentChildIndex = 0;
            return BTNodeResult.Succeeded;
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
        
        
        public IEnumerable<IBTNode> GetChildren() => data.children;
        
        public IEnumerable<IBTNode> GetActiveChildren()
        {
            if (m_CurrentChildIndex < ChildCount)
                yield return data.children[m_CurrentChildIndex];
        }

        #endregion
    }
}