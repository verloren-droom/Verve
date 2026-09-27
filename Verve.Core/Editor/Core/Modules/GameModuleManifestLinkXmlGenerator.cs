#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Text;
    using System.Collections;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Build;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEditor.Build.Reporting;
    
    /// <summary>
    ///   <para>模块清单链接配置生成器；保留反射创建的类型。</para>
    /// </summary>
    sealed class GameModuleManifestLinkXmlGenerator : IPreprocessBuildWithReport
    {
        /// <summary>
        ///   <para>输出目录。</para>
        /// </summary>
        private const string k_OutputDirectory = "Assets/Verve.Generated";
        /// <summary>
        ///   <para>输出路径。</para>
        /// </summary>
        private const string k_OutputPath = k_OutputDirectory + "/GameModuleManifest.link.xml";
        /// <summary>
        ///   <para>最大保留图深度。</para>
        /// </summary>
        private const int k_MaxPreserveGraphDepth = 32;

        /// <summary>
        ///   <para>不含 BOM 的 UTF-8 编码。</para>
        /// </summary>
        private static readonly UTF8Encoding s_Utf8WithoutBom = new(false);

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => GenerateLinkXml();

        /// <summary>
        ///   <para>生成链接 XML。</para>
        /// </summary>
        private static void GenerateLinkXml()
        {
            var moduleTypes = FindManifestTypesToPreserve();
            CoreEditorUtility.WriteTextAsset(k_OutputPath, CoreEditorUtility.CreateLinkXml(moduleTypes), s_Utf8WithoutBom);
        }

        /// <summary>
        ///   <para>查找清单中需要保留的类型。</para>
        /// </summary>
        private static List<Type> FindManifestTypesToPreserve()
        {
            var result = new List<Type>();
            var seen = new HashSet<RuntimeTypeHandle>();
            var manifestGuids = AssetDatabase.FindAssets($"t:{nameof(GameModuleManifestAsset)}");
            for (int i = 0; i < manifestGuids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(manifestGuids[i]);
                var manifest = AssetDatabase.LoadAssetAtPath<GameModuleManifestAsset>(assetPath);
                if (manifest == null)
                {
                    continue;
                }

                AppendManifestTypesToPreserve(manifest, assetPath, result, seen);
            }

            result.Sort(static (left, right) =>
            {
                int assemblyComparison = string.CompareOrdinal(
                    left.Assembly.GetName().Name,
                    right.Assembly.GetName().Name);
                return assemblyComparison != 0
                    ? assemblyComparison
                    : string.CompareOrdinal(left.FullName, right.FullName);
            });
            return result;
        }

        /// <summary>
        ///   <para>添加清单中需要保留的类型。</para>
        /// </summary>
        /// <param name="manifest">清单。</param>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="result">结果。</param>
        /// <param name="seen">已访问。</param>
        private static void AppendManifestTypesToPreserve(
            GameModuleManifestAsset manifest,
            string assetPath,
            List<Type> result,
            HashSet<RuntimeTypeHandle> seen)
        {
            try
            {
                var descriptors = manifest.ToManifest().GetInstallDescriptors();
                using var factory = new GameModuleFactory();
                var visitedObjects = new HashSet<object>(ReferenceEqualityComparer<object>.Instance);
                foreach (var descriptor in descriptors)
                {
                    AppendTypeToPreserve(descriptor.ModuleType, result, seen, k_MaxPreserveGraphDepth);
                    AppendDependencyTypesToPreserve(descriptor.DependencyTypes, result, seen);
                    using var module = descriptor.CreateModule(factory);
                    descriptor.Configure?.Invoke(module);
                    AppendRuntimeValueTypesToPreserve(module, result, seen, visitedObjects, k_MaxPreserveGraphDepth);
                }
            }
            catch (Exception failure)
            {
                throw new BuildFailedException(
                    $"{nameof(GameModuleManifestAsset)} could not generate stripping data. asset={assetPath}, error={failure}");
            }
        }

        /// <summary>
        ///   <para>添加需要保留的依赖类型。</para>
        /// </summary>
        /// <param name="dependencies">依赖。</param>
        /// <param name="result">结果。</param>
        /// <param name="seen">已访问。</param>
        private static void AppendDependencyTypesToPreserve(
            Type[] dependencies,
            List<Type> result,
            HashSet<RuntimeTypeHandle> seen)
        {
            if (dependencies == null || dependencies.Length == 0)
            {
                return;
            }

            for (int i = 0; i < dependencies.Length; i++)
            {
                AppendTypeToPreserve(dependencies[i], result, seen, k_MaxPreserveGraphDepth);
            }
        }

        /// <summary>
        ///   <para>添加运行时字段值涉及的类型。</para>
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="result">结果。</param>
        /// <param name="seenTypes">已访问类型集合。</param>
        /// <param name="visitedObjects">已访问对象集合。</param>
        /// <param name="remainingDepth">剩余遍历深度。</param>
        private static void AppendRuntimeValueTypesToPreserve(
            object value,
            List<Type> result,
            HashSet<RuntimeTypeHandle> seenTypes,
            HashSet<object> visitedObjects,
            int remainingDepth)
        {
            if (value == null || remainingDepth <= 0)
            {
                return;
            }

            if (value is UnityEngine.Object)
            {
                return;
            }

            var valueType = value.GetType();
            AppendTypeToPreserve(valueType, result, seenTypes, remainingDepth);
            if (!valueType.IsValueType && !visitedObjects.Add(value))
            {
                return;
            }

            if (value is Array array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    AppendRuntimeValueTypesToPreserve(array.GetValue(i), result, seenTypes, visitedObjects, remainingDepth - 1);
                }

                return;
            }

            if (value is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    AppendRuntimeValueTypesToPreserve(list[i], result, seenTypes, visitedObjects, remainingDepth - 1);
                }

                return;
            }

            foreach (var field in GetSerializedFields(valueType))
            {
                AppendRuntimeValueTypesToPreserve(
                    field.GetValue(value),
                    result,
                    seenTypes,
                    visitedObjects,
                    remainingDepth - 1);
            }
        }

        /// <summary>
        ///   <para>添加需要保留的类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <param name="result">结果。</param>
        /// <param name="seen">已访问。</param>
        /// <param name="remainingDepth">剩余遍历深度。</param>
        private static void AppendTypeToPreserve(
            Type type,
            List<Type> result,
            HashSet<RuntimeTypeHandle> seen,
            int remainingDepth)
        {
            if (type == null || remainingDepth <= 0)
            {
                return;
            }

            if (type.IsArray)
            {
                AppendTypeToPreserve(type.GetElementType(), result, seen, remainingDepth);
                return;
            }

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var arguments = type.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    AppendTypeToPreserve(arguments[i], result, seen, remainingDepth - 1);
                }

                type = type.GetGenericTypeDefinition();
            }

            if (!ShouldPreserveType(type))
            {
                return;
            }

            if (seen.Add(type.TypeHandle))
            {
                result.Add(type);
            }

            foreach (var field in GetSerializedFields(type))
            {
                AppendTypeToPreserve(field.FieldType, result, seen, remainingDepth - 1);
            }
        }

        /// <summary>
        ///   <para>获取序列化字段。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static IEnumerable<FieldInfo> GetSerializedFields(Type type)
        {
            foreach (var field in Game.ReflectionUtility.EnumerateFields(type))
            {
                if (IsSerializedField(field)) yield return field;
            }
        }

        /// <summary>
        ///   <para>判断是否为序列化字段。</para>
        /// </summary>
        /// <param name="field">字段。</param>
        private static bool IsSerializedField(FieldInfo field)
        {
            if (field == null ||
                field.IsStatic ||
                field.IsInitOnly ||
                field.IsLiteral ||
                field.IsNotSerialized)
            {
                return false;
            }

            return field.IsPublic ||
                   field.GetCustomAttribute<SerializeField>() != null ||
                   field.GetCustomAttribute<SerializeReference>() != null;
        }

        /// <summary>
        ///   <para>判断是否需要保留类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool ShouldPreserveType(Type type)
        {
            if (type == null ||
                type.IsPrimitive ||
                type.IsEnum ||
                type.IsPointer ||
                type.IsByRef ||
                type.IsGenericParameter ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                typeof(Delegate).IsAssignableFrom(type) ||
                typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }

            var namespaceName = type.Namespace;
            if (!string.IsNullOrEmpty(namespaceName) &&
                (namespaceName == "System" ||
                 namespaceName.StartsWith("System.", StringComparison.Ordinal) ||
                 namespaceName == "UnityEngine" ||
                 namespaceName.StartsWith("UnityEngine.", StringComparison.Ordinal) ||
                 namespaceName == "UnityEditor" ||
                 namespaceName.StartsWith("UnityEditor.", StringComparison.Ordinal)))
            {
                return false;
            }

            return type.IsClass || type.IsValueType || type.IsInterface;
        }

    }
}

#endif