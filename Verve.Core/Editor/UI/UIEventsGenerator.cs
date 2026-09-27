#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine.Events;
    using System.Reflection;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>UI 事件定义；描述可绑定的 <see cref="UnityEngine.Events.UnityEvent"/> 成员。</para>
    /// </summary>
    internal readonly struct UIEventDefinition
    {
        /// <summary>
        ///   <para>名称。</para>
        /// </summary>
        internal string Name { get; }
        /// <summary>
        ///   <para>参数类型。</para>
        /// </summary>
        internal Type[] ParameterTypes { get; }
        /// <summary>
        ///   <para>显示名称。</para>
        /// </summary>
        internal string DisplayName { get; }
        /// <summary>
        ///   <para>方法后缀。</para>
        /// </summary>
        internal string MethodSuffix { get; }

        /// <summary>
        ///   <para>创建 UI 事件定义。</para>
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="parameterTypes">参数类型。</param>
        internal UIEventDefinition(string name, Type[] parameterTypes)
        {
            Name = name;
            ParameterTypes = parameterTypes ?? Array.Empty<Type>();
            DisplayName = CreateDisplayName(name, ParameterTypes);
            MethodSuffix = CreateMethodSuffix(name);
        }

        /// <summary>
        ///   <para>创建显示名称。</para>
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="parameterTypes">参数类型。</param>
        private static string CreateDisplayName(string name, IReadOnlyList<Type> parameterTypes)
        {
            if (parameterTypes.Count == 0)
            {
                return name;
            }

            var builder = new StringBuilder(name).Append(" (");
            for (var i = 0; i < parameterTypes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(UIVariablesGenerator.GetTypeName(parameterTypes[i]));
            }

            return builder.Append(')').ToString();
        }

        /// <summary>
        ///   <para>创建方法后缀。</para>
        /// </summary>
        /// <param name="name">名称。</param>
        private static string CreateMethodSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Event";
            }

            var start = name.StartsWith("on", StringComparison.Ordinal) && name.Length > 2 ? 2 : 0;
            var builder = new StringBuilder(name.Length - start);
            var capitalize = true;
            for (var i = start; i < name.Length; i++)
            {
                var character = name[i];
                if (!char.IsLetterOrDigit(character))
                {
                    capitalize = true;
                    continue;
                }

                builder.Append(capitalize ? char.ToUpperInvariant(character) : character);
                capitalize = false;
            }

            return builder.Length == 0 ? "Event" : builder.ToString();
        }
    }

    /// <summary>
    ///   <para>UI 事件代码生成器；按组件配置生成事件绑定。</para>
    /// </summary>
    internal static class UIEventsGenerator
    {
        /// <summary>
        ///   <para>输出后缀。</para>
        /// </summary>
        internal const string OutputSuffix = ".Events.cs";
        /// <summary>
        ///   <para>添加监听器。</para>
        /// </summary>
        private const string AddListener = "AddListener";
        /// <summary>
        ///   <para>移除监听器。</para>
        /// </summary>
        private const string RemoveListener = "RemoveListener";
        /// <summary>
        ///   <para>签名版本。</para>
        /// </summary>
        private const int SignatureVersion = 1;
        /// <summary>
        ///   <para>公开实例成员反射标记。</para>
        /// </summary>
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        /// <summary>
        ///   <para>空定义。</para>
        /// </summary>
        private static readonly IReadOnlyList<UIEventDefinition> EmptyDefinitions =
            Array.Empty<UIEventDefinition>();
        /// <summary>
        ///   <para>定义缓存。</para>
        /// </summary>
        private static readonly Dictionary<Type, IReadOnlyList<UIEventDefinition>> DefinitionCache =
            new();
        /// <summary>
        ///   <para><see cref="UnityEngine.Events.UnityAction"/> 类型名前缀。</para>
        /// </summary>
        private static readonly string UnityActionPrefix =
            typeof(UnityAction).Namespace + ".UnityAction`";

        /// <summary>
        ///   <para>生成或移除目标类型的事件文件。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="variables">变量。</param>
        internal static void Generate(Type targetType, IReadOnlyList<UIVariableDefinition> variables)
        {
            var script = UIVariablesGenerator.FindTypeScript(targetType);
            if (script == null)
            {
                throw new InvalidOperationException($"找不到 {targetType.FullName} 对应的源脚本。");
            }

            var scriptPath = AssetDatabase.GetAssetPath(script);
            var outputPath = UIVariablesGenerator.GetOutputPath(scriptPath, OutputSuffix);
            var bindings = CreateEventBindings(targetType, variables);
            if (bindings.Count == 0)
            {
                if (File.Exists(outputPath))
                {
                    AssetDatabase.DeleteAsset(outputPath);
                }

                return;
            }

            CoreEditorUtility.WriteTextAsset(outputPath, BuildSource(targetType, bindings), new UTF8Encoding(false));
        }

        /// <summary>
        ///   <para>生成事件绑定 partial 类源码。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="variables">变量。</param>
        internal static string BuildSource(Type targetType, IReadOnlyList<UIVariableDefinition> variables) => BuildSource(targetType, CreateEventBindings(targetType, variables));

        /// <summary>
        ///   <para>生成源代码。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="bindings">绑定。</param>
        private static string BuildSource(Type targetType, IReadOnlyList<EventBinding> bindings)
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
                .Append(targetType.IsPublic ? "public " : "internal ")
                .Append("partial class ").Append(targetType.Name).AppendLine();
            builder.Append(indent).AppendLine("{");
            builder.Append(indent).AppendLine("    protected override void OnBindEvents()");
            builder.Append(indent).AppendLine("    {");
            builder.Append(indent).AppendLine("        base.OnBindEvents();");
            AppendListenerCalls(builder, bindings, AddListener, indent);
            builder.Append(indent).AppendLine("    }");
            builder.AppendLine();
            builder.Append(indent).AppendLine("    protected override void OnUnbindEvents()");
            builder.Append(indent).AppendLine("    {");
            AppendListenerCalls(builder, bindings, RemoveListener, indent);
            builder.Append(indent).AppendLine("        base.OnUnbindEvents();");
            builder.Append(indent).AppendLine("    }");
            builder.AppendLine();
            AppendEventMethods(builder, bindings, indent);
            builder.Append(indent).AppendLine("}");
            if (hasNamespace)
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        /// <summary>
        ///   <para>获取组件公开的可绑定 <see cref="UnityEngine.Events.UnityEvent"/> 成员。</para>
        /// </summary>
        /// <param name="componentType">组件类型。</param>
        internal static IReadOnlyList<UIEventDefinition> GetEventDefinitions(Type componentType)
        {
            if (componentType == null)
            {
                return EmptyDefinitions;
            }

            if (DefinitionCache.TryGetValue(componentType, out var definitions))
            {
                return definitions;
            }

            var result = new List<UIEventDefinition>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in componentType.GetFields(PublicInstance))
            {
                if (!TryCreateDefinition(field.Name, field.FieldType, out var definition) ||
                    !names.Add(field.Name))
                {
                    continue;
                }

                result.Add(definition);
            }

            foreach (var property in componentType.GetProperties(PublicInstance))
            {
                var getter = property.GetGetMethod();
                if (getter == null || property.GetIndexParameters().Length != 0 ||
                    !TryCreateDefinition(property.Name, property.PropertyType, out var definition) ||
                    !names.Add(property.Name))
                {
                    continue;
                }

                result.Add(definition);
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            definitions = result.Count == 0 ? EmptyDefinitions : result.ToArray();
            DefinitionCache[componentType] = definitions;
            return definitions;
        }

        /// <summary>
        ///   <para>计算事件配置签名，事件顺序与定义名称固定化。</para>
        /// </summary>
        /// <param name="variables">变量。</param>
        internal static int CalculateSignature(IReadOnlyList<UIVariableDefinition> variables)
        {
            unchecked
            {
                // 生成模板变更时递增版本，确保 Inspector 能提示重新生成旧事件文件。
                var hash = UIVariablesGenerator.CalculateSignature(variables) * 31 + SignatureVersion;
                // 签名只描述配置，不依赖用户脚本当前是否存在同名方法。
                var bindings = CreateEventBindings(null, variables);
                for (var i = 0; i < bindings.Count; i++)
                {
                    var definition = bindings[i].Definition;
                    hash = hash * 31 + StableHash(definition.Name);
                    for (var parameterIndex = 0;
                         parameterIndex < definition.ParameterTypes.Length;
                         parameterIndex++)
                    {
                        hash = hash * 31 +
                               StableHash(definition.ParameterTypes[parameterIndex].AssemblyQualifiedName);
                    }
                }

                return hash;
            }
        }

        /// <summary>
        ///   <para>尝试创建定义。</para>
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="eventType">事件类型。</param>
        /// <param name="definition">定义。</param>
        private static bool TryCreateDefinition(
            string name,
            Type eventType,
            out UIEventDefinition definition)
        {
            definition = default;
            if (!typeof(UnityEventBase).IsAssignableFrom(eventType) ||
                !TryGetParameterTypes(eventType, out var parameterTypes))
            {
                return false;
            }

            definition = new UIEventDefinition(name, parameterTypes);
            return true;
        }

        /// <summary>
        ///   <para>尝试获取参数类型。</para>
        /// </summary>
        /// <param name="eventType">事件类型。</param>
        /// <param name="parameterTypes">参数类型。</param>
        private static bool TryGetParameterTypes(Type eventType, out Type[] parameterTypes)
        {
            parameterTypes = Array.Empty<Type>();
            foreach (var method in eventType.GetMethods(PublicInstance))
            {
                if (!string.Equals(method.Name, AddListener, StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }

                var actionType = parameters[0].ParameterType;
                if (eventType.GetMethod(
                        RemoveListener,
                        PublicInstance,
                        binder: null,
                        types: new[] { actionType },
                        modifiers: null) == null)
                {
                    continue;
                }

                if (actionType == typeof(UnityAction))
                {
                    return true;
                }

                var actionName = actionType.IsGenericType
                    ? actionType.GetGenericTypeDefinition().FullName
                    : null;
                if (actionName == null ||
                    !actionName.StartsWith(UnityActionPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                parameterTypes = actionType.GetGenericArguments();
                return parameterTypes.Length > 0;
            }

            return false;
        }

        /// <summary>
        ///   <para>创建事件绑定。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="variables">变量。</param>
        private static List<EventBinding> CreateEventBindings(
            Type targetType,
            IReadOnlyList<UIVariableDefinition> variables)
        {
            var bindings = new List<EventBinding>();
            if (variables == null)
            {
                return bindings;
            }

            var usedMethodNames = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                var eventIds = variable.EventIds;
                if (eventIds == null || eventIds.Count == 0)
                {
                    continue;
                }

                var definitions = GetEventDefinitions(variable.Type);
                var methodName = UIVariablesGenerator.CreateIdentifier(variable.Name);
                for (var j = 0; j < definitions.Count; j++)
                {
                    var definition = definitions[j];
                    if (!UIComponentEditorUtility.Contains(eventIds, definition.Name))
                    {
                        continue;
                    }

                    var suffix = MakeUniqueSuffix(
                        targetType,
                        methodName,
                        definition.MethodSuffix,
                        usedMethodNames);
                    bindings.Add(new EventBinding(variable, definition, methodName, suffix));
                }
            }

            return bindings;
        }

        /// <summary>
        ///   <para>创建唯一后缀。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="methodName">方法名称。</param>
        /// <param name="baseSuffix">基类后缀。</param>
        /// <param name="usedMethodNames">已使用方法名称集合。</param>
        private static string MakeUniqueSuffix(
            Type targetType,
            string methodName,
            string baseSuffix,
            HashSet<string> usedMethodNames)
        {
            var suffix = baseSuffix;
            var index = 2;
            while (true)
            {
                var handlerName = "Handle" + methodName + suffix;
                var callbackName = "On" + methodName + suffix;
                if (!usedMethodNames.Contains(handlerName) &&
                    !usedMethodNames.Contains(callbackName) &&
                    !HasUserMember(targetType, callbackName) &&
                    !HasUserMember(targetType, handlerName))
                {
                    usedMethodNames.Add(handlerName);
                    usedMethodNames.Add(callbackName);
                    return suffix;
                }

                suffix = baseSuffix + index++;
            }
        }

        /// <summary>
        ///   <para>判断是否包含用户成员。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="name">名称。</param>
        private static bool HasUserMember(Type targetType, string name)
        {
            if (targetType == null || string.IsNullOrEmpty(name))
            {
                return false;
            }

            // 编辑器工具已排除当前 Variables/Events 生成文件中的成员。
            return UIComponentEditorUtility.HasMemberConflict(targetType, name);
        }

        /// <summary>
        ///   <para>追加监听器调用。</para>
        /// </summary>
        /// <param name="builder">构建器。</param>
        /// <param name="bindings">绑定。</param>
        /// <param name="operation">操作。</param>
        /// <param name="indent">缩进。</param>
        private static void AppendListenerCalls(
            StringBuilder builder,
            IReadOnlyList<EventBinding> bindings,
            string operation,
            string indent)
        {
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                builder.Append(indent).Append("        ")
                    .Append(binding.Variable.Name).Append('.')
                    .Append(binding.Definition.Name).Append('.')
                    .Append(operation).Append('(')
                    .Append(binding.HandlerName).AppendLine(");");
            }
        }

        /// <summary>
        ///   <para>追加事件方法。</para>
        /// </summary>
        /// <param name="builder">构建器。</param>
        /// <param name="bindings">绑定。</param>
        /// <param name="indent">缩进。</param>
        private static void AppendEventMethods(
            StringBuilder builder,
            IReadOnlyList<EventBinding> bindings,
            string indent)
        {
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                var definition = binding.Definition;
                builder.Append(indent).Append("    private void ").Append(binding.HandlerName).Append('(');
                AppendParameters(builder, definition.ParameterTypes, "value");
                builder.AppendLine(")");
                builder.Append(indent).AppendLine("    {");
                builder.Append(indent).Append("        ").Append(binding.CallbackName).Append('(');
                for (var parameterIndex = 0; parameterIndex < definition.ParameterTypes.Length; parameterIndex++)
                {
                    if (parameterIndex > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append("value").Append(parameterIndex);
                }

                builder.AppendLine(");");
                builder.Append(indent).AppendLine("    }");
                builder.Append(indent).Append("    partial void ").Append(binding.CallbackName).Append('(');
                AppendParameters(builder, definition.ParameterTypes, "value");
                builder.AppendLine(");");
            }
        }

        /// <summary>
        ///   <para>追加参数。</para>
        /// </summary>
        /// <param name="builder">构建器。</param>
        /// <param name="parameterTypes">参数类型。</param>
        /// <param name="name">名称。</param>
        private static void AppendParameters(
            StringBuilder builder,
            IReadOnlyList<Type> parameterTypes,
            string name)
        {
            for (var i = 0; i < parameterTypes.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(UIVariablesGenerator.GetTypeName(parameterTypes[i]))
                    .Append(' ').Append(name).Append(i);
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
                if (value == null)
                {
                    return hash;
                }

                for (var i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        /// <summary>
        ///   <para>事件绑定。</para>
        /// </summary>
        private readonly struct EventBinding
        {
            /// <summary>
            ///   <para>变量。</para>
            /// </summary>
            internal UIVariableDefinition Variable { get; }
            /// <summary>
            ///   <para>定义。</para>
            /// </summary>
            internal UIEventDefinition Definition { get; }
            /// <summary>
            ///   <para>处理函数名称。</para>
            /// </summary>
            internal string HandlerName { get; }
            /// <summary>
            ///   <para>回调名称。</para>
            /// </summary>
            internal string CallbackName { get; }

            /// <summary>
            ///   <para>创建事件绑定。</para>
            /// </summary>
            /// <param name="variable">变量。</param>
            /// <param name="definition">定义。</param>
            /// <param name="methodName">方法名称。</param>
            /// <param name="suffix">后缀。</param>
            internal EventBinding(
                UIVariableDefinition variable,
                UIEventDefinition definition,
                string methodName,
                string suffix)
            {
                Variable = variable;
                Definition = definition;
                HandlerName = "Handle" + methodName + suffix;
                CallbackName = "On" + methodName + suffix;
            }
        }
    }
}

#endif
