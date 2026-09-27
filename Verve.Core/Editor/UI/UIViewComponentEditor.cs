#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEditorInternal;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>页面组件编辑器；配置页面类型与节点变量。</para>
    /// </summary>
    [CustomEditor(typeof(UIViewComponent))]
    internal sealed class UIViewComponentEditor : UnityEditor.Editor
    {
        /// <summary>
        ///   <para>页面类型按钮最小宽度。</para>
        /// </summary>
        private const float ViewTypeButtonMinWidth = 280f;
        /// <summary>
        ///   <para>页面类型控件高度。</para>
        /// </summary>
        private const float ViewTypeControlHeight = 22f;
        /// <summary>
        ///   <para>变量列表最大高度。</para>
        /// </summary>
        private const float VariableListMaxHeight = 260f;
        /// <summary>
        ///   <para>运行时列表最小高度。</para>
        /// </summary>
        private const float RuntimeListMinHeight = 42f;
        /// <summary>
        ///   <para>运行时列表最大高度。</para>
        /// </summary>
        private const float RuntimeListMaxHeight = 220f;

        /// <summary>
        ///   <para>页面类型名称。</para>
        /// </summary>
        private SerializedProperty m_ViewTypeName;
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
        ///   <para>显示页面局部组件。</para>
        /// </summary>
        private bool m_ShowViewParts;
        /// <summary>
        ///   <para>显示部件。</para>
        /// </summary>
        private bool m_ShowWidgets;
        /// <summary>
        ///   <para>页面局部组件滚动位置。</para>
        /// </summary>
        private Vector2 m_ViewPartsScrollPosition;
        /// <summary>
        ///   <para>部件滚动位置。</para>
        /// </summary>
        private Vector2 m_WidgetsScrollPosition;
        /// <summary>
        ///   <para>变量列表滚动位置。</para>
        /// </summary>
        private Vector2 m_VariableListScrollPosition;
        /// <summary>
        ///   <para>已展开的局部组件部件列表。</para>
        /// </summary>
        private readonly HashSet<ViewPartBase> m_ExpandedPartWidgets = new();
        /// <summary>
        ///   <para>变量搜索文本。</para>
        /// </summary>
        private string m_VariableSearchText = string.Empty;

        /// <summary>
        ///   <para>启用时初始化。</para>
        /// </summary>
        private void OnEnable()
        {
            m_ViewTypeName = serializedObject.FindProperty(UIComponentEditorUtility.ViewTypeNameField);
            m_Variables = serializedObject.FindProperty(UIComponentEditorUtility.VariablesField);
            m_VariableSignature = serializedObject.FindProperty(UIComponentEditorUtility.VariableSignatureField);
            m_EventSignature = serializedObject.FindProperty(UIComponentEditorUtility.EventSignatureField);
            if (UIComponentEditorUtility.CanEditComponent(target))
            {
                TrySelectViewFromPrefabPath();
            }
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

            var isPlaying = Application.isPlaying;
            var canEdit = UIComponentEditorUtility.CanEditComponent(target);
            var selectedViewType = GetViewType();
            DrawViewType(selectedViewType, canEdit);

            var canEditVariables = canEdit && selectedViewType != null;
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

            var validationError = ValidateVariables(selectedViewType);
            var variables = validationError == null ? GetVariables() : null;
            var variableSignature = variables == null ? 0 : UIVariablesGenerator.CalculateSignature(variables);
            var eventSignature = variables == null ? 0 : UIEventsGenerator.CalculateSignature(variables);
            if (validationError != null)
            {
                EditorGUILayout.HelpBox(validationError, MessageType.Error);
            }
            else if (selectedViewType != null &&
                     (variableSignature != m_VariableSignature.intValue ||
                      eventSignature != m_EventSignature.intValue))
            {
                EditorGUILayout.HelpBox("变量或事件配置已修改，请重新生成代码。", MessageType.Warning);
            }

            var codeIsCurrent = variables != null &&
                                variableSignature == m_VariableSignature.intValue &&
                                eventSignature == m_EventSignature.intValue;
            var canGenerate = canEditVariables && variables != null;
            using (new EditorGUI.DisabledScope(!canGenerate))
            {
                if (GUILayout.Button(
                        codeIsCurrent ? "重新生成代码" : "生成代码",
                        GUILayout.MinHeight(30f)))
                {
                    try
                    {
                        Generate();
                    }
                    catch (Exception exception)
                    {
                        EditorUtility.DisplayDialog("生成 UI 代码", exception.Message, "确定");
                    }

                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.Space(4f);
            if (isPlaying)
            {
                DrawViewParts();
                DrawWidgets();
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///   <para>绘制页面类型。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        /// <param name="canEdit">允许编辑。</param>
        private void DrawViewType(Type viewType, bool canEdit)
        {
            var rect = EditorGUILayout.GetControlRect(false, ViewTypeControlHeight);
            var component = (UIViewComponent)target;
            var script = viewType == null ? null : UIVariablesGenerator.FindTypeScript(viewType);
            var scriptPath = script == null ? null : AssetDatabase.GetAssetPath(script);
            var prefabAsset = GetPrefabAsset(component, viewType);
            var fieldRect = EditorGUI.PrefixLabel(rect, new GUIContent("View 类型"));
            var scriptContent = viewType == null
                ? new GUIContent("未选择", "选择一个继承自 ViewBase 的 View")
                : new GUIContent(viewType.Name, scriptPath ?? "未找到 View 脚本");
            if (script == null && viewType != null)
            {
                scriptContent.tooltip = "未找到 View 指向的脚本文件";
            }

            var availableGroupWidth = Mathf.Max(0f, fieldRect.width);
            var buttonCount = prefabAsset == null ? 1f : 2f;
            var dropdownWidth = Mathf.Min(fieldRect.height, availableGroupWidth / buttonCount);
            var prefabButtonWidth = prefabAsset == null ? 0f : dropdownWidth;
            var preferredScriptWidth = GUI.skin.button.CalcSize(scriptContent).x + 24f;
            var fixedButtonWidth = dropdownWidth + prefabButtonWidth;
            var preferredGroupWidth = Mathf.Max(
                ViewTypeButtonMinWidth + fixedButtonWidth,
                preferredScriptWidth + fixedButtonWidth);
            var groupWidth = Mathf.Min(availableGroupWidth, preferredGroupWidth);
            var scriptButtonWidth = Mathf.Max(0f, groupWidth - fixedButtonWidth);
            var groupX = fieldRect.xMax - groupWidth;
            var prefabRect = new Rect(
                groupX,
                fieldRect.y,
                prefabButtonWidth,
                fieldRect.height);
            var scriptRect = new Rect(
                prefabRect.xMax,
                fieldRect.y,
                scriptButtonWidth,
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
                dropdownContent.tooltip = "选择 View 类型";
                if (GUI.Button(dropdownRect, dropdownContent))
                {
                    ShowViewTypePopup(dropdownRect);
                }
            }
        }

        /// <summary>
        ///   <para>取得场景或运行时实例对应的源 Prefab；Prefab 资源和 Prefab Stage 不显示该入口。</para>
        /// </summary>
        /// <param name="component">组件。</param>
        /// <param name="viewType">页面类型。</param>
        private static GameObject GetPrefabAsset(UIViewComponent component, Type viewType)
        {
            if (component == null || component.gameObject == null ||
                EditorUtility.IsPersistent(component.gameObject) ||
                PrefabStageUtility.GetPrefabStage(component.gameObject) != null ||
                !component.gameObject.scene.IsValid())
            {
                return null;
            }

            var prefabAsset = CoreEditorUtility.GetSourcePrefabAsset(component);
            var prefabPath = string.Empty;

            // 运行时实例可能已经断开 Prefab 映射；ViewConfig 是稳定的资源路径兜底。
            if (prefabAsset == null)
            {
                var configuredViewType = viewType ?? component.RuntimeView?.GetType();
                prefabPath = configuredViewType?.GetCustomAttribute<ViewConfigAttribute>(true)?.AssetPath;
            }

            if (prefabAsset != null)
            {
                return prefabAsset;
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                return null;
            }

            prefabPath = Game.PathUtility.Normalize(prefabPath);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        /// <summary>
        ///   <para>显示当前运行时 <see cref="ViewPartBase"/> 及其直接持有的 <see cref="WidgetBase"/>。</para>
        /// </summary>
        private void DrawViewParts()
        {
            var component = (UIViewComponent)target;
            var view = component.RuntimeView;
            var parts = view?.ViewParts;
            var partCount = parts?.Count ?? 0;
            if (partCount <= 0) return;
            m_ShowViewParts = EditorGUILayout.Foldout(
                m_ShowViewParts,
                $"ViewParts ({partCount})",
                true);
            if (!m_ShowViewParts)
            {
                return;
            }

            var contentHeight = GetViewPartsContentHeight(parts);
            var scrollHeight = Mathf.Clamp(
                contentHeight,
                RuntimeListMinHeight,
                RuntimeListMaxHeight);
            m_ViewPartsScrollPosition = EditorGUILayout.BeginScrollView(
                m_ViewPartsScrollPosition,
                GUILayout.Height(scrollHeight));
            try
            {
                for (var i = 0; i < parts.Count; i++)
                {
                    var part = parts[i];
                    if (part == null)
                    {
                        continue;
                    }

                    var widgets = part.Widgets;
                    var widgetCount = widgets?.Count ?? 0;
                    var partObject = part.GameObject;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(
                            new GUIContent(string.Empty, part.GetType().FullName),
                            partObject,
                            typeof(GameObject),
                            true);
                    }

                    if (widgetCount <= 0)
                    {
                        continue;
                    }

                    EditorGUI.indentLevel++;
                    try
                    {
                        var widgetsExpanded = m_ExpandedPartWidgets.Contains(part);
                        var nextWidgetsExpanded = EditorGUILayout.Foldout(
                            widgetsExpanded,
                            $"Widgets ({widgetCount})",
                            true);
                        if (nextWidgetsExpanded != widgetsExpanded)
                        {
                            if (nextWidgetsExpanded)
                            {
                                m_ExpandedPartWidgets.Add(part);
                            }
                            else
                            {
                                m_ExpandedPartWidgets.Remove(part);
                            }
                        }

                        if (!nextWidgetsExpanded)
                        {
                            continue;
                        }

                        EditorGUI.indentLevel++;
                        try
                        {
                            for (var widgetIndex = 0; widgetIndex < widgets.Count; widgetIndex++)
                            {
                                var widget = widgets[widgetIndex];
                                if (widget == null)
                                {
                                    continue;
                                }

                                using (new EditorGUI.DisabledScope(true))
                                {
                                    EditorGUILayout.ObjectField(
                                        new GUIContent(
                                            widget.GetType().Name,
                                            widget.GetType().FullName),
                                        widget.GameObject,
                                        typeof(GameObject),
                                        true);
                                }
                            }
                        }
                        finally
                        {
                            EditorGUI.indentLevel--;
                        }
                    }
                    finally
                    {
                        EditorGUI.indentLevel--;
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        ///   <para>获取页面局部组件内容高度。</para>
        /// </summary>
        /// <param name="parts">路径片段。</param>
        private float GetViewPartsContentHeight(IReadOnlyList<ViewPartBase> parts)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var height = 0f;
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part == null)
                {
                    continue;
                }

                height += lineHeight;
                var widgets = part.Widgets;
                if (widgets == null || widgets.Count == 0)
                {
                    continue;
                }

                height += lineHeight;
                if (!m_ExpandedPartWidgets.Contains(part))
                {
                    continue;
                }

                for (var widgetIndex = 0; widgetIndex < widgets.Count; widgetIndex++)
                {
                    if (widgets[widgetIndex] != null)
                    {
                        height += lineHeight;
                    }
                }
            }

            return Mathf.Max(RuntimeListMinHeight, height + 4f);
        }

        /// <summary>
        ///   <para>折叠显示当前运行时 <see cref="ViewBase"/> 直接持有的 <see cref="WidgetBase"/>。</para>
        /// </summary>
        private void DrawWidgets()
        {
            var component = (UIViewComponent)target;
            var view = component.RuntimeView;
            var widgets = view?.Widgets;
            var widgetCount = widgets?.Count ?? 0;
            if (widgetCount <= 0)
            {
                return;
            }

            m_ShowWidgets = EditorGUILayout.Foldout(
                m_ShowWidgets,
                $"Widgets ({widgetCount})",
                true);
            if (!m_ShowWidgets)
            {
                return;
            }

            var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            GetRuntimeInfoColumnRects(headerRect, out var classHeaderRect, out var objectHeaderRect, out var stateHeaderRect);
            EditorGUI.LabelField(classHeaderRect, "类名", EditorStyles.miniLabel);
            EditorGUI.LabelField(objectHeaderRect, "对象", EditorStyles.miniLabel);
            EditorGUI.LabelField(stateHeaderRect, "状态", EditorStyles.miniLabel);

            var scrollHeight = Mathf.Clamp(
                EditorGUIUtility.singleLineHeight * widgetCount + 4f,
                RuntimeListMinHeight,
                RuntimeListMaxHeight);
            m_WidgetsScrollPosition = EditorGUILayout.BeginScrollView(
                m_WidgetsScrollPosition,
                GUILayout.Height(scrollHeight));
            try
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    for (var i = 0; i < widgets.Count; i++)
                    {
                        var widget = widgets[i];
                        if (widget == null)
                        {
                            continue;
                        }

                        var rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                        GetRuntimeInfoColumnRects(rowRect, out var classRect, out var objectRect, out var stateRect);
                        EditorGUI.LabelField(classRect, widget.GetType().FullName);
                        EditorGUI.ObjectField(objectRect, GUIContent.none, widget.GameObject, typeof(GameObject), true);
                        EditorGUI.LabelField(
                            stateRect,
                            widget.IsReleased ? "已释放" : widget.IsCreated ? "已创建" : "创建中");
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        ///   <para>获取运行时信息列区域。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="classRect">类区域。</param>
        /// <param name="objectRect">对象区域。</param>
        /// <param name="stateRect">状态区域。</param>
        private static void GetRuntimeInfoColumnRects(
            Rect rect,
            out Rect classRect,
            out Rect objectRect,
            out Rect stateRect)
        {
            const float spacing = 4f;
            var stateWidth = Mathf.Clamp(rect.width * 0.22f, 48f, 56f);
            var contentWidth = Mathf.Max(0f, rect.width - spacing * 2f);
            var classWidth = contentWidth * 0.46f;
            var objectWidth = Mathf.Max(0f, contentWidth - classWidth - stateWidth);
            classRect = new Rect(rect.x, rect.y, classWidth, rect.height);
            objectRect = new Rect(classRect.xMax + spacing, rect.y, objectWidth, rect.height);
            stateRect = new Rect(objectRect.xMax + spacing, rect.y, stateWidth, rect.height);
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
            var nameProperty = element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField);
            var valueProperty = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField);
            var eventIdsProperty = element.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField);
            var selectedValue = valueProperty.objectReferenceValue;
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
            EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
            var displayValue = selectedComponent != null ? node : selectedValue;
            EditorGUI.BeginChangeCheck();
            var nextObject = EditorGUI.ObjectField(
                nodeRect,
                displayValue,
                typeof(UnityEngine.Object),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                if (UIComponentEditorUtility.CanAssignValue((UIViewComponent)target, nextObject))
                {
                    selectedValue = nextObject is GameObject gameObject &&
                                     !EditorUtility.IsPersistent(gameObject)
                        ? gameObject.transform
                        : nextObject;
                    selectedComponent = selectedValue as Component;
                    node = selectedComponent != null ? selectedComponent.gameObject : null;
                    valueProperty.objectReferenceValue = selectedValue;
                    eventIdsProperty.arraySize = 0;
                    if (nextObject != null && string.IsNullOrWhiteSpace(nameProperty.stringValue))
                    {
                        nameProperty.stringValue = UIVariablesGenerator.CreateIdentifier(nextObject.name);
                    }
                }
            }

            DrawTypePopup(typeRect, index, node, selectedComponent, selectedValue);
            var eventDefinitions = selectedComponent == null
                ? Array.Empty<UIEventDefinition>()
                : UIEventsGenerator.GetEventDefinitions(selectedComponent.GetType());
            var eventIds = UIComponentEditorUtility.ReadEventIds(eventIdsProperty);
            if (UIComponentEditorUtility.CanEditBinding(
                    (UIViewComponent)target,
                    selectedComponent) &&
                UIComponentEditorUtility.RemoveUnsupportedEvents(eventIds, eventDefinitions))
            {
                UIComponentEditorUtility.WriteEventIds(eventIdsProperty, eventIds);
            }

            DrawEventPopup(eventRect, index, eventIds, eventDefinitions);
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
        ///   <para>绘制事件弹窗。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="variableIndex">变量索引。</param>
        /// <param name="selectedIds">选中标识。</param>
        /// <param name="definitions">定义。</param>
        private void DrawEventPopup(
            Rect rect,
            int variableIndex,
            IReadOnlyList<string> selectedIds,
            IReadOnlyList<UIEventDefinition> definitions)
        {
            var hasEvents = definitions.Count > 0;
            UIComponentEditorUtility.BuildEventOptions(
                selectedIds,
                definitions,
                out var label,
                out var options,
                out var current);
            var tooltip = selectedIds.Count == 0 ? "选择要生成绑定代码的事件" : label;
            using (new EditorGUI.DisabledScope(
                       !hasEvents ||
                       !UIComponentEditorUtility.CanEditBinding(
                           (UIViewComponent)target,
                           UIComponentEditorUtility.GetVariableComponent(m_Variables, variableIndex))))
            {
                if (!EditorGUI.DropdownButton(
                        rect,
                        new GUIContent(label, tooltip),
                        FocusType.Passive))
                {
                    return;
                }
            }

            if (!hasEvents)
            {
                return;
            }

            PopupWindow.Show(
                rect,
                new SearchableOptionsPopup(
                    current,
                    options,
                    selected => SetEventIds(
                        variableIndex,
                        UIComponentEditorUtility.GetEventIds(selected, definitions)),
                    $"UIBindingEvents_{variableIndex}",
                    "绑定事件"));
        }

        /// <summary>
        ///   <para>设置事件标识列表。</para>
        /// </summary>
        /// <param name="index">索引。</param>
        /// <param name="eventIds">事件标识列表。</param>
        private void SetEventIds(int index, IReadOnlyList<string> eventIds)
        {
            if (!UIComponentEditorUtility.CanEditBinding(
                    (UIViewComponent)target,
                    UIComponentEditorUtility.GetVariableComponent(m_Variables, index)))
            {
                return;
            }

            serializedObject.Update();
            if ((uint)index < (uint)m_Variables.arraySize)
            {
                var property = m_Variables.GetArrayElementAtIndex(index)
                    .FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField);
                UIComponentEditorUtility.WriteEventIds(property, eventIds);
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        ///   <para>绘制变量表头。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        private static void DrawVariablesHeader(Rect rect)
        {
            UIComponentEditorUtility.GetVariableRects(
                rect,
                out var nameRect,
                out var nodeRect,
                out var typeRect,
                out var eventRect);
            EditorGUI.LabelField(nameRect, "变量名");
            EditorGUI.LabelField(nodeRect, "引用");
            EditorGUI.LabelField(typeRect, "类型");
            EditorGUI.LabelField(eventRect, "事件");
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

            var index = m_Variables.arraySize;
            m_Variables.arraySize++;
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
        ///   <para>显示节点对象及其组件类型选择弹窗。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="variableIndex">变量索引。</param>
        /// <param name="node">节点。</param>
        /// <param name="selectedValue">选中值。</param>
        /// <param name="selectedObject">选中对象。</param>
        private void DrawTypePopup(
            Rect rect,
            int variableIndex,
            GameObject node,
            Component selectedValue,
            UnityEngine.Object selectedObject)
        {
            if (selectedObject != null && !(selectedObject is Component))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.DropdownButton(
                        rect,
                        new GUIContent(UIComponentEditorUtility.GetValueTypeLabel(selectedObject)),
                        FocusType.Passive);
                }

                return;
            }

            if (node == null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.DropdownButton(
                        rect,
                        new GUIContent("未选择"),
                        FocusType.Passive);
                }

                return;
            }

            using (new EditorGUI.DisabledScope(
                       !UIComponentEditorUtility.CanEditObject(
                           (UIViewComponent)target,
                           node)))
            {
                if (!EditorGUI.DropdownButton(
                        rect,
                        new GUIContent(selectedValue == null
                            ? "未选择"
                            : UIVariablesGenerator.GetTypeName(selectedValue.GetType())),
                        FocusType.Passive))
                {
                    return;
                }
            }

            UIComponentEditorUtility.BuildComponentOptions(
                node,
                selectedValue,
                out var currentLabel,
                out var optionLabels,
                out var optionValues);
            PopupWindow.Show(
                rect,
                new SearchableOptionsPopup(
                    currentLabel,
                    optionLabels,
                    selectedLabel =>
                    {
                        var selectedIndex = Array.IndexOf(optionLabels, selectedLabel);
                        if ((uint)selectedIndex < (uint)optionValues.Length)
                        {
                            SetVariableValue(variableIndex, optionValues[selectedIndex]);
                        }
                    },
                    $"UIBindingType_{variableIndex}",
                    "组件类型",
                    "."));
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

            if (!UIComponentEditorUtility.CanAssignValue((UIViewComponent)target, value))
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
        ///   <para>显示可搜索的 <see cref="ViewBase"/> 类型选择弹窗。</para>
        /// </summary>
        /// <param name="anchorRect">锚点区域。</param>
        private void ShowViewTypePopup(Rect anchorRect)
        {
            var labels = new List<string>();
            var types = new List<Type>();
            var currentLabel = "未选择";
            var availableTypes = UIComponentEditorUtility.GetViewTypes();
            foreach (var type in availableTypes)
            {
                labels.Add(type.FullName);
                types.Add(type);
                if (string.Equals(m_ViewTypeName.stringValue, type.AssemblyQualifiedName, StringComparison.Ordinal))
                {
                    currentLabel = labels[^1];
                }
            }

            var optionLabels = labels.ToArray();
            var optionTypes = types.ToArray();
            PopupWindow.Show(
                anchorRect,
                new SearchableOptionsPopup(
                    currentLabel,
                    optionLabels,
                    selectedLabel =>
                    {
                        var selectedIndex = Array.IndexOf(optionLabels, selectedLabel);
                        if ((uint)selectedIndex < (uint)optionTypes.Length)
                        {
                            SetViewType(optionTypes[selectedIndex]);
                        }
                    },
                    "UIViewType",
                    "View 类型",
                    "."));
        }

        /// <summary>
        ///   <para>设置页面类型。</para>
        /// </summary>
        /// <param name="type">类型。</param>
        private void SetViewType(Type type)
        {
            if (!UIComponentEditorUtility.CanEditComponent(target) ||
                !UIComponentEditorUtility.IsConcreteViewType(type))
            {
                return;
            }

            serializedObject.Update();
            if (string.Equals(m_ViewTypeName.stringValue, type.AssemblyQualifiedName, StringComparison.Ordinal))
            {
                return;
            }

            m_ViewTypeName.stringValue = type.AssemblyQualifiedName;
            m_VariableSignature.intValue = 0;
            m_EventSignature.intValue = 0;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        ///   <para>按预制体路径选择页面类型。</para>
        /// </summary>
        private void TrySelectViewFromPrefabPath()
        {
            if (!string.IsNullOrEmpty(m_ViewTypeName.stringValue))
            {
                return;
            }

            var component = (UIViewComponent)target;
            var prefabPath = AssetDatabase.GetAssetPath(component.gameObject);
            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component.gameObject);
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                return;
            }

            Type matchedType = null;
            foreach (var type in UIComponentEditorUtility.GetViewTypes())
            {
                var attribute = type.GetCustomAttribute<ViewConfigAttribute>(true);
                if (attribute == null || !string.Equals(
                        Game.PathUtility.Normalize(attribute.AssetPath),
                        Game.PathUtility.Normalize(prefabPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (matchedType != null)
                {
                    return;
                }

                matchedType = type;
            }

            if (matchedType != null)
            {
                m_ViewTypeName.stringValue = matchedType.AssemblyQualifiedName;
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        ///   <para>获取页面类型。</para>
        /// </summary>
        private Type GetViewType()
        {
            var type = UIComponentEditorUtility.ReadType(m_ViewTypeName);
            return UIComponentEditorUtility.IsConcreteViewType(type) ? type : null;
        }

        /// <summary>
        ///   <para>校验变量。</para>
        /// </summary>
        /// <param name="viewType">页面类型。</param>
        private string ValidateVariables(Type viewType)
        {
            var compositionError = UICompositionPolicy.GetCompositionError((UIViewComponent)target);
            if (compositionError != null)
            {
                return compositionError;
            }

            if (viewType == null)
            {
                return null;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < m_Variables.arraySize; i++)
            {
                var element = m_Variables.GetArrayElementAtIndex(i);
                var propertyName = element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField).stringValue;
                var rawValue = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField).objectReferenceValue;
                var value = rawValue as Component;
                if (rawValue == null)
                {
                    return $"变量“{propertyName}”尚未指定引用对象。";
                }
                if (!UIVariablesGenerator.IsValidIdentifier(propertyName))
                {
                    return $"第 {i + 1} 个变量需要一个有效的 C# 名称。";
                }

                if (!names.Add(propertyName))
                {
                    return $"变量名“{propertyName}”重复。";
                }

                var targetNode = UICompositionPolicy.GetReferenceNode(rawValue);
                if (targetNode != null && !UIComponentEditorUtility.IsOwnedObject(
                        ((UIViewComponent)target).gameObject,
                        targetNode))
                {
                    return $"变量“{propertyName}”必须引用当前 Prefab 内的节点。";
                }

                var bindingError = UICompositionPolicy.GetBindingError(
                    (UIViewComponent)target,
                    targetNode);
                if (bindingError != null)
                {
                    return $"变量“{propertyName}”：{bindingError}";
                }

                if (UIComponentEditorUtility.HasMemberConflict(viewType, propertyName))
                {
                    return $"变量名“{propertyName}”与 {viewType.Name} 成员冲突。";
                }
            }

            return null;
        }

        /// <summary>
        ///   <para>生成。</para>
        /// </summary>
        private void Generate()
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            var viewType = GetViewType();
            if (viewType == null)
            {
                throw new InvalidOperationException("生成 UI 代码前，请先选择有效的 View 类型。");
            }

            var validationError = ValidateVariables(viewType);
            if (validationError != null)
            {
                throw new InvalidOperationException(validationError);
            }

            var variables = GetVariables();
            var variableSignature = UIVariablesGenerator.CalculateSignature(variables);
            var eventSignature = UIEventsGenerator.CalculateSignature(variables);
            AssetDatabase.StartAssetEditing();
            try
            {
                UIVariablesGenerator.Generate(viewType, variables, variableSignature);
                UIEventsGenerator.Generate(viewType, variables);
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
            var variables = new List<UIVariableDefinition>(m_Variables.arraySize);
            for (var i = 0; i < m_Variables.arraySize; i++)
            {
                var element = m_Variables.GetArrayElementAtIndex(i);
                var propertyName = element.FindPropertyRelative(UIComponentEditorUtility.VariableNameField).stringValue;
                var value = element.FindPropertyRelative(UIComponentEditorUtility.VariableValueField).objectReferenceValue;
                if (value == null) return null;
                var eventIds = UIComponentEditorUtility.ReadEventIds(
                    element.FindPropertyRelative(UIComponentEditorUtility.VariableEventIdsField));
                variables.Add(new UIVariableDefinition(propertyName, value.GetType(), eventIds));
            }

            return variables;
        }
    }
}

#endif
