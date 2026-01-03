namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>游戏流程输入节点特性</para>
    ///   <para>用于标记游戏流程输入节点</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class GameFlowInputAttribute : Attribute
    {
        /// <summary>
        ///   <para>显示名</para>
        /// </summary>
        public string DisplayName { get; }


        public GameFlowInputAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}