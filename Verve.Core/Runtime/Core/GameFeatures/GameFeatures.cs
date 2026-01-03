#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    

    /// <summary>
    ///   <para>游戏功能访问工具类</para>
    ///   <para>封装了游戏功能组件和子模块的访问方法</para>
    /// </summary>
    public static partial class GameFeatures
    {
        private static readonly Dictionary<Type, GameFeatureComponent> s_ComponentCache = 
            new Dictionary<Type, GameFeatureComponent>();
        private static readonly Dictionary<Type, IGameFeatureSubmodule> s_SubmoduleCache = 
            new Dictionary<Type, IGameFeatureSubmodule>();
        
        static GameFeatures()
        {
            GameFeaturesRunner.Instance.OnModuleAdded += (module) =>
            { 
                ClearCache();
            };
            GameFeaturesRunner.Instance.OnModuleRemoved += (module) =>
            { 
                ClearCache();
            };
        }
        
        /// <summary>
        ///   <para>尝试获取游戏功能组件</para>
        /// </summary>
        /// <param name="component">游戏功能组件</param>
        /// <returns>
        ///   <para>成功获取返回true</para>
        /// </returns>
        public static bool TryGetComponent<T>(out T component)
            where T : GameFeatureComponent
        {
            component = null;
            var runner = GameFeaturesRunner.Instance;
            if (runner.ComponentProfile == null) return false;
            
            var type = typeof(T);
            if (s_ComponentCache.TryGetValue(type, out var cachedComponent) && cachedComponent is T typedComponent)
            {
                component = typedComponent;
                return true;
            }
            
            if (runner.ComponentProfile.TryGet(out component))
            {
                s_ComponentCache[type] = component;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        ///   <para>尝试获取游戏功能组件</para>
        /// </summary>
        /// <param name="type">游戏功能组件类型</param>
        /// <param name="component">游戏功能组件</param>
        /// <returns>
        ///   <para>成功获取返回true</para>
        /// </returns>
        public static bool TryGetComponent(Type type, out GameFeatureComponent component)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(GameFeatureComponent).IsAssignableFrom(type)) throw new ArgumentException($"Type {type} is not a GameFeatureComponent");
            
            component = null;
            var runner = GameFeaturesRunner.Instance;
            if (runner.ComponentProfile == null) return false;
            
            if (s_ComponentCache.TryGetValue(type, out var cachedComponent))
            {
                component = cachedComponent;
                return true;
            }
            
            if (runner.ComponentProfile.TryGet(out component))
            {
                s_ComponentCache[type] = component;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        ///   <para>获取游戏功能组件</para>
        /// </summary>
        /// <typeparam name="T">游戏功能组件类型</typeparam>
        /// <returns>
        ///   <para>游戏功能组件</para>
        /// </returns>
        public static T GetComponent<T>()
            where T : GameFeatureComponent
        {
            TryGetComponent(out T component);
            return component;
        }
        
        /// <summary>
        ///   <para>获取游戏功能组件</para>
        /// </summary>
        /// <param name="type">游戏功能组件类型</param>
        /// <returns>
        ///   <para>游戏功能组件</para>
        /// </returns>
        public static GameFeatureComponent GetComponent(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(GameFeatureComponent).IsAssignableFrom(type)) throw new ArgumentException($"Type {type} is not a GameFeatureComponent");

            TryGetComponent(type, out var component);
            return component;
        }
        
        /// <summary>
        ///  <para>尝试获取游戏功能子模块</para>
        /// </summary>
        /// <typeparam name="T">游戏功能子模块类型</typeparam>
        /// <param name="submodule">游戏功能子模块</param>
        /// <returns>
        ///   <para>成功获取返回true</para>
        /// </returns>
        public static bool TryGetSubmodule<T>(out T submodule)
            where T : class, IGameFeatureSubmodule
        {
            submodule = GetSubmodule<T>();
            return submodule != null;
        }
        
        /// <summary>
        ///   <para>获取游戏功能子模块</para>
        /// </summary>
        /// <param name="typeName">类型名 [命名空间.类名, 程序集名]</param>
        /// <returns>
        ///   <para>游戏功能子模块</para>
        /// </returns>
        public static IGameFeatureSubmodule GetSubmodule(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) throw new ArgumentNullException(nameof(typeName));
            var type = Type.GetType(typeName);
            if (type == null) throw new ArgumentException($"Type {typeName} not found");
            return GetSubmodule(type);
        }

        /// <summary>
        ///   <para>获取游戏功能子模块</para>
        /// </summary>
        /// <param name="type">游戏功能子模块类型</param>
        /// <returns>
        ///   <para>游戏功能子模块</para>
        /// </returns>
        public static IGameFeatureSubmodule GetSubmodule(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(IGameFeatureSubmodule).IsAssignableFrom(type)) throw new ArgumentException($"Type {type} is not a IGameFeatureSubmodule");
            
            if (s_SubmoduleCache.TryGetValue(type, out var cachedSubmodule))
            {
                return cachedSubmodule;
            }
            
            var runner = GameFeaturesRunner.Instance;
            if (runner?.AllSubmodules == null) return null;
            
            foreach (var sub in runner.AllSubmodules)
            {
                if (sub.GetType() == type)
                {
                    s_SubmoduleCache[type] = sub;
                    return sub;
                }
            }

            return null;
        }

        /// <summary>
        ///  <para>获取游戏功能子模块</para>
        /// </summary>
        /// <typeparam name="T">游戏功能子模块类型</typeparam>
        /// <returns>
        ///   <para>游戏功能子模块</para>
        /// </returns>
        public static T GetSubmodule<T>()
            where T : class, IGameFeatureSubmodule
        {
            var type = typeof(T);
            return GetSubmodule(type) as T;
        }

        /// <summary>
        ///   <para>添加模块（需要等一帧调用）</para>
        /// </summary>
        /// <param name="module">功能模块</param>
        public static void AddModule(GameFeatureModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            GameFeaturesRunner.Instance.AddModule(module);
        }
        
        /// <summary>
        ///   <para>添加模块（需要等一帧调用）</para>
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <returns>
        ///   <para>成功添加返回true</para>
        /// </returns>
        public static bool AddModule(string menuPath)
            => GameFeaturesRunner.Instance.AddModule(menuPath);
        
        /// <summary>
        ///   <para>移除模块（需要等一帧调用）</para>
        /// </summary>
        /// <param name="module">功能模块</param>
        public static void RemoveModule(GameFeatureModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            GameFeaturesRunner.Instance.RemoveModule(module);
        }
        
        /// <summary>
        ///   <para>移除模块（需要等一帧调用）</para>
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <returns>
        ///   <para>成功移除返回true</para>
        /// </returns>
        public static bool RemoveModule(string menuPath)
            => GameFeaturesRunner.Instance.RemoveModule(menuPath);
        
        /// <summary>
        ///   <para>获取模块是否激活</para>
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <returns>
        ///   <para>模块是否激活</para>
        /// </returns>
        public static bool GetModuleActive(string menuPath)
            => GameFeaturesRunner.Instance.GetModuleActive(menuPath);
        
        /// <summary>
        ///   <para>设置模块激活</para>
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <param name="isActive">是否激活</param>
        /// <returns>
        ///   <para>成功设置返回true</para>
        /// </returns>
        public static bool SetModuleActive(string menuPath, bool isActive)
            => GameFeaturesRunner.Instance.SetModuleActive(menuPath, isActive);
        
        /// <summary>
        ///   <para>调用模块方法</para>
        /// </summary>
        /// <typeparam name="TSubmodule">子模块类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="expression">方法表达式</param>
        public static TResult CallSubmoduleMethod<TSubmodule, TResult>(Expression<Func<TSubmodule, TResult>> expression)
            where TSubmodule : class, IGameFeatureSubmodule
        {
            var submodule = GetSubmodule<TSubmodule>();
            if (submodule == null)
                throw new InvalidOperationException($"Submodule {typeof(TSubmodule).Name} not found");

            var compiled = expression.Compile();
            return compiled(submodule);
        }
        
        /// <summary>
        ///   <para>调用模块方法</para>
        /// </summary>
        /// <typeparam name="TSubmodule">子模块类型</typeparam>
        /// <param name="expression">方法表达式</param>
        public static void CallSubmoduleMethod<TSubmodule>(Expression<Action<TSubmodule>> expression)
            where TSubmodule : class, IGameFeatureSubmodule
        {
            var submodule = GetSubmodule<TSubmodule>();
            if (submodule == null)
                throw new InvalidOperationException($"Submodule {typeof(TSubmodule).Name} not found");

            var compiled = expression.Compile();
            compiled(submodule);
        }

        /// <summary>
        ///   <para>清除缓存（在模块变化时调用）</para>
        /// </summary>
        private static void ClearCache()
        {
            s_ComponentCache.Clear();
            s_SubmoduleCache.Clear();
        }
    }
}

#endif