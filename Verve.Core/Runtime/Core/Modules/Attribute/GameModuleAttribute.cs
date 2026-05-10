namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para>游戏模块特性，用于声明模块编辑器与调试展示所需的附加元数据</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GameModuleAttribute : Attribute
    {
        /// <summary>
        ///   <para>模块描述</para>
        /// </summary>
        public readonly string description;
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameModuleAttribute(string description = null)
        {
            this.description = description;
        }
    }
}