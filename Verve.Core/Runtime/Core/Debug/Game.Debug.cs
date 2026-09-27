#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    using System.Text;
    using System.Reflection;
    using System.Diagnostics;
    using UnityEngine.Profiling;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Runtime.CompilerServices;
    using Object = UnityEngine.Object;

    /// <summary>
    ///   <para>游戏入口；调试器。</para>
    /// </summary>
    public static partial class Game
    {
        /// <summary>
        ///   <para>控制台驱动组件。</para>
        /// </summary>
        private static DebugConsoleRunner s_DebugConsoleRunner;
        /// <summary>
        ///   <para>调试控制台启用。</para>
        /// </summary>
        private static bool s_DebugConsoleEnabled;
        /// <summary>
        ///   <para>调试控制台可见。</para>
        /// </summary>
        private static bool s_DebugConsoleVisible;
        /// <summary>
        ///   <para>输出消息队列。</para>
        /// </summary>
        private static readonly List<DebugConsoleMessage> s_DebugConsoleOutput = new();
        /// <summary>
        ///   <para>当前输入命令文本。</para>
        /// </summary>
        private static string s_DebugConsoleInput = "";
        /// <summary>
        ///   <para>输出滚动位置。</para>
        /// </summary>
        private static Vector2 s_DebugConsoleScrollPosition = Vector2.zero;
        /// <summary>
        ///   <para>输出区域是否展开。</para>
        /// </summary>
        private static bool s_DebugConsoleOutputFoldout;
        /// <summary>
        ///   <para>控制台当前高度。</para>
        /// </summary>
        private static float s_DebugConsoleHeight = 320f;
        /// <summary>
        ///   <para>控制台最小高度。</para>
        /// </summary>
        private static readonly float s_DebugConsoleMinHeight = 120f;
        /// <summary>
        ///   <para>折叠时高度。</para>
        /// </summary>
        private static readonly float s_DebugConsoleCollapsedHeight = 50f;
        /// <summary>
        ///   <para>拖拽条高度。</para>
        /// </summary>
        private static readonly float s_DebugConsoleResizeHandleHeight = 16f;
        /// <summary>
        ///   <para>是否正在拖拽调整高度。</para>
        /// </summary>
        private static bool s_DebugConsoleResizing;
        /// <summary>
        ///   <para>拖拽起点 Y 坐标。</para>
        /// </summary>
        private static float s_DebugConsoleDragStartY;
        /// <summary>
        ///   <para>拖拽起点高度。</para>
        /// </summary>
        private static float s_DebugConsoleHeightOnDragStart;
        /// <summary>
        ///   <para>控制台字体大小。</para>
        /// </summary>
        private static int s_DebugConsoleFontSize = 20;
        /// <summary>
        ///   <para>控制台字体颜色。</para>
        /// </summary>
        private static Color s_DebugConsoleFontColor = Color.white;
        /// <summary>
        ///   <para>控制台背景颜色。</para>
        /// </summary>
        private static Color s_DebugConsoleBackgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.6f);
        /// <summary>
        ///   <para>控制台命令表。</para>
        /// </summary>
        private static Dictionary<string, DebugConsoleCommandInfo> s_DebugConsoleCommands = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        ///   <para>控制台是否已初始化。</para>
        /// </summary>
        private static bool s_DebugConsoleInitialized;
        /// <summary>
        ///   <para>控制台容器样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleStyle;
        /// <summary>
        ///   <para>输入框样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleInputStyle;
        /// <summary>
        ///   <para>输出标签样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleLabelStyle;
        /// <summary>
        ///   <para>提示按钮与面板样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleTipStyle;
        /// <summary>
        ///   <para>设置面板样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleSettingsStyle;
        /// <summary>
        ///   <para>内联标签样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleInlineLabelStyle;
        /// <summary>
        ///   <para>内联按钮样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleInlineButtonStyle;
        /// <summary>
        ///   <para>背景贴图。</para>
        /// </summary>
        private static Texture2D s_DebugConsoleBackgroundTexture;
        /// <summary>
        ///   <para>样式字体大小缓存。</para>
        /// </summary>
        private static int s_DebugConsoleStyleFontSize = -1;
        /// <summary>
        ///   <para>样式字体颜色缓存。</para>
        /// </summary>
        private static Color s_DebugConsoleStyleFontColor;
        /// <summary>
        ///   <para>样式背景颜色缓存。</para>
        /// </summary>
        private static Color s_DebugConsoleStyleBackgroundColor;
        /// <summary>
        ///   <para>命令是否已加载。</para>
        /// </summary>
        private static bool s_DebugConsoleCommandsLoaded;
        /// <summary>
        ///   <para>提示按钮是否靠右显示。</para>
        /// </summary>
        private static bool s_DebugConsoleTipRight = true;
        /// <summary>
        ///   <para>设置面板是否显示。</para>
        /// </summary>
        private static bool s_DebugConsoleSettingsVisible;
        /// <summary>
        ///   <para>设置面板滚动位置。</para>
        /// </summary>
        private static Vector2 s_DebugConsoleSettingsScrollPosition = Vector2.zero;
        /// <summary>
        ///   <para>默认程序集排除前缀。</para>
        /// </summary>
        private static readonly string[] s_DebugConsoleAssemblyExcludePrefixes = { "System.", "Microsoft.", "Mono.", "netstandard" };
        /// <summary>
        ///   <para>默认程序集排除名称。</para>
        /// </summary>
        private static readonly HashSet<string> s_DebugConsoleAssemblyExcludeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "UnityEngine",
            "UnityEditor",
            "UnityEditor.CoreModule",
        };
        /// <summary>
        ///   <para>默认程序集排除规则。</para>
        /// </summary>
        private static readonly Regex[] s_DebugConsoleAssemblyExcludeRegex =
        {
            new("^Unity(\\.|$)", RegexOptions.Compiled),
        };
        /// <summary>
        ///   <para>程序集缓存。</para>
        /// </summary>
        private static List<Assembly> s_DebugAssemblyCache;
        /// <summary>
        ///   <para>程序集缓存是否可用。</para>
        /// </summary>
        private static bool s_DebugAssemblyCacheReady;
        /// <summary>
        ///   <para>提示按钮显示开关。</para>
        /// </summary>
        private static bool s_DebugConsoleTipEnabled = true;
        /// <summary>
        ///   <para>提示折叠面板是否展开。</para>
        /// </summary>
        private static bool s_DebugTipFoldout;
        /// <summary>
        ///   <para>下一次采样时间点。</para>
        /// </summary>
        private static float s_DebugTipNextUpdateTime;
        /// <summary>
        ///   <para>采样间隔（秒）。</para>
        /// </summary>
        private static float s_DebugTipUpdateInterval = 0.7f;
        /// <summary>
        ///   <para>平滑后的 FPS。</para>
        /// </summary>
        private static float s_DebugTipFps;
        /// <summary>
        ///   <para>CPU 占用百分比。</para>
        /// </summary>
        private static float s_DebugTipCpuUsage;
        /// <summary>
        ///   <para>上一帧采样时间（秒）。</para>
        /// </summary>
        private static double s_DebugTipPrevSampleTime;
        /// <summary>
        ///   <para>上一帧 CPU 时间戳。</para>
        /// </summary>
        private static TimeSpan s_DebugTipPrevCpuTime;
        /// <summary>
        ///   <para>GPU 占用百分比（不可用时为 -1）。</para>
        /// </summary>
        private static float s_DebugTipGpuUsage = -1f;
        /// <summary>
        ///   <para>内存总量（MB）。</para>
        /// </summary>
        private static float s_DebugTipMemMB;
        /// <summary>
        ///   <para>折线图弹窗是否显示。</para>
        /// </summary>
        private static bool s_DebugTipChartVisible;
        /// <summary>
        ///   <para>当前折线图指标。</para>
        /// </summary>
        private static DebugTipMetric? s_DebugTipChartMetric;
        /// <summary>
        ///   <para>折线图弹窗位置与尺寸。</para>
        /// </summary>
        private static Rect s_DebugTipChartRect = new Rect(20f, 80f, 320f, 180f);
        /// <summary>
        ///   <para>提示按钮拖拽偏移。</para>
        /// </summary>
        private static Vector2 s_DebugTipOffset;
        /// <summary>
        ///   <para>提示按钮是否正在拖拽。</para>
        /// </summary>
        private static bool s_DebugTipDragging;
        /// <summary>
        ///   <para>拖拽起点偏移。</para>
        /// </summary>
        private static Vector2 s_DebugTipDragOffset;
        /// <summary>
        ///   <para>FPS 采样序列。</para>
        /// </summary>
        private static readonly List<float> s_DebugTipFpsSamples = new();
        /// <summary>
        ///   <para>CPU 采样序列。</para>
        /// </summary>
        private static readonly List<float> s_DebugTipCpuSamples = new();
        /// <summary>
        ///   <para>GPU 采样序列。</para>
        /// </summary>
        private static readonly List<float> s_DebugTipGpuSamples = new();
        /// <summary>
        ///   <para>内存采样序列。</para>
        /// </summary>
        private static readonly List<float> s_DebugTipMemSamples = new();
        /// <summary>
        ///   <para>采样序列容量。</para>
        /// </summary>
        private static int s_DebugTipSampleCapacity = 72;
        /// <summary>
        ///   <para>提示面板行文字样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleTipLineStyle;
        /// <summary>
        ///   <para>提示面板按钮样式。</para>
        /// </summary>
        private static GUIStyle s_DebugConsoleTipButtonStyle;
        /// <summary>
        ///   <para>折线图绘制贴图。</para>
        /// </summary>
        private static Texture2D s_DebugConsoleTipLineTexture;
        /// <summary>
        ///   <para>折线图指标类型。</para>
        /// </summary>
        private enum DebugTipMetric : byte
        {
            /// <summary>
            ///   <para>帧率。</para>
            /// </summary>
            Fps,
            /// <summary>
            ///   <para>CPU。</para>
            /// </summary>
            Cpu,
            /// <summary>
            ///   <para>GPU。</para>
            /// </summary>
            Gpu,
            /// <summary>
            ///   <para>内存。</para>
            /// </summary>
            Mem,
        }

        /// <summary>
        ///   <para>命令调试器开关。</para>
        /// </summary>
        public static KeyCode DebugConsoleToggleKey { get; set; } = KeyCode.BackQuote;

        /// <summary>
        ///   <para>命令调试器是否启用。</para>
        /// </summary>
        public static bool DebugConsoleEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_DebugConsoleEnabled;
            set
            {
                if (s_DebugConsoleEnabled == value) return;
                s_DebugConsoleEnabled = value;
                if (s_DebugConsoleEnabled)
                {
                    EnsureDebugConsoleRunner();
                }
                else
                {
                    CleanupDebugConsoleRunner();
                }
            }
        }
        
        /// <summary>
        ///   <para>提示按钮是否显示。</para>
        /// </summary>
        public static bool DebugConsoleTipEnabled
        {
            get => s_DebugConsoleTipEnabled;
            set => s_DebugConsoleTipEnabled = value;
        }

        /// <summary>
        ///   <para>重置调试控制台状态。</para>
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetDebugConsoleState()
        {
            s_DebugConsoleEnabled = false;
            CleanupDebugConsoleRunner();
            HookDebugConsoleEvents();
        }

        /// <summary>
        ///   <para>订阅调试控制台事件。</para>
        /// </summary>
        private static void HookDebugConsoleEvents()
        {
            OnModulesCreated -= HandleModulesCreatedForDebugConsole;
            OnModulesDestroyed -= HandleModulesDestroyedForDebugConsole;
            OnModulesCreated += HandleModulesCreatedForDebugConsole;
            OnModulesDestroyed += HandleModulesDestroyedForDebugConsole;
        }

        /// <summary>
        ///   <para>模块容器创建后刷新控制台命令。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        private static void HandleModulesCreatedForDebugConsole(GameModulesHandle handle)
        {
            if (!s_DebugConsoleEnabled) return;
            EnsureDebugConsoleRunner();
        }

        /// <summary>
        ///   <para>模块容器销毁后刷新控制台命令。</para>
        /// </summary>
        /// <param name="handle">句柄。</param>
        private static void HandleModulesDestroyedForDebugConsole(GameModulesHandle handle)
        {
            if (!HasActiveModuleHandles())
            {
                CleanupDebugConsoleRunner();
            }
        }

        /// <summary>
        ///   <para>确保调试控制台运行器存在。</para>
        /// </summary>
        private static void EnsureDebugConsoleRunner()
        {
            if (!Application.isPlaying) return;
            DebugConsoleStartup();
            if (s_DebugConsoleRunner != null) return;
            var runnerObj = new GameObject($"[{nameof(DebugConsoleRunner)}]");
            Object.DontDestroyOnLoad(runnerObj);
            s_DebugConsoleRunner = runnerObj.AddComponent<DebugConsoleRunner>();
        }

        /// <summary>
        ///   <para>销毁调试控制台运行器。</para>
        /// </summary>
        private static void CleanupDebugConsoleRunner()
        {
            DebugConsoleShutdown();
            if (s_DebugConsoleRunner != null)
            {
                Object.Destroy(s_DebugConsoleRunner.gameObject);
                s_DebugConsoleRunner = null;
            }
        }

        /// <summary>
        ///   <para>控制台驱动组件。</para>
        /// </summary>
        [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
        private sealed class DebugConsoleRunner : ComponentInstanceBase<DebugConsoleRunner>
        {
            /// <summary>
            ///   <para>绘制界面。</para>
            /// </summary>
            private void OnGUI()
            {
                DebugConsoleDrawGUI();
                DebuggerWindow.DrawGUI();
            }
        }
        
        /// <summary>
        ///   <para>控制台命令信息。</para>
        /// </summary>
        private class DebugConsoleCommandInfo
        {
            /// <summary>
            ///   <para>描述。</para>
            /// </summary>
            public readonly string description;
            /// <summary>
            ///   <para>方法。</para>
            /// </summary>
            public readonly MethodInfo method;
            
            /// <summary>
            ///   <para>创建控制台命令信息。</para>
            /// </summary>
            /// <param name="description">描述。</param>
            /// <param name="method">方法。</param>
            public DebugConsoleCommandInfo(string description, MethodInfo method)
            {
                this.description = description;
                this.method = method;
            }
        }

        /// <summary>
        ///   <para>控制台消息。</para>
        /// </summary>
        private readonly struct DebugConsoleMessage
        {
            /// <summary>
            ///   <para>文本。</para>
            /// </summary>
            public readonly string text;
            /// <summary>
            ///   <para>时间戳。</para>
            /// </summary>
            public readonly DateTime timestamp;

            /// <summary>
            ///   <para>创建控制台消息。</para>
            /// </summary>
            /// <param name="text">要压缩的字符串。</param>
            public DebugConsoleMessage(string text)
            {
                this.text = text;
                timestamp = DateTime.Now;
            }
        }

        /// <summary>
        ///   <para>初始化调试控制台。</para>
        /// </summary>
        private static void DebugConsoleStartup()
        {
            if (s_DebugConsoleInitialized) return;
            s_DebugConsoleCommandsLoaded = false;
            DebugConsoleAddToOutput("Console ready. Type 'help'.");
            s_DebugConsoleInitialized = true;
        }

        /// <summary>
        ///   <para>关闭并清理调试控制台。</para>
        /// </summary>
        private static void DebugConsoleShutdown()
        {
            s_DebugConsoleVisible = false;
            s_DebugConsoleInput = "";
            s_DebugConsoleScrollPosition = Vector2.zero;
            s_DebugConsoleOutputFoldout = true;
            s_DebugConsoleResizing = false;
            s_DebugConsoleOutput.Clear();
            s_DebugConsoleCommands.Clear();
            s_DebugConsoleCommandsLoaded = false;
            s_DebugConsoleInitialized = false;
        }

        /// <summary>
        ///   <para>绘制调试控制台 UI。</para>
        /// </summary>
        private static void DebugConsoleDrawGUI()
        {
            DebugConsoleHandleInputEvents(Event.current);

            DebugConsoleDrawTip(Event.current);

            if (!s_DebugConsoleVisible) return;

            DebugConsoleEnsureStyles();

            float maxHeight = Mathf.Max(s_DebugConsoleMinHeight, Screen.height - 20f);
            float displayHeight = s_DebugConsoleOutputFoldout
                ? Mathf.Clamp(s_DebugConsoleHeight, s_DebugConsoleMinHeight, maxHeight)
                : s_DebugConsoleCollapsedHeight;
            float inputRowHeight = Mathf.Max(24f, s_DebugConsoleFontSize + 8f);

            Rect consoleRect = new Rect(0, Screen.height - displayHeight, Screen.width, displayHeight);
            Rect resizeHandleRect = new Rect(0, consoleRect.y, consoleRect.width, s_DebugConsoleResizeHandleHeight);

            DebugConsoleHandleResize(Event.current, resizeHandleRect, maxHeight);

            GUILayout.BeginArea(consoleRect, s_DebugConsoleStyle);

            GUILayout.BeginVertical();
            DebugConsoleDrawHeaderRow();

            if (s_DebugConsoleOutputFoldout)
            {
                if (s_DebugConsoleSettingsVisible)
                {
                    DebugConsoleDrawSettingsPanel();
                }

                s_DebugConsoleScrollPosition = GUILayout.BeginScrollView(s_DebugConsoleScrollPosition, GUILayout.ExpandHeight(true));

                s_DebugConsoleLabelStyle.normal.textColor = s_DebugConsoleFontColor;
                foreach (var message in s_DebugConsoleOutput)
                {
                    GUILayout.Label($"[{message.timestamp:yyyy-MM-dd HH:mm:ss}] {message.text}", s_DebugConsoleLabelStyle);
                }

                GUILayout.EndScrollView();
            }

            DebugConsoleDrawInputRow(inputRowHeight);
            GUILayout.EndVertical();

            GUILayout.EndArea();

            DebugConsoleConsumeEvents(Event.current, consoleRect);
        }

        /// <summary>
        ///   <para>处理输入事件。</para>
        /// </summary>
        /// <param name="e">事件参数。</param>
        private static void DebugConsoleHandleInputEvents(Event e)
        {
            if (e.isKey && e.type == EventType.KeyDown && e.keyCode == DebugConsoleToggleKey)
            {
                s_DebugConsoleVisible = !s_DebugConsoleVisible;
                e.Use();
                return;
            }

            if (GUI.GetNameOfFocusedControl() == "ConsoleInput" && e.isKey)
            {
                if (e.keyCode == KeyCode.Return)
                {
                    DebugConsoleExecuteInput();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow && !string.IsNullOrWhiteSpace(s_DebugConsoleInput))
                {
                    e.Use();
                }
                else if (e.keyCode == KeyCode.DownArrow && !string.IsNullOrWhiteSpace(s_DebugConsoleInput))
                {
                    e.Use();
                }
            }
        }

        /// <summary>
        ///   <para>处理控制台高度拖拽。</para>
        /// </summary>
        /// <param name="e">事件参数。</param>
        /// <param name="handleRect">句柄区域。</param>
        /// <param name="maxHeight">最大高度。</param>
        private static void DebugConsoleHandleResize(Event e, Rect handleRect, float maxHeight)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && handleRect.Contains(e.mousePosition))
            {
                s_DebugConsoleResizing = true;
                s_DebugConsoleDragStartY = e.mousePosition.y;
                s_DebugConsoleHeightOnDragStart = s_DebugConsoleHeight;
                e.Use();
            }

            if (s_DebugConsoleResizing && e.type == EventType.MouseDrag)
            {
                float delta = s_DebugConsoleDragStartY - e.mousePosition.y;
                s_DebugConsoleHeight = Mathf.Clamp(s_DebugConsoleHeightOnDragStart + delta, s_DebugConsoleMinHeight, maxHeight);
                e.Use();
            }

            if (s_DebugConsoleResizing && e.type == EventType.MouseUp)
            {
                s_DebugConsoleResizing = false;
                e.Use();
            }

#if UNITY_EDITOR
            if (e.type == EventType.Repaint)
            {
                UnityEditor.EditorGUIUtility.AddCursorRect(handleRect, UnityEditor.MouseCursor.ResizeVertical);
            }
#endif
        }

        /// <summary>
        ///   <para>吞掉控制台区域事件。</para>
        /// </summary>
        /// <param name="e">事件参数。</param>
        /// <param name="consoleRect">控制台区域。</param>
        private static void DebugConsoleConsumeEvents(Event e, Rect consoleRect)
        {
            if (e == null) return;
            if (!consoleRect.Contains(e.mousePosition) && !s_DebugConsoleResizing) return;

            if (e.type == EventType.ScrollWheel ||
                e.type == EventType.MouseDown ||
                e.type == EventType.MouseUp ||
                e.type == EventType.MouseDrag)
            {
                e.Use();
            }
        }

        /// <summary>
        ///   <para>绘制提示按钮与折叠信息面板。</para>
        /// </summary>
        /// <param name="e">事件参数。</param>
        private static void DebugConsoleDrawTip(Event e)
        {
            DebugConsoleEnsureStyles();
            if (!s_DebugConsoleTipEnabled) return;
            string tipText = $"[{DebugConsoleToggleKey}] CMD {(s_DebugConsoleVisible ? "ON" : "OFF")}";
            var size = s_DebugConsoleTipStyle.CalcSize(new GUIContent(tipText));
            float padding = 6f;
            float baseX = s_DebugConsoleTipRight ? Screen.width - size.x - padding : padding;
            float baseY = padding;
            var tipPosition = new Vector2(baseX, baseY) + s_DebugTipOffset;
            tipPosition = ClampTipAnchor(tipPosition, size);
            s_DebugTipOffset = tipPosition - new Vector2(baseX, baseY);
            float x = tipPosition.x;
            float y = tipPosition.y;
            var rect = new Rect(x, y, size.x, size.y);
            var dragRect = new Rect(rect.x - 6f, rect.y - 6f, rect.width + 12f, rect.height + 12f);
            if (e != null)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && dragRect.Contains(e.mousePosition) && !rect.Contains(e.mousePosition))
                {
                    s_DebugTipDragging = true;
                    s_DebugTipDragOffset = e.mousePosition - tipPosition;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && s_DebugTipDragging)
                {
                    var newPosition = e.mousePosition - s_DebugTipDragOffset;
                    newPosition = ClampTipAnchor(newPosition, size);
                    s_DebugTipOffset = newPosition - new Vector2(baseX, baseY);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0 && s_DebugTipDragging)
                {
                    s_DebugTipDragging = false;
                    e.Use();
                }
            }
            if (GUI.Button(rect, tipText, s_DebugConsoleTipStyle))
            {
                s_DebugConsoleVisible = !s_DebugConsoleVisible;
            }
            if (e != null && rect.Contains(e.mousePosition) &&
                (e.type == EventType.ScrollWheel || e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag))
            {
                e.Use();
            }
            DrawTipOutline(rect, 1f);
            var arrowText = s_DebugTipFoldout ? "▲" : "▼";
            var arrowSize = s_DebugConsoleTipStyle.CalcSize(new GUIContent(arrowText));
            var arrowRect = new Rect(rect.x, rect.y + rect.height, rect.width, arrowSize.y);
            if (GUI.Button(arrowRect, arrowText, s_DebugConsoleTipStyle))
            {
                s_DebugTipFoldout = !s_DebugTipFoldout;
            }
            if (e != null && arrowRect.Contains(e.mousePosition) &&
                (e.type == EventType.ScrollWheel || e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag))
            {
                e.Use();
            }
            DrawTipOutline(arrowRect, 1f);
            if (s_DebugTipFoldout || s_DebugTipChartVisible)
            {
                UpdateTipMetrics();
            }
            if (s_DebugTipFoldout)
            {
                var lineStyle = s_DebugConsoleTipLineStyle;
                var buttonStyle = s_DebugConsoleTipButtonStyle;
                var lineHeight = Mathf.Max(16f, lineStyle.fontSize + 6f);
                var fpsText = $"FPS: {s_DebugTipFps:0.0}";
                var cpuText = $"CPU: {s_DebugTipCpuUsage:0.0}%";
                var gpuText = s_DebugTipGpuUsage < 0f ? "GPU: N/A" : $"GPU: {s_DebugTipGpuUsage:0.0}%";
                var memText = $"MEM: {s_DebugTipMemMB:0.0} MB";
                var panelWidth = rect.width;
                var panelHeight = lineHeight * 4f + 8f;
                var panelX = s_DebugConsoleTipRight ? rect.x + rect.width - panelWidth : rect.x;
                var panelRect = new Rect(panelX, arrowRect.y + arrowRect.height, panelWidth, panelHeight);
                GUI.Box(panelRect, GUIContent.none, s_DebugConsoleTipStyle);
                DrawTipOutline(panelRect, 1f);
                var labelRect = new Rect(panelRect.x + 6f, panelRect.y + 4f, panelRect.width - 12f, lineHeight);
                if (GUI.Button(labelRect, fpsText, buttonStyle))
                {
                    ToggleTipChart(DebugTipMetric.Fps);
                }
                labelRect.y += lineHeight;
                if (GUI.Button(labelRect, cpuText, buttonStyle))
                {
                    ToggleTipChart(DebugTipMetric.Cpu);
                }
                labelRect.y += lineHeight;
                if (GUI.Button(labelRect, gpuText, buttonStyle))
                {
                    ToggleTipChart(DebugTipMetric.Gpu);
                }
                labelRect.y += lineHeight;
                if (GUI.Button(labelRect, memText, buttonStyle))
                {
                    ToggleTipChart(DebugTipMetric.Mem);
                }
            }
            if (s_DebugTipChartVisible && s_DebugTipChartMetric.HasValue)
            {
                s_DebugTipChartRect = ClampTipChartRect(s_DebugTipChartRect);
                s_DebugTipChartRect = GUI.Window(721357, s_DebugTipChartRect, DebugTipChartWindow, "");
                s_DebugTipChartRect = ClampTipChartRect(s_DebugTipChartRect);
                if (e != null && s_DebugTipChartRect.Contains(e.mousePosition) &&
                    (e.type == EventType.ScrollWheel || e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag))
                {
                    e.Use();
                }
            }
        }

        /// <summary>
        ///   <para>采样并缓存提示面板所需指标。</para>
        /// </summary>
        private static void UpdateTipMetrics()
        {
            var now = Time.unscaledTime;
            if (now < s_DebugTipNextUpdateTime) return;
            s_DebugTipNextUpdateTime = now + s_DebugTipUpdateInterval;
            var fpsInstant = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            s_DebugTipFps = s_DebugTipFps > 0f ? Mathf.Lerp(s_DebugTipFps, fpsInstant, 0.25f) : fpsInstant;
            var proc = Process.GetCurrentProcess();
            var cpuTime = proc.TotalProcessorTime;
            var dt = now - (float)s_DebugTipPrevSampleTime;
            if (dt > 0.0001f)
            {
                var cpuDelta = (float)(cpuTime - s_DebugTipPrevCpuTime).TotalSeconds;
                var cpuPercent = (cpuDelta / (dt * Mathf.Max(1, SystemInfo.processorCount))) * 100f;
                s_DebugTipCpuUsage = Mathf.Clamp(cpuPercent, 0f, 100f);
            }
            s_DebugTipPrevCpuTime = cpuTime;
            s_DebugTipPrevSampleTime = now;
            s_DebugTipMemMB = (GC.GetTotalMemory(false) + Profiler.GetTotalAllocatedMemoryLong()) / 1048576f;
            var vramMB = SystemInfo.graphicsMemorySize;
            if (vramMB > 0)
            {
                var gfxBytes = Profiler.GetAllocatedMemoryForGraphicsDriver();
                var percent = (gfxBytes / (vramMB * 1048576f)) * 100f;
                s_DebugTipGpuUsage = Mathf.Clamp((float)percent, 0f, 100f);
            }
            else
            {
                s_DebugTipGpuUsage = -1f;
            }
            AppendTipSample(s_DebugTipFpsSamples, s_DebugTipFps);
            AppendTipSample(s_DebugTipCpuSamples, s_DebugTipCpuUsage);
            AppendTipSample(s_DebugTipGpuSamples, s_DebugTipGpuUsage);
            AppendTipSample(s_DebugTipMemSamples, s_DebugTipMemMB);
        }

        /// <summary>
        ///   <para>初始化并同步 GUI 样式。</para>
        /// </summary>
        private static void DebugConsoleEnsureStyles()
        {
            if (s_DebugConsoleStyle == null)
            {
                s_DebugConsoleStyle = new GUIStyle(GUI.skin.box);
            }
            if (s_DebugConsoleInputStyle == null)
            {
                s_DebugConsoleInputStyle = new GUIStyle(GUI.skin.textField);
                s_DebugConsoleInputStyle.normal.textColor = s_DebugConsoleFontColor;
                s_DebugConsoleInputStyle.focused.textColor = s_DebugConsoleFontColor;
            }
            if (s_DebugConsoleLabelStyle == null)
            {
                s_DebugConsoleLabelStyle = new GUIStyle(GUI.skin.label);
                s_DebugConsoleLabelStyle.wordWrap = true;
            }
            if (s_DebugConsoleTipStyle == null)
            {
                s_DebugConsoleTipStyle = new GUIStyle(GUI.skin.box);
                s_DebugConsoleTipStyle.normal.textColor = s_DebugConsoleFontColor;
                s_DebugConsoleTipStyle.padding = new RectOffset(6, 6, 4, 4);
            }
            if (s_DebugConsoleSettingsStyle == null)
            {
                s_DebugConsoleSettingsStyle = new GUIStyle(GUI.skin.box);
                s_DebugConsoleSettingsStyle.padding = new RectOffset(8, 8, 6, 6);
            }
            if (s_DebugConsoleInlineLabelStyle == null)
            {
                s_DebugConsoleInlineLabelStyle = new GUIStyle(GUI.skin.label);
                s_DebugConsoleInlineLabelStyle.alignment = TextAnchor.MiddleLeft;
            }
            if (s_DebugConsoleInlineButtonStyle == null)
            {
                s_DebugConsoleInlineButtonStyle = new GUIStyle(GUI.skin.button);
                s_DebugConsoleInlineButtonStyle.alignment = TextAnchor.MiddleCenter;
            }
            if (s_DebugConsoleTipLineStyle == null)
            {
                s_DebugConsoleTipLineStyle = new GUIStyle(GUI.skin.label);
                s_DebugConsoleTipLineStyle.alignment = TextAnchor.MiddleLeft;
                s_DebugConsoleTipLineStyle.wordWrap = false;
            }
            if (s_DebugConsoleTipButtonStyle == null)
            {
                s_DebugConsoleTipButtonStyle = new GUIStyle(GUI.skin.label);
                s_DebugConsoleTipButtonStyle.alignment = TextAnchor.MiddleLeft;
                s_DebugConsoleTipButtonStyle.wordWrap = false;
            }
            if (s_DebugConsoleTipLineTexture == null)
            {
                s_DebugConsoleTipLineTexture = TextureUtility.MakeTex2D(1, 1, Color.white);
            }
            if (s_DebugConsoleBackgroundTexture == null || s_DebugConsoleStyleBackgroundColor != s_DebugConsoleBackgroundColor)
            {
                if (s_DebugConsoleBackgroundTexture != null)
                {
                    Object.Destroy(s_DebugConsoleBackgroundTexture);
                }
                s_DebugConsoleBackgroundTexture = TextureUtility.MakeTex2D(2, 2, s_DebugConsoleBackgroundColor);
                s_DebugConsoleStyle.normal.background = s_DebugConsoleBackgroundTexture;
                s_DebugConsoleTipStyle.normal.background = s_DebugConsoleBackgroundTexture;
                s_DebugConsoleSettingsStyle.normal.background = s_DebugConsoleBackgroundTexture;
                s_DebugConsoleStyleBackgroundColor = s_DebugConsoleBackgroundColor;
            }
            if (s_DebugConsoleStyleFontSize != s_DebugConsoleFontSize ||
                s_DebugConsoleStyleFontColor != s_DebugConsoleFontColor)
            {
                s_DebugConsoleStyleFontSize = s_DebugConsoleFontSize;
                s_DebugConsoleInputStyle.fontSize = s_DebugConsoleFontSize;
                s_DebugConsoleLabelStyle.fontSize = s_DebugConsoleFontSize;
                s_DebugConsoleInputStyle.normal.textColor = s_DebugConsoleFontColor;
                s_DebugConsoleInputStyle.focused.textColor = s_DebugConsoleFontColor;
                s_DebugConsoleTipStyle.normal.textColor = s_DebugConsoleFontColor;
                s_DebugConsoleInlineLabelStyle.fontSize = s_DebugConsoleFontSize;
                s_DebugConsoleInlineButtonStyle.fontSize = s_DebugConsoleFontSize;
                s_DebugConsoleTipLineStyle.fontSize = Mathf.Max(10, s_DebugConsoleFontSize - 4);
                s_DebugConsoleTipButtonStyle.fontSize = Mathf.Max(10, s_DebugConsoleFontSize - 4);
                s_DebugConsoleTipLineStyle.normal.textColor = s_DebugConsoleFontColor;
                s_DebugConsoleTipButtonStyle.normal.textColor = s_DebugConsoleFontColor;
                s_DebugConsoleStyleFontColor = s_DebugConsoleFontColor;
            }
        }

        /// <summary>
        ///   <para>切换提示折线图弹窗。</para>
        /// </summary>
        /// <param name="metric">指标。</param>
        private static void ToggleTipChart(DebugTipMetric metric)
        {
            if (s_DebugTipChartVisible && s_DebugTipChartMetric == metric)
            {
                s_DebugTipChartVisible = false;
                s_DebugTipChartMetric = null;
                return;
            }
            s_DebugTipChartMetric = metric;
            s_DebugTipChartVisible = true;
            s_DebugTipChartRect = ClampTipChartRect(s_DebugTipChartRect);
        }

        /// <summary>
        ///   <para>限制折线图弹窗在屏幕范围内。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        private static Rect ClampTipChartRect(Rect rect)
        {
            float maxWidth = Mathf.Max(120f, Screen.width - 10f);
            float maxHeight = Mathf.Max(80f, Screen.height - 10f);
            rect.width = Mathf.Min(rect.width, maxWidth);
            rect.height = Mathf.Min(rect.height, maxHeight);
            rect.x = Mathf.Clamp(rect.x, 0f, Screen.width - rect.width);
            rect.y = Mathf.Clamp(rect.y, 0f, Screen.height - rect.height);
            return rect;
        }

        /// <summary>
        ///   <para>限制提示锚点。</para>
        /// </summary>
        /// <param name="position">位置。</param>
        /// <param name="size">大小。</param>
        private static Vector2 ClampTipAnchor(Vector2 position, Vector2 size)
        {
            float x = Mathf.Clamp(position.x, 0f, Screen.width - size.x);
            float y = Mathf.Clamp(position.y, 0f, Screen.height - size.y);
            return new Vector2(x, y);
        }

        /// <summary>
        ///   <para>追加采样并保持容量。</para>
        /// </summary>
        /// <param name="samples">采样。</param>
        /// <param name="value">值。</param>
        private static void AppendTipSample(List<float> samples, float value)
        {
            if (samples.Count >= s_DebugTipSampleCapacity)
            {
                samples.RemoveAt(0);
            }
            samples.Add(value);
        }

        /// <summary>
        ///   <para>折线图弹窗内容绘制。</para>
        /// </summary>
        /// <param name="id">标识。</param>
        private static void DebugTipChartWindow(int id)
        {
            var lineStyle = s_DebugConsoleTipLineStyle;
            var title = GetTipMetricTitle(s_DebugTipChartMetric.Value);
            var titleHeight = Mathf.Max(18f, lineStyle.fontSize + 6f);
            var titleRect = new Rect(8f, 6f, s_DebugTipChartRect.width - 16f, titleHeight);
            GUI.Label(titleRect, title, lineStyle);
            var chartRect = new Rect(8f, titleRect.yMax + 6f, s_DebugTipChartRect.width - 16f, s_DebugTipChartRect.height - titleRect.yMax - 14f);
            DrawTipChart(chartRect, GetTipSamples(s_DebugTipChartMetric.Value));
            GUI.DragWindow(new Rect(0f, 0f, s_DebugTipChartRect.width, titleHeight + 6f));
        }

        /// <summary>
        ///   <para>获取指标标题。</para>
        /// </summary>
        /// <param name="metric">指标。</param>
        private static string GetTipMetricTitle(DebugTipMetric metric)
        {
            switch (metric)
            {
                case DebugTipMetric.Fps: return "FPS";
                case DebugTipMetric.Cpu: return "CPU %";
                case DebugTipMetric.Gpu: return "GPU %";
                case DebugTipMetric.Mem: return "MEM MB";
                default: return "";
            }
        }

        /// <summary>
        ///   <para>获取对应采样序列。</para>
        /// </summary>
        /// <param name="metric">指标。</param>
        private static List<float> GetTipSamples(DebugTipMetric metric)
        {
            switch (metric)
            {
                case DebugTipMetric.Fps: return s_DebugTipFpsSamples;
                case DebugTipMetric.Cpu: return s_DebugTipCpuSamples;
                case DebugTipMetric.Gpu: return s_DebugTipGpuSamples;
                case DebugTipMetric.Mem: return s_DebugTipMemSamples;
                default: return s_DebugTipFpsSamples;
            }
        }

        /// <summary>
        ///   <para>绘制折线图。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="samples">采样。</param>
        private static void DrawTipChart(Rect rect, List<float> samples)
        {
            GUI.Box(rect, GUIContent.none, s_DebugConsoleTipStyle);
            if (samples == null || samples.Count < 2)
            {
                GUI.Label(rect, "N/A", s_DebugConsoleTipLineStyle);
                return;
            }
            float min = float.MaxValue;
            float max = float.MinValue;
            bool hasValid = false;
            for (int i = 0; i < samples.Count; i++)
            {
                var value = samples[i];
                if (value < 0f) continue;
                hasValid = true;
                if (value < min) min = value;
                if (value > max) max = value;
            }
            if (!hasValid)
            {
                GUI.Label(rect, "N/A", s_DebugConsoleTipLineStyle);
                return;
            }
            if (Mathf.Abs(max - min) < 0.001f) max = min + 1f;
            Vector2 prev = Vector2.zero;
            bool hasPrev = false;
            for (int i = 0; i < samples.Count; i++)
            {
                var value = samples[i];
                if (value < 0f) continue;
                float x = rect.x + (rect.width * i / Mathf.Max(1, samples.Count - 1));
                float y = rect.y + rect.height - ((value - min) / (max - min)) * rect.height;
                var current = new Vector2(x, y);
                if (hasPrev)
                {
                    DrawTipLine(prev, current, 1.5f, s_DebugConsoleFontColor);
                }
                prev = current;
                hasPrev = true;
            }
        }

        /// <summary>
        ///   <para>绘制提示线段。</para>
        /// </summary>
        /// <param name="start">开始。</param>
        /// <param name="end">结束。</param>
        /// <param name="width">宽度。</param>
        /// <param name="color">颜色。</param>
        private static void DrawTipLine(Vector2 start, Vector2 end, float width, Color color)
        {
            var savedMatrix = GUI.matrix;
            var savedColor = GUI.color;
            var direction = end - start;
            float length = direction.magnitude;
            if (length <= 0.01f)
            {
                GUI.matrix = savedMatrix;
                GUI.color = savedColor;
                return;
            }
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            GUI.color = color;
            var pivot = start;
            GUIUtility.RotateAroundPivot(angle, pivot);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, length, width), s_DebugConsoleTipLineTexture);
            GUI.matrix = savedMatrix;
            GUI.color = savedColor;
        }

        /// <summary>
        ///   <para>绘制提示框外框。</para>
        /// </summary>
        /// <param name="rect">区域。</param>
        /// <param name="thickness">厚度。</param>
        private static void DrawTipOutline(Rect rect, float thickness)
        {
            var color = GUI.color;
            GUI.color = s_DebugConsoleFontColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), s_DebugConsoleTipLineTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), s_DebugConsoleTipLineTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), s_DebugConsoleTipLineTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), s_DebugConsoleTipLineTexture);
            GUI.color = color;
        }

        /// <summary>
        ///   <para>设置面板：用于调整调试器显示参数。</para>
        /// </summary>
        private static void DebugConsoleDrawSettingsPanel()
        {
            GUILayout.BeginVertical(s_DebugConsoleSettingsStyle);
            float panelHeight = Mathf.Min(180f, s_DebugConsoleHeight * 0.4f);
            s_DebugConsoleSettingsScrollPosition = GUILayout.BeginScrollView(s_DebugConsoleSettingsScrollPosition, GUILayout.Height(panelHeight));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Font Size", GUILayout.Width(90));
            s_DebugConsoleFontSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(s_DebugConsoleFontSize, 10f, 32f));
            GUILayout.Label(s_DebugConsoleFontSize.ToString(), GUILayout.Width(30));
            GUILayout.EndHorizontal();

            DebugConsoleDrawColorSliders("Font Color", ref s_DebugConsoleFontColor, true);
            DebugConsoleDrawColorSliders("Background", ref s_DebugConsoleBackgroundColor, false);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>
        ///   <para>颜色滑条：RGB + Alpha（背景使用）。</para>
        /// </summary>
        /// <param name="label">标签。</param>
        /// <param name="color">颜色。</param>
        /// <param name="alphaReadonly">是否禁止修改透明度。</param>
        private static void DebugConsoleDrawColorSliders(string label, ref Color color, bool alphaReadonly)
        {
            GUILayout.Label(label);
            color.r = DebugConsoleDrawColorSlider("R", color.r);
            color.g = DebugConsoleDrawColorSlider("G", color.g);
            color.b = DebugConsoleDrawColorSlider("B", color.b);
            if (!alphaReadonly)
            {
                color.a = DebugConsoleDrawColorSlider("A", color.a);
            }
        }

        /// <summary>
        ///   <para>调试控制台绘制颜色滑动条。</para>
        /// </summary>
        /// <param name="label">标签。</param>
        /// <param name="value">值。</param>
        private static float DebugConsoleDrawColorSlider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(16));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.Label(value.ToString("0.00"), GUILayout.Width(38));
            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        ///   <para>调试控制台绘制表头行。</para>
        /// </summary>
        private static void DebugConsoleDrawHeaderRow()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(s_DebugConsoleSettingsVisible ? "✖" : "...", GUILayout.Width(32f), GUILayout.Height(22f)))
            {
                s_DebugConsoleSettingsVisible = !s_DebugConsoleSettingsVisible;
            }
            if (GUILayout.Button(s_DebugConsoleOutputFoldout ? "▲" : "▼", GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
            {
                s_DebugConsoleOutputFoldout = !s_DebugConsoleOutputFoldout;
            }
            if (GUILayout.Button(DebuggerWindow.Visible ? "■" : "□", GUILayout.Width(32f), GUILayout.Height(22f)))
            {
                DebuggerWindow.Toggle();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>调试控制台绘制输入行。</para>
        /// </summary>
        /// <param name="inputRowHeight">输入行高度。</param>
        private static void DebugConsoleDrawInputRow(float inputRowHeight)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(">", s_DebugConsoleInlineLabelStyle, GUILayout.Width(15), GUILayout.Height(inputRowHeight));

            GUI.SetNextControlName("ConsoleInput");
            s_DebugConsoleInput = GUILayout.TextField(s_DebugConsoleInput, s_DebugConsoleInputStyle, GUILayout.ExpandWidth(true), GUILayout.Height(inputRowHeight));

            if (GUILayout.Button("RUN", s_DebugConsoleInlineButtonStyle, GUILayout.Width(85), GUILayout.Height(inputRowHeight)))
            {
                DebugConsoleExecuteInput();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///   <para>调试控制台执行输入。</para>
        /// </summary>
        private static void DebugConsoleExecuteInput()
        {
            if (string.IsNullOrWhiteSpace(s_DebugConsoleInput)) return;

            DebugConsoleAddToOutput($"> {s_DebugConsoleInput}");

            try
            {
                object result = DebugConsoleExecuteCommand(s_DebugConsoleInput);

                if (result != null)
                {
                    DebugConsoleAddToOutput($"{result}");
                }
            }
            catch (Exception ex)
            {
                DebugConsoleAddToOutput($"Command error: {ex.Message}");
            }

            s_DebugConsoleInput = "";
            s_DebugConsoleScrollPosition = new Vector2(0, float.MaxValue);
        }

        /// <summary>
        ///   <para>向控制台追加输出。</para>
        /// </summary>
        /// <param name="message">消息内容。</param>
        private static void DebugConsoleAddToOutput(string message)
        {
            s_DebugConsoleOutput.Add(new DebugConsoleMessage(message));

            if (s_DebugConsoleOutput.Count > 100)
            {
                s_DebugConsoleOutput.RemoveAt(0);
            }
        }

        /// <summary>
        ///   <para>查找全部控制台命令。</para>
        /// </summary>
        private static Dictionary<string, DebugConsoleCommandInfo> DebugConsoleFindAllConsoleCommand()
        {
            var commands = new Dictionary<string, DebugConsoleCommandInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in DebugGetFilteredAssemblies())
            foreach (var type in assembly.GetTypes())
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                if (attribute == null) continue;
                if (string.IsNullOrWhiteSpace(attribute.command))
                    throw new InvalidOperationException($"控制台命令名称为空：{type.FullName}.{method.Name}。");
                if (!commands.TryAdd(attribute.command, new DebugConsoleCommandInfo(attribute.description, method)))
                    throw new InvalidOperationException($"控制台命令名称重复：{attribute.command}（{type.FullName}.{method.Name}）。");
            }

            return commands;
        }

        /// <summary>
        ///   <para>调试获取筛选后的程序集。</para>
        /// </summary>
        /// <param name="forceReload">是否强制重新加载。</param>
        internal static IReadOnlyList<Assembly> DebugGetFilteredAssemblies(bool forceReload = false)
        {
            if (!forceReload && s_DebugAssemblyCacheReady && s_DebugAssemblyCache != null) return s_DebugAssemblyCache;
            s_DebugAssemblyCacheReady = true;
            if (s_DebugAssemblyCache == null) s_DebugAssemblyCache = new List<Assembly>(64);
            else s_DebugAssemblyCache.Clear();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (assembly.IsDynamic) continue;
                string assemblyName = assembly.GetName().Name;
                if (string.IsNullOrEmpty(assemblyName)) continue;
                if (s_DebugConsoleAssemblyExcludeNames.Contains(assemblyName)) continue;
                if (DebugConsoleIsAssemblyNameExcluded(assemblyName)) continue;
                s_DebugAssemblyCache.Add(assembly);
            }
            return s_DebugAssemblyCache;
        }

        /// <summary>
        ///   <para>按需加载控制台命令。</para>
        /// </summary>
        private static void DebugConsoleEnsureCommandsLoaded()
        {
            if (s_DebugConsoleCommandsLoaded) return;
            s_DebugConsoleCommands = DebugConsoleFindAllConsoleCommand();
            s_DebugConsoleCommandsLoaded = true;
        }

        /// <summary>
        ///   <para>判断程序集名称是否被排除。</para>
        /// </summary>
        /// <param name="assemblyName">程序集名称。</param>
        private static bool DebugConsoleIsAssemblyNameExcluded(string assemblyName)
        {
            for (int i = 0; i < s_DebugConsoleAssemblyExcludePrefixes.Length; i++)
            {
                if (assemblyName.StartsWith(s_DebugConsoleAssemblyExcludePrefixes[i], StringComparison.Ordinal))
                    return true;
            }

            for (int i = 0; i < s_DebugConsoleAssemblyExcludeRegex.Length; i++)
            {
                if (s_DebugConsoleAssemblyExcludeRegex[i].IsMatch(assemblyName))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>调试控制台执行命令。</para>
        /// </summary>
        /// <param name="commandLine">命令行。</param>
        private static object DebugConsoleExecuteCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return null;
            DebugConsoleEnsureCommandsLoaded();

            string[] parts = Regex.Split(commandLine.Trim(), @"\s+(?=(?:[^""]*""[^""]*"")*[^""]*$)");
            string command = parts[0];
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            if (s_DebugConsoleCommands.TryGetValue(command, out DebugConsoleCommandInfo info))
            {
                var parameters = info.method.GetParameters();
                if (args.Length > parameters.Length)
                    return $"Invalid arguments: expected at most {parameters.Length} parameter(s)";
                object[] convertedArgs = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i < args.Length)
                    {
                        if (args[i].StartsWith("\"") && args[i].EndsWith("\""))
                        {
                            args[i] = args[i].Substring(1, args[i].Length - 2);
                        }

                        convertedArgs[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                    }
                    else if (parameters[i].HasDefaultValue)
                    {
                        convertedArgs[i] = parameters[i].DefaultValue;
                    }
                    else
                    {
                        return $"Invalid arguments: expected {parameters.Length} parameter(s)";
                    }
                }

                return info.method.Invoke(null, convertedArgs);
            }

            return $"Unknown command: {command}";
        }

        #region 内置命令

        /// <summary>
        ///   <para>帮助命令。</para>
        /// </summary>
        [ConsoleCommand("help", "显示所有命令")]
        private static string HelpCommand()
        {
            DebugConsoleEnsureCommandsLoaded();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("可用命令:");
            var keys = new List<string>(s_DebugConsoleCommands.Count);
            foreach (var pair in s_DebugConsoleCommands)
            {
                keys.Add(pair.Key);
            }
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var info = s_DebugConsoleCommands[key];
                sb.AppendLine($"    {key} - {info.description}");
            }
            return sb.ToString();
        }

        /// <summary>
        ///   <para>清空命令。</para>
        /// </summary>
        [ConsoleCommand("clear", "清理输出命令")]
        private static void ClearCommand() => s_DebugConsoleOutput.Clear();

        /// <summary>
        ///   <para>重新加载命令。</para>
        /// </summary>
        [ConsoleCommand("reload_commands", "重新加载命令")]
        private static string ReloadCommandsCommand()
        {
            s_DebugConsoleCommands = DebugConsoleFindAllConsoleCommand();
            s_DebugConsoleCommandsLoaded = true;
            return $"已加载完成 {s_DebugConsoleCommands.Count} 个命令";
        }

        /// <summary>
        ///   <para>时间缩放命令。</para>
        /// </summary>
        /// <param name="timeScale">时间缩放。</param>
        [ConsoleCommand("time_scale", "设置时间缩放（格式: time_scale [值]）")]
        private static string TimeScaleCommand(float timeScale = 1.0f)
        {
            timeScale = Mathf.Max(timeScale, 0.0f);
            Time.timeScale = timeScale;
            return $"时间缩放设置为: {Time.timeScale}";
        }

        /// <summary>
        ///   <para>帧率命令。</para>
        /// </summary>
        /// <param name="targetFrameRate">目标帧率。</param>
        [ConsoleCommand("fps", "显示或设置目标帧率（格式: fps [目标帧率]）")]
        private static string FPSCommand(int targetFrameRate = -1)
        {
            if (targetFrameRate == -1)
            {
                return $"当前帧率: {1f / Time.deltaTime:F1}, 目标帧率: {Application.targetFrameRate}";
            }
            Application.targetFrameRate = targetFrameRate;
            return $"目标帧率设置为: {Application.targetFrameRate}";
        }

        /// <summary>
        ///   <para>信息命令。</para>
        /// </summary>
        [ConsoleCommand("info", "显示系统信息")]
        private static string InfoCommand()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"设备名称: {SystemInfo.deviceName}");
            sb.AppendLine($"设备型号: {SystemInfo.deviceModel}");
            sb.AppendLine($"操作系统: {SystemInfo.operatingSystem}");
            sb.AppendLine($"处理器: {SystemInfo.processorType}");
            sb.AppendLine($"内存: {SystemInfo.systemMemorySize} MB");
            sb.AppendLine($"显卡: {SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"显存: {SystemInfo.graphicsMemorySize} MB");
            sb.AppendLine($"屏幕分辨率: {Screen.width}x{Screen.height}");
            return sb.ToString();
        }

        /// <summary>
        ///   <para>垃圾回收命令。</para>
        /// </summary>
        [ConsoleCommand("gc", "执行垃圾回收")]
        private static string GCCommand()
        {
            long startMem = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long endMem = GC.GetTotalMemory(true);
            return $"垃圾回收完成 | 释放: {(startMem - endMem) / 1024:F1}KB | 当前: {endMem / 1024:F1}KB";
        }

        /// <summary>
        ///   <para>音量命令。</para>
        /// </summary>
        /// <param name="volume">音量。</param>
        [ConsoleCommand("volume", "设置全局音量（格式: volume [0.0 - 1.0]）")]
        private static string VolumeCommand(float volume)
        {
            volume = Mathf.Clamp(volume, 0f, 1f);
            AudioListener.volume = volume;
            return $"音量设置为: {AudioListener.volume:F2}";
        }

        /// <summary>
        ///   <para>重置场景命令。</para>
        /// </summary>
        [ConsoleCommand("reset_scene", "重置当前场景")]
        private static string ResetSceneCommand()
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
            return $"场景 '{currentSceneName}' 已重置";
        }

        /// <summary>
        ///   <para>分辨率命令。</para>
        /// </summary>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <param name="fullscreen">全屏。</param>
        [ConsoleCommand("resolution", "设置分辨率 (格式: resolution 宽 高 [全屏])")]
        private static string ResolutionCommand(int width, int height, bool fullscreen = false)
        {
            if (width < 100 || height < 100)
                return $"分辨率错误: 宽或高过低";

            Screen.SetResolution(width, height, fullscreen);
            return $"分辨率已设置: {Screen.width}x{Screen.height} {(fullscreen ? "全屏" : "窗口")}";
        }

        /// <summary>
        ///   <para>连通性测试命令。</para>
        /// </summary>
        /// <param name="host">主机。</param>
        [ConsoleCommand("ping", "测试网络延迟（格式: ping [地址]）")]
        private static string PingCommand(string host = "8.8.8.8")
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return "WebGL平台不支持网络诊断命令";

            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = ping.Send(host, 2000);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success
                ? $"{host} 延迟: {reply.RoundtripTime}ms (TTL: {reply.Options.Ttl})"
                : $"无法 ping 通 {host}: {reply.Status}";
        }

        /// <summary>
        ///   <para>退出命令。</para>
        /// </summary>
        [ConsoleCommand("quit", "退出应用程序")]
        private static void QuitCommand()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}

#endif
