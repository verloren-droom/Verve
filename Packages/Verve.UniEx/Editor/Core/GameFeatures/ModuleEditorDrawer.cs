#if UNITY_EDITOR

namespace VerveEditor
{
    using System;
    
    
    /// <summary>
    ///   <para>模块设置绘制器特性</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModuleEditorDrawerAttribute : Attribute
    {
        /// <summary>
        ///   <para>模块类型</para>
        /// </summary>
        public readonly Type moduleType;
        
        
        public ModuleEditorDrawerAttribute(Type moduleType) => this.moduleType = moduleType;
    }

    
    /// <summary>
    ///   <para>模块设置绘制器基类</para>
    /// </summary>
    [Serializable]
    public class ModuleEditorDrawer
    {
        /// <summary>
        ///   <para>绘制</para>
        /// </summary>
        public virtual bool OnGUI() => false;
        
        /// <summary>
        ///   <para>重置为默认值</para>
        /// </summary>
        public virtual void ResetToDefault() { }
    }
}

#endif
