#if UNITY_5_3_OR_NEWER

namespace Verve.UniEx
{
    using UnityEngine;
    
    
    /// <summary>
    ///   <para>GameObject扩展类</para>
    /// </summary>
    public static class GameObjectExtension
    {
        /// <summary>
        ///   <para>获取或添加组件</para>
        ///   <para>如果组件不存在就添加组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>
        ///   <para>组件实例</para>
        /// </returns>
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            return self?.GetComponent<T>() ?? self?.AddComponent<T>();
        }
        
        /// <summary>
        ///   <para>获取或添加组件</para>
        ///   <para>如果组件不存在就添加组件</para>
        /// </summary>
        /// <param name="self">组件类型</param>
        /// <returns>
        ///   <para>组件实例</para>
        /// </returns>
        public static Component GetOrAddComponent(this GameObject self, System.Type type)
        {
            return self?.GetComponent(type) ?? self?.AddComponent(type);
        }
    }
}

#endif