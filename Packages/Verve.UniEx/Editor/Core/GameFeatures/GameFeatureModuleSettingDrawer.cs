#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    using Verve.UniEx;
    
    
    /// <summary>
    ///   <para>游戏功能模块设置绘制器特性</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class GameFeatureModuleSettingDrawerAttribute : Attribute
    {
        /// <summary>
        ///   <para>模块类型</para>
        /// </summary>
        public readonly Type moduleType;


        public GameFeatureModuleSettingDrawerAttribute(Type moduleType)
        {
            if (!typeof(GameFeatureModule).IsAssignableFrom(moduleType))
                throw new ArgumentException($"{moduleType} is not a subclass of {nameof(GameFeatureModule)}.");
            this.moduleType = moduleType;
        }
    }

    
    /// <summary>
    ///   <para>游戏功能模块设置绘制器基类</para>
    /// </summary>
    [Serializable]
    public class GameFeatureModuleSettingDrawer
    {
        /// <summary>
        ///   <para>绘制</para>
        /// </summary>
        public virtual void OnGUI() { }
    }
}

#endif
