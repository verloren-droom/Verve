namespace Verve.Unit
{
    using System;
    
    
    [Obsolete("Unit system is deprecated, please use the GameFeatures system")]
    public class UnitRulesNotInitializeException : Exception
    {
        public UnitRulesNotInitializeException(string name) 
            : base($"Unit Rules '{name}' not initialize") { }
    }
    
    [Obsolete("Unit system is deprecated, please use the GameFeatures system")]
    public class UnitNotFoundException : Exception
    {
        public UnitNotFoundException(string name) 
            : base($"Unit '{name}' not found in loaded assemblies") { }
    }
    
    [Obsolete("Unit system is deprecated, please use the GameFeatures system")]
    public class InvalidUnitTypeException : Exception
    {
        public InvalidUnitTypeException(Type type) 
            : base($"{type.Name} must implement {nameof(ICustomUnit)}") { }
    }
    
    [Obsolete("Unit system is deprecated, please use the GameFeatures system")]
    public class UnitDependencyNotFoundException : Exception
    {
        public UnitDependencyNotFoundException(string unitName, string dependName) 
            : base($"Unit '{unitName}' not found {dependName} unit") { }
    }
}