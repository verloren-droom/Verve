namespace Verve
{
    /// <summary>
    ///   <para>更新分组</para>
    /// </summary>
    public enum TickGroup
    {
#if UNITY_2018_3_OR_NEWER || !UNITY_5_1_OR_NEWER
        /// <summary>
        ///   <para>早期更新阶段</para>
        /// </summary>
        Early,
#endif
        /// <summary>
        ///   <para>物理更新阶段</para>
        /// </summary>
        Physics,
        /// <summary>
        ///   <para>游戏逻辑更新阶段</para>
        /// </summary>
        Gameplay,
        /// <summary>
        ///   <para>延迟更新阶段</para>
        /// </summary>
        Late
    }
}