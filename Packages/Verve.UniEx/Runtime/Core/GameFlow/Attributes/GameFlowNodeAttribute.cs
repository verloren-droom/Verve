#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    
    
    /// <summary>
    ///  <para>游戏流程节点特性</para>
    ///  <para>用于将节点暴露到可视化流程图中，注意节点类一定要存在一个无参构造方法</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GameFlowNodeAttribute : Attribute
    {
        /// <summary>
        ///  <para>节点路径</para>
        /// </summary>
        public string MenuPath { get; }
        
        /// <summary>
        ///  <para>节点名</para>
        /// </summary>
        public string NodeName { get; }
        
        /// <summary>
        ///  <para>节点描述</para>
        /// </summary>
        public string Description { get; }

        
        public GameFlowNodeAttribute(string menuPath, string name = null, string description = "")
        {
            MenuPath = menuPath;
            NodeName = name;
            Description = description;
        }
    }
}

#endif