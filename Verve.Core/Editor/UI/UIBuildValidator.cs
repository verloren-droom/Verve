#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.IO;
    using UnityEngine;
    using System.Text;
    using UnityEditor;
    using System.Reflection;
    using UnityEditor.Build;
    using System.Collections.Generic;
    using UnityEditor.Build.Reporting;
    using System.Text.RegularExpressions;

    /// <summary>
    ///   <para>UI 构建校验器；检查页面元数据、预制体绑定和生成代码。</para>
    /// </summary>
    public sealed class UIBuildValidator : IPreprocessBuildWithReport
    {
        /// <inheritdoc />
        public int callbackOrder => -1000;

        /// <inheritdoc />
        public void OnPreprocessBuild(BuildReport report) => Validate();

        /// <summary>
        ///   <para>校验 UI 配置；失败时抛出异常。</para>
        /// </summary>
        public static void Validate()
        {
            var errors = new List<string>();
            var validatedPrefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var viewTypes = UIComponentEditorUtility.GetViewTypes();
            for (var i = 0; i < viewTypes.Count; i++)
            {
                var viewType = viewTypes[i];
                if (!IsRuntimeViewType(viewType))
                {
                    continue;
                }

                try
                {
                    ValidateView(viewType, validatedPrefabPaths, errors);
                }
                catch (Exception exception)
                {
                    errors.Add($"{viewType.FullName}: 校验异常：{exception.Message}");
                }
            }

            ValidateStandalonePrefabs(validatedPrefabPaths, errors);
            if (errors.Count == 0)
            {
                return;
            }

            var message = new StringBuilder("Verve UI 构建前校验失败：");
            for (var i = 0; i < errors.Count; i++)
            {
                message.AppendLine().Append("- ").Append(errors[i]);
            }

            throw new BuildFailedException(message.ToString());
        }

        /// <summary>
        ///   <para>校验页面。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="validatedPrefabPaths">已校验预制体路径。</param>
        /// <param name="errors">错误。</param>
        private static void ValidateView(
            Type viewType,
            HashSet<string> validatedPrefabPaths,
            List<string> errors)
        {
            var attribute = viewType.GetCustomAttribute<ViewConfigAttribute>(true);
            if (attribute == null || string.IsNullOrEmpty(attribute.AssetPath))
            {
                return;
            }

            var assetPath = Game.PathUtility.Normalize(attribute.AssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                errors.Add($"{viewType.FullName}: ViewConfig 指向的 Prefab 不存在：{assetPath}");
                return;
            }

            validatedPrefabPaths.Add(assetPath);

            var component = prefab.GetComponent<UIViewComponent>();
            if (component == null)
            {
                errors.Add($"{viewType.FullName}: Prefab 缺少根节点 {nameof(UIViewComponent)}：{assetPath}");
                return;
            }

            ValidateComponent(viewType, assetPath, component, errors);
            ValidateNestedWidgets(prefab, assetPath, component, errors);
        }

        /// <summary>
        ///   <para>校验独立预制体。</para>
        /// </summary>
        /// <param name="validatedPrefabPaths">已校验预制体路径。</param>
        /// <param name="errors">错误。</param>
        private static void ValidateStandalonePrefabs(
            HashSet<string> validatedPrefabPaths,
            List<string> errors)
        {
            var prefabPaths = GetRuntimePrefabPaths();
            for (var i = 0; i < prefabPaths.Count; i++)
            {
                var assetPath = prefabPaths[i];
                if (validatedPrefabPaths.Contains(assetPath))
                {
                    continue;
                }

                try
                {
                    ValidateStandalonePrefab(assetPath, errors);
                }
                catch (Exception exception)
                {
                    errors.Add($"{assetPath}: 校验异常：{exception.Message}");
                }
            }
        }

        /// <summary>
        ///   <para>获取运行时预制体路径。</para>
        /// </summary>
        private static List<string> GetRuntimePrefabPaths()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (IsRuntimeAssetPath(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
            return paths;
        }

        /// <summary>
        ///   <para>校验独立预制体。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="errors">错误。</param>
        private static void ValidateStandalonePrefab(string assetPath, List<string> errors)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                return;
            }

            var component = prefab.GetComponent<UIViewComponent>();
            if (component == null)
            {
                var widget = prefab.GetComponent<UIWidgetComponent>();
                if (widget != null)
                {
                    using var serializedWidget = new SerializedObject(widget);
                    var widgetType = UIComponentEditorUtility.ReadType(
                        serializedWidget.FindProperty(UIComponentEditorUtility.WidgetTypeNameField));
                    if (widgetType != null &&
                        IsEditorOrTestAssembly(widgetType.Assembly.GetName().Name))
                    {
                        return;
                    }

                    ValidateWidgetComponent(assetPath, widget, errors);
                    ValidateNestedWidgets(prefab, assetPath, widget, errors);
                }

                return;
            }

            using var serialized = new SerializedObject(component);
            var viewType = UIComponentEditorUtility.ReadType(
                serialized.FindProperty(UIComponentEditorUtility.ViewTypeNameField));
            if (viewType != null && typeof(ViewBase).IsAssignableFrom(viewType) &&
                IsEditorOrTestAssembly(viewType.Assembly.GetName().Name))
            {
                return;
            }

            if (!IsRuntimeViewType(viewType))
            {
                errors.Add($"{assetPath}: UIViewComponent 未绑定有效的具体 View 类型。");
                return;
            }

            ValidateComponent(viewType, assetPath, component, errors);
            ValidateNestedWidgets(prefab, assetPath, component, errors);
        }

        /// <summary>
        ///   <para>校验页面或部件节点树中的嵌套 <see cref="WidgetBase"/> 配置。</para>
        /// </summary>
        /// <param name="prefab">预制体。</param>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="rootComponent">根节点组件。</param>
        /// <param name="errors">错误。</param>
        private static void ValidateNestedWidgets(
            GameObject prefab,
            string assetPath,
            Component rootComponent,
            List<string> errors)
        {
            var widgets = prefab.GetComponentsInChildren<UIWidgetComponent>(true);
            for (var i = 0; i < widgets.Length; i++)
            {
                var widget = widgets[i];
                if (widget == null || ReferenceEquals(widget, rootComponent))
                {
                    continue;
                }

                using var serializedWidget = new SerializedObject(widget);
                var widgetType = UIComponentEditorUtility.ReadType(
                    serializedWidget.FindProperty(UIComponentEditorUtility.WidgetTypeNameField));
                if (widgetType != null && IsEditorOrTestAssembly(widgetType.Assembly.GetName().Name))
                {
                    continue;
                }

                ValidateWidgetComponent(assetPath, widget, errors);
            }
        }

        /// <summary>
        ///   <para>校验部件组件。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="component">组件。</param>
        /// <param name="errors">错误。</param>
        private static void ValidateWidgetComponent(
            string assetPath,
            UIWidgetComponent component,
            List<string> errors)
        {
            using var serialized = new SerializedObject(component);
            var typeProperty = serialized.FindProperty(UIComponentEditorUtility.WidgetTypeNameField);
            var variablesProperty = serialized.FindProperty(UIComponentEditorUtility.VariablesField);
            var variableSignatureProperty = serialized.FindProperty(UIComponentEditorUtility.VariableSignatureField);
            var eventSignatureProperty = serialized.FindProperty(UIComponentEditorUtility.EventSignatureField);
            if (typeProperty == null || variablesProperty == null ||
                variableSignatureProperty == null || eventSignatureProperty == null)
            {
                errors.Add($"UIWidgetComponent 序列化字段不完整，请重新导入包或 Prefab：{assetPath}");
                return;
            }

            var widgetType = UIComponentEditorUtility.ReadType(typeProperty);
            if (!IsRuntimeWidgetType(widgetType))
            {
                errors.Add($"{assetPath}: UIWidgetComponent 未绑定有效的具体 Widget 类型。");
                return;
            }

            var compositionError = UICompositionPolicy.GetCompositionError(component);
            if (compositionError != null)
            {
                errors.Add($"{widgetType.FullName}: {compositionError}：{assetPath}");
                return;
            }

            if (!TryReadVariables(
                    variablesProperty,
                    widgetType,
                    component,
                    assetPath,
                    errors,
                    out var variables))
            {
                return;
            }

            var hasStoredSignatures = variableSignatureProperty.intValue != 0 ||
                                       eventSignatureProperty.intValue != 0;
            var variableSignature = UIVariablesGenerator.CalculateSignature(variables);
            var eventSignature = UIEventsGenerator.CalculateSignature(variables);
            var signaturesValid = true;
            if ((variables.Count > 0 || hasStoredSignatures) &&
                variableSignature != variableSignatureProperty.intValue)
            {
                errors.Add($"{widgetType.FullName}: Widget 节点变量签名已过期，请重新生成代码：{assetPath}");
                signaturesValid = false;
            }

            if ((variables.Count > 0 || hasStoredSignatures) &&
                eventSignature != eventSignatureProperty.intValue)
            {
                errors.Add($"{widgetType.FullName}: Widget 控件事件签名已过期，请重新生成代码：{assetPath}");
                signaturesValid = false;
            }

            if (signaturesValid)
            {
                ValidateGeneratedFiles(widgetType, assetPath, variables, hasStoredSignatures, errors);
            }
        }

        /// <summary>
        ///   <para>校验组件。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="component">组件。</param>
        /// <param name="errors">错误。</param>
        private static void ValidateComponent(
            Type viewType,
            string assetPath,
            UIViewComponent component,
            List<string> errors)
        {
            using var serialized = new SerializedObject(component);
            var typeProperty = serialized.FindProperty(UIComponentEditorUtility.ViewTypeNameField);
            var variablesProperty = serialized.FindProperty(UIComponentEditorUtility.VariablesField);
            var variableSignatureProperty = serialized.FindProperty(UIComponentEditorUtility.VariableSignatureField);
            var eventSignatureProperty = serialized.FindProperty(UIComponentEditorUtility.EventSignatureField);
            if (typeProperty == null || variablesProperty == null ||
                variableSignatureProperty == null || eventSignatureProperty == null)
            {
                errors.Add($"{viewType.FullName}: UIViewComponent 序列化字段不完整，请重新导入包或 Prefab：{assetPath}");
                return;
            }

            var selectedType = UIComponentEditorUtility.ReadType(typeProperty);
            if (!ReferenceEquals(selectedType, viewType))
            {
                var selectedName = selectedType?.FullName ?? "未选择";
                errors.Add($"{viewType.FullName}: Prefab 的 View 类型为 {selectedName}，应为 {viewType.FullName}：{assetPath}");
                // 绑定类型不一致时，后续变量和生成文件属于另一个类型；继续校验只会产生误报。
                return;
            }

            var compositionError = UICompositionPolicy.GetCompositionError(component);
            if (compositionError != null)
            {
                errors.Add($"{viewType.FullName}: {compositionError}：{assetPath}");
                return;
            }

            if (!TryReadVariables(
                    variablesProperty,
                    viewType,
                    component,
                    assetPath,
                    errors,
                    out var variables))
            {
                return;
            }

            var hasStoredSignatures = variableSignatureProperty.intValue != 0 ||
                                       eventSignatureProperty.intValue != 0;
            var variableSignature = UIVariablesGenerator.CalculateSignature(variables);
            var eventSignature = UIEventsGenerator.CalculateSignature(variables);
            var signaturesValid = true;
            if ((variables.Count > 0 || hasStoredSignatures) &&
                variableSignature != variableSignatureProperty.intValue)
            {
                errors.Add($"{viewType.FullName}: 节点变量签名已过期，请重新生成代码：{assetPath}");
                signaturesValid = false;
            }

            if ((variables.Count > 0 || hasStoredSignatures) &&
                eventSignature != eventSignatureProperty.intValue)
            {
                errors.Add($"{viewType.FullName}: 控件事件签名已过期，请重新生成代码：{assetPath}");
                signaturesValid = false;
            }

            if (signaturesValid)
            {
                ValidateGeneratedFiles(
                    viewType,
                    assetPath,
                    variables,
                    hasStoredSignatures,
                    errors);
            }
        }

        /// <summary>
        ///   <para>尝试读取变量。</para>
        /// </summary>
        /// <param name="variablesProperty">变量属性。</param>
        /// <param name="ownerType">所有者类型。</param>
        /// <param name="owner">所有者。</param>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="errors">错误。</param>
        /// <param name="variables">变量。</param>
        private static bool TryReadVariables(
            SerializedProperty variablesProperty,
            Type ownerType,
            Component owner,
            string assetPath,
            List<string> errors,
            out List<UIVariableDefinition> variables)
        {
            var initialErrorCount = errors.Count;
            variables = new List<UIVariableDefinition>(variablesProperty.arraySize);
            var names = new HashSet<string>(StringComparer.Ordinal);
            var prefabRoot = owner == null ? null : owner.gameObject;
            for (var i = 0; i < variablesProperty.arraySize; i++)
            {
                var element = variablesProperty.GetArrayElementAtIndex(i);
                var name = element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField)?.stringValue;
                var rawValue = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField)?.objectReferenceValue;
                if (!UIVariablesGenerator.IsValidIdentifier(name))
                {
                    errors.Add($"{ownerType.FullName}: 第 {i + 1} 个变量名称无效：{assetPath}");
                    continue;
                }

                if (!names.Add(name))
                {
                    errors.Add($"{ownerType.FullName}: 变量名称重复：{name}：{assetPath}");
                    continue;
                }

                if (rawValue == null)
                {
                    errors.Add($"{ownerType.FullName}: 变量 {name} 未绑定引用对象：{assetPath}");
                    continue;
                }

                var value = rawValue as Component;
                var targetNode = UICompositionPolicy.GetReferenceNode(rawValue);
                if (targetNode != null && !UIComponentEditorUtility.IsOwnedObject(prefabRoot, targetNode))
                {
                    errors.Add($"{ownerType.FullName}: 变量 {name} 必须引用同一 Prefab 内的节点：{assetPath}");
                    continue;
                }

                var viewBindingError = UICompositionPolicy.GetBindingError(
                    owner,
                    targetNode);
                if (viewBindingError != null)
                {
                    errors.Add($"{ownerType.FullName}: 变量 {name} {viewBindingError}：{assetPath}");
                    continue;
                }

                if (UIComponentEditorUtility.HasMemberConflict(ownerType, name))
                {
                    errors.Add($"{ownerType.FullName}: 变量 {name} 与现有成员冲突：{assetPath}");
                }

                var eventIdsProperty = element.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField);
                var eventIds = UIComponentEditorUtility.ReadEventIds(eventIdsProperty);
                var definitions = value == null
                    ? Array.Empty<UIEventDefinition>()
                    : UIEventsGenerator.GetEventDefinitions(value.GetType());
                for (var eventIndex = 0; eventIndex < eventIds.Count; eventIndex++)
                {
                    if (!UIComponentEditorUtility.ContainsEvent(definitions, eventIds[eventIndex]))
                    {
                        errors.Add($"{ownerType.FullName}: 变量 {name} 选择了不存在的控件事件 {eventIds[eventIndex]}：{assetPath}");
                    }
                }

                variables.Add(new UIVariableDefinition(name, rawValue.GetType(), eventIds));
            }

            return errors.Count == initialErrorCount;
        }

        /// <summary>
        ///   <para>校验已生成文件。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="assetPath">资源路径。</param>
        /// <param name="variables">变量。</param>
        /// <param name="hasStoredSignatures">包含已保存签名。</param>
        /// <param name="errors">错误。</param>
        private static void ValidateGeneratedFiles(
            Type targetType,
            string assetPath,
            IReadOnlyList<UIVariableDefinition> variables,
            bool hasStoredSignatures,
            List<string> errors)
        {
            var script = UIVariablesGenerator.FindTypeScript(targetType);
            if (script == null)
            {
                errors.Add($"{targetType.FullName}: 找不到类型源脚本：{assetPath}");
                return;
            }

            var scriptPath = AssetDatabase.GetAssetPath(script);
            var variablePath = UIVariablesGenerator.GetOutputPath(
                scriptPath,
                UIVariablesGenerator.OutputSuffix);
            var eventPath = UIVariablesGenerator.GetOutputPath(
                scriptPath,
                UIEventsGenerator.OutputSuffix);

            var hasEvents = HasSelectedEvents(variables);
            var hasVariableFile = variables.Count > 0 || hasStoredSignatures || File.Exists(variablePath);
            if (hasVariableFile || hasEvents)
            {
                var source = File.ReadAllText(scriptPath);
                if (!Regex.IsMatch(
                        source,
                        @"\bpartial\s+class\s+" + Regex.Escape(targetType.Name) + @"\b",
                        RegexOptions.CultureInvariant))
                {
                    errors.Add($"{targetType.FullName}: 源脚本必须声明 partial class：{scriptPath}");
                }
            }

            if (hasVariableFile)
            {
                ValidateGeneratedSource(
                    variablePath,
                    UIVariablesGenerator.BuildSource(
                        targetType,
                        variables,
                        UIVariablesGenerator.CalculateSignature(variables)),
                    targetType.FullName,
                    "节点变量",
                    errors);
            }

            if (hasEvents)
            {
                ValidateGeneratedSource(
                    eventPath,
                    UIEventsGenerator.BuildSource(targetType, variables),
                    targetType.FullName,
                    "控件事件",
                    errors);
            }
            else if (File.Exists(eventPath))
            {
                errors.Add($"{targetType.FullName}: 没有绑定事件但仍存在过期生成文件：{eventPath}");
            }
        }

        /// <summary>
        ///   <para>校验已生成源。</para>
        /// </summary>
        /// <param name="path">路径。</param>
        /// <param name="expected">预期。</param>
        /// <param name="viewTypeName">页面类型名称。</param>
        /// <param name="category">分类。</param>
        /// <param name="errors">错误。</param>
        private static void ValidateGeneratedSource(
            string path,
            string expected,
            string viewTypeName,
            string category,
            List<string> errors)
        {
            if (!File.Exists(path))
            {
                errors.Add($"{viewTypeName}: 缺少{category}生成文件：{path}");
                return;
            }

            var actual = NormalizeLineEndings(File.ReadAllText(path));
            if (!string.Equals(actual, NormalizeLineEndings(expected), StringComparison.Ordinal))
            {
                errors.Add($"{viewTypeName}: {category}生成文件已过期，请重新生成：{path}");
            }
        }

        /// <summary>
        ///   <para>判断是否包含选中事件。</para>
        /// </summary>
        /// <param name="variables">变量。</param>
        private static bool HasSelectedEvents(IReadOnlyList<UIVariableDefinition> variables)
        {
            for (var i = 0; i < variables.Count; i++)
            {
                if (variables[i].EventIds.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>统一换行符。</para>
        /// </summary>
        /// <param name="source">源。</param>
        private static string NormalizeLineEndings(string source) => source?.Replace("\r\n", "\n").Replace('\r', '\n');

        /// <summary>
        ///   <para>判断是否为运行时页面类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsRuntimeViewType(Type type)
        {
            if (!UIComponentEditorUtility.IsConcreteViewType(type))
            {
                return false;
            }

            var assemblyName = type.Assembly.GetName().Name;
            if (string.IsNullOrEmpty(assemblyName))
            {
                return false;
            }

            return !IsEditorOrTestAssembly(assemblyName);
        }

        /// <summary>
        ///   <para>判断是否为运行时部件类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private static bool IsRuntimeWidgetType(Type type)
        {
            if (!UIComponentEditorUtility.IsConcreteWidgetType(type))
            {
                return false;
            }

            var assemblyName = type.Assembly.GetName().Name;
            return !string.IsNullOrEmpty(assemblyName) && !IsEditorOrTestAssembly(assemblyName);
        }

        /// <summary>
        ///   <para>判断是否为编辑器或测试程序集。</para>
        /// </summary>
        /// <param name="assemblyName">程序集名称。</param>
        private static bool IsEditorOrTestAssembly(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                return true;
            }

            var segments = assemblyName.Split('.', '-');
            for (var i = 0; i < segments.Length; i++)
            {
                if (IsEditorOrTestSegment(segments[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>判断是否为运行时资源路径。</para>
        /// </summary>
        /// <param name="assetPath">资源路径。</param>
        private static bool IsRuntimeAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            var segments = Game.PathUtility.Normalize(assetPath).Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                if (IsEditorOrTestSegment(segments[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///   <para>判断是否为编辑器或测试片段。</para>
        /// </summary>
        /// <param name="value">值。</param>
        private static bool IsEditorOrTestSegment(string value)
        {
            return value.Equals("Editor", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Test", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Tests", StringComparison.OrdinalIgnoreCase);
        }
    }
}

#endif