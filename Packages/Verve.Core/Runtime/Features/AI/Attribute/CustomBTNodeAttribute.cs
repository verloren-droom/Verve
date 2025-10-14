namespace Verve.AI
{
    using System;
    

    /// <summary>
    /// 自定义行为树节点属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class CustomBTNodeAttribute : Attribute
    {
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