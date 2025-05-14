namespace Verve.ECS
{
    using System;

    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ECSSystemAttribute : Attribute
    {
        public Type SystemType { get; }
        public int Order { get; }
        
        public ECSSystemAttribute(Type systemType, int order = 0)
        {
            if (!typeof(SystemBase).IsAssignableFrom(systemType))
                throw new ArgumentException($"{systemType.Name} is not a system");
            SystemType = systemType;
            Order = order;
        }
    }
}