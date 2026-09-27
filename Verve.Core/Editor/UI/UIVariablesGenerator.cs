#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using Microsoft.CSharp;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    ///   <para>UI 变量定义。</para>
    /// </summary>
    internal readonly struct UIVariableDefinition
    {
        /// <summary>
        ///   <para>名称。</para>
        /// </summary>
        internal string Name { get; }
        /// <summary>
        ///   <para>类型。</para>
        /// </summary>
        internal Type Type { get; }
        /// <summary>
        ///   <para>事件标识列表。</para>
        /// </summary>
        internal IReadOnlyList<string> EventIds { get; }

        /// <summary>
        ///   <para>创建 UI 变量定义。</para>
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="type">类型。</param>
        /// <param name="eventIds">事件标识列表。</param>
        internal UIVariableDefinition(
            string name,
            Type type,
            IReadOnlyList<string> eventIds = null)
        {
            Name = name;
            Type = type;
            EventIds = eventIds ?? Array.Empty<string>();
        }
    }

    /// <summary>
    ///   <para>UI 变量代码生成器；按组件配置生成 Unity 对象访问代码。</para>
    /// </summary>
    internal static class UIVariablesGenerator
    {
        /// <summary>
        ///   <para>输出后缀。</para>
        /// </summary>
        internal const string OutputSuffix = ".Variables.cs";
        /// <summary>
        ///   <para>标识符提供者。</para>
        /// </summary>
        private static readonly CSharpCodeProvider IdentifierProvider = new();
        /// <summary>
        ///   <para>已生成属性正则表达式。</para>
        /// </summary>
        private static readonly Regex GeneratedPropertyRegex = new(
            @"(?m)^\s*private\s+[^\r\n;]+\s+(?<name>[A-Za-z_]\w*)\s*=>\s*GetVariable\s*<",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary>
        ///   <para>已生成事件方法正则表达式。</para>
        /// </summary>
        private static readonly Regex GeneratedEventMethodRegex = new(
            @"(?m)^\s*(?:private\s+void|partial\s+void)\s+(?<name>[A-Za-z_]\w*)\s*\(",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        /// <summary>
        ///   <para>已生成成员缓存。</para>
        /// </summary>
        private static readonly Dictionary<Type, GeneratedMemberCacheEntry> GeneratedMemberCache = new();
        /// <summary>
        ///   <para>类型脚本缓存。</para>
        /// </summary>
        private static readonly Dictionary<Type, MonoScript> TypeScriptCache = new();

        /// <summary>
        ///   <para>判断是否为有效标识符。</para>
        /// </summary>
        /// <param name="value">值。</param>
        internal static bool IsValidIdentifier(string value) => !string.IsNullOrEmpty(value) && IdentifierProvider.IsValidIdentifier(value);

        /// <summary>
        ///   <para>创建标识符。</para>
        /// </summary>
        /// <param name="value">值。</param>
        internal static string CreateIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Variable";
            }

            var builder = new StringBuilder(value.Length + 1);
            var capitalize = true;
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (!char.IsLetterOrDigit(character))
                {
                    capitalize = true;
                    continue;
                }

                if (builder.Length == 0 && char.IsDigit(character))
                {
                    builder.Append('_');
                }

                builder.Append(capitalize ? char.ToUpperInvariant(character) : character);
                capitalize = false;
            }

            var identifier = builder.Length == 0 ? "Variable" : builder.ToString();
            return IsValidIdentifier(identifier) ? identifier : "Variable";
        }

        /// <summary>
        ///   <para>生成。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="variables">变量。</param>
        /// <param name="signature">签名。</param>
        internal static void Generate(
            Type targetType,
            IReadOnlyList<UIVariableDefinition> variables,
            int signature)
        {
            var script = FindTypeScript(targetType);
            if (script == null)
            {
                throw new InvalidOperationException($"找不到 {targetType.FullName} 对应的源脚本。");
            }

            var scriptPath = AssetDatabase.GetAssetPath(script);
            EnsurePartialType(scriptPath, targetType);

            var outputPath = GetOutputPath(scriptPath, OutputSuffix);
            var source = BuildSource(targetType, variables, signature);
            GeneratedMemberCache.Remove(targetType);
            CoreEditorUtility.WriteTextAsset(outputPath, source, new UTF8Encoding(false));
        }

        /// <summary>
        ///   <para>生成源代码。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="variables">变量。</param>
        /// <param name="signature">签名。</param>
        internal static string BuildSource(
            Type targetType,
            IReadOnlyList<UIVariableDefinition> variables,
            int signature)
        {
            var builder = new StringBuilder(512);
            builder.AppendLine("// <auto-generated />");

            var hasNamespace = !string.IsNullOrEmpty(targetType.Namespace);
            var indent = hasNamespace ? "    " : string.Empty;
            if (hasNamespace)
            {
                builder.Append("namespace ").Append(targetType.Namespace).AppendLine();
                builder.AppendLine("{");
            }

            builder.Append(indent)
                .Append(GetAccessibility(targetType)).Append("partial class ")
                .Append(targetType.Name).AppendLine();
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).Append("    protected override int VariableSignature => ")
                .Append(signature).AppendLine(";");
            for (var i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                var typeName = GetTypeName(variable.Type);
                builder.Append(indent).Append("    private ").Append(typeName).Append(' ').Append(variable.Name)
                    .Append(" => GetVariable<").Append(typeName).Append(">(").Append(i).AppendLine(");");
            }

            builder.Append(indent).AppendLine("}");
            if (hasNamespace)
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        /// <summary>
        ///   <para>获取可访问性。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static string GetAccessibility(Type type) => type.IsPublic ? "public " : "internal ";

        /// <summary>
        ///   <para>获取类型名称。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        internal static string GetTypeName(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (type.IsByRef)
            {
                return GetTypeName(type.GetElementType()) + "&";
            }

            if (type.IsArray)
            {
                return GetTypeName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericType)
            {
                var genericName = type.GetGenericTypeDefinition().FullName;
                if (string.IsNullOrEmpty(genericName))
                {
                    throw new InvalidOperationException($"无法为类型 {type} 生成有效的 C# 类型名。");
                }

                var tickIndex = genericName.IndexOf('`');
                var builder = new StringBuilder(
                    (tickIndex < 0 ? genericName : genericName.Substring(0, tickIndex)).Replace('+', '.'));
                builder.Append('<');
                var arguments = type.GetGenericArguments();
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(GetTypeName(arguments[i]));
                }

                return builder.Append('>').ToString();
            }

            var fullName = type.FullName;
            if (string.IsNullOrEmpty(fullName))
            {
                throw new InvalidOperationException($"无法为类型 {type} 生成有效的 C# 类型名。");
            }

            return fullName.Replace('+', '.');
        }

        /// <summary>
        ///   <para>查找类型对应的用户源脚本，排除自动生成文件。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        internal static MonoScript FindTypeScript(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (TypeScriptCache.TryGetValue(type, out var cached) &&
                cached != null &&
                cached.GetClass() == type &&
                !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(cached)))
            {
                return cached;
            }

            TypeScriptCache.Remove(type);

            var script = CoreEditorUtility.FindMonoScriptForType(type, path => !IsGeneratedPath(path));
            if (script != null) TypeScriptCache[type] = script;
            return script;
        }

        /// <summary>
        ///   <para>检查目标类型使用 partial 声明。</para>
        /// </summary>
        /// <param name="scriptPath">脚本路径。</param>
        /// <param name="targetType">目标类型。</param>
        private static void EnsurePartialType(string scriptPath, Type targetType)
        {
            var source = File.ReadAllText(scriptPath);
            if (!Regex.IsMatch(
                    source,
                    @"\bpartial\s+class\s+" + Regex.Escape(targetType.Name) + @"\b",
                    RegexOptions.CultureInvariant))
            {
                throw new InvalidOperationException(
                    $"生成变量代码前，必须将 {targetType.Name} 声明为 partial class。");
            }
        }

        /// <summary>
        ///   <para>获取输出路径。</para>
        /// </summary>
        /// <param name="scriptPath">脚本路径。</param>
        /// <param name="suffix">后缀。</param>
        internal static string GetOutputPath(string scriptPath, string suffix)
        {
            return Game.PathUtility.Normalize(Path.Combine(
                    Path.GetDirectoryName(scriptPath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(scriptPath) + suffix));
        }

        /// <summary>
        ///   <para>判断是否为已生成路径。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        internal static bool IsGeneratedPath(string path)
        {
            return path.EndsWith(OutputSuffix, StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(UIEventsGenerator.OutputSuffix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///   <para>判断成员是否由当前变量生成器产生。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="memberName">成员名称。</param>
        internal static bool IsGeneratedMember(Type targetType, string memberName)
        {
            if (targetType == null || string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            // 变量和事件生成文件中的成员都由生成器管理，不能被当作用户成员冲突。
            return TryGetGeneratedMembers(targetType, out var generatedMembers) &&
                   generatedMembers.Contains(memberName);
        }

        /// <summary>
        ///   <para>尝试获取已生成成员集合。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="generatedMembers">已生成成员集合。</param>
        private static bool TryGetGeneratedMembers(
            Type targetType,
            out HashSet<string> generatedMembers)
        {
            generatedMembers = null;
            if (GeneratedMemberCache.TryGetValue(targetType, out var cached) &&
                IsGeneratedFileCurrent(cached.VariablePath, cached.VariableWriteTicks) &&
                IsGeneratedFileCurrent(cached.EventPath, cached.EventLastWriteTicks))
            {
                generatedMembers = cached.Names;
                return true;
            }

            var script = FindTypeScript(targetType);
            if (script == null)
            {
                GeneratedMemberCache.Remove(targetType);
                return false;
            }

            var scriptPath = AssetDatabase.GetAssetPath(script);
            var generatedPath = GetOutputPath(scriptPath, OutputSuffix);
            var eventPath = GetOutputPath(scriptPath, UIEventsGenerator.OutputSuffix);
            if (!File.Exists(generatedPath) && !File.Exists(eventPath))
            {
                GeneratedMemberCache.Remove(targetType);
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            if (File.Exists(generatedPath))
            {
                var matches = GeneratedPropertyRegex.Matches(File.ReadAllText(generatedPath));
                for (var i = 0; i < matches.Count; i++)
                {
                    names.Add(matches[i].Groups["name"].Value);
                }
            }

            if (File.Exists(eventPath))
            {
                var matches = GeneratedEventMethodRegex.Matches(File.ReadAllText(eventPath));
                for (var i = 0; i < matches.Count; i++)
                {
                    names.Add(matches[i].Groups["name"].Value);
                }
            }

            GeneratedMemberCache[targetType] = new GeneratedMemberCacheEntry(
                generatedPath,
                File.Exists(generatedPath) ? File.GetLastWriteTimeUtc(generatedPath).Ticks : 0,
                eventPath,
                File.Exists(eventPath) ? File.GetLastWriteTimeUtc(eventPath).Ticks : 0,
                names);
            generatedMembers = names;
            return true;
        }

        /// <summary>
        ///   <para>判断生成文件是否仍与配置一致。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        /// <param name="lastWriteTicks">文件修改时间刻度。</param>
        private static bool IsGeneratedFileCurrent(string path, long lastWriteTicks)
        {
            var exists = File.Exists(path);
            return exists
                ? File.GetLastWriteTimeUtc(path).Ticks == lastWriteTicks
                : lastWriteTicks == 0;
        }

        /// <summary>
        ///   <para>计算签名。</para>
        /// </summary>
        /// <param name="variables">变量。</param>
        internal static int CalculateSignature(IReadOnlyList<UIVariableDefinition> variables)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < variables.Count; i++)
                {
                    hash = hash * 31 + StableHash(variables[i].Name);
                    hash = hash * 31 + StableHash(variables[i].Type.AssemblyQualifiedName);
                }

                return hash;
            }
        }

        /// <summary>
        ///   <para>计算稳定哈希。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 23;
                for (var i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        /// <summary>
        ///   <para>生成成员缓存项。</para>
        /// </summary>
        private readonly struct GeneratedMemberCacheEntry
        {
            /// <summary>
            ///   <para>变量路径。</para>
            /// </summary>
            internal readonly string VariablePath;
            /// <summary>
            ///   <para>变量文件修改时间刻度。</para>
            /// </summary>
            internal readonly long VariableWriteTicks;
            /// <summary>
            ///   <para>事件路径。</para>
            /// </summary>
            internal readonly string EventPath;
            /// <summary>
            ///   <para>事件文件修改时间刻度。</para>
            /// </summary>
            internal readonly long EventLastWriteTicks;
            /// <summary>
            ///   <para>名称。</para>
            /// </summary>
            internal readonly HashSet<string> Names;

            /// <summary>
            ///   <para>创建生成成员缓存项。</para>
            /// </summary>
            /// <param name="variablePath">变量路径。</param>
            /// <param name="variableWriteTicks">变量文件修改时间刻度。</param>
            /// <param name="eventPath">事件路径。</param>
            /// <param name="eventLastWriteTicks">事件文件修改时间刻度。</param>
            /// <param name="names">名称。</param>
            internal GeneratedMemberCacheEntry(
                string variablePath,
                long variableWriteTicks,
                string eventPath,
                long eventLastWriteTicks,
                HashSet<string> names)
            {
                VariablePath = variablePath;
                VariableWriteTicks = variableWriteTicks;
                EventPath = eventPath;
                EventLastWriteTicks = eventLastWriteTicks;
                Names = names;
            }
        }
    }
}

#endif
