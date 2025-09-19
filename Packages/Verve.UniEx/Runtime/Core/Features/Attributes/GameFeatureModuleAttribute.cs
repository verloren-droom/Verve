#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    
    
    /// <summary>
    /// 游戏模块特性 - 用于标识模块
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