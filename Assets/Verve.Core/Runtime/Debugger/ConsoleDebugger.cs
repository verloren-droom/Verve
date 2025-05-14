namespace Verve.Debugger
{
    
    using System;
    
    
    internal sealed partial class ConsoleDebugger : DebuggerBase
    {
        [System.Diagnostics.DebuggerHidden, System.Diagnostics.DebuggerStepThrough]
        protected override void Log_Implement(string msg, LogLevel level)
        {
            if (!IsEnable || string.IsNullOrEmpty(msg)) return;
            
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