namespace Verve.Unit
{
    using System;
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CustomUnitAttribute : Attribute
    {
        public string UnitName { get; }
        public int Priority { get; }
        
        public CustomUnitAttribute(string name, int priority = 0)
        {
            UnitName = name;
            Priority = priority;
        }
    }
}