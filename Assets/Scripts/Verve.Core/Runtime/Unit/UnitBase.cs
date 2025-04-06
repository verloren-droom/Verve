namespace Verve.Unit
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    
    /// <summary>
    /// 单元基类
    /// </summary>
    [CustomUnit("Base")]
    public abstract partial class UnitBase : ICustomUnit
    {
        public virtual string UnitName => Regex.Replace(GetType().GetCustomAttribute<CustomUnitAttribute>()?.UnitName ?? GetType().Name, "Unit", string.Empty);
        public virtual int Priority => 0;

        private bool m_IsInitialized;
        internal bool IsInitialized => m_IsInitialized;

        private HashSet<Type> m_DependencyUnits = new HashSet<Type>();
        public virtual HashSet<Type> DependencyUnits { get => m_DependencyUnits; protected set => m_DependencyUnits = value; }
        
        public virtual void Startup(UnitRules parent, params object[] args)
        {
            m_IsInitialized = true;
        }

        public virtual void Tick(float deltaTime, float unscaledTime) { }
        
        public virtual void Shutdown() { }
    }
}