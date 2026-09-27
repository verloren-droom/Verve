#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para><see cref="UnityEngine.GameObject"/> 扩展类。</para>
    /// </summary>
    public static class GameObjectExtension
    {
        /// <summary>
        ///   <para>获取或添加组件；如果组件不存在就添加组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>
        ///   <para>组件实例</para>
        /// </returns>
        /// <param name="self">目标对象。</param>
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            return self.GetComponent<T>() ?? self.AddComponent<T>();
        }
        
        /// <summary>
        ///   <para>获取或添加组件；如果组件不存在就添加组件。</para>
        /// </summary>
        /// <param name="self">组件类型。</param>
        /// <returns>
        ///   <para>组件实例</para>
        /// </returns>
        /// <param name="type">类型。</param>
        public static Component GetOrAddComponent(this GameObject self, System.Type type)
        {
            if (self == null) throw new ArgumentNullException(nameof(self));
            if (type == null) throw new ArgumentNullException(nameof(type));
            return self.GetComponent(type) ?? self.AddComponent(type);
        }
    }
}

#endif