#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using System.Reflection;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>模块字段编辑器；由清单拥有，通过 <see cref="SerializedProperty"/> 编辑配置。</para>
    /// </summary>
    public class GameModuleEditor : DisposableObject
    {
        /// <summary>
        ///   <para>模块编辑器类型缓存；不持有编辑器实例或模块。</para>
        /// </summary>
        private static readonly Dictionary<Type, Type> s_ModuleEditorTypes = new();

        /// <summary>
        ///   <para>创建模块编辑器；调用方拥有实例，未声明时使用默认字段界面。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        internal static GameModuleEditor Create(Type moduleType)
        {
            if (!s_ModuleEditorTypes.TryGetValue(moduleType, out var editorType))
                s_ModuleEditorTypes.Add(moduleType, editorType = FindEditorType(moduleType));
            return (GameModuleEditor)Activator.CreateInstance(editorType, nonPublic: true);
        }

        /// <summary>
        ///   <para>选择最近的编辑器声明；同一目标重复声明或无效实现直接报错。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        private static Type FindEditorType(Type moduleType)
        {
            var candidates = TypeCache.GetTypesWithAttribute<CustomGameModuleEditorAttribute>();
            for (var current = moduleType; current != null && typeof(GameModule).IsAssignableFrom(current); current = current.BaseType)
            {
                Type match = null;
                foreach (var candidate in candidates)
                {
                    var declaration = candidate.GetCustomAttribute<CustomGameModuleEditorAttribute>(inherit: false);
                    if (declaration.ModuleType != current || current != moduleType && !declaration.EditorForChildClasses) continue;
                    if (!typeof(GameModuleEditor).IsAssignableFrom(candidate) || candidate.IsAbstract || candidate.ContainsGenericParameters ||
                        candidate.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                            null, Type.EmptyTypes, null) == null)
                        throw new InvalidOperationException($"Module editor '{candidate.FullName}' must be a concrete GameModuleEditor with a parameterless constructor.");
                    if (match != null)
                        throw new InvalidOperationException($"Multiple module editors target '{current.FullName}': '{match.FullName}', '{candidate.FullName}'.");
                    match = candidate;
                }
                if (match != null) return match;
            }
            return typeof(GameModuleEditor);
        }

        /// <summary>
        ///   <para>绘制模块字段；调用基类保留默认界面，提交和撤销由清单处理。</para>
        /// </summary>
        /// <param name="module">当前模块属性；仅在本次调用中借用，不缓存或释放。</param>
        public virtual void OnInspectorGUI(SerializedProperty module)
        {
            ThrowIfDisposed();
            using var child = module.Copy();
            using var end = child.GetEndProperty();
            var hasFields = false;
            var enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                hasFields = true;
                EditorGUILayout.PropertyField(child, true);
                enterChildren = false;
            }
            if (!hasFields) EditorGUILayout.HelpBox("This module does not expose editable serialized fields.", MessageType.Info);
        }
    }
}

#endif
