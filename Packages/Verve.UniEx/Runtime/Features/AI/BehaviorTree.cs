#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.AI
{
    using System;
    using Verve.AI;
    using System.Text;
    using UnityEngine;
    using System.Linq;
    using System.Buffers;
    using System.Threading;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.CodeAnalysis;


    /// <summary>
    ///   <para>行为树</para>
    /// </summary>
    [Serializable]
    public partial class BehaviorTree : IBehaviorTree
    {
        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct NodeData
        {
            [FieldOffset(0)]
            public IBTNode node;
            [FieldOffset(8)]
            public BTNodeResult lastResult;
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


        [SerializeField, Tooltip("唯一ID"), ReadOnly] private string m_TreeID;
        private NodeData[] m_ActiveNodes = new NodeData[64];
        private int m_NodeCount;
        private Blackboard m_Blackboard;
        private bool m_IsDisposed;
        private int m_CurrentExecutingIndex = -1;
        private BitArray m_PausedStateMask;
        private bool m_IsPaused;
        private int m_PausedAtIndex = -1;
        
        public string TreeID => m_TreeID;
        public bool IsDisposed => m_IsDisposed;
        
        public event Action<IBTNode, BTNodeResult> OnNodeResultChanged;
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
        
        public IBlackboard BB
        {
            get => m_Blackboard;
            set
            {
                if (m_Blackboard == value) return;
                m_Blackboard?.Dispose();
                m_Blackboard = (value) as Blackboard ?? throw new ArgumentNullException(nameof(value));
            }
        }

        
        private static readonly ParallelOptions s_ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };
        
        private static readonly ArrayPool<NodeData> s_NodePool = ArrayPool<NodeData>.Create(
            maxArrayLength: 1024 * 1024,
            maxArraysPerBucket: 10
        );
        
        private static readonly List<BehaviorTree> s_AllBehaviorTrees = new();
        
        /// <summary>
        ///   <para>所有行为树</para>
        /// </summary>
        public static IReadOnlyList<BehaviorTree> AllBehaviorTrees => new ReadOnlyCollection<BehaviorTree>(s_AllBehaviorTrees);


        public BehaviorTree(int initialCapacity = 128, Blackboard blackboard = null)
        {
            m_TreeID = $"bt_{Guid.NewGuid().ToString("N")[..16]}";
            m_ActiveNodes = new NodeData[initialCapacity];
            m_Blackboard = blackboard ?? new Blackboard();
            s_AllBehaviorTrees.Add(this);
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
                node = node,
                lastResult = BTNodeResult.Running
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetAllNodes([NotNull] BTNodeResetMode resetMode = BTNodeResetMode.Full)
        {
            for (int i = 0; i < m_NodeCount; i++)
            {
                var node = m_ActiveNodes[i].node;
                var resetCtx = new BTNodeResetContext()
                {
                    bb = m_Blackboard,
                    resetMode = resetMode
                };
                if (node is IBTNodeResettable resetable)
                    resetable.Reset(ref resetCtx);
                
                m_ActiveNodes[i] = new NodeData {
                    node = node,
                    lastResult = BTNodeResult.Running
                };
            }
        }

        public void ResetNode(int nodeIndex)
        {
            var result = m_ActiveNodes[nodeIndex];
            result.lastResult = BTNodeResult.Running;
            m_ActiveNodes[nodeIndex] = result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(in float deltaTime)
        {
            if (m_IsDisposed || m_Blackboard == null || m_NodeCount == 0 || IsPaused) return;

            bool foundRunning = false;
            int startIndex = m_CurrentExecutingIndex >= 0 ? m_CurrentExecutingIndex : 0;
            m_CurrentExecutingIndex = -1;
            
            var ctx = new BTNodeRunContext {
                bb = m_Blackboard,
                deltaTime = deltaTime,
            };

            for (int i = 0; i < startIndex; i++)
            {
                var resetCtx = new BTNodeResetContext()
                {
                    bb = m_Blackboard,
                    resetMode = BTNodeResetMode.Partial
                };
                if (m_ActiveNodes[i].node is IBTNodeResettable resettableNode)
                    resettableNode.Reset(ref resetCtx);
            }

            Action<int> body = (i) =>
            {
                ref var result = ref m_ActiveNodes[i];
                
                if (result.node is IBTNodePreparable prepareNode)
                {
                    prepareNode.Prepare(ref ctx);
                }
                
                var newResult = result.node.Run(ref ctx);

                if (newResult != result.lastResult)
                {
                    OnNodeResultChanged?.Invoke(result.node, newResult);
                    result.lastResult = newResult;
                }
            
                if (newResult == BTNodeResult.Running)
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
                m_PausedStateMask[i] = m_ActiveNodes[i].lastResult == BTNodeResult.Running;
            }
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
        
            m_CurrentExecutingIndex = m_PausedAtIndex;
            for (int i = 0; i < m_NodeCount; i++) {
                if (m_PausedStateMask[i]) {
                    m_ActiveNodes[i].lastResult = BTNodeResult.Running;
                }
            }
            IsPaused = false;
        }

        public IEnumerable<IBTNode> FindNodes(Func<IBTNode, bool> predicate)
        {
            foreach (var nodeData in m_ActiveNodes?.Take(m_NodeCount))
            {
                var node = nodeData.node;

                var stack = new Stack<IBTNode>();
                stack.Push(node);
        
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (predicate(current)) yield return current;

                    if (current is ICompositeBTNode composite)
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
            
            s_AllBehaviorTrees.Remove(this);
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
                var node = m_ActiveNodes[i].node;
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
        
            if (node is ICompositeBTNode composite)
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
                if (m_ActiveNodes[i].lastResult == BTNodeResult.Running)
                {
                    TraverseNode(m_ActiveNodes[i].node, "", activePaths);
                }
            }
            return activePaths.Distinct();
        }
        
        public IEnumerable<NodeData> GetActiveNodes()
        {
            for (int i = 0; i < m_NodeCount; i++)
            {
                if (m_ActiveNodes[i].lastResult == BTNodeResult.Running)
                {
                    yield return m_ActiveNodes[i];
                }
            }
        }
        
        public BTNodeResult GetNodeResult(IBTNode node)
        {
            foreach(var nodeData in GetActiveNodes())
            {
                if(nodeData.node.Equals(node))
                {
                    return nodeData.lastResult;
                }
            }
            return BTNodeResult.Running;
        }

        public IReadOnlyList<IBTNode> AllNodes => m_ActiveNodes.Take(m_NodeCount).Select(n => n.node).ToList();

        public override string ToString()
        {
            var sb = new StringBuilder($"BehaviorTree(ID: {m_TreeID}, Children: {m_NodeCount})\n");
            sb.AppendLine("Node Paths: ");
            
            foreach (var path in GenerateNodePaths())
            {
                sb.AppendLine($"\t{path}");
            }

            return sb.ToString();
        }
    }
}

#endif