#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections;
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.Build;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEditor.Build.Reporting;


    /// <summary>
    ///   <para>为模块清单中的反射创建类型生成 UnityLinker 保留配置</para>
    /// </summary>
    sealed class GameModuleManifestLinkXmlGenerator : IPreprocessBuildWithReport
    {
        private const string k_OutputDirectory = "Assets/Verve.Generated";
        private const string k_OutputPath = k_OutputDirectory + "/GameModuleManifest.link.xml";
        private const int k_MaxPreserveGraphDepth = 32;

        private static readonly UTF8Encoding s_Utf8WithoutBom = new(false);

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            GenerateLinkXml();
        }

        private static void GenerateLinkXml()
        {
            var moduleTypes = FindManifestTypesToPreserve();
            Directory.CreateDirectory(k_OutputDirectory);
            File.WriteAllText(k_OutputPath, CreateLinkXml(moduleTypes), s_Utf8WithoutBom);
            AssetDatabase.ImportAsset(k_OutputPath, ImportAssetOptions.ForceUpdate);
        }

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

        private static void AppendManifestTypesToPreserve(
            GameModuleManifestAsset manifest,
            string assetPath,
            List<Type> result,
            HashSet<RuntimeTypeHandle> seen)
        {
            IReadOnlyList<GameModuleManifest.InstallItem> installItems = null;
            Exception failure = null;
            try
            {
                installItems = manifest.ToManifest().CreateInstallItems();
                var visitedObjects = new HashSet<object>(GameModuleUtility.ReferenceComparer<object>.Instance);
                for (int i = 0; i < installItems.Count; i++)
                {
                    var item = installItems[i];
                    AppendTypeToPreserve(item.moduleType, result, seen, k_MaxPreserveGraphDepth);
                    AppendDependencyTypesToPreserve(item.dependencies, result, seen);
                    AppendRuntimeValueTypesToPreserve(item.module, result, seen, visitedObjects, k_MaxPreserveGraphDepth);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                failure = GameModuleUtility.CombineErrors(
                    failure,
                    DisposeManifestInstallItems(installItems));
            }

            if (failure != null)
            {
                throw new BuildFailedException(
                    $"{nameof(GameModuleManifestAsset)} could not generate stripping data. asset={assetPath}, error={failure.Message}");
            }
        }

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

        private static Exception DisposeManifestInstallItems(
            IReadOnlyList<GameModuleManifest.InstallItem> installItems)
        {
            if (installItems == null || installItems.Count == 0)
            {
                return null;
            }

            Exception failure = null;
            for (int i = installItems.Count - 1; i >= 0; i--)
            {
                failure = GameModuleUtility.CombineErrors(
                    failure,
                    GameModuleUtility.DisposeModuleAndCreateFailure(
                        installItems[i].module,
                        "disposing module after link.xml generation"));
            }

            return failure;
        }

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

        private static IEnumerable<FieldInfo> GetSerializedFields(Type type)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                var fields = current.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (IsSerializedField(fields[i]))
                    {
                        yield return fields[i];
                    }
                }
            }
        }

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

        private static string CreateLinkXml(List<Type> moduleTypes)
        {
            var builder = new StringBuilder(1024);
            builder.AppendLine("<linker>");

            string currentAssembly = null;
            for (int i = 0; i < moduleTypes.Count; i++)
            {
                var moduleType = moduleTypes[i];
                var assemblyName = moduleType.Assembly.GetName().Name;
                if (!string.Equals(currentAssembly, assemblyName, StringComparison.Ordinal))
                {
                    if (currentAssembly != null)
                    {
                        builder.AppendLine("  </assembly>");
                    }

                    currentAssembly = assemblyName;
                    builder
                        .Append("  <assembly fullname=\"")
                        .Append(EscapeXml(assemblyName))
                        .AppendLine("\">");
                }

                builder
                    .Append("    <type fullname=\"")
                    .Append(EscapeXml(GetLinkerTypeName(moduleType)))
                    .AppendLine("\" preserve=\"all\" />");
            }

            if (currentAssembly != null)
            {
                builder.AppendLine("  </assembly>");
            }

            builder.AppendLine("</linker>");
            return builder.ToString();
        }

        private static string GetLinkerTypeName(Type type)
        {
            return (type.FullName ?? type.Name).Replace('+', '/');
        }

        private static string EscapeXml(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}

#endif