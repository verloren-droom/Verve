#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using System;
    using UnityEditor;
    using UnityEngine;
    
    
    /// <summary>
    /// 要求引用的 GameObject 上必须包含指定组件
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class RequireComponentOnGameObjectAttribute : PropertyAttribute
    {
        public Type RequiredType { get; }
        
        public RequireComponentOnGameObjectAttribute(Type requiredType)
        {
            RequiredType = requiredType;
        }
    }
}

#endif