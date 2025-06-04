namespace Verve.Unit
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    
    /// <summary>
    /// 单元基类
    /// </summary>
    public abstract partial class UnitBase : ICustomUnit
    {
        public virtual string UnitName => Regex.Replace(GetType().GetCustomAttribute<CustomUnitAttribute>()?.UnitName ?? GetType().Name, "Unit", string.Empty);
        public virtual int Priority => GetType().GetCustomAttribute<CustomUnitAttribute>()?.Priority ?? 0;

        public virtual bool CanEverTick { get; protected set; }
        
        void ICustomUnit.Startup(UnitRules parent, params object[] args)
        {
            OnStartup(args);
            parent.OnInitialized += OnPostStartup;
            parent.OnDeinitialized += OnPostShutdown;
        }
        void ICustomUnit.Tick(float deltaTime, float unscaledTime)
        {
            // if (!CanEverTick) return;
            OnTick(deltaTime, unscaledTime);
        }
        void ICustomUnit.Shutdown()
        {
            Dispose();
            OnShutdown();
        }
        
        public virtual void Dispose() { }

        /// <summary>
        /// 单元被启用
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnStartup(params object[] args) { }

        /// <summary>
        /// 单元被初始化后被调用
        /// </summary>
        /// <param name="parent"></param>
        protected virtual void OnPostStartup(UnitRules parent)
        {
            parent.OnInitialized -= OnPostStartup;
        }

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

        /// <summary>
        /// 单元被卸载后被调用
        /// </summary>
        protected virtual void OnPostShutdown(UnitRules parent)
        {
            parent.OnDeinitialized -= OnPostShutdown;
        }
    }

    
    public abstract partial class UnitBase<TUnitService> : UnitBase where TUnitService : IUnitService
    {
        protected readonly Dictionary<Type, TUnitService> m_UnitServices = new Dictionary<Type, TUnitService>();

        
        protected TUnitService GetService(Type type)
        {
            if (!typeof(TUnitService).IsAssignableFrom(type))
            {
                throw new Exception($"{type.Name} is not a {typeof(TUnitService).Name}");
            }
            return m_UnitServices.TryGetValue(type, out var factory) ? factory : default;
        }
        
        protected T GetService<T>() where T : class, IUnitService
        {
            return m_UnitServices.TryGetValue(typeof(T), out var factory) ? factory as T : null;
        }
        
        protected void AddService<T>() where T : class, IUnitService
        {
            if (!m_UnitServices.ContainsKey(typeof(T)))
            {
                m_UnitServices[typeof(T)] = (TUnitService)Activator.CreateInstance(typeof(T));
            }
        }

        protected void AddService(TUnitService factory)
        {
            if (factory != null || !m_UnitServices.ContainsKey(factory.GetType()))
            {
                m_UnitServices[factory.GetType()] = factory;
            }
        }
        
        protected void RemoveService<T>() where T : class, IUnitService
        {
            if (m_UnitServices.ContainsKey(typeof(T)))
            {
                m_UnitServices.Remove(typeof(T));
            }
        }
        
        protected void RemoveService(TUnitService factory)
        {
            if (factory != null && m_UnitServices.ContainsKey(factory.GetType()))
            {
                m_UnitServices.Remove(factory.GetType());
            }
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            m_UnitServices.Clear();
        }
    }

    
    public interface IUnitService
    {
        
    }
}