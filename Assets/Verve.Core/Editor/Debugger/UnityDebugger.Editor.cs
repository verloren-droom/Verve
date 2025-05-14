namespace Verve.Debugger
{
    
#if UNITY_EDITOR
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using System.Text.RegularExpressions;
    
    
    internal sealed partial class UnityDebugger
    {
        /// <summary>
        /// 跳过的调试器类文件列表
        /// </summary>
        private static readonly string[] DEBUGGER_CLASS_NAMES = 
        {
            nameof(DebuggerBase) + ".cs",
            nameof(UnityDebugger) + ".cs",
            nameof(DebuggerUnit) + ".cs",
        };

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

                if (!int.TryParse(lineStr, out var currentLine)) 
                    continue;

                if (IsDebuggerClass(absolutePath))
                    continue;

                filePath = absolutePath;
                lineNumber = currentLine;
                return true;
            }
    
            return false;
        }
    
        private static string ConvertToAbsolutePath(string relativePath)
        {
            var projectPath = Application.dataPath[..Application.dataPath.LastIndexOf("Assets", System.StringComparison.Ordinal)];
            return System.IO.Path.Combine(projectPath, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }
    
        private static bool IsDebuggerClass(string absolutePath)
        {
            var fileName = Path.GetFileName(absolutePath);
            return DEBUGGER_CLASS_NAMES.Any(className => 
                fileName.Equals(className, System.StringComparison.OrdinalIgnoreCase));
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
#endif
    
}