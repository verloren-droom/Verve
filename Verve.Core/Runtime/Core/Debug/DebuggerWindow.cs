#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;
    
    
    /// <summary>
    ///   <para>调试器窗口</para>
    /// </summary>
    internal static class DebuggerWindow
    {
        private static bool s_Visible;
        private static Rect s_WindowRect = new Rect(40f, 80f, 820f, 520f);
        private static readonly Vector2 s_WindowBaseMinSize = new Vector2(600f, 420f);
        private static bool s_Dragging;
        private static bool s_Resizing;
        private static Vector2 s_DragOffset;
        private static Vector2 s_ResizeStart;
        private static Vector2 s_WindowSizeStart;
        private static List<DebugWindowEntry> s_Windows;
        private static bool s_WindowsRegistered;
        private static int s_SelectedTab;
        private static Vector2 s_ContentScroll;
        private static Vector2 s_TabScroll;
        private static GUIStyle s_WindowStyle;
        private static GUIStyle s_TitleStyle;
        private static GUIStyle s_TabStyle;
        private static GUIStyle s_TabActiveStyle;
        private static GUIStyle s_SettingsStyle;
        private static GUIStyle s_ToolbarStyle;
        private static GUIStyle s_ContentStyle;
        private static bool s_StylesInitialized;
        private static bool s_SettingsVisible;
        private static Vector2 s_SettingsScroll;
        private static Texture2D s_BackgroundTexture;
        private static Texture2D s_BorderTexture;
        private static DebugTabWindowSettings s_TabSettings;
        private static int s_FontSize = 12;
        private static Color s_FontColor = Color.white;
        private static Color s_BackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        private static Color s_BorderColor = new Color(1f, 1f, 1f, 0.7f);
        private static float s_BorderWidth = 1f;
        private static int s_StyleFontSize = -1;
        private static Color s_StyleFontColor;
        private static Color s_StyleBackgroundColor;

        public static bool Visible => s_Visible;

        public static void Toggle()
        {
            RegisterDebugWindows(false);
            if (s_Visible) Hide();
            else Show();
        }

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

        private static float DrawColorSlider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(16));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.Label(value.ToString("0.00"), GUILayout.Width(38));
            GUILayout.EndHorizontal();
            return value;
        }

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

        private static Vector2 GetActiveMinSize()
        {
            var min = s_WindowBaseMinSize;
            if (s_Windows == null || s_Windows.Count == 0) return min;
            var window = s_Windows[Mathf.Clamp(s_SelectedTab, 0, s_Windows.Count - 1)];
            min = new Vector2(Mathf.Max(min.x, window.minSize.x), Mathf.Max(min.y, window.minSize.y));
            return min;
        }

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

        private static void DrawResizeHandle()
        {
            var handleSize = 14f;
            var rect = new Rect(s_WindowRect.xMax - handleSize, s_WindowRect.yMax - handleSize, handleSize, handleSize);
            GUI.Box(rect, "");
        }

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

        private static Rect ClampToScreen(Rect rect)
        {
            var maxX = Mathf.Max(0f, Screen.width - rect.width);
            var maxY = Mathf.Max(0f, Screen.height - rect.height);
            rect.x = Mathf.Clamp(rect.x, 0f, maxX);
            rect.y = Mathf.Clamp(rect.y, 0f, maxY);
            return rect;
        }

        private static void RegisterDebugWindows(bool force)
        {
            if (s_WindowsRegistered && s_Windows != null && !force) return;
            var wasVisible = s_Visible;
            if (s_Windows != null && wasVisible)
            {
                for (int i = 0; i < s_Windows.Count; i++)
                {
                    s_Windows[i].tabWindow.OnHide();
                }
            }

            s_WindowsRegistered = true;
            if (s_Windows == null) s_Windows = new List<DebugWindowEntry>(8);
            else s_Windows.Clear();
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

            var types = GetDebugWindowTypes();
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];
                try
                {
                    var attr = type.GetCustomAttribute<DebugItemAttribute>();
                    if (attr == null) continue;
                    if (type.IsAbstract || type.IsInterface) continue;
                    if (!typeof(DebugTabWindow).IsAssignableFrom(type)) continue;
                    if (Activator.CreateInstance(type, s_TabSettings) is not DebugTabWindow window) continue;
                    s_Windows.Add(new DebugWindowEntry(window, attr.title, s_WindowBaseMinSize, attr.order));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to create debug tab window '{type.FullName}'. {ex.Message}");
                }
            }

            s_Windows.Sort((a, b) => a.order.CompareTo(b.order));
            s_SelectedTab = Mathf.Clamp(s_SelectedTab, 0, Math.Max(0, s_Windows.Count - 1));

            if (wasVisible)
            {
                for (int i = 0; i < s_Windows.Count; i++)
                {
                    s_Windows[i].tabWindow.OnShow();
                }
            }
        }

        private static List<Type> GetDebugWindowTypes()
        {
            var result = new List<Type>(64);
            var assemblies = Game.DebugGetFilteredAssemblies();
            for (int i = 0; i < assemblies.Count; i++)
            {
                var assembly = assemblies[i];
                Type[] types = null;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                if (types == null) continue;
                for (int t = 0; t < types.Length; t++)
                {
                    var type = types[t];
                    if (type == null) continue;
                    if (type.GetCustomAttribute<DebugItemAttribute>() == null) continue;
                    result.Add(type);
                }
            }
            return result;
        }

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

        private readonly struct DebugWindowEntry
        {
            public readonly DebugTabWindow tabWindow;
            public readonly string title;
            public readonly Vector2 minSize;
            public readonly int order;

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
    ///   <para>调试窗口栏设置</para>
    /// </summary>
    public sealed class DebugTabWindowSettings
    {
        /// <summary>
        ///   <para>字体大小</para>
        /// </summary>
        public int FontSize { get; internal set;}
        /// <summary>
        ///   <para>字体颜色</para>
        /// </summary>
        public Color FontColor { get; internal set;}
        /// <summary>
        ///   <para>边框颜色</para>
        /// </summary>
        public Color BorderColor { get; internal set; }
        /// <summary>
        ///   <para>边框宽度</para>
        /// </summary>
        public float BorderWidth { get; internal set; }
    }

    /// <summary>
    ///   <para>调试窗口栏基类</para>
    /// </summary>
    public abstract class DebugTabWindow
    {
        protected DebugTabWindowSettings Settings { get; }

        protected DebugTabWindow(DebugTabWindowSettings settings)
        {
            Settings = settings;
        }

        public abstract void OnShow();
        public abstract void OnHide();
        public abstract void Draw();
    }
}

#endif