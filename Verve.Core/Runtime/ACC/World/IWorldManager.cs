namespace Verve
{
    using System.Collections.Generic;

    /// <summary>
    ///   <para>ACC 世界管理服务。</para>
    /// </summary>
    public interface IWorldManager : IGameModule
    {
        /// <summary>
        ///   <para>当前世界数量。</para>
        /// </summary>
        int Count { get; }

        /// <summary>
        ///   <para>当前世界的只读快照。</para>
        /// </summary>
        IReadOnlyCollection<World> Worlds { get; }

        /// <summary>
        ///   <para>当前活跃世界；没有可用世界时返回 <c>null</c>。</para>
        /// </summary>
        World Active { get; }

        /// <summary>
        ///   <para>创建世界，或激活同名的现有世界。</para>
        /// </summary>
        World Create(string worldName, ActorManagerOptions? actorManagerOptions = null, ReplicationOptions replicationOptions = null);

        /// <summary>
        ///   <para>判断指定世界是否存在且可用。</para>
        /// </summary>
        bool Contains(string worldName);

        /// <summary>
        ///   <para>获取指定世界；不存在时返回 <c>null</c>。</para>
        /// </summary>
        World Get(string worldName);

        /// <summary>
        ///   <para>切换当前活跃世界。</para>
        /// </summary>
        bool SetActive(string worldName);

        /// <summary>
        ///   <para>销毁指定世界。</para>
        /// </summary>
        void Destroy(string worldName);

        /// <summary>
        ///   <para>销毁全部世界。</para>
        /// </summary>
        void DestroyAll();
    }
}