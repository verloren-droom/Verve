#if UNITY_5_3_OR_NEWER

namespace Verve
{
    using UnityEngine;

    /// <summary>
    ///   <para>项目工具设置；持久化实现映射，不持有工具实例。</para>
    /// </summary>
    internal sealed class GameToolSettings : ScriptableObjectInstanceBase<GameToolSettings>
    {
        /// <summary>
        ///   <para>实现配置。</para>
        /// </summary>
        [SerializeField] internal GameToolConfiguration configuration = new();
    }
}

#endif