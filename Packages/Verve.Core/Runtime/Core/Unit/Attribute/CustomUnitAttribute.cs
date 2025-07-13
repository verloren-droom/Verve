namespace Verve.Unit
{
    using System;
    using System.Linq;
 
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false), Obsolete("Unit system is deprecated, please use the GameFeatures system")]
    public sealed class CustomUnitAttribute : Attribute
    {
        public string UnitName { get; }
        public int Priority { get; }
        public Type[] DependencyUnits { get; }
        
        
        /// <param name="name">单元名</param>
        /// <param name="priority">单元加载优先级</param>
        /// <param name="dependencyUnits">依赖单元，仅做提示作用</param>
        public CustomUnitAttribute(string name, int priority = 0, params Type[] dependencyUnits)
        {
            UnitName = name;
            Priority = priority;
            DependencyUnits = (dependencyUnits ?? new Type[] {}).Distinct().ToArray();
        }
    }
}