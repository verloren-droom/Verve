#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    
    
    /// <summary>
    ///   <para>游戏子模块特性</para>
    ///   <para>用于标识子模块</para>
    /// </summary>
    public sealed class GameFeatureSubmoduleAttribute : GameFeatureAttribute
    {
        /// <summary>
        ///   <para>子模块所属的模块</para>
        /// </summary>
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