#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine.Scripting;
    
    
    /// <summary>
    ///   <para>标记调试项类型属性，必须继承 <see cref="DebugTabWindow"/> 抽象类 </para>
    /// </summary>
    [Preserve, AttributeUsage(AttributeTargets.Class)]
    public sealed class DebugItemAttribute : Attribute
    {
        /// <summary>
        ///   <para>标题</para>
        /// </summary>
        public readonly string title;
        /// <summary>
        ///   <para>排序值</para>
        /// </summary>
        public readonly int order;
        
        
        [Preserve]
        public DebugItemAttribute(string title, int order = 0)
        {
            this.title = title;
            this.order = order;
        }
    }
}

#endif