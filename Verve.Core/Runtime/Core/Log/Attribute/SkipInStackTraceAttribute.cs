namespace Verve
{
    using System;

    
    /// <summary>
    ///   <para>标记类跳过堆栈跟踪</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SkipInStackTraceAttribute : Attribute
    {
        /// <summary>
        ///   <para>类名</para>
        /// </summary>
        public string ClassName { get; }
        
        
        public SkipInStackTraceAttribute(string className = null)
        {
            ClassName = className;
        }
    }
}
