#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    
    
    /// <summary>
    ///   <para>游戏流程输出节点特性</para>
    ///   <para>用于标记游戏流程输出节点</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class GameFlowOutputAttribute : Attribute
    {
        /// <summary>
        ///   <para>显示名</para>
        /// </summary>
        public string DisplayName { get; }


        public GameFlowOutputAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}

#endif