namespace Verve.ECS
{
    using System;
    
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class RequiredComponentImplAttribute : Attribute
    {
        public Type[] RequiredComponents { get; }

        public RequiredComponentImplAttribute(params Type[] compTypes)
        {
            foreach (var type in compTypes)
            {
                if (!typeof(IComponentBase).IsAssignableFrom(type))
                    throw new ArgumentException($"{type} is not a component");
            }
            RequiredComponents = compTypes;
        }
    }
}