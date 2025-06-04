namespace Verve.Unit
{
    using System;
    
    
    public class UnitRulesNotInitializeException : Exception
    {
        public UnitRulesNotInitializeException(string name) 
            : base($"Unit Rules '{name}' not initialize") { }
    }
    
    public class UnitNotFoundException : Exception
    {
        public UnitNotFoundException(string name) 
            : base($"Unit '{name}' not found in loaded assemblies") { }
    }

    public class InvalidUnitTypeException : Exception
    {
        public InvalidUnitTypeException(Type type) 
            : base($"{type.Name} must implement {nameof(ICustomUnit)}") { }
    }
    
    public class UnitDependencyNotFoundException : Exception
    {
        public UnitDependencyNotFoundException(string unitName, string dependName) 
            : base($"Unit '{unitName}' not found {dependName} unit") { }
    }
}