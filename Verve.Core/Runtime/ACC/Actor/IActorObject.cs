// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    /// <summary>
    ///   <para>行动者对象接口；表示外部对象对应的行动者，不拥有行动者资源。</para>
    /// </summary>
    public interface IActorObject
    {
        /// <summary>
        ///   <para>当前对象对应的行动者。</para>
        /// </summary>
        Actor Actor { get; }
    }
}