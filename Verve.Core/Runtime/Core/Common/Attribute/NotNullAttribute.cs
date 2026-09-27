#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;

    /// <summary>
    ///   <para>非空标记。</para>
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class NotNullAttribute : PropertyAttribute { }
}

#endif