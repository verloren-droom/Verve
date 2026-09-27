#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEditorInternal;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>部件组件编辑器；配置部件类型、节点变量和事件。</para>
    /// </summary>
    [CustomEditor(typeof(UIWidgetComponent))]
    internal sealed class UIWidgetComponentEditor : UnityEditor.Editor
    {
        /// <summary>
        ///   <para>部件类型按钮最小宽度。</para>
        /// </summary>
        private const float WidgetTypeButtonMinWidth = 280f;
        /// <summary>
        ///   <para>部件类型控件高度。</para>
        /// </summary>
        private const float WidgetTypeControlHeight = 22f;
        /// <summary>
        ///   <para>变量列表最大高度。</para>
        /// </summary>
        private const float VariableListMaxHeight = 260f;

        /// <summary>
        ///   <para>部件类型名称。</para>
        /// </summary>
        private SerializedProperty m_WidgetTypeName;
        /// <summary>
        ///   <para>变量。</para>
        /// </summary>
        private SerializedProperty m_Variables;
        /// <summary>
        ///   <para>变量签名。</para>
        /// </summary>
        private SerializedProperty m_VariableSignature;
        /// <summary>
        ///   <para>事件签名。</para>
        /// </summary>
        private SerializedProperty m_EventSignature;
        /// <summary>
        ///   <para>变量列表。</para>
        /// </summary>
        private ReorderableList m_VariableList;
        /// <summary>
        ///   <para>变量列表滚动位置。</para>
        /// </summary>
        private Vector2 m_VariableListScrollPosition;
        /// <summary>
        ///   <para>变量搜索文本。</para>
        /// </summary>
        private string m_VariableSearchText = string.Empty;

        /// <summary>
        ///   <para>启用时初始化。</para>
        /// </summary>
        private void OnEnable()
        {
            m_WidgetTypeName = serializedObject.FindProperty(UIComponentEditorUtility.WidgetTypeNameField);
            m_Variables = serializedObject.FindProperty(UIComponentEditorUtility.VariablesField);
            m_VariableSignature = serializedObject.FindProperty(UIComponentEditorUtility.VariableSignatureField);
            m_EventSignature = serializedObject.FindProperty(UIComponentEditorUtility.EventSignatureField);
            m_VariableList = new ReorderableList(serializedObject, m_Variables, true, true, true, true)
            {
                drawHeaderCallback = DrawVariablesHeader,
                elementHeight = EditorGUIUtility.singleLineHeight + 4f,
                elementHeightCallback = GetVariableHeight,
                drawElementCallback = DrawVariable,
                onAddCallback = AddVariable,
                onRemoveCallback = RemoveVariable,
            };
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var widgetType = GetWidgetType();
            var canEdit = UIComponentEditorUtility.CanEditComponent(target);
            DrawWidgetType(widgetType, canEdit);

            var canEditVariables = canEdit && widgetType != null;
            if (IsVariableSearchActive() && !IsVariableVisible(m_VariableList.index))
            {
                m_VariableList.index = -1;
            }

            m_VariableList.draggable = canEditVariables && !IsVariableSearchActive();
            m_VariableList.displayAdd = canEditVariables;
            m_VariableList.displayRemove = canEditVariables &&
                                           (!IsVariableSearchActive() ||
                                            IsVariableVisible(m_VariableList.index));
            using (new EditorGUI.DisabledScope(!canEditVariables))
            {
                DrawVariableList();
            }

            var error = ValidateVariables(widgetType);
            var variables = error == null ? GetVariables() : null;
            var variableSignature = variables == null ? 0 : UIVariablesGenerator.CalculateSignature(variables);
            var eventSignature = variables == null ? 0 : UIEventsGenerator.CalculateSignature(variables);
            if (error != null)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
            else if (widgetType != null &&
                     (variableSignature != m_VariableSignature.intValue ||
                      eventSignature != m_EventSignature.intValue))
            {
                EditorGUILayout.HelpBox("变量或事件配置已修改，请重新生成代码。", MessageType.Warning);
            }

            var current = variables != null &&
                          variableSignature == m_VariableSignature.intValue &&
                          eventSignature == m_EventSignature.intValue;
            using (new EditorGUI.DisabledScope(!canEditVariables || variables == null))
            {
                if (GUILayout.Button(current ? "重新生成代码" : "生成代码", GUILayout.MinHeight(30f)))
                {
                    try
                    {
                        Generate(widgetType, variables, variableSignature, eventSignature);
                    }
                    catch (Exception exception)
                    {
                        EditorUtility.DisplayDialog("生成 UI Widget 代码", exception.Message, "确定");
                    }

                    GUIUtility.ExitGUI();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///   <para>绘制部件类型。</para>
        /// </summary>
        /// <param name="widgetType">部件类型。</param>
        /// <param name="canEdit">允许编辑。</param>
        private void DrawWidgetType(Type widgetType, bool canEdit)
        {
            var rect = EditorGUILayout.GetControlRect(false, WidgetTypeControlHeight);
            var fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("Widget 类型"));
            var script = widgetType == null ? null : UIVariablesGenerator.FindTypeScript(widgetType);
            var scriptPath = script == null ? null : AssetDatabase.GetAssetPath(script);
            var prefabAsset = CoreEditorUtility.GetSourcePrefabAsset((UIWidgetComponent)target);
            var scriptContent = widgetType == null
                ? new GUIContent("未选择", "选择一个继承自 WidgetBase 的 Widget")
                : new GUIContent(widgetType.Name, scriptPath ?? "未找到 Widget 脚本");
            if (script == null && widgetType != null)
            {
                scriptContent.tooltip = "未找到 Widget 指向的脚本文件";
            }

            var buttonCount = prefabAsset == null ? 1f : 2f;
            var dropdownWidth = Mathf.Min(WidgetTypeControlHeight, fieldRect.width / buttonCount);
            var prefabButtonWidth = prefabAsset == null ? 0f : dropdownWidth;
            var preferredScriptWidth = GUI.skin.button.CalcSize(scriptContent).x + 24f;
            var fixedButtonWidth = dropdownWidth + prefabButtonWidth;
            var groupWidth = Mathf.Min(
                fieldRect.width,
                Mathf.Max(WidgetTypeButtonMinWidth + fixedButtonWidth, preferredScriptWidth + fixedButtonWidth));
            var prefabRect = new Rect(
                fieldRect.xMax - groupWidth,
                fieldRect.y,
                prefabButtonWidth,
                fieldRect.height);
            var scriptRect = new Rect(
                prefabRect.xMax,
                fieldRect.y,
                Mathf.Max(0f, groupWidth - fixedButtonWidth),
                fieldRect.height);
            var dropdownRect = new Rect(
                scriptRect.xMax,
                fieldRect.y,
                dropdownWidth,
                fieldRect.height);

            if (prefabAsset != null)
            {
                var prefabContent = EditorGUIUtility.IconContent("Prefab Icon");
                var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
                prefabContent.tooltip = string.IsNullOrEmpty(prefabPath)
                    ? "定位源 Prefab 资源"
                    : $"定位源 Prefab：{prefabPath}";
                if (GUI.Button(prefabRect, prefabContent))
                {
                    EditorGUIUtility.PingObject(prefabAsset);
                }
            }

            using (new EditorGUI.DisabledScope(script == null))
            {
                if (GUI.Button(scriptRect, scriptContent))
                {
                    if (Event.current != null && Event.current.clickCount == 2)
                    {
                        AssetDatabase.OpenAsset(script);
                    }
                    else
                    {
                        EditorGUIUtility.PingObject(script);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!canEdit))
            {
                var dropdownContent = EditorGUIUtility.IconContent("icon dropdown");
                dropdownContent.tooltip = "选择 Widget 类型";
                if (!GUI.Button(dropdownRect, dropdownContent))
                {
                    return;
                }

                var labels = new List<string>();
                var types = new List<Type>();
                foreach (var type in UIComponentEditorUtility.GetWidgetTypes())
                {
                    labels.Add(type.FullName);
                    types.Add(type);
                }

                var optionLabels = labels.ToArray();
                var optionTypes = types.ToArray();
                PopupWindow.Show(
                    dropdownRect,
                    new SearchableOptionsPopup(
                        widgetType?.FullName ?? "未选择",
                        optionLabels,
                        selected =>
                        {
                            var index = Array.IndexOf(optionLabels, selected);
                            if ((uint)index < (uint)optionTypes.Length) SetWidgetType(optionTypes[index]);
                        },
                        "UIWidgetType",
                        "Widget 类型",
                        "."));
            }
        }

        /// <summary>
        ///   <para>绘制变量。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="index">索引。</param>
        /// <param name="active">激活。</param>
        /// <param name="focused">获得焦点。</param>
        private void DrawVariable(Rect rect, int index, bool active, bool focused)
        {
            if (!IsVariableVisible(index))
            {
                return;
            }

            var element = m_Variables.GetArrayElementAtIndex(index);
            var name = element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField);
            var value = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField);
            var events = element.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField);
            var selectedValue = value.objectReferenceValue;
            var selectedComponent = selectedValue as Component;
            var node = selectedComponent != null ? selectedComponent.gameObject : null;
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            UIComponentEditorUtility.GetVariableRects(
                rect,
                out var nameRect,
                out var nodeRect,
                out var typeRect,
                out var eventRect);
            EditorGUI.PropertyField(nameRect, name, GUIContent.none);
            var displayValue = selectedComponent != null ? node : selectedValue;
            EditorGUI.BeginChangeCheck();
            var nextObject = EditorGUI.ObjectField(
                nodeRect,
                displayValue,
                typeof(UnityEngine.Object),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                if (UIComponentEditorUtility.CanAssignValue((UIWidgetComponent)target, nextObject))
                {
                    selectedValue = nextObject is GameObject gameObject &&
                                     !EditorUtility.IsPersistent(gameObject)
                        ? gameObject.transform
                        : nextObject;
                    selectedComponent = selectedValue as Component;
                    node = selectedComponent != null ? selectedComponent.gameObject : null;
                    value.objectReferenceValue = selectedValue;
                    events.arraySize = 0;
                    if (nextObject != null && string.IsNullOrWhiteSpace(name.stringValue))
                    {
                        name.stringValue = UIVariablesGenerator.CreateIdentifier(nextObject.name);
                    }
                }
            }

            DrawTypePopup(
                typeRect,
                index,
                node,
                selectedComponent,
                selectedValue);
            var definitions = selectedComponent == null
                ? Array.Empty<UIEventDefinition>()
                : UIEventsGenerator.GetEventDefinitions(selectedComponent.GetType());
            var selected = UIComponentEditorUtility.ReadEventIds(events);
            if (UIComponentEditorUtility.CanEditBinding(
                    (UIWidgetComponent)target,
                    selectedComponent) &&
                UIComponentEditorUtility.RemoveUnsupportedEvents(selected, definitions))
            {
                UIComponentEditorUtility.WriteEventIds(events, selected);
            }
            DrawEventPopup(eventRect, index, selected, definitions);
        }

        /// <summary>
        ///   <para>绘制变量列表。</para>
        /// </summary>
        private void DrawVariableList()
        {
            DrawVariableSearchBar();
            var visibleCount = GetVisibleVariableCount();

            var listHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight * 3f,
                m_VariableList.GetHeight());
            var viewHeight = Mathf.Min(listHeight, VariableListMaxHeight);
            m_VariableListScrollPosition = EditorGUILayout.BeginScrollView(
                m_VariableListScrollPosition,
                GUILayout.Height(viewHeight));
            try
            {
                var rect = GUILayoutUtility.GetRect(
                    0f,
                    listHeight,
                    GUILayout.ExpandWidth(true));
                m_VariableList.DoList(rect);
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }

            if (m_Variables.arraySize > 0 && visibleCount == 0)
            {
                EditorGUILayout.HelpBox("没有匹配的变量引用。", MessageType.Info);
            }
        }

        /// <summary>
        ///   <para>绘制变量搜索栏。</para>
        /// </summary>
        private void DrawVariableSearchBar()
        {
            var nextSearch = UIComponentEditorUtility.DrawVariableSearchBar(
                m_VariableSearchText,
                m_Variables.arraySize);
            if (!string.Equals(nextSearch, m_VariableSearchText, StringComparison.Ordinal))
            {
                m_VariableSearchText = nextSearch;
                m_VariableListScrollPosition = Vector2.zero;
                if (!IsVariableVisible(m_VariableList.index))
                {
                    m_VariableList.index = -1;
                }
            }
        }

        /// <summary>
        ///   <para>获取变量高度。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        private float GetVariableHeight(int index)
        {
            return IsVariableVisible(index)
                ? EditorGUIUtility.singleLineHeight + 4f
                : 0f;
        }

        /// <summary>
        ///   <para>获取可见变量数量。</para>
        /// </summary>
        private int GetVisibleVariableCount()
        {
            var count = 0;
            for (var i = 0; i < m_Variables.arraySize; i++)
            {
                if (IsVariableVisible(i))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        ///   <para>判断变量是否通过显示筛选。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        private bool IsVariableVisible(int index)
        {
            if ((uint)index >= (uint)m_Variables.arraySize)
            {
                return false;
            }

            return !IsVariableSearchActive() ||
                   UIComponentEditorUtility.MatchesVariableSearch(
                       m_Variables.GetArrayElementAtIndex(index),
                       m_VariableSearchText);
        }

        /// <summary>
        ///   <para>判断变量搜索是否生效。</para>
        /// </summary>
        private bool IsVariableSearchActive() => !string.IsNullOrWhiteSpace(m_VariableSearchText);

        /// <summary>
        ///   <para>绘制类型弹窗。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="index">索引。</param>
        /// <param name="node">节点。</param>
        /// <param name="selected">选中。</param>
        /// <param name="selectedObject">选中对象。</param>
        private void DrawTypePopup(
            Rect rect,
            int index,
            GameObject node,
            Component selected,
            UnityEngine.Object selectedObject)
        {
            var label = selectedObject != null
                ? UIVariablesGenerator.GetTypeName(selectedObject.GetType())
                : "未选择";
            if (selectedObject != null && selected == null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.DropdownButton(rect, new GUIContent(label), FocusType.Passive);
                }

                return;
            }
            var canEditBinding = UIComponentEditorUtility.CanEditObject(
                (UIWidgetComponent)target,
                node);
            using (new EditorGUI.DisabledScope(node == null || !canEditBinding))
            {
                if (!EditorGUI.DropdownButton(rect, new GUIContent(label), FocusType.Passive) || node == null) return;
            }

            UIComponentEditorUtility.BuildComponentOptions(
                node,
                selected,
                out label,
                out var optionLabels,
                out var optionValues);
            PopupWindow.Show(
                rect,
                new SearchableOptionsPopup(
                    label,
                    optionLabels,
                    selectedLabel =>
                    {
                        var selectedIndex = Array.IndexOf(optionLabels, selectedLabel);
                        if ((uint)selectedIndex < (uint)optionValues.Length) SetVariableValue(index, optionValues[selectedIndex]);
                    },
                    $"UIBindingType_{index}",
                    "组件类型",
                    "."));
        }

        /// <summary>
        ///   <para>绘制事件弹窗。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="index">索引。</param>
        /// <param name="selected">选中。</param>
        /// <param name="definitions">定义。</param>
        private void DrawEventPopup(Rect rect, int index, IReadOnlyList<string> selected, IReadOnlyList<UIEventDefinition> definitions)
        {
            UIComponentEditorUtility.BuildEventOptions(
                selected,
                definitions,
                out var label,
                out var labels,
                out var current);
            using (new EditorGUI.DisabledScope(
                       definitions.Count == 0 ||
                       !UIComponentEditorUtility.CanEditBinding(
                           (UIWidgetComponent)target,
                           UIComponentEditorUtility.GetVariableComponent(m_Variables, index))))
            {
                if (!EditorGUI.DropdownButton(rect, new GUIContent(label), FocusType.Passive)) return;

                PopupWindow.Show(
                    rect,
                    new SearchableOptionsPopup(
                        current,
                        labels,
                        values => SetEventIds(
                            index,
                            UIComponentEditorUtility.GetEventIds(values, definitions)),
                        $"UIBindingEvents_{index}",
                        "绑定事件"));
            }
        }

        /// <summary>
        ///   <para>绘制变量表头。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        private void DrawVariablesHeader(Rect rect)
        {
            UIComponentEditorUtility.GetVariableRects(
                rect,
                out var name,
                out var node,
                out var type,
                out var events);
            EditorGUI.LabelField(name, "变量名");
            EditorGUI.LabelField(node, "引用");
            EditorGUI.LabelField(type, "类型");
            EditorGUI.LabelField(events, "事件");
        }

        /// <summary>
        ///   <para>添加变量。</para>
        /// </summary>
        /// <param name="list">列表。</param>
        private void AddVariable(ReorderableList list)
        {
            if (!UIComponentEditorUtility.CanEditComponent(target))
            {
                return;
            }

            var index = m_Variables.arraySize++;
            var element = m_Variables.GetArrayElementAtIndex(index);
            element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField).stringValue = string.Empty;
            element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField).objectReferenceValue = null;
            element.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField).arraySize = 0;
            list.index = index;
            m_VariableSearchText = string.Empty;
            m_VariableListScrollPosition = Vector2.zero;
        }

        /// <summary>
        ///   <para>移除变量。</para>
        /// </summary>
        /// <param name="list">列表。</param>
        private void RemoveVariable(ReorderableList list)
        {
            if (!UIComponentEditorUtility.CanEditComponent(target) ||
                (uint)list.index >= (uint)m_Variables.arraySize ||
                !IsVariableVisible(list.index))
            {
                return;
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }

        /// <summary>
        ///   <para>设置部件类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private void SetWidgetType(Type type)
        {
            if (!UIComponentEditorUtility.CanEditComponent(target) ||
                !UIComponentEditorUtility.IsConcreteWidgetType(type))
            {
                return;
            }

            serializedObject.Update();
            if (string.Equals(m_WidgetTypeName.stringValue, type.AssemblyQualifiedName, StringComparison.Ordinal))
            {
                return;
            }

            m_WidgetTypeName.stringValue = type.AssemblyQualifiedName;
            m_VariableSignature.intValue = 0;
            m_EventSignature.intValue = 0;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///   <para>获取部件类型。</para>
        /// </summary>
        private Type GetWidgetType()
        {
            var type = UIComponentEditorUtility.ReadType(m_WidgetTypeName);
            return UIComponentEditorUtility.IsConcreteWidgetType(type) ? type : null;
        }

        /// <summary>
        ///   <para>校验变量。</para>
        /// </summary>
        /// <param name="widgetType">部件类型。</param>
        private string ValidateVariables(Type widgetType)
        {
            var compositionError = UICompositionPolicy.GetCompositionError((UIWidgetComponent)target);
            if (compositionError != null) return compositionError;
            if (widgetType == null) return null;

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < m_Variables.arraySize; i++)
            {
                var element = m_Variables.GetArrayElementAtIndex(i);
                var name = element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField).stringValue;
                var rawValue = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField).objectReferenceValue;
                if (rawValue == null) return $"变量“{name}”尚未指定引用对象。";
                var value = rawValue as Component;
                if (!UIVariablesGenerator.IsValidIdentifier(name)) return $"第 {i + 1} 个变量需要一个有效的 C# 名称。";
                if (!names.Add(name)) return $"变量名“{name}”重复。";
                var targetNode = UICompositionPolicy.GetReferenceNode(rawValue);
                if (targetNode != null && !UIComponentEditorUtility.IsOwnedObject(
                        ((UIWidgetComponent)target).gameObject,
                        targetNode))
                {
                    return $"变量“{name}”必须引用当前 Prefab 内的节点。";
                }

                var bindingError = UICompositionPolicy.GetBindingError(
                    (UIWidgetComponent)target,
                    targetNode);
                if (bindingError != null)
                {
                    return $"变量“{name}”：{bindingError}";
                }

                if (UIComponentEditorUtility.HasMemberConflict(widgetType, name))
                {
                    return $"变量名“{name}”与 {nameof(WidgetBase)} 成员冲突。";
                }
            }

            return null;
        }

        /// <summary>
        ///   <para>生成。</para>
        /// </summary>
        /// <param name="widgetType">部件类型。</param>
        /// <param name="variables">变量。</param>
        /// <param name="variableSignature">变量签名。</param>
        /// <param name="eventSignature">事件签名。</param>
        private void Generate(Type widgetType, IReadOnlyList<UIVariableDefinition> variables, int variableSignature, int eventSignature)
        {
            if (widgetType == null) throw new InvalidOperationException("生成 UI Widget 代码前，请先选择有效的 Widget 类型。");
            AssetDatabase.StartAssetEditing();
            try
            {
                UIVariablesGenerator.Generate(widgetType, variables, variableSignature);
                UIEventsGenerator.Generate(widgetType, variables);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            m_VariableSignature.intValue = variableSignature;
            m_EventSignature.intValue = eventSignature;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        /// <summary>
        ///   <para>获取变量。</para>
        /// </summary>
        private List<UIVariableDefinition> GetVariables()
        {
            var result = new List<UIVariableDefinition>(m_Variables.arraySize);
            for (var i = 0; i < m_Variables.arraySize; i++)
            {
                var element = m_Variables.GetArrayElementAtIndex(i);
                var value = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField).objectReferenceValue;
                if (value == null) return null;
                result.Add(new UIVariableDefinition(
                    element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField).stringValue,
                    value.GetType(),
                    UIComponentEditorUtility.ReadEventIds(
                        element.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField))));
            }

            return result;
        }

        /// <summary>
        ///   <para>设置变量值。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="value">值。</param>
        private void SetVariableValue(int index, UnityEngine.Object value)
        {
            if (!UIComponentEditorUtility.CanEditComponent(target))
            {
                return;
            }

            if (!UIComponentEditorUtility.CanAssignValue((UIWidgetComponent)target, value))
            {
                return;
            }

            serializedObject.Update();
            if ((uint)index < (uint)m_Variables.arraySize)
            {
                var element = m_Variables.GetArrayElementAtIndex(index);
                element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField).objectReferenceValue = value;
                element.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField).arraySize = 0;
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        ///   <para>设置事件标识列表。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="ids">事件标识列表。</param>
        private void SetEventIds(int index, IReadOnlyList<string> ids)
        {
            if (!UIComponentEditorUtility.CanEditBinding(
                    (UIWidgetComponent)target,
                    UIComponentEditorUtility.GetVariableComponent(m_Variables, index)))
            {
                return;
            }

            serializedObject.Update();
            if ((uint)index < (uint)m_Variables.arraySize)
            {
                UIComponentEditorUtility.WriteEventIds(
                    m_Variables.GetArrayElementAtIndex(index)
                        .FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField),
                    ids);
                serializedObject.ApplyModifiedProperties();
            }
        }

    }
}

#endif
