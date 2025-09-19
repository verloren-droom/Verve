#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    
    
    /// <summary>
    /// 游戏子模块特性 - 用于标识子模块
    /// </summary>
    public sealed class GameFeatureSubmoduleAttribute : GameFeatureAttribute
    {
        /// <summary> 子模块从属的模块类型 </summary>
        public Type BelongsToModule { get; }
        
        
        public GameFeatureSubmoduleAttribute(Type belongsToModule, params Type[] dependencies) 
            : base(null, dependencies)
        {
            if (!typeof(IGameFeatureModule).IsAssignableFrom(belongsToModule))
                throw new ArgumentException($"{nameof(belongsToModule)} must implement IGameFeatureModule");
                
            BelongsToModule = belongsToModule;
        }
        
        public GameFeatureSubmoduleAttribute(string menuPath, params Type[] dependencies) 
            : base(menuPath, dependencies)
        {
            if (string.IsNullOrEmpty(menuPath))
                throw new ArgumentException("Menu path cannot be null or empty for modules");
        }
    }
}

#endif