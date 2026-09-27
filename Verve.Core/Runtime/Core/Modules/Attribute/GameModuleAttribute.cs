namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>模块特性；提供编辑器与调试器展示元数据。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GameModuleAttribute : Attribute
    {
        /// <summary>
        ///   <para>模块描述。</para>
        /// </summary>
        public readonly string description;

        /// <summary>
        ///   <para>创建模块特性。</para>
        /// </summary>
        /// <param name="description">描述。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameModuleAttribute(string description = null) => this.description = description;
    }
}