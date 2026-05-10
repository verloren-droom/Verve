#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using System.Collections.Generic;


    /// <summary>
    ///   <para>模块清单条目属性工具</para>
    /// </summary>
    internal static class GameModuleManifestEntryPropertyUtility
    {
        /// <summary>
        ///   <para>从模块条目属性中解析当前模块类型</para>
        /// </summary>
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
        ///   <para>判断模块条目列表是否已包含某个精确模块类型</para>
        /// </summary>
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
        ///   <para>判断模块条目列表是否已包含可用于当前依赖的模块类型</para>
        /// </summary>
        public static bool ContainsModuleForDependency(SerializedProperty entryArrayProperty, Type dependencyType)
        {
            return ContainsModuleForDependency(entryArrayProperty, dependencyType, -1, -1);
        }

        /// <summary>
        ///   <para>查找移除指定模块后会失去依赖满足项的模块类型</para>
        /// </summary>
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

                if (!TryGetDependencies(entryProperty, dependentType, out var dependencies, out error))
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

        private static bool TryGetDependencies(
            SerializedProperty entryProperty,
            Type moduleType,
            out Type[] dependencies,
            out string error)
        {
            dependencies = Array.Empty<Type>();
            error = null;

            var entry = GameModuleManifestEditorData.ReadModuleEntry(entryProperty);
            if (entry.module != null && !entry.usesFallbackModuleInstance)
            {
                try
                {
                    dependencies = GameModuleDependencyUtility.GetDependencies(entry.module);
                    return true;
                }
                catch (Exception ex)
                {
                    error =
                        $"Failed to inspect dependencies for {GameModuleUtility.GetTypeDisplayName(moduleType)}. {ex.Message}";
                    return false;
                }
            }

            if (!GameModuleDependencyUtility.TryInspectDependencies(
                    moduleType,
                    entry.fields,
                    out dependencies,
                    out var inspectError))
            {
                error =
                    $"Failed to inspect dependencies for {GameModuleUtility.GetTypeDisplayName(moduleType)}. {inspectError}";
                return false;
            }

            return true;
        }
    }
}

#endif