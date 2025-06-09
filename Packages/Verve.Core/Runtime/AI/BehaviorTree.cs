namespace Verve.AI
{
    using System;
    using System.Text;
    using System.Linq;
    using System.Buffers;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.CodeAnalysis;


    /// <summary>
    /// 行为树
    /// </summary>
    [Serializable]
    public partial class BehaviorTree : IBehaviorTree
    {
        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct NodeData
        {
            [FieldOffset(0)]
            public IBTNode Node;
            [FieldOffset(8)]
            public NodeStatus LastStatus;
            [FieldOffset(12)] private int _padding;
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
        private BitArray m_PausedStateMask;
        private bool m_IsPaused;
        private int m_PausedAtIndex = -1;
        
        public int ID => m_ID;
        
        public event Action<IBTNode, NodeStatus> OnNodeStatusChanged;
        public event Action<bool> OnPauseStateChanged;

        public bool IsPaused
        {
            get => m_IsPaused;
            private set
            {
                if (m_IsPaused == value) return;
                m_IsPaused = value;
                OnPauseStateChanged?.Invoke(value);
            }
        }
        
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
        private static int s_NextTreeId = 0; 
        public static int GenerateTreeId() => Interlocked.Increment(ref s_NextTreeId);
        
        
        public BehaviorTree(int initialCapacity = 128, Blackboard blackboard = null)
        {
            m_ID = GenerateTreeId();
            m_ActiveNodes = new NodeData[initialCapacity];
            m_Blackboard = blackboard ?? new Blackboard();
        }

        ~BehaviorTree() => Dispose();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNode<T>([NotNull] in T node) where T : struct, IBTNode
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetAllNodes([NotNull] NodeResetMode resetMode = NodeResetMode.Full)
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

        public void ResetNode(int nodeIndex)
        {
            var state = m_ActiveNodes[nodeIndex];
            state.LastStatus = NodeStatus.Running;
            m_ActiveNodes[nodeIndex] = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(in float deltaTime)
        {
            if (m_IsDisposed || m_Blackboard == null || m_NodeCount == 0 || IsPaused) return;

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
        
        public void Pause()
        {
            if (IsPaused) return;
        
            m_PausedAtIndex = m_CurrentExecutingIndex;
            m_PausedStateMask = new BitArray(m_NodeCount);
            for (int i = 0; i < m_NodeCount; i++) {
                m_PausedStateMask[i] = m_ActiveNodes[i].LastStatus == NodeStatus.Running;
            }
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
        
            m_CurrentExecutingIndex = m_PausedAtIndex;
            for (int i = 0; i < m_NodeCount; i++) {
                if (m_PausedStateMask[i]) {
                    m_ActiveNodes[i].LastStatus = NodeStatus.Running;
                }
            }
            IsPaused = false;
        }

        public IEnumerable<IBTNode> FindNodes(Func<IBTNode, bool> predicate)
        {
            foreach (var nodeData in m_ActiveNodes.Take(m_NodeCount))
            {
                var node = nodeData.Node;

                var stack = new Stack<IBTNode>();
                stack.Push(node);
        
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (predicate(current)) yield return current;

                    if (current is ICompositeNode composite)
                    {
                        foreach (var child in composite.GetChildren())
                            stack.Push(child);
                    }
                }
            }
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
        
        private IEnumerable<string> GenerateNodePaths()
        {
            var paths = new List<string>();
            for (int i = 0; i < m_NodeCount; i++)
            {
                var node = m_ActiveNodes[i].Node;
                TraverseNode(node, "", paths);
            }
            return paths.Distinct();
        }
        
        private void TraverseNode(IBTNode node, string parentPath, List<string> paths)
        {
            var currentPath = string.IsNullOrEmpty(parentPath) 
                ? node.GetType().Name 
                : $"{parentPath}/{node.GetType().Name}";
            
            paths.Add(currentPath);
        
            if (node is ICompositeNode composite)
            {
                foreach (var child in composite.GetChildren())
                {
                    TraverseNode(child, currentPath, paths);
                }
            }
        }
        
        public IEnumerable<string> GetActivePath()
        {
            var activePaths = new List<string>();
            for (int i = 0; i < m_NodeCount; i++)
            {
                if (m_ActiveNodes[i].LastStatus == NodeStatus.Running)
                {
                    TraverseNode(m_ActiveNodes[i].Node, "", activePaths);
                }
            }
            return activePaths.Distinct();
        }
        
        public IEnumerable<NodeData> GetActiveNodes()
        {
            for (int i = 0; i < m_NodeCount; i++)
            {
                if (m_ActiveNodes[i].LastStatus == NodeStatus.Running)
                {
                    yield return m_ActiveNodes[i];
                }
            }
        }
        
        public NodeStatus GetNodeStatus(IBTNode node)
        {
            foreach(var nodeData in GetActiveNodes())
            {
                if(nodeData.Node.Equals(node))
                {
                    return nodeData.LastStatus;
                }
            }
            return NodeStatus.Running;
        }

        public IReadOnlyList<IBTNode> AllNodes => 
            m_ActiveNodes.Take(m_NodeCount).Select(n => n.Node).ToList();

        public override string ToString()
        {
            var sb = new StringBuilder($"BehaviorTree(ID:{m_ID}, Nodes:{m_NodeCount})\n");
            sb.AppendLine("Node Paths:");
            
            foreach (var path in GenerateNodePaths())
            {
                sb.AppendLine($"\t{path}");
            }
            
            return sb.ToString();
        }
    }
}