#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    
    
    /// <summary>
    ///   <para>游戏功能特性基类</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class GameFeatureAttribute : Attribute
    {
        /// <summary>
        ///   <para>菜单路径</para>
        /// </summary>
        public string MenuPath { get; protected set; }
        /// <summary>
        ///   <para>依赖模块</para>
        /// </summary>
        public Type[] Dependencies { get; protected set; }
        /// <summary>
        ///   <para>描述</para>
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        ///   <para>选择模式</para>
        /// </summary>
        public SubmoduleSelectionMode SelectionMode { get; set; } = SubmoduleSelectionMode.Multiple;
        
        
        protected GameFeatureAttribute(string menuPath, Type[] dependencies)
        {
            MenuPath = menuPath;
            Dependencies = dependencies ?? Array.Empty<Type>();
            ValidateDependencies();
        }
        
        private void ValidateDependencies()
        {
            foreach (var type in Dependencies)
            {
                if (!typeof(IGameFeatureModule).IsAssignableFrom(type)
                    // && !typeof(IGameFeatureSubmodule).IsAssignableFrom(type)
                    )
                {
                    throw new InvalidOperationException($"{type} is not a valid dependency. " +
                                                        "Dependencies must be IGameFeatureModule or IGameFeatureSubmodule.");
                }
            }
        }
    }
    
    /// <summary>
    /// 游戏功能子模块选择模式枚举
    /// </summary>
    [Flags]
    public enum SubmoduleSelectionMode : byte
    {
        /// <summary> 多选模式 - 可以选择多个实例 </summary>
        Multiple,
        /// <summary> 单选模式 - 只能选择一个实例 </summary>
        Single,
        /// <summary> 锁定模式 - 用户不能手动更改 </summary>
        Locked,
    }
}

#endif