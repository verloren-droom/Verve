// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

#if UNITY_EDITOR

namespace Verve.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    ///   <para>ACC 能力事件时间轴。</para>
    /// </summary>
    internal sealed partial class ACCTemporalLoggerWindow : EditorWindow
    {
        private const double RefreshInterval = 0.1d;
        private const double MinDuration = 0.01d;
        private const double MaxDuration = 86400d;
        private const float RulerHeight = 32f;
        private const float ScrollbarWidth = 14f;
        private const float DefaultTrackWidth = 260f;
        private const float ToolbarHeight = 22f;
        private const float ControlHeight = ToolbarHeight - 1f;
        private const float IconSize = 16f;

        [SerializeField] private bool m_RecordingEnabled = true;
        [SerializeField] private bool m_ShowSpans = true;
        [SerializeField] private bool m_TallRows;
        [SerializeField] private bool m_RelativeTime = true;
        [SerializeField] private float m_TrackWidth = DefaultTrackWidth;
        [SerializeField] private string m_Search = string.Empty;
        [SerializeField] private int m_KindMask = ACCTemporalTimeline.AllKinds;
        [SerializeField] private double m_ViewStart;
        [SerializeField] private double m_ViewDuration = 10d;
        [NonSerialized] private double m_ObservedEnd;

        private readonly ACCTemporalTimeline m_Timeline = new();
        private readonly List<GameModulesHandle> m_ModuleHandles = new(4);
        private readonly List<World> m_Worlds = new(8);
        private readonly List<World> m_WorldBuffer = new(8);
        private readonly List<CapabilityDebugEvent> m_EventBuffer = new(256);
        private readonly HashSet<Actor> m_CollapsedActors = new();
        private readonly List<Label> m_RulerLabels = new(32);
        private readonly List<VisualElement> m_RowHeaders = new();
        private readonly List<VisualElement> m_RuntimeControls = new(9);

        private DropdownField m_WorldField;
        private ToolbarToggle m_RecordToggle;
        private ToolbarToggle m_PlayToggle;
        private Image m_PlayIcon;
        private ToolbarButton m_FilterMenu;
        private Label m_FilterLabel;
        private ToolbarSearchField m_SearchField;
        private DoubleField m_TimeField;
        private Label m_StatusLabel;
        private Label m_EmptyLabel;
        private Label m_TimeBadge;
        private ACCComponentView m_ComponentView;
        private VisualElement m_TrackHeader;
        private VisualElement m_TrackRows;
        private VisualElement m_Ruler;
        private VisualElement m_Canvas;
        private VisualElement m_Splitter;
        private VisualElement m_TimelineView;
        private VisualElement m_TimelinePane;
        private TwoPaneSplitView m_ContentSplit;
        private ScrollView m_TrackScroll;
        private Scroller m_TimeScroller;

        // World 可序列化，但只读调试服务不会随 EditorWindow 热重载恢复。
        // 运行时引用必须重新从 Game 的模块注册表获取，禁止保存成窗口状态。
        [NonSerialized] private World m_SelectedWorld;
        [NonSerialized] private long m_SelectedSequence = -1;
        [NonSerialized] private ACCTemporalTimeline.Track m_SelectedTrack;
        [NonSerialized] private Actor m_SelectedActor = Actor.none;
        [NonSerialized] private double m_Playhead;
        [NonSerialized] private double m_TimeOrigin;
        private double m_DataStart;
        private double m_DataEnd = 10d;
        private double m_NextRefresh;
        private double m_NextWorldRefresh;
        private double m_LastUpdate;
        private bool m_Playing;
        [NonSerialized] private bool m_FollowLatest = true;
        private bool m_ComponentsVisible;
        [NonSerialized] private bool m_HasOrigin;
        private bool m_UpdatingScroller;
        private bool m_DarkSkin;
        private bool m_Initialized;
        [NonSerialized] private bool m_PlayModeTransition;
        private int m_CapturedPointer = -1;
        private VisualElement m_DragTarget;
        private Vector2 m_DragPosition;
        private double m_DragViewStart;
        private double m_DragDuration;
        private float m_DragScroll;
        private float m_DragTrackWidth;
        private DragMode m_DragMode;

        private enum DragMode { None, Scrub, Pan, Zoom, Resize }

        private float RowHeight => m_TallRows ? 36f : 22f;
        private float CanvasWidth => !Game.NumberUtility.IsFinite(m_Canvas.resolvedStyle.width)
            ? 1f : Mathf.Max(1f, m_Canvas.resolvedStyle.width);
        private double TimeOffset => m_RelativeTime && m_HasOrigin ? m_TimeOrigin : 0d;
        private bool HasWorld => m_SelectedWorld != null && !m_SelectedWorld.IsDisposed && m_SelectedWorld.DebugTrace != null;
        private bool IsPlaybackActive => m_Playing || (m_FollowLatest && m_Timeline.events.Count > 0);
        private bool IsObserving => HasWorld && m_SelectedWorld.DebugTrace.IsEnabled &&
            EditorApplication.isPlaying && !EditorApplication.isPaused;

        /// <summary>
        ///   <para>打开时间记录器；入口同时供 ACC 工具栏调用。</para>
        /// </summary>
        [MenuItem("Window/Verve/ACC 时间记录器")]
        public static void Open()
        {
            var window = GetWindow<ACCTemporalLoggerWindow>();
            window.titleContent = new GUIContent("ACC 时间记录器", EditorGUIUtility.IconContent("Animation.EventMarker").image);
            window.minSize = new Vector2(760f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            m_Initialized = false;
            minSize = new Vector2(760f, 420f);
            // CreateGUI 可能因布局或主题切换重复调用；订阅归生命周期管理。
            EditorApplication.update -= UpdateWindow;
            EditorApplication.update += UpdateWindow;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            m_LastUpdate = EditorApplication.timeSinceStartup;
            m_NextRefresh = m_NextWorldRefresh = 0d;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateWindow;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            ReleaseRecording();
            m_SelectedWorld = null;
            m_SelectedActor = Actor.none;
            m_ComponentView?.Refresh(null, Actor.none);
            StopDrag();
            m_Playing = false;
            m_Initialized = false;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            m_NextWorldRefresh = 0d;
            m_PlayModeTransition = state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode;
            if (!m_Initialized) return;
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                StopDrag();
                ReleaseRecording();
                m_SelectedWorld = null;
                ResetViewState();
            }
            RefreshWorlds();
            UpdateStatus();
        }

        private void CreateGUI()
        {
            StopDrag();
            m_DarkSkin = EditorGUIUtility.isProSkin;
            m_TrackWidth = Mathf.Clamp(m_TrackWidth, 180f, Mathf.Max(760f, position.width) - 320f);
            m_ViewDuration = ClampDuration(m_ViewDuration);
            m_RulerLabels.Clear();
            m_RowHeaders.Clear();
            rootVisualElement.Clear();
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(System.IO.Path.ChangeExtension(
                AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)), ".uss"));
            if (stylesheet != null && !rootVisualElement.styleSheets.Contains(stylesheet))
                rootVisualElement.styleSheets.Add(stylesheet);
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.backgroundColor = WindowColor;
            rootVisualElement.style.color = TextColor;
            rootVisualElement.focusable = true;
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            BuildToolbar();
            BuildTimelineView();
            BuildFooter();
            m_Initialized = true;
            if (!EditorApplication.isPlaying) ResetViewState();
            RefreshWorlds();
            if (HasWorld) m_SelectedWorld.DebugTrace.SetOwnerEnabled(this, m_RecordingEnabled);
            UpdateFileLogging();
            RefreshEvents();
            RebuildRows();
            UpdateView();
        }

        private void BuildToolbar()
        {
            var toolbar = new Toolbar { name = "acc-transport" };
            toolbar.style.height = ToolbarHeight;
            toolbar.style.flexShrink = 0f;
            m_WorldField = new DropdownField { name = "acc-world", tooltip = "运行中的 ACC 世界" };
            m_WorldField.style.width = m_TrackWidth;
            m_WorldField.style.flexShrink = 0f;
            m_WorldField.RegisterValueChangedCallback(evt =>
            {
                var index = m_WorldField.choices.IndexOf(evt.newValue);
                if (index >= 0 && index < m_Worlds.Count) SelectWorld(m_Worlds[index]);
            });
            toolbar.Add(m_WorldField);
            m_RecordToggle = CreateIconToggle("Animation.Record", "启用 / 禁用事件记录（默认启用）", m_RecordingEnabled, SetRecordingEnabled);
            m_RecordToggle.name = "acc-record";
            m_RecordToggle.style.color = new Color(0.9f, 0.25f, 0.25f);
            toolbar.Add(m_RecordToggle);
            toolbar.Add(CreateIconButton("Animation.FirstKey", "首个事件（Home）", () => NavigateEvent(-2)));
            toolbar.Add(CreateIconButton("Animation.PrevKey", "上一个事件（←）", () => NavigateEvent(-1)));
            m_PlayToggle = CreateIconToggle(IsPlaybackActive ? "PauseButton" : "Animation.Play",
                "播放 / 暂停（空格）；最新位置跟随实时进度，历史位置预览记录；End 返回实时", IsPlaybackActive, SetPlaying);
            m_PlayIcon = m_PlayToggle.Q<Image>();
            m_PlayToggle.name = "acc-preview";
            toolbar.Add(m_PlayToggle);
            toolbar.Add(CreateIconButton("Animation.NextKey", "下一个事件（→）", () => NavigateEvent(1)));
            toolbar.Add(CreateIconButton("Animation.LastKey", "最新记录 / 恢复跟随（End）", () => NavigateEvent(2)));
            m_TimeField = new DoubleField { name = "acc-time", isDelayed = true };
            m_TimeField.style.width = 82f;
            m_TimeField.tooltip = "播放头时间（秒）；使用当前显示的相对 / 绝对时间";
            m_TimeField.formatString = "0.000";
            m_TimeField.RegisterValueChangedCallback(evt =>
            {
                if (!Game.NumberUtility.IsFinite(evt.newValue))
                {
                    m_TimeField.SetValueWithoutNotify(m_Playhead - TimeOffset);
                    return;
                }
                SetPlaying(false);
                SetPlayhead(evt.newValue + TimeOffset, true);
            });
            toolbar.Add(m_TimeField);
            toolbar.Add(new Label("s") { tooltip = "秒；悬停事件查看采样帧号和原因" });
            m_RuntimeControls.Clear();
            foreach (var control in toolbar.Children())
                if (control != m_RecordToggle) m_RuntimeControls.Add(control);
            toolbar.Add(new ToolbarSpacer { flex = true });
            toolbar.Add(CreateIconButton("TreeEditor.Trash", "清空当前世界的事件记录", ClearEvents));
            var options = new ToolbarMenu { name = "acc-options", tooltip = "选项" };
            options.style.width = 26f;
            options.Q(className: ToolbarMenu.textUssClassName).style.display = DisplayStyle.None;
            options.Q(className: ToolbarMenu.arrowUssClassName).style.display = DisplayStyle.None;
            options.Add(CreateToolbarIcon("_Popup"));
            options.menu.AppendAction("显示激活区间", _ =>
            {
                m_ShowSpans = !m_ShowSpans;
                m_Canvas.MarkDirtyRepaint();
            }, _ => Checked(m_ShowSpans));
            options.menu.AppendAction("高轨道", _ =>
            {
                m_TallRows = !m_TallRows;
                RebuildRows();
            }, _ => Checked(m_TallRows));
            options.menu.AppendAction("相对时间", _ =>
            {
                m_RelativeTime = !m_RelativeTime;
                UpdateView();
            }, _ => Checked(m_RelativeTime));
            options.menu.AppendSeparator();
            options.menu.AppendAction("写入日志文件（Logs）", _ =>
            {
                m_WriteLog = !m_WriteLog;
                UpdateFileLogging();
            }, _ => Checked(m_WriteLog));
            options.menu.AppendSeparator();
            options.menu.AppendAction("展开所有轨道", _ =>
            {
                m_CollapsedActors.Clear();
                RebuildRows();
            });
            options.menu.AppendAction("折叠所有轨道", _ =>
            {
                foreach (var group in m_Timeline.groups) m_CollapsedActors.Add(group.actor);
                RebuildRows();
            });
            options.menu.AppendSeparator();
            options.menu.AppendAction("复制事件详情", _ => CopySelectedEvent(),
                _ => m_Timeline.FindSequence(m_SelectedSequence) >= 0
                    ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            options.menu.AppendAction("复制可见事件（TSV）", _ => CopyVisibleEvents(),
                _ => m_Timeline.visibleEvents.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            toolbar.Add(options);
            NormalizeToolbar(toolbar);
            rootVisualElement.Add(toolbar);

            var filters = new Toolbar { name = "acc-filters" };
            filters.style.height = ToolbarHeight;
            filters.style.flexShrink = 0f;
            m_SearchField = new ToolbarSearchField { name = "acc-search" };
            m_SearchField.style.width = m_TrackWidth;
            m_SearchField.tooltip = "筛选能力完整类型名、Actor 或异常 / 阻塞说明";
            m_SearchField.SetValueWithoutNotify(m_Search);
            m_SearchField.RegisterValueChangedCallback(evt =>
            {
                m_Search = evt.newValue;
                RebuildRows();
            });
            filters.Add(m_SearchField);
            m_FilterMenu = CreateTextButton("acc-event-filter", "全部事件", ShowEventFilterMenu);
            m_FilterLabel = m_FilterMenu.Q<Label>();
            var filterArrow = new VisualElement { name = "acc-event-filter-arrow", pickingMode = PickingMode.Ignore };
            filterArrow.AddToClassList(ToolbarMenu.arrowUssClassName);
            filterArrow.style.width = filterArrow.style.height = 10f;
            filterArrow.style.flexShrink = 0f;
            filterArrow.style.alignSelf = Align.Center;
            filterArrow.style.marginLeft = 4f;
            filterArrow.style.marginRight = filterArrow.style.marginTop = filterArrow.style.marginBottom = 0f;
            m_FilterMenu.Add(filterArrow);
            filters.Add(m_FilterMenu);
            NormalizeToolbar(filters);
            m_FilterMenu.style.width = 100f;
            rootVisualElement.Add(filters);
        }

        private void ShowEventFilterMenu()
        {
            var menu = BuildEventFilterMenu();
            menu.DropDown(m_FilterMenu.worldBound, m_FilterMenu, DropdownMenuSizeMode.Content);
        }

        private GenericDropdownMenu BuildEventFilterMenu()
        {
            // 原生 UI Toolkit 菜单保留勾选和键盘导航；色标直接复用轨道配色。
            var menu = new GenericDropdownMenu();
            menu.AddItem("全部事件", m_KindMask == ACCTemporalTimeline.AllKinds,
                () => SetKindMask(ACCTemporalTimeline.AllKinds));
            var errors = (1 << (int)CapabilityDebugEventKind.Failed) | (1 << (int)CapabilityDebugEventKind.Blocked);
            menu.AddItem("仅异常与阻塞", m_KindMask == errors, () => SetKindMask(errors));
            menu.AddSeparator(string.Empty);
            foreach (CapabilityDebugEventKind kind in Enum.GetValues(typeof(CapabilityDebugEventKind)))
            {
                var value = kind;
                menu.AddItem(GetEventName(value), (m_KindMask & (1 << (int)value)) != 0,
                    () => SetKindMask(m_KindMask ^ (1 << (int)value)));
                var item = menu.contentContainer[menu.contentContainer.childCount - 1];
                var icon = new Label("◆") { name = "acc-event-icon-" + value, pickingMode = PickingMode.Ignore };
                icon.style.color = GetEventColor(value);
                icon.style.width = 14f;
                icon.style.flexShrink = 0f;
                icon.style.marginRight = 4f;
                icon.style.unityTextAlign = TextAnchor.MiddleCenter;
                item.Q(className: GenericDropdownMenu.itemContentUssClassName).Insert(1, icon);
            }
            return menu;
        }

        private void BuildTimelineView()
        {
            // 原生上下分栏负责组件区尺寸与布局持久化；左右轨道仍共享纵向滚动。
            m_ContentSplit = new TwoPaneSplitView(1, 180f, TwoPaneSplitViewOrientation.Vertical)
            {
                name = "acc-content-split", viewDataKey = "acc-content-split"
            };
            m_ContentSplit.style.flexGrow = 1f;
            m_ContentSplit.style.minHeight = 0f;
            m_TimelinePane = new VisualElement { name = "acc-timeline-pane" };
            m_TimelinePane.style.minHeight = 120f;
            m_ContentSplit.Add(m_TimelinePane);
            rootVisualElement.Add(m_ContentSplit);
            var timeline = new VisualElement { name = "acc-timeline-layout" };
            timeline.style.flexGrow = 1f;
            timeline.style.minHeight = 80f;
            timeline.style.overflow = Overflow.Hidden;
            m_TimelineView = new VisualElement { name = "acc-timeline" };
            m_TimelineView.style.flexGrow = 1f;
            m_TimelineView.style.minHeight = 0f;
            timeline.Add(m_TimelineView);
            var header = CreateHorizontal();
            header.style.height = RulerHeight;
            header.style.flexShrink = 0f;
            header.style.backgroundColor = HeaderColor;
            m_TrackHeader = new Label("Actor / 能力");
            m_TrackHeader.style.width = m_TrackWidth;
            m_TrackHeader.style.flexShrink = 0f;
            m_TrackHeader.style.paddingLeft = 10f;
            m_TrackHeader.style.unityTextAlign = TextAnchor.MiddleLeft;
            m_TrackHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_TrackHeader.style.borderBottomWidth = 1f;
            m_TrackHeader.style.borderBottomColor = BorderColor;
            header.Add(m_TrackHeader);
            m_Ruler = new VisualElement { name = "acc-ruler", focusable = true };
            m_Ruler.tooltip = "拖动定位 · 中键平移 · 滚轮缩放 · 空格预览 · ← / → 切换事件";
            m_Ruler.style.flexGrow = 1f;
            m_Ruler.style.overflow = Overflow.Hidden;
            m_Ruler.generateVisualContent += PaintRuler;
            RegisterTimelineInput(m_Ruler);
            header.Add(m_Ruler);
            var gutter = new VisualElement();
            gutter.style.width = ScrollbarWidth;
            gutter.style.flexShrink = 0f;
            header.Add(gutter);
            m_TimelineView.Add(header);

            // 单一纵向 ScrollView 同时承载表头和轨道，避免双滚动容器行高与滚动位置漂移。
            m_TrackScroll = new ScrollView(ScrollViewMode.Vertical) { name = "acc-tracks" };
            m_TrackScroll.style.flexGrow = 1f;
            m_TrackScroll.style.minHeight = 0f;
            m_TrackScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            m_TrackScroll.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            m_TrackScroll.verticalScroller.style.width = ScrollbarWidth;
            m_TrackScroll.verticalScroller.style.minWidth = ScrollbarWidth;
            m_TrackScroll.verticalScroller.style.marginLeft = 0f;
            m_TrackScroll.verticalScroller.style.marginRight = 0f;
            m_TrackScroll.contentContainer.style.flexDirection = FlexDirection.Row;
            m_TrackScroll.contentContainer.style.flexGrow = 1f;
            m_TrackScroll.contentContainer.style.minWidth = 0f;
            m_TrackScroll.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => UpdateTrackHeight());
            m_TrackScroll.verticalScroller.valueChanged += _ => m_Canvas.MarkDirtyRepaint();
            m_TrackRows = new VisualElement { name = "acc-track-headers" };
            m_TrackRows.style.width = m_TrackWidth;
            m_TrackRows.style.flexShrink = 0f;
            m_TrackRows.style.backgroundColor = HeaderColor;
            m_TrackScroll.Add(m_TrackRows);
            m_Canvas = new VisualElement { name = "acc-track-canvas", focusable = true };
            m_Canvas.style.flexGrow = 1f;
            m_Canvas.style.minWidth = 0f;
            m_Canvas.style.overflow = Overflow.Hidden;
            m_Canvas.generateVisualContent += PaintCanvas;
            m_Canvas.RegisterCallback<GeometryChangedEvent>(_ => UpdateView());
            RegisterTimelineInput(m_Canvas);
            m_Canvas.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
            m_TrackScroll.Add(m_Canvas);
            m_TimelineView.Add(m_TrackScroll);

            m_EmptyLabel = new Label { name = "acc-empty", pickingMode = PickingMode.Ignore };
            m_EmptyLabel.style.position = Position.Absolute;
            m_EmptyLabel.style.left = m_TrackWidth;
            m_EmptyLabel.style.right = ScrollbarWidth;
            m_EmptyLabel.style.top = RulerHeight;
            m_EmptyLabel.style.bottom = 0f;
            m_EmptyLabel.style.paddingLeft = m_EmptyLabel.style.paddingRight = 20f;
            m_EmptyLabel.style.whiteSpace = WhiteSpace.Normal;
            m_EmptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_EmptyLabel.style.color = MutedColor;
            m_TimelineView.Add(m_EmptyLabel);
            m_Splitter = new VisualElement { name = "acc-header-resizer" };
            m_Splitter.style.position = Position.Absolute;
            m_Splitter.style.left = m_TrackWidth - 2f;
            m_Splitter.style.top = 0f;
            m_Splitter.style.bottom = 0f;
            m_Splitter.style.width = 5f;
            m_Splitter.style.borderLeftWidth = 1f;
            m_Splitter.style.borderLeftColor = BorderColor;
            m_Splitter.tooltip = "拖动调整轨道列表宽度；双击恢复默认宽度";
            m_Splitter.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (evt.clickCount == 2) SetTrackWidth(DefaultTrackWidth);
                else BeginDrag(m_Splitter, evt, DragMode.Resize);
                evt.StopPropagation();
            });
            RegisterDragCallbacks(m_Splitter);
            timeline.Add(m_Splitter);
            m_TimelinePane.Add(timeline);
        }

        private void BuildFooter()
        {
            var navigation = CreateHorizontal();
            navigation.style.height = 20f;
            navigation.style.alignItems = Align.Center;
            navigation.style.flexShrink = 0f;
            navigation.style.backgroundColor = HeaderColor;
            navigation.style.borderTopWidth = 1f;
            navigation.style.borderTopColor = BorderColor;
            m_StatusLabel = new Label { name = "acc-status" };
            m_StatusLabel.style.width = m_TrackWidth;
            m_StatusLabel.style.flexShrink = 0f;
            m_StatusLabel.style.paddingLeft = 8f;
            m_StatusLabel.style.fontSize = 10f;
            m_StatusLabel.style.color = MutedColor;
            m_StatusLabel.style.height = 19f;
            m_StatusLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            m_StatusLabel.style.overflow = Overflow.Hidden;
            m_StatusLabel.style.textOverflow = TextOverflow.Ellipsis;
            navigation.Add(m_StatusLabel);
            m_TimeScroller = new Scroller(0f, 1f, value =>
            {
                if (m_UpdatingScroller) return;
                m_FollowLatest = false;
                var maximum = Math.Max(m_DataEnd, m_ViewStart + m_ViewDuration);
                m_ViewStart = m_DataStart + value * Math.Max(0d, maximum - m_DataStart - m_ViewDuration);
                UpdateView();
            }, SliderDirection.Horizontal) { name = "acc-time-scroll" };
            m_TimeScroller.style.flexGrow = 1f;
            m_TimeScroller.style.marginRight = ScrollbarWidth;
            navigation.Add(m_TimeScroller);
            m_TimelinePane.Add(navigation);
            var components = new VisualElement { name = "acc-components-pane" };
            components.style.minHeight = 100f;
            components.style.borderTopWidth = 1f;
            components.style.borderTopColor = BorderColor;
            m_ContentSplit.Add(components);
            var header = new Toolbar { name = "acc-component-toolbar" };
            m_ComponentView = new ACCComponentView(header);
            NormalizeToolbar(header);
            header.Q<Label>().style.unityTextAlign = TextAnchor.MiddleLeft;
            header.Q<Label>().style.paddingLeft = 6f;
            components.Add(header);
            components.Add(m_ComponentView);
            m_ComponentsVisible = true;
        }

        private void UpdateWindow()
        {
            if (!m_Initialized) return;
            var now = EditorApplication.timeSinceStartup;
            var delta = Math.Max(0d, now - m_LastUpdate);
            m_LastUpdate = now;
            if (m_DarkSkin != EditorGUIUtility.isProSkin)
            {
                CreateGUI();
                return;
            }
            if (now >= m_NextWorldRefresh)
            {
                m_NextWorldRefresh = now + 0.5d;
                RefreshWorlds();
            }
            if (now >= m_NextRefresh)
            {
                m_NextRefresh = now + RefreshInterval;
                RefreshEvents();
                UpdateComponents();
                FlushLog();
            }
            if (!m_Playing) return;
            SetPlayhead(Math.Min(m_Playhead + delta, m_DataEnd), true);
            if (m_Playhead >= m_DataEnd) ResumeFollowing();
        }

        private void RefreshWorlds()
        {
            m_ModuleHandles.Clear();
            m_WorldBuffer.Clear();
            if (EditorApplication.isPlaying && !m_PlayModeTransition)
                Game.CopyModuleHandlesTo(m_ModuleHandles);
            foreach (var handle in m_ModuleHandles)
            {
                if (handle == null || !handle.TryGetModules(out var modules) ||
                    !modules.TryGetModule<IWorldManager>(out var manager)) continue;
                foreach (var world in manager.Worlds)
                    if (world != null && !world.IsDisposed && world.DebugTrace != null && !m_WorldBuffer.Contains(world))
                        m_WorldBuffer.Add(world);
            }
            var changed = m_WorldBuffer.Count != m_Worlds.Count;
            if (!changed)
                for (var i = 0; i < m_Worlds.Count; i++)
                    if (!ReferenceEquals(m_WorldBuffer[i], m_Worlds[i])) { changed = true; break; }
            if (changed || m_WorldField.choices.Count == 0)
            {
                m_Worlds.Clear();
                m_Worlds.AddRange(m_WorldBuffer);
                var names = new List<string>(Math.Max(1, m_Worlds.Count));
                for (var i = 0; i < m_Worlds.Count; i++) names.Add($"{m_Worlds[i].Name} [{i + 1}]");
                if (names.Count == 0) names.Add("无运行中的 ACC 世界");
                m_WorldField.choices = names;
            }
            if (HasWorld && m_Worlds.Contains(m_SelectedWorld))
            {
                m_WorldField.SetValueWithoutNotify(m_WorldField.choices[m_Worlds.IndexOf(m_SelectedWorld)]);
                return;
            }
            if (m_Worlds.Count > 0) SelectWorld(m_Worlds[0]);
            else
            {
                ReleaseRecording();
                m_SelectedWorld = null;
                if (m_Timeline.events.Count > 0 || m_HasOrigin || m_Playhead != 0d)
                    ResetViewState();
                m_WorldField.SetValueWithoutNotify(m_WorldField.choices[0]);
                UpdateStatus();
            }
        }

        private void SelectWorld(World world)
        {
            if (ReferenceEquals(world, m_SelectedWorld)) return;
            ReleaseRecording();
            m_SelectedWorld = world;
            ResetViewState();
            if (HasWorld)
            {
                m_SelectedWorld.DebugTrace.SetOwnerEnabled(this, m_RecordingEnabled);
                m_WorldField.SetValueWithoutNotify(m_WorldField.choices[m_Worlds.IndexOf(world)]);
            }
            UpdateFileLogging();
            RefreshEvents();
        }

        private void ReleaseRecording()
        {
            StopFileLogging();
            m_SelectedWorld?.DebugTrace?.SetOwnerEnabled(this, false);
        }

        private void SetRecordingEnabled(bool enabled)
        {
            m_RecordingEnabled = enabled;
            m_RecordToggle?.SetValueWithoutNotify(enabled);
            if (HasWorld) m_SelectedWorld.DebugTrace.SetOwnerEnabled(this, enabled);
            UpdateFileLogging();
            UpdateStatus();
        }

        private void ClearEvents()
        {
            if (HasWorld) m_SelectedWorld.DebugTrace.Clear();
            ResetViewState();
            RefreshEvents();
        }

        private void ResetViewState()
        {
            SetPlaying(false);
            m_FollowLatest = true;
            m_EventBuffer.Clear();
            m_ObservedEnd = 0d;
            m_Timeline.SetEvents(m_EventBuffer);
            m_SelectedSequence = -1;
            m_SelectedTrack = null;
            m_SelectedActor = Actor.none;
            m_CollapsedActors.Clear();
            m_HasOrigin = false;
            m_TimeOrigin = m_Playhead = m_DataStart = m_ViewStart = 0d;
            m_ViewDuration = m_DataEnd = 10d;
            m_TrackScroll.scrollOffset = Vector2.zero;
            RebuildRows();
            UpdateView();
        }

        private void RefreshEvents()
        {
            if (!m_Initialized) return;
            if (!EditorApplication.isPlaying || m_PlayModeTransition || !HasWorld)
            {
                if (m_Timeline.events.Count > 0 || m_HasOrigin || m_Playhead != 0d)
                    ResetViewState();
                return;
            }
            if (HasWorld)
            {
                m_SelectedWorld.DebugTrace.CopyTo(m_EventBuffer);
                if (!SameEvents())
                {
                    var oldTrack = m_SelectedTrack;
                    var selectedIndex = m_Timeline.FindSequence(m_SelectedSequence);
                    var selectedEvent = selectedIndex >= 0 ? m_Timeline.events[selectedIndex] : default;
                    var reset = m_Timeline.events.Count > 0 && (m_EventBuffer.Count == 0 ||
                        m_EventBuffer[m_EventBuffer.Count - 1].sequence < m_Timeline.events[m_Timeline.events.Count - 1].sequence);
                    if (!reset && m_EventBuffer.Count > 0)
                    {
                        var first = m_EventBuffer[0];
                        var previous = m_Timeline.FindSequence(first.sequence);
                        reset = previous >= 0 && !SameEvent(first, m_Timeline.events[previous]);
                    }
                    if (reset)
                    {
                        SetPlaying(false);
                        m_FollowLatest = true;
                        m_HasOrigin = false;
                        m_SelectedSequence = -1;
                        m_ObservedEnd = 0d;
                    }
                    m_Timeline.SetEvents(m_EventBuffer);
                    m_SelectedTrack = null;
                    if (oldTrack != null)
                        foreach (var group in m_Timeline.groups)
                            foreach (var track in group.tracks)
                                if (track.actor == oldTrack.actor && track.capabilityType == oldTrack.capabilityType)
                                    m_SelectedTrack = track;
                    var newSelected = m_Timeline.FindSequence(m_SelectedSequence);
                    if (newSelected < 0 || (selectedIndex >= 0 && !SameEvent(selectedEvent, m_Timeline.events[newSelected])))
                        m_SelectedSequence = -1;
                    RebuildRows();
                }
            }
            if (m_Timeline.events.Count > 0)
            {
                m_DataStart = m_Timeline.events[0].time;
                m_ObservedEnd = Math.Max(m_ObservedEnd, m_Timeline.events[m_Timeline.events.Count - 1].time);
                if (IsObserving) m_ObservedEnd = Math.Max(m_ObservedEnd, Time.realtimeSinceStartupAsDouble);
                m_DataEnd = m_ObservedEnd;
                if (!m_HasOrigin)
                {
                    m_TimeOrigin = m_DataStart;
                    m_HasOrigin = true;
                    m_ViewStart = m_DataStart;
                    m_Playhead = m_DataStart;
                    UpdateComponents();
                }
            }
            else
            {
                m_DataStart = m_TimeOrigin = m_Playhead = m_ViewStart = 0d;
                m_DataEnd = 10d;
                m_HasOrigin = false;
            }
            FollowLatest();
            UpdateView();
            UpdateStatus();
        }

        private bool SameEvents()
        {
            if (m_EventBuffer.Count != m_Timeline.events.Count) return false;
            for (var i = 0; i < m_EventBuffer.Count; i++)
            {
                if (!SameEvent(m_EventBuffer[i], m_Timeline.events[i])) return false;
            }
            return true;
        }

        private static bool SameEvent(CapabilityDebugEvent left, CapabilityDebugEvent right)
            => left.sequence == right.sequence && left.time == right.time && left.frame == right.frame &&
                left.actor == right.actor && left.capabilityType == right.capabilityType && left.kind == right.kind && left.error == right.error;

        private void SetKindMask(int mask)
        {
            m_KindMask = mask;
            RebuildRows();
        }

        private void SetPlaying(bool playing)
        {
            m_Playing = playing && m_Timeline.events.Count > 0 && m_Playhead < m_DataEnd;
            m_FollowLatest = playing && !m_Playing;
            m_LastUpdate = EditorApplication.timeSinceStartup;
            FollowLatest();
            UpdateView();
        }

        private void FollowLatest()
        {
            if (!m_FollowLatest || m_Timeline.events.Count == 0 || m_DragMode != DragMode.None) return;
            m_Playhead = m_DataEnd;
            m_ViewStart = ACCTemporalTimeline.FollowViewStart(m_DataStart, m_DataEnd, m_ViewStart, m_ViewDuration);
        }

        private void ResumeFollowing()
        {
            m_Playing = false;
            m_FollowLatest = true;
            m_LastUpdate = EditorApplication.timeSinceStartup;
            FollowLatest();
            UpdateView();
        }

        private void SetPlayhead(double time, bool reveal)
        {
            m_FollowLatest = false;
            m_Playhead = Math.Max(m_DataStart, Math.Min(m_DataEnd, time));
            if (reveal && (m_Playhead < m_ViewStart || m_Playhead > m_ViewStart + m_ViewDuration))
                m_ViewStart = Math.Max(m_DataStart, m_Playhead - m_ViewDuration * 0.5d);
            UpdateView();
        }

        private void SelectEvent(int index, bool reveal = false)
        {
            if (index < 0 || index >= m_Timeline.events.Count) return;
            var item = m_Timeline.events[index];
            m_SelectedSequence = item.sequence;
            m_SelectedTrack = m_Timeline.GetTrack(item);
            m_SelectedActor = item.actor;
            SetPlaying(false);
            if (reveal && m_CollapsedActors.Remove(item.actor)) RebuildRows();
            SetPlayhead(item.time, reveal);
            if (reveal && m_SelectedTrack != null && m_SelectedTrack.row >= 0)
                m_TrackScroll.ScrollTo(m_RowHeaders[m_SelectedTrack.row]);
            UpdateSelection();
            UpdateComponents();
        }

        private void NavigateEvent(int direction)
        {
            var visible = m_Timeline.visibleEvents;
            if (direction == 2)
            {
                if (visible.Count > 0) SelectEvent(visible[visible.Count - 1], true);
                ResumeFollowing();
                return;
            }
            if (visible.Count == 0) return;
            if (direction == -2)
            {
                SelectEvent(visible[0], true);
                return;
            }
            var selected = m_Timeline.FindSequence(m_SelectedSequence);
            var cursor = visible.IndexOf(selected);
            if (cursor >= 0)
            {
                cursor = Math.Max(0, Math.Min(visible.Count - 1, cursor + direction));
                SelectEvent(visible[cursor], true);
                return;
            }
            for (var i = direction < 0 ? visible.Count - 1 : 0; i >= 0 && i < visible.Count; i += direction)
            {
                var item = m_Timeline.events[visible[i]];
                if ((direction < 0 && item.time <= m_Playhead) || (direction > 0 && item.time >= m_Playhead))
                {
                    SelectEvent(visible[i], true);
                    return;
                }
            }
        }

        private void UpdateComponents()
        {
            if (m_ComponentView == null) return;
            var visible = HasWorld && !m_SelectedActor.IsNone && m_SelectedWorld.IsActorAlive(m_SelectedActor);
            if (m_ComponentsVisible != visible)
            {
                m_ComponentsVisible = visible;
                if (visible) m_ContentSplit.UnCollapse();
                else m_ContentSplit.CollapseChild(1);
                m_ComponentView.SetVisible(visible);
                if (!visible) m_ComponentView.Refresh(null, Actor.none);
            }
            if (visible) m_ComponentView.Refresh(m_SelectedWorld, m_SelectedActor);
        }

        private void SelectActor(Actor actor)
        {
            m_SelectedActor = actor;
            m_SelectedTrack = null;
            m_SelectedSequence = -1;
            m_Canvas.Focus();
            UpdateSelection();
            UpdateComponents();
        }

        private string FormatEvent(CapabilityDebugEvent item)
            => $"#{item.sequence}  {GetEventName(item.kind)}  {item.actor}  帧 {item.frame}  " +
                $"时间 {item.time - TimeOffset:0.000}s（运行时间 {item.time:0.000}s）\n" +
                $"{item.capabilityType?.FullName ?? "未知能力"}" +
                (string.IsNullOrEmpty(item.error) ? string.Empty : $"\n{item.error}");

        private void CopySelectedEvent()
        {
            var index = m_Timeline.FindSequence(m_SelectedSequence);
            if (index >= 0) EditorGUIUtility.systemCopyBuffer = FormatEvent(m_Timeline.events[index]);
        }

        private void CopyVisibleEvents()
        {
            var text = new StringBuilder("Sequence\tActor\tCapability\tKind\tFrame\tRealtimeSeconds\tMessage\n");
            foreach (var index in m_Timeline.visibleEvents)
            {
                var item = m_Timeline.events[index];
                text.Append(item.sequence).Append('\t').Append(item.actor).Append('\t')
                    .Append(EscapeCell(item.capabilityType?.FullName)).Append('\t').Append(item.kind).Append('\t')
                    .Append(item.frame).Append('\t').Append(item.time.ToString("R", CultureInfo.InvariantCulture)).Append('\t')
                    .Append(EscapeCell(item.error)).Append('\n');
            }
            EditorGUIUtility.systemCopyBuffer = text.ToString();
        }

        private static string EscapeCell(string text)
            => string.IsNullOrEmpty(text) ? string.Empty : "\"" + text.Replace("\"", "\"\"") + "\"";

        private void UpdateStatus()
        {
            if (m_StatusLabel == null) return;
            var running = EditorApplication.isPlaying && !m_PlayModeTransition;
            foreach (var control in m_RuntimeControls) control.SetEnabled(running);
            m_TimelineView.SetEnabled(running);
            m_TimelineView.style.opacity = running ? 1f : 0.5f;
            m_TimeScroller.SetEnabled(running);
            m_TimeScroller.style.opacity = running ? 1f : 0.5f;
            var state = !HasWorld ? "未连接" :
                m_RecordingEnabled ? EditorApplication.isPaused ? "记录已随编辑器暂停" : "记录中" :
                m_SelectedWorld.DebugTrace.IsEnabled ? "本窗口记录已禁用；其他工具仍在记录" : "记录已禁用";
            var count = m_Timeline.events.Count;
            m_StatusLabel.text = HasWorld
                ? $"{m_Timeline.visibleEvents.Count} / {count} 事件 · {m_Timeline.groups.Count} Actor" : string.Empty;
            m_StatusLabel.tooltip = $"{state}  ·  {m_Timeline.visibleEvents.Count} / {count} 个事件  ·  " +
                $"{m_Timeline.groups.Count} 个 Actor" +
                (count > 0 ? $"  ·  保留序号 #{m_Timeline.events[0].sequence}–#{m_Timeline.events[count - 1].sequence}（滚动缓冲）" : string.Empty);
            m_EmptyLabel.text = !HasWorld && count == 0 ? "等待运行中的 ACC 世界" :
                count == 0 ? "暂无能力事件" : "没有匹配的轨道";
            m_EmptyLabel.style.display = m_Timeline.rows.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            m_FilterLabel.text = m_KindMask == ACCTemporalTimeline.AllKinds ? "全部事件" : "事件筛选";
        }

        private static double ClampDuration(double value)
            => !Game.NumberUtility.IsFinite(value) ? 10d : Math.Max(MinDuration, Math.Min(MaxDuration, value));

        private static DropdownMenuAction.Status Checked(bool value)
            => value ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
    }
}

#endif
