#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>调试器窗口。</para>
    /// </summary>
    internal static class DebuggerWindow
    {
        /// <summary>
        ///   <para>可见。</para>
        /// </summary>
        private static bool s_Visible;
        /// <summary>
        ///   <para>窗口区域。</para>
        /// </summary>
        private static Rect s_WindowRect = new Rect(40f, 80f, 820f, 520f);
        /// <summary>
        ///   <para>窗口最小基础尺寸。</para>
        /// </summary>
        private static readonly Vector2 s_WindowBaseMinSize = new Vector2(600f, 420f);
        /// <summary>
        ///   <para>拖动中。</para>
        /// </summary>
        private static bool s_Dragging;
        /// <summary>
        ///   <para>调整大小中。</para>
        /// </summary>
        private static bool s_Resizing;
        /// <summary>
        ///   <para>拖动偏移量。</para>
        /// </summary>
        private static Vector2 s_DragOffset;
        /// <summary>
        ///   <para>开始调整窗口大小时的鼠标位置。</para>
        /// </summary>
        private static Vector2 s_ResizeStart;
        /// <summary>
        ///   <para>开始调整时的窗口尺寸。</para>
        /// </summary>
        private static Vector2 s_WindowSizeStart;
        /// <summary>
        ///   <para>窗口。</para>
        /// </summary>
        private static List<DebugWindowEntry> s_Windows;
        /// <summary>
        ///   <para>窗口已注册。</para>
        /// </summary>
        private static bool s_WindowsRegistered;
        /// <summary>
        ///   <para>选中标签页。</para>
        /// </summary>
        private static int s_SelectedTab;
        /// <summary>
        ///   <para>内容滚动。</para>
        /// </summary>
        private static Vector2 s_ContentScroll;
        /// <summary>
        ///   <para>标签页滚动。</para>
        /// </summary>
        private static Vector2 s_TabScroll;
        /// <summary>
        ///   <para>窗口样式。</para>
        /// </summary>
        private static GUIStyle s_WindowStyle;
        /// <summary>
        ///   <para>标题样式。</para>
        /// </summary>
        private static GUIStyle s_TitleStyle;
        /// <summary>
        ///   <para>标签页样式。</para>
        /// </summary>
        private static GUIStyle s_TabStyle;
        /// <summary>
        ///   <para>标签页激活样式。</para>
        /// </summary>
        private static GUIStyle s_TabActiveStyle;
        /// <summary>
        ///   <para>设置样式。</para>
        /// </summary>
        private static GUIStyle s_SettingsStyle;
        /// <summary>
        ///   <para>工具栏样式。</para>
        /// </summary>
        private static GUIStyle s_ToolbarStyle;
        /// <summary>
        ///   <para>内容样式。</para>
        /// </summary>
        private static GUIStyle s_ContentStyle;
        /// <summary>
        ///   <para>样式已初始化。</para>
        /// </summary>
        private static bool s_StylesInitialized;
        /// <summary>
        ///   <para>设置可见。</para>
        /// </summary>
        private static bool s_SettingsVisible;
        /// <summary>
        ///   <para>设置滚动。</para>
        /// </summary>
        private static Vector2 s_SettingsScroll;
        /// <summary>
        ///   <para>背景纹理。</para>
        /// </summary>
        private static Texture2D s_BackgroundTexture;
        /// <summary>
        ///   <para>边框纹理。</para>
        /// </summary>
        private static Texture2D s_BorderTexture;
        /// <summary>
        ///   <para>标签页设置。</para>
        /// </summary>
        private static DebugTabWindowSettings s_TabSettings;
        /// <summary>
        ///   <para>字体大小。</para>
        /// </summary>
        private static int s_FontSize = 12;
        /// <summary>
        ///   <para>字体颜色。</para>
        /// </summary>
        private static Color s_FontColor = Color.white;
        /// <summary>
        ///   <para>背景颜色。</para>
        /// </summary>
        private static Color s_BackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        /// <summary>
        ///   <para>边框颜色。</para>
        /// </summary>
        private static Color s_BorderColor = new Color(1f, 1f, 1f, 0.7f);
        /// <summary>
        ///   <para>边框宽度。</para>
        /// </summary>
        private static float s_BorderWidth = 1f;
        /// <summary>
        ///   <para>样式字体大小。</para>
        /// </summary>
        private static int s_StyleFontSize = -1;
        /// <summary>
        ///   <para>样式字体颜色。</para>
        /// </summary>
        private static Color s_StyleFontColor;
        /// <summary>
        ///   <para>样式背景颜色。</para>
        /// </summary>
        private static Color s_StyleBackgroundColor;

        /// <summary>
        ///   <para>可见。</para>
        /// </summary>
        public static bool Visible => s_Visible;

        /// <summary>
        ///   <para>切换。</para>
        /// </summary>
        public static void Toggle()
        {
            RegisterDebugWindows(false);
            if (s_Visible) Hide();
            else Show();
        }

        /// <summary>
        ///   <para>显示。</para>
        /// </summary>
        public static void Show()
        {
            RegisterDebugWindows(false);
            if (s_Visible) return;
            s_Visible = true;
            for (int i = 0; i < s_Windows.Count; i++)
            {
                s_Windows[i].tabWindow.OnShow();
            }
        }

        /// <summary>
        ///   <para>隐藏。</para>
        /// </summary>
        public static void Hide()
        {
            if (!s_Visible) return;
            if (s_Windows != null)
            {
                for (int i = 0; i < s_Windows.Count; i++)
                {
                    s_Windows[i].tabWindow.OnHide();
                }
            }
            s_Visible = false;
        }

        /// <summary>
        ///   <para>绘制界面。</para>
        /// </summary>
        public static void DrawGUI()
        {
            if (!s_Visible) return;
            RegisterDebugWindows(false);
            EnsureStyles();

            var minSize = GetActiveMinSize();
            s_WindowRect.width = Mathf.Max(s_WindowRect.width, minSize.x);
            s_WindowRect.height = Mathf.Max(s_WindowRect.height, minSize.y);

            var e = Event.current;
            HandleDrag(e);
            HandleResize(e, minSize);

            s_WindowRect = ClampToScreen(s_WindowRect);

            GUI.Box(s_WindowRect, GUIContent.none, s_WindowStyle);
            GUI.Label(new Rect(s_WindowRect.x, s_WindowRect.y, s_WindowRect.width, 28f), "Debugger", s_TitleStyle);
            DrawBorder(s_WindowRect);
            
            GUILayout.BeginArea(new Rect(s_WindowRect.x + 6f, s_WindowRect.y + 32f, s_WindowRect.width - 12f, s_WindowRect.height - 38f));
            GUILayout.BeginVertical();
            GUILayout.BeginVertical(s_ToolbarStyle);
            DrawTabs();
            GUILayout.EndVertical();
            GUILayout.Space(6f);
            DrawSettingsPanel();
            GUILayout.Space(6f);
            GUILayout.BeginVertical(s_ContentStyle);
            DrawActiveWindow();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
            GUILayout.EndArea();

            DrawResizeHandle();
            ConsumeEvents(e);
        }

        /// <summary>
        ///   <para>绘制标签页组。</para>
        /// </summary>
        private static void DrawTabs()
        {
            if (s_Windows == null || s_Windows.Count == 0) return;
            var tabHeight = Mathf.Max(22f, s_FontSize + 10f);
            var scrollHeight = tabHeight + 12f;
            GUILayout.BeginHorizontal();
            s_TabScroll = GUILayout.BeginScrollView(s_TabScroll, true, false, GUIStyle.none, GUI.skin.horizontalScrollbar, GUILayout.Height(scrollHeight), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal(GUILayout.Height(tabHeight));
            for (int i = 0; i < s_Windows.Count; i++)
            {
                var window = s_Windows[i];
                var isActive = i == s_SelectedTab;
                var style = isActive ? s_TabActiveStyle : s_TabStyle;
                if (GUILayout.Button(window.title, style, GUILayout.Height(tabHeight)))
                {
                    s_SelectedTab = i;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            if (GUILayout.Button("...", s_TabStyle, GUILayout.Width(36f), GUILayout.Height(tabHeight)))
            {
                s_SettingsVisible = !s_SettingsVisible;
            }
            if (GUILayout.Button("✖", s_TabStyle, GUILayout.Width(36f), GUILayout.Height(tabHeight)))
            {
                Hide();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>绘制设置面板。</para>
        /// </summary>
        private static void DrawSettingsPanel()
        {
            if (!s_SettingsVisible) return;
            GUILayout.BeginVertical(s_SettingsStyle);
            s_SettingsScroll = GUILayout.BeginScrollView(s_SettingsScroll, GUILayout.Height(180f));

            if (GUILayout.Button("Reload Windows", GUILayout.Width(140f)))
            {
                RegisterDebugWindows(true);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Font Size", GUILayout.Width(90));
            s_FontSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(s_FontSize, 10f, 24f));
            GUILayout.Label(s_FontSize.ToString(), GUILayout.Width(30));
            GUILayout.EndHorizontal();

            DrawColorSliders("Font Color", ref s_FontColor, true);
            DrawColorSliders("Background", ref s_BackgroundColor, false);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Border Width", GUILayout.Width(90));
            s_BorderWidth = GUILayout.HorizontalSlider(s_BorderWidth, 0f, 6f);
            GUILayout.Label(s_BorderWidth.ToString("0.0"), GUILayout.Width(30));
            GUILayout.EndHorizontal();
            DrawColorSliders("Border Color", ref s_BorderColor, false);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>绘制颜色滑动条组。</para>
        /// </summary>
        /// <param name="label">标签。</param>
        /// <param name="color">颜色。</param>
        /// <param name="alphaReadonly">是否禁止修改透明度。</param>
        private static void DrawColorSliders(string label, ref Color color, bool alphaReadonly)
        {
            GUILayout.Label(label);
            color.r = DrawColorSlider("R", color.r);
            color.g = DrawColorSlider("G", color.g);
            color.b = DrawColorSlider("B", color.b);
            if (!alphaReadonly)
            {
                color.a = DrawColorSlider("A", color.a);
            }
        }

        /// <summary>
        ///   <para>绘制颜色滑动条。</para>
        /// </summary>
        /// <param name="label">标签。</param>
        /// <param name="value">值。</param>
        private static float DrawColorSlider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(16));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.Label(value.ToString("0.00"), GUILayout.Width(38));
            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        ///   <para>绘制激活窗口。</para>
        /// </summary>
        private static void DrawActiveWindow()
        {
            if (s_Windows == null || s_Windows.Count == 0) return;
            s_SelectedTab = Mathf.Clamp(s_SelectedTab, 0, s_Windows.Count - 1);
            s_ContentScroll = GUILayout.BeginScrollView(s_ContentScroll);
            var label = GUI.skin.label;
            var button = GUI.skin.button;
            var prevLabelSize = label.fontSize;
            var prevLabelColor = label.normal.textColor;
            var prevButtonSize = button.fontSize;
            var prevButtonNormal = button.normal.textColor;
            var prevButtonHover = button.hover.textColor;
            var prevButtonActive = button.active.textColor;
            var prevButtonFocused = button.focused.textColor;
            if (s_TabSettings != null)
            {
                label.fontSize = s_TabSettings.FontSize;
                label.normal.textColor = s_TabSettings.FontColor;
                button.fontSize = s_TabSettings.FontSize;
                button.normal.textColor = s_TabSettings.FontColor;
                button.hover.textColor = s_TabSettings.FontColor;
                button.active.textColor = s_TabSettings.FontColor;
                button.focused.textColor = s_TabSettings.FontColor;
            }
            try
            {
                s_Windows[s_SelectedTab].tabWindow.Draw();
            }
            finally
            {
                label.fontSize = prevLabelSize;
                label.normal.textColor = prevLabelColor;
                button.fontSize = prevButtonSize;
                button.normal.textColor = prevButtonNormal;
                button.hover.textColor = prevButtonHover;
                button.active.textColor = prevButtonActive;
                button.focused.textColor = prevButtonFocused;
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        ///   <para>获取当前标签页的最小尺寸。</para>
        /// </summary>
        private static Vector2 GetActiveMinSize()
        {
            var min = s_WindowBaseMinSize;
            if (s_Windows == null || s_Windows.Count == 0) return min;
            var window = s_Windows[Mathf.Clamp(s_SelectedTab, 0, s_Windows.Count - 1)];
            min = new Vector2(Mathf.Max(min.x, window.minSize.x), Mathf.Max(min.y, window.minSize.y));
            return min;
        }

        /// <summary>
        ///   <para>处理拖动。</para>
        /// </summary>
        /// <param name="e">事件参数。</param>
        private static void HandleDrag(Event e)
        {
            if (e == null) return;
            var headerRect = new Rect(s_WindowRect.x, s_WindowRect.y, s_WindowRect.width, 28f);
            if (e.type == EventType.MouseDown && e.button == 0 && headerRect.Contains(e.mousePosition))
            {
                s_Dragging = true;
                s_DragOffset = e.mousePosition - new Vector2(s_WindowRect.x, s_WindowRect.y);
                e.Use();
            }
            if (s_Dragging && e.type == EventType.MouseDrag)
            {
                s_WindowRect.position = e.mousePosition - s_DragOffset;
                e.Use();
            }
            if (s_Dragging && e.type == EventType.MouseUp)
            {
                s_Dragging = false;
                e.Use();
            }
        }

        /// <summary>
        ///   <para>处理调整大小。</para>
        /// </summary>
        /// <param name="e">事件参数。</param>
        /// <param name="minSize">最小尺寸。</param>
        private static void HandleResize(Event e, Vector2 minSize)
        {
            if (e == null) return;
            var handleSize = 14f;
            var handleRect = new Rect(s_WindowRect.xMax - handleSize, s_WindowRect.yMax - handleSize, handleSize, handleSize);
            var controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.MouseDown && e.button == 0 && handleRect.Contains(e.mousePosition))
            {
                GUIUtility.hotControl = controlId;
                s_Resizing = true;
                s_ResizeStart = e.mousePosition;
                s_WindowSizeStart = new Vector2(s_WindowRect.width, s_WindowRect.height);
                e.Use();
            }
            if (s_Resizing && e.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
            {
                var delta = e.mousePosition - s_ResizeStart;
                s_WindowRect.width = Mathf.Max(minSize.x, s_WindowSizeStart.x + delta.x);
                s_WindowRect.height = Mathf.Max(minSize.y, s_WindowSizeStart.y + delta.y);
                e.Use();
            }
            if (s_Resizing && e.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                s_Resizing = false;
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        /// <summary>
        ///   <para>绘制窗口缩放手柄。</para>
        /// </summary>
        private static void DrawResizeHandle()
        {
            var handleSize = 14f;
            var rect = new Rect(s_WindowRect.xMax - handleSize, s_WindowRect.yMax - handleSize, handleSize, handleSize);
            GUI.Box(rect, "");
        }

        /// <summary>
        ///   <para>绘制边框。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        private static void DrawBorder(Rect rect)
        {
            if (s_BorderWidth <= 0f) return;
            if (s_BorderTexture == null)
            {
                s_BorderTexture = Game.TextureUtility.MakeTex2D(2, 2, Color.white);
            }
            var prevColor = GUI.color;
            GUI.color = s_BorderColor;
            var width = Mathf.Max(0.5f, s_BorderWidth);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, width), s_BorderTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - width, rect.width, width), s_BorderTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, width, rect.height), s_BorderTexture);
            GUI.DrawTexture(new Rect(rect.xMax - width, rect.y, width, rect.height), s_BorderTexture);
            GUI.color = prevColor;
        }

        /// <summary>
        ///   <para>消费事件。</para>
        /// </summary>
        /// <param name="e">事件参数。</param>
        private static void ConsumeEvents(Event e)
        {
            if (e == null) return;
            if (!s_WindowRect.Contains(e.mousePosition) && !s_Dragging && !s_Resizing) return;
            if (e.type == EventType.ScrollWheel ||
                e.type == EventType.MouseDown ||
                e.type == EventType.MouseUp ||
                e.type == EventType.MouseDrag)
            {
                e.Use();
            }
        }

        /// <summary>
        ///   <para>将窗口限制在屏幕内。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        private static Rect ClampToScreen(Rect rect)
        {
            var maxX = Mathf.Max(0f, Screen.width - rect.width);
            var maxY = Mathf.Max(0f, Screen.height - rect.height);
            rect.x = Mathf.Clamp(rect.x, 0f, maxX);
            rect.y = Mathf.Clamp(rect.y, 0f, maxY);
            return rect;
        }

        /// <summary>
        ///   <para>注册调试窗口。</para>
        /// </summary>
        /// <param name="force">强制。</param>
        private static void RegisterDebugWindows(bool force)
        {
            if (s_WindowsRegistered && s_Windows != null && !force) return;
            if (s_TabSettings == null)
            {
                s_TabSettings = new DebugTabWindowSettings
                {
                    FontSize = s_FontSize,
                    FontColor = s_FontColor,
                    BorderColor = s_BorderColor,
                    BorderWidth = s_BorderWidth,
                };
            }
            else
            {
                s_TabSettings.FontSize = s_FontSize;
                s_TabSettings.FontColor = s_FontColor;
                s_TabSettings.BorderColor = s_BorderColor;
                s_TabSettings.BorderWidth = s_BorderWidth;
            }

            var windows = new List<DebugWindowEntry>();
            foreach (var assembly in Game.DebugGetFilteredAssemblies())
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(DebugTabWindow).IsAssignableFrom(type)) continue;
                var attribute = type.GetCustomAttribute<DebugItemAttribute>();
                if (attribute == null) continue;
                var window = (DebugTabWindow)Activator.CreateInstance(type, s_TabSettings);
                windows.Add(new DebugWindowEntry(window, attribute.title, s_WindowBaseMinSize, attribute.order));
            }
            windows.Sort((a, b) => a.order.CompareTo(b.order));

            var wasVisible = s_Visible;
            if (wasVisible && s_Windows != null)
                foreach (var entry in s_Windows) entry.tabWindow.OnHide();
            s_Windows = windows;
            s_WindowsRegistered = true;
            s_SelectedTab = Mathf.Clamp(s_SelectedTab, 0, Math.Max(0, s_Windows.Count - 1));

            if (wasVisible)
            {
                for (int i = 0; i < s_Windows.Count; i++)
                {
                    s_Windows[i].tabWindow.OnShow();
                }
            }
        }

        /// <summary>
        ///   <para>初始化尚未创建的样式。</para>
        /// </summary>
        private static void EnsureStyles()
        {
            if (s_StylesInitialized)
            {
                ApplyStyleSettings();
                return;
            }
            s_StylesInitialized = true;
            s_WindowStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(6, 6, 6, 6) };
            s_TitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = s_FontSize + 1,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 0, 0)
            };
            s_TabStyle = new GUIStyle(GUI.skin.button) { padding = new RectOffset(8, 8, 2, 2), fontSize = s_FontSize };
            s_TabActiveStyle = new GUIStyle(s_TabStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = s_TabStyle.active
            };
            s_SettingsStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6) };
            s_ToolbarStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(6, 6, 4, 4) };
            s_ContentStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 8, 8) };
            ApplyStyleSettings();
        }

        /// <summary>
        ///   <para>应用样式设置。</para>
        /// </summary>
        private static void ApplyStyleSettings()
        {
            if (s_BackgroundTexture == null || s_StyleBackgroundColor != s_BackgroundColor)
            {
                if (s_BackgroundTexture != null)
                {
                    UnityEngine.Object.Destroy(s_BackgroundTexture);
                }
                s_BackgroundTexture = Game.TextureUtility.MakeTex2D(2, 2, s_BackgroundColor);
                s_WindowStyle.normal.background = s_BackgroundTexture;
                s_SettingsStyle.normal.background = s_BackgroundTexture;
                s_StyleBackgroundColor = s_BackgroundColor;
            }

            if (s_StyleFontSize != s_FontSize || s_StyleFontColor != s_FontColor)
            {
                s_StyleFontSize = s_FontSize;
                s_TabStyle.fontSize = s_FontSize;
                s_TabActiveStyle.fontSize = s_FontSize;
                s_TitleStyle.fontSize = s_FontSize + 1;
                s_TitleStyle.normal.textColor = s_FontColor;
                s_TabStyle.normal.textColor = s_FontColor;
                s_TabStyle.hover.textColor = s_FontColor;
                s_TabStyle.active.textColor = s_FontColor;
                s_TabStyle.focused.textColor = s_FontColor;
                s_TabActiveStyle.normal.textColor = s_FontColor;
                s_TabActiveStyle.hover.textColor = s_FontColor;
                s_TabActiveStyle.active.textColor = s_FontColor;
                s_TabActiveStyle.focused.textColor = s_FontColor;
                s_StyleFontColor = s_FontColor;
            }
            if (s_TabSettings != null)
            {
                s_TabSettings.FontSize = s_FontSize;
                s_TabSettings.FontColor = s_FontColor;
                s_TabSettings.BorderColor = s_BorderColor;
                s_TabSettings.BorderWidth = s_BorderWidth;
            }
        }

        /// <summary>
        ///   <para>调试窗口条目。</para>
        /// </summary>
        private readonly struct DebugWindowEntry
        {
            /// <summary>
            ///   <para>标签页窗口。</para>
            /// </summary>
            public readonly DebugTabWindow tabWindow;
            /// <summary>
            ///   <para>标题。</para>
            /// </summary>
            public readonly string title;
            /// <summary>
            ///   <para>最小尺寸。</para>
            /// </summary>
            public readonly Vector2 minSize;
            /// <summary>
            ///   <para>顺序。</para>
            /// </summary>
            public readonly int order;

            /// <summary>
            ///   <para>创建调试窗口条目。</para>
            /// </summary>
            /// <param name="tabWindow">标签页窗口。</param>
            /// <param name="title">标题。</param>
            /// <param name="minSize">最小尺寸。</param>
            /// <param name="order">顺序。</param>
            public DebugWindowEntry(DebugTabWindow tabWindow, string title, Vector2 minSize, int order)
            {
                this.tabWindow = tabWindow;
                this.title = title;
                this.minSize = minSize;
                this.order = order;
            }
        }
    }

    /// <summary>
    ///   <para>调试窗口栏设置。</para>
    /// </summary>
    public sealed class DebugTabWindowSettings
    {
        /// <summary>
        ///   <para>字体大小。</para>
        /// </summary>
        public int FontSize { get; internal set;}
        /// <summary>
        ///   <para>字体颜色。</para>
        /// </summary>
        public Color FontColor { get; internal set;}
        /// <summary>
        ///   <para>边框颜色。</para>
        /// </summary>
        public Color BorderColor { get; internal set; }
        /// <summary>
        ///   <para>边框宽度。</para>
        /// </summary>
        public float BorderWidth { get; internal set; }
    }

    /// <summary>
    ///   <para>调试窗口栏基类。</para>
    /// </summary>
    public abstract class DebugTabWindow
    {
        /// <summary>
        ///   <para>设置。</para>
        /// </summary>
        protected DebugTabWindowSettings Settings { get; }

        /// <summary>
        ///   <para>创建调试标签页窗口。</para>
        /// </summary>
        /// <param name="settings">设置。</param>
        protected DebugTabWindow(DebugTabWindowSettings settings) => Settings = settings;

        /// <summary>
        ///   <para>显示时更新状态。</para>
        /// </summary>
        public abstract void OnShow();
        /// <summary>
        ///   <para>隐藏时清理状态。</para>
        /// </summary>
        public abstract void OnHide();
        /// <summary>
        ///   <para>绘制内容。</para>
        /// </summary>
        public abstract void Draw();
    }
}

#endif