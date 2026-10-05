// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;

    /// <summary>
    ///   <para><see cref="CapabilitySheetAsset"/> 可选类型标记；为能力和组件提供编辑器显示名。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class CapabilitySheetTypeAttribute : Attribute
    {
        /// <summary>
        ///   <para>编辑器显示名；为空时使用类型名。</para>
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        ///   <para>创建 <see cref="CapabilitySheetAsset"/> 类型标记。</para>
        /// </summary>
        /// <param name="displayName">编辑器显示名。</param>
        public CapabilitySheetTypeAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}