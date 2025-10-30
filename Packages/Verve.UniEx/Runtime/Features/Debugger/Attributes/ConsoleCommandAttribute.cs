#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Debug
{
    using System;

    
    /// <summary>
    ///   <para>添加此特性的函数将注册为控制台命令</para>
    ///   <para>函数暂时只支持静态函数</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        /// <summary>
        ///   <para>命令名称</para>
        /// </summary>
        public string Command { get; }
        
        /// <summary>
        ///   <para>命令描述</para>
        /// </summary>
        public string Description { get; }

        
        public ConsoleCommandAttribute(string command, string description = "")
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentException("command is not null");
            Command = command;
            Description = description;
        }
    }
}

#endif