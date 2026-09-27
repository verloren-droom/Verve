#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para>必需组件特性；约束所引用 <see cref="UnityEngine.GameObject"/> 的组件类型。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class RequireComponentOnGameObjectAttribute : PropertyAttribute
    {
        /// <summary>
        ///   <para>需要 <see cref="UnityEngine.GameObject"/> 上的组件类型。</para>
        /// </summary>
        public Type RequiredType { get; }
        
        
        /// <summary>
        ///   <para>创建必需组件特性。</para>
        /// </summary>
        /// <param name="requiredType">必需的类型。</param>
        public RequireComponentOnGameObjectAttribute(Type requiredType)
        {
            if (requiredType == null) throw new ArgumentNullException(nameof(requiredType));
            if (!requiredType.IsInterface && !typeof(Component).IsAssignableFrom(requiredType))
                throw new ArgumentException("必需类型应为组件或接口。", nameof(requiredType));
            RequiredType = requiredType;
        }
    }
}

#endif
