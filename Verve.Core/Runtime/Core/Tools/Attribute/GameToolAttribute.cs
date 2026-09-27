namespace Verve
{
    using System;

    /// <summary>
    ///   <para>工具声明；提供展示名与可选的默认实现。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class GameToolAttribute : Attribute
    {
        /// <summary>
        ///   <para>展示名；不作为配置标识。</para>
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        ///   <para>默认实现类型；仅在接口未配置时采用，空值表示无默认实现。</para>
        /// </summary>
        public Type DefaultImplementationType { get; }

        /// <summary>
        ///   <para>声明工具信息。</para>
        /// </summary>
        /// <param name="displayName">展示名。</param>
        /// <param name="defaultImplementationType">默认实现类型；需具备无参构造函数。</param>
        public GameToolAttribute(string displayName, Type defaultImplementationType = null)
        {
            DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName
                : throw new ArgumentException("A tool display name is required.", nameof(displayName));
            DefaultImplementationType = defaultImplementationType;
        }
    }
}