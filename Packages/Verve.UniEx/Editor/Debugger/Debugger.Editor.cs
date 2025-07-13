#if UNITY_EDITOR
    
namespace VerveEditor.UniEx.Debugger
{
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Verve.Debug;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    
    
    internal sealed partial class DebuggerEditor
    {
        /// <summary>
        /// 跳过的调试器类文件列表
        /// </summary>
        private static readonly HashSet<string> DEBUGGER_CLASS_NAMES = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<SkipInStackTraceAttribute>() != null)
                .Select(t => t.GetCustomAttribute<SkipInStackTraceAttribute>()?.ClassName ?? t.Name),
            StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 调用栈正则
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
            var fieldInfo = consoleWindowType.GetField("m_ActiveText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
            return consoleWindow != null ? fieldInfo?.GetValue(consoleWindow)?.ToString() : null;
        }
    }
}
    
#endif