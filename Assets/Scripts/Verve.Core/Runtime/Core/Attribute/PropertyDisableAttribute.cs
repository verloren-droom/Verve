namespace Verve
{
    using System;
#if UNITY_5_3_OR_NEWER
    using UnityEngine;
#endif

    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
#if UNITY_5_3_OR_NEWER
    public class PropertyDisableAttribute : PropertyAttribute { }
#else
    public class PropertyDisableAttribute : Attribute { }
#endif
}
