namespace Verve.AI
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    /// 行为树
    /// </summary>
    [Serializable]
    public sealed class BehaviorTree : IBehaviorTree
    {
        private struct NodeData
        {
            public IBTNode Node;
            public NodeStatus LastStatus;
        }

        
        private NodeData[] m_ActiveNodes = new NodeData[64];
        private int m_NodeCount;
        private Blackboard m_Blackboard;
        private bool m_IsDisposed;
        private int m_CurrentExecutingIndex = -1;
        private int m_ID;
        public int ID => m_ID;

        public event Action<IBTNode, NodeStatus> OnNodeStatusChanged;
        
        public Blackboard BB
        {
            get => m_Blackboard;
            set
            {
                if (m_Blackboard == value) return;
                m_Blackboard?.Dispose();
                m_Blackboard = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
        
        private static int s_NextTreeId = 1; 
        public static int GenerateTreeId() => Interlocked.Increment(ref s_NextTreeId);


        
        internal BehaviorTree(int initialCapacity = 64, Blackboard blackboard = null)
        {
            m_ID = GenerateTreeId();
            m_ActiveNodes = new NodeData[initialCapacity];
            m_Blackboard = blackboard ?? new Blackboard();
        }

        public void AddNode<T>(in T node) where T : struct, IBTNode
        {
            if (m_NodeCount >= m_ActiveNodes.Length)
            {
                Array.Resize(ref m_ActiveNodes, m_ActiveNodes.Length * 2);
            }
        
            m_ActiveNodes[m_NodeCount++] = new NodeData {
                Node = node,
                LastStatus = NodeStatus.Running
            };
        }

        public void ResetAllNodes()
        {
            for (int i = 0; i < m_NodeCount; i++)
            {
                var node = m_ActiveNodes[i].Node;
                if (node is IResetableNode resetable)
                    resetable.Reset();
                
                m_ActiveNodes[i] = new NodeData {
                    Node = node,
                    LastStatus = NodeStatus.Running
                };
            }
        }

        public NodeStatus GetNodeStatus(int nodeIndex)
        {
            return m_ActiveNodes[nodeIndex].LastStatus;
        }

        public void ResetNode(int nodeIndex)
        {
            var state = m_ActiveNodes[nodeIndex];
            state.LastStatus = NodeStatus.Running;
            m_ActiveNodes[nodeIndex] = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IBehaviorTree.Update(in float deltaTime)
        {
            if (m_IsDisposed || m_Blackboard == null || m_NodeCount == 0) return;

            bool foundRunning = false;
            int startIndex = m_CurrentExecutingIndex >= 0 ? m_CurrentExecutingIndex : 0;
            m_CurrentExecutingIndex = -1;
            
            for (int i = 0; i < startIndex; i++) 
            {
                if (m_ActiveNodes[i].Node is IResetableNode resetable)
                    resetable.Reset();
            }

            for (int i = startIndex; i < m_NodeCount; i++)
            {
                ref var state = ref m_ActiveNodes[i];

                var newStatus = state.Node.Run(ref m_Blackboard, deltaTime);

                if (newStatus != state.LastStatus)
                {
                    OnNodeStatusChanged?.Invoke(state.Node, newStatus);
                    state.LastStatus = newStatus;
                }

                if (newStatus == NodeStatus.Running)
                {
                    m_CurrentExecutingIndex = i;
                    foundRunning = true;
                    break;
                }
            }
        
            if (!foundRunning)
            {
                m_CurrentExecutingIndex = -1;
                ResetAllNodes();
            }
        }

        public IEnumerable<IBTNode> FindNodes(Func<IBTNode, bool> predicate)
        {
            return m_ActiveNodes.Select(x => x.Node).Where(predicate);
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;
            
            m_Blackboard?.Dispose();
            Array.Clear( m_ActiveNodes, 0, m_ActiveNodes.Length);
            m_IsDisposed = true;
        }
    }
}
