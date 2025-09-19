#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using System.Text.RegularExpressions;

    
    /// <summary>
    /// 游戏功能组件菜单显示特性 - 用于可视化创建游戏功能模块组件
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class GameFeatureComponentMenuAttribute : Attribute
    {
        /// <summary> 菜单路径 </summary>
        public string MenuPath { get; }
        
        
        public GameFeatureComponentMenuAttribute() { }
        
        public GameFeatureComponentMenuAttribute(string menuPath) : this()
        {
            if (!IsValidMenuPath(menuPath))
                throw new ArgumentException("Menu path contains invalid characters.");
                
            MenuPath = menuPath;
        }
        
        private static bool IsValidMenuPath(string path)
        {
            return Regex.IsMatch(path, @"^[a-zA-Z0-9\s_\-/]+$");
        }
    }
}

#endif