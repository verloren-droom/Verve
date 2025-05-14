namespace Verve
{
#if UNITY_5_3_OR_NEWER
    using System;
    using UnityEngine;
    
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class PropertyDisableAttribute : PropertyAttribute { }
#endif
    
}
