#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine.Scripting;

    /// <summary>
    ///   <para>调试项特性；标记 <see cref="DebugTabWindow"/> 派生类型。</para>
    /// </summary>
    [Preserve, AttributeUsage(AttributeTargets.Class)]
    public sealed class DebugItemAttribute : Attribute
    {
        /// <summary>
        ///   <para>标题。</para>
        /// </summary>
        public readonly string title;
        /// <summary>
        ///   <para>排序值。</para>
        /// </summary>
        public readonly int order;
        
        
        /// <summary>
        ///   <para>创建调试项特性。</para>
        /// </summary>
        /// <param name="title">标题。</param>
        /// <param name="order">顺序。</param>
        [Preserve]
        public DebugItemAttribute(string title, int order = 0)
        {
            this.title = title;
            this.order = order;
        }
    }
}

#endif