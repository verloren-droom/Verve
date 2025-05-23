namespace Verve.ECS
{
    using System;
    using System.Text;
    using System.Linq;
    using System.Threading;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    
    
    /// <summary>
    /// ECS实体
    /// </summary>
    [Serializable]
    public sealed partial class Entity : IDisposable
    {
        private string m_ID;

        /// <summary>
        /// 实体唯一ID
        /// </summary>
        public string ID => m_ID;
        /// <summary>
        /// 存储已添加到实体中的组件
        /// </summary>
        private readonly Dictionary<Type, IComponentBase> m_Components = new Dictionary<Type, IComponentBase>();
        
        /// <summary>
        /// 实体ID计数
        /// </summary>
        // private static int m_IDCounter;
        /// <summary>
        /// 存储所有已创建的实体
        /// </summary>
        private static ConcurrentDictionary<string, Entity> m_Entities = new ConcurrentDictionary<string, Entity>();

        private Entity()
        {
            // do {
            //    m_ID = Interlocked.Increment(ref m_IDCounter);
            //     if (m_ID == int.MaxValue) 
            //         Interlocked.Exchange(ref m_IDCounter, 0);
            // } while (m_Entities.ContainsKey(m_ID));
            m_ID = Guid.NewGuid().ToString();
        }

        ~Entity() => Dispose();

        /// <summary>
        /// 添加ECS组件到ECS实体中
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public TComponent AddComponent<TComponent>() where TComponent : IComponentBase, new() 
            => AddComponent<TComponent>(new TComponent());

        /// <summary>
        /// 添加ECS组件到ECS实体中，并设置值
        /// </summary>
        /// <param name="comp">ECS组件数据</param>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public TComponent AddComponent<TComponent>(TComponent comp) where TComponent : IComponentBase, new()
        {
            if (TryGetComponent<TComponent>(out var outComp))
            {
                return outComp;
            }
            m_Components[typeof(TComponent)] = comp;
            return comp;
        }

        /// <summary>
        /// 添加ECS组件到ECS实体中
        /// </summary>
        /// <param name="compType">ECS组件类型</param>
        /// <returns></returns>
        public IComponentBase AddComponent(Type compType) =>
            AddComponent(compType, (IComponentBase)Activator.CreateInstance(compType));

        /// <summary>
        /// 添加ECS组件到ECS实体中，并设置值
        /// </summary>
        /// <param name="compType">ECS组件类型</param>
        /// <param name="comp">ECS组件数据</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">类型错误</exception>
        public IComponentBase AddComponent(Type compType, IComponentBase comp)
        {
            if (!typeof(IComponentBase).IsAssignableFrom(compType))
                throw new ArgumentException($"{compType.Name} is not a component");
            if (m_Components.TryGetValue(compType, out var outComp))
            {
                return outComp;
            }
            m_Components[compType] = comp;
            return m_Components[compType];
        }

        /// <summary>
        /// 移除ECS组件
        /// </summary>
        /// <param name="compType">ECS数据类型</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public bool RemoveComponent(Type compType)
        {
            if (!typeof(IComponentBase).IsAssignableFrom(compType))
                throw new ArgumentException($"{compType.Name} is not a component");
            return m_Components.Remove(compType);
        }
        
        /// <summary>
        /// 移除ECS组件
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public bool RemoveComponent<TComponent>() where TComponent : IComponentBase, new() 
            => RemoveComponent(typeof(TComponent));

        /// <summary>
        /// 尝试获取ECS组件
        /// </summary>
        /// <param name="component"></param>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public bool TryGetComponent<TComponent>(out TComponent component) where TComponent : IComponentBase, new()
        {
            if (m_Components.TryGetValue(typeof(TComponent), out var comp))
            {
                component = (TComponent)comp;
                return true;
            }
            component = default;
            return false;
        }

        /// <summary>
        /// 设置ECS组件数据
        /// </summary>
        /// <param name="inComp"></param>
        /// <typeparam name="TComponent"></typeparam>
        public void SetComponent<TComponent>(TComponent inComp) where TComponent : IComponentBase, new()
        {
            var compType = typeof(TComponent);
            if (TryGetComponent<TComponent>(out var existingComp))
            {
                // foreach (var property in compType.GetProperties())
                // {
                //     var newValue = property.GetValue(inComp);
                //     property.SetValue(existingComp, newValue);
                // }
                m_Components[compType] = inComp;
            }
            else
            {
                m_Components.Add(compType, inComp);
            }
        }

        /// <summary>
        /// 获取所有ECS组件数据
        /// </summary>
        /// <returns></returns>
        public IComponentBase[] GetAllComponents()
        {
            return m_Components.Values.ToArray();
        }

        public void Dispose()
        {
            try
            {
                if (m_Entities.TryRemove(m_ID, out var _))
                {
                    m_Components.Clear();
                }
                GC.SuppressFinalize(this);
            }
            catch { }
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append($"[ID {m_ID}] Entity\n\bComponents:");
            foreach (var key in m_Components.Keys)
            {
                str.Append($" {key.Name}");
            }
            return str.ToString();
        }

        #region 静态方法
        
        /// <summary>
        /// 创建ECS实体
        /// </summary>
        /// <param name="compTypes"></param>
        /// <returns></returns>
        public static Entity Create(params Type[] compTypes)
        {
            var entity = new Entity();
            if (m_Entities.TryAdd(entity.m_ID, entity) && compTypes != null)
            {
                for (int i = 0; i < compTypes.Length; i++)
                {
                    entity.AddComponent(compTypes[i]);
                }
            }
            return entity;
        }

        /// <summary>
        /// 创建ECS实体
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        public static Entity Create<T1>() where T1 : IComponentBase, new() => Create(typeof(T1));
        public static Entity Create<T1, T2>() where T1 : IComponentBase, new() where T2 : IComponentBase, new() => Create(typeof(T1), typeof(T2));
        public static Entity Create<T1, T2, T3>() where T1 : IComponentBase, new() where T2 : IComponentBase, new() where T3 : IComponentBase, new() => Create(typeof(T1), typeof(T2), typeof(T3));
        public static Entity Create<T1, T2, T3, T4>() where T1 : IComponentBase, new() where T2 : IComponentBase, new() where T3 : IComponentBase, new() where T4 : IComponentBase, new() => Create(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        public static Entity Create<T1, T2, T3, T4, T5>() where T1 : IComponentBase, new() where T2 : IComponentBase, new() where T3 : IComponentBase, new() where T4 : IComponentBase, new() where T5 : IComponentBase, new() => Create(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

        /// <summary>
        /// 获取指定ID的实体
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Entity Get(string id)
        {
            return m_Entities.TryGetValue(id, out var entity) ? entity : null;
        }

        /// <summary>
        /// 获取或创建实体
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Entity GetOrCreate(string id) => Get(id) ?? Create();

        public static Entity[] GetAll()
        {
            return m_Entities.Values.ToArray();
        }

        public static void Destroy(string id)
        {
            if (m_Entities.TryGetValue(id, out var entity))
            {
                entity?.Dispose();
            }
        }
        
        public static void Destroy(Entity entity)
        {
            entity?.Dispose();
        }

        public static void DestroyAll()
        {
            foreach (var entity in m_Entities.Values)
            {
                Destroy(entity);
            }
        }

        #endregion
    }
    
    
    /// <summary>
    /// ECS实体查找
    /// </summary>
    [Serializable]
    public sealed partial class EntityQuery
    { 
        private HashSet<Entity> m_Entities = new HashSet<Entity>();
        
        internal EntityQuery(IEnumerable<Entity> entities)
        {
            if (entities != null)
            {
                m_Entities = new HashSet<Entity>(entities.ToHashSet());
            }
        }
        
        internal EntityQuery(params Entity[] entities)
        {
            if (entities != null)
            {
                m_Entities = new HashSet<Entity>(entities.ToHashSet());
            }
        }

        public void AddEntity(params Entity[] entities)
        {
            if (entities == null) return;
            foreach (var e in entities)
            {
                m_Entities.Add(e);
            }
        }
        
        /// <summary>
        /// 遍历实体
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public EntityQuery ForEach(Action<Entity> action)
        {
            foreach (var e in m_Entities)
            {
                action?.Invoke(e);
            }
            return this;
        }
        
        /// <summary>
        /// 筛选实体中指定的组件
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <returns></returns>
        public EntityQuery WithAll<TComponent>() where TComponent : IComponentBase, new()
        {
            m_Entities = m_Entities.Where(entity => entity.TryGetComponent<TComponent>(out _)).ToHashSet();
            return this;
        }
    }
}