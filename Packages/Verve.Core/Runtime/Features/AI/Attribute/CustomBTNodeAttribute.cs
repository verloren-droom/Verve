namespace Verve.AI
{
    using System;
    

    /// <summary>
    ///   <para>自定义行为树节点属性</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class CustomBTNodeAttribute : Attribute
    {
        /// <summary>
        ///   <para>节点名称</para>
        /// </summary>
        public string NodeName { get; }
        
        
        public CustomBTNodeAttribute(string nodeName)
        {
            if (!typeof(IBTNode).IsAssignableFrom(GetType().DeclaringType))
            {
                throw new InvalidOperationException($"{nameof(CustomBTNodeAttribute)} can only be applied to structs implementing {nameof(IBTNode)} interface");
            }
            
            NodeName = nodeName;
        }
    }
}