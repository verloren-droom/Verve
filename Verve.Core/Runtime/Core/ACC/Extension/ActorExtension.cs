namespace Verve
{
    using System;
    using System.Runtime.CompilerServices;

    
    /// <summary>
    ///   <para><see cref="Actor"/>的扩展方法</para>
    /// </summary>
    public static class ActorExtension
    {
        /// <summary>
        ///   <para>为行动者添加组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddComponent<T>(this Actor self, World world) where T : struct, IComponent
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return ref world.AddComponent<T>(self);
        }

        /// <summary>
        ///   <para>获取行动者组件引用</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponent<T>(this Actor self, World world) where T : struct, IComponent
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return ref world.GetComponent<T>(self);
        }

        /// <summary>
        ///   <para>尝试获取行动者组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponent<T>(this Actor self, World world, out T component) where T : struct, IComponent
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return world.TryGetComponent(self, out component);
        }

        /// <summary>
        ///   <para>移除行动者组件</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveComponent<T>(this Actor self, World world) where T : struct, IComponent
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return world.RemoveComponent<T>(self);
        }

        /// <summary>
        ///   <para>设置行动者组件数据</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponent<T>(this Actor self, World world, in T component) where T : struct, IComponent
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.SetComponent(self, component);
        }

        /// <summary>
        ///   <para>判断行动者是否拥有组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasComponent<T>(this Actor actor, World world) where T : struct, IComponent
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return world.HasComponent<T>(actor);
        }

        /// <summary>
        ///   <para>判断行动者是否存活</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlive(this Actor self, World world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return world.IsActorAlive(self);
        }

        /// <summary>
        ///   <para>销毁行动者</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(this Actor self, World world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.DestroyActor(self);
        }

        /// <summary>
        ///   <para>为行动者添加能力</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AddCapability<T>(this Actor self, World world) where T : Capability, new()
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return world.AddCapability<T>(self);
        }

        /// <summary>
        ///   <para>移除行动者上的指定能力</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveCapability<T>(this Actor self, World world) where T : Capability
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return world.RemoveCapability<T>(self);
        }

        /// <summary>
        ///   <para>应用表单到行动者</para>
        /// </summary>
        /// <param name="sheet">表单</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SheetInstance ApplySheet(this Actor self, World world, CapabilitySheet sheet)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            return world.ApplySheet(self, sheet);
        }
        
                /// <summary>
        ///   <para>添加组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddComponent<T>(this Actor self) where T : struct, IComponent
            => ref self.AddComponent<T>(Game.World);
        
        /// <summary>
        ///   <para>获取组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponent<T>(this Actor self) where T : struct, IComponent
            => ref self.GetComponent<T>(Game.World);
        
        /// <summary>
        ///   <para>尝试获取组件（当前活跃世界）</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponent<T>(this Actor self, out T component) where T : struct, IComponent
            => self.TryGetComponent(Game.World, out component);
        
        /// <summary>
        ///   <para>设置组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponent<T>(this Actor self, in T component) where T : struct, IComponent
            => self.SetComponent(Game.World, component);
        
        /// <summary>
        ///   <para>检查是否拥有组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasComponent<T>(this Actor actor) where T : struct, IComponent
            => actor.HasComponent<T>(Game.World);
    
        /// <summary>
        ///   <para>检查行动者是否存活（当前活跃世界）</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlive(this Actor self)
            => self.IsAlive(Game.World);
        
        /// <summary>
        ///   <para>销毁行动者（当前活跃世界）</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(this Actor self)
            => self.Destroy(Game.World);

        /// <summary>
        ///   <para>为行动者添加能力（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AddCapability<T>(this Actor self) where T : Capability, new()
            => self.AddCapability<T>(Game.World);

        /// <summary>
        ///   <para>为行动者移除能力（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveCapability<T>(this Actor self) where T : Capability
            => self.RemoveCapability<T>(Game.World);

        /// <summary>
        ///   <para>为行动者应用表单（当前活跃世界）</para>
        /// </summary>
        /// <param name="sheet">表单</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SheetInstance ApplySheet(this Actor self, CapabilitySheet sheet)
            => self.ApplySheet(Game.World, sheet);
    }
}