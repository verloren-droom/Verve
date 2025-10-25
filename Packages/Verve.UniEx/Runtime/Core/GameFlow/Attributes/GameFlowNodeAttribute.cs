#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    
    
    /// <summary>
    ///  <para>游戏流程节点特性<</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class GameFlowNodeAttribute : Attribute
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