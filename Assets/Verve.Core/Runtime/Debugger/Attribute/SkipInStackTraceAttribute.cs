namespace Verve.Debugger
{
    using System;

    
    /// <summary>
    /// 标记类跳过堆栈跟踪
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SkipInStackTraceAttribute : Attribute
    {
        public string ClassName { get; }
        public SkipInStackTraceAttribute() { }
        public SkipInStackTraceAttribute(string className)
        {
            ClassName = className;
        }
    }
}
