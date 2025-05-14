namespace Verve.ECS
{
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;


    /// <summary>
    /// ECS系统
    /// </summary>
    [System.Serializable]
    public abstract class SystemBase
    { 
        private int m_Order;
        public int Order => m_Order;
        
        private EntityQuery m_Entities;
        
        public EntityQuery Entities => m_Entities;
        
        private bool m_IsCreated;

        protected SystemBase()
        {
            m_Entities = new EntityQuery(Entity.GetAll());
            m_Order = GetType().GetCustomAttribute<ECSSystemAttribute>()?.Order ?? 0;
        }

        ~SystemBase() => Destroy();

        public void Create()
        {
            if (m_IsCreated) return;
            OnCreate();
            m_IsCreated = true;
        }
        
        public void Update() => OnUpdate();

        public void Destroy()
        {
            if (!m_IsCreated) return;
            OnDestroy();
            m_IsCreated = false;
        }
        
        /// <summary>
        /// ECS系统创建
        /// </summary>
        protected virtual void OnCreate() { }
        /// <summary>
        /// ECS系统更新
        /// </summary>
        protected virtual void OnUpdate() { }
        /// <summary>
        /// ECS系统销毁
        /// </summary>
        protected virtual void OnDestroy() { }
    }

    
    /// <summary>
    /// ECS系统容器
    /// </summary>
    [System.Serializable]
    public sealed partial class SystemContainer
    {
        private Dictionary<Type, SystemBase> m_Systems = new Dictionary<Type, SystemBase>();
        
        private List<SystemBase> m_OrderedSystems = new List<SystemBase>();

        public SystemContainer()
        {
            foreach (var sys in AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(a => a.GetTypes())
                         .Where(t =>
                             t.GetCustomAttribute<ECSSystemAttribute>() != null &&
                             typeof(SystemBase).IsAssignableFrom(t)))
            {
                Register(sys);
            }
        }

        public void Register<TSystem>(params Entity[] entities) where TSystem : SystemBase => Register(typeof(TSystem), entities);
        public void Register(Type systemType, params Entity[] entities)
        {
            if (!typeof(SystemBase).IsAssignableFrom(systemType))
                throw new ArgumentException($"{systemType.Name} is not a system");
            if (m_Systems.ContainsKey(systemType))
            {
                m_Systems[systemType].Entities.AddEntity(entities);
                return;
            }
            var sys = (SystemBase)Activator.CreateInstance(systemType, entities);
            m_Systems.Add(systemType, sys);
            m_OrderedSystems.Add(sys);
            m_OrderedSystems = m_OrderedSystems.OrderBy(s => s.Order).ToList();
        }

        public void UnRegister<TSystem>() where TSystem : SystemBase => UnRegister(typeof(TSystem));
        public void UnRegister(Type systemType)
        {
            if (!m_Systems.ContainsKey(systemType)) return;
            var system = m_Systems[systemType];
            m_Systems.Remove(systemType);
            m_OrderedSystems.Remove(system);
        }

        public TSystem Get<TSystem>() where TSystem : SystemBase =>
            m_Systems.TryGetValue(typeof(TSystem), out SystemBase system) ? (TSystem)system : null;

        public SystemBase[] GetAll()
        {
            return m_OrderedSystems.ToArray();
        }

        public void ForEach(Action<SystemBase> action)
        {
            // m_OrderedSystems?.ForEach(s =>
            // {
            //     action?.Invoke(s);
            // });
            // foreach (var sys in m_OrderedSystems)
            // {
            //     action?.Invoke(sys);
            // }
            for (int i = 0; i < m_OrderedSystems.Count; i++)
            {
                action?.Invoke(m_OrderedSystems[i]);
            }
        }
        
        public void ForEach<TSystem>(Action<TSystem> action) where TSystem : SystemBase
        {
            // m_OrderedSystems?.Where((e) => e.GetType() == typeof(TSystem)).ToList().ForEach(s =>
            // {
            //     action?.Invoke((TSystem)s);
            // });
            var systems = m_OrderedSystems?.Where((e) => e.GetType() == typeof(TSystem)).ToList();
            for (int i = 0; i < systems.Count; i++)
            {
                action?.Invoke((TSystem)systems[i]);
            }
        }
    }
}