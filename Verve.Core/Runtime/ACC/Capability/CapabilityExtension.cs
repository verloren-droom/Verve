namespace Verve
{
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para><see cref="Capability"/> 的组件访问扩展。</para>
    /// </summary>
    public static class CapabilityExtension
    {
        /// <summary>
        ///   <para>获取所属行动者的组件引用。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="self">持有组件的 <see cref="Capability"/>。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponent<T>(this Capability self)
            where T : struct, IComponent
            => ref self.OwnerWorld.GetComponent<T>(self.OwnerActor);

        /// <summary>
        ///   <para>尝试获取所属行动者的组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="self">持有组件的 <see cref="Capability"/>。</param>
        /// <param name="component">接收组件数据。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponent<T>(this Capability self, out T component)
            where T : struct, IComponent
            => self.OwnerWorld.TryGetComponent(self.OwnerActor, out component);

        /// <summary>
        ///   <para>设置所属行动者的组件数据。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="self">持有组件的 <see cref="Capability"/>。</param>
        /// <param name="component">要写入的组件数据。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponent<T>(this Capability self, in T component)
            where T : struct, IComponent
            => self.OwnerWorld.SetComponent(self.OwnerActor, component);
    }
}
