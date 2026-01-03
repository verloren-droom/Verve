#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;

    
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class NotNullAttribute : PropertyAttribute { }
}

#endif