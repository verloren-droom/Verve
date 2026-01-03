#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>只读属性</para>
    ///   <para>用于在Inspector窗口显示只读属性</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ReadOnlyAttribute : PropertyAttribute { }
}

#endif
