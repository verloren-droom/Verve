namespace Verve.Debug
{
    using System;
    
    
    /// <summary>
    /// 默认控制台调试子模块
    /// </summary>
    [Serializable]
    public sealed partial class ConsoleDebuggerSubmodule : DebuggerSubmodule
    {
        public override string ModuleName => "ConsoleDebugger";
        

        [System.Diagnostics.DebuggerHidden, System.Diagnostics.DebuggerStepThrough]
        protected override void InternalLog_Implement(string msg, LogLevel level)
        {
            var color = GetConsoleColor(level);
            var originalColor = Console.ForegroundColor;
        
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = originalColor;
            Console.ResetColor();
        }
        
        private static ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Log => ConsoleColor.Black,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Exception => ConsoleColor.DarkRed,
                LogLevel.Assert => ConsoleColor.Red,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };
        }
    }
}