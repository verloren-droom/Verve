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
            if (runner?.ComponentProfile == null) return false;
            
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
        /// 获取游戏功能组件
        /// </summary>
        public static T GetComponent<T>()
            where T : GameFeatureComponent
        {
            TryGetComponent(out T component);
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
        public static T GetSubmodule<T>()
            where T : class, IGameFeatureSubmodule
        {
            var type = typeof(T);
            if (s_SubmoduleCache.TryGetValue(type, out var cachedSubmodule) && cachedSubmodule is T typedSubmodule)
            {
                return typedSubmodule;
            }
            
            var runner = GameFeaturesRunner.Instance;
            if (runner?.AllSubmodules == null) return null;
                
            foreach (var sub in runner.AllSubmodules)
            {
                if (sub is T typedSub)
                {
                    s_SubmoduleCache[type] = typedSub;
                    return typedSub;
                }
            }

            return null;
        }
        
        /// <summary>
        /// 添加模块（需要等一帧调用）
        /// </summary>
        /// <param name="menuPath">菜单路径</param>
        /// <returns></returns>
        public static bool AddModule(string menuPath)
            => GameFeaturesRunner.Instance.AddModule(menuPath);
        
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