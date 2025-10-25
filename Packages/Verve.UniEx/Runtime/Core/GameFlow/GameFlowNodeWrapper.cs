#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///  <para>游戏流程节点包装器</para>
    /// </summary>
    [Serializable]
    public sealed class GameFlowNodeWrapper : ScriptableObject
    {
        [SerializeField, Tooltip("节点名称"), ReadOnly] private string m_NodeName;
        [SerializeField, Tooltip("节点描述"), ReadOnly] private string m_Description;
        [SerializeField, Tooltip("节点位置"), ReadOnly] private Vector2 m_Position;
        [SerializeField, Tooltip("节点大小"), ReadOnly] private Vector2 m_NodeSize = new Vector2(DEFAULT_NODE_WIDTH, DEFAULT_NODE_HEIGHT);
        
        [SerializeField, Tooltip("被包装的节点"), SerializeReference] private IGameFlowNode m_WrappedNode;
        [SerializeField] private List<VisualFlowPort> m_InputPorts = new List<VisualFlowPort>();
        [SerializeField] private List<VisualFlowPort> m_OutputPorts = new List<VisualFlowPort>();
        
        public Vector2 Position { get => m_Position; set => m_Position = value; }
        public Vector2 NodeSize { get => m_NodeSize; set => m_NodeSize = value; }
        public string NodeName { get => m_NodeName; set => m_NodeName = value; }
        /// <summary>
        ///  <para>被包装的节点</para>
        /// </summary>
        public IGameFlowNode WrappedNode => m_WrappedNode;
        public List<VisualFlowPort> InputPorts => m_InputPorts;
        public List<VisualFlowPort> OutputPorts => m_OutputPorts;
        public bool IsExecuting => m_WrappedNode?.IsExecuting ?? false;
        public bool IsCompleted => m_WrappedNode?.IsCompleted ?? false;
        
        /// <summary>
        ///  <para>默认节点宽度</para>
        /// </summary>
        public const float DEFAULT_NODE_WIDTH = 180f;
        /// <summary>
        ///  <para>默认节点高度</para>
        /// </summary>
        public const float DEFAULT_NODE_HEIGHT = 80f;
        /// <summary>
        ///  <para>节点端口高度</para>
        /// </summary>
        public const float PORT_HEIGHT = 22f;
        /// <summary>
        ///  <para>按钮区域高度</para>
        /// </summary>
        public const float BUTTON_AREA_HEIGHT = 40f;

        /// <summary>
        ///  <para>节点端口类型定义</para>
        /// </summary>
        public enum PortType
        {
            /// <summary> 输入端口 </summary>
            In,
            /// <summary> 子节点输出端口 </summary>
            Child,
            /// <summary> 自定义输入端口 </summary>
            CustomInput,
            /// <summary> 自定义输出端口 </summary>
            CustomOutput
        }
        
        /// <summary>
        ///  <para>创建节点包装器</para>
        /// </summary>
        /// <param name="node">游戏流程节点</param>
        /// <param name="position">节点位置</param>
        /// <returns>节点包装器</returns>
        public static GameFlowNodeWrapper CreateWrapper(GameFlowNode node, Vector2 position)
        {
            var wrapper = CreateInstance<GameFlowNodeWrapper>();
            var nodeType = node.GetType();
#if UNITY_EDITOR
            wrapper.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            wrapper.name = $"{nodeType.Name}_{DateTime.Now.Ticks:X}";
#endif
            var attribute = nodeType.GetCustomAttribute<GameFlowNodeAttribute>();
            wrapper.m_NodeName = attribute?.NodeName ?? nodeType.Name;
            wrapper.m_Description = attribute?.Description ?? "";
            
            wrapper.m_Position = position;
            wrapper.m_WrappedNode = node;
            
            wrapper.RefreshPorts();

            return wrapper;
        }

        /// <summary>
        ///  <para>刷新所有端口</para>
        /// </summary>
        public void RefreshPorts()
        {
            if (m_WrappedNode == null) 
            {
                Debug.LogWarning("Cannot refresh ports: WrappedNode is null");
                return;
            }

            var oldConnections = new Dictionary<string, List<string>>();
            foreach (var port in m_InputPorts.Concat(m_OutputPorts))
            {
                if (port != null && !string.IsNullOrEmpty(port.PortID) && port.PortType != PortType.Child)
                {
                    oldConnections[port.PortID] = new List<string>(port.ConnectionIDs);
                }
            }
            
            var childConnections = new Dictionary<int, List<string>>();
            for (int i = 0; i < m_OutputPorts.Count; i++)
            {
                var port = m_OutputPorts[i];
                if (port != null && port.PortType == PortType.Child && !string.IsNullOrEmpty(port.PortID))
                {
                    childConnections[i] = new List<string>(port.ConnectionIDs);
                }
            }
            
            var oldInputPorts = new List<VisualFlowPort>(m_InputPorts);
            m_InputPorts.Clear();
            m_OutputPorts.Clear();
            
            var nodeType = m_WrappedNode.GetType();
            
            RefreshInputPorts(oldInputPorts);
            
            RefreshCustomOutputPorts(nodeType);
            
            if (m_WrappedNode is ICompositeGameFlowNode compositeNode)
            {
                RefreshCompositePorts(compositeNode, childConnections);
            }
            
            RestoreConnectionIDs(oldConnections);
            
            UpdateNodeSize();
        }

        /// <summary>
        ///  <para>刷新节点输入端口</para>
        /// 
        /// </summary>
        private void RefreshInputPorts(List<VisualFlowPort> oldInputPorts)
        {
            var defaultInputPort = oldInputPorts.FirstOrDefault(p => p.PortType == PortType.In && p.DisplayName == "In");
            if (defaultInputPort != null)
            {
                m_InputPorts.Add(defaultInputPort);
            }
            else
            {
                m_InputPorts.Add(new VisualFlowPort(this, PortType.In, "In"));
            }
            
            var nodeType = m_WrappedNode.GetType();
            var fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var inputAttr = field.GetCustomAttribute<GameFlowInputAttribute>();
                if (inputAttr != null && field.FieldType.IsAssignableFrom(typeof(IGameFlowNode)))
                {
                    var portName = inputAttr.DisplayName ?? field.Name;
                    
                    var existingPort = oldInputPorts.FirstOrDefault(p => 
                        p.PortType == PortType.CustomInput && p.DisplayName == portName);
                    
                    if (existingPort != null)
                    {
                        m_InputPorts.Add(existingPort);
                    }
                    else
                    {
                        var customPort = new VisualFlowPort(this, PortType.CustomInput, portName);
                        m_InputPorts.Add(customPort);
                    }
                }
            }
        }

        /// <summary>
        ///  <para>刷新节点自定义输出端口</para>
        /// </summary>
        private void RefreshCustomOutputPorts(Type nodeType)
        {
            var fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var outputAttr = field.GetCustomAttribute<GameFlowOutputAttribute>();
                if (outputAttr != null && field.FieldType.IsAssignableFrom(typeof(IGameFlowNode)))
                {
                    var portName = outputAttr.DisplayName ?? field.Name;
                    var customPort = new VisualFlowPort(this, PortType.CustomOutput, portName);
                    m_OutputPorts.Add(customPort);
                }
            }
        }

        /// <summary>
        ///  <para>恢复端口的连接ID</para>
        /// </summary>
        private void RestoreConnectionIDs(Dictionary<string, List<string>> oldConnections)
        {
            foreach (var port in m_InputPorts.Concat(m_OutputPorts))
            {
                if (port != null && oldConnections.TryGetValue(port.PortID, out var connectionIDs))
                {
                    port.ConnectionIDs.Clear();
                    port.ConnectionIDs.AddRange(connectionIDs);
                }
            }
        }

        /// <summary>
        ///  <para>刷新组合节点的端口</para>
        /// </summary>
        private void RefreshCompositePorts(ICompositeGameFlowNode compositeNode, Dictionary<int, List<string>> childConnections)
        {
            var children = compositeNode.Children?.ToList() ?? new List<IGameFlowNode>();
            
            for (int i = 0; i < children.Count; i++)
            {
                var childPort = new VisualFlowPort(this, PortType.Child, $"Child_{i}");
                
                if (childConnections.TryGetValue(i, out var connections))
                {
                    childPort.ConnectionIDs.Clear();
                    childPort.ConnectionIDs.AddRange(connections);
                }
                
                m_OutputPorts.Add(childPort);
            }
        }
        
        /// <summary>
        ///  <para>更新节点大小</para>
        /// </summary>
        private void UpdateNodeSize()
        {
            float portCount = Mathf.Max(m_InputPorts.Count, m_OutputPorts.Count);
            float portHeight = portCount * PORT_HEIGHT;
            float buttonAreaHeight = 0f;
    
            if (m_WrappedNode is ICompositeGameFlowNode)
            {
                buttonAreaHeight = BUTTON_AREA_HEIGHT;
            }
    
            m_NodeSize = new Vector2(DEFAULT_NODE_WIDTH, DEFAULT_NODE_HEIGHT + portHeight + buttonAreaHeight);
        }

        /// <summary>
        ///  <para>添加子节点到复合节点</para>
        /// </summary>
        public void AddChildToComposite()
        {
            if (m_WrappedNode is ICompositeGameFlowNode compositeNode)
            {
                compositeNode.Append(new PlaceholderGameFlowNode());
                RefreshPorts();
            }
        }

        /// <summary>
        ///  <para>从复合节点移除子节点</para>
        /// </summary>
        public void RemoveChildFromComposite()
        {
            if (m_WrappedNode is ICompositeGameFlowNode compositeNode)
            {
                var children = compositeNode.Children?.ToList() ?? new List<IGameFlowNode>();
                if (children.Count > 0)
                {
                    compositeNode.RemoveAt(children.Count - 1);
                    RefreshPorts();
                }
            }
        }
        
        /// <summary>
        ///  <para>获取子节点数量</para>
        /// </summary>
        public int GetChildCount()
        {
            if (m_WrappedNode is ICompositeGameFlowNode compositeNode)
            {
                return compositeNode.Children?.Count() ?? 0;
            }
            return 0;
        }
        
        /// <summary>
        ///  <para>重建所有端口的运行时引用</para>
        /// </summary>
        public void RebuildNodeReferences(Dictionary<string, GameFlowNodeWrapper> nodeLookup)
        {
            if (nodeLookup == null)
            {
                Debug.LogError("Node lookup is null");
                return;
            }

            foreach (var port in m_InputPorts)
            {
                port.Node = this;
            }
    
            foreach (var port in m_OutputPorts)
            {
                port.Node = this;
            }

            foreach (var port in m_InputPorts.Concat(m_OutputPorts))
            {
                port.RebuildConnections(nodeLookup);
            }
        }

        /// <summary>
        ///  <para>通过端口ID查找端口</para>
        /// </summary>
        public VisualFlowPort FindPortByID(string portID)
        {
            if (string.IsNullOrEmpty(portID)) 
                return null;

            foreach (var port in m_InputPorts)
            {
                if (port != null && port.PortID == portID)
                    return port;
            }
    
            foreach (var port in m_OutputPorts)
            {
                if (port != null && port.PortID == portID)
                    return port;
            }
    
            return null;
        }
    }
    
    /// <summary>
    ///  <para>占位符节点</para>
    ///  <para>用于组合节点的占位符节点</para>
    /// </summary>
    [Serializable]
    internal class PlaceholderGameFlowNode : GameFlowNode
    {
        public PlaceholderGameFlowNode() : base() { }
        
        protected override System.Threading.Tasks.Task OnExecute(System.Threading.CancellationToken ct = default)
        {
            MarkCompleted();
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}

#endif