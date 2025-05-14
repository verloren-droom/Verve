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
        public virtual int Priority => GetType().GetCustomAttribute<CustomUnitAttribute>()?.Priority ?? 0;

        void ICustomUnit.Startup(UnitRules parent, params object[] args) => OnStartup(parent, args);
        void ICustomUnit.Tick(float deltaTime, float unscaledTime) => OnTick(deltaTime, unscaledTime);
        void ICustomUnit.Shutdown()
        {
            Dispose();
            OnShutdown();
        }
        
        public virtual void Dispose() { }

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

    
    public abstract partial class UnitBase<TUnitService> : UnitBase where TUnitService : IUnitService
    {
        private readonly Dictionary<Type, TUnitService> m_UnitServices = new Dictionary<Type, TUnitService>();

        
        protected TUnitService Resolve(Type type)
        {
            return m_UnitServices.TryGetValue(type, out var factory) ? factory : default;
        }
        
        protected T Resolve<T>() where T : class, IUnitService
        {
            return m_UnitServices.TryGetValue(typeof(T), out var factory) ? factory as T : null;
        }
        
        protected void Register(Func<TUnitService> factory)
        {
            if (factory != null || !m_UnitServices.ContainsKey(typeof(TUnitService)))
            {
                m_UnitServices[typeof(TUnitService)] = factory.Invoke();
            }
        }
        
        protected void Register(TUnitService factory)
        {
            if (factory != null || !m_UnitServices.ContainsKey(typeof(TUnitService)))
            {
                m_UnitServices[typeof(TUnitService)] = factory;
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
            m_UnitServices.Clear();
        }
        
        protected override void OnShutdown()
        {
            base.OnShutdown();
            Dispose();
        }
    }

    
    public interface IUnitService
    {
        
    }
    
}