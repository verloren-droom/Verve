#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;

    
    /// <summary>
    ///   <para>World调试器窗口</para>
    /// </summary>
    internal sealed class WorldDebuggerWindow : EditorWindow
    {
        private const float LEFT_PANEL_WIDTH = 280f;
        private const float RIGHT_PANEL_WIDTH = 450f;
        private const float TOOLBAR_HEIGHT = 28f;
        private const float ITEM_HEIGHT = 28f;
        private const float SEPARATOR_THICKNESS = 1f;
        private const float PADDING = 10f;
        private const float DETAIL_LABEL_WIDTH = 120f;

        private Vector2 m_LeftScrollPos;
        private Vector2 m_MiddleScrollPos;
        private Vector2 m_RightScrollPos;

        private World m_SelectedWorld;
        private Actor m_SelectedActor;
        private Type m_SelectedCapabilityType;
        private Actor m_HoveredActor;
        private Type m_HoveredCapabilityType;

        private enum ViewMode { Actors, Capabilities }
        private ViewMode m_CurrentViewMode = ViewMode.Actors;
        private string m_SearchText = "";
        private bool m_ShowComponentDetails = true;
        private bool m_ShowCapabilityDetails = true;
        private double m_LastUpdateTime = 0;
        private bool m_AutoRefresh = true;
        private int m_RefreshIntervalIndex = 2;

        private static readonly float[] s_RefreshIntervals = { 0.1f, 0.25f, 0.5f, 1f, 2f };
        private static readonly string[] s_RefreshIntervalLabels = { "0.1s", "0.25s", "0.5s", "1s", "2s" };

        private static bool s_ReflectionInitialized = false;
        private static FieldInfo s_ActorsField;
        private static MethodInfo s_GetComponentMaskMethod;
        private static MethodInfo s_GetCapabilitiesMethod;
        private static FieldInfo s_ActionQueueField;
        private static FieldInfo s_ActionQueueGroupsField;
        private static FieldInfo s_UpdateGroupActiveField;
        private static FieldInfo s_UpdateGroupInactiveField;
        private static FieldInfo s_ActorDataIsAliveField;
        private static FieldInfo s_ActorDataVersionField;

        /// <summary>
        ///   <para>样式</para>
        /// </summary>
        private static class Styles
        {
            public static readonly GUIStyle HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 8, 8)
            };
            
            public static readonly GUIStyle ItemStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                padding = new RectOffset(10, 10, 6, 6),
                margin = new RectOffset(2, 2, 0, 0),
                clipping = TextClipping.Clip
            };
            
            public static readonly GUIStyle SelectedItemStyle = new GUIStyle(ItemStyle)
            {
                normal = { 
                    background = CoreEditorUtility.MakeTex(1, 1, new Color(0.1f, 0.5f, 0.9f, 0.4f)),
                    textColor = Color.white 
                },
                hover = { 
                    background = CoreEditorUtility.MakeTex(1, 1, new Color(0.1f, 0.5f, 0.9f, 0.3f)),
                    textColor = Color.white 
                },
                clipping = TextClipping.Clip
            };
            
            public static readonly GUIStyle HoveredItemStyle = new GUIStyle(ItemStyle)
            {
                normal = { 
                    background = CoreEditorUtility.MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 0.15f)),
                    textColor = ItemStyle.normal.textColor 
                },
                hover = { 
                    background = CoreEditorUtility.MakeTex(1, 1, new Color(0.3f, 0.3f, 0.3f, 0.2f)),
                    textColor = ItemStyle.normal.textColor 
                },
                clipping = TextClipping.Clip
            };
            
            public static readonly GUIStyle TagStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 3, 3),
                margin = new RectOffset(3, 3, 2, 2),
                normal = { 
                    background = CoreEditorUtility.MakeTex(1, 1, EditorGUIUtility.isProSkin ? 
                        new Color(0.3f, 0.3f, 0.3f, 0.3f) : 
                        new Color(0.8f, 0.8f, 0.8f, 0.3f)) 
                },
                border = new RectOffset(5, 5, 5, 5),
                clipping = TextClipping.Clip
            };
            
            public static readonly GUIStyle PropertyStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                padding = new RectOffset(6, 6, 3, 3),
                margin = new RectOffset(0, 0, 2, 2),
                clipping = TextClipping.Clip
            };
            
            public static readonly GUIStyle MiniTagStyle = new GUIStyle(TagStyle)
            {
                fontSize = 9,
                padding = new RectOffset(6, 6, 2, 2),
                clipping = TextClipping.Clip
            };
            
            public static readonly GUIStyle SearchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                margin = new RectOffset(6, 6, 6, 6),
                fontSize = 12,
                clipping = TextClipping.Clip
            };
            
            public static readonly GUIStyle CardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 10, 10),
                margin = new RectOffset(0, 0, 8, 8),
                normal = { 
                    background = CoreEditorUtility.MakeTex(1, 1, EditorGUIUtility.isProSkin ? 
                        new Color(0.2f, 0.2f, 0.2f, 0.5f) : 
                        new Color(0.95f, 0.95f, 0.95f, 0.5f)) 
                }
            };
            
            public static readonly GUIStyle LabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            
            public static readonly GUIStyle HighlightStyle = new GUIStyle(SelectedItemStyle)
            {
                normal = { 
                    background = CoreEditorUtility.MakeTex(1, 1, new Color(1f, 0.8f, 0f, 0.3f)),
                    textColor = Color.white 
                },
                hover = { 
                    background = CoreEditorUtility.MakeTex(1, 1, new Color(1f, 0.8f, 0f, 0.4f)),
                    textColor = Color.white 
                }
            };
        }

        [MenuItem("Window/Verve/World Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<WorldDebuggerWindow>("World Debugger");
            window.minSize = new Vector2(1200f, 700f);
            window.titleContent = new GUIContent("World Debugger", EditorGUIUtility.IconContent("d_UnityEditor.SceneHierarchyWindow").image);
        }

        #region Unity生命周期
        
        private void OnEnable()
        {
            InitializeReflection();
            
            EditorApplication.update += OnEditorUpdate;
            m_LastUpdateTime = EditorApplication.timeSinceStartup;
            
            wantsMouseMove = true;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            ClearSelection();
        }

        private void OnEditorUpdate()
        {
            if (!m_AutoRefresh) return;

            double currentTime = EditorApplication.timeSinceStartup;
            var interval = s_RefreshIntervals[Mathf.Clamp(m_RefreshIntervalIndex, 0, s_RefreshIntervals.Length - 1)];
            
            if (currentTime - m_LastUpdateTime > interval)
            {
                m_LastUpdateTime = currentTime;
                
                if (m_SelectedWorld != null && (m_SelectedWorld.IsDisposed || !IsWorldValid(m_SelectedWorld)))
                {
                    m_SelectedWorld = null;
                    m_SelectedActor = default;
                    m_SelectedCapabilityType = null;
                    Repaint();
                }

                if (!m_SelectedActor.IsNone && m_SelectedWorld != null)
                {
                    if (!m_SelectedWorld.IsActorAlive(m_SelectedActor))
                    {
                        m_SelectedActor = default;
                        Repaint();
                    }
                }

                if (m_SelectedCapabilityType != null && m_SelectedWorld != null)
                {
                    var caps = GetAllCapabilityTypes();
                    if (caps == null || !caps.Contains(m_SelectedCapabilityType))
                    {
                        m_SelectedCapabilityType = null;
                        Repaint();
                    }
                }
            }
        }

        private bool IsWorldValid(World world)
        {
            try
            {
                var worlds = Game.Worlds;
                if (worlds == null) return false;
                return worlds.Any(w => w == world && !w.IsDisposed);
            }
            catch
            {
                return false;
            }
        }
        
        #endregion

        #region 主界面绘制
        
        private void OnGUI()
        {
            DrawToolbar();
            DrawMainLayout();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TOOLBAR_HEIGHT));
            
            try
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    m_LastUpdateTime = 0;
                    OnEditorUpdate();
                    Repaint();
                }

                m_AutoRefresh = GUILayout.Toggle(m_AutoRefresh, "自动刷新", EditorStyles.toolbarButton, GUILayout.Width(70));
                m_RefreshIntervalIndex = EditorGUILayout.Popup(m_RefreshIntervalIndex, s_RefreshIntervalLabels, EditorStyles.toolbarPopup, GUILayout.Width(60));

                if (GUILayout.Button("清除选择", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    ClearSelection();
                    Repaint();
                }

                if (m_SelectedWorld != null && !m_SelectedWorld.IsDisposed && IsWorldValid(m_SelectedWorld))
                {
                    GUILayout.FlexibleSpace();

                    if (!m_SelectedActor.IsNone)
                    {
                        GUILayout.Label($"Actor #{m_SelectedActor.id}", Styles.TagStyle, GUILayout.Width(90));
                        if (GUILayout.Button("复制", EditorStyles.toolbarButton, GUILayout.Width(36)))
                        {
                            EditorGUIUtility.systemCopyBuffer = m_SelectedActor.id.ToString();
                        }
                    }
                    else if (m_SelectedCapabilityType != null)
                    {
                        GUILayout.Label(TruncateText(m_SelectedCapabilityType.Name, 20), Styles.TagStyle, GUILayout.Width(110));
                        if (GUILayout.Button("复制", EditorStyles.toolbarButton, GUILayout.Width(36)))
                        {
                            EditorGUIUtility.systemCopyBuffer = m_SelectedCapabilityType.FullName ?? m_SelectedCapabilityType.Name;
                        }
                    }

                    var stateColor = m_SelectedWorld.TimeScale > 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.8f, 0.2f);
                    var oldColor = GUI.color;
                    GUI.color = stateColor;
                    GUILayout.Label($"TimeScale: {m_SelectedWorld.TimeScale:F1}", Styles.TagStyle, GUILayout.Width(90));
                    GUI.color = oldColor;
                }
            }
            catch (Exception ex)
            {
                GUILayout.Label($"Error: {ex.Message}", EditorStyles.miniLabel, GUILayout.Width(200));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawMainLayout()
        {
            var remainingHeight = position.height - TOOLBAR_HEIGHT;
            
            var leftRect = new Rect(0, TOOLBAR_HEIGHT, LEFT_PANEL_WIDTH, remainingHeight);
            DrawWorldsPanel(leftRect);
            
            DrawVerticalSeparator(LEFT_PANEL_WIDTH, TOOLBAR_HEIGHT, remainingHeight);
            
            var middleWidth = position.width - LEFT_PANEL_WIDTH - RIGHT_PANEL_WIDTH;
            var middleRect = new Rect(LEFT_PANEL_WIDTH + SEPARATOR_THICKNESS, TOOLBAR_HEIGHT, 
                middleWidth - SEPARATOR_THICKNESS, remainingHeight);
            DrawContentPanel(middleRect);
            
            DrawVerticalSeparator(position.width - RIGHT_PANEL_WIDTH, TOOLBAR_HEIGHT, remainingHeight);
            
            var rightRect = new Rect(position.width - RIGHT_PANEL_WIDTH + SEPARATOR_THICKNESS, 
                TOOLBAR_HEIGHT, RIGHT_PANEL_WIDTH - SEPARATOR_THICKNESS, remainingHeight);
            DrawDetailsPanel(rightRect);
        }

        private void DrawVerticalSeparator(float x, float y, float height)
        {
            var separatorColor = EditorGUIUtility.isProSkin 
                ? new Color(0.15f, 0.15f, 0.15f) 
                : new Color(0.7f, 0.7f, 0.7f);
            
            EditorGUI.DrawRect(new Rect(x, y, SEPARATOR_THICKNESS, height), separatorColor);
        }
        
        #endregion

        #region 左面板 - Worlds列表
        
        private void DrawWorldsPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            
            EditorGUILayout.LabelField("Worlds", Styles.HeaderStyle);
            
            List<World> worlds = null;
            try
            {
                worlds = Game.Worlds?.ToList() ?? new List<World>();
            }
            catch
            {
                worlds = new List<World>();
            }

            if (worlds.Count == 0)
            {
                EditorGUILayout.HelpBox("没有活跃的World", MessageType.Info);
                GUILayout.EndArea();
                return;
            }

            m_LeftScrollPos = EditorGUILayout.BeginScrollView(m_LeftScrollPos, 
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            foreach (var world in worlds)
            {
                if (world == null || world.IsDisposed) continue;

                bool isSelected = m_SelectedWorld == world;
                DrawWorldItem(world, isSelected);
            }

            EditorGUILayout.EndScrollView();
            
            GUILayout.EndArea();
        }

        private void DrawWorldItem(World world, bool isSelected)
        {
            var itemRect = EditorGUILayout.GetControlRect(false, ITEM_HEIGHT);
            
            bool isHovered = itemRect.Contains(Event.current.mousePosition);
            
            if (isSelected)
            {
                GUI.Box(itemRect, "", Styles.HighlightStyle);
            }
            else if (isHovered)
            {
                GUI.Box(itemRect, "", Styles.HoveredItemStyle);
            }

            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                SelectWorld(world);
                Event.current.Use();
            }

            var contentRect = new Rect(itemRect.x + PADDING, itemRect.y, 
                itemRect.width - PADDING * 2, itemRect.height);
            
            EditorGUI.LabelField(
                new Rect(contentRect.x, contentRect.y, contentRect.width - 80, contentRect.height),
                new GUIContent(TruncateText(world.Name, 20), EditorGUIUtility.IconContent("d_SceneAsset Icon").image),
                isSelected ? Styles.HighlightStyle : (isHovered ? Styles.HoveredItemStyle : Styles.ItemStyle));
            
            var stateRect = new Rect(contentRect.xMax - 75, contentRect.y, 70, contentRect.height);
            var stateColor = world.TimeScale > 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.8f, 0.2f);
            var oldColor = GUI.color;
            GUI.color = stateColor;
            GUI.Label(stateRect, (world.TimeScale > 0 && world == Game.World) ? "运行中" : "已暂停", Styles.MiniTagStyle);
            GUI.color = oldColor;
        }
        
        #endregion

        #region 中面板 - Actors/Capabilities列表
        
        private void DrawContentPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            
            if (m_SelectedWorld == null || m_SelectedWorld.IsDisposed || !IsWorldValid(m_SelectedWorld))
            {
                EditorGUILayout.HelpBox("选择一个World开始调试", MessageType.Info);
                GUILayout.EndArea();
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            m_CurrentViewMode = GUILayout.Toggle(
                m_CurrentViewMode == ViewMode.Actors,
                $"Actors ({m_SelectedWorld.Actors?.AliveActorCount ?? 0})",
                EditorStyles.toolbarButton) ? ViewMode.Actors : m_CurrentViewMode;

            var capCount = GetTotalCapabilityCount();
            m_CurrentViewMode = GUILayout.Toggle(
                m_CurrentViewMode == ViewMode.Capabilities,
                $"Capabilities ({capCount})",
                EditorStyles.toolbarButton) ? ViewMode.Capabilities : m_CurrentViewMode;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("SearchField");
            m_SearchText = EditorGUILayout.TextField(m_SearchText, Styles.SearchFieldStyle, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(50)) && 
                !string.IsNullOrEmpty(m_SearchText))
            {
                m_SearchText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            m_MiddleScrollPos = EditorGUILayout.BeginScrollView(
                m_MiddleScrollPos,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            if (m_CurrentViewMode == ViewMode.Actors)
                DrawActorsList();
            else
                DrawCapabilitiesList();

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawActorsList()
        {
            try
            {
                var actors = GetAliveActors();
                if (actors == null || actors.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有Actor", MessageType.Info);
                    return;
                }

                var filteredActors = string.IsNullOrEmpty(m_SearchText)
                    ? actors
                    : actors.Where(a => a.ToString().IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                if (filteredActors.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到匹配的Actor", MessageType.Info);
                    return;
                }

                foreach (var actor in filteredActors)
                {
                    bool isSelected = m_SelectedActor.Equals(actor);
                    bool isHovered = actor.Equals(m_HoveredActor);
                    DrawActorItem(actor, isSelected, isHovered);
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"获取Actor列表失败: {ex.Message}", MessageType.Error);
            }
        }

        private void DrawActorItem(Actor actor, bool isSelected, bool isHovered)
        {
            var itemRect = EditorGUILayout.GetControlRect(false, ITEM_HEIGHT);
            
            bool mouseOver = itemRect.Contains(Event.current.mousePosition);
            if (mouseOver)
            {
                m_HoveredActor = actor;
                if (Event.current.type == EventType.MouseMove)
                {
                    Repaint();
                }
            }
            
            if (isSelected)
            {
                GUI.Box(itemRect, "", Styles.HighlightStyle);
            }
            else if (isHovered)
            {
                GUI.Box(itemRect, "", Styles.HoveredItemStyle);
            }

            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                SelectActor(actor);
                Event.current.Use();
            }

            var contentRect = new Rect(itemRect.x + PADDING, itemRect.y, 
                itemRect.width - PADDING * 2, itemRect.height);
            
            var infoRect = new Rect(contentRect.x, contentRect.y, 
                contentRect.width - 90, contentRect.height);
            
            GUI.Label(infoRect, TruncateText($"Actor #{actor.id}", 25), 
                isSelected ? Styles.HighlightStyle : (isHovered ? Styles.HoveredItemStyle : Styles.ItemStyle));
            
            var countRect = new Rect(contentRect.xMax - 85, contentRect.y, 80, contentRect.height);
            try
            {
                var componentMask = GetComponentMask(actor);
                var componentCount = CountComponents(componentMask);
                GUI.Label(countRect, $"{componentCount} 组件", Styles.MiniTagStyle);
            }
            catch
            {
                GUI.Label(countRect, "N/A", Styles.MiniTagStyle);
            }
        }

        private void DrawCapabilitiesList()
        {
            try
            {
                var caps = GetAllCapabilityTypesWithCounts();
                if (caps == null || caps.Count == 0)
                {
                    EditorGUILayout.HelpBox("没有Capability", MessageType.Info);
                    return;
                }

                var filtered = string.IsNullOrEmpty(m_SearchText)
                    ? caps
                    : caps.Where(kvp => kvp.Key.Name.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                          .ToDictionary(k => k.Key, v => v.Value);

                if (filtered.Count == 0)
                {
                    EditorGUILayout.HelpBox("未找到匹配的Capability", MessageType.Info);
                    return;
                }

                foreach (var kvp in filtered.OrderBy(kvp => kvp.Key.Name))
                {
                    bool isSelected = m_SelectedCapabilityType == kvp.Key;
                    bool isHovered = m_HoveredCapabilityType == kvp.Key;
                    DrawCapabilityItem(kvp.Key, kvp.Value.total, kvp.Value.active, isSelected, isHovered);
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"获取Capability列表失败: {ex.Message}", MessageType.Error);
            }
        }

        private void DrawCapabilityItem(Type capType, int total, int active, bool isSelected, bool isHovered)
        {
            var itemRect = EditorGUILayout.GetControlRect(false, ITEM_HEIGHT);
            
            bool mouseOver = itemRect.Contains(Event.current.mousePosition);
            if (mouseOver)
            {
                m_HoveredCapabilityType = capType;
                if (Event.current.type == EventType.MouseMove)
                {
                    Repaint();
                }
            }
            
            if (isSelected)
            {
                GUI.Box(itemRect, "", Styles.HighlightStyle);
            }
            else if (isHovered)
            {
                GUI.Box(itemRect, "", Styles.HoveredItemStyle);
            }

            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                SelectCapabilityType(capType);
                Event.current.Use();
            }

            var contentRect = new Rect(itemRect.x + PADDING, itemRect.y, 
                itemRect.width - PADDING * 2, itemRect.height);
            
            var nameRect = new Rect(contentRect.x, contentRect.y, 
                contentRect.width - 130, contentRect.height);
            GUI.Label(nameRect, TruncateText(capType.Name, 30), 
                isSelected ? Styles.HighlightStyle : (isHovered ? Styles.HoveredItemStyle : Styles.ItemStyle));
            
            var activeRect = new Rect(contentRect.xMax - 125, contentRect.y, 60, contentRect.height);
            var stateColor = active > 0 ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
            var oldColor = GUI.color;
            GUI.color = stateColor;
            GUI.Label(activeRect, $"活跃:{active}", Styles.MiniTagStyle);
            GUI.color = oldColor;
            
            var totalRect = new Rect(contentRect.xMax - 60, contentRect.y, 55, contentRect.height);
            GUI.Label(totalRect, $"总:{total}", Styles.MiniTagStyle);
        }

        #endregion

        #region 右面板 - 详细信息
        
        private void DrawDetailsPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            
            if (m_SelectedWorld == null || m_SelectedWorld.IsDisposed || !IsWorldValid(m_SelectedWorld))
            {
                EditorGUILayout.HelpBox("请选择一个World", MessageType.Info);
                GUILayout.EndArea();
                return;
            }

            m_RightScrollPos = EditorGUILayout.BeginScrollView(m_RightScrollPos, 
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (!m_SelectedActor.IsNone && m_SelectedWorld.IsActorAlive(m_SelectedActor))
            {
                DrawActorDetails();
            }
            else if (m_SelectedCapabilityType != null)
            {
                DrawCapabilityDetails();
            }
            else
            {
                DrawWorldDetails();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawWorldDetails()
        {
            EditorGUILayout.LabelField("World 详情", Styles.HeaderStyle);
            
            EditorGUILayout.BeginVertical(Styles.CardStyle);
            
            DrawDetailRow("名称", m_SelectedWorld.Name);
            DrawDetailRow("时间缩放", m_SelectedWorld.TimeScale.ToString("F2"));
            DrawDetailRow("Actor数量", m_SelectedWorld.Actors?.AliveActorCount.ToString() ?? "0");
            DrawDetailRow("Capability总数", GetTotalCapabilityCount().ToString());
            DrawDetailRow("空闲Actor", m_SelectedWorld.Actors.FreeActorCount.ToString());
            
            EditorGUILayout.EndVertical();
        }

        private void DrawActorDetails()
        {
            EditorGUILayout.LabelField("Actor 详情", Styles.HeaderStyle);
            
            EditorGUILayout.BeginVertical(Styles.CardStyle);
            DrawDetailRow("ID", m_SelectedActor.id.ToString());
            DrawDetailRow("索引", m_SelectedActor.Index.ToString());
            DrawDetailRow("版本", m_SelectedActor.Version.ToString());
            DrawDetailRow("状态", m_SelectedWorld.IsActorAlive(m_SelectedActor) ? "存活" : "已销毁");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            
            m_ShowComponentDetails = EditorGUILayout.Foldout(m_ShowComponentDetails, "组件详情", true);
            if (m_ShowComponentDetails)
            {
                var components = GetActorComponents(m_SelectedActor);
                if (components.Count == 0)
                {
                    EditorGUILayout.HelpBox("无组件", MessageType.Info);
                }
                else
                {
                    foreach (var kvp in components)
                    {
                        DrawComponentDetails(kvp.Key, kvp.Value);
                    }
                }
            }
            
            EditorGUILayout.Space(10);
            m_ShowCapabilityDetails = EditorGUILayout.Foldout(m_ShowCapabilityDetails, "能力详情", true);
            if (m_ShowCapabilityDetails)
            {
                var caps = GetCapabilitiesForActor(m_SelectedActor);
                if (caps == null || caps.Count == 0)
                {
                    EditorGUILayout.HelpBox("无能力", MessageType.Info);
                }
                else
                {
                    var uniqueCaps = new List<(Capability capability, string tickGroup, string tickOrder)>(caps.Count);
                    var seenCaps = new HashSet<Capability>();

                    foreach (var ci in caps)
                    {
                        try
                        {
                            var ciType = ci.GetType();
                            var capField = ciType.GetField("capability", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            var tickGroupField = ciType.GetField("tickGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            var tickOrderField = ciType.GetField("tickOrder", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            var capObj = capField?.GetValue(ci) as Capability;
                            if (capObj == null) continue;
                            if (!seenCaps.Add(capObj)) continue;

                            var tg = tickGroupField?.GetValue(ci)?.ToString() ?? "N/A";
                            var to = tickOrderField?.GetValue(ci)?.ToString() ?? "N/A";
                            uniqueCaps.Add((capObj, tg, to));
                        }
                        catch { }
                    }

                    foreach (var group in uniqueCaps.GroupBy(c => c.capability.GetType()).OrderBy(g => g.Key.Name))
                    {
                        var capType = group.Key;
                        int total = group.Count();
                        int active = group.Count(x => x.capability.IsActive);

                        EditorGUILayout.BeginVertical(Styles.CardStyle);
                        if (total == 1)
                        {
                            var item = group.First();
                            var state = item.capability.IsActive ? "活跃" : "未激活";
                            EditorGUILayout.LabelField($"{capType.Name} ({state})", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"TickGroup: {item.tickGroup}", Styles.PropertyStyle);
                            EditorGUILayout.LabelField($"TickOrder: {item.tickOrder}", Styles.PropertyStyle);
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"{capType.Name} (活跃 {active}/{total})", EditorStyles.boldLabel);
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
            }
        }

        private void DrawComponentDetails(Type componentType, object componentValue)
        {
            EditorGUILayout.BeginVertical(Styles.CardStyle);
            
            EditorGUILayout.LabelField(componentType.Name, EditorStyles.boldLabel);
            
            if (componentValue == null)
            {
                EditorGUILayout.LabelField("组件值为null");
            }
            else
            {
                try
                {
                    string valueString = componentValue.ToString();
                    EditorGUILayout.LabelField(valueString, Styles.LabelStyle);
                }
                catch
                {
                    EditorGUILayout.LabelField("无法获取组件值");
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawCapabilityDetails()
        {
            EditorGUILayout.LabelField("Capability 详情", Styles.HeaderStyle);
            
            EditorGUILayout.BeginVertical(Styles.CardStyle);
            
            var type = m_SelectedCapabilityType;
            DrawDetailRow("类型", type.Name);
            DrawDetailRow("命名空间", type.Namespace ?? "<无>");
            var counts = GetAllCapabilityTypesWithCounts();
            if (counts.TryGetValue(type, out var c))
            {
                DrawDetailRow("活跃数量", c.active.ToString());
                DrawDetailRow("总数量", c.total.ToString());
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawDetailRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, Styles.PropertyStyle, GUILayout.Width(DETAIL_LABEL_WIDTH));
            EditorGUILayout.LabelField(TruncateText(value, 50), Styles.PropertyStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion

        #region 工具方法
        
        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 3) + "...";
        }

        private void ClearSelection()
        {
            m_SelectedActor = default;
            m_SelectedCapabilityType = null;
            m_HoveredActor = default;
            m_HoveredCapabilityType = null;
        }

        private void SelectWorld(World world)
        {
            if (m_SelectedWorld != world)
            {
                m_SelectedWorld = world;
                ClearSelection();
                Repaint();
            }
        }

        private void SelectActor(Actor actor)
        {
            if (!m_SelectedActor.Equals(actor))
            {
                m_SelectedActor = actor;
                m_SelectedCapabilityType = null;
                m_HoveredActor = actor;
                m_HoveredCapabilityType = null;
                Repaint();
            }
        }

        private void SelectCapabilityType(Type capType)
        {
            if (m_SelectedCapabilityType != capType)
            {
                m_SelectedCapabilityType = capType;
                m_SelectedActor = default;
                m_HoveredCapabilityType = capType;
                m_HoveredActor = default;
                Repaint();
            }
        }
        
        #endregion

        #region 反射辅助方法
        
        private static void InitializeReflection()
        {
            if (s_ReflectionInitialized) return;

            try
            {
                var actorManagerType = typeof(ActorManager);
                
                s_ActorsField = actorManagerType.GetField("m_Actors", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                s_GetComponentMaskMethod = actorManagerType.GetMethod("GetComponentMask", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                s_GetCapabilitiesMethod = actorManagerType.GetMethod("GetCapabilities",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                var actorsFieldType = s_ActorsField?.FieldType;
                var actorDataType = actorsFieldType != null ? actorsFieldType.GetElementType() : null;
                if (actorDataType != null)
                {
                    s_ActorDataIsAliveField = actorDataType.GetField("isAlive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    s_ActorDataVersionField = actorDataType.GetField("version", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }

                s_ReflectionInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WorldDebugger反射初始化失败: {ex.Message}");
            }
        }

        private ComponentMask GetComponentMask(Actor actor)
        {
            if (m_SelectedWorld == null || s_GetComponentMaskMethod == null)
                return default;

            try
            {
                return (ComponentMask)s_GetComponentMaskMethod.Invoke(
                    m_SelectedWorld.Actors, new object[] { actor });
            }
            catch
            {
                return default;
            }
        }

        private int CountComponents(ComponentMask mask)
        {
            int count = 0;
            foreach (var _ in mask)
                count++;
            return count;
        }

        private Dictionary<Type, object> GetActorComponents(Actor actor)
        {
            var result = new Dictionary<Type, object>();
            
            if (m_SelectedWorld == null)
                return result;

            try
            {
                var componentMask = GetComponentMask(actor);
                
                foreach (var typeId in componentMask)
                {
                    try
                    {
                        var componentType = GetComponentType(typeId);
                        if (componentType == null) continue;

                        var method = typeof(World).GetMethod("TryGetComponent");
                        var genericMethod = method.MakeGenericMethod(componentType);
                        var parameters = new object[] { actor, null };
                        var success = (bool)genericMethod.Invoke(m_SelectedWorld, parameters);
                        
                        if (success && parameters[1] != null)
                        {
                            result[componentType] = parameters[1];
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return result;
        }

        private Type GetComponentType(int typeId)
        {
            try
            {
                var componentTypeRegistryType = typeof(ComponentTypeId).Assembly
                    .GetType("Verve.ComponentTypeRegistry");
                if (componentTypeRegistryType != null)
                {
                    var getTypeMethod = componentTypeRegistryType.GetMethod("GetType", 
                        BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
                    
                    if (getTypeMethod != null)
                    {
                        return getTypeMethod.Invoke(null, new object[] { typeId }) as Type;
                    }
                }
            }
            catch { }

            return null;
        }

        private List<Actor> GetAliveActors()
        {
            var result = new List<Actor>();
            try
            {
                var actorsArray = s_ActorsField?.GetValue(m_SelectedWorld.Actors) as Array;
                if (actorsArray == null || s_ActorDataIsAliveField == null || s_ActorDataVersionField == null)
                    return result;

                int length = actorsArray.Length;
                for (int i = 0; i < length; i++)
                {
                    var actorDataObj = actorsArray.GetValue(i);
                    if (actorDataObj == null) continue;
                    var alive = (bool)(s_ActorDataIsAliveField.GetValue(actorDataObj) ?? false);
                    if (!alive) continue;
                    int version = (int)(s_ActorDataVersionField.GetValue(actorDataObj) ?? 0);
                    if (version <= 0) continue;
                    result.Add(new Actor(i, version));
                }
            }
            catch { }
            return result;
        }

        private List<object> GetCapabilitiesForActor(Actor actor)
        {
            if (m_SelectedWorld == null || s_GetCapabilitiesMethod == null) return null;
            try
            {
                if (s_GetCapabilitiesMethod.Invoke(m_SelectedWorld.Actors, new object[] { actor }) is not IEnumerable listObj) return null;
                var result = new List<object>();
                foreach (var item in listObj) result.Add(item);
                return result;
            }
            catch { return null; }
        }

        private int GetTotalCapabilityCount()
        {
            int total = 0;
            try
            {
                var actorsArray = s_ActorsField?.GetValue(m_SelectedWorld.Actors) as Array;
                if (actorsArray == null) return 0;
                for (int i = 0; i < actorsArray.Length; i++)
                {
                    var actorDataObj = actorsArray.GetValue(i);
                    if (actorDataObj == null) continue;
                    var dataType = actorDataObj.GetType();
                    var isAliveField = dataType.GetField("isAlive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var capabilitiesField = dataType.GetField("capabilities", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (isAliveField == null || capabilitiesField == null) continue;
                    var alive = (bool)isAliveField.GetValue(actorDataObj);
                    if (!alive) continue;
                    if (capabilitiesField.GetValue(actorDataObj) is ICollection list) total += list.Count;
                }
            }
            catch { }
            return total;
        }

        private Dictionary<Type, (int total, int active)> GetAllCapabilityTypesWithCounts()
        {
            var dict = new Dictionary<Type, (int total, int active)>();
            try
            {
                var actorsArray = s_ActorsField?.GetValue(m_SelectedWorld.Actors) as Array;
                if (actorsArray == null) return dict;
                for (int i = 0; i < actorsArray.Length; i++)
                {
                    var actorDataObj = actorsArray.GetValue(i);
                    if (actorDataObj == null) continue;
                    var dataType = actorDataObj.GetType();
                    var isAliveField = dataType.GetField("isAlive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var capabilitiesField = dataType.GetField("capabilities", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (isAliveField == null || capabilitiesField == null) continue;
                    var alive = (bool)isAliveField.GetValue(actorDataObj);
                    if (!alive) continue;
                    var list = capabilitiesField.GetValue(actorDataObj) as System.Collections.IEnumerable;
                    if (list == null) continue;
                    foreach (var item in list)
                    {
                        var itemType = item.GetType();
                        var capField = itemType.GetField("capability", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        var cap = capField?.GetValue(item) as Capability;
                        var type = cap?.GetType();
                        if (type == null) continue;
                        var entry = dict.TryGetValue(type, out var v) ? v : (0, 0);
                        entry.Item1++;
                        if (cap.IsActive) entry.Item2++;
                        dict[type] = entry;
                    }
                }
            }
            catch { }
            return dict;
        }

        private List<Type> GetAllCapabilityTypes()
        {
            try
            {
                var dict = GetAllCapabilityTypesWithCounts();
                return dict.Keys.ToList();
            }
            catch { return new List<Type>(); }
        }
        
        #endregion
    }
}

#endif