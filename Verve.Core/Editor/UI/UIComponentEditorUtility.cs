#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;

    /// <summary>
    ///   <para>UI 绑定编辑工具；供页面和部件组件编辑器共用。</para>
    /// </summary>
    internal static class UIComponentEditorUtility
    {
        /// <summary>
        ///   <para>页面类型名称字段。</para>
        /// </summary>
        internal const string ViewTypeNameField = "m_ViewTypeName";
        /// <summary>
        ///   <para>部件类型名称字段。</para>
        /// </summary>
        internal const string WidgetTypeNameField = "m_WidgetTypeName";
        /// <summary>
        ///   <para>变量字段。</para>
        /// </summary>
        internal const string VariablesField = "m_Variables";
        /// <summary>
        ///   <para>变量签名字段。</para>
        /// </summary>
        internal const string VariableSignatureField = "m_VariableSignature";
        /// <summary>
        ///   <para>事件签名字段。</para>
        /// </summary>
        internal const string EventSignatureField = "m_EventSignature";
        /// <summary>
        ///   <para>变量名称字段。</para>
        /// </summary>
        internal const string VariableNameField = "m_Name";
        /// <summary>
        ///   <para>变量值字段。</para>
        /// </summary>
        internal const string VariableValueField = "m_Value";
        /// <summary>
        ///   <para>变量事件标识列表字段。</para>
        /// </summary>
        internal const string VariableEventIdsField = "m_EventIds";

        /// <summary>
        ///   <para>判断类型是否可以由 UI 工厂创建。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        internal static bool IsConcreteViewType(Type type) => IsConcreteType(type, typeof(ViewBase));

        /// <summary>
        ///   <para>判断类型是否可以由 <see cref="WidgetBase"/> 工厂创建。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        internal static bool IsConcreteWidgetType(Type type) => IsConcreteType(type, typeof(WidgetBase));

        /// <summary>
        ///   <para>读取组件序列化的 UI 类型；字段缺失或类型名无效时返回 null。</para>
        /// </summary>
        /// <param name="typeProperty">类型属性。</param>
        internal static Type ReadType(SerializedProperty typeProperty)
        {
            if (typeProperty == null || string.IsNullOrEmpty(typeProperty.stringValue))
            {
                return null;
            }

            return Type.GetType(typeProperty.stringValue, false);
        }

        /// <summary>
        ///   <para>读取并验证组件当前配置的 <see cref="ViewBase"/> 或 <see cref="WidgetBase"/> 类型。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        internal static Type GetConfiguredType(Component owner)
        {
            if (owner == null)
            {
                return null;
            }

            var fieldName = owner is UIViewComponent
                ? ViewTypeNameField
                : owner is UIWidgetComponent
                    ? WidgetTypeNameField
                    : null;
            if (fieldName == null)
            {
                return null;
            }

            using var serializedObject = new SerializedObject(owner);
            serializedObject.Update();
            var type = ReadType(serializedObject.FindProperty(fieldName));
            if (owner is UIViewComponent)
            {
                return IsConcreteViewType(type) ? type : null;
            }

            return IsConcreteWidgetType(type) ? type : null;
        }

        /// <summary>
        ///   <para>判断 UI 组件当前是否处于可编辑的实例上下文。</para>
        /// </summary>
        /// <remarks>
        ///   <para>只允许场景对象或 Prefab Mode 中的对象修改类型、变量和事件；Project 中的 Prefab 资源保持只读。</para>
        /// </remarks>
        /// <param name="target">目标。</param>
        internal static bool CanEditComponent(UnityEngine.Object target)
        {
            if (Application.isPlaying || !(target is Component component) || component.gameObject == null)
            {
                return false;
            }

            return !EditorUtility.IsPersistent(component.gameObject) && component.gameObject.scene.IsValid();
        }

        /// <summary>
        ///   <para>判断组件引用是否仍属于当前可编辑 UI 节点树。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="value">值。</param>
        internal static bool CanEditBinding(Component owner, Component value) => CanEditObject(owner, value != null ? value.gameObject : null);

        /// <summary>
        ///   <para>判断变量引用是否可以写入当前 UI 组件。</para>
        /// </summary>
        /// <remarks>资源对象允许引用；场景节点必须属于当前 UI 节点树，不能越过 <see cref="WidgetBase"/> 边界。</remarks>
        /// <param name="owner">所有者。</param>
        /// <param name="value">值。</param>
        internal static bool CanAssignValue(Component owner, UnityEngine.Object value)
        {
            if (owner == null || !CanEditComponent(owner))
            {
                return false;
            }

            if (UICompositionPolicy.GetCompositionError(owner) != null)
            {
                return false;
            }

            var gameObject = UICompositionPolicy.GetReferenceNode(value);
            if (gameObject == null)
            {
                return true;
            }

            if (EditorUtility.IsPersistent(gameObject))
            {
                return UICompositionPolicy.GetBindingError(owner, gameObject) == null;
            }

            return IsOwnedObject(owner.gameObject, gameObject) &&
                   UICompositionPolicy.GetBindingError(owner, gameObject) == null;
        }

        /// <summary>
        ///   <para>判断节点上的组件类型是否可以编辑。</para>
        /// </summary>
        /// <param name="owner">所有者。</param>
        /// <param name="node">节点。</param>
        internal static bool CanEditObject(Component owner, GameObject node)
        {
            return owner != null && node != null && CanEditComponent(owner) &&
                   UICompositionPolicy.GetCompositionError(owner) == null &&
                   IsOwnedObject(owner.gameObject, node) &&
                   UICompositionPolicy.GetBindingError(owner, node) == null;
        }

        /// <summary>
        ///   <para>读取变量列表中的组件引用。</para>
        /// </summary>
        /// <param name="variables">变量。</param>
        /// <param name="index">索引。</param>
        internal static Component GetVariableComponent(SerializedProperty variables, int index)
        {
            if (variables == null || !variables.isArray || (uint)index >= (uint)variables.arraySize)
            {
                return null;
            }

            return variables.GetArrayElementAtIndex(index)
                .FindPropertyRelative(VariableValueField)
                ?.objectReferenceValue as Component;
        }

        /// <summary>
        ///   <para>判断是否为具体类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <param name="baseType">允许的基类。</param>
        private static bool IsConcreteType(Type type, Type baseType)
        {
            return type != null && !type.IsAbstract && !type.IsGenericType && !type.IsNested &&
                   baseType.IsAssignableFrom(type) &&
                   type.GetConstructor(
                       BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                       binder: null,
                       types: Type.EmptyTypes,
                       modifiers: null) != null;
        }

        /// <summary>
        ///   <para>页面类型。</para>
        /// </summary>
        private static Type[] s_ViewTypes;
        /// <summary>
        ///   <para>部件类型。</para>
        /// </summary>
        private static Type[] s_WidgetTypes;

        /// <summary>
        ///   <para>变量搜索清空宽度。</para>
        /// </summary>
        private const float VariableSearchClearWidth = 18f;

        /// <summary>
        ///   <para>返回当前程序集可由编辑器创建的 <see cref="ViewBase"/> 类型。</para>
        /// </summary>
        internal static IReadOnlyList<Type> GetViewTypes() => s_ViewTypes ??= FindConcreteTypes<ViewBase>(IsConcreteViewType);

        /// <summary>
        ///   <para>返回当前程序集可由编辑器创建的 <see cref="WidgetBase"/> 类型。</para>
        /// </summary>
        internal static IReadOnlyList<Type> GetWidgetTypes() => s_WidgetTypes ??= FindConcreteTypes<WidgetBase>(IsConcreteWidgetType);

        /// <summary>
        ///   <para>查找具体类型。</para>
        /// </summary>
        /// <param name="predicate">筛选条件。</param>
        /// <typeparam name="TBase">基类类型。</typeparam>
        private static Type[] FindConcreteTypes<TBase>(Func<Type, bool> predicate)
        {
            var types = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<TBase>())
            {
                if (predicate(type))
                {
                    types.Add(type);
                }
            }

            types.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return types.ToArray();
        }

        /// <summary>
        ///   <para>计算变量列表的四列布局。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="nameRect">名称区域。</param>
        /// <param name="nodeRect">节点区域。</param>
        /// <param name="typeRect">类型区域。</param>
        /// <param name="eventRect">事件区域。</param>
        internal static void GetVariableRects(
            Rect rect,
            out Rect nameRect,
            out Rect nodeRect,
            out Rect typeRect,
            out Rect eventRect)
        {
            const float spacing = 4f;
            var contentWidth = Mathf.Max(0f, rect.width - spacing * 3f);
            var eventWidth = Mathf.Clamp(contentWidth * 0.2f, 56f, 92f);
            var nameWidth = contentWidth * 0.26f;
            var nodeWidth = contentWidth * 0.34f;
            var typeWidth = Mathf.Max(0f, contentWidth - nameWidth - nodeWidth - eventWidth);
            nameRect = new Rect(rect.x, rect.y, nameWidth, rect.height);
            nodeRect = new Rect(nameRect.xMax + spacing, rect.y, nodeWidth, rect.height);
            typeRect = new Rect(nodeRect.xMax + spacing, rect.y, typeWidth, rect.height);
            eventRect = new Rect(typeRect.xMax + spacing, rect.y, eventWidth, rect.height);
        }

        /// <summary>
        ///   <para>绘制变量引用搜索栏并返回最新搜索文本。</para>
        /// </summary>
        /// <param name="searchText">搜索文本。</param>
        /// <param name="totalCount">总计数量。</param>
        internal static string DrawVariableSearchBar(
            string searchText,
            int totalCount)
        {
            var barRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                EditorStyles.toolbarSearchField,
                GUILayout.ExpandWidth(true));
            var currentText = searchText ?? string.Empty;

            using (new EditorGUI.DisabledScope(totalCount == 0))
            {
                var fieldRect = barRect;
                var clearRect = new Rect(
                    Mathf.Max(fieldRect.x, fieldRect.xMax - VariableSearchClearWidth),
                    fieldRect.y,
                    Mathf.Min(VariableSearchClearWidth, fieldRect.width),
                    fieldRect.height);
                var currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseDown &&
                    currentEvent.button == 0 &&
                    currentText.Length > 0 &&
                    clearRect.Contains(currentEvent.mousePosition))
                {
                    GUI.FocusControl(null);
                    GUI.changed = true;
                    currentEvent.Use();
                    return string.Empty;
                }

                EditorGUI.BeginChangeCheck();
                var nextSearch = EditorGUI.TextField(
                    fieldRect,
                    currentText,
                    EditorStyles.toolbarSearchField);
                if (EditorGUI.EndChangeCheck())
                {
                    currentText = nextSearch ?? string.Empty;
                }

                if (currentText.Length > 0)
                {
                    DrawVariableSearchClearButton(clearRect);
                }
                return currentText;
            }
        }

        /// <summary>
        ///   <para>绘制变量搜索清空按钮。</para>
        /// </summary>
        /// <param name="clearRect">清空区域。</param>
        private static void DrawVariableSearchClearButton(Rect clearRect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var style = GUI.skin.FindStyle("ToolbarSearchCancelButton") ??
                        GUI.skin.FindStyle("ToolbarSeachCancelButton") ??
                        EditorStyles.toolbarButton;
            style.Draw(
                clearRect,
                GUIContent.none,
                false,
                clearRect.Contains(Event.current.mousePosition),
                false,
                false);
        }

        /// <summary>
        ///   <para>判断节点是否属于当前 UI 组件所在的节点树。</para>
        /// </summary>
        /// <param name="root">根节点。</param>
        /// <param name="node">节点。</param>
        internal static bool IsOwnedObject(GameObject root, GameObject node) => UICompositionPolicy.IsOwnedNode(root?.transform, node?.transform);

        /// <summary>
        ///   <para>判断生成变量是否会与目标类型的现有成员重名。</para>
        /// </summary>
        /// <param name="targetType">目标类型。</param>
        /// <param name="memberName">成员名称。</param>
        internal static bool HasMemberConflict(Type targetType, string memberName)
        {
            if (targetType == null || string.IsNullOrEmpty(memberName))
            {
                return false;
            }

            var members = targetType.GetMember(
                memberName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (members.Length == 0)
            {
                return false;
            }

            return !UIVariablesGenerator.IsGeneratedMember(targetType, memberName);
        }

        /// <summary>
        ///   <para>读取事件 ID，并移除空值和重复项。</para>
        /// </summary>
        /// <param name="property">属性。</param>
        internal static List<string> ReadEventIds(SerializedProperty property)
        {
            var result = new List<string>(property != null && property.isArray ? property.arraySize : 0);
            if (property == null || !property.isArray)
            {
                return result;
            }

            for (var i = 0; i < property.arraySize; i++)
            {
                var id = property.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrEmpty(id) && !Contains(result, id))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        /// <summary>
        ///   <para>判断变量是否匹配变量列表搜索文本。</para>
        /// </summary>
        /// <param name="variable">变量。</param>
        /// <param name="searchText">搜索文本。</param>
        internal static bool MatchesVariableSearch(SerializedProperty variable, string searchText)
        {
            if (variable == null || string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            var tokens = searchText.Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++)
            {
                if (!MatchesVariableToken(variable, tokens[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///   <para>匹配变量搜索词。</para>
        /// </summary>
        /// <param name="variable">变量。</param>
        /// <param name="token">搜索词元。</param>
        private static bool MatchesVariableToken(SerializedProperty variable, string token)
        {
            var name = variable.FindPropertyRelative(VariableNameField)?.stringValue;
            if (ContainsSearchText(name, token))
            {
                return true;
            }

            var value = variable.FindPropertyRelative(VariableValueField)?.objectReferenceValue;
            if (value != null)
            {
                if (ContainsSearchText(value.name, token) ||
                    ContainsSearchText(value.GetType().Name, token) ||
                    ContainsSearchText(value.GetType().FullName, token) ||
                    ContainsSearchText(UIVariablesGenerator.GetTypeName(value.GetType()), token))
                {
                    return true;
                }

                if (value is Component component &&
                    ContainsSearchText(component.gameObject.name, token))
                {
                    return true;
                }
            }

            var eventIds = variable.FindPropertyRelative(VariableEventIdsField);
            if (eventIds == null || !eventIds.isArray || eventIds.arraySize == 0)
            {
                return false;
            }

            for (var i = 0; i < eventIds.arraySize; i++)
            {
                var eventId = eventIds.GetArrayElementAtIndex(i).stringValue;
                if (ContainsSearchText(eventId, token))
                {
                    return true;
                }
            }

            if (value == null)
            {
                return false;
            }

            var definitions = UIEventsGenerator.GetEventDefinitions(value.GetType());
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (ContainsEventId(eventIds, definition.Name) &&
                    ContainsSearchText(definition.DisplayName, token))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>包含搜索文本。</para>
        /// </summary>
        /// <param name="text">要压缩的字符串。</param>
        /// <param name="searchText">搜索文本。</param>
        private static bool ContainsSearchText(string text, string searchText)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        ///   <para>包含事件标识。</para>
        /// </summary>
        /// <param name="eventIds">事件标识列表。</param>
        /// <param name="eventId">事件标识。</param>
        private static bool ContainsEventId(SerializedProperty eventIds, string eventId)
        {
            if (eventIds == null || !eventIds.isArray)
            {
                return false;
            }

            for (var i = 0; i < eventIds.arraySize; i++)
            {
                if (string.Equals(
                        eventIds.GetArrayElementAtIndex(i).stringValue,
                        eventId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>将事件 ID 写回序列化数组。</para>
        /// </summary>
        /// <param name="property">属性。</param>
        /// <param name="eventIds">事件标识列表。</param>
        internal static void WriteEventIds(SerializedProperty property, IReadOnlyList<string> eventIds)
        {
            if (property == null)
            {
                return;
            }

            eventIds ??= Array.Empty<string>();
            property.arraySize = eventIds.Count;
            for (var i = 0; i < eventIds.Count; i++)
            {
                property.GetArrayElementAtIndex(i).stringValue = eventIds[i];
            }
        }

        /// <summary>
        ///   <para>移除组件类型上已经不存在的事件。</para>
        /// </summary>
        /// <param name="eventIds">事件标识列表。</param>
        /// <param name="definitions">定义。</param>
        internal static bool RemoveUnsupportedEvents(
            List<string> eventIds,
            IReadOnlyList<UIEventDefinition> definitions)
        {
            if (eventIds == null || definitions == null)
            {
                return false;
            }

            var changed = false;
            for (var i = eventIds.Count - 1; i >= 0; i--)
            {
                if (!ContainsEvent(definitions, eventIds[i]))
                {
                    eventIds.RemoveAt(i);
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        ///   <para>根据当前节点组件生成类型选择项。</para>
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="selected">选中。</param>
        /// <param name="selectedLabel">选中标签。</param>
        /// <param name="labels">标签。</param>
        /// <param name="values">值。</param>
        internal static void BuildComponentOptions(
            GameObject node,
            Component selected,
            out string selectedLabel,
            out string[] labels,
            out Component[] values)
        {
            selectedLabel = GetValueTypeLabel(selected);
            if (node == null)
            {
                labels = Array.Empty<string>();
                values = Array.Empty<Component>();
                return;
            }

            var labelList = new List<string>();
            var valueList = new List<Component>();

            var componentTypeCounts = new Dictionary<Type, int>();
            foreach (var component in node.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                var componentType = component.GetType();
                componentTypeCounts.TryGetValue(componentType, out var count);
                count++;
                componentTypeCounts[componentType] = count;
                var typeName = UIVariablesGenerator.GetTypeName(componentType);
                var label = count == 1 ? typeName : $"{typeName} #{count}";
                labelList.Add(label);
                valueList.Add(component);
                if (ReferenceEquals(component, selected))
                {
                    selectedLabel = label;
                }
            }

            labels = labelList.ToArray();
            values = valueList.ToArray();
        }

        /// <summary>
        ///   <para>根据选中事件生成弹窗的显示项。</para>
        /// </summary>
        /// <param name="selectedIds">选中标识。</param>
        /// <param name="definitions">定义。</param>
        /// <param name="selectedLabel">选中标签。</param>
        /// <param name="labels">标签。</param>
        /// <param name="selectedLabels">选中标签。</param>
        internal static void BuildEventOptions(
            IReadOnlyList<string> selectedIds,
            IReadOnlyList<UIEventDefinition> definitions,
            out string selectedLabel,
            out string[] labels,
            out List<string> selectedLabels)
        {
            selectedLabel = GetSelectedEventLabel(selectedIds, definitions);
            labels = new string[definitions.Count];
            selectedLabels = new List<string>(selectedIds.Count);
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                labels[i] = definition.DisplayName;
                if (Contains(selectedIds, definition.Name))
                {
                    selectedLabels.Add(definition.DisplayName);
                }
            }
        }

        /// <summary>
        ///   <para>将弹窗显示名称转换为事件 ID。</para>
        /// </summary>
        /// <param name="labels">标签。</param>
        /// <param name="definitions">定义。</param>
        internal static List<string> GetEventIds(
            IReadOnlyList<string> labels,
            IReadOnlyList<UIEventDefinition> definitions)
        {
            var result = new List<string>(labels.Count);
            for (var i = 0; i < definitions.Count; i++)
            {
                if (Contains(labels, definitions[i].DisplayName))
                {
                    result.Add(definitions[i].Name);
                }
            }

            return result;
        }

        /// <summary>
        ///   <para>返回当前事件绑定的显示名称。</para>
        /// </summary>
        /// <param name="selectedIds">选中标识。</param>
        /// <param name="definitions">定义。</param>
        internal static string GetSelectedEventLabel(
            IReadOnlyList<string> selectedIds,
            IReadOnlyList<UIEventDefinition> definitions)
        {
            var names = new List<string>(selectedIds.Count);
            for (var i = 0; i < definitions.Count; i++)
            {
                if (Contains(selectedIds, definitions[i].Name))
                {
                    names.Add(definitions[i].DisplayName);
                }
            }

            return names.Count == 0 ? "无" : string.Join(", ", names);
        }

        /// <summary>
        ///   <para>取得当前变量引用对应的类型名称。</para>
        /// </summary>
        /// <param name="selected">选中。</param>
        internal static string GetValueTypeLabel(UnityEngine.Object selected)
        {
            return selected != null
                ? UIVariablesGenerator.GetTypeName(selected.GetType())
                : "未选择";
        }

        /// <summary>
        ///   <para>包含事件。</para>
        /// </summary>
        /// <param name="definitions">定义。</param>
        /// <param name="eventId">事件标识。</param>
        internal static bool ContainsEvent(
            IReadOnlyList<UIEventDefinition> definitions,
            string eventId)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (string.Equals(definitions[i].Name, eventId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   <para>包含。</para>
        /// </summary>
        /// <param name="values">值。</param>
        /// <param name="value">值。</param>
        internal static bool Contains(IReadOnlyList<string> values, string value)
        {
            if (values == null)
            {
                return false;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
