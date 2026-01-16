namespace Verve
{
    using System;
    using System.Collections.Generic;
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
        /// <param name="world">所在世界</param>
        /// <param name="direction">同步方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddComponent<T>(this Actor self, World world, NetworkSyncDirection direction = NetworkSyncDirection.None)
            where T : struct, IComponent
            => ref (world ?? throw new ArgumentNullException(nameof(world))).AddComponent<T>(self, direction);

        /// <summary>
        ///   <para>为行动者添加组件（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="worldName">世界名称</param>
        /// <param name="direction">同步方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddComponent<T>(this Actor self, string worldName, NetworkSyncDirection direction = NetworkSyncDirection.None)
            where T : struct, IComponent
            => ref self.AddComponent<T>(Game.GetWorld(worldName), direction);

        /// <summary>
        ///   <para>获取行动者组件引用</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponent<T>(this Actor self, World world)
            where T : struct, IComponent
            => ref (world ?? throw new ArgumentNullException(nameof(world))).GetComponent<T>(self);
        
        /// <summary>
        ///   <para>获取行动者组件引用（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponent<T>(this Actor self, string worldName)
            where T : struct, IComponent
            => ref self.GetComponent<T>(Game.GetWorld(worldName));
        
        /// <summary>
        ///   <para>获取或添加行动者组件引用</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="world">所在世界</param>
        /// <param name="direction">同步方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetOrAddComponent<T>(this Actor self, World world, NetworkSyncDirection direction = NetworkSyncDirection.None)
            where T : struct, IComponent
            => ref (world ?? throw new ArgumentNullException(nameof(world))).GetOrAddComponent<T>(self, direction);
        
        /// <summary>
        ///   <para>获取或添加行动者组件引用（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="worldName">世界名称</param>
        /// <param name="direction">同步方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetOrAddComponent<T>(this Actor self, string worldName, NetworkSyncDirection direction = NetworkSyncDirection.None)
            where T : struct, IComponent
            => ref self.GetOrAddComponent<T>(Game.GetWorld(worldName), direction);

        /// <summary>
        ///   <para>尝试获取行动者组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="world">所在世界</param>
        /// <param name="component">组件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponent<T>(this Actor self, World world, out T component)
            where T : struct, IComponent
            => (world ?? throw new ArgumentNullException(nameof(world))).TryGetComponent(self, out component);
        
        /// <summary>
        ///   <para>尝试获取行动者组件（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="worldName">世界名称</param>
        /// <param name="component">组件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponent<T>(this Actor self, string worldName, out T component)
            where T : struct, IComponent
            => self.TryGetComponent<T>(Game.GetWorld(worldName), out component);
        
        /// <summary>
        ///   <para>移除行动者组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveComponent<T>(this Actor self, World world)
            where T : struct, IComponent
            => (world ?? throw new ArgumentNullException(nameof(world))).RemoveComponent<T>(self);
        
        /// <summary>
        ///   <para>移除行动者组件（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveComponent<T>(this Actor self, string worldName)
            where T : struct, IComponent
            => self.RemoveComponent<T>(Game.GetWorld(worldName));
        
        /// <summary>
        ///   <para>设置行动者组件数据</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="world">所在世界</param>
        /// <param name="component">组件数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponent<T>(this Actor self, World world, in T component)
            where T : struct, IComponent
            => (world ?? throw new ArgumentNullException(nameof(world))).SetComponent(self, component);
        
        /// <summary>
        ///   <para>设置行动者组件数据（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="worldName">世界名称</param>
        /// <param name="component">组件数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponent<T>(this Actor self, string worldName, in T component)
            where T : struct, IComponent
            => self.SetComponent<T>(Game.GetWorld(worldName), component);
        
        /// <summary>
        ///   <para>判断行动者是否拥有组件</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasComponent<T>(this Actor self, World world)
            where T : struct, IComponent
            => (world ?? throw new ArgumentNullException(nameof(world))).HasComponent<T>(self);

        /// <summary>
        ///   <para>判断行动者是否拥有组件（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasComponent<T>(this Actor self, string worldName)
            where T : struct, IComponent
            => self.HasComponent<T>(Game.GetWorld(worldName));

        /// <summary>
        ///   <para>判断行动者是否存活</para>
        /// </summary>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlive(this Actor self, World world)
            => (world ?? throw new ArgumentNullException(nameof(world))).IsActorAlive(self);
        
        /// <summary>
        ///   <para>判断行动者是否存活（指定世界）</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlive(this Actor self, string worldName)
            => self.IsAlive(Game.GetWorld(worldName));

        /// <summary>
        ///   <para>销毁行动者</para>
        /// </summary>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(this Actor self, World world)
            => (world ?? throw new ArgumentNullException(nameof(world))).DestroyActor(self);
        
        /// <summary>
        ///   <para>销毁行动者（指定世界）</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Destroy(this Actor self, string worldName)
            => self.Destroy(Game.GetWorld(worldName));

        /// <summary>
        ///   <para>为行动者添加能力</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AddCapability<T>(this Actor self, World world)
            where T : Capability, new()
            => (world ?? throw new ArgumentNullException(nameof(world))).AddCapability<T>(self);
        
        /// <summary>
        ///   <para>为行动者添加能力（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AddCapability<T>(this Actor self, string worldName)
            where T : Capability, new()
            => self.AddCapability<T>(Game.GetWorld(worldName));
        
        /// <summary>
        ///   <para>移除行动者上的指定能力</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveCapability<T>(this Actor self, World world)
            where T : Capability
            => (world ?? throw new ArgumentNullException(nameof(world))).RemoveCapability<T>(self);

        /// <summary>
        ///   <para>移除行动者上的指定能力（指定世界）</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveCapability<T>(this Actor self, string worldName)
            where T : Capability
            => self.RemoveCapability<T>(Game.GetWorld(worldName));

        /// <summary>
        ///   <para>应用表单到行动者</para>
        /// </summary>
        /// <param name="world">所在世界</param>
        /// <param name="sheet">表单</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SheetInstance ApplySheet(this Actor self, World world, CapabilitySheet sheet, CapabilitySheetApplyMode mode = CapabilitySheetApplyMode.All)
            => (world ?? throw new ArgumentNullException(nameof(world))).ApplySheet(self, sheet, mode);
        
        /// <summary>
        ///   <para>应用表单到行动者（指定世界）</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        /// <param name="sheet">表单</param>
        /// <param name="mode">应用模式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SheetInstance ApplySheet(this Actor self, string worldName, CapabilitySheet sheet, CapabilitySheetApplyMode mode = CapabilitySheetApplyMode.All)
            => self.ApplySheet(Game.GetWorld(worldName), sheet, mode);

        /// <summary>
        ///   <para>移除指定表单实例</para>
        /// </summary>
        /// <param name="world">所在世界</param>
        /// <param name="instance">表单实例</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveSheet(this Actor self, World world, SheetInstance instance)
            => (world ?? throw new ArgumentNullException(nameof(world))).RemoveSheet(self, instance);
        
        /// <summary>
        ///   <para>移除指定表单实例（指定世界）</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        /// <param name="instance">表单实例</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveSheet(this Actor self, string worldName, SheetInstance instance)
            => self.RemoveSheet(Game.GetWorld(worldName), instance);
        
        /// <summary>
        ///   <para>移除全部表单</para>
        /// </summary>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAllSheets(this Actor self, World world)
            => (world ?? throw new ArgumentNullException(nameof(world))).RemoveAllSheets(self);
        
        /// <summary>
        ///   <para>移除全部表单（指定世界）</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAllSheets(this Actor self, string worldName)
            => self.RemoveAllSheets(Game.GetWorld(worldName));
        
        /// <summary>
        ///   <para>获取表单实例列表</para>
        /// </summary>
        /// <param name="world">所在世界</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<SheetInstance> GetActorSheets(this Actor self, World world)
            => (world ?? throw new ArgumentNullException(nameof(world))).GetActorSheets(self);
        
        /// <summary>
        ///   <para>获取表单实例列表（指定世界）</para>
        /// </summary>
        /// <param name="worldName">世界名称</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<SheetInstance> GetActorSheets(this Actor self, string worldName)
            => self.GetActorSheets(Game.GetWorld(worldName));

        /// <summary>
        ///   <para>添加组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="direction">同步方向</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AddComponent<T>(this Actor self, NetworkSyncDirection direction = NetworkSyncDirection.None) 
            where T : struct, IComponent 
            => ref self.AddComponent<T>(Game.World, direction);
        
        /// <summary>
        ///   <para>获取组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponent<T>(this Actor self)
            where T : struct, IComponent
            => ref self.GetComponent<T>(Game.World);
        
        /// <summary>
        ///   <para>获取或添加组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="direction">同步方向</param>
        public static ref T GetOrAddComponent<T>(this Actor self, NetworkSyncDirection direction = NetworkSyncDirection.None)
            where T : struct, IComponent
            => ref self.GetOrAddComponent<T>(Game.World, direction);
        
        /// <summary>
        ///   <para>尝试获取组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetComponent<T>(this Actor self, out T component)
            where T : struct, IComponent
            => self.TryGetComponent(Game.World, out component);
        
        /// <summary>
        ///   <para>设置组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="component">组件</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponent<T>(this Actor self, in T component)
            where T : struct, IComponent
            => self.SetComponent(Game.World, component);
        
        /// <summary>
        ///   <para>检查是否拥有组件（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasComponent<T>(this Actor self)
            where T : struct, IComponent
            => self.HasComponent<T>(Game.World);
    
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
        public static T AddCapability<T>(this Actor self)
            where T : Capability, new()
            => self.AddCapability<T>(Game.World);

        /// <summary>
        ///   <para>为行动者移除能力（当前活跃世界）</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveCapability<T>(this Actor self)
            where T : Capability
            => self.RemoveCapability<T>(Game.World);

        /// <summary>
        ///   <para>为行动者应用表单（当前活跃世界）</para>
        /// </summary>
        /// <param name="sheet">表单</param>
        /// <param name="mode">应用模式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SheetInstance ApplySheet(this Actor self, CapabilitySheet sheet, CapabilitySheetApplyMode mode = CapabilitySheetApplyMode.All)
            => self.ApplySheet(Game.World, sheet, mode);

        /// <summary>
        ///   <para>移除指定表单实例（当前活跃世界）</para>
        /// </summary>
        /// <param name="instance">表单实例</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RemoveSheet(this Actor self, SheetInstance instance)
            => self.RemoveSheet(Game.World, instance);
        
        /// <summary>
        ///   <para>移除全部表单（当前活跃世界）</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAllSheets(this Actor self)
            => self.RemoveAllSheets(Game.World);
        
        /// <summary>
        ///   <para>获取表单实例列表（当前活跃世界）</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<SheetInstance> GetActorSheets(this Actor self)
            => self.GetActorSheets(Game.World);
    }
}