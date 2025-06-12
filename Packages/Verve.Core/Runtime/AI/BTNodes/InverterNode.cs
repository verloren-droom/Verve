namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    
    [Serializable]
    public struct InverterNodeData : INodeData
    {
        /// <summary> 子节点 </summary>
        [NotNull] public IBTNode Child;
    }


    /// <summary>
    /// 反转节点（将子节点结果取反）
    /// </summary>
    [Serializable]
    public struct InverterNode : ICompositeNode
    {
        public InverterNodeData Data;
        public NodeStatus LastStatus { get; private set; }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            LastStatus = this.RunChildNode(ref Data.Child, ref ctx);
            return LastStatus switch {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => LastStatus
            };
        }

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
    }
}