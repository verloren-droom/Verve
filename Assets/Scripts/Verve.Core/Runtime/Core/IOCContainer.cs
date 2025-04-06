namespace Verve
{
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif
    using System.Reflection;
    using System.Collections.Generic;
    using System.Collections.Concurrent;

    
    public class IOCContainer
    {
        private readonly ConcurrentDictionary<Type, object> m_Factories = new ConcurrentDictionary<Type, object>();

        public void Register<T>(T instance, bool overwrite = true) where T : class
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

        public object ResolveOrRegister(System.Type type, params object[] args)
        {
            return ResolveOrRegister(type, _ =>
            {
#if UNITY_5_3_OR_NEWER
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    return GameObject.FindObjectOfType(type);
                }
                else
#endif
                {
                    return Activator.CreateInstance(type, args);
                }
                
            });
        }

        public T Resolve<T>() where T : class
        {
            return m_Factories.TryGetValue(typeof(T), out var value) ? (T)value : null;
        }

        public T ResolveOrRegister<T>(Func<System.Type, object> valueFactory) where T : class
        {
            return (T)ResolveOrRegister(typeof(T), valueFactory);
        }

        public T ResolveOrRegister<T>(params object[] args) where T : class
        {
            return ResolveOrRegister<T>(_ =>
            {
#if UNITY_5_3_OR_NEWER
                if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
                {
                    return GameObject.FindObjectOfType(typeof(T));
                }
                else
#endif
                {
                    return Activator.CreateInstance(typeof(T), args);
                }
            });
        }

        public bool TryResolve<T>(out T outInstance) where T : class
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

        public void InjectDependencies(object target)
        {
            if (target == null) return;

            var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.GetCustomAttribute(typeof(InjectAttribute)) is InjectAttribute injectAttribute)
                {
                    var fieldType = field.FieldType;
                    var instance = injectAttribute.Args is { Length: > 0 } ? ResolveOrRegister(fieldType, injectAttribute.Args) : Resolve(fieldType);
                    if (!injectAttribute.IsAdd)
                    {
                        instance = Resolve(fieldType);
                    }
                    if (!TryResolve(fieldType, out var _))
                    {
                        throw new InvalidOperationException($"无法找到类型为 {fieldType.FullName} 的注入实例，请确保已注册该类型");
                    }
                    field.SetValue(target, instance);
                }
            }
        }


        public void InjectDependencies(IEnumerable<object> targets)
        {
            foreach (var target in targets)
            {
                InjectDependencies(target);
            }
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

    [AttributeUsage(AttributeTargets.Field)]
    public class InjectAttribute : Attribute
    {
        public object[] Args { get; }
        public bool IsAdd { get; } = false;

        public InjectAttribute()
        {
            IsAdd = false;
        }

        public InjectAttribute(object arg1, params object[] args)
        {
            IsAdd = true;
            Args = new object[args.Length + 1];
            Args[0] = arg1;
            Array.Copy(args, 0, Args, 1, args.Length);
        }
    }
}