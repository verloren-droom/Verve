#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;

    /// <summary>
    ///   <para>模块编辑器声明；将自定义界面关联到模块类型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class CustomGameModuleEditorAttribute : Attribute
    {
        /// <summary>
        ///   <para>目标模块类型。</para>
        /// </summary>
        public Type ModuleType { get; }
        /// <summary>
        ///   <para>是否同时编辑派生模块；优先使用最近类型的声明。</para>
        /// </summary>
        public bool EditorForChildClasses { get; }

        /// <summary>
        ///   <para>声明模块编辑器。</para>
        /// </summary>
        /// <param name="moduleType">目标模块类型。</param>
        /// <param name="editorForChildClasses">是否用于派生模块。</param>
        public CustomGameModuleEditorAttribute(Type moduleType, bool editorForChildClasses = false)
        {
            ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
            if (!typeof(GameModule).IsAssignableFrom(moduleType) || moduleType.ContainsGenericParameters)
                throw new ArgumentException("The target must be a closed GameModule type.", nameof(moduleType));
            EditorForChildClasses = editorForChildClasses;
        }
    }
}

#endif