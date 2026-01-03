#if UNITY_EDITOR
    
namespace Verve.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    
    
    /// <summary>
    ///   <para>调试器编辑器</para>
    /// </summary>
    internal sealed partial class LoggerEditor
    {
        /// <summary>
        ///   <para>跳过的调试器类文件列表</para>
        /// </summary>
        private static readonly HashSet<string> DEBUGGER_CLASS_NAMES = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<SkipInStackTraceAttribute>() != null)
                .Select(t => t.GetCustomAttribute<SkipInStackTraceAttribute>()?.ClassName ?? t.Name),
            StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///   <para>调用栈正则</para>
        /// </summary>
        private static readonly Regex STACK_TRACE_REGEX = new Regex(
            @"\(at (.+?):(\d+)\)", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
        
        
        [UnityEditor.Callbacks.OnOpenAsset(0)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            if (!IsConsoleWindowFocused()) 
                return false;
    
            var stackTrace = GetStackTrace();
            if (string.IsNullOrEmpty(stackTrace))
                return false;
    
            if (TryFindValidStackTraceEntry(stackTrace, out var filePath, out var lineNumber))
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNumber);
                return true;
            }
    
            return false;
        }
    
        private static bool TryFindValidStackTraceEntry(string stackTrace, out string filePath, out int lineNumber)
        {
            filePath = null;
            lineNumber = 0;

            foreach (Match match in STACK_TRACE_REGEX.Matches(stackTrace))
            {
                var relativePath = match.Groups[1].Value;
                var absolutePath = ConvertToAbsolutePath(relativePath);
                var lineStr = match.Groups[2].Value;

                if (!int.TryParse(lineStr, out var currentLine)
                    || DEBUGGER_CLASS_NAMES.Contains(Path.GetFileNameWithoutExtension(absolutePath))) 
                    continue;

                filePath = absolutePath;
                lineNumber = currentLine;
                return true;
            }
    
            return false;
        }
    
        private static string ConvertToAbsolutePath(string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;
        
            if (relativePath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
            {
                var projectPath = Application.dataPath[..Application.dataPath.LastIndexOf("Assets", System.StringComparison.Ordinal)];
                return Path.Combine(projectPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            }
        
            return Path.GetFullPath(relativePath);
        }

        private static bool IsConsoleWindowFocused()
        {
            var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
            return consoleWindow != null && consoleWindow == EditorWindow.focusedWindow;
        }
    
        private static string GetStackTrace()
        {
            var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            var fieldInfo = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
            var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
            return consoleWindow != null ? fieldInfo?.GetValue(consoleWindow)?.ToString() : null;
        }
    }
}
    
#endif