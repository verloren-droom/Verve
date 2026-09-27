#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    ///   <para>控制台命令特性；将方法注册为命令。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        /// <summary>
        ///   <para>命令正则表达式。</para>
        /// </summary>
        private static readonly Regex s_CommandRegex = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        ///   <para>命令（不区分大小写）。</para>
        /// </summary>
        public readonly string command;
        /// <summary>
        ///   <para>命令描述。</para>
        /// </summary>
        public readonly string description;
        
        
        /// <summary>
        ///   <para>创建控制台命令特性。</para>
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="description">描述。</param>
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