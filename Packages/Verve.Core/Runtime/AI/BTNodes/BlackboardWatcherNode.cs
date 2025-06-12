namespace Verve.AI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    
    
    [Serializable]
    public struct BlackboardWatcherNodeData<T> : INodeData
        where T : class
    {
        [Serializable]
        public enum WatchMode : byte
        {
            /// <summary> 值变化时触发 </summary>
            OnValueChanged,
            /// <summary> 值首次可用时触发 </summary>
            OnValueAvailable,
            /// <summary> 值丢失时触发 </summary>
            OnValueLost,
            /// <summary> 值变化或丢失时触发 </summary>
            OnAnyChange
        }

        
        /// <summary> 被装饰的子节点 </summary>
        [NotNull] public IBTNode Child;
        /// <summary> 监听的黑板键 </summary>
        [NotNull] public string KeyToWatch;
        /// <summary> 监听模式 </summary>
        public WatchMode Mode;
        /// <summary> 值变化回调（可选） </summary>
        public Action<T> OnValueChanged;
    }


    /// <summary>
    /// 黑板值监听装饰器节点
    /// </summary>
    [Serializable]
    public struct BlackboardWatcherNode<T> : ICompositeNode, IResetableNode 
        where T : class
    {
        public BlackboardWatcherNodeData<T> Data;
        public NodeStatus LastStatus { get; private set; }
        
        /// <summary> 最后一次缓存的值 </summary>
        private T m_CachedValue;
        /// <summary> 是否已订阅 </summary>
        private bool m_IsSubscribed;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NodeStatus IBTNode.Run(ref NodeRunContext ctx)
        {
            if (Data.Child == null || string.IsNullOrEmpty(Data.KeyToWatch)) return NodeStatus.Failure;
            
            if (!m_IsSubscribed)
            {
                SubscribeToBlackboard(ctx.BB);
            }

            if (Data.Child is IPreparableNode preparable)
            {
                preparable.Prepare(ref ctx);
            }
            LastStatus = Data.Child?.Run(ref ctx) ?? NodeStatus.Failure;
            return LastStatus;
        }

        private void HandleValueChanged(string key, object newValue)
        {
            if (key != Data.KeyToWatch) return;

            T typedValue = newValue as T;
            
            switch (Data.Mode)
            {
                case BlackboardWatcherNodeData<T>.WatchMode.OnValueChanged:
                    if (!EqualityComparer<T>.Default.Equals(typedValue, m_CachedValue))
                    {
                        ResetChildNode();
                    }
                    break;

                case BlackboardWatcherNodeData<T>.WatchMode.OnValueAvailable:
                    if (m_CachedValue == null && typedValue != null)
                    {
                        ResetChildNode();
                    }
                    break;

                case BlackboardWatcherNodeData<T>.WatchMode.OnValueLost:
                    if (m_CachedValue != null && typedValue == null)
                    {
                        ResetChildNode();
                    }
                    break;

                case BlackboardWatcherNodeData<T>.WatchMode.OnAnyChange:
                    if (!EqualityComparer<T>.Default.Equals(typedValue, m_CachedValue))
                    {
                        ResetChildNode();
                    }
                    break;
            }

            m_CachedValue = typedValue;
            Data.OnValueChanged?.Invoke(typedValue);
        }

        private void ResetChildNode()
        {
            var resetCtx = new NodeResetContext
            {
                BB = default,
                Mode = NodeResetMode.Full
            };

            (Data.Child as IResetableNode)?.Reset(ref resetCtx);
        }
        
        private void SubscribeToBlackboard(Blackboard bb)
        {
            bb.OnValueChanged += HandleValueChanged;
            m_IsSubscribed = true;
            
            bb.TryGetValue<T>(Data.KeyToWatch, out var initialValue);
            m_CachedValue = initialValue;
        }

        private void UnsubscribeFromBlackboard(Blackboard bb)
        {
            bb.OnValueChanged -= HandleValueChanged;
            m_IsSubscribed = false;
        }

        #region 复合节点

        public int ChildCount => 1;

        public IEnumerable<IBTNode> GetChildren() => new IBTNode[] { Data.Child };

        public IEnumerable<IBTNode> GetActiveChildren()
        {
            if (LastStatus == NodeStatus.Running)
            {
                yield return Data.Child;
            }
        }

        #endregion

        #region 可重置节点

        void IResetableNode.Reset(ref NodeResetContext ctx)
        {
            m_CachedValue = default;
            
            if (m_IsSubscribed)
            {
                UnsubscribeFromBlackboard(ctx.BB);
                SubscribeToBlackboard(ctx.BB);
            }
            
            var resetCtx = new NodeResetContext
            {
                BB = ctx.BB,
                Mode = NodeResetMode.Full
            };
            
            (Data.Child as IResetableNode)?.Reset(ref resetCtx);
        }

        #endregion
    }
}