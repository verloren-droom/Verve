#if UNITY_EDITOR

namespace VerveEditor.Flow
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
    ///  <para>流程节点图编辑窗口</para>
    /// </summary>
    internal class GameFlowGraphEditorWindow : EditorWindow
    {
        [SerializeField, Tooltip("当前流程图")] private GameFlowGraphAsset m_CurrentGraph;
        [SerializeField, Tooltip("当前流程图偏移量")] private Vector2 m_GraphOffset;
        [SerializeField, Tooltip("当前流程图输入端口")] private VisualFlowPort m_SelectedInputPort;
        [SerializeField, Tooltip("当前流程图输出端口")] private VisualFlowPort m_SelectedOutputPort;

        /// <summary>
        ///  <para>当前选中的输入端口</para>
        /// </summary>
        private GameFlowNodeWrapper m_SelectedNode;
        /// <summary>
        ///  <para>当前选中的端口</para>
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
        
        /// <summary>
        ///  <para>连接线宽度</para>
        /// </summary>
        private const float CONNECTION_LINE_WIDTH = 4f;
        
        /// <summary>
        ///  <para>内部样式</para>
        /// </summary>
        private static class Styles
        {
            private static readonly List<Texture2D> s_CreatedTextures = new List<Texture2D>();

            /// <summary>
            ///  <para>节点样式</para>
            /// </summary>
            public static GUIStyle NodeStyle { get; private set; }
            /// <summary>
            ///  <para>已选节点样式</para>
            /// </summary>
            public static GUIStyle SelectedNodeStyle { get; private set; }
            /// <summary>
            ///  <para>执行中节点样式</para>
            /// </summary>
            public static GUIStyle ExecutingNodeStyle { get; private set; }
            /// <summary>
            ///  <para>已完成节点样式</para>
            /// </summary>
            public static GUIStyle CompletedNodeStyle { get; private set; }
            /// <summary>
            ///  <para>节点标题样式</para>
            /// </summary>
            public static GUIStyle NodeTitleStyle { get; private set; }
            /// <summary>
            ///  <para>端口样式</para>
            /// </summary>
            public static GUIStyle PortStyle { get; private set; }
            /// <summary>
            ///  <para>端口标签样式</para>
            /// </summary>
            public static GUIStyle PortLabelStyle { get; private set; }
            /// <summary>
            ///  <para>高亮端口样式</para>
            /// </summary>
            public static GUIStyle HighlightPort { get; private set; }
            
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
            }
            
            /// <summary>
            ///  <para>创建颜色纹理</para>
            /// </summary>
            private static Texture2D CreateColorTexture(Color color)
            {
                var texture = new Texture2D(1, 1);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.SetPixel(0, 0, color);
                texture.Apply();
                s_CreatedTextures.Add(texture);
                return texture;
            }
            
            /// <summary>
            ///  <para>清理纹理</para>
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
        ///  <para>打开流程图编辑窗口</para>
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<GameFlowGraphEditorWindow>();
            window.titleContent = new GUIContent("Flow Graph");
            window.minSize = new Vector2(800, 600);
            window.Focus();
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshNodeTypeCache();
            
            if (m_CurrentGraph != null)
            {
                m_CurrentGraph.RebuildGraphReferences();
            }
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            DrawGraphView();
            DrawSidePanel();
            HandleEvents();
            
            if (m_CurrentGraph != null && GUI.changed)
            {
                m_CurrentGraph.RebuildGraphReferences();
            }
            
            if (GUI.changed) Repaint();
        }

        // private void OnDisable()
        // {
        //     Styles.Cleanup();
        // }

        /// <summary>
        ///  <para>刷新节点类型缓存</para>
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
        ///  <para>流程图资源发生改变时调用</para>
        /// </summary>
        private void OnGraphAssetChanged()
        {
            m_SelectedNode = null;
            m_SelectedPort = null;
            m_SelectedInputPort = null;
            m_SelectedOutputPort = null;
            
            if (m_CurrentGraph != null)
            {
                m_CurrentGraph.RebuildGraphReferences();
            }
        }

        /// <summary>
        ///  <para>绘制复合节点的子节点控制按钮</para>
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
                // Undo.RecordObject(node, "Add Child");
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
                // Undo.RecordObject(node, "Remove Child");
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
        ///  <para>获取端口颜色</para>
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
        ///  <para>绘制侧边栏</para>
        /// </summary>
        private void DrawSidePanel()
        {
            var sidePanelRect = new Rect(position.width - 300, 20, 300, position.height - 20);
            GUILayout.BeginArea(sidePanelRect, EditorStyles.helpBox);
            {
                if (m_SelectedPort != null)
                {
                    DrawPortInspector();
                }
                else if (m_SelectedNode != null)
                {
                    DrawNodeInspector();
                }
                else
                {
                    DrawGraphInspector();
                }
            }
            GUILayout.EndArea();
        }
        
        /// <summary>
        ///  <para>绘制端口属性面板</para>
        /// </summary>
        private void DrawPortInspector()
        {
            GUILayout.Label("Port Inspector", EditorStyles.boldLabel);
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
        ///  <para>绘制流程图节点属性面板</para>
        /// </summary>
        private void DrawNodeInspector()
        {
            GUILayout.Label("Node Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
    
            if (m_SelectedNode != null && m_SelectedNode.WrappedNode != null)
            {
                var serializedNode = new SerializedObject(m_SelectedNode);
                serializedNode.Update();

                var nodeNameProp = serializedNode.FindProperty("m_NodeName");
                var descriptionProp = serializedNode.FindProperty("m_Description");
                if (nodeNameProp != null)
                    EditorGUILayout.PropertyField(nodeNameProp);
                if (descriptionProp != null)
                    EditorGUILayout.PropertyField(descriptionProp);

                EditorGUILayout.Space();
        
                var wrappedNodeProp = serializedNode.FindProperty("m_WrappedNode");
                if (wrappedNodeProp != null)
                {
                    var childProperty = wrappedNodeProp.Copy();
                    var enterChildren = true;
                    while (childProperty.NextVisible(enterChildren))
                    {
                        if (!childProperty.propertyPath.StartsWith(wrappedNodeProp.propertyPath))
                            break;

                        EditorGUILayout.PropertyField(childProperty, true);
                        enterChildren = false;
                    }
                }        
                
                if (serializedNode.ApplyModifiedProperties())
                {
                    SaveGraphChanges();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No node selected", MessageType.Info);
            }
        }
        
        /// <summary>
        ///  <para>绘制流程图绘制工具栏面板</para>
        /// </summary>
        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUI.BeginChangeCheck();
                m_CurrentGraph = (GameFlowGraphAsset)EditorGUILayout.ObjectField(
                    m_CurrentGraph, typeof(GameFlowGraphAsset), false, GUILayout.Width(200));
                if (EditorGUI.EndChangeCheck())
                {
                    OnGraphAssetChanged();
                }
                
                if (GUILayout.Button("New Graph", EditorStyles.toolbarButton))
                {
                    CreateNewGraph();
                }
                
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    SaveGraph();
                }
                
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// <para>绘制图形属性面板</para>
        /// </summary>
        private void DrawGraphInspector()
        {
            GUILayout.Label("Flow Graph", EditorStyles.boldLabel);
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
        ///  <para>获取指定位置的连接</para>
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        /// <returns></returns>
        private VisualFlowConnection GetConnectionAtPosition(Vector2 screenPosition)
        {
            if (m_CurrentGraph?.nodes == null) return null;
    
            foreach (var node in m_CurrentGraph.nodes)
            {
                if (node == null) continue;
                foreach (var port in node.OutputPorts)
                {
                    foreach (var connection in port.Connections)
                    {
                        if (IsPointNearConnection(screenPosition, connection))
                        {
                            return connection;
                        }
                    }
                }
            }
    
            return null;
        }

        /// <summary>
        ///  <para>判断点是否接近连线</para>
        /// </summary>
        private bool IsPointNearConnection(Vector2 point, VisualFlowConnection connection)
        {
            var startPos = GetPortCenter(connection.OutputPort);
            var endPos = GetPortCenter(connection.InputPort);
    
            return HandleUtility.DistancePointToLine(point, startPos, endPos) < 10f;
        }

        /// <summary>
        ///  <para>删除连线</para>
        /// </summary>
        private void DeleteConnection(VisualFlowConnection connection)
        {
            if (connection == null) return;
    
            connection.InputPort?.Disconnect(connection);
            connection.OutputPort?.Disconnect(connection);
    
            m_SelectedConnection = null;
            SaveGraphChanges();
            
            GUI.changed = true;
        }

        /// <summary>
        ///  <para>显示菜单内容</para>
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
        ///  <para>刷新流程图</para>
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
        ///  <para>设置为根节点</para>
        /// </summary>
        /// <param name="node">节点</param>
        private void SetAsRootNode(GameFlowNodeWrapper node)
        {
            if (m_CurrentGraph != null && node != null)
            {
                // Undo.RecordObject(m_CurrentGraph, "Set Root Node");
                m_CurrentGraph.RootNode = node;
                SaveGraphChanges();
                m_CurrentGraph.RebuildGraphReferences();
                GUI.changed = true;
            }
        }
        
        /// <summary>
        ///  <para>绘制图形界面</para>
        /// </summary>
        private void DrawGraphView()
        {
            m_GraphViewRect = new Rect(0, 20, position.width - 300, position.height - 20);
        
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
        ///  <para>绘制网格</para>
        /// </summary>
        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            var widthDivs = Mathf.CeilToInt(m_GraphViewRect.width / gridSpacing);
            var heightDivs = Mathf.CeilToInt(m_GraphViewRect.height / gridSpacing);
            
            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
            
            var gridOffsetX = m_GraphOffset.x % gridSpacing;
            var gridOffsetY = m_GraphOffset.y % gridSpacing;
            
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
        ///  <para>绘制所有节点</para>
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
        ///  <para>绘制单个节点</para>
        /// </summary>
        private void DrawNode(GameFlowNodeWrapper node)
        {
            var viewNodePos = node.Position + m_GraphOffset;
            var nodeRect = new Rect(viewNodePos.x, viewNodePos.y, node.NodeSize.x, node.NodeSize.y);
            
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
                else
                    nodeStyle = node == m_SelectedNode ? Styles.SelectedNodeStyle : Styles.NodeStyle;
            }
            else
            {
                nodeStyle = node == m_SelectedNode ? Styles.SelectedNodeStyle : Styles.NodeStyle;
            }
            
            GUI.Box(nodeRect, "", nodeStyle);
            
            var titleRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 24);
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.2f, 1f));
            
            string title = node.NodeName;
            if (m_CurrentGraph != null && node == m_CurrentGraph.RootNode)
            {
                title += " (Root)";
            }
            GUI.Label(titleRect, title, Styles.NodeTitleStyle);
            
            DrawNodePorts(node, nodeRect);
            
            if (node.WrappedNode is ICompositeGameFlowNode)
            {
                DrawCompositeNodeControls(node, nodeRect);
            }
        }
        
        /// <summary>
        ///  <para>绘制节点的所有端口</para>
        /// </summary>
        private void DrawNodePorts(GameFlowNodeWrapper node, Rect nodeRect)
        {
            float inputY = nodeRect.y + 30;
            foreach (var port in node.InputPorts)
            {
                var portRect = new Rect(nodeRect.x - 6, inputY, 12, 12);
                
                bool isSelected = port == m_SelectedPort || port == m_SelectedInputPort;
                var portStyle = isSelected ? Styles.HighlightPort : Styles.PortStyle;
                
                if (GUI.Button(portRect, "", portStyle))
                {
                    OnPortClicked(port);
                }
                
                var labelRect = new Rect(nodeRect.x + 15, inputY - 2, nodeRect.width - 30, 16);
                Styles.PortLabelStyle.normal.textColor = GetPortColor(port);
                Styles.PortLabelStyle.alignment = TextAnchor.MiddleLeft;
                GUI.Label(labelRect, port.DisplayName, Styles.PortLabelStyle);
                
                inputY += 22;
            }
            
            float outputY = nodeRect.y + 30;
            foreach (var port in node.OutputPorts)
            {
                var portRect = new Rect(nodeRect.x + nodeRect.width - 6, outputY, 12, 12);
                
                bool isSelected = port == m_SelectedPort || port == m_SelectedOutputPort;
                var portStyle = isSelected ? Styles.HighlightPort : Styles.PortStyle;
                
                if (GUI.Button(portRect, "", portStyle))
                {
                    OnPortClicked(port);
                }
                
                var labelRect = new Rect(nodeRect.x + 10, outputY - 2, nodeRect.width - 30, 16);
                Styles.PortLabelStyle.normal.textColor = GetPortColor(port);
                Styles.PortLabelStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(labelRect, port.DisplayName, Styles.PortLabelStyle);
                
                outputY += 22;
            }
        }
        
        /// <summary>
        ///  <para>绘制所有连接</para>
        /// </summary>
        private void DrawConnections()
        {
            if (m_CurrentGraph == null) return;

            Handles.BeginGUI();

            var drawnConnections = new HashSet<VisualFlowConnection>();
    
            foreach (var node in m_CurrentGraph.nodes)
            {
                if (node?.OutputPorts == null) continue;

                var nodeViewRect = new Rect(node.Position + m_GraphOffset, node.NodeSize);
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
        
                        var inputNodeViewRect = new Rect(inputNode.Position + m_GraphOffset, inputNode.NodeSize);
                        var outputNodeViewRect = new Rect(outputNode.Position + m_GraphOffset, outputNode.NodeSize);
        
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
        ///  <para>绘制连接</para>
        /// </summary>
        private void DrawConnection(VisualFlowConnection connection)
        {
            if (connection?.InputPort == null || connection?.OutputPort == null || connection?.InputPort?.Node == null || connection?.OutputPort?.Node == null) return;
        
            // Undo.RecordObjects(new Object[] {connection.OutputPort.Node, connection.InputPort.Node}, "Delete Connection");
            
            var startNodePos = connection.OutputPort.Node.Position + m_GraphOffset;
            var endNodePos = connection.InputPort.Node.Position + m_GraphOffset;
            
            var startPos = GetPortCenter(connection.OutputPort, startNodePos);
            var endPos = GetPortCenter(connection.InputPort, endNodePos);
        
            var startTangent = startPos + Vector2.right * 80;
            var endTangent = endPos + Vector2.left * 80;
        
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.white, null, CONNECTION_LINE_WIDTH);
        }
        
        /// <summary>
        ///  <para>获取端口中心点</para>
        /// </summary>
        private Vector2 GetPortCenter(VisualFlowPort port, Vector2 nodeViewPosition)
        {
            if (port?.Node == null) return Vector2.zero;
        
            var nodeSize = port.Node.NodeSize;
            var portList = port.PortDirection == VisualFlowPort.Direction.Input ? 
                port.Node.InputPorts : port.Node.OutputPorts;
        
            var portIndex = portList.IndexOf(port);
            if (portIndex < 0) return nodeViewPosition;
        
            float portHeight = 22f;
            float titleHeight = 24f;
            float portVerticalSpacing = 2f;
        
            float yOffset = titleHeight + (portIndex * portHeight) + portVerticalSpacing + 6;
            float xOffset = port.PortDirection == VisualFlowPort.Direction.Input ? -4 : nodeSize.x - 4;
        
            var portCenter = new Vector2(nodeViewPosition.x + xOffset, nodeViewPosition.y + yOffset);
        
            return portCenter;
        }
        
        /// <summary>
        ///  <para>绘制挂起连接</para>
        /// </summary>
        private void DrawPendingConnection()
        {
            if (m_SelectedOutputPort?.Node == null || m_SelectedInputPort?.Node == null) return;

            Handles.BeginGUI();
    
            var startNodePos = m_SelectedOutputPort.Node.Position + m_GraphOffset;
            var endNodePos = m_SelectedInputPort.Node.Position + m_GraphOffset;
            
            var startPos = GetPortCenter(m_SelectedOutputPort, startNodePos);
            var endPos = GetPortCenter(m_SelectedInputPort, endNodePos);
    
            var startTangent = startPos + Vector2.right * 80;
            var endTangent = endPos + Vector2.left * 80;
    
            Handles.DrawBezier(startPos, endPos, startTangent, endTangent, Color.yellow, null, CONNECTION_LINE_WIDTH);
    
            Handles.EndGUI();
        }
        
        /// <summary>
        ///  <para>屏幕坐标转图形坐标</para>
        /// </summary>
        private Vector2 ScreenToGraphPosition(Vector2 screenPosition)
        {
            var viewLocalPos = screenPosition - m_GraphViewRect.position;
            return viewLocalPos - m_GraphOffset;
        }
        
        /// <summary>
        ///  <para>获取节点</para>
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        /// <returns></returns>
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
        ///  <para>获取端口</para>
        /// </summary>
        /// <param name="screenPosition">屏幕坐标</param>
        /// <returns></returns>
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
        ///  <para>获取端口中心点</para>
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
        
            return portCenter;
        }
                
        /// <summary>
        ///  <para>处理输入事件</para>
        /// </summary>
        private void HandleEvents()
        {
            var e = Event.current;
            
            if (!m_GraphViewRect.Contains(e.mousePosition)) return;
            
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
        ///  <para>处理鼠标点击事件</para>
        /// </summary>
        private void HandleMouseDown(Event e)
        {
            if (e.button == 1)
            {
                var clickedConnection = GetConnectionAtPosition(e.mousePosition);
                if (clickedConnection != null)
                {
                    m_SelectedConnection = clickedConnection;
                    ShowContextMenu(e.mousePosition);
                    e.Use();
                    return;
                }
            }
    
            var clickedPort = GetPortAtPosition(e.mousePosition);
            if (clickedPort != null)
            {
                OnPortClicked(clickedPort);
                e.Use();
                return;
            }
    
            var clickedNode = GetNodeAtPosition(e.mousePosition);
            if (clickedNode != null)
            {
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
        /// 处理鼠标抬起事件
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
        ///  <para>处理鼠标拖拽事件</para>
        ///  <para>鼠标中键或Alt+左键拖拽视图</para>
        /// </summary>
        private void HandleMouseDrag(Event e)
        {
            if (m_IsDragging && m_SelectedNode != null)
            {
                // Undo.RecordObject(m_SelectedNode, "Move Node");
                var delta = e.delta;
                m_SelectedNode.Position += delta;
                e.Use();
                GUI.changed = true;
            }
            else if (e.button == 2 || (e.button == 0 && e.alt))
            {
                m_GraphOffset += e.delta;
                e.Use();
                GUI.changed = true;
            }
        }

        /// <summary>
        ///  <para>处理按键按下事件</para>
        ///  <para>按下F键聚焦图形</para>
        ///  <para>按下Delete键删除选中节点</para>
        /// </summary>
        private void HandleKeyDown(Event e)
        {
            if (e.keyCode == KeyCode.Delete && m_SelectedNode != null)
            {
                DeleteSelectedNode();
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
                e.Use();
            }
        }

        /// <summary>
        ///  <para>尝试创建连线</para>
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
                    // Undo.RecordObjects(new Object[] { outputPort.Node, inputPort.Node }, "Create Connection");
                    
                    outputPort.ConnectTo(inputPort);
                    SaveGraphChanges();
                    
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
        ///  <para>处理端口事件</para>
        /// </summary>
        private void OnPortClicked(VisualFlowPort port)
        {
            if (port == null) return;
        
            m_SelectedPort = port;
            m_SelectedNode = null;
            
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
        ///  <para>聚焦流程图</para>
        /// </summary>
        private void FocusOnGraph()
        {
            if (m_CurrentGraph?.nodes == null || m_CurrentGraph.nodes.Count == 0)
            {
                m_GraphOffset = Vector2.zero;
                return;
            }
            
            var minX = m_CurrentGraph.nodes.Min(n => n.Position.x);
            var minY = m_CurrentGraph.nodes.Min(n => n.Position.y);
            var maxX = m_CurrentGraph.nodes.Max(n => n.Position.x + n.NodeSize.x);
            var maxY = m_CurrentGraph.nodes.Max(n => n.Position.y + n.NodeSize.y);
            
            var center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            
            m_GraphOffset = -center + m_GraphViewRect.size * 0.5f;
            
            GUI.changed = true;
        }
        
        /// <summary>
        ///  <para>清空端口选择</para>
        /// </summary>
        private void ClearPortSelection()
        {
            m_SelectedInputPort = null;
            m_SelectedOutputPort = null;
        }
        
        /// <summary>
        ///  <para>创建节点</para>
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="position">位置</param>
        private void CreateNodeFromTypeAtPosition(Type nodeType, Vector2 position)
        {
            if (m_CurrentGraph == null)
            {
                CreateNewGraph();
                if (m_CurrentGraph == null) return;
            }
            
            try
            {
                // Undo.RecordObject(m_CurrentGraph, "Create Node");
                var runtimeNode = (GameFlowNode)Activator.CreateInstance(nodeType);
                
                var wrapper = GameFlowNodeWrapper.CreateWrapper(runtimeNode, position);
                
                m_CurrentGraph.nodes.Add(wrapper);
                // Undo.RegisterCreatedObjectUndo(wrapper, "Create Node");
                AssetDatabase.AddObjectToAsset(wrapper, m_CurrentGraph);
                
                if (m_CurrentGraph.nodes.Count == 1 && m_CurrentGraph.RootNode == null)
                {
                    m_CurrentGraph.RootNode = wrapper;
                }
                
                SaveGraphChanges();
                
                GUI.changed = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create node of type {nodeType}: {ex.Message}");
            }
        }
        
        /// <summary>
        ///  <para>删除选中的节点</para>
        /// </summary>
        private void DeleteSelectedNode()
        {
            if (m_SelectedNode == null || m_CurrentGraph == null) return;
            
            // Undo.RecordObject(m_CurrentGraph, "Delete Node");
    
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
                string nodeName = m_SelectedNode.name;
        
                DestroyImmediate(m_SelectedNode, true);
            }
    
            m_SelectedNode = null;
            SaveGraphChanges();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
    
            GUI.changed = true;
    
            m_CurrentGraph.RebuildGraphReferences();
        }
        
        /// <summary>
        ///  <para>创建新流程图形资源</para>
        /// </summary>
        private void CreateNewGraph()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Flow Graph", "NewFlowGraph", "asset", "Create a new flow graph");
                
            if (!string.IsNullOrEmpty(path))
            {
                var graph = CreateInstance<GameFlowGraphAsset>();
                AssetDatabase.CreateAsset(graph, path);
                m_CurrentGraph = graph;
                GUI.changed = true;
                Focus();
                FocusOnGraph();
            }
        }
        
        /// <summary>
        ///  <para>保存图形更改</para>
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
            }
        }
        
        /// <summary>
        ///  <para>保存流程图</para>
        /// </summary>
        private void SaveGraph()
        {
            if (m_CurrentGraph != null)
            {
                SaveGraphChanges();
                AssetDatabase.Refresh();
            }
        }
    }
}

#endif