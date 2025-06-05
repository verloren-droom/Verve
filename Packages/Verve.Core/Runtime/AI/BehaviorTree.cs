namespace Verve.AI
{
    using System;
    using System.Linq;
    using System.Buffers;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;


    /// <summary>
    /// 行为树
    /// </summary>
    [Serializable]
    public partial class BehaviorTree : IBehaviorTree
    {
        [Serializable]
        // [StructLayout(LayoutKind.Sequential, Pack = 16)]
        private struct NodeData
        {
            public IBTNode Node;
            public NodeStatus LastStatus;
            // private fixed byte _padding[4]; // 显式填充剩余容量
        }
        
        private static int? s_MainThreadId;
    
        public static bool IsMainThread
        {
            get
            {
                s_MainThreadId ??= Thread.CurrentThread.ManagedThreadId;
                return Thread.CurrentThread.ManagedThreadId == s_MainThreadId.Value;
            }
        }


        private NodeData[] m_ActiveNodes = new NodeData[64];
        private int m_NodeCount;
        private Blackboard m_Blackboard;
        private bool m_IsDisposed;
        private int m_CurrentExecutingIndex = -1;
        private int m_ID;
        public int ID => m_ID;

        public bool IsActive { get; set; } = true;
        
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

        private static readonly ParallelOptions s_ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };
        private static readonly ArrayPool<NodeData> s_NodePool = ArrayPool<NodeData>.Create(
            maxArrayLength: 1024 * 1024,
            maxArraysPerBucket: 10
        );
        private static int s_NextTreeId = 1; 
        public static int GenerateTreeId() => Interlocked.Increment(ref s_NextTreeId);
        
        
        public BehaviorTree(int initialCapacity = 128, Blackboard blackboard = null)
        {
            m_ID = GenerateTreeId();
            m_ActiveNodes = new NodeData[initialCapacity];
            m_Blackboard = blackboard ?? new Blackboard();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNode<T>(in T node) where T : struct, IBTNode
        {
            if (m_NodeCount >= m_ActiveNodes.Length)
            {
                int newSize = (int)(m_ActiveNodes.Length * 1.5f);
                var newPool = s_NodePool.Rent(newSize);
                Array.Copy(m_ActiveNodes, 0, newPool, 0, m_NodeCount);
                s_NodePool.Return(m_ActiveNodes);
                m_ActiveNodes = newPool;
            }
        
            m_ActiveNodes[m_NodeCount++] = new NodeData {
                Node = node,
                LastStatus = NodeStatus.Running
            };
        }

        public void ResetAllNodes(NodeResetMode resetMode = NodeResetMode.Full)
        {
            for (int i = 0; i < m_NodeCount; i++)
            {
                var node = m_ActiveNodes[i].Node;
                var resetCtx = new NodeResetContext()
                {
                    BB = m_Blackboard,
                    Mode = resetMode
                };
                if (node is IResetableNode resetable)
                    resetable.Reset(ref resetCtx);
                
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
            if (m_IsDisposed || m_Blackboard == null || m_NodeCount == 0 || !IsActive) return;

            bool foundRunning = false;
            int startIndex = m_CurrentExecutingIndex >= 0 ? m_CurrentExecutingIndex : 0;
            m_CurrentExecutingIndex = -1;

            for (int i = 0; i < startIndex; i++)
            {
                var resetCtx = new NodeResetContext()
                {
                    BB = m_Blackboard,
                    Mode = NodeResetMode.Partial
                };
                if (m_ActiveNodes[i].Node is IResetableNode resetable)
                    resetable.Reset(ref resetCtx);
            }
            
            var ctx = new NodeRunContext {
                BB = m_Blackboard,
                DeltaTime = deltaTime,
            };
            
            Action<int> body = (i) =>
            {
                ref var state = ref m_ActiveNodes[i];
                var newStatus = state.Node.Run(ref ctx);
            
                if (newStatus != state.LastStatus)
                {
                    OnNodeStatusChanged?.Invoke(state.Node, newStatus);
                    state.LastStatus = newStatus;
                }
            
                if (newStatus == NodeStatus.Running)
                {
                    m_CurrentExecutingIndex = i;
                    foundRunning = true;
                }
            };
            
            if (IsMainThread)
            {
                for (int i = startIndex; i < m_NodeCount; i++)
                {
                    body(i);
                }
            }
            else 
            {
                Parallel.For(startIndex, m_NodeCount, s_ParallelOptions, body);
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
            s_NodePool.Return(m_ActiveNodes);
            Array.Clear( m_ActiveNodes, 0, m_ActiveNodes.Length);
            m_ActiveNodes = null;
            m_IsDisposed = true;
        }
    }
}
