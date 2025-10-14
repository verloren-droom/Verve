#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.Linq.Expressions;
    using System.Collections.Generic;
    

    /// <summary>
    /// 游戏功能访问工具类 - 提供全局访问游戏功能组件和子模块的便捷方法
    /// </summary>
    public static class GameFeatures
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
        /// 尝试获取游戏功能组件
        /// </summary>
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
        /// 获取游戏功能组件
        /// </summary>
        public static T GetComponent<T>()
            where T : GameFeatureComponent
        {
            TryGetComponent(out T component);
            return component;
        }
        
        public static GameFeatureComponent GetComponent(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(GameFeatureComponent).IsAssignableFrom(type)) throw new ArgumentException($"Type {type} is not a GameFeatureComponent");

            TryGetComponent(type, out var component);
            return component;
        }
        
        /// <summary>
        /// 尝试获取游戏功能子模块
        /// </summary>
        public static bool TryGetSubmodule<T>(out T submodule)
            where T : class, IGameFeatureSubmodule
        {
            submodule = GetSubmodule<T>();
            return submodule != null;
        }
        
        /// <summary>
        /// 获取游戏功能子模块
        /// </summary>
        /// <param name="typeName">类型名 [命名空间.类名, 程序集名]</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static IGameFeatureSubmodule GetSubmodule(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) throw new ArgumentNullException(nameof(typeName));
            var type = Type.GetType(typeName);
            if (type == null) throw new ArgumentException($"Type {typeName} not found");
            return GetSubmodule(type);
        }

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
        /// 获取游戏功能子模块
        /// </summary>
        public static T GetSubmodule<T>()
            where T : class, IGameFeatureSubmodule
        {
            var type = typeof(T);
            return GetSubmodule(type) as T;
        }

        public static void AddModule(GameFeatureModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            GameFeaturesRunner.Instance.AddModule(module);
        }
        
        /// <summary>
        /// 添加模块（需要等一帧调用）
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <returns></returns>
        public static bool AddModule(string menuPath)
            => GameFeaturesRunner.Instance.AddModule(menuPath);
        
        public static void RemoveModule(GameFeatureModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            GameFeaturesRunner.Instance.RemoveModule(module);
        }
        
        /// <summary>
        /// 移除模块（需要等一帧调用）
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <returns></returns>
        public static bool RemoveModule(string menuPath)
            => GameFeaturesRunner.Instance.RemoveModule(menuPath);
        
        /// <summary>
        /// 获取模块是否激活
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <returns></returns>
        public static bool GetModuleActive(string menuPath)
            => GameFeaturesRunner.Instance.GetModuleActive(menuPath);
        
        /// <summary>
        /// 设置模块激活
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <param name="isActive">是否激活</param>
        public static bool SetModuleActive(string menuPath, bool isActive)
            => GameFeaturesRunner.Instance.SetModuleActive(menuPath, isActive);
        
        /// <summary>
        /// 调用模块方法
        /// </summary>
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
        /// 调用模块方法
        /// </summary>
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
        /// 清除缓存（在模块变化时调用）
        /// </summary>
        internal static void ClearCache()
        {
            s_ComponentCache.Clear();
            s_SubmoduleCache.Clear();
        }
    }
}

#endif