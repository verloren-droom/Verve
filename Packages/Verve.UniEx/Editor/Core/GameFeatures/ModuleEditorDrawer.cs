#if UNITY_EDITOR

namespace VerveEditor.UniEx
{
    using System;
    
    
    /// <summary>
    /// 模块设置绘制器特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ModuleEditorDrawerAttribute : Attribute
    {
        public readonly Type moduleType;
        public ModuleEditorDrawerAttribute(Type moduleType) => this.moduleType = moduleType;
    }

    
    /// <summary>
    /// 模块设置绘制器基类
    /// </summary>
    [Serializable]
    public class ModuleEditorDrawer
    {
        public virtual bool OnGUI() => false;
        public virtual void ResetToDefault() { }
    }
}

#endif
