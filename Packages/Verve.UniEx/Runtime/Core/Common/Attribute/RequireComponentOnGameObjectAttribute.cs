#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>引用的 GameObject 上必须包含指定组件</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class RequireComponentOnGameObjectAttribute : PropertyAttribute
    {
        /// <summary>
        ///   <para>需要 GameObject 上的组件类型</para>
        /// </summary>
        public Type RequiredType { get; }
        
        
        public RequireComponentOnGameObjectAttribute(Type requiredType)
        {
            RequiredType = requiredType;
        }
    }
}

#endif