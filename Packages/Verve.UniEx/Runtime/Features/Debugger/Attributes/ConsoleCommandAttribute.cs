#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx.Debug
{
    using System;

    
    /// <summary>
    /// 添加此特性的函数将注册为控制台命令
    /// </summary>
    /// <remarks>
    /// 函数暂时只支持静态函数
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public string Command { get; }
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