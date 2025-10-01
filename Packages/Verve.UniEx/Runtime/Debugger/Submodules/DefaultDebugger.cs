#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Debug
{
    using System;
    using System.Linq;
    using UnityEngine;
    using Verve.Debug;
    using System.Text;
    using System.Reflection;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Object = UnityEngine.Object;
    using Debug = UnityEngine.Debug;


    /// <summary>
    /// 默认调试器
    /// </summary>
    [System.Serializable, SkipInStackTrace, GameFeatureSubmodule(typeof(DebugGameFeature), Description = "默认调试器")]
    public sealed partial class DefaultDebugger : DebuggerSubmodule, IDrawableSubmodule
    {
        [NonSerialized, Tooltip("是否显示控制台")] private bool m_ConsoleVisible;
        [NonSerialized, Tooltip("控制台输出")] private List<ConsoleMessage> m_ConsoleOutput = new List<ConsoleMessage>();
        [NonSerialized, Tooltip("控制台输入")] private string m_ConsoleInput = "";
        [NonSerialized, Tooltip("控制台滚动位置")] private Vector2 m_ConsoleScrollPosition = Vector2.zero;
        [NonSerialized, Tooltip("输出是否折叠")] private bool m_OutputFoldout = true;
        [NonSerialized, Tooltip("命令集")] private static Dictionary<string, CommandInfo> s_Commands =
            new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);

        
        private class CommandInfo
        {
            public string Description;
            public MethodInfo Method;
        }
        
        /// <summary>
        /// 命令控制台信息
        /// </summary>
        private struct ConsoleMessage
        {
            public string Text;
            public LogLevel Level;
            public float Timestamp;
            
            public ConsoleMessage(string text, LogLevel level = LogLevel.Log)
            {
                Text = text;
                Level = level;
                Timestamp = Time.realtimeSinceStartup;
            }
        }

        
        protected override IEnumerator OnStartup()
        {
            s_Commands = FindAllConsoleCommand();
            AddToConsoleOutput("调试控制台已启动，输入 'help' 查看可用命令。");
            yield return base.OnStartup();
        }

        protected override void OnTick(in GameFeatureContext ctx)
        {
            base.OnTick(in ctx);
// #if ENABLE_LEGACY_INPUT_MANAGER
//             if (Input.GetKeyDown(Component.commandToggleKey))
//             {
//                 m_ConsoleVisible = !m_ConsoleVisible;
//             }
// #endif
        }

        protected override void OnShutdown()
        {
            m_ConsoleVisible = false;
            m_ConsoleInput = "";
            base.OnShutdown();
        }

        void IDrawableSubmodule.DrawGUI()
        {
            HandleConsoleInputEvents(Event.current);

            if (!m_ConsoleVisible) return;

            GUIStyle consoleStyle = new GUIStyle(GUI.skin.box);
            consoleStyle.normal.background = MakeTex(2, 2, new Color(0.0f, 0.0f, 0.0f, 0.6f));
            
            GUIStyle inputStyle = new GUIStyle(GUI.skin.textField);
            inputStyle.normal.textColor = Color.white;
            inputStyle.focused.textColor = Color.white;
            inputStyle.fontSize = 16;
            
            float consoleHeight = Screen.height / 3f;
            
            float displayHeight = m_OutputFoldout ? consoleHeight : 50.0f;
            
            GUILayout.BeginArea(new Rect(0, Screen.height - displayHeight, Screen.width, displayHeight), consoleStyle);
            
            if (m_OutputFoldout)
            {
                GUILayout.BeginVertical();
                
                if (GUILayout.Button(m_OutputFoldout ? "▲" : "▼", GUILayout.Height(15)))
                {
                    m_OutputFoldout = !m_OutputFoldout;
                }
                
                m_ConsoleScrollPosition = GUILayout.BeginScrollView(m_ConsoleScrollPosition, GUILayout.ExpandHeight(true));
                
                foreach (var message in m_ConsoleOutput)
                {
                    Color messageColor = GetMessageColor(message.Level);
                    GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                    labelStyle.normal.textColor = messageColor;
                    labelStyle.wordWrap = true;
                    labelStyle.fontSize = 16;
                    GUILayout.Label($"[{message.Timestamp:F2}] {message.Text}", labelStyle);
                }
                
                GUILayout.EndScrollView();
                
                GUILayout.BeginHorizontal();
                
                GUILayout.Label(">", GUILayout.Width(15));
                
                GUI.SetNextControlName("ConsoleInput");
                m_ConsoleInput = GUILayout.TextField(m_ConsoleInput, inputStyle, GUILayout.ExpandWidth(true));
                
                if (GUILayout.Button("执行", GUILayout.Width(70)))
                {
                    ExecuteConsoleInput();
                }
                
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical();
                
                if (GUILayout.Button(m_OutputFoldout ? "▲" : "▼", GUILayout.Height(15)))
                {
                    m_OutputFoldout = !m_OutputFoldout;
                }
                
                GUILayout.BeginHorizontal();
                
                GUILayout.Label(">", GUILayout.Width(15));
                
                GUI.SetNextControlName("ConsoleInput");
                m_ConsoleInput = GUILayout.TextField(m_ConsoleInput, inputStyle, GUILayout.ExpandWidth(true));
                
                if (GUILayout.Button("执行", GUILayout.Width(70)))
                {
                    ExecuteConsoleInput();
                }
                
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            
            GUILayout.EndArea();

            if (GUI.GetNameOfFocusedControl() != "ConsoleInput")
            {
                GUI.FocusControl("ConsoleInput");
            }
        }
        
        void IDrawableSubmodule.DrawGizmos() { }

        /// <summary>
        /// 处理控制台输入事件
        /// </summary>
        private void HandleConsoleInputEvents(Event e)
        {
            if (e.isKey && e.type == EventType.KeyDown && e.keyCode == Component.commandToggleKey)
            {
                m_ConsoleVisible = !m_ConsoleVisible;
                e.Use();
                return;
            }
            
            if (GUI.GetNameOfFocusedControl() == "ConsoleInput" && e.isKey)
            {
                if (e.keyCode == KeyCode.Return)
                {
                    ExecuteConsoleInput();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow && !string.IsNullOrWhiteSpace(m_ConsoleInput))
                {
                    // TODO: 历史记录
                    e.Use();
                }
                else if (e.keyCode == KeyCode.DownArrow && !string.IsNullOrWhiteSpace(m_ConsoleInput))
                {
                    // TODO: 历史记录
                    e.Use();
                }
            }
        }

        /// <summary>
        /// 执行控制台输入
        /// </summary>
        private void ExecuteConsoleInput()
        {
            if (string.IsNullOrWhiteSpace(m_ConsoleInput)) return;
            
            AddToConsoleOutput($"> {m_ConsoleInput}");
            
            try
            {
                object result = ExecuteCommand(m_ConsoleInput);
    
                if (result != null)
                {
                    AddToConsoleOutput($"{result}");
                }
            }
            catch (Exception ex)
            {
                AddToConsoleOutput($"命令执行错误: {ex.Message}", LogLevel.Error);
            }
            
            m_ConsoleInput = "";
            m_ConsoleScrollPosition = new Vector2(0, float.MaxValue);
        }
        
        /// <summary>
        /// 添加消息到控制台
        /// </summary>
        private void AddToConsoleOutput(string message, LogLevel level = LogLevel.Log)
        {
            m_ConsoleOutput.Add(new ConsoleMessage(message, level));
            
            if (m_ConsoleOutput.Count > 100)
            {
                m_ConsoleOutput.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 根据日志级别获取消息颜色
        /// </summary>
        private Color GetMessageColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error: return Color.red;
                case LogLevel.Warning: return Color.yellow;
                case LogLevel.Log: return Color.white;
                default: return Color.gray;
            }
        }
        
        /// <summary>
        /// 创建纯色纹理
        /// </summary>
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        [System.Diagnostics.DebuggerHidden, System.Diagnostics.DebuggerStepThrough]
        protected override void InternalLog_Implement(string msg, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(msg);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(msg);
                    break;
                case LogLevel.Log:
                    Debug.Log(msg);
                    break;
            }
        }

        [System.Diagnostics.DebuggerHidden, System.Diagnostics.DebuggerStepThrough]
        public override void Assert(bool condition, object msg)
        {
            if (!IsEnabled || msg == null) return;
            Debug.Assert(condition, msg?.ToString());
        }
        
        /// <summary>
        /// 查找所有控制台命令
        /// </summary>
        private static Dictionary<string, CommandInfo> FindAllConsoleCommand()
        {
            var commands = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && 
                            !a.FullName.Contains("Unity") && 
                            !a.FullName.StartsWith("System.") && 
                            !a.FullName.StartsWith("Microsoft.") &&
                            !a.FullName.StartsWith("Mono."));
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var staticMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                            .Where(m => m.GetCustomAttribute<ConsoleCommandAttribute>() != null && !string.IsNullOrWhiteSpace(m.GetCustomAttribute<ConsoleCommandAttribute>().Command));

                        foreach (var method in staticMethods)
                        {
                            var attr = method.GetCustomAttribute<ConsoleCommandAttribute>();
                            var commandKey = attr.Command.ToLower();
                            if (commands.ContainsKey(commandKey)) continue;

                            commands.Add(commandKey, new CommandInfo()
                            {
                                Description = attr.Description,
                                Method = method,
                            });
                        }
                    }
                }
                catch { }
            }

            return commands;
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="commandLine">命令行</param>
        /// <returns></returns>
        private static object ExecuteCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return null;
            
            string[] parts = Regex.Split(commandLine.Trim(), @"\s+(?=(?:[^""]*""[^""]*"")*[^""]*$)");
            string command = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            if (s_Commands.TryGetValue(command, out CommandInfo info))
            {
                var parameters = info.Method.GetParameters();
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
                        return $"参数错误: 参数不足，需要 {parameters.Length} 个参数";
                    }
                }

                return info.Method.Invoke(null, convertedArgs);
            }

            return $"未知命令: {command}";
        }

        #region 内置命令

        [ConsoleCommand("help", "显示所有命令")]
        private static string HelpCommand()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("可用命令:");
            foreach (var command in s_Commands.OrderBy(c => c.Key))
            {
                sb.AppendLine($"    {command.Key} - {command.Value.Description}");
            }
            return sb.ToString();
        }

        [ConsoleCommand("time_scale", "设置时间缩放（格式: time_scale [值]）")]
        private static string TimeScaleCommand(float timeScale = 1.0f)
        {
            timeScale = Mathf.Max(timeScale, 0.0f);
            Time.timeScale = timeScale;
            return $"时间缩放设置为: {Time.timeScale}";
        }

        [ConsoleCommand("fps", "显示或设置目标帧率（格式: fps [目标帧率]）")]
        private static string FPSCommand(int targetFrameRate = -1)
        {
            if (targetFrameRate == -1)
            {
                return $"当前帧率: {1f / Time.deltaTime:F1}, 目标帧率: {Application.targetFrameRate}";
            }
            else
            {
                Application.targetFrameRate = targetFrameRate;
                return $"目标帧率设置为: {Application.targetFrameRate}";
            }
        }
        
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
            sb.AppendLine($"帧率: {1f / Time.deltaTime:F1}");
            return sb.ToString();
        }
        
        [ConsoleCommand("gc", "执行垃圾回收")]
        private static string GCCommand()
        {
            long startMem = GC.GetTotalMemory(false);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long endMem = GC.GetTotalMemory(true);
            return $"垃圾回收完成 | 释放: {(startMem - endMem)/1024:F1}KB | 当前: {endMem/1024:F1}KB";
        }

        [ConsoleCommand("volume", "设置全局音量（格式: volume [0.0 - 1.0]）")]
        private static string VolumeCommand(float volume)
        {
            volume = Mathf.Clamp(volume, 0f, 1f);
            AudioListener.volume = volume;
            return $"音量设置为: {AudioListener.volume:F2}";
        }

        [ConsoleCommand("reset_scene", "重置当前场景")]
        private static string ResetSceneCommand()
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
            return $"场景 '{currentSceneName}' 已重置";
        }
        
        [ConsoleCommand("resolution", "设置分辨率 (格式: resolution 宽 高 [全屏])")]
        private static string ResolutionCommand(int width, int height, bool fullscreen = false)
        {
            if (width < 100 || height < 100) 
                return $"分辨率错误: 宽或高过低";
            
            Screen.SetResolution(width, height, fullscreen);
            return $"分辨率已设置: {Screen.width}x{Screen.height} {(fullscreen ? "全屏" : "窗口")}";
        }
        
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