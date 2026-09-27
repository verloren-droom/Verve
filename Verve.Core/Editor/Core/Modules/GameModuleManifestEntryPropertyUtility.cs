#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>模块清单条目属性工具。</para>
    /// </summary>
    internal static class GameModuleManifestEntryPropertyUtility
    {
        /// <summary>
        ///   <para>从模块条目属性中解析当前模块类型。</para>
        /// </summary>
        /// <param name="entryProperty">条目属性。</param>
        /// <param name="moduleType">模块类型。</param>
        public static bool TryGetModuleType(SerializedProperty entryProperty, out Type moduleType)
        {
            moduleType = null;
            if (entryProperty == null)
            {
                return false;
            }

            var moduleProperty = GameModuleManifestEditorData.FindModuleProperty(entryProperty);
            if (moduleProperty?.managedReferenceValue is GameModule module)
            {
                moduleType = module.GetType();
                return moduleType != null;
            }

            return GameModuleManifestAsset.TryGetModuleType(
                GameModuleManifestEditorData.ReadModuleType(entryProperty),
                out moduleType);
        }

        /// <summary>
        ///   <para>判断模块条目列表是否已包含某个精确模块类型。</para>
        /// </summary>
        /// <param name="entryArrayProperty">条目数组属性。</param>
        /// <param name="moduleType">模块类型。</param>
        public static bool ContainsModuleType(SerializedProperty entryArrayProperty, Type moduleType)
        {
            if (entryArrayProperty == null || moduleType == null)
            {
                return false;
            }

            for (int i = 0; i < entryArrayProperty.arraySize; i++)
            {
                if (!TryGetModuleType(entryArrayProperty.GetArrayElementAtIndex(i), out var existingType))
                {
                    continue;
                }

                if (existingType == moduleType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>判断模块条目列表是否已包含可用于当前依赖的模块类型。</para>
        /// </summary>
        /// <param name="entryArrayProperty">条目数组属性。</param>
        /// <param name="dependencyType">依赖类型。</param>
        public static bool ContainsModuleForDependency(SerializedProperty entryArrayProperty, Type dependencyType) => ContainsModuleForDependency(entryArrayProperty, dependencyType, -1, -1);

        /// <summary>
        ///   <para>查找移除指定模块后会失去依赖满足项的模块类型。</para>
        /// </summary>
        /// <param name="entryArrayProperty">条目数组属性。</param>
        /// <param name="removedIndex">已移除索引。</param>
        /// <param name="dependentModuleTypes">依赖者模块类型。</param>
        /// <param name="error">错误。</param>
        public static bool TryFindBrokenDependents(
            SerializedProperty entryArrayProperty,
            int removedIndex,
            List<Type> dependentModuleTypes,
            out string error)
        {
            if (dependentModuleTypes == null) throw new ArgumentNullException(nameof(dependentModuleTypes));

            dependentModuleTypes.Clear();
            error = null;

            if (entryArrayProperty == null ||
                removedIndex < 0 ||
                removedIndex >= entryArrayProperty.arraySize ||
                !TryGetModuleType(entryArrayProperty.GetArrayElementAtIndex(removedIndex), out var removedType))
            {
                return true;
            }

            for (int i = 0; i < entryArrayProperty.arraySize; i++)
            {
                if (i == removedIndex)
                {
                    continue;
                }

                var entryProperty = entryArrayProperty.GetArrayElementAtIndex(i);
                if (!TryGetModuleType(entryProperty, out var dependentType))
                {
                    continue;
                }

                if (!TryGetDependencies(dependentType, out var dependencies, out error))
                {
                    return false;
                }

                for (int j = 0; j < dependencies.Length; j++)
                {
                    var dependencyType = dependencies[j];
                    if (dependencyType == null || !dependencyType.IsAssignableFrom(removedType))
                    {
                        continue;
                    }

                    if (ContainsModuleForDependency(entryArrayProperty, dependencyType, removedIndex, i))
                    {
                        continue;
                    }

                    if (!dependentModuleTypes.Contains(dependentType))
                    {
                        dependentModuleTypes.Add(dependentType);
                    }
                    break;
                }
            }

            return true;
        }

        /// <summary>
        ///   <para>判断清单是否包含所需依赖模块。</para>
        /// </summary>
        /// <param name="entryArrayProperty">条目数组属性。</param>
        /// <param name="dependencyType">依赖类型。</param>
        /// <param name="excludedIndexA">第一个排除索引。</param>
        /// <param name="excludedIndexB">第二个排除索引。</param>
        private static bool ContainsModuleForDependency(
            SerializedProperty entryArrayProperty,
            Type dependencyType,
            int excludedIndexA,
            int excludedIndexB)
        {
            if (entryArrayProperty == null || dependencyType == null)
            {
                return false;
            }

            for (int i = 0; i < entryArrayProperty.arraySize; i++)
            {
                if (i == excludedIndexA || i == excludedIndexB)
                {
                    continue;
                }

                if (!TryGetModuleType(entryArrayProperty.GetArrayElementAtIndex(i), out var existingType))
                {
                    continue;
                }

                if (dependencyType.IsAssignableFrom(existingType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>尝试获取依赖。</para>
        /// </summary>
        /// <param name="moduleType">模块类型。</param>
        /// <param name="dependencies">依赖。</param>
        /// <param name="error">错误。</param>
        private static bool TryGetDependencies(
            Type moduleType,
            out Type[] dependencies,
            out string error)
        {
            dependencies = Array.Empty<Type>();
            error = null;

            try
            {
                dependencies = GameModuleDependencyUtility.GetDependencies(moduleType);
            }
            catch (Exception ex)
            {
                error = $"Failed to inspect dependencies for {GameModuleUtility.GetTypeDisplayName(moduleType)}. {ex.Message}";
                return false;
            }

            return true;
        }
    }
}

#endif