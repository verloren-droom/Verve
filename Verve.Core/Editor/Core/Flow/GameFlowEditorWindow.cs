#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using UnityEditor.Callbacks;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>流程节点编辑窗口。</para>
    /// </summary>
    sealed class GameFlowEditorWindow : EditorWindow
    {
        private const float k_NodeWidth = 230f;
        private const float k_NodeHeight = 104f;
        private const float k_PortRadius = 5f;
        private const float k_DetailWidth = 200f;
        private const float k_MinDetailWidth = 220f;
        private const float k_MaxDetailWidth = 560f;
        private const float k_GridSize = 24f;
        private const float k_MinCanvasZoom = 0.75f;
        private const float k_MaxCanvasZoom = 1.25f;
        private const float k_RuntimeBannerHeight = 22f;

        [SerializeField] private GameFlowAsset m_Flow;
        [SerializeField] private Vector2 m_CanvasScroll;
        [SerializeField] private float m_CanvasZoom = 1f;
        [SerializeField] private float m_DetailWidth = k_DetailWidth;

        private SerializedObject m_SerializedObject;
        private SerializedProperty m_Steps;
        [SerializeField] private Vector2 m_DetailsScroll;
        [NonSerialized] private Type[] m_HandlerTypes;
        [NonSerialized] private Type[] m_ConditionTypes;
        [NonSerialized] private string[] m_ConditionOptions;
        private GameFlow m_RuntimeFlow;
        private Vector2 m_PanStartMouse;
        private Vector2 m_PanStartScroll;
        private Vector2 m_ConnectionMousePosition;
        private string m_ContextSourceId;
        private readonly HashSet<int> m_SelectedIndices = new();
        private readonly Dictionary<int, Vector2> m_SelectedDragOffsets = new();

        private enum OutputPort : sbyte
        {
            None = -1,
            True = 0,
            False = 1,
            Default = 2
        }

        private OutputPort m_ContextBranch = OutputPort.None;
        private bool m_IsConnecting;
        private int m_SelectedIndex = -1;
        private bool m_IsDraggingNodes;
        private int m_PanControlId;
        private int m_DragControlId;
        private int m_DragUndoGroup;
        private int m_DetailSplitterControlId;
        private bool m_IsPanning;
        private bool m_IsResizingDetails;
        private bool m_IsSelecting;
        private Vector2 m_SelectionStart;
        private Vector2 m_SelectionCurrent;
        private int m_SelectionControlId;
        private bool m_SelectionAdditive;
        private float m_NodeStyleZoom = -1f;
        private GUIStyle m_NodeTitleStyle;
        private GUIStyle m_NodeMetaStyle;
        private GUIStyle m_NodePopupStyle;
        private GUIStyle m_RuntimeBannerStyle;

        private bool IsFlowEditingLocked =>
            IsFlowExecutionLocked(m_RuntimeFlow, EditorApplication.isPlaying);

        private static bool IsFlowExecutionLocked(GameFlow flow, bool isPlaying)
        {
            return isPlaying && flow != null && flow.State != GameFlowState.Created;
        }

        private static class Styles
        {
            public static readonly GUIStyle Canvas = new(EditorStyles.helpBox);
            public static readonly GUIStyle Node = new(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8)
            };
            public static readonly GUIStyle NodeTitle = new(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            public static readonly GUIStyle NodeMeta = new(EditorStyles.miniLabel)
            {
                normal = { textColor = Color.gray },
                clipping = TextClipping.Clip
            };
            public static readonly GUIStyle NodeStatus = new(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            var flow = EditorUtility.EntityIdToObject(instanceId) as GameFlowAsset;
            if (flow == null) return false;

            Open(flow);
            return true;
        }

        internal static void Open(GameFlowAsset flow)
        {
            var window = GetWindow<GameFlowEditorWindow>("流程编辑器");
            window.SetFlow(flow);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeTypeOptions();
            GameFlowAsset.FlowCreated += OnFlowCreated;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void InitializeTypeOptions()
        {
            m_HandlerTypes = TypeCache.GetTypesDerivedFrom<GameFlowStepHandler>()
                .Where(IsSelectableType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
            m_ConditionTypes = TypeCache.GetTypesDerivedFrom<GameFlowCondition>()
                .Where(IsSelectableType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
            m_ConditionOptions = new[] { "无条件" }
                .Concat(m_ConditionTypes.Select(GetDisplayName))
                .ToArray();
        }

        private void OnGUI()
        {
            wantsMouseMove = true;
            DrawToolbar();
            if (m_Flow == null)
            {
                EditorGUILayout.HelpBox("请从 Project 窗口选择一个流程资产。", MessageType.Info);
                return;
            }

            if (m_SerializedObject == null || m_SerializedObject.targetObject != m_Flow)
                BindAsset();

            m_SerializedObject.Update();
            var content = GUILayoutUtility.GetRect(0f, 0f,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var detailWidth = Mathf.Clamp(m_DetailWidth, k_MinDetailWidth, k_MaxDetailWidth);
            var splitter = new Rect(content.xMax - detailWidth - 2f, content.y, 4f, content.height);
            var canvas = new Rect(content.x, content.y, splitter.x - content.x, content.height);
            var details = new Rect(splitter.xMax, content.y, detailWidth, content.height);

            HandleDetailsSplitter(splitter, content);
            DrawCanvas(canvas);
            DrawDetails(details);

            if (m_SerializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(m_Flow);
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("流程资源", GUILayout.Width(60f));
            GUILayout.Space(4f);
            var flow = EditorGUILayout.ObjectField(m_Flow, typeof(GameFlowAsset), false,
                GUILayout.Width(k_DetailWidth)) as GameFlowAsset;
            if (EditorGUI.EndChangeCheck())
                SetFlow(flow);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void HandleDetailsSplitter(Rect splitter, Rect content)
        {
            EditorGUIUtility.AddCursorRect(splitter, MouseCursor.ResizeHorizontal);
            var currentEvent = Event.current;
            m_DetailSplitterControlId = GUIUtility.GetControlID(FocusType.Passive);
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 &&
                splitter.Contains(currentEvent.mousePosition))
            {
                m_IsResizingDetails = true;
                GUIUtility.hotControl = m_DetailSplitterControlId;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && m_IsResizingDetails &&
                GUIUtility.hotControl == m_DetailSplitterControlId)
            {
                m_DetailWidth = Mathf.Clamp(content.xMax - currentEvent.mousePosition.x,
                    k_MinDetailWidth, k_MaxDetailWidth);
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.rawType == EventType.MouseUp && m_IsResizingDetails)
            {
                m_IsResizingDetails = false;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }
        }

        private void DrawDetails(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            var contentWidth = Mathf.Max(0f, rect.width - 20f);
            var contentHeight = GetDetailsHeight(contentWidth);
            var viewRect = new Rect(0f, 0f, contentWidth, contentHeight);
            m_DetailsScroll = GUI.BeginScrollView(rect, m_DetailsScroll, viewRect);

            if (m_Flow == null || m_Steps == null || (uint)m_SelectedIndex >= (uint)m_Steps.arraySize)
            {
                GUI.Label(new Rect(8f, 8f, contentWidth - 16f, EditorGUIUtility.singleLineHeight),
                    "选择一个步骤查看参数", EditorStyles.centeredGreyMiniLabel);
                GUI.EndScrollView();
                return;
            }

            if (m_SelectedIndices.Count > 1)
            {
                GUI.Label(new Rect(8f, 8f, contentWidth - 16f, EditorGUIUtility.singleLineHeight),
                    $"已选择 {m_SelectedIndices.Count} 个步骤", EditorStyles.centeredGreyMiniLabel);
                GUI.EndScrollView();
                return;
            }

            var element = m_Steps.GetArrayElementAtIndex(m_SelectedIndex);
            var y = 8f;
            GUI.Label(new Rect(8f, y, contentWidth - 16f, EditorGUIUtility.singleLineHeight),
                "步骤参数", EditorStyles.boldLabel);
            y += EditorGUIUtility.singleLineHeight + 6f;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(new Rect(8f, y, contentWidth - 16f, EditorGUIUtility.singleLineHeight),
                "Id", element.FindPropertyRelative("m_Id").stringValue);
            EditorGUI.EndDisabledGroup();
            y += EditorGUIUtility.singleLineHeight + 4f;

            EditorGUI.BeginDisabledGroup(IsFlowEditingLocked);
            y = DrawProperty(ref y, contentWidth, element.FindPropertyRelative("m_Name"), "名称");
            y = DrawProperty(ref y, contentWidth, element.FindPropertyRelative("m_Handler"), "处理器");

            var condition = element.FindPropertyRelative("m_Condition");
            y += 4f;
            if (condition.managedReferenceValue == null)
            {
                GUI.Label(new Rect(8f, y, contentWidth - 16f, EditorGUIUtility.singleLineHeight),
                    "条件：无条件");
                y += EditorGUIUtility.singleLineHeight + 4f;
            }
            else
            {
                y = DrawProperty(ref y, contentWidth, condition, "条件");
                y = DrawTargetField(y, contentWidth, "True 目标", element.FindPropertyRelative("m_TrueTargetStepId"));
                y = DrawTargetField(y, contentWidth, "False 目标", element.FindPropertyRelative("m_FalseTargetStepId"));
            }

            if (GUI.Button(new Rect(8f, y + 4f, contentWidth - 16f, 22f), "删除步骤"))
                RemoveStep(m_SelectedIndex);
            EditorGUI.EndDisabledGroup();
            GUI.EndScrollView();
        }

        private float GetDetailsHeight(float contentWidth)
        {
            if (m_Steps == null || (uint)m_SelectedIndex >= (uint)m_Steps.arraySize ||
                m_SelectedIndices.Count > 1)
                return 40f;

            var element = m_Steps.GetArrayElementAtIndex(m_SelectedIndex);
            var height = EditorGUIUtility.singleLineHeight + 14f;
            height += EditorGUIUtility.singleLineHeight + 4f;
            height += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("m_Name"), true) + 2f;
            height += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("m_Handler"), true) + 6f;
            var condition = element.FindPropertyRelative("m_Condition");
            height += condition.managedReferenceValue == null
                ? EditorGUIUtility.singleLineHeight + 8f
                : EditorGUI.GetPropertyHeight(condition, true) + EditorGUIUtility.singleLineHeight * 2f + 14f;
            return height + 34f;
        }

        private static float DrawProperty(ref float y, float contentWidth, SerializedProperty property, string label)
        {
            var height = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(new Rect(8f, y, contentWidth - 16f, height), property,
                new GUIContent(label), true);
            return y += height + 2f;
        }

        private float DrawTargetField(float y, float contentWidth, string label, SerializedProperty target)
        {
            var names = new List<string> { "（结束流程）" };
            var ids = new List<string> { string.Empty };
            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var step = m_Steps.GetArrayElementAtIndex(i);
                names.Add($"{i + 1}. {step.FindPropertyRelative("m_Name").stringValue}");
                ids.Add(step.FindPropertyRelative("m_Id").stringValue);
            }

            var selected = Mathf.Max(0, ids.IndexOf(target.stringValue));
            EditorGUI.BeginChangeCheck();
            var next = EditorGUI.Popup(new Rect(8f, y, contentWidth - 16f, EditorGUIUtility.singleLineHeight),
                label, selected, names.ToArray());
            if (EditorGUI.EndChangeCheck() && next != selected)
            {
                Undo.RecordObject(m_Flow, "连接流程步骤");
                target.stringValue = ids[next];
                EditorUtility.SetDirty(m_Flow);
            }

            return y + EditorGUIUtility.singleLineHeight + 2f;
        }

        private void SetCondition(int index, int option)
        {
            if (IsFlowEditingLocked) return;
            if (m_Steps == null || (uint)index >= (uint)m_Steps.arraySize) return;
            m_SerializedObject.Update();
            var element = m_Steps.GetArrayElementAtIndex(index);
            var condition = element.FindPropertyRelative("m_Condition");
            var current = condition.managedReferenceValue == null ? 0 :
                Array.IndexOf(m_ConditionTypes, condition.managedReferenceValue.GetType()) + 1;
            if (current == option) return;
            Undo.RecordObject(m_Flow, "更改流程判断条件");
            condition.managedReferenceValue = option == 0
                ? null
                : Activator.CreateInstance(m_ConditionTypes[option - 1], true);
            if (option == 0)
                element.FindPropertyRelative("m_FalseTargetStepId").stringValue = string.Empty;
            m_SerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Flow);
            Repaint();
        }

        internal void DrawCanvas(Rect viewport)
        {
            GUI.Box(viewport, GUIContent.none, Styles.Canvas);

            GUI.BeginGroup(viewport);
            try
            {
                var localRect = new Rect(Vector2.zero, viewport.size);
                DrawGrid(localRect);
                DrawConnections();
                DrawNodes();
                DrawSelectionRect();
            }
            finally
            {
                GUI.EndGroup();
            }

            HandleCanvasInput(viewport);
        }

        private void HandleCanvasInput(Rect viewport)
        {
            if (m_IsPanning)
                EditorGUIUtility.AddCursorRect(viewport, MouseCursor.Pan);
            var currentEvent = Event.current;
            if (IsFlowEditingLocked && m_IsDraggingNodes)
                EndDrag();
            if (IsFlowEditingLocked)
                m_IsConnecting = false;
            if (currentEvent.type == EventType.KeyDown &&
                (currentEvent.keyCode == KeyCode.Delete || currentEvent.keyCode == KeyCode.Backspace) &&
                viewport.Contains(currentEvent.mousePosition) && !EditorGUIUtility.editingTextField &&
                m_SelectedIndices.Count > 0 && !IsFlowEditingLocked)
            {
                RemoveSelectedSteps();
                currentEvent.Use();
                Repaint();
                return;
            }

            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape && m_IsConnecting)
            {
                m_IsConnecting = false;
                currentEvent.Use();
                Repaint();
                return;
            }

            if (currentEvent.type == EventType.MouseMove && m_IsConnecting)
            {
                m_ConnectionMousePosition = currentEvent.mousePosition - viewport.position;
                Repaint();
            }

            if (currentEvent.type == EventType.ScrollWheel && viewport.Contains(currentEvent.mousePosition))
            {
                var oldZoom = m_CanvasZoom;
                m_CanvasZoom = Mathf.Clamp(m_CanvasZoom * Mathf.Pow(1.1f, -currentEvent.delta.y),
                    k_MinCanvasZoom, k_MaxCanvasZoom);
                if (!Mathf.Approximately(oldZoom, m_CanvasZoom))
                {
                    var localMouse = currentEvent.mousePosition - viewport.position;
                    var canvasPoint = localMouse / oldZoom + m_CanvasScroll;
                    m_CanvasScroll = canvasPoint - localMouse / m_CanvasZoom;
                    currentEvent.Use();
                    Repaint();
                    return;
                }
            }

            if (currentEvent.type == EventType.ContextClick && viewport.Contains(currentEvent.mousePosition))
            {
                if (IsFlowEditingLocked)
                {
                    var lockedNodeIndex = FindNodeIndex(currentEvent.mousePosition - viewport.position);
                    if (lockedNodeIndex >= 0)
                        SelectSingle(lockedNodeIndex);
                    currentEvent.Use();
                    return;
                }

                if (m_IsConnecting)
                {
                    m_IsConnecting = false;
                    currentEvent.Use();
                    Repaint();
                    return;
                }

                var localMouse = currentEvent.mousePosition - viewport.position;
                var nodeIndex = FindNodeIndex(localMouse);
                if (nodeIndex >= 0)
                {
                    SelectSingle(nodeIndex);
                    var nodeRect = GetNodeRect(nodeIndex);
                    var branch = GetOutputBranch(nodeIndex, localMouse);
                    if (branch != OutputPort.None)
                    {
                        var node = m_Steps.GetArrayElementAtIndex(nodeIndex);
                        m_ContextSourceId = node.FindPropertyRelative("m_Id").stringValue;
                        m_ContextBranch = branch;
                        ShowBranchTargetMenu(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition));
                    }
                    else
                    {
                        m_ContextSourceId = null;
                        ShowNodeContextMenu(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition), nodeIndex);
                    }
                }
                else
                {
                    if (TryFindConnection(localMouse, out var sourceIndex, out var connectionBranch))
                    {
                        SelectSingle(sourceIndex);
                        m_ContextSourceId = m_Steps.GetArrayElementAtIndex(sourceIndex)
                            .FindPropertyRelative("m_Id").stringValue;
                        m_ContextBranch = connectionBranch;
                        ShowBranchTargetMenu(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition));
                    }
                    else
                    {
                        m_ContextSourceId = null;
                        ShowCanvasContextMenu(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition),
                            localMouse + m_CanvasScroll);
                    }
                }

                currentEvent.Use();
                return;
            }

            var panId = GUIUtility.GetControlID(FocusType.Passive);
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 2 &&
                viewport.Contains(currentEvent.mousePosition) && GUIUtility.hotControl == 0)
            {
                m_IsPanning = true;
                m_PanControlId = panId;
                m_PanStartMouse = currentEvent.mousePosition;
                m_PanStartScroll = m_CanvasScroll;
                GUIUtility.hotControl = panId;
                currentEvent.Use();
                return;
            }

            if (currentEvent.rawType == EventType.MouseUp && currentEvent.button == 2 && m_IsPanning)
            {
                EndDrag();
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && m_IsPanning &&
                GUIUtility.hotControl == m_PanControlId)
            {
                m_CanvasScroll = m_PanStartScroll - (currentEvent.mousePosition - m_PanStartMouse) / m_CanvasZoom;
                currentEvent.Use();
                Repaint();
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 &&
                viewport.Contains(currentEvent.mousePosition) && GUIUtility.hotControl == 0)
            {
                var localMouse = currentEvent.mousePosition - viewport.position;
                if (m_IsConnecting && !IsFlowEditingLocked)
                {
                    var targetIndex = FindNodeIndex(localMouse);
                    var targetRect = targetIndex >= 0 ? GetNodeRect(targetIndex) : default;
                    if (targetIndex >= 0 && IsInputPort(targetRect, localMouse))
                    {
                        var targetId = m_Steps.GetArrayElementAtIndex(targetIndex)
                            .FindPropertyRelative("m_Id").stringValue;
                        SetBranchTarget(targetId);
                        m_IsConnecting = false;
                    }
                    else if (targetIndex < 0)
                    {
                        m_IsConnecting = false;
                    }

                    currentEvent.Use();
                    Repaint();
                    return;
                }

                for (var i = m_Steps.arraySize - 1; i >= 0; i--)
                {
                    var element = m_Steps.GetArrayElementAtIndex(i);
                    var nodeRect = GetNodeRect(i);
                    if (!nodeRect.Contains(localMouse) && GetOutputBranch(i, localMouse) == OutputPort.None) continue;

                    var branch = GetOutputBranch(i, localMouse);
                    if (branch != OutputPort.None && !IsFlowEditingLocked)
                    {
                        SelectSingle(i);
                        m_ContextSourceId = element.FindPropertyRelative("m_Id").stringValue;
                        m_ContextBranch = branch;

                        m_IsConnecting = true;
                        m_ConnectionMousePosition = localMouse;
                        currentEvent.Use();
                        Repaint();
                        return;
                    }

                    if (currentEvent.control || currentEvent.command)
                    {
                        ToggleSelection(i);
                        currentEvent.Use();
                        Repaint();
                        return;
                    }

                    if (!m_SelectedIndices.Contains(i))
                        SelectSingle(i);

                    if (IsFlowEditingLocked)
                    {
                        currentEvent.Use();
                        Repaint();
                        return;
                    }

                    m_IsDraggingNodes = true;
                    m_DragControlId = GUIUtility.GetControlID(FocusType.Passive);
                    m_SelectedDragOffsets.Clear();
                    foreach (var selectedIndex in m_SelectedIndices)
                    {
                        var selectedPosition = m_Steps.GetArrayElementAtIndex(selectedIndex)
                            .FindPropertyRelative("m_Position").vector2Value;
                        m_SelectedDragOffsets[selectedIndex] =
                            localMouse / m_CanvasZoom + m_CanvasScroll - selectedPosition;
                    }
                    Undo.IncrementCurrentGroup();
                    m_DragUndoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("移动流程步骤");
                    GUIUtility.hotControl = m_DragControlId;
                    currentEvent.Use();
                    Repaint();
                    return;
                }

                m_IsSelecting = true;
                m_SelectionControlId = GUIUtility.GetControlID(FocusType.Passive);
                m_SelectionStart = localMouse;
                m_SelectionCurrent = localMouse;
                m_SelectionAdditive = currentEvent.control || currentEvent.command;
                if (!m_SelectionAdditive)
                    ClearSelection();
                GUIUtility.hotControl = m_SelectionControlId;
                currentEvent.Use();
                Repaint();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && m_IsSelecting &&
                GUIUtility.hotControl == m_SelectionControlId)
            {
                m_SelectionCurrent = currentEvent.mousePosition - viewport.position;
                currentEvent.Use();
                Repaint();
                return;
            }

            if (currentEvent.rawType == EventType.MouseUp && currentEvent.button == 0 && m_IsSelecting)
            {
                m_SelectionCurrent = currentEvent.mousePosition - viewport.position;
                ApplySelectionBox();
                m_IsSelecting = false;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
                Repaint();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && m_IsDraggingNodes &&
                GUIUtility.hotControl == m_DragControlId)
            {
                var localMouse = currentEvent.mousePosition - viewport.position;
                var canvasMouse = localMouse / m_CanvasZoom + m_CanvasScroll;
                foreach (var selectedIndex in m_SelectedIndices)
                {
                    if (!m_SelectedDragOffsets.TryGetValue(selectedIndex, out var offset)) continue;
                    m_Steps.GetArrayElementAtIndex(selectedIndex)
                        .FindPropertyRelative("m_Position").vector2Value = canvasMouse - offset;
                }
                currentEvent.Use();
                Repaint();
            }

            if (currentEvent.rawType == EventType.MouseUp && currentEvent.button == 0 && m_IsDraggingNodes)
            {
                EndDrag();
                currentEvent.Use();
            }
        }

        private int FindNodeIndex(Vector2 localMouse)
        {
            for (var i = m_Steps.arraySize - 1; i >= 0; i--)
            {
                var nodeRect = GetNodeRect(i);
                if (nodeRect.Contains(localMouse) || IsInputPort(nodeRect, localMouse) ||
                    IsOutputPort(i, localMouse))
                    return i;
            }

            return -1;
        }

        private void DrawSelectionRect()
        {
            if (!m_IsSelecting || Event.current.type != EventType.Repaint) return;

            var selection = GetSelectionRect();
            EditorGUI.DrawRect(selection, new Color(0.25f, 0.6f, 0.95f, 0.12f));
            EditorGUI.DrawRect(new Rect(selection.xMin, selection.yMin, selection.width, 1f),
                new Color(0.25f, 0.6f, 0.95f, 0.9f));
            EditorGUI.DrawRect(new Rect(selection.xMin, selection.yMax - 1f, selection.width, 1f),
                new Color(0.25f, 0.6f, 0.95f, 0.9f));
            EditorGUI.DrawRect(new Rect(selection.xMin, selection.yMin, 1f, selection.height),
                new Color(0.25f, 0.6f, 0.95f, 0.9f));
            EditorGUI.DrawRect(new Rect(selection.xMax - 1f, selection.yMin, 1f, selection.height),
                new Color(0.25f, 0.6f, 0.95f, 0.9f));
        }

        private Rect GetSelectionRect()
        {
            return Rect.MinMaxRect(
                Mathf.Min(m_SelectionStart.x, m_SelectionCurrent.x),
                Mathf.Min(m_SelectionStart.y, m_SelectionCurrent.y),
                Mathf.Max(m_SelectionStart.x, m_SelectionCurrent.x),
                Mathf.Max(m_SelectionStart.y, m_SelectionCurrent.y));
        }

        private void ApplySelectionBox()
        {
            var selection = GetSelectionRect();
            if (!m_SelectionAdditive)
                ClearSelection();

            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                if (selection.Overlaps(GetNodeRect(i), true))
                    m_SelectedIndices.Add(i);
            }

            m_SelectedIndex = m_SelectedIndices.Count == 0
                ? -1
                : m_SelectedIndices.Max();
        }

        private void SelectSingle(int index)
        {
            m_SelectedIndices.Clear();
            m_SelectedIndices.Add(index);
            m_SelectedIndex = index;
        }

        private void ToggleSelection(int index)
        {
            if (!m_SelectedIndices.Add(index))
            {
                m_SelectedIndices.Remove(index);
                m_SelectedIndex = m_SelectedIndices.Count == 0 ? -1 : m_SelectedIndices.First();
            }
            else
                m_SelectedIndex = index;
        }

        private void ClearSelection()
        {
            m_SelectedIndices.Clear();
            m_SelectedIndex = -1;
        }

        private bool IsOutputPort(int index, Vector2 localMouse)
        {
            return GetOutputBranch(index, localMouse) != OutputPort.None;
        }

        private OutputPort GetOutputBranch(int index, Vector2 localMouse)
        {
            var nodeRect = GetNodeRect(index);
            var condition = m_Steps.GetArrayElementAtIndex(index)
                .FindPropertyRelative("m_Condition").managedReferenceValue != null;
            var output = condition ? new Vector2(nodeRect.xMax, nodeRect.y + 36f * m_CanvasZoom) :
                new Vector2(nodeRect.xMax, nodeRect.center.y);
            var radius = k_PortRadius * 2f * m_CanvasZoom;
            if (!condition && Vector2.Distance(output, localMouse) <= radius) return OutputPort.Default;
            var truePort = new Vector2(nodeRect.xMax, nodeRect.y + 36f * m_CanvasZoom);
            var falsePort = new Vector2(nodeRect.xMax, nodeRect.y + 72f * m_CanvasZoom);
            if (condition && Vector2.Distance(truePort, localMouse) <= radius) return OutputPort.True;
            if (condition && Vector2.Distance(falsePort, localMouse) <= radius) return OutputPort.False;
            return OutputPort.None;
        }

        private static bool IsInputPort(Rect nodeRect, Vector2 localMouse)
        {
            return Vector2.Distance(new Vector2(nodeRect.xMin, nodeRect.center.y), localMouse)
                <= k_PortRadius * 2f;
        }

        private Rect GetNodeRect(int index)
        {
            var position = m_Steps.GetArrayElementAtIndex(index)
                .FindPropertyRelative("m_Position").vector2Value;
            return new Rect((position - m_CanvasScroll) * m_CanvasZoom,
                new Vector2(k_NodeWidth, k_NodeHeight) * m_CanvasZoom);
        }

        private void DrawGrid(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;

            var color = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.035f)
                : new Color(0f, 0f, 0f, 0.045f);
            var gridSize = k_GridSize * m_CanvasZoom;
            var offsetX = -m_CanvasScroll.x * m_CanvasZoom % gridSize;
            var offsetY = -m_CanvasScroll.y * m_CanvasZoom % gridSize;
            for (var x = offsetX; x < rect.width; x += gridSize)
                EditorGUI.DrawRect(new Rect(x, 0f, 1f, rect.height), color);
            for (var y = offsetY; y < rect.height; y += gridSize)
                EditorGUI.DrawRect(new Rect(0f, y, rect.width, 1f), color);
        }

        private void DrawConnections()
        {
            if (Event.current.type != EventType.Repaint) return;

            var positions = new Dictionary<string, Rect>(StringComparer.Ordinal);
            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                var id = element.FindPropertyRelative("m_Id").stringValue;
                positions[id] = GetNodeRect(i);
            }

            Handles.BeginGUI();
            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                var sourceId = element.FindPropertyRelative("m_Id").stringValue;
                if (!positions.TryGetValue(sourceId, out var source)) continue;

                var condition = element.FindPropertyRelative("m_Condition").managedReferenceValue;
                var trueId = element.FindPropertyRelative("m_TrueTargetStepId").stringValue;
                var falseId = element.FindPropertyRelative("m_FalseTargetStepId").stringValue;
                if (condition == null)
                {
                    if (positions.TryGetValue(trueId, out var target))
                        DrawConnection(source, target, m_SelectedIndices.Contains(i), OutputPort.Default);
                    continue;
                }
                if (positions.TryGetValue(trueId, out var trueTarget))
                    DrawConnection(source, trueTarget, m_SelectedIndices.Contains(i), OutputPort.True);
                if (positions.TryGetValue(falseId, out var falseTarget))
                    DrawConnection(source, falseTarget, m_SelectedIndices.Contains(i), OutputPort.False);
            }

            if (m_IsConnecting && positions.TryGetValue(m_ContextSourceId, out var connectionSource))
            {
                var start = GetConnectionStart(connectionSource, m_ContextBranch);
                var end = (Vector3)m_ConnectionMousePosition;
                var direction = end.x >= start.x ? 1f : -1f;
                var tangent = Mathf.Max(48f * m_CanvasZoom,
                    Mathf.Abs(end.x - start.x) * 0.45f);
                Handles.color = m_ContextBranch == OutputPort.True ? new Color(0.28f, 0.72f, 0.42f) :
                    m_ContextBranch == OutputPort.False ? new Color(0.85f, 0.36f, 0.34f) :
                    new Color(0.48f, 0.65f, 0.86f);
                Handles.DrawBezier(start, end,
                    start + Vector3.right * direction * tangent,
                    end - Vector3.right * direction * tangent,
                    Handles.color, null, 2f);
            }

            Handles.EndGUI();
        }

        private static void DrawConnection(Rect source, Rect target, bool selected, OutputPort branch)
        {
            var start = GetConnectionStart(source, branch);
            var end = new Vector3(target.xMin, target.center.y);
            var direction = end.x >= start.x ? 1f : -1f;
            var scale = source.height / k_NodeHeight;
            var tangent = Mathf.Max(48f * scale, Mathf.Abs(end.x - start.x) * 0.45f);
            var color = selected ? new Color(0.25f, 0.65f, 1f) : branch == OutputPort.True
                ? new Color(0.28f, 0.72f, 0.42f)
                : branch == OutputPort.False ? new Color(0.85f, 0.36f, 0.34f)
                : new Color(0.48f, 0.65f, 0.86f);
            Handles.color = color;
            Handles.DrawBezier(start, end,
                start + Vector3.right * direction * tangent,
                end - Vector3.right * direction * tangent,
                color, null, selected ? 3f : 2f);

            var arrowBase = end - Vector3.right * direction * 10f * scale;
            Handles.DrawAAConvexPolygon(end,
                arrowBase + Vector3.up * 5f * scale,
                arrowBase - Vector3.up * 5f * scale);
        }

        private static Vector3 GetConnectionStart(Rect source, OutputPort branch)
        {
            return new Vector3(source.xMax, source.y + (branch == OutputPort.True
                ? source.height * 36f / k_NodeHeight
                : branch == OutputPort.False
                    ? source.height * 72f / k_NodeHeight
                    : source.height * 0.5f));
        }

        private bool TryFindConnection(Vector2 point, out int sourceIndex, out OutputPort branch)
        {
            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                var source = GetNodeRect(i);
                var condition = element.FindPropertyRelative("m_Condition").managedReferenceValue;
                if (condition == null)
                {
                    if (TryFindConnectionTarget(point, source,
                            element.FindPropertyRelative("m_TrueTargetStepId").stringValue,
                            OutputPort.Default, i, out sourceIndex, out branch))
                        return true;
                    continue;
                }

                if (TryFindConnectionTarget(point, source,
                        element.FindPropertyRelative("m_TrueTargetStepId").stringValue,
                        OutputPort.True, i, out sourceIndex, out branch))
                    return true;
                if (TryFindConnectionTarget(point, source,
                        element.FindPropertyRelative("m_FalseTargetStepId").stringValue,
                        OutputPort.False, i, out sourceIndex, out branch))
                    return true;
            }

            sourceIndex = -1;
            branch = OutputPort.None;
            return false;

            bool TryFindConnectionTarget(Vector2 cursor, Rect sourceRect, string targetId,
                OutputPort output, int sourceIndex, out int source, out OutputPort port)
            {
                source = -1;
                port = OutputPort.None;
                if (string.IsNullOrEmpty(targetId)) return false;

                for (var targetIndex = 0; targetIndex < m_Steps.arraySize; targetIndex++)
                {
                    var target = m_Steps.GetArrayElementAtIndex(targetIndex);
                    if (target.FindPropertyRelative("m_Id").stringValue != targetId) continue;
                    var targetRect = GetNodeRect(targetIndex);
                    var start = GetConnectionStart(sourceRect, output);
                    var end = new Vector3(targetRect.xMin, targetRect.center.y);
                    var direction = end.x >= start.x ? 1f : -1f;
                    var scale = sourceRect.height / k_NodeHeight;
                    var tangent = Mathf.Max(48f * scale, Mathf.Abs(end.x - start.x) * 0.45f);
                    var previous = start;
                    for (var sample = 1; sample <= 20; sample++)
                    {
                        var t = sample / 20f;
                        var current = Game.VectorUtility.Bezier(start,
                            start + Vector3.right * direction * tangent,
                            end - Vector3.right * direction * tangent, end, t);
                        if (HandleUtility.DistancePointToLineSegment(cursor, previous, current)
                            <= Mathf.Max(6f, 8f * m_CanvasZoom))
                        {
                            source = sourceIndex;
                            port = output;
                            return true;
                        }
                        previous = current;
                    }
                }

                return false;
            }
        }

        private void DrawNodes()
        {
            UpdateNodeStyles();
            var runtimeIndex = 0;
            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                var rect = GetNodeRect(i);

                GUI.Box(rect, GUIContent.none, Styles.Node);
                if (m_SelectedIndices.Contains(i))
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f),
                        new Color(0.25f, 0.6f, 0.95f));

                var name = element.FindPropertyRelative("m_Name").stringValue;
                var handler = element.FindPropertyRelative("m_Handler").managedReferenceValue;
                var handlerName = handler == null ? "未设置处理器" : GetDisplayName(handler.GetType());
                var enabledProperty = element.FindPropertyRelative("m_Enabled");
                var editingLocked = IsFlowEditingLocked;
                EditorGUI.BeginDisabledGroup(editingLocked);
                EditorGUI.BeginChangeCheck();
                var enabled = EditorGUI.Toggle(new Rect(rect.x + 10f * m_CanvasZoom,
                    rect.y + 10f * m_CanvasZoom, 16f * m_CanvasZoom, 16f * m_CanvasZoom),
                    enabledProperty.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_Flow, "切换流程步骤");
                    enabledProperty.boolValue = enabled;
                    EditorUtility.SetDirty(m_Flow);
                }

                GUI.Label(new Rect(rect.x + 32f * m_CanvasZoom, rect.y + 9f * m_CanvasZoom,
                        rect.width - 44f * m_CanvasZoom, 22f * m_CanvasZoom),
                    $"{i + 1}. {name}", m_NodeTitleStyle);
                GUI.Label(new Rect(rect.x + 12f * m_CanvasZoom, rect.y + 36f * m_CanvasZoom,
                        rect.width - 24f * m_CanvasZoom, 18f * m_CanvasZoom),
                    handlerName, m_NodeMetaStyle);
                var conditionProperty = element.FindPropertyRelative("m_Condition");

                var selectedCondition = conditionProperty.managedReferenceValue == null ? 0 :
                    Array.IndexOf(m_ConditionTypes, conditionProperty.managedReferenceValue.GetType()) + 1;
                EditorGUI.BeginChangeCheck();
                var nextCondition = EditorGUI.Popup(
                    new Rect(rect.x + 12f * m_CanvasZoom, rect.y + 51f * m_CanvasZoom,
                        (k_NodeWidth - 64f) * m_CanvasZoom, 22f * m_CanvasZoom),
                    selectedCondition, m_ConditionOptions, m_NodePopupStyle);
                if (EditorGUI.EndChangeCheck())
                    SetCondition(i, nextCondition);
                EditorGUI.EndDisabledGroup();

                var runtimeState = GetRuntimeState(i, enabled ? runtimeIndex++ : -1);
                if (runtimeState.HasValue)
                {
                    var banner = GetRuntimeBannerRect(rect, m_CanvasZoom);
                    EditorGUI.DrawRect(banner, GetRuntimeStateColor(runtimeState.Value));
                    GUI.Label(banner, GetRuntimeStateLabel(runtimeState.Value), m_RuntimeBannerStyle);
                }

                Handles.BeginGUI();
                Handles.color = new Color(0.25f, 0.25f, 0.25f);
                Handles.DrawSolidDisc(new Vector3(rect.xMin, rect.center.y), Vector3.forward, k_PortRadius * m_CanvasZoom);
                if (conditionProperty.managedReferenceValue == null)
                {
                    Handles.color = new Color(0.48f, 0.65f, 0.86f);
                    Handles.DrawSolidDisc(new Vector3(rect.xMax, rect.center.y), Vector3.forward, k_PortRadius * m_CanvasZoom);
                }
                else
                {
                    Handles.color = new Color(0.28f, 0.72f, 0.42f);
                    Handles.DrawSolidDisc(new Vector3(rect.xMax, rect.y + 36f * m_CanvasZoom), Vector3.forward, k_PortRadius * m_CanvasZoom);
                    Handles.color = new Color(0.85f, 0.36f, 0.34f);
                    Handles.DrawSolidDisc(new Vector3(rect.xMax, rect.y + 72f * m_CanvasZoom), Vector3.forward, k_PortRadius * m_CanvasZoom);
                }
                Handles.EndGUI();
            }
        }

        private static Rect GetRuntimeBannerRect(Rect nodeRect, float zoom)
        {
            var height = Mathf.Min(k_RuntimeBannerHeight * zoom, nodeRect.height);
            return new Rect(nodeRect.x, nodeRect.yMax - height, nodeRect.width, height);
        }

        private GameFlowStepState? GetRuntimeState(int assetIndex, int runtimeIndex)
        {
            if (!EditorApplication.isPlaying || m_Flow == null)
                return null;

            var runtimeFlow = m_RuntimeFlow;
            if (runtimeFlow == null)
                return null;

            var definition = m_Steps.GetArrayElementAtIndex(assetIndex);
            if (!definition.FindPropertyRelative("m_Enabled").boolValue)
                return GameFlowStepState.NotExecuted;

            return runtimeIndex < runtimeFlow.StepStates.Count
                ? runtimeFlow.StepStates[runtimeIndex]
                : GameFlowStepState.NotExecuted;
        }

        private static string GetRuntimeStateLabel(GameFlowStepState state)
        {
            return state switch
            {
 GameFlowStepState.Current => "当前执行",
 GameFlowStepState.Executed => "已执行",
                _ => "未执行"
            };
        }

        private static Color GetRuntimeStateColor(GameFlowStepState state)
        {
            return state switch
            {
 GameFlowStepState.Current => new Color(0.95f, 0.65f, 0.15f),
 GameFlowStepState.Executed => new Color(0.3f, 0.75f, 0.4f),
                _ => new Color(0.55f, 0.55f, 0.55f)
            };
        }

        private void UpdateNodeStyles()
        {
            if (Mathf.Approximately(m_NodeStyleZoom, m_CanvasZoom)) return;

            m_NodeStyleZoom = m_CanvasZoom;
            m_NodeTitleStyle = CreateScaledStyle(Styles.NodeTitle, 12, m_CanvasZoom);
            m_NodeMetaStyle = CreateScaledStyle(Styles.NodeMeta, 10, m_CanvasZoom);
            m_NodePopupStyle = CreateScaledStyle(EditorStyles.popup, 12, m_CanvasZoom);
            m_RuntimeBannerStyle = CreateScaledStyle(Styles.NodeStatus, 10, m_CanvasZoom);
            m_RuntimeBannerStyle.alignment = TextAnchor.MiddleCenter;
            m_RuntimeBannerStyle.normal.textColor = Color.white;
        }

        private static GUIStyle CreateScaledStyle(GUIStyle source, int baseFontSize, float zoom)
        {
            var style = new GUIStyle(source)
            {
                fontSize = GetScaledFontSize(baseFontSize, zoom)
            };
            return style;
        }

        private static int GetScaledFontSize(int baseFontSize, float zoom)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseFontSize * zoom));
        }

        private void BindAsset()
        {
            m_SerializedObject?.Dispose();
            m_SerializedObject = m_Flow == null ? null : new SerializedObject(m_Flow);
            m_Steps = m_SerializedObject?.FindProperty("m_Steps");
        }

        internal void SetFlow(GameFlowAsset flow)
        {
            EndDrag();
            m_Flow = flow;
            m_RuntimeFlow = null;
            m_SelectedIndex = -1;
            m_IsConnecting = false;
            m_ContextSourceId = null;
            m_ContextBranch = OutputPort.None;
            ClearSelection();
            BindAsset();
            Repaint();
        }

        private void EndDrag()
        {
            if (m_IsDraggingNodes)
                Undo.CollapseUndoOperations(m_DragUndoGroup);
            if ((m_IsPanning && GUIUtility.hotControl == m_PanControlId) ||
                (m_IsDraggingNodes && GUIUtility.hotControl == m_DragControlId))
                GUIUtility.hotControl = 0;
            m_IsPanning = false;
            m_IsDraggingNodes = false;
            m_SelectedDragOffsets.Clear();
            if (m_IsSelecting && GUIUtility.hotControl == m_SelectionControlId)
                GUIUtility.hotControl = 0;
            m_IsSelecting = false;
        }

        private void OnLostFocus()
        {
            EndDrag();
            m_IsConnecting = false;
        }

        private void Update()
        {
            if (EditorApplication.isPlaying && m_RuntimeFlow != null)
                Repaint();
        }

        private void OnDisable()
        {
            GameFlowAsset.FlowCreated -= OnFlowCreated;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EndDrag();
            m_SerializedObject?.Dispose();
            m_SerializedObject = null;
            m_Steps = null;
            m_RuntimeFlow = null;
            ClearSelection();
        }

        private void OnFlowCreated(GameFlowAsset asset, GameFlow flow)
        {
            if (asset != m_Flow) return;
            m_RuntimeFlow = flow;
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode &&
                state != PlayModeStateChange.EnteredEditMode) return;
            m_RuntimeFlow = null;
            Repaint();
        }

        private void AutoLayout()
        {
            if (IsFlowEditingLocked) return;
            m_SerializedObject.Update();
            var count = m_Steps.arraySize;
            if (count == 0)
            {
                m_SerializedObject.ApplyModifiedProperties();
                return;
            }

            var indices = new Dictionary<string, int>(count, StringComparer.Ordinal);
            var outgoing = new List<int>[count];
            var incoming = new int[count];
            var levels = new int[count];
            for (var i = 0; i < count; i++)
            {
                indices[m_Steps.GetArrayElementAtIndex(i).FindPropertyRelative("m_Id").stringValue] = i;
                outgoing[i] = new List<int>(2);
                levels[i] = -1;
            }

            for (var i = 0; i < count; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                var trueTarget = element.FindPropertyRelative("m_TrueTargetStepId").stringValue;
                var falseTarget = element.FindPropertyRelative("m_FalseTargetStepId").stringValue;
                AddLayoutEdge(trueTarget, i, indices, outgoing, incoming);
                if (falseTarget != trueTarget)
                    AddLayoutEdge(falseTarget, i, indices, outgoing, incoming);
            }

            var queue = new Queue<int>();
            for (var i = 0; i < count; i++)
            {
                if (incoming[i] != 0) continue;
                levels[i] = 0;
                queue.Enqueue(i);
            }

            while (queue.Count > 0)
            {
                var source = queue.Dequeue();
                foreach (var target in outgoing[source])
                {
                    levels[target] = Mathf.Max(levels[target], levels[source] + 1);
                    if (--incoming[target] == 0)
                        queue.Enqueue(target);
                }
            }

            var maxLevel = 0;
            for (var i = 0; i < count; i++)
                maxLevel = Mathf.Max(maxLevel, levels[i]);
            var fallbackLevel = maxLevel + 1;
            for (var i = 0; i < count; i++)
            {
                if (levels[i] >= 0) continue;
                levels[i] = fallbackLevel;
            }

            var columns = new Dictionary<int, List<int>>();
            for (var i = 0; i < count; i++)
            {
                if (!columns.TryGetValue(levels[i], out var column))
                {
                    column = new List<int>();
                    columns.Add(levels[i], column);
                }
                column.Add(i);
            }

            Undo.RecordObject(m_Flow, "整理流程节点");
            var origin = Vector2.one * (k_GridSize * 2f);
            var spacing = new Vector2(k_NodeWidth + k_GridSize * 3f,
                k_NodeHeight + k_GridSize * 2f);
            foreach (var column in columns)
            {
                for (var row = 0; row < column.Value.Count; row++)
                {
                    var position = m_Steps.GetArrayElementAtIndex(column.Value[row])
                        .FindPropertyRelative("m_Position");
                    position.vector2Value = origin + new Vector2(column.Key * spacing.x,
                        row * spacing.y);
                }
            }

            m_SerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Flow);
            Repaint();
        }

        private static void AddLayoutEdge(string targetId, int source, Dictionary<string, int> indices,
            List<int>[] outgoing, int[] incoming)
        {
            if (string.IsNullOrEmpty(targetId) || !indices.TryGetValue(targetId, out var target)) return;
            outgoing[source].Add(target);
            incoming[target]++;
        }

        private void RemoveStep(int index)
        {
            if (IsFlowEditingLocked) return;
            if ((uint)index >= (uint)m_Steps.arraySize) return;

            var removedId = m_Steps.GetArrayElementAtIndex(index).FindPropertyRelative("m_Id").stringValue;
            if (removedId == m_ContextSourceId) m_IsConnecting = false;
            Undo.RecordObject(m_Flow, "删除流程节点");
            m_Steps.DeleteArrayElementAtIndex(index);
            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                ClearTarget(element.FindPropertyRelative("m_TrueTargetStepId"), removedId);
                ClearTarget(element.FindPropertyRelative("m_FalseTargetStepId"), removedId);
            }

            m_SelectedIndex = Mathf.Min(index, m_Steps.arraySize - 1);
            if (m_SelectedIndex >= 0)
                SelectSingle(m_SelectedIndex);
            else
                ClearSelection();
            m_SerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Flow);
            Repaint();
        }

        private void RemoveSelectedSteps()
        {
            if (IsFlowEditingLocked) return;
            if (m_SelectedIndices.Count == 0) return;

            var indices = m_SelectedIndices
                .Where(index => (uint)index < (uint)m_Steps.arraySize)
                .OrderByDescending(index => index)
                .ToArray();
            if (indices.Length == 0)
            {
                ClearSelection();
                return;
            }

            var removedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var index in indices)
                removedIds.Add(m_Steps.GetArrayElementAtIndex(index).FindPropertyRelative("m_Id").stringValue);

            Undo.RecordObject(m_Flow, "删除流程步骤");
            foreach (var index in indices)
                m_Steps.DeleteArrayElementAtIndex(index);

            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                ClearTarget(element.FindPropertyRelative("m_TrueTargetStepId"), removedIds);
                ClearTarget(element.FindPropertyRelative("m_FalseTargetStepId"), removedIds);
            }

            m_SerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Flow);
            m_IsConnecting = false;
            m_ContextSourceId = null;
            m_ContextBranch = OutputPort.None;
            ClearSelection();
            Repaint();
        }

        private void ClearTarget(SerializedProperty target, string id)
        {
            if (target.stringValue == id) target.stringValue = string.Empty;
        }

        private static void ClearTarget(SerializedProperty target, ISet<string> ids)
        {
            if (ids.Contains(target.stringValue))
                target.stringValue = string.Empty;
        }

        private void SetBranchTarget(string targetId)
        {
            if (IsFlowEditingLocked) return;
            m_SerializedObject.Update();
            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("m_Id").stringValue != m_ContextSourceId) continue;
                Undo.RecordObject(m_Flow, "连接流程步骤");
                var propertyName = m_ContextBranch == OutputPort.False ? "m_FalseTargetStepId" : "m_TrueTargetStepId";
                element.FindPropertyRelative(propertyName).stringValue = targetId ?? string.Empty;
                break;
            }

            m_SerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Flow);
            Repaint();
        }

        private void ShowCanvasContextMenu(Vector2 screenPosition, Vector2 canvasPosition)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("自动布局"), false, AutoLayout);
            menu.AddSeparator(string.Empty);
            AddCreateStepItems(menu, canvasPosition);
            menu.DropDown(new Rect(screenPosition, Vector2.zero));
        }

        private void ShowBranchTargetMenu(Vector2 screenPosition)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("断开连接"), false, () => SetBranchTarget(null));
            menu.AddSeparator(string.Empty);
            for (var i = 0; i < m_Steps.arraySize; i++)
            {
                var element = m_Steps.GetArrayElementAtIndex(i);
                var id = element.FindPropertyRelative("m_Id").stringValue;
                if (id == m_ContextSourceId) continue;
                var title = $"连接到/{i + 1}. {element.FindPropertyRelative("m_Name").stringValue}";
                menu.AddItem(new GUIContent(title), false, () => SetBranchTarget(id));
            }
            menu.DropDown(new Rect(screenPosition, Vector2.zero));
        }

        private void AddCreateStepItems(GenericMenu menu, Vector2 canvasPosition)
        {
            if (m_HandlerTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("创建步骤/没有可用处理器"));
                return;
            }

            foreach (var handlerType in m_HandlerTypes)
            {
                var capturedType = handlerType;
                menu.AddItem(new GUIContent($"创建步骤/{GetDisplayName(capturedType)}"), false,
                    () => AddStep(capturedType, canvasPosition));
            }
        }

        private void ShowNodeContextMenu(Vector2 screenPosition, int index)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("删除步骤"), false, () => RemoveStep(index));
            menu.DropDown(new Rect(screenPosition, Vector2.zero));
        }

        private void AddStep(Type handlerType, Vector2 position)
        {
            if (IsFlowEditingLocked) return;
            m_SerializedObject.Update();
            Undo.RecordObject(m_Flow, "创建流程节点");
            var index = m_Steps.arraySize;
            m_Steps.InsertArrayElementAtIndex(index);
            var element = m_Steps.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("m_Id").stringValue = Guid.NewGuid().ToString("N");
            element.FindPropertyRelative("m_Name").stringValue = GetDisplayName(handlerType);
            element.FindPropertyRelative("m_Enabled").boolValue = true;
            element.FindPropertyRelative("m_Position").vector2Value = position;
            element.FindPropertyRelative("m_Condition").managedReferenceValue = null;
            element.FindPropertyRelative("m_TrueTargetStepId").stringValue = string.Empty;
            element.FindPropertyRelative("m_FalseTargetStepId").stringValue = string.Empty;
            element.FindPropertyRelative("m_Handler").managedReferenceValue = Activator.CreateInstance(handlerType, true);

            m_SerializedObject.ApplyModifiedProperties();
            SelectSingle(index);
            EditorUtility.SetDirty(m_Flow);
            Repaint();
        }

        private static bool IsSelectableType(Type type)
        {
            return type != null && !type.IsAbstract && !type.IsGenericType && type.IsSerializable &&
                   type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                       null, Type.EmptyTypes, null) != null;
        }

        private static string GetDisplayName(Type type)
        {
            var metadata = type.GetCustomAttribute<GameFlowDisplayNameAttribute>();
            return string.IsNullOrWhiteSpace(metadata?.DisplayName)
                ? ObjectNames.NicifyVariableName(type.Name)
                : metadata.DisplayName;
        }
    }
}

#endif