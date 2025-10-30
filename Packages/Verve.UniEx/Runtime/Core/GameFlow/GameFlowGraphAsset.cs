#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{ 
    using UnityEngine;
    using System.Linq;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>流程图形资源</para>
    /// </summary>
    [System.Serializable, CreateAssetMenu(fileName = "New GameFlowGraph", menuName = "Verve/GameFlow/GameFlowGraph")]
    public sealed class GameFlowGraphAsset : ScriptableObject
    {
        [SerializeField, Tooltip("节点")] public List<GameFlowNodeWrapper> nodes = new List<GameFlowNodeWrapper>();
        [SerializeField, Tooltip("根节点")] private GameFlowNodeWrapper m_RootNode;

        /// <summary>
        ///   <para>根节点</para>
        /// </summary>
        public GameFlowNodeWrapper RootNode 
        {
            get
            {
                if (m_RootNode == null && nodes != null && nodes.Count > 0)
                {
                    return nodes[0];
                }
                return m_RootNode;
            }
            set => m_RootNode = value;
        }

        private void OnEnable()
        {
            nodes.RemoveAll(node => node == null);
        }

        /// <summary>
        ///   <para>通过节点ID获取节点</para>
        /// </summary>
        /// <param name="nodeID">节点ID</param>
        /// <returns>
        ///   <para>节点包装器实例</para>
        /// </returns>
        public GameFlowNodeWrapper GetNodeByID(string nodeID)
        {
            if (string.IsNullOrEmpty(nodeID) || nodes == null) 
                return null;
                
            return nodes.FirstOrDefault(n => n?.WrappedNode?.NodeID == nodeID);
        }

        /// <summary>
        ///   <para>通过端口ID获取端口</para>
        /// </summary>
        /// <param name="portID">端口ID</param>
        /// <returns>
        ///   <para>端口实例</para>
        /// </returns>
        public VisualFlowPort GetPortByID(string portID)
        {
            if (string.IsNullOrEmpty(portID) || nodes == null) 
                return null;
                
            foreach (var node in nodes)
            {
                if (node == null) continue;
                
                var port = node.FindPortByID(portID);
                if (port != null) 
                    return port;
            }
            
            return null;
        }

        /// <summary>
        ///   <para>重建图形中的所有运行时引用</para>
        /// </summary>
        public void RebuildGraphReferences()
        {
            if (nodes == null) 
            {
                Debug.LogWarning("Graph nodes list is null");
                return;
            }

            var nodeLookup = new Dictionary<string, GameFlowNodeWrapper>();
            foreach (var node in nodes)
            {
                if (node?.WrappedNode != null)
                {
                    var nodeID = node.WrappedNode.NodeID;
                    if (!string.IsNullOrEmpty(nodeID) && !nodeLookup.ContainsKey(nodeID))
                    {
                        nodeLookup[nodeID] = node;
                    }
                }
            }

            foreach (var node in nodes)
            {
                if (node != null)
                {
                    node.RebuildNodeReferences(nodeLookup);
                }
            }
        }

//         private void OnValidate()
//         {
// #if UNITY_EDITOR
//             if (m_Nodes != null)
//             {
//                 foreach (var node in m_Nodes)
//                 {
//                     if (node != null)
//                     {
//                         foreach (var port in node.InputPorts.Concat(node.OutputPorts))
//                         {
//                             port.Node = node;
//                         }
//                     }
//                 }
//             }
// #endif
//         }
    }
}

#endif