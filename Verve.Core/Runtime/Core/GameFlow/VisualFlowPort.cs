#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>可视化端口</para>
    /// </summary>
    [Serializable]
    public class VisualFlowPort
    {
        /// <summary>
        ///   <para>端口方向</para>
        /// </summary>
        [Serializable]
        public enum Direction { Input, Output }
        
        [SerializeField, Tooltip("端口ID")] private string m_PortID;
        [SerializeField, Tooltip("端口显示名称")] private string m_DisplayName;
        [SerializeField, Tooltip("端口类型")] private GameFlowNodeWrapper.PortType m_PortType;
        [SerializeField, Tooltip("端口方向")] private Direction m_PortDirection;
        [SerializeField, Tooltip("允许多个连接")] private bool m_AllowMultipleConnections;

        [SerializeField] private List<string> m_ConnectionIDs = new List<string>();

        [NonSerialized] private GameFlowNodeWrapper m_Node;
        [NonSerialized] private List<VisualFlowConnection> m_Connections;

        /// <summary>
        ///   <para>节点</para>
        /// </summary>
        public GameFlowNodeWrapper Node 
        { 
            get => m_Node;
            set => m_Node = value;
        }
        
        /// <summary>
        ///   <para>端口ID</para>
        /// </summary>
        public string PortID => m_PortID ??= $"port_{Guid.NewGuid().ToString("N")[..32]}";
        
        /// <summary>
        ///   <para>端口显示名称</para>
        /// </summary>
        public string DisplayName => m_DisplayName;
        
        /// <summary>
        ///   <para>端口类型</para>
        /// </summary>
        public GameFlowNodeWrapper.PortType PortType => m_PortType;
        
        /// <summary>
        ///   <para>端口方向</para>
        /// </summary>
        public Direction PortDirection => m_PortDirection;
        
        /// <summary>
        ///   <para>是否允许多个连接</para>
        /// </summary>
        public bool AllowMultipleConnections => m_AllowMultipleConnections;
        
        /// <summary>
        ///   <para>连接ID列表</para>
        /// </summary>
        public List<string> ConnectionIDs => m_ConnectionIDs;
        
        /// <summary>
        ///   <para>可读连接列表</para>
        /// </summary>
        public IReadOnlyList<VisualFlowConnection> Connections 
        { 
            get 
            {
                if (m_Connections == null)
                {
                    m_Connections = new List<VisualFlowConnection>();
                }
                return m_Connections;
            }
        }
        
        /// <summary>
        ///   <para>构造函数</para>
        /// </summary>
        /// <param name="node">节点</param>
        /// <param name="portType">端口类型</param>
        /// <param name="displayName">显示名称</param>
        public VisualFlowPort(GameFlowNodeWrapper node, GameFlowNodeWrapper.PortType portType, string displayName)
        {
            m_Node = node;
            m_PortType = portType;
            m_DisplayName = displayName;
            
            if (portType == GameFlowNodeWrapper.PortType.In
                || portType == GameFlowNodeWrapper.PortType.CustomInput)
            {
                m_PortDirection = Direction.Input;
                m_AllowMultipleConnections = true;
            } 
            else if (portType == GameFlowNodeWrapper.PortType.CustomOutput
                       || portType == GameFlowNodeWrapper.PortType.Child)
            {
                m_PortDirection = Direction.Output;
                m_AllowMultipleConnections = false;
            }
        }

        /// <summary>
        ///   <para>断开所有连接</para>
        /// </summary>
        public void DisconnectAll()
        {
            if (m_Connections != null)
            {
                foreach (var connection in m_Connections.ToList())
                {
                    Disconnect(connection);
                }
            }
            else
            {
#if UNITY_EDITOR
                foreach (var connectedPortID in m_ConnectionIDs.ToList())
                {
                    var graph = UnityEditor.AssetDatabase.LoadAssetAtPath<GameFlowGraphAsset>(
                        UnityEditor.AssetDatabase.GetAssetPath(m_Node));
                    var targetPort = graph?.GetPortByID(connectedPortID);
                    if (targetPort != null)
                    {
                        targetPort.m_ConnectionIDs.Remove(PortID);
                    }
                }
#endif
                m_ConnectionIDs.Clear();
                ClearConnectionAssignment();
            }
        }

        /// <summary>
        ///   <para>获取连接的另一端端口</para>
        /// </summary>
        /// <param name="connection">连接</param>
        /// <returns>
        ///   <para>连接的另一端端口</para>
        /// </returns>
        public VisualFlowPort GetOtherPort(VisualFlowConnection connection)
        {
            return connection?.GetOtherPort(this);
        }

        /// <summary>
        ///   <para>立即应用连接</para>
        /// </summary>
        private void ApplyImmediateConnection()
        {
            if (PortDirection != Direction.Output || Connections.Count == 0)
                return;

            try
            {
                var connection = Connections[0];
                var inputPort = connection.InputPort;
        
                if (inputPort?.Node?.WrappedNode == null) return;
        
                var outputNode = Node.WrappedNode;
                var inputNode = inputPort.Node.WrappedNode;
        
                SetNodeField(outputNode, inputNode);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to apply immediate connection: {ex.Message}");
            }
        }

        /// <summary>
        ///   <para>清除连接的赋值</para>
        /// </summary>
        private void ClearConnectionAssignment()
        {
            if (PortDirection == Direction.Output && Node?.WrappedNode != null)
            {
                try
                {
                    ClearNodeField(Node.WrappedNode);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to clear connection assignment: {ex.Message}");
                }
            }
        }

        /// <summary>
        ///   <para>设置节点字段</para>
        /// </summary>
        /// <param name="outputNode">输出节点</param>
        /// <param name="inputNode">输入节点</param>
        private void SetNodeField(IGameFlowNode outputNode, IGameFlowNode inputNode)
        {
            if (outputNode == null || inputNode == null) return;
            
            var nodeType = outputNode.GetType();
            
            if (PortType == GameFlowNodeWrapper.PortType.Child && outputNode is ICompositeGameFlowNode compositeNode)
            {
                HandleCompositeNodeConnection(compositeNode, inputNode);
                return;
            }
            
            if (PortDirection == Direction.Output)
            {
                SetOutputField(nodeType, outputNode, inputNode);
            }
        }
        
        /// <summary>
        ///   <para>获取子节点端口索引</para>
        /// </summary>
        /// <returns>
        ///   <para>子节点端口索引</para>
        /// </returns>
        private int GetChildPortIndex()
        {
            if (PortType != GameFlowNodeWrapper.PortType.Child) return -1;
            
            var childPorts = Node.OutputPorts
                .Where(p => p.PortType == GameFlowNodeWrapper.PortType.Child)
                .OrderBy(p => p.DisplayName)
                .ToList();
            
            return childPorts.IndexOf(this);
        }
        
        /// <summary>
        ///   <para>处理复合节点连接</para>
        /// </summary>
        /// <param name="compositeNode">复合节点</param>
        /// <param name="inputNode">输入节点</param>
        private void HandleCompositeNodeConnection(ICompositeGameFlowNode compositeNode, IGameFlowNode inputNode)
        {
            var index = GetChildPortIndex();
            if (index < 0) return;
            
            var children = compositeNode.Children?.ToList() ?? new List<IGameFlowNode>();
            
            while (children.Count <= index)
            {
                compositeNode.Append(new PlaceholderGameFlowNode());
                children = compositeNode.Children?.ToList() ?? new List<IGameFlowNode>();
            }
            
            var oldChild = children[index];
            if (oldChild != null)
            {
                compositeNode.Remove(oldChild);
            }
            compositeNode.Insert(inputNode, index);
        }
        
        /// <summary>
        ///   <para>设置输出字段</para>
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="outputNode">输出节点</param>
        /// <param name="inputNode">输入节点</param>
        private void SetOutputField(Type nodeType, IGameFlowNode outputNode, IGameFlowNode inputNode)
        {
            var fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var outputAttr = field.GetCustomAttribute<GameFlowOutputAttribute>();
                if (outputAttr != null && field.FieldType.IsAssignableFrom(typeof(IGameFlowNode)))
                {
                    var displayName = outputAttr.DisplayName ?? field.Name;
                    if (displayName == DisplayName)
                    {
                        try
                        {
                            field.SetValue(outputNode, inputNode);
                            return;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to set field {field.Name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   <para>清除节点字段</para>
        /// </summary>
        /// <param name="outputNode">输出节点</param>
        private void ClearNodeField(IGameFlowNode outputNode)
        {
            var nodeType = outputNode.GetType();
            var fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (PortType == GameFlowNodeWrapper.PortType.Child && outputNode is ICompositeGameFlowNode compositeNode)
            {
                var index = GetChildPortIndex();
                if (index >= 0)
                {
                    var children = compositeNode.Children?.ToList() ?? new List<IGameFlowNode>();
                    if (index < children.Count)
                    {
                        compositeNode.RemoveAt(index);
                    }
                }
                return;
            }
            
            foreach (var field in fields)
            {
                var outputAttr = field.GetCustomAttribute<GameFlowOutputAttribute>();
                if (outputAttr != null && field.FieldType.IsAssignableFrom(typeof(IGameFlowNode)))
                {
                    var displayName = outputAttr.DisplayName ?? field.Name;
                    if (displayName == DisplayName)
                    {
                        field.SetValue(outputNode, null);
                        return;
                    }
                }
            }
        }

        /// <summary>
        ///   <para>是否可连接</para>
        /// </summary>
        /// <param name="targetPort">目标端口</param>
        /// <returns>
        ///   <para>是否可连接</para>
        /// </returns>
        public bool CanConnectTo(VisualFlowPort targetPort)
        {
            if (targetPort == null || Node == null || targetPort.Node == null) 
            {
                return false;
            }

            if (PortDirection == targetPort.PortDirection)
            {
                return false;
            }
            
            if (Node == targetPort.Node)
            {
                return false;
            }
            
            if (!AllowMultipleConnections && m_ConnectionIDs.Count > 0)
            {
                return false;
            }
            
            if (!targetPort.AllowMultipleConnections && targetPort.m_ConnectionIDs.Count > 0)
            {
                return false;
            }
            
            if (m_ConnectionIDs.Contains(targetPort.PortID))
            {
                return false;
            }
            
            if (PortType == GameFlowNodeWrapper.PortType.Child)
            {
                return targetPort.PortDirection == Direction.Input;
            }
            
            return true;
        }
        
        /// <summary>
        ///   <para>重建连接</para>
        /// </summary>
        /// <param name="nodeLookup">节点对照表</param>
        public void RebuildConnections(Dictionary<string, GameFlowNodeWrapper> nodeLookup)
        {
            if (m_Connections == null)
            {
                m_Connections = new List<VisualFlowConnection>();
            }
            else
            {
                m_Connections.Clear();
            }
            
            if (m_ConnectionIDs == null || nodeLookup == null) 
                return;
        
            foreach (var connectedPortID in m_ConnectionIDs)
            {
                if (string.IsNullOrEmpty(connectedPortID)) continue;
                
                VisualFlowPort targetPort = null;
                foreach (var nodeEntry in nodeLookup)
                {
                    var node = nodeEntry.Value;
                    if (node == null) continue;
                    
                    var foundPort = node.FindPortByID(connectedPortID);
                    if (foundPort != null)
                    {
                        targetPort = foundPort;
                        break;
                    }
                }
                
                if (targetPort == null) { continue; }
                
                // 检查是否已经存在相同的连接（避免重复创建）
                bool connectionExists = false;
                if (m_Connections != null)
                {
                    connectionExists = m_Connections.Any(conn => 
                        (conn.InputPort == this && conn.OutputPort == targetPort) ||
                        (conn.InputPort == targetPort && conn.OutputPort == this));
                }
                
                if (!connectionExists)
                {
                    VisualFlowConnection connection;
                    if (PortDirection == Direction.Input)
                    {
                        connection = new VisualFlowConnection(this, targetPort);
                    }
                    else
                    {
                        connection = new VisualFlowConnection(targetPort, this);
                    }
                    
                    m_Connections.Add(connection);
                    
                    if (targetPort.m_Connections == null)
                    {
                        targetPort.m_Connections = new List<VisualFlowConnection>();
                    }
                    
                    // 同样检查目标端口是否已存在该连接
                    bool targetConnectionExists = targetPort.m_Connections.Any(conn => 
                        (conn.InputPort == this && conn.OutputPort == targetPort) ||
                        (conn.InputPort == targetPort && conn.OutputPort == this));
                    
                    if (!targetConnectionExists)
                    {
                        targetPort.m_Connections.Add(connection);
                    }
                    
                    ApplyImmediateConnection();
                }
            }
        }
        
        /// <summary>
        ///   <para>连接到目标端口</para>
        /// </summary>
        /// <param name="targetPort">目标端口</param>
        public void ConnectTo(VisualFlowPort targetPort)
        {
            if (!CanConnectTo(targetPort)) 
            {
                Debug.LogError($"Cannot connect {Node?.NodeName}.{DisplayName} to {targetPort.Node?.NodeName}.{targetPort.DisplayName}");
                return;
            }
        
            // 检查是否已经连接（避免重复连接）
            bool alreadyConnected = m_ConnectionIDs.Contains(targetPort.PortID) && 
                                   targetPort.m_ConnectionIDs.Contains(PortID);
            
            if (!alreadyConnected)
            {
                if (!AllowMultipleConnections)
                {
                    DisconnectAll();
                }
                
                if (!targetPort.AllowMultipleConnections)
                {
                    targetPort.DisconnectAll();
                }
        
                if (!m_ConnectionIDs.Contains(targetPort.PortID))
                {
                    m_ConnectionIDs.Add(targetPort.PortID);
                }
                
                if (!targetPort.m_ConnectionIDs.Contains(PortID))
                {
                    targetPort.m_ConnectionIDs.Add(PortID);
                }
            }
        
            // 确保连接对象存在（但避免重复创建）
            if (m_Connections == null)
            {
                m_Connections = new List<VisualFlowConnection>();
            }
            
            // 检查是否已存在连接对象
            bool connectionObjectExists = m_Connections.Any(conn => 
                conn.InputPort == (PortDirection == Direction.Input ? this : targetPort) &&
                conn.OutputPort == (PortDirection == Direction.Input ? targetPort : this));
            
            if (!connectionObjectExists)
            {
                VisualFlowConnection connection;
                if (PortDirection == Direction.Input)
                {
                    connection = new VisualFlowConnection(this, targetPort);
                }
                else
                {
                    connection = new VisualFlowConnection(targetPort, this);
                }
        
                m_Connections.Add(connection);
                
                if (targetPort.m_Connections == null)
                {
                    targetPort.m_Connections = new List<VisualFlowConnection>();
                }
                
                // 同样检查目标端口是否已存在连接对象
                bool targetConnectionObjectExists = targetPort.m_Connections.Any(conn => 
                    conn.InputPort == (PortDirection == Direction.Input ? this : targetPort) &&
                    conn.OutputPort == (PortDirection == Direction.Input ? targetPort : this));
                
                if (!targetConnectionObjectExists)
                {
                    targetPort.m_Connections.Add(connection);
                }
            }
        
            ApplyImmediateConnection();
        }
        
        /// <summary>
        ///   <para>断开连接</para>
        /// </summary>
        /// <param name="connection">要断开的连接对象</param>
        public void Disconnect(VisualFlowConnection connection)
        {
            if (connection == null) return;
            
            var otherPort = connection.GetOtherPort(this);
            if (otherPort != null)
            {
                // 只有在确实存在连接时才移除连接ID
                if (m_ConnectionIDs.Contains(otherPort.PortID))
                {
                    m_ConnectionIDs.Remove(otherPort.PortID);
                }
                
                if (otherPort.m_ConnectionIDs.Contains(PortID))
                {
                    otherPort.m_ConnectionIDs.Remove(PortID);
                }
                
                if (otherPort.m_Connections != null)
                {
                    otherPort.m_Connections.Remove(connection);
                }
                
                ClearConnectionAssignment();
            }
            
            if (m_Connections != null)
            {
                m_Connections.Remove(connection);
            }
        }
    }
}

#endif