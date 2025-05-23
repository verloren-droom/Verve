namespace Verve
{
    using System;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    
    public partial class IOCContainer
    {
        private readonly ConcurrentDictionary<Type, object> m_Factories = new ConcurrentDictionary<Type, object>();

        public void Register<T>(T instance, bool overwrite = true) where T : new()
        {
            instance = instance ?? throw new ArgumentNullException(nameof(instance));
            m_Factories.AddOrUpdate(typeof(T), instance, (type, oldInstance) => overwrite ? instance : oldInstance);
        }
        
        public void Unregister<T>() where T : class => Unregister(typeof(T));

        public void Unregister(System.Type type)
        {
            m_Factories.TryRemove(type, out var _);
        }
        
        public object Resolve(System.Type type)
        {
            return m_Factories.TryGetValue(type, out var value) ? value : null;
        }

        public object ResolveOrRegister(System.Type type, Func<System.Type, object> valueFactory)
        {
            return m_Factories.GetOrAdd(type, valueFactory);
        }

        public virtual object ResolveOrRegister(System.Type type, params object[] args)
        {
            return ResolveOrRegister(type, _ => Activator.CreateInstance(type, args));
        }

        public T Resolve<T>() where T : new()
        {
            return m_Factories.TryGetValue(typeof(T), out var value) ? (T)value : default;
        }

        public T ResolveOrRegister<T>(Func<System.Type, object> valueFactory) where T : new()
        {
            return (T)ResolveOrRegister(typeof(T), valueFactory);
        }

        public virtual T ResolveOrRegister<T>(params object[] args) where T : new()
        {
            return ResolveOrRegister<T>(_ => Activator.CreateInstance(typeof(T), args));
        }

        public bool TryResolve<T>(out T outInstance) where T : new()
        {
            outInstance = Resolve<T>();
            return outInstance != null;
        }

        public bool TryResolve(System.Type type, out object outInstance)
        {
            outInstance = Resolve(type);
            return outInstance != null;
        }

        public void Clear()
        {
            foreach (var pair in m_Factories)
            {
                if (pair.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            m_Factories.Clear();
        }

        public static IOCContainer operator +(IOCContainer left, IOCContainer right)
        {
            var merged = new IOCContainer();
            if (left == null && right == null) return merged;
            left ??= new IOCContainer();
            right ??= new IOCContainer();

            foreach (var pair in left.m_Factories)
            {
                merged.m_Factories.TryAdd(pair.Key, pair.Value);
            }
            foreach (var pair in right.m_Factories)
            {
                merged.m_Factories.AddOrUpdate(
                    pair.Key,
                    pair.Value,
                    (key, existing) => existing
                );
            }
            return merged;
        }
    }
}