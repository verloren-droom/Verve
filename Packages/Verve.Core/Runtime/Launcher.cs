namespace Verve
{
    using Unit;
    using System;


    public static partial class Launcher
    {
        private static UnitRules m_UnitRules = new UnitRules();
        
        
        // private Launcher() { }
        
        public static void Initialize()
        {
            m_UnitRules.Initialize();
        }
        
        public static bool IsDebug { get; set; }
        
        public static bool TryGetUnit<TUnit>(out TUnit module) where TUnit : UnitBase, ICustomUnit => m_UnitRules.TryGetDependency<TUnit>(out module);
        public static bool TryGetUnit(Type unitType, out ICustomUnit module) => m_UnitRules.TryGetDependency(unitType, out module);

        public static TUnit GetUnit<TUnit>() where TUnit : UnitBase, ICustomUnit
        {
            return TryGetUnit<TUnit>(out var module)
                ? module
                : throw new System.Exception($"{typeof(TUnit)} is not registered.");
        }
        
        public static ICustomUnit GetUnit(Type unitType)
        {
            return TryGetUnit(unitType, out var module)
                ? module
                : throw new System.Exception($"{unitType} is not registered.");
        }
        
        public static void AddUnit<TUnit>(params object[] args) where TUnit : UnitBase, ICustomUnit => m_UnitRules.AddDependency<TUnit>(args);
        public static void AddUnit(string moduleName, params object[] args) => m_UnitRules.AddDependency(moduleName, args);
        
        public static void Update(float deltaTime, float unscaledTime)
        {
            m_UnitRules.Update(deltaTime, unscaledTime);
        }
    }
}