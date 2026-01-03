#if UNITY_5_3_OR_NEWER

namespace Verve.ScriptRuntime
{
    using System;
    using System.Linq;
    using UnityEngine;
    using System.Threading;
    using System.Reflection;
    using System.Threading.Tasks;


    /// <summary>
    ///   <para>程序集运行时子模块基类</para>
    /// </summary>
    public abstract class AssemblyRuntimeSubmodule : GameFeatureSubmodule<ScriptRuntimeGameFeatureComponent>, IAssemblyRuntime
    {
        public virtual async Task<Assembly> LoadAssemblyAsync(string assemblyName, byte[] assemblyData, byte[] pdbData = null, CancellationToken ct = default)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(assemblyName)) throw new ArgumentNullException(nameof(assemblyName));
            ct.ThrowIfCancellationRequested();
            return AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == assemblyName);
#else
            if (assemblyData == null || assemblyData.Length == 0) throw new ArgumentNullException(nameof(assemblyData));
            ct.ThrowIfCancellationRequested();
            if (pdbData != null && pdbData.Length > 0)
            {
                return Assembly.Load(assemblyData, pdbData);
            }
            else
            {
                return Assembly.Load(assemblyData);
            }
#endif
        }

        public virtual object CreateInstance(string typeName, params object[] args)
        {
            var type = GetType(typeName);
            
            if (type == null)
                throw new TypeLoadException($"无法找到类型: {typeName}");

            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                return ScriptableObject.CreateInstance(type);
            }
            if (typeof(Component).IsAssignableFrom(type))
            {
                var go = new GameObject(type.Name);
                return go.AddComponent(type);
            }
            
            return Activator.CreateInstance(type, args);
        }

        public virtual object InvokeStaticMethod(string typeName, string methodName, params object[] args)
        {
            var type = GetType(typeName);

            if (type == null)
                throw new TypeLoadException($"无法找到类型: {typeName}");

            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
                throw new MissingMethodException($"无法找到静态方法: {methodName} 在类型: {typeName} 中");

            return methodInfo.Invoke(null, args);
        }

        public virtual object InvokeInstanceMethod(object instance, string methodName, params object[] args)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();
            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
                throw new MissingMethodException($"无法找到实例方法: {methodName} 在类型: {type.FullName} 中");

            return methodInfo.Invoke(instance, args);
        }
        
        /// <summary>
        ///   <para>获取类型</para>
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>
        ///   <para>类型</para>
        /// </returns>
        protected static Type GetType(string typeName)
        {
            return Type.GetType(typeName) ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);
        }
    }
}

#endif