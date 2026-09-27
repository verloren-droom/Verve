namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;
    
    /// <summary>
    ///   <para>游戏模块必需依赖；容器据此安排安装与卸载顺序。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class GameModuleDependencyAttribute : Attribute
    {
        /// <summary>
        ///   <para>依赖模块类型列表。</para>
        /// </summary>
        public readonly Type[] dependencies;

        /// <summary>
        ///   <para>创建模块依赖特性。</para>
        /// </summary>
        /// <param name="dependencies">依赖。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GameModuleDependencyAttribute(params Type[] dependencies) => this.dependencies = dependencies ?? Array.Empty<Type>();
    }
}