#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    
    
    /// <summary>
    ///  <para>游戏流程输出节点特性</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class GameFlowOutputAttribute : Attribute
    {
        /// <summary>
        ///  <para>显示名</para>
        /// </summary>
        public string DisplayName { get; }
        
        /// <summary>
        ///  <para>节点描述</para>
        /// </summary>
        public string Description { get; }
        
        
        public GameFlowOutputAttribute(string displayName = null, string description = "")
        {
            DisplayName = displayName;
            Description = description;
        }
    }
}

#endif