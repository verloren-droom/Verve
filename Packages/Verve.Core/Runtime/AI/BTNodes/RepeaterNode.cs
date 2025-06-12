namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct RepeaterNodeData : INodeData
    {
        [Serializable]
        public enum RepeatMode : byte
        {
            /// <summary> 无限 </summary>
            Infinite,
            /// <summary> 次数限制 </summary>
            CountLimited,
            /// <summary> 直到成功 </summary>
            UntilSuccess,
            /// <summary> 直到失败 </summary>
            UntilFailure
        }
        
        
        /// <summary> 子节点 </summary>
        public IBTNode Child;
        /// <summary> 循环次数（仅限 RepeatMode.CountLimited 模式） </summary>
        public int RepeatCount;
        /// <summary> 重复模式 </summary>
        public RepeatMode Mode;
    }
    
    
    /// <summary>
    /// 重复执行节点
    /// </summary>
    [Serializable]
    public struct RepeaterNode : ICompositeNode, IResetableNode, IPreparableNode
    {
        public string Key;
        public RepeaterNodeData Data;
        public NodeStatus LastStatus { get; private set; }

        private int m_CurrentChildIndex;

        public readonly int CurrentChildIndex => m_CurrentChildIndex;

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Data.RepeatCount <= 0 && Data.Mode == RepeaterNodeData.RepeatMode.CountLimited) return NodeStatus.Failure;
            if (CheckExitCondition()) return NodeStatus.Success;

            LastStatus = this.RunChildNode(ref Data.Child, ref ctx);

            if (LastStatus != NodeStatus.Running)
                m_CurrentChildIndex++;
            
            return NodeStatus.Running;
        }
        
        private bool CheckExitCondition()
        {
            return Data.Mode switch
            {
                RepeaterNodeData.RepeatMode.CountLimited => m_CurrentChildIndex >= Data.RepeatCount,
                RepeaterNodeData.RepeatMode.UntilSuccess => LastStatus == NodeStatus.Success,
                RepeaterNodeData.RepeatMode.UntilFailure => LastStatus == NodeStatus.Failure,
                _ => false
            };
        }

        #region 可重置节点

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_CurrentChildIndex = 0;
            LastStatus = NodeStatus.Running;
            if (Data.Child is IResetableNode resetable)
                resetable.Reset(ref ctx);
        }

        #endregion

        #region 复合节点

        public int ChildCount => 1;
        
        
        IEnumerable<IBTNode> ICompositeNode.GetChildren() => new[] { Data.Child };
        
        IEnumerable<IBTNode> ICompositeNode.GetActiveChildren()
        {
            if (LastStatus == NodeStatus.Running)
            {
                yield return Data.Child;
            }
        }

        #endregion
        
        #region 可准备节点

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IPreparableNode.Prepare(ref NodeRunContext ctx)
        {
            if (ctx.BB.HasValue(Key))
            {
                Data = ctx.BB.GetValue<RepeaterNodeData>(Key);
            }
        }
        
        #endregion
    }
}