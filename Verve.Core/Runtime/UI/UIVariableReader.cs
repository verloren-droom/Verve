#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using UnityEngine;

    /// <summary>
    ///   <para>UI 变量读取器；读取组件中序列化的 Unity 对象引用。</para>
    /// </summary>
    internal static class UIVariableReader
    {
        /// <summary>
        ///   <para>将变量转换为生成代码声明的 Unity 对象类型。</para>
        /// </summary>
        /// <param name="value">序列化变量值。</param>
        /// <param name="index">变量索引。</param>
        /// <param name="ownerName">变量所属节点名称。</param>
        /// <param name="context">变量所属组件类型描述。</param>
        /// <typeparam name="T">期望的 Unity 对象类型。</typeparam>
        internal static T Read<T>(
            UnityEngine.Object value,
            int index,
            string ownerName,
            string context)
            where T : UnityEngine.Object
        {
            if (value is T target)
            {
                return target;
            }

            var actualType = value == null ? "空" : value.GetType().FullName;
            throw new InvalidOperationException(
                $"{context} 变量索引 {index} 在 {ownerName} 上的实际类型为 {actualType}，期望类型为 {typeof(T).FullName}。");
        }
    }
}

#endif