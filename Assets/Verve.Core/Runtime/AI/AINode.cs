namespace Verve.AI
{
    using System;
    using System.Linq;

    
    /// <summary>
    /// 节点状态枚举
    /// </summary>
    [Serializable]
    public enum NodeStatus : byte
    {
        /// <summary> 节点运行中（阻塞当前节点直到完成） </summary>
        Running,
        /// <summary> 节点成功（允许执行后续节点） </summary>
        Success,
        /// <summary> 节点失败（中断当前行为层级，结果将传递给父节点处理） </summary>
        Failure
    }


    /// <summary>
    /// AI节点接口
    /// </summary>
    /// <remarks>
    /// 所有行为树节点必须实现此接口，
    /// 定义节点的执行逻辑，注意尽量使用结构体
    /// </remarks>
    public interface IAINode
    {
        /// <summary>
        /// 执行节点
        /// </summary>
        /// <param name="bb">共享数据黑板</param>
        /// <param name="deltaTime">时间增量</param>
        /// <returns></returns>
        NodeStatus Execute(ref Blackboard bb, float deltaTime);
    }
    
    
    /// <summary>
    /// 可重置节点接口
    /// </summary>
    /// <remarks>
    /// 需要清理内部状态的节点应实现此接口，
    /// 当节点被打断或行为树重置时会被调用
    /// </remarks>
    public interface IResetableNode
    {
        /// <summary>
        /// 重置节点内部状态
        /// </summary>
        void Reset();
    }
    
    
    /// <summary>
    /// 顺序节点（按顺序执行所有子节点）
    /// </summary>
    [Serializable]
    public struct SequenceNode : IAINode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IAINode[] Children;
        
        /// <summary> 当前节点索引值 </summary>
        private int m_CurrentIndex;

        
        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            while (m_CurrentIndex < Children.Length)
            {
                var status = Children[m_CurrentIndex].Execute(ref bb, deltaTime);
        
                if (status == NodeStatus.Running) 
                    return NodeStatus.Running;
            
                if (status == NodeStatus.Failure)
                {
                    m_CurrentIndex = 0;
                    return NodeStatus.Failure;
                }
        
                m_CurrentIndex++;
            }
    
            m_CurrentIndex = 0;
            return NodeStatus.Success;
        }
        
        void IResetableNode.Reset()
        {
            m_CurrentIndex = 0;
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i] is IResetableNode resetable)
                    resetable.Reset();
            }
        }
    }
    
    
    /// <summary>
    /// 条件节点（根据条件回调的返回值决定成功/失败状态）
    /// </summary>
    [Serializable]
    public struct ConditionNode : IAINode
    {
        /// <summary> 条件回调 </summary>
        public Func<Blackboard, bool> Condition;
        
        
        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            return Condition?.Invoke(bb) == true 
                ? NodeStatus.Success 
                : NodeStatus.Failure;
        }
    }
    
    
    /// <summary>
    /// 反转节点（将子节点结果取反）
    /// </summary>
    [Serializable]
    public struct InverterNode : IAINode
    {
        /// <summary> 子节点 </summary>
        public IAINode Child;
        
        
        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            var status = Child.Execute(ref bb, deltaTime);
            return status switch {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => status
            };
        }
    }
    

    /// <summary>
    /// 并行节点（同时执行所有子节点）
    /// </summary>
    [Serializable]
    public struct ParallelNode : IAINode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IAINode[] Children;
        /// <summary> 允许所有子节点成功 </summary>
        public bool RequireAllSuccess;
        
        private NodeStatus[] m_ChildStatus;
    
        
        public NodeStatus Execute(ref Blackboard bb, float deltaTime)
        {
            if (m_ChildStatus == null || m_ChildStatus.Length != Children.Length)
                m_ChildStatus = new NodeStatus[Children.Length];
            
            int successCount = 0;
            int runningCount = 0;
            
            for (int i = 0; i < Children.Length; i++)
            {
                if (m_ChildStatus[i] == NodeStatus.Running)
                {
                    m_ChildStatus[i] = Children[i].Execute(ref bb, deltaTime);
                }
                
                if (m_ChildStatus[i] == NodeStatus.Success) successCount++;
                if (m_ChildStatus[i] == NodeStatus.Running) runningCount++;
                if (m_ChildStatus[i] == NodeStatus.Failure && RequireAllSuccess)
                    return NodeStatus.Failure;
            }
            
            if (runningCount > 0) 
                return NodeStatus.Running;
                
            return successCount > 0 ? NodeStatus.Success : NodeStatus.Failure;
        }
        
        void IResetableNode.Reset()
        {
            m_ChildStatus = null;
            for (int i = 0; i < Children.Length; i++)
            {
                if (Children[i] is IResetableNode resetable)
                    resetable.Reset();
            }
        }
    }
    
    
    /// <summary>
    /// 动作节点（通过回调函数执行具体行为，回调返回值决定节点状态）
    /// </summary>
    [Serializable]
    public struct ActionNode : IAINode
    {
        /// <summary> 动作回调 </summary>
        public Func<Blackboard, NodeStatus> Callback;
        
        
        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            return Callback?.Invoke(bb) ?? NodeStatus.Failure;
        }
    }
    
    
    /// <summary>
    /// 重复执行节点
    /// </summary>
    [Serializable]
    public struct RepeaterNode : IAINode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IAINode Child;
        /// <summary> 循环次数 </summary>
        public int RepeatCount;
        
        private int m_CurrentCount;


        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            if (RepeatCount <= 0) return NodeStatus.Failure;
            if (m_CurrentCount >= RepeatCount)
                return NodeStatus.Success;

            var status = Child.Execute(ref bb, deltaTime);
        
            if (status == NodeStatus.Running)
                return NodeStatus.Running;
            
            if (status == NodeStatus.Success)
                m_CurrentCount++;
            else
                return NodeStatus.Failure;

            return m_CurrentCount < RepeatCount ? NodeStatus.Running : NodeStatus.Success;
        }

        void IResetableNode.Reset()
        {
            m_CurrentCount = 0;
            if (Child is IResetableNode resetable)
                resetable.Reset();
        }
    }


    /// <summary>
    /// 等待时间节点
    /// </summary>
    [Serializable]
    public struct WaitNode : IAINode, IResetableNode
    {
        [Serializable]
        public enum ResetMode : byte
        {
            /// <summary> 自动重置计时器 </summary>
            AutoReset,
            /// <summary> 仅生效一次 </summary>
            OneTime
        }

        
        /// <summary> 等待时长 </summary>
        public float Duration;
        /// <summary> 重置模式 </summary>
        public ResetMode ResetType;
        
        public float ElapsedTime => m_ElapsedTime;
        public bool IsCompleted => m_IsCompleted;
        
        /// <summary> 累计时间 </summary>
        private float m_ElapsedTime;
        /// <summary> 是否完成 </summary>
        private bool m_IsCompleted;


        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            if (Duration <= 0.0f)
                return NodeStatus.Failure;
            if (m_IsCompleted) 
                return NodeStatus.Success;
            
            m_ElapsedTime += deltaTime;
            if (m_ElapsedTime >= Duration)
            {
                m_IsCompleted = true;
                return NodeStatus.Success;
            }
            return NodeStatus.Running;
        }
    
        void IResetableNode.Reset()
        {
            if (ResetType != ResetMode.OneTime)
            {
                m_ElapsedTime = 0f;
                m_IsCompleted = false;
            }
        }
    }
    
    
    /// <summary>
    /// 选择节点（顺序执行子节点，直到某个子节点成功）
    /// </summary>
    [Serializable]
    public struct SelectorNode : IAINode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IAINode[] Children;
        
        private int m_CurrentChildIndex;
    
        
        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            for (int i = m_CurrentChildIndex; i < Children.Length; i++)
            {
                var status = Children[i].Execute(ref bb, deltaTime);
                if (status == NodeStatus.Running)
                {
                    m_CurrentChildIndex = i;
                    return NodeStatus.Running;
                }
                if (status == NodeStatus.Success)
                {
                    return NodeStatus.Success;
                }
            }
            return NodeStatus.Failure;
        }
    
        void IResetableNode.Reset()
        {
            m_CurrentChildIndex = 0;
            foreach (var child in Children)
            {
                if (child is IResetableNode resetable)
                {
                    resetable.Reset();
                }
            }
        }
    }
    
    
    /// <summary>
    /// 随机选择节点
    /// </summary>
    [Serializable]
    public struct RandomSelectorNode : IAINode, IResetableNode
    {
        /// <summary> 子节点 </summary>
        public IAINode[] Children;
        
        private int m_SelectedIndex;
        private bool m_IsInitialized;

        
        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            if (!m_IsInitialized || m_SelectedIndex >= Children.Length)
            {
                m_SelectedIndex = new System.Random().Next(0, Children.Length);
                m_IsInitialized = true;
            }
        
            return Children[m_SelectedIndex].Execute(ref bb, deltaTime);
        }

        void IResetableNode.Reset()
        {
            m_SelectedIndex = 0;
            m_IsInitialized = false;
            foreach (var child in Children)
            {
                if (child is IResetableNode resetable)
                    resetable.Reset();
            }
        }
    }
    
    
    /// <summary>
    /// 权重选择节点
    /// </summary>
    [Serializable]
    public struct WeightedSelectorNode : IAINode, IResetableNode
    {
        [Serializable]
        public struct WeightedNode
        {
            public double Weight;
            public IAINode Node;
        }
    
        
        public WeightedNode[] Nodes;
        
        private int m_SelectedIndex;
        private bool m_IsSelected;
    

        NodeStatus IAINode.Execute(ref Blackboard bb, float deltaTime)
        {
            if (Nodes == null || Nodes.Length == 0) 
                return NodeStatus.Failure;
    
            if (!m_IsSelected)
            {
                double totalWeight = 0;
                for (int i = 0; i < Nodes.Length; i++)
                {
                    if (Nodes[i].Node == null) continue;
                    totalWeight += Math.Max(0, Nodes[i].Weight);
                }
                
                System.Random random = new System.Random();
    
                if (totalWeight <= 0)
                {
                    var validNodes = Nodes.Where(n => n.Node != null).ToArray();
                    if (validNodes.Length == 0) return NodeStatus.Failure;
                    
                    m_SelectedIndex = random.Next(validNodes.Length);
                    m_IsSelected = true;
                    return validNodes[m_SelectedIndex].Node.Execute(ref bb, deltaTime);
                }
    
                var randomValue = random.NextDouble() * totalWeight;
                double cumulativeWeight = 0f;
                
                for (int i = 0; i < Nodes.Length; i++)
                {
                    if (Nodes[i].Node == null) continue;
                    
                    double currentWeight = Math.Max(0, Nodes[i].Weight);
                    cumulativeWeight += currentWeight;
                    
                    if (randomValue <= cumulativeWeight)
                    {
                        m_SelectedIndex = i;
                        break;
                    }
                }
                m_IsSelected = true;
            }
    
            if (Nodes[m_SelectedIndex].Node == null)
            {
                m_IsSelected = false;
                return NodeStatus.Failure;
            }
    
            var status = Nodes[m_SelectedIndex].Node.Execute(ref bb, deltaTime);
            if (status != NodeStatus.Running)
                m_IsSelected = false;
    
            return status;
        }
    
        void IResetableNode.Reset()
        {
            m_SelectedIndex = -1;
            m_IsSelected = false;
            foreach (var weightedNode in Nodes)
            {
                if (weightedNode.Node is IResetableNode resetable)
                {
                    resetable.Reset();
                }
            }
        }
    }
}