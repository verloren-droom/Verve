#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>游戏模块特性</para>
    ///   <para>用于标识模块</para>
    /// </summary>
    public sealed class GameFeatureModuleAttribute : GameFeatureAttribute
    {
        public GameFeatureModuleAttribute(string menuPath, params Type[] dependencies) 
            : base(menuPath, dependencies)
        {
            if (string.IsNullOrEmpty(menuPath))
                throw new ArgumentException("Menu path cannot be null or empty for modules");
        }
    }
}

#endif