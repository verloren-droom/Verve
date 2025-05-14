namespace Verve.Unit
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    
    
    /// <summary>
    /// 单元管理 
    /// </summary>
    [System.Serializable]
    public sealed partial class UnitRules : IDisposable
    {
        private readonly ConcurrentDictionary<string, UnitInfo> m_Units = new ConcurrentDictionary<string, UnitInfo>();
        private bool m_IsInitialized;

        /// <summary>
        /// 初始化完成事件
        /// </summary>
        public event Action<UnitRules> onInitialized;
        /// <summary>
        /// 销毁事件
        /// </summary>
        public event Action onDeinitialized;

        public void Initialize()
        {
            if (m_IsInitialized) return;
            
            foreach (var unitInfo in GetOrderedUnits())
            {
#if UNITY_EDITOR || DEBUG
                // 遍历查找是否存在缺少的依赖未添加
                foreach (var dependencyUnit in unitInfo.Instance.GetType().GetCustomAttribute<CustomUnitAttribute>()?.DependencyUnits)
                {
                    var (name, _) = GetModuleMetadata(dependencyUnit);
                    if (!m_Units.ContainsKey(name))
                    {
                        throw new UnitDependencyNotFoundException(unitInfo.Instance.UnitName, name);
                    }
                }
#endif
                unitInfo.Instance.Startup(this, unitInfo.StartupArgs);
            }
            m_IsInitialized = true;
            onInitialized?.Invoke(this);
        }

        public void Update(float deltaTime, float unscaledTime)
        {
            if (m_IsInitialized || !m_Units.Any()) return;
            
            foreach (var unitInfo in GetOrderedUnits())
            {
                unitInfo.Instance.Tick(deltaTime, unscaledTime);
            }
        }

        public void DeInitialize()
        {
            if (!m_IsInitialized) return;
            
            foreach (var unitInfo in GetOrderedUnits().Reverse())
            {
                unitInfo.Instance.Shutdown();
            }
            m_Units.Clear();
            m_IsInitialized = false;
            onDeinitialized?.Invoke();
        }
    
        public void Dispose()
        {
            if (!m_IsInitialized) return;
            foreach (var unitInfo in GetOrderedUnits())
            {
                unitInfo.Instance.Dispose();
            }
        }
        
        internal UnitRules AddDependency(Type unitType, params object[] startupArgs)
        {
            if (!typeof(ICustomUnit).IsAssignableFrom(unitType))
                throw new InvalidUnitTypeException(unitType);
            var (unitName, priority) = GetModuleMetadata(unitType);
            if (m_Units.ContainsKey(unitName)) return this;
            m_Units.TryAdd(unitName, new UnitInfo
            {
                Priority = priority,
                StartupArgs = startupArgs,
                Instance = (ICustomUnit)Activator.CreateInstance(unitType)
            });

            return this;
        }

        public UnitRules AddDependency<TUnit>(params object[] startupArgs)
            where TUnit : ICustomUnit => AddDependency(typeof(TUnit), startupArgs);

        public UnitRules AddDependency(string unitName, params object[] startupArgs) => AddDependency(FindUnitTypeByName(unitName), startupArgs);

        public bool TryGetDependency<TUnit>(out TUnit unit) where TUnit : ICustomUnit
        {
            if (!m_IsInitialized)
            {
                throw new UnitRulesNotInitializeException(GetType().Name);
            }
            var (moduleName, _) = GetModuleMetadata(typeof(TUnit));
            unit = m_Units.TryGetValue(moduleName, out var info) ? (TUnit)info.Instance : default;
            return unit != null;
        }
        
        public bool TryGetDependency(System.Type unitType, out ICustomUnit unit)
        {
            if (!m_IsInitialized)
            {
                throw new UnitRulesNotInitializeException(GetType().Name);
            }
            var (moduleName, _) = GetModuleMetadata(unitType);
            unit = m_Units.TryGetValue(moduleName, out var info) ? info.Instance : null;
            return unit != null;
        }

        private IEnumerable<UnitInfo> GetOrderedUnits()
        {
            return m_Units.Values.OrderBy(m => m.Priority).Reverse();
        }
    
        private Type FindUnitTypeByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(a => a.GetTypes())
                       .FirstOrDefault(t => 
                           t.GetCustomAttribute<CustomUnitAttribute>()?.UnitName == name &&
                           typeof(ICustomUnit).IsAssignableFrom(t)) 
                   ?? throw new UnitNotFoundException(name);
        }
    
        private (string name, int priority) GetModuleMetadata(Type unitType)
        {
            var attr = unitType.GetCustomAttribute<CustomUnitAttribute>();
            return (attr?.UnitName ?? unitType.Name, attr?.Priority ?? 0);
        }

        private struct UnitInfo
        {
            public int Priority { get; set; }
            public object[] StartupArgs { get; set; }
            public ICustomUnit Instance { get; set; }
        }
    }
}