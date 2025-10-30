namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Linq;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para>并行节点数据</para>
    /// </summary>
    [Serializable]
    public struct ParallelBTNodeData : INodeData
    {
        /// <summary>
        ///   <para>子节点</para>
        /// </summary>
        public IBTNode[] children;
        /// <summary>
        ///   <para>允许所有子节点成功</para>
        /// </summary>
        public bool requireAllSuccess;
    }
    
    
    /// <summary>
    ///   <para>并行节点</para>
    ///   <para>同时执行所有子节点</para>
    /// </summary>
    [CustomBTNode(nameof(ParallelBTNode)), Serializable]
    public struct ParallelBTNode : ICompositeBTNode, IBTNodeResettable
    {
        public ParallelBTNodeData data;
        public BTNodeResult LastResult { get; private set; }

        private BTNodeResult[] m_ChildResult;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        BTNodeResult IBTNode.Run(ref BTNodeRunContext ctx)
        {
            if (m_ChildResult == null || m_ChildResult.Length != data.children.Length)
                m_ChildResult = new BTNodeResult[data.children.Length];
            
            int successCount = 0;
            int runningCount = 0;
            
            for (int i = 0; i < data.children.Length; i++)
            {
                if (m_ChildResult[i] == BTNodeResult.Running)
                {
                    m_ChildResult[i] = this.RunChildNode(ref data.children[i], ref ctx);
                }
                
                if (m_ChildResult[i] == BTNodeResult.Succeeded) successCount++;
                if (m_ChildResult[i] == BTNodeResult.Running) runningCount++;
                if (m_ChildResult[i] == BTNodeResult.Failed && data.requireAllSuccess)
                    return BTNodeResult.Failed;
            }
            
            if (runningCount > 0) 
                return BTNodeResult.Running;
            
            return successCount > 0 ? BTNodeResult.Succeeded : BTNodeResult.Failed;
        }

        #region 可重置节点
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBTNodeResettable.Reset(ref BTNodeResetContext ctx)
        {
            if (m_ChildResult == null) return;
            Array.Clear(m_ChildResult, 0, m_ChildResult.Length);
            this.ResetChildrenNode(ref ctx);
        }

        #endregion

        #region 复合节点

        public int ChildCount => data.children?.Length ?? 0;
        
        
        IEnumerable<IBTNode> ICompositeBTNode.GetChildren() => data.children;
        
        IEnumerable<IBTNode> ICompositeBTNode.GetActiveChildren()
        {
            for (int i = 0; i < ChildCount; i++)
            {
                if (m_ChildResult[i] == BTNodeResult.Running)
                    yield return data.children[i];
            }
        }

        #endregion
    }
}