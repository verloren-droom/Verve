#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    ///   <para>标签注册表设置；保存项目级标签名称和稳定标识。</para>
    /// </summary>
    internal sealed class TagRegistrySettings : ScriptableObjectInstanceBase<TagRegistrySettings>
    {
        /// <summary>
        ///   <para>标签条目；保存名称和稳定标识。</para>
        /// </summary>
        [Serializable]
        internal sealed class Entry
        {
            /// <summary>
            ///   <para>标签名称。</para>
            /// </summary>
            [SerializeField] internal string name;
            /// <summary>
            ///   <para>标签标识。</para>
            /// </summary>
            [SerializeField] internal int id;
        }

        /// <summary>
        ///   <para>下一个可分配标识；已删除标识不会再次使用。</para>
        /// </summary>
        [SerializeField] internal int nextId = 1;
        /// <summary>
        ///   <para>项目标签条目。</para>
        /// </summary>
        [SerializeField] internal List<Entry> entries = new();
    }
}

#endif