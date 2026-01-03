namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>可视化连接</para>
    /// </summary>
    [Serializable]
    public class VisualFlowConnection
    {
        [NonSerialized] private VisualFlowPort m_InputPort;
        [NonSerialized] private VisualFlowPort m_OutputPort;
    
        /// <summary>
        ///   <para>输入端口</para>
        /// </summary>
        public VisualFlowPort InputPort => m_InputPort;
        
        /// <summary>
        ///   <para>输出端口</para>
        /// </summary>
        public VisualFlowPort OutputPort => m_OutputPort;
    
        
        /// <summary>
        ///   <para>可视化连接</para>
        /// </summary>
        /// <param name="inputPort">输入端口</param>
        /// <param name="outputPort">输出端口</param>
        public VisualFlowConnection(VisualFlowPort inputPort, VisualFlowPort outputPort)
        {
            m_InputPort = inputPort;
            m_OutputPort = outputPort;
        }
    
        /// <summary>
        ///   <para>获取连接的另一端端口</para>
        /// </summary>
        /// <param name="port">端口</param>
        /// <returns>
        ///   <para>连接的另一端端口</para>
        /// </returns>
        public VisualFlowPort GetOtherPort(VisualFlowPort port)
        {
            if (port == m_InputPort) return m_OutputPort;
            if (port == m_OutputPort) return m_InputPort;
            return null;
        }
    }
}