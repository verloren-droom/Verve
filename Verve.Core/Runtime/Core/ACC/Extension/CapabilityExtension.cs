namespace Verve
{
    using System.Runtime.CompilerServices;
    
    
    /// <summary>
    ///   <para><see cref="Capability"/>扩展方法</para>
    /// </summary>
    public static class CapabilityExtension
    {
        /// <summary>
        ///   <para>获取组件引用</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponent<T>(this Capability self) where T : struct, IComponent 
            => ref self.OwnerActor.GetComponent<T>(self.OwnerWorld);
        
        /// <summary>
        ///   <para>尝试获取组件引用</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponent<T>(this Capability self, out T component) where T : struct, IComponent 
            => self.OwnerActor.TryGetComponent(self.OwnerWorld, out component);
        
        /// <summary>
        ///   <para>设置组件数据</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponent<T>(this Capability self, in T component) where T : struct, IComponent 
            => self.OwnerActor.SetComponent(self.OwnerWorld, component);
        
        /// <summary>
        ///   <para>手动标记Actor为脏数据（用于触发检查是否激活或失活）</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarkActorDirty(this Capability self) => self.OwnerWorld.Capabilities.MarkActorDirty(self.OwnerActor);
    }
}