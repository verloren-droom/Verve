namespace Verve
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   <para>接收组件列；解码与世界提交分离，畸形批次不产生部分组件写入。</para>
    /// </summary>
    internal abstract class ReplicationBatch
    {
        /// <summary>
        ///   <para>已提交的组件成员。</para>
        /// </summary>
        internal readonly HashSet<long> Members = new();
        /// <summary>
        ///   <para>本批待移除的组件。</para>
        /// </summary>
        internal readonly List<long> Removed = new();
        /// <summary>
        ///   <para>本批待写入的组件。</para>
        /// </summary>
        internal readonly List<long> Updated = new();

        /// <summary>
        ///   <para>解码并暂存一个值。</para>
        /// </summary>
        /// <param name="bytes">完整定长编码。</param>
        internal abstract void Decode(ReadOnlySpan<byte> bytes);
        /// <summary>
        ///   <para>提交组件变更。</para>
        /// </summary>
        /// <param name="world">目标世界。</param>
        /// <param name="actors">连接拥有的实体映射。</param>
        internal abstract void Apply(World world, Dictionary<long, Actor> actors);
        /// <summary>
        ///   <para>清空暂存并复用容量。</para>
        /// </summary>
        internal virtual void Clear() { Removed.Clear(); Updated.Clear(); }
    }

    /// <summary>
    ///   <para>强类型暂存；值仅解码一次，无逐组件对象分配。</para>
    /// </summary>
    /// <typeparam name="T">组件类型。</typeparam>
    internal sealed class ReplicationBatch<T> : ReplicationBatch where T : unmanaged, IComponent
    {
        /// <summary>
        ///   <para>不可替换的字段编码器。</para>
        /// </summary>
        private readonly IReplicationCodec<T> m_Codec;
        /// <summary>
        ///   <para>已经解码的连续值。</para>
        /// </summary>
        private readonly List<T> m_Values = new();
        /// <summary>
        ///   <para>创建暂存列。</para>
        /// </summary>
        /// <param name="codec">编码器。</param>
        internal ReplicationBatch(IReplicationCodec<T> codec) => m_Codec = codec;

        /// <inheritdoc />
        internal override void Decode(ReadOnlySpan<byte> bytes) => m_Values.Add(m_Codec.Decode(bytes));
        /// <inheritdoc />
        internal override void Apply(World world, Dictionary<long, Actor> actors)
        {
            foreach (long id in Removed)
            {
                world.RemoveComponent<T>(actors[id]);
                Members.Remove(id);
            }
            for (int i = 0; i < Updated.Count; i++)
            {
                world.SetComponent(actors[Updated[i]], m_Values[i]);
                Members.Add(Updated[i]);
            }
        }
        /// <inheritdoc />
        internal override void Clear() { base.Clear(); m_Values.Clear(); }
    }
}
