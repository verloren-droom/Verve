#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Text.RegularExpressions;
    
    
    /// <summary>
    ///   <para>标记控制台命令方法</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        private static readonly Regex s_CommandRegex = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        ///   <para>命令（不区分大小写）</para>
        /// </summary>
        public readonly string command;
        /// <summary>
        ///   <para>命令描述</para>
        /// </summary>
        public readonly string description;
        
        
        public ConsoleCommandAttribute(string command, string description = "")
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("command is null or empty.");
            command = command.Trim();
            if (!s_CommandRegex.IsMatch(command))
                throw new ArgumentException("command is invalid.");
            this.command = command;
            this.description = description;
        }
    }
}

#endif