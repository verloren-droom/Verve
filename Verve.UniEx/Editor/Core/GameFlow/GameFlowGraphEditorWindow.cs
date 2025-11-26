#if UNITY_EDITOR

namespace VerveEditor
{
    using Verve;
    using System;
    using Verve.UniEx;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;
    using Object = UnityEngine.Object;
    
    
    /// <summary>
    ///   <para>可视化流程节点图编辑窗口</para>
    /// </summary>
    internal class GameFlowGraphEditorWindow : EditorWindow
    {
        [SerializeField, Tooltip("当前流程图")] private GameFlowGraphAsset m_CurrentGraph;
        [NonSerialized, Tooltip("当前流程图输入端口")] private VisualFlowPort m_SelectedInputPort;
        [NonSerialized, Tooltip("当前流程图输出端口")] private VisualFlowPort m_SelectedOutputPort;

        /// <summary>
        ///   <para>当前选中的输入端口</para>
        /// </summary>
        private GameFlowNodeWrapper m_SelectedNode;
        /// <summary>
        ///   <para>当前选中的端口</para>
        /// </summary>
        private VisualFlowPort m_SelectedPort;
        private bool m_IsDragging;
        private Rect m_GraphViewRect;
        private bool m_IsConnecting;
        private Vector2 m_ConnectionStartPos;
        private VisualFlowPort m_FirstSelectedPort;
        private VisualFlowPort m_SecondSelectedPort;
        private bool m_WaitingForSecondClick;
        private VisualFlowConnection m_SelectedConnection;
        
        private static Dictionary<string, Type> s_NodeTypeCache;
        
        private bool m_IsInspectorDragging;
        private bool m_IsInspectorResizing;
        private Vector2 m_InspectorDragStart;
        private bool m_IsDirty;

        /// <summary>
        ///   <para>标题ICON路径</para>
        /// </summary>
        private const string kTitleIcon = "Packages/com.benfach.verve.uniex/Editor/Core/GameFlow/Icons/GameFlowGraphIcon_64x64.png";
        /// <summary>
        ///   <para>默认标题</para>
        /// </summary>
        private const string kDefaultTitle = "Flow Graph";
        /// <summary>
        ///   <para>连接线宽度</para>
        /// </summary>
        private const float kConnectionLineWidth = 4f;
        /// <summary>
        ///   <para>检查器缩放句柄大小</para>
        /// </summary>
        private const float kInspectorResizeHandleSize = 15f;
        /// <summary>
        ///   <para>检查器标题高度</para>
        /// </summary>
        private const float kInspectorHeaderHeight = 25f;
        /// <summary>
        ///   <para>最小缩放比例</para>
        /// </summary>
        private const float kMinZoom = 0.5f;
        /// <summary>
        ///   <para>最大缩放比例</para>
        /// </summary>
        private const float kMaxZoom = 1.5f;
        /// <summary>
        ///   <para>缩放步进</para>
        /// </summary>
        private const float kZoomStep = 0.1f;
        
        private static Dictionary<string, GameFlowGraphEditorWindow> s_OpenWindows = new Dictionary<string, GameFlowGraphEditorWindow>();
        
        /// <summary>
        ///   <para>内部样式</para>
        /// </summary>
        private static class Styles
        {
            private static readonly List<Texture2D> s_CreatedTextures = new List<Texture2D>();

            /// <summary>
            ///   <para>节点样式</para>
            /// </summary>
            public static GUIStyle NodeStyle { get; private set; }
            /// <summary>
            ///   <para>已选节点样式</para>
            /// </summary>
            public static GUIStyle SelectedNodeStyle { get; private set; }
            /// <summary>
            ///   <para>执行中节点样式</para>
            /// </summary>
            public static GUIStyle ExecutingNodeStyle { get; private set; }
            /// <summary>
            ///   <para>已完成节点样式</para>
            /// </summary>
            public static GUIStyle CompletedNodeStyle { get; private set; }
            /// <summary>
            ///   <para>节点标题样式</para>
            /// </summary>
            public static GUIStyle NodeTitleStyle { get; private set; }
            /// <summary>
            ///   <para>端口样式</para>
            /// </summary>
            public static GUIStyle PortStyle { get; private set; }
            /// <summary>
            ///   <para>端口标签样式</para>
            /// </summary>
            public static GUIStyle PortLabelStyle { get; private set; }
            /// <summary>
            ///   <para>高亮端口样式</para>
            /// </summary>
            public static GUIStyle HighlightPort { get; private set; }
            /// <summary>
            ///   <para>根节点样式</para>
            /// </summary>
            public static GUIStyle RootNodeStyle { get; private set; }

            /// <summary>
            ///   <para>流程图检查器样式</para>
            /// </summary>
            public static GUIStyle GraphInspector { get; private set; }
            /// <summary>
            ///   <para>流程图检查器标题样式</para>
            /// </summary>
            public static GUIStyle GraphInspectorHeader { get; private set; }
            /// <summary>
            ///   <para>流程图检查器标题文本样式</para>
            /// </summary>
            public static GUIStyle GraphInspectorTitle { get; private set; }
            /// <summary>
            ///   <para>流程图检查器拖拽句柄样式</para>
            /// </summary>
            public static GUIStyle GraphInspectorDragHandle { get; private set; }
            
            static Styles()
            {
                NodeStyle = new GUIStyle();
                NodeStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.95f));
                NodeStyle.border = new RectOffset(12, 12, 12, 12);
                NodeStyle.padding = new RectOffset(10, 10, 10, 10);
                
                SelectedNodeStyle = new GUIStyle();
                SelectedNodeStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.4f, 0.8f, 0.95f));
                SelectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
                SelectedNodeStyle.padding = new RectOffset(10, 10, 10, 10);
                
                ExecutingNodeStyle = new GUIStyle();
                ExecutingNodeStyle.normal.background = CreateColorTexture(new Color(0.9f, 0.9f, 0.2f, 0.5f));
                ExecutingNodeStyle.border = new RectOffset(12, 12, 12, 12);
                ExecutingNodeStyle.padding = new RectOffset(10, 10, 10, 10);
                
                CompletedNodeStyle = new GUIStyle();
                CompletedNodeStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.8f, 0.4f, 0.5f));
                CompletedNodeStyle.border = new RectOffset(12, 12, 12, 12);
                CompletedNodeStyle.padding = new RectOffset(10, 10, 10, 10);
                
                NodeTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                NodeTitleStyle.alignment = TextAnchor.MiddleCenter;
                NodeTitleStyle.normal.textColor = Color.white;
                
                PortStyle = new GUIStyle();
                PortStyle.normal.background = CreateColorTexture(Color.gray);
                PortStyle.active.background = CreateColorTexture(Color.white);
                PortStyle.border = new RectOffset(4, 4, 4, 4);
                PortStyle.fixedWidth = 12f;
                PortStyle.fixedHeight = 12f;
                PortStyle.alignment = TextAnchor.MiddleCenter;
                
                PortLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                PortLabelStyle.normal.textColor = Color.white;
                
                HighlightPort = new GUIStyle();
                HighlightPort.normal.background = CreateColorTexture(Color.yellow);
                HighlightPort.active.background = CreateColorTexture(Color.white);
                HighlightPort.border = new RectOffset(4, 4, 4, 4);
                HighlightPort.fixedWidth = 12f;
                HighlightPort.fixedHeight = 12f;
                HighlightPort.alignment = TextAnchor.MiddleCenter;
                
                RootNodeStyle = new GUIStyle(NodeStyle);
                
                GraphInspector = new GUIStyle();
                GraphInspector.normal.background = CreateColorTexture(new Color(0.22f, 0.22f, 0.22f, 0.95f));
                GraphInspector.border = new RectOffset(1, 1, 1, 1);
                GraphInspector.padding = new RectOffset(5, 5, 0, 5);
                
                GraphInspectorHeader = new GUIStyle(EditorStyles.toolbar);
                GraphInspectorHeader.normal.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 1.0f));
                GraphInspectorHeader.fixedHeight = kInspectorHeaderHeight;
                
                GraphInspectorTitle = new GUIStyle(EditorStyles.boldLabel);
                GraphInspectorTitle.normal.textColor = Color.white;
                GraphInspectorTitle.alignment = TextAnchor.MiddleLeft;
                GraphInspectorTitle.fixedHeight = kInspectorHeaderHeight;
                GraphInspectorTitle.alignment = TextAnchor.MiddleLeft;

                GraphInspectorDragHandle = new GUIStyle();
                GraphInspectorDragHandle.normal.background = CreateColorTexture(new Color(0.4f, 0.4f, 0.4f, 0.8f));
            }
            
            /// <summary>
            ///   <para>创建颜色纹理</para>
            /// </summary>
            private static Texture2D CreateColorTexture(Color color, int width = 1, int height = 1)
            {
                var texture = new Texture2D(width, height);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.SetPixel(0, 0, color);
                texture.Apply();
                s_CreatedTextures.Add(texture);
                return texture;
            }
            
            /// <summary>
            ///   <para>清理纹理</para>
            /// </summary>
            public static void Cleanup()
            {
                foreach (var texture in s_CreatedTextures)
                {
                    if (texture != null)
                        DestroyImmediate(texture);
                }
                s_CreatedTextures.Clear();
            }
        }

        /// <summary>
        ///   <para>创建或打开已有的流程图编辑窗口</para>
        /// </summary>
        public static void CreateNewOrOpenWindow(GameFlowGraphAsset graphAsset)
        {
            if (graphAsset == null) return;
            
            string assetPath = AssetDatabase.GetAssetPath(graphAsset);
            
            if (s_OpenWindows.ContainsKey(assetPath))
            {
                var existingWindow = s_OpenWindows[assetPath];
                if (existingWindow != null)
                {
                    existingWindow.Focus();
                    return;
                }
                else
                {
                    s_OpenWindows.Remove(assetPath);
                }
            }
            
            var window = CreateWindow<GameFlowGraphEditorWindow>();
            window.minSize = new Vector2(800, 600);
            window.m_CurrentGraph = graphAsset;
            
            window.m_CurrentGraph.inspectorRect = new Rect(
                window.position.width - 310,
                30, 
                300, 
                400
            );
            
            s_OpenWindows[assetPath] = window;
            
            window.UpdateWindowTitle();
            window.Show();
            window.Focus();
        }
        
        private void OnEnable()
        {
            RefreshNodeTypeCache();
            
            if (m_CurrentGraph != null)
            {
                m_CurrentGraph.RebuildGraphReferences();
                string assetPath = AssetDatabase.GetAssetPath(m_CurrentGraph);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    s_OpenWindows[assetPath] = this;
                }
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawGraphView();
            DrawGraphInspector();
            HandleEvents();
            
            if (m_CurrentGraph != null && GUI.changed)
            {
                m_CurrentGraph.RebuildGraphReferences();
            }
            
            if (GUI.changed) 
            {
                if (!m_IsDirty)
                {
                    m_IsDirty = true;
                    UpdateWindowTitle();
                }
                Repaint();
            }
        }

        /// <summary>
        ///   <para>更新窗口标题</para>
        /// </summary>
        private void UpdateWindowTitle()
        {
            string graphName = m_CurrentGraph?.name ?? kDefaultTitle;
            titleContent = new GUIContent($"{graphName} (Experimental)", 
                AssetDatabase.LoadAssetAtPath<Texture2D>(kTitleIcon));
        }

        /// <summary>
        ///   <para>刷新节点类型缓存</para>
        /// </summary>
        private void RefreshNodeTypeCache()
        {
            s_NodeTypeCache = new Dictionary<string, Type>();
            
            try
            {
                var types = TypeCache.GetTypesDerivedFrom<GameFlowNode>().Where(t => typeof(GameFlowNode).IsAssignableFrom(t) && !t.IsAbstract)
                    .Where(t => t.GetCustomAttribute<GameFlowNodeAttribute>() != null);
                
                foreach (var type in types)
                {
                    var attribute = type.GetCustomAttribute<GameFlowNodeAttribute>();
                    var menuPath = attribute.MenuPath ?? "Custom/" + type.Name;
                    s_NodeTypeCache[menuPath] = type;
                }
            }
            catch (ReflectionTypeLoadException) { }
        }
        
        /// <summary>
        ///   <para>绘制复合节点的子节点控制按钮</para>
        /// </summary>
        private void DrawCompositeNodeControls(GameFlowNodeWrapper node, Rect nodeRect)
        {
            float buttonAreaHeight = 40f;
            float buttonAreaY = nodeRect.y + nodeRect.height - buttonAreaHeight;
    
            var buttonAreaRect = new Rect(nodeRect.x, buttonAreaY, nodeRect.width, buttonAreaHeight);
            EditorGUI.DrawRect(buttonAreaRect, new Color(0.25f, 0.25f, 0.25f, 0.8f));
    
            float buttonSpacing = 5f;
            float buttonWidth = 20f;
            float buttonHeight = 16f;
            float buttonsTotalWidth = buttonWidth * 2 + buttonSpacing;
            float buttonX = nodeRect.x + nodeRect.width - buttonsTotalWidth - 5f;
            float buttonY = buttonAreaY + (buttonAreaHeight - buttonHeight) / 2f;
    
            var addButtonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);
            var removeButtonRect = new Rect(buttonX + buttonWidth + buttonSpacing, buttonY, buttonWidth, buttonHeight);
    
            var miniButtonStyle = new GUIStyle(EditorStyles.miniButton);
            miniButtonStyle.fontSize = 10;
            miniButtonStyle.padding = new RectOffset(2, 2, 0, 0);
    
            if (GUI.Button(addButtonRect, "+", miniButtonStyle))
            {
                node.AddChildToComposite();
                SaveGraphChanges();
                if (m_CurrentGraph != null)
                {
                    m_CurrentGraph.RebuildGraphReferences();
                }
                GUI.changed = true;
            }
    
            if (GUI.Button(removeButtonRect, "-", miniButtonStyle))
            {
                node.RemoveChildFromComposite();
                SaveGraphChanges();
                if (m_CurrentGraph != null)
                {
                    m_CurrentGraph.RebuildGraphReferences();
                }
                GUI.changed = true;
            }
        }

        /// <summary>
        ///   <para>获取端口颜色</para>
        /// </summary>
        private Color GetPortColor(VisualFlowPort port)
        {
            switch (port.PortType)
            {
                case GameFlowNodeWrapper.PortType.In:
                    return Color.green;
                case GameFlowNodeWrapper.PortType.Child:
                    return Color.yellow;
                case GameFlowNodeWrapper.PortType.CustomInput:
                case GameFlowNodeWrapper.PortType.CustomOutput:
                default:
                    return Color.white;
            }
        }

        /// <summary>
        ///   <para>绘制图形检查器</para>
        /// </summary>
        private void DrawGraphInspector()
        {
            if (!m_CurrentGraph.inspectorVisible) return;

            m_CurrentGraph.inspectorRect.x = Mathf.Clamp(m_CurrentGraph.inspectorRect.x, 0, position.width - m_CurrentGraph.inspectorRect.width);
            m_CurrentGraph.inspectorRect.y = Mathf.Clamp(m_CurrentGraph.inspectorRect.y, 20, position.height - 40);
            m_CurrentGraph.inspectorRect.width = Mathf.Clamp(m_CurrentGraph.inspectorRect.width, 250, 500);
            m_CurrentGraph.inspectorRect.height = Mathf.Clamp(m_CurrentGraph.inspectorRect.height, 200, position.height - 60);

            GUI.Box(m_CurrentGraph.inspectorRect, "", Styles.GraphInspector);

            var headerRect = new Rect(m_CurrentGraph.inspectorRect.x, m_CurrentGraph.inspectorRect.y, m_CurrentGraph.inspectorRect.width, kInspectorHeaderHeight);
            GUI.Box(headerRect, "", Styles.GraphInspectorHeader);

            var dragHandleRect = new Rect(headerRect.x, headerRect.y, headerRect.width, headerRect.height);
            GUI.Box(dragHandleRect, "", Styles.GraphInspectorDragHandle);

            var titleContent = new GUIContent("Graph Inspector");
            var titleRect = new Rect(
                headerRect.x + 5,
                headerRect.y,
                headerRect.width - 10,
                headerRect.height
            );
            GUI.Label(titleRect, titleContent, Styles.GraphInspectorTitle);

            var contentRect = new Rect(
                m_CurrentGraph.inspectorRect.x + 5, 
                m_CurrentGraph.inspectorRect.y + kInspectorHeaderHeight + 5, 
                m_CurrentGraph.inspectorRect.width - 10, 
                m_CurrentGraph.inspectorRect.height - kInspectorHeaderHeight - 10
            );
            
            GUILayout.BeginArea(contentRect);
            {
                m_CurrentGraph.inspectorScrollPos = EditorGUILayout.BeginScrollView(m_CurrentGraph.inspectorScrollPos, GUILayout.Height(contentRect.height));
                {
                    DrawInspectorContent();
                }
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndArea();

            DrawResizeHandle();

            HandleInspectorInteraction(headerRect);
        }

        /// <summary>
        ///   <para>绘制调整大小的手柄</para>
        /// </summary>
        private void DrawResizeHandle()
        {
            var resizeHandleRect = new Rect(
                m_CurrentGraph.inspectorRect.x + m_CurrentGraph.inspectorRect.width - kInspectorResizeHandleSize,
                m_CurrentGraph.inspectorRect.y + m_CurrentGraph.inspectorRect.height - kInspectorResizeHandleSize,
                kInspectorResizeHandleSize,
                kInspectorResizeHandleSize
            );
            
            EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeUpLeft);
            GUI.Box(resizeHandleRect, "", Styles.GraphInspectorDragHandle);
            
            Handles.BeginGUI();
            Handles.color = Color.white;
            Vector2 start = new Vector2(resizeHandleRect.x + 5, resizeHandleRect.y + resizeHandleRect.height - 5);
            Vector2 end = new Vector2(resizeHandleRect.x + resizeHandleRect.width - 5, resizeHandleRect.y + 5);
            Handles.DrawLine(start, end);
            Handles.EndGUI();
        }

        /// <summary>
        ///   <para>处理检查器交互（拖拽和调整大小）</para>
        /// </summary>
        private void HandleInspectorInteraction(Rect headerRect)
        {
            Event currentEvent = Event.current;
            Vector2 mousePos = currentEvent.mousePosition;

            var resizeHandleRect = new Rect(
                m_CurrentGraph.inspectorRect.x + m_CurrentGraph.inspectorRect.width - kInspectorResizeHandleSize,
                m_CurrentGraph.inspectorRect.y + m_CurrentGraph.inspectorRect.height - kInspectorResizeHandleSize,
                kInspectorResizeHandleSize,
                kInspectorResizeHandleSize
            );

            if (currentEvent.type == EventType.MouseDown)
            {
                if (headerRect.Contains(mousePos))
                {
                    m_IsInspectorDragging = true;
                    m_InspectorDragStart = mousePos - new Vector2(m_CurrentGraph.inspectorRect.x, m_CurrentGraph.inspectorRect.y);
                    currentEvent.Use();
                }
                else if (resizeHandleRect.Contains(mousePos))
                {
                    m_IsInspectorResizing = true;
                    m_InspectorDragStart = mousePos;
                    currentEvent.Use();
                }
            }

            if (m_IsInspectorDragging && currentEvent.type == EventType.MouseDrag)
            {
                m_CurrentGraph.inspectorRect.position = mousePos - m_InspectorDragStart;
                currentEvent.Use();
            }

            if (m_IsInspectorResizing && currentEvent.type == EventType.MouseDrag)
            {
                Vector2 delta = mousePos - m_InspectorDragStart;
                m_CurrentGraph.inspectorRect.width = Mathf.Max(250, m_CurrentGraph.inspectorRect.width + delta.x);
                m_CurrentGraph.inspectorRect.height = Mathf.Max(200, m_CurrentGraph.inspectorRect.height + delta.y);
                m_InspectorDragStart = mousePos;
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                m_IsInspectorDragging = false;
                m_IsInspectorResizing = false;
            }
        }

        /// <summary>
        ///   <para>绘制检查器内容</para>
        /// </summary>
        private void DrawInspectorContent()
        {
            if (m_SelectedPort != null)
            {
                DrawPortSettings();
            }
            else if (m_SelectedNode != null)
            {
                DrawNodeSettings();
            }
            else if (m_SelectedConnection != null)
            {
                DrawConnectionSettings();
            }
            else
            {
                DrawGraphSettings();
            }
        }

        /// <summary>
        ///   <para>绘制连接线设置</para>
        /// </summary>
        private void DrawConnectionSettings()
        {
            GUILayout.Label("Connection Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_SelectedConnection != null)
            {
                EditorGUILayout.LabelField("From Node", m_SelectedConnection.OutputPort?.Node?.NodeName ?? "Unknown");
                EditorGUILayout.LabelField("From Port", m_SelectedConnection.OutputPort?.DisplayName ?? "Unknown");
                EditorGUILayout.LabelField("To Node", m_SelectedConnection.InputPort?.Node?.NodeName ?? "Unknown");
                EditorGUILayout.LabelField("To Port", m_SelectedConnection.InputPort?.DisplayName ?? "Unknown");
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Delete Connection"))
                {
                    DeleteConnection(m_SelectedConnection);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No connection selected", MessageType.Info);
            }
        }
        
        /// <summary>
        ///   <para>绘制端口属性设置面板</para>
        /// </summary>
        private void DrawPortSettings()
        {
            GUILayout.Label("Port Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_SelectedPort != null)
            {
                EditorGUILayout.LabelField("Port ID", m_SelectedPort.PortID);
                EditorGUILayout.LabelField("Port Name", m_SelectedPort.DisplayName);
                EditorGUILayout.LabelField("Direction", m_SelectedPort.PortDirection.ToString());
                EditorGUILayout.LabelField("Node", m_SelectedPort.Node?.NodeName ?? "None");
            }
            else
            {
                EditorGUILayout.HelpBox("No port selected", MessageType.Info);
            }
        }
        
        /// <summary>
        ///   <para>绘制流程图节点属性设置</para>
        /// </summary>
        private void DrawNodeSettings()
        {
            GUILayout.Label("Node Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_SelectedNode != null && m_SelectedNode.WrappedNode != null)
            {
                using var serializedNode = new SerializedObject(m_SelectedNode);
                using var nodeChange = new EditorGUI.ChangeCheckScope();
                serializedNode.Update();
                
                var nodeNameProp = serializedNode.FindProperty("m_NodeName");
                var descriptionProp = serializedNode.FindProperty("m_Description");
                var tooltipProp = serializedNode.FindProperty("m_Tooltip");
                EditorGUILayout.PropertyField(nodeNameProp, true);
                EditorGUILayout.PropertyField(descriptionProp, true);
                EditorGUILayout.PropertyField(tooltipProp, true);
                
                EditorGUILayout.Space();

                var wrappedNodeProp = serializedNode.FindProperty("m_WrappedNode");
                var childProperty = wrappedNodeProp.Copy();
                var enterChildren = true;
                while (childProperty.NextVisible(enterChildren))
                {
                    if (!childProperty.propertyPath.StartsWith(wrappedNodeProp.propertyPath)) 
                        break;
                    
                    var fieldInfo = GetFieldInfoFromProperty(m_SelectedNode.WrappedNode.GetType(), childProperty.name);
                    if (fieldInfo != null && (fieldInfo.GetCustomAttribute<GameFlowOutputAttribute>() != null || fieldInfo.GetCustomAttribute<GameFlowInputAttribute>() != null))
                    {
                        enterChildren = false;
                        continue;
                    }
                    
                    EditorGUILayout.PropertyField(childProperty, true);
                    enterChildren = false;
                }
                
                if (nodeChange.changed)
                {
                    serializedNode.ApplyModifiedProperties();
                    MarkDirty();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No node selected", MessageType.Info);
            }
        }
        
        /// <summary>
        ///   <para>根据属性名称获取字段信息</para>
        /// </summary>
        private FieldInfo GetFieldInfoFromProperty(Type type, string propertyName)
        {
            if (type == null || string.IsNullOrEmpty(propertyName))
                return null;
                
            var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return field;
                
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.Name.StartsWith("<" + propertyName + ">"))
                    return f;
            }
            
            return null;
        }
        
        /// <summary>
        ///   <para>绘制流程图绘制工具栏面板</para>
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.FlexibleSpace();
                
                GUILayout.Label("Zoom:", GUILayout.ExpandWidth(false));
                m_CurrentGraph.zoom = GUILayout.HorizontalSlider(m_CurrentGraph.zoom, kMinZoom, kMaxZoom, GUILayout.Width(100));
                GUILayout.Label($"{m_CurrentGraph.zoom * 100:F0}%", GUILayout.Width(40));
                
                GUILayout.Space(10);

                if (GUILayout.Button(m_CurrentGraph.inspectorVisible ? "Hide Inspector" : "Show Inspector", EditorStyles.toolbarButton))
                {
                    m_CurrentGraph.inspectorVisible = !m_CurrentGraph.inspectorVisible;
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>绘制图形属性设置面板</para>
        /// </summary>
        private void DrawGraphSettings()
        {
            GUILayout.Label("Flow Graph Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_CurrentGraph != null)
            {
                EditorGUILayout.LabelField("Node Count", m_CurrentGraph.nodes?.Count.ToString() ?? "0");
                EditorGUILayout.LabelField("Root Node", m_CurrentGraph.RootNode?.NodeName ?? "Not Set");
            }
            else
            {
                EditorGUILayout.HelpBox("No graph selected. Create or load a flow graph to start editing.", MessageType.Info);
            }
        }

        /// <summary>
        ///   <para>获取指定位置的连接</para>
        /// </summary>
        private VisualFlowConnection GetConnectionAtPosition(Vector2 screenPosition)
        {
            if (m_CurrentGraph?.nodes == null) return null;
    
            float closestDistance = float.MaxValue;
            VisualFlowConnection closestConnection = null;
            
            foreach (var node in m_CurrentGraph.nodes)
            {
                if (node == null) continue;
                foreach (var port in node.OutputPorts)
                {
                    foreach (var connection in port.Connections)
                    {
                        if (connection == null) continue;
                        
                        float distance = GetDistanceToConnection(screenPosition, connection);
                        if (distance < 10f && distance < closestDistance) // 10像素阈值
                        {
                            closestDistance = distance;
                            closestConnection = connection;
                        }
                    }
                }
            }
    
            return closestConnection;
        }

        /// <summary>
        ///   <para>计算点到连接线的距离</para>
        /// </summary>
        private float GetDistanceToConnection(Vector2 point, VisualFlowConnection connection)
        {
            var startPos = GetPortCenter(connection.OutputPort);
            var endPos = GetPortCenter(connection.InputPort);
            
            var startTangent = startPos + Vector2.right * 80 * m_CurrentGraph.zoom;
            var endTangent = endPos + Vector2.left * 80 * m_CurrentGraph.zoom;
            
            int segments = 20;
            float minDistance = float.MaxValue;
            
            for (int i = 0; i < segments; i++)
            {
                float t1 = i / (float)segments;
                float t2 = (i + 1) / (float)segments;
                
                Vector2 p1 = CalculateBezierPoint(t1, startPos, startTangent, endTangent, endPos);
                Vector2 p2 = CalculateBezierPoint(t2, startPos, startTangent, endTangent, endPos);
                
                float distance = HandleUtility.DistancePointToLineSegment(point, p1, p2);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
            
            return minDistance;
        }

        /// <summary>
        ///   <para>计算贝塞尔曲线上的点</para>
        /// </summary>
        private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            
            Vector2 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;
            
            return p;
        }

        /// <summary>
        ///   <para>删除连线</para>
        /// </summary>
        private void DeleteConnection(VisualFlowConnection connection)
        {
            if (connection == null) return;
    
            connection.InputPort?.Disconnect(connection);
            connection.OutputPort?.Disconnect(connection);
    
            m_SelectedConnection = null;
            MarkDirty();
            
            GUI.changed = true;
        }

        /// <summary>
        ///   <para>显示菜单内容</para>
        /// </summary>
        private void ShowContextMenu(Vector2 position)
        {
            var menu = new GenericMenu();
    
            if (m_SelectedConnection != null)
            {
                menu.AddItem(new GUIContent("Delete Connection"), false, () => DeleteConnection(m_SelectedConnection));
                menu.AddSeparator("");
            }

            if (s_NodeTypeCache == null) RefreshNodeTypeCache();

            var categories = s_NodeTypeCache.Keys
                .Select(path => path.Split('/')[0])
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            foreach (var category in categories)
            {
                var nodeTypes = s_NodeTypeCache
                    .Where(kv => kv.Key.StartsWith(category + "/"))
                    .OrderBy(kv => kv.Key);
    
                foreach (var (menuPath, nodeType) in nodeTypes)
                {
                    var displayName = menuPath.Substring(category.Length + 1);
                    menu.AddItem(new GUIContent($"Create/{category}/{displayName}"), false, () => 
                        CreateNodeFromTypeAtPosition(nodeType, ScreenToGraphPosition(position)));
                }
            }
            
            menu.AddSeparator("");

            if (m_SelectedNode != null)
            {
                if (m_CurrentGraph != null && m_CurrentGraph.RootNode != m_SelectedNode)
                {
                    menu.AddItem(new GUIContent("Set as Root Node"), false, () => SetAsRootNode(m_SelectedNode));
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Set as Root Node"));
                }
                menu.AddItem(new GUIContent("Delete Node"), false, DeleteSelectedNode);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Set as Root Node"));
                menu.AddDisabledItem(new GUIContent("Delete Node"));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Focus Graph"), false, FocusOnGraph);
            menu.AddItem(new GUIContent("Refresh"), false, RefreshGraph);

            menu.ShowAsContext();
        }

        /// <summary>
        ///   <para>刷新流程图</para>
        /// </summary>
        private void RefreshGraph()
        {
            RefreshNodeTypeCache();
            if (m_CurrentGraph != null)
            {
                m_CurrentGraph.RebuildGraphReferences();
            }
            Repaint();
        }
        
        /// <summary>
        ///   <para>设置为根节点</para>
        /// </summary>
        /// <param name="node">节点</param>
        private void SetAsRootNode(GameFlowNodeWrapper node)
        {
            if (m_CurrentGraph != null && node != null)
            {
                m_CurrentGraph.RootNode = node;
                MarkDirty();
                m_CurrentGraph.RebuildGraphReferences();
                GUI.changed = true;
            }
        }
        
        /// <summary>
        ///   <para>绘制图形界面</para>
        /// </summary>
        private void DrawGraphView()
        {
            m_GraphViewRect = new Rect(0, 20, position.width, position.height - 20);
        
            EditorGUI.DrawRect(m_GraphViewRect, new Color(0.15f, 0.15f, 0.15f, 1f));
        
            DrawGrid(20, 0.1f, new Color(1, 1, 1, 0.1f));
            DrawGrid(100, 0.2f, new Color(1, 1, 1, 0.2f));
        
            GUILayout.BeginArea(m_GraphViewRect);
            {
                GUI.BeginClip(new Rect(0, 0, m_GraphViewRect.width, m_GraphViewRect.height));
                
                DrawConnections();
                
                DrawNodes();
                
                GUI.EndClip();
            }
            GUILayout.EndArea();
            
            DrawPendingConnection();
        }
        
        /// <summary>
        ///   <para>绘制网格</para>
        /// </summary>
        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            var widthDivs = Mathf.CeilToInt(m_GraphViewRect.width / gridSpacing);
            var heightDivs = Mathf.CeilToInt(m_GraphViewRect.height / gridSpacing);
            
            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
            
            var gridOffsetX = m_CurrentGraph.offset.x % gridSpacing;
            var gridOffsetY = m_CurrentGraph.offset.y % gridSpacing;
            
            for (int i = 0; i < widthDivs; i++)
            {
                var xPos = gridSpacing * i - gridOffsetX;
                Handles.DrawLine(
                    new Vector3(xPos, 0, 0),
                    new Vector3(xPos, m_GraphViewRect.height, 0));
            }
            
            for (int j = 0; j < heightDivs; j++)
            {
                var yPos = gridSpacing * j - gridOffsetY;
                Handles.DrawLine(
                    new Vector3(0, yPos, 0),
                    new Vector3(m_GraphViewRect.width, yPos, 0));
            }
            
            Handles.color = Color.white;
            Handles.EndGUI();
        }

        /// <summary>
        ///   <para>绘制所有节点</para>
        /// </summary>
        private void DrawNodes()
        {
            if (m_CurrentGraph == null || m_CurrentGraph.nodes == null) return;
            
            foreach (var node in m_CurrentGraph.nodes)
            {
                DrawNode(node);
            }
        }
        
        /// <summary>
        ///   <para>绘制单个节点</para>
        /// </summary>
        private void DrawNode(GameFlowNodeWrapper node)
        {
            var viewNodePos = (node.Position + m_CurrentGraph.offset) * m_CurrentGraph.zoom;
            var nodeSize = node.NodeSize * m_CurrentGraph.zoom;
            var nodeRect = new Rect(viewNodePos.x, viewNodePos.y, nodeSize.x, nodeSize.y);
            
            var viewportRect = new Rect(0, 0, m_GraphViewRect.width, m_GraphViewRect.height);
            if (!viewportRect.Overlaps(nodeRect))
                return;
            
            GUIStyle nodeStyle;
            if (Application.isPlaying)
            {
                if (node.IsExecuting)
                    nodeStyle = Styles.ExecutingNodeStyle;
                else if (node.IsCompleted)
                    nodeStyle = Styles.CompletedNodeStyle;
                else if (node == m_SelectedNode)
                    nodeStyle = Styles.SelectedNodeStyle;
                else
                    nodeStyle = m_CurrentGraph != null && node == m_CurrentGraph.RootNode ? Styles.RootNodeStyle : Styles.NodeStyle;
            }
            else
            {
                if (node == m_SelectedNode)
                    nodeStyle = Styles.SelectedNodeStyle;
                else
                    nodeStyle = m_CurrentGraph != null && node == m_CurrentGraph.RootNode ? Styles.RootNodeStyle : Styles.NodeStyle;
            }
            
            GUI.Box(nodeRect, new GUIContent(node.NodeName, node.Tooltip), nodeStyle);
            
            var titleRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 24 * m_CurrentGraph.zoom);
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.2f, 1f));
            
            string title = node.NodeName;
            if (m_CurrentGraph != null && node == m_CurrentGraph.RootNode)
            {
                title += " (Root)";
            }
            
            var scaledTitleStyle = new GUIStyle(Styles.NodeTitleStyle);
            scaledTitleStyle.fontSize = Mathf.RoundToInt(12 * m_CurrentGraph.zoom);
            GUI.Label(titleRect, title, scaledTitleStyle);
            
            DrawNodePorts(node, nodeRect);
            
            if (node.WrappedNode is ICompositeGameFlowNode)
            {
                DrawCompositeNodeControls(node, nodeRect);
            }
        }
        
        /// <summary>
        ///   <para>绘制节点的所有端口</para>
        /// </summary>
        private void DrawNodePorts(GameFlowNodeWrapper node, Rect nodeRect)
        {
            float portSize = 12f * m_CurrentGraph.zoom;
            float portSpacing = 22f * m_CurrentGraph.zoom;
            
            float inputY = nodeRect.y + 30 * m_CurrentGraph.zoom;
            foreach (var port in node.InputPorts)
            {
                var portRect = new Rect(nodeRect.x - portSize * 0.5f, inputY, portSize, portSize);
                
                bool isSelected = port == m_SelectedPort || port == m_SelectedInputPort;
                var portStyle = isSelected ? Styles.HighlightPort : Styles.PortStyle;
                
                if (GUI.Button(portRect, "", portStyle))
                {
                    OnPortClicked(port);
                }
                
                var labelRect = new Rect(nodeRect.x + portSize + 3, inputY - 2, nodeRect.width - portSize - 6, 16 * m_CurrentGraph.zoom);
                var scaledPortLabelStyle = new GUIStyle(Styles.PortLabelStyle);
                scaledPortLabelStyle.fontSize = Mathf.RoundToInt(9 * m_CurrentGraph.zoom);
                scaledPortLabelStyle.normal.textColor = GetPortColor(port);
                scaledPortLabelStyle.alignment = TextAnchor.MiddleLeft;
                GUI.Label(labelRect, port.DisplayName, scaledPortLabelStyle);
                
                inputY += portSpacing;
            }
            
            float outputY = nodeRect.y + 30 * m_CurrentGraph.zoom;
            foreach (var port in node.OutputPorts)
            {
                var portRect = new Rect(nodeRect.x + nodeRect.width - portSize * 0.5f, outputY, portSize, portSize);
                
                bool isSelected = port == m_SelectedPort || port == m_SelectedOutputPort;
                var portStyle = isSelected ? Styles.HighlightPort : Styles.PortStyle;
                
                if (GUI.Button(portRect, "", portStyle))
                {
                    OnPortClicked(port);
                }
                
                var labelRect = new Rect(nodeRect.x + 3, outputY - 2, nodeRect.width - portSize - 6, 16 * m_CurrentGraph.zoom);
                var scaledPortLabelStyle = new GUIStyle(Styles.PortLabelStyle);
                scaledPortLabelStyle.fontSize = Mathf.RoundToInt(9 * m_CurrentGraph.zoom);
                scaledPortLabelStyle.normal.textColor = GetPortColor(port);
                scaledPortLabelStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(labelRect, port.DisplayName, scaledPortLabelStyle);
                
                outputY += portSpacing;
            }
        }
        
        /// <summary>
        ///   <para>绘制所有连接</para>
        /// </summary>
        private void DrawConnections()
        {
            if (m_CurrentGraph == null) return;

            Handles.BeginGUI();

            var drawnConnections = new HashSet<VisualFlowConnection>();
    
            foreach (var node in m_CurrentGraph.nodes)
            {
                if (node?.OutputPorts == null) continue;

                var nodeViewRect = new Rect((node.Position + m_CurrentGraph.offset) * m_CurrentGraph.zoom, node.NodeSize * m_CurrentGraph.zoom);
                if (!m_GraphViewRect.Overlaps(nodeViewRect))
                    continue;

                foreach (var port in node.OutputPorts)
                {
                    if (port?.Connections == null) continue;
    
                    foreach (var connection in port.Connections)
                    {
                        if (connection == null || drawnConnections.Contains(connection))
                            continue;
        
                        var inputNode = connection.InputPort?.Node;
                        var outputNode = connection.OutputPort?.Node;
        
                        if (inputNode == null || outputNode == null) 
                        {
                            continue;
                        }
        
                        var inputNodeViewRect = new Rect((inputNode.Position + m_CurrentGraph.offset) * m_CurrentGraph.zoom, inputNode.NodeSize * m_CurrentGraph.zoom);
                        var outputNodeViewRect = new Rect((outputNode.Position + m_CurrentGraph.offset) * m_CurrentGraph.zoom, outputNode.NodeSize * m_CurrentGraph.zoom);
        
                        if (!m_GraphViewRect.Overlaps(inputNodeViewRect) && 
                            !m_GraphViewRect.Overlaps(outputNodeViewRect))
                            continue;
        
                        DrawConnection(connection);
                        drawnConnections.Add(connection);
                    }
                }
            }

            Handles.EndGUI();
        }
        
        /// <summary>
        ///   <para>绘制连接</para>
        /// </summary>
        private void DrawConnection(VisualFlowConnection connection)
        {
            if (connection?.InputPort == null || connection?.OutputPort == null || connection?.InputPort?.Node == null || connection?.OutputPort?.Node == null) return;
        
            var startNodePos = (connection.OutputPort.Node.Position + m_CurrentGraph.offset) * m_CurrentGraph.zoom;
            var endNodePos = (connection.InputPort.Node.Position + m_CurrentGraph.offset) * m_CurrentGraph.zoom;
            
            var startPos = GetPortCenter(connection.OutputPort, startNodePos);
            var endPos = GetPortCenter(connection.InputPort, endNodePos);
        
            var startTangent = startPos + Vector2.right * 80 * m_CurrentGraph.zoom;
            var endTangent = endPos + Vector2.left * 80 * m_CurrentGraph.zoom;

            // 新增：连接线高亮显示
            Color connectionColor = connection == m_SelectedConnection ? Color.yellow : Color.white;
            float connectionWidth = kConnectionLineWidth * m_CurrentGraph.zoom;
        
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, connectionColor, null, connectionWidth);
        }
        
        /// <summary>
        ///  <para>获取端口中心点</para>
        /// </summary>
        private Vector2 GetPortCenter(VisualFlowPort port, Vector2 nodeViewPosition)
        {
            if (port?.Node == null) return Vector2.zero;
        
            var nodeSize = port.Node.NodeSize * m_CurrentGraph.zoom;
            var portList = port.PortDirection == VisualFlowPort.Direction.Input ? 
                port.Node.InputPorts : port.Node.OutputPorts;
        
            var portIndex = portList.IndexOf(port);
            if (portIndex < 0) return nodeViewPosition;
        
            float portHeight = 22f * m_CurrentGraph.zoom;
            float titleHeight = 24f * m_CurrentGraph.zoom;
            float portVerticalSpacing = 2f * m_CurrentGraph.zoom;
        
            float yOffset = titleHeight + (portIndex * portHeight) + portVerticalSpacing + 6 * m_CurrentGraph.zoom;
            float xOffset = port.PortDirection == VisualFlowPort.Direction.Input ? -4 * m_CurrentGraph.zoom : nodeSize.x - 4 * m_CurrentGraph.zoom;
        
            var portCenter = new Vector2(nodeViewPosition.x + xOffset, nodeViewPosition.y + yOffset);
        
            return portCenter;
        }
        
        /// <summary>
        ///   <para>绘制挂起连接</para>
        /// </summary>
        private void DrawPendingConnection()
        {
            if (m_SelectedOutputPort?.Node == null || m_SelectedInputPort?.Node == null) return;

            Handles.BeginGUI();
    
            var startNodePos = (m_SelectedOutputPort.Node.Position + m_CurrentGraph.offset) * m_CurrentGraph.zoom;
            var endNodePos = (m_SelectedInputPort.Node.Position + m_CurrentGraph.offset) * m_CurrentGraph.zoom;
            
            var startPos = GetPortCenter(m_SelectedOutputPort, startNodePos);
            var endPos = GetPortCenter(m_SelectedInputPort, endNodePos);
    
            var startTangent = startPos + Vector2.right * 80 * m_CurrentGraph.zoom;
            var endTangent = endPos + Vector2.left * 80 * m_CurrentGraph.zoom;
    
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.yellow, null, kConnectionLineWidth * m_CurrentGraph.zoom);
    
            Handles.EndGUI();
        }
        
        /// <summary>
        ///   <para>屏幕坐标转图形坐标</para>
        /// </summary>
        private Vector2 ScreenToGraphPosition(Vector2 screenPosition)
        {
            var viewLocalPos = screenPosition - m_GraphViewRect.position;
            return (viewLocalPos / m_CurrentGraph.zoom) - m_CurrentGraph.offset;
        }
        
        /// <summary>
        ///   <para>获取节点</para>
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        private GameFlowNodeWrapper GetNodeAtPosition(Vector2 screenPosition)
        {
            if (m_CurrentGraph?.nodes == null) return null;
        
            var graphPosition = ScreenToGraphPosition(screenPosition);
        
            for (int i = m_CurrentGraph.nodes.Count - 1; i >= 0; i--)
            {
                var node = m_CurrentGraph.nodes[i];
                var nodeRect = new Rect(node.Position.x, node.Position.y, node.NodeSize.x, node.NodeSize.y);
                if (nodeRect.Contains(graphPosition))
                {
                    return node;
                }
            }
        
            return null;
        }
        
        /// <summary>
        ///   <para>获取端口</para>
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        private VisualFlowPort GetPortAtPosition(Vector2 screenPosition)
        {
            if (m_CurrentGraph?.nodes == null) return null;
        
            var graphPosition = ScreenToGraphPosition(screenPosition);
        
            foreach (var node in m_CurrentGraph.nodes)
            {
                var nodeRect = new Rect(node.Position.x, node.Position.y, node.NodeSize.x, node.NodeSize.y);
                if (!nodeRect.Contains(graphPosition)) continue;
        
                float inputY = nodeRect.y + 30;
                foreach (var port in node.InputPorts)
                {
                    var portRect = new Rect(nodeRect.x - 12, inputY - 6, 24, 24);
                    if (portRect.Contains(graphPosition)) 
                    {
                        return port;
                    }
                    inputY += 22;
                }
                
                float outputY = nodeRect.y + 30;
                foreach (var port in node.OutputPorts)
                {
                    var portRect = new Rect(nodeRect.x + nodeRect.width - 12, outputY - 6, 24, 24);
                    if (portRect.Contains(graphPosition)) 
                    {
                        return port;
                    }
                    outputY += 22;
                }
            }
        
            return null;
        }

        /// <summary>
        ///   <para>获取端口中心点</para>
        /// </summary>
        private Vector2 GetPortCenter(VisualFlowPort port)
        {
            if (port?.Node == null) return Vector2.zero;
        
            var nodePos = port.Node.Position;
            var nodeSize = port.Node.NodeSize;
            var portList = port.PortDirection == VisualFlowPort.Direction.Input ? 
                port.Node.InputPorts : port.Node.OutputPorts;
        
            var portIndex = portList.IndexOf(port);
            if (portIndex < 0) return nodePos;
        
            float portHeight = 22f;
            float titleHeight = 24f;
            float portVerticalSpacing = 2f;
        
            float yOffset = titleHeight + (portIndex * portHeight) + portVerticalSpacing + 6;
            float xOffset = port.PortDirection == VisualFlowPort.Direction.Input ? -6 : nodeSize.x - 6;
        
            var portCenter = new Vector2(nodePos.x + xOffset, nodePos.y + yOffset);
        
            return portCenter * m_CurrentGraph.zoom + m_CurrentGraph.offset * m_CurrentGraph.zoom;
        }
                
        /// <summary>
        ///   <para>处理输入事件</para>
        /// </summary>
        private void HandleEvents()
        {
            var e = Event.current;
            
            bool isMouseOverInspector = m_CurrentGraph.inspectorVisible && m_CurrentGraph.inspectorRect.Contains(e.mousePosition);
            
            if (m_IsInspectorDragging || m_IsInspectorResizing)
            {
                if (e.type == EventType.MouseUp)
                {
                    m_IsInspectorDragging = false;
                    m_IsInspectorResizing = false;
                }
                return;
            }
            
            if (isMouseOverInspector && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                e.Use();
                return;
            }
            
            if (!m_GraphViewRect.Contains(e.mousePosition)) return;
            
            if (e.type == EventType.ScrollWheel && !e.control && !e.alt && !e.shift)
            {
                float zoomDelta = -e.delta.y * 0.01f;
                m_CurrentGraph.zoom = Mathf.Clamp(m_CurrentGraph.zoom + zoomDelta, kMinZoom, kMaxZoom);
                e.Use();
                return;
            }
            
            switch (e.type)
            {
                case EventType.MouseDown:
                    HandleMouseDown(e);
                    break;
                    
                case EventType.MouseDrag:
                    HandleMouseDrag(e);
                    break;
                    
                case EventType.MouseUp:
                    HandleMouseUp(e);
                    break;
                    
                case EventType.KeyDown:
                    HandleKeyDown(e);
                    break;
            }
        }
        
        /// <summary>
        ///   <para>处理鼠标点击事件</para>
        /// </summary>
        private void HandleMouseDown(Event e)
        {
            var clickedConnection = GetConnectionAtPosition(e.mousePosition);
            if (clickedConnection != null)
            {
                m_SelectedConnection = clickedConnection;
                m_SelectedNode = null;
                m_SelectedPort = null;
                
                if (e.button == 1)
                {
                    ShowContextMenu(e.mousePosition);
                }
                e.Use();
                return;
            }
            
            if (e.button == 1)
            {
                if (m_SelectedConnection != null)
                {
                    m_SelectedConnection = null;
                }
                
                var clickedConnectionForMenu = GetConnectionAtPosition(e.mousePosition);
                if (clickedConnectionForMenu != null)
                {
                    m_SelectedConnection = clickedConnectionForMenu;
                    ShowContextMenu(e.mousePosition);
                    e.Use();
                    return;
                }
            }
    
            var clickedPort = GetPortAtPosition(e.mousePosition);
            if (clickedPort != null)
            {
                m_SelectedConnection = null;
                OnPortClicked(clickedPort);
                e.Use();
                return;
            }
    
            var clickedNode = GetNodeAtPosition(e.mousePosition);
            if (clickedNode != null)
            {
                m_SelectedConnection = null;
                m_SelectedNode = clickedNode;
                m_SelectedPort = null;
        
                if (e.button == 0)
                {
                    m_IsDragging = true;
                }
                else if (e.button == 1)
                {
                    ShowContextMenu(e.mousePosition);
                }
        
                e.Use();
                return;
            }
    
            if (e.button == 0)
            {
                ClearPortSelection();
                m_SelectedNode = null;
                m_SelectedConnection = null;
                m_SelectedPort = null;
            }
            else if (e.button == 1)
            {
                ShowContextMenu(e.mousePosition);
            }
    
            e.Use();
        }
                
        /// <summary>
        ///   <para>处理鼠标抬起事件</para>
        /// </summary>
        private void HandleMouseUp(Event e)
        {
            if (m_SelectedPort != null)
            {
                var targetPort = GetPortAtPosition(e.mousePosition);
                if (targetPort != null)
                {
                    TryCreateConnection(m_SelectedPort, targetPort);
                }
        
                m_SelectedPort = null;
                e.Use();
            }
        
            m_IsDragging = false;
            e.Use();
        }
                
        /// <summary>
        ///   <para>处理鼠标拖拽事件</para>
        /// </summary>
        /// <remarks>
        ///   <para>鼠标中键或Alt+左键拖拽视图</para>
        /// </remarks>
        private void HandleMouseDrag(Event e)
        {
            if (m_IsDragging && m_SelectedNode != null)
            {
                var delta = e.delta / m_CurrentGraph.zoom;
                m_SelectedNode.Position += delta;
                MarkDirty();
                e.Use();
                GUI.changed = true;
            }
            else if (e.button == 2 || (e.button == 0 && e.alt))
            {
                m_CurrentGraph.offset += e.delta / m_CurrentGraph.zoom;
                e.Use();
                GUI.changed = true;
            }
        }

        /// <summary>
        ///   <para>处理按键按下事件</para>
        /// </summary>
        /// <remarks>
        ///   <para>按下F键聚焦图形</para>
        ///   <para>按下Delete键删除选中节点</para>
        ///   <para>按下Esc键取消选中</para>
        ///   <para>按下Ctrl/Command + I键显示/隐藏属性面板</para>
        ///   <para>按下Ctrl/Command + +键放大视图</para>
        ///   <para>按下Ctrl/Command + -键缩小视图</para>
        /// </remarks>
        private void HandleKeyDown(Event e)
        {
            if (e.keyCode == KeyCode.Delete)
            {
                if (m_SelectedNode != null)
                {
                    DeleteSelectedNode();
                }
                else if (m_SelectedConnection != null)
                {
                    DeleteConnection(m_SelectedConnection);
                }
                e.Use();
            }
            else if (e.keyCode == KeyCode.F && m_CurrentGraph?.nodes != null)
            {
                FocusOnGraph();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                ClearPortSelection();
                m_SelectedPort = null;
                m_SelectedConnection = null;
                e.Use();
            }
            else if (e.keyCode == KeyCode.I && e.command)
            {
                m_CurrentGraph.inspectorVisible = !m_CurrentGraph.inspectorVisible;
                e.Use();
            }
            else if (e.keyCode == KeyCode.Equals && e.command)
            {
                m_CurrentGraph.zoom = Mathf.Clamp(m_CurrentGraph.zoom + kZoomStep, kMinZoom, kMaxZoom);
                e.Use();
            }
            else if (e.keyCode == KeyCode.Minus && e.command)
            {
                m_CurrentGraph.zoom = Mathf.Clamp(m_CurrentGraph.zoom - kZoomStep, kMinZoom, kMaxZoom);
                e.Use();
            }
        }

        /// <summary>
        ///   <para>尝试创建连线</para>
        /// </summary>
        private void TryCreateConnection(VisualFlowPort sourcePort, VisualFlowPort targetPort)
        {
            if (sourcePort == null || targetPort == null)
            {
                Debug.LogError("Cannot create connection: one or both ports are null");
                return;
            }

            VisualFlowPort outputPort = null;
            VisualFlowPort inputPort = null;

            if (sourcePort.PortDirection == VisualFlowPort.Direction.Output && 
                targetPort.PortDirection == VisualFlowPort.Direction.Input)
            {
                outputPort = sourcePort;
                inputPort = targetPort;
            }
            else if (sourcePort.PortDirection == VisualFlowPort.Direction.Input && 
                     targetPort.PortDirection == VisualFlowPort.Direction.Output)
            {
                outputPort = targetPort;
                inputPort = sourcePort;
            }

            try
            {
                if (outputPort?.CanConnectTo(inputPort) == true)
                {
                    outputPort.ConnectTo(inputPort);
                    MarkDirty();
                    
                    if (m_CurrentGraph != null)
                    {
                        m_CurrentGraph.RebuildGraphReferences();
                    }
                    GUI.changed = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating connection: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        ///   <para>处理端口事件</para>
        /// </summary>
        private void OnPortClicked(VisualFlowPort port)
        {
            if (port == null) return;
        
            m_SelectedPort = port;
            m_SelectedNode = null;
            m_SelectedConnection = null;
            
            if (port.PortDirection == VisualFlowPort.Direction.Input)
            {
                if (m_SelectedInputPort == port)
                {
                    m_SelectedInputPort = null;
                }
                else
                {
                    m_SelectedInputPort = port;
        
                    if (m_SelectedOutputPort != null)
                    {
                        TryCreateConnection(m_SelectedOutputPort, m_SelectedInputPort);
                        ClearPortSelection();
                        m_SelectedPort = null;
                    }
                }
            }
            else
            {
                if (m_SelectedOutputPort == port)
                {
                    m_SelectedOutputPort = null;
                }
                else
                {
                    m_SelectedOutputPort = port;
        
                    if (m_SelectedInputPort != null)
                    {
                        TryCreateConnection(m_SelectedOutputPort, m_SelectedInputPort);
                        ClearPortSelection();
                        m_SelectedPort = null;
                    }
                }
            }
        
            GUI.changed = true;
        }
        
        /// <summary>
        ///   <para>聚焦流程图</para>
        /// </summary>
        private void FocusOnGraph()
        {
            if (m_CurrentGraph?.nodes == null || m_CurrentGraph.nodes.Count == 0)
            {
                m_CurrentGraph.offset = Vector2.zero;
                return;
            }
            
            var minX = m_CurrentGraph.nodes.Min(n => n.Position.x);
            var minY = m_CurrentGraph.nodes.Min(n => n.Position.y);
            var maxX = m_CurrentGraph.nodes.Max(n => n.Position.x + n.NodeSize.x);
            var maxY = m_CurrentGraph.nodes.Max(n => n.Position.y + n.NodeSize.y);
            
            var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            
            m_CurrentGraph.offset = -center + m_GraphViewRect.size * 0.5f / m_CurrentGraph.zoom;
            
            GUI.changed = true;
        }
        
        /// <summary>
        ///   <para>清空端口选择</para>
        /// </summary>
        private void ClearPortSelection()
        {
            m_SelectedInputPort = null;
            m_SelectedOutputPort = null;
        }
        
        /// <summary>
        ///   <para>创建节点</para>
        /// </summary>
        private void CreateNodeFromTypeAtPosition(Type nodeType, Vector2 position)
        {
            if (m_CurrentGraph == null)
            {
                CreateNewGraph();
            }
            
            try
            {
                var wrapper = GameFlowNodeWrapper.CreateWrapper((GameFlowNode)Activator.CreateInstance(nodeType), position);
                
                AssetDatabase.AddObjectToAsset(wrapper, m_CurrentGraph);
                m_CurrentGraph.nodes.Add(wrapper);
                
                if (m_CurrentGraph.nodes.Count == 1 && m_CurrentGraph.RootNode == null)
                {
                    m_CurrentGraph.RootNode = wrapper;
                }

                m_CurrentGraph.RebuildGraphReferences();
            
                MarkDirty();
                GUI.changed = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create node of type {nodeType}: {ex.Message}");
            }
        }
        
        /// <summary>
        ///   <para>删除选中的节点</para>
        /// </summary>
        private void DeleteSelectedNode()
        {
            if (m_SelectedNode == null || m_CurrentGraph == null) return;
            
            if (m_CurrentGraph.RootNode == m_SelectedNode)
            {
                m_CurrentGraph.RootNode = null;
            }
    
            foreach (var port in m_SelectedNode.InputPorts)
            {
                port?.DisconnectAll();
            }
    
            foreach (var port in m_SelectedNode.OutputPorts)
            {
                port?.DisconnectAll();
            }
    
            m_CurrentGraph.nodes.Remove(m_SelectedNode);
    
            if (m_SelectedNode != null)
            {
                DestroyImmediate(m_SelectedNode, true);
            }
    
            m_SelectedNode = null;
            MarkDirty();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
    
            GUI.changed = true;
    
            m_CurrentGraph.RebuildGraphReferences();
        }
        
        /// <summary>
        ///   <para>创建新流程图形资源</para>
        /// </summary>
        private void CreateNewGraph()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Flow Graph",
                "NewFlowGraph",
                "asset",
                "Create a new flow graph");
                
            if (!string.IsNullOrEmpty(path))
            {
                var graph = CreateInstance<GameFlowGraphAsset>();
                AssetDatabase.CreateAsset(graph, path);
                m_CurrentGraph = graph;
                
                m_CurrentGraph.inspectorRect = new Rect(
                    position.width - 310,
                    30, 
                    300, 
                    400
                );
                
                s_OpenWindows[path] = this;
                
                GUI.changed = true;
                Focus();
                FocusOnGraph();
            }
        }
        
        /// <summary>
        ///   <para>标记为有未保存的更改</para>
        /// </summary>
        private void MarkDirty()
        {
            if (!m_IsDirty)
            {
                m_IsDirty = true;
                UpdateWindowTitle();
            }
        }

        /// <summary>
        ///   <para>保存图形更改</para>
        /// </summary>
        private void SaveGraphChanges()
        {
            if (m_CurrentGraph != null)
            {
                EditorUtility.SetDirty(m_CurrentGraph);
                foreach (var node in m_CurrentGraph.nodes)
                {
                    if (node != null) EditorUtility.SetDirty(node);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                if (m_IsDirty)
                {
                    m_IsDirty = false;
                    UpdateWindowTitle();
                }
            }
        }
    }
}

#endif