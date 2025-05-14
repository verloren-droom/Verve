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
        
        private HashSet<Type> m_DependencyUnits = new HashSet<Type>();
        public virtual HashSet<Type> DependencyUnits { get => m_DependencyUnits; protected set => m_DependencyUnits = value; }

        public virtual void Dispose() { }

        void ICustomUnit.Startup(UnitRules parent, params object[] args) => OnStartup(parent, args);
        void ICustomUnit.Tick(float deltaTime, float unscaledTime) => OnTick(deltaTime, unscaledTime);
        void ICustomUnit.Shutdown()
        {
            Dispose();
            OnShutdown();
        }

        /// <summary>
        /// 单元被启用
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="args"></param>
        protected virtual void OnStartup(UnitRules parent, params object[] args) { }

        /// <summary>
        /// 单元每帧被调用
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="unscaledTime"></param>
        protected virtual void OnTick(float deltaTime, float unscaledTime) { }
        
        /// <summary>
        /// 单元被卸载
        /// </summary>
        protected virtual void OnShutdown() { }
    }
}