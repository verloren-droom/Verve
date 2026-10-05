// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Threading;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>游戏世界；拥有 <see cref="Actor"/>、<see cref="IComponent"/>、<see cref="Capability"/> 和 <see cref="SheetManager"/>。</para>
    /// </summary>
    [Serializable, DebuggerDisplay("{ToString}")]
    public sealed class World : IDisposable
    {
        /// <summary>
        ///   <para>世界复制层；由当前 <see cref="World"/> 独占并释放。</para>
        /// </summary>
        public WorldReplication Replication { get; }
        /// <summary>
        ///   <para>行动者外部表现登记表；由当前 <see cref="World"/> 独占管理。</para>
        /// </summary>
        private readonly ActorObjectRegistry m_Objects;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        ///   <para>能力调试记录；仅在调试构建中按需启用。</para>
        /// </summary>
        internal CapabilityDebugTrace DebugTrace { get; }
#endif
        /// <summary>
        ///   <para>世界所属线程；结构变更统一在此线程提交。</para>
        /// </summary>
        private readonly int m_OwnerThread = Environment.CurrentManagedThreadId;
        /// <summary>
        ///   <para>是否正在清空或释放。</para>
        /// </summary>
        private bool m_IsClearing;
        /// <summary>
        ///   <para>是否正在执行世界 Tick。</para>
        /// </summary>
        private bool m_IsTicking;

        /// <summary>
        ///   <para>世界时间缩放。</para>
        /// </summary>
        private float m_TimeScale = 1f;
        /// <summary>
        ///   <para>保护待执行动作队列。</para>
        /// </summary>
        private SpinLock m_DispatchLock;
        /// <summary>
        ///   <para>动作写入缓冲区。</para>
        /// </summary>
        private Action<World>[] m_DispatchWriteBuffer;
        /// <summary>
        ///   <para>待执行动作数量。</para>
        /// </summary>
        private int m_DispatchWriteCount;
        /// <summary>
        ///   <para>当前执行批次缓冲区。</para>
        /// </summary>
        private Action<World>[] m_DispatchExecuteBuffer;

        /// <summary>
        ///   <para>是否已释放世界。</para>
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///   <para>世界名称；创建后不可更改。</para>
        /// </summary>
        public string Name { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
        /// <summary>
        ///   <para>行动者管理器；由当前 <see cref="World"/> 创建并释放。</para>
        /// </summary>
        public ActorManager Actors { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
        /// <summary>
        ///   <para>能力管理器；由当前 <see cref="World"/> 创建并释放。</para>
        /// </summary>
        public CapabilityManager Capabilities { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; }
        /// <summary>
        ///   <para>表单管理器；由 <see cref="CapabilityManager"/> 拥有。</para>
        /// </summary>
        public SheetManager Sheets { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Capabilities.Sheets; }
        /// <summary>
        ///   <para>时间缩放（更新速率）</para>
        /// </summary>
        public float TimeScale
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_TimeScale;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set
            {
                if (!Game.NumberUtility.IsFinite(value))
                    throw new ArgumentOutOfRangeException(nameof(value));
                m_TimeScale = Math.Max(0, value);
            }
        }
        /// <summary>
        ///   <para>当前等待执行的派发任务数量。</para>
        /// </summary>
        public int PendingActionCount
        {
            get
            {
                if (IsDisposed) return 0;
                bool lockTaken = false;
                try
                {
                    m_DispatchLock.Enter(ref lockTaken);
                    return m_DispatchWriteCount;
                }
                finally
                {
                    if (lockTaken) m_DispatchLock.Exit();
                }
            }
        }

        /// <summary>
        ///   <para>创建世界及其专属管理器。</para>
        /// </summary>
        /// <param name="name">世界名称。</param>
        /// <param name="actorManagerOptions">行动者存储配置；为空时使用默认值。</param>
        /// <param name="replicationOptions">网络配置；为空时不启用同步。</param>
        internal World(string name, ActorManagerOptions? actorManagerOptions = null, ReplicationOptions replicationOptions = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unnamed World" : name;
            Actors = new ActorManager(actorManagerOptions ?? ActorManagerOptions.Default);
            Capabilities = new CapabilityManager(this);
            Replication = replicationOptions == null ? null : new WorldReplication(this, replicationOptions);
            m_Objects = new ActorObjectRegistry();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugTrace = new CapabilityDebugTrace();
#endif
            m_DispatchLock = new SpinLock(false);
            m_DispatchWriteBuffer = new Action<World>[16];
            m_DispatchExecuteBuffer = new Action<World>[16];
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (IsDisposed) return;
            ValidateStructuralChange();
            if (m_IsTicking || Capabilities.IsUpdating || m_IsClearing)
                throw new InvalidOperationException("The world cannot be disposed during Tick or cleanup.");
            m_IsClearing = true;
            List<Exception> errors = null;
            try { Replication?.Close(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            try { DebugTrace.Clear(); DebugTrace.DisableAll(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
#endif
            try { m_Objects.Clear(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            try { Capabilities.Dispose(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            try { Actors.Dispose(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }

            try { ClearDispatchQueue(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }

            IsDisposed = true;
            m_IsClearing = false;
            if (errors != null)
                throw new AggregateException("Disposing the world failed.", errors);
        }

        /// <summary>
        ///   <para>创建一个行动者；其资源由当前 <see cref="World"/> 管理。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Actor CreateActor()
        {
            ValidateStructuralChange(true);
            return Actors.CreateActor();
        }

        /// <summary>
        ///   <para>批量创建行动者。</para>
        /// </summary>
        /// <param name="count">数量</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Actor[] CreateActors(int count)
        {
            ValidateStructuralChange(true);
            return Actors.CreateActors(count);
        }

        /// <summary>
        ///   <para>销毁一个行动者并释放其组件、能力和表单。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyActor(Actor actor)
        {
            ValidateStructuralChange();
            if (Replication?.Owns(actor) == true)
                throw new InvalidOperationException("Replicated actors are owned by their connection; detach the peer to release them.");
            if (!IsActorAlive(actor)) return;
            if (Capabilities.IsUpdating)
            {
                Capabilities.DeferActorDestruction(actor);
                return;
            }
            Capabilities.BeginActorDestruction(actor);
            DestroyActorNow(actor);
        }

        /// <summary>
        ///   <para>完成已登记的行动者销毁；单项清理失败也继续释放其余资源。</para>
        /// </summary>
        /// <param name="actor">待销毁行动者。</param>
        internal void DestroyActorNow(Actor actor)
        {
            List<Exception> errors = null;
            try
            {
                try { m_Objects.RemoveActor(actor); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                try { Sheets.RemoveAllSheets(actor); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                try { Capabilities.RemoveAllCapabilitiesNow(actor); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                try { Actors.DestroyActor(actor); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            }
            finally { Capabilities.EndActorDestruction(actor); }
            if (errors != null) throw new AggregateException("Destroying the actor failed.", errors);
        }

        /// <summary>
        ///   <para>为行动者添加组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T AddComponent<T>(Actor actor) where T : struct, IComponent
        {
            ValidateStructuralChange(true);
            return ref Actors.AddComponent<T>(actor);
        }

        /// <summary>
        ///   <para>获取行动者组件引用。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetComponent<T>(Actor actor)
            where T : struct, IComponent
            => ref Actors.GetComponent<T>(actor);

        /// <summary>
        ///   <para>获取或添加行动者组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetOrAddComponent<T>(Actor actor) where T : struct, IComponent
            => ref (HasComponent<T>(actor) ? ref GetComponent<T>(actor) : ref AddComponent<T>(actor));

        /// <summary>
        ///   <para>尝试获取行动者组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        /// <param name="component">组件数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(Actor actor, out T component)
            where T : struct, IComponent
            => Actors.TryGetComponent(actor, out component);

        /// <summary>
        ///   <para>移除行动者组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent<T>(Actor actor)
            where T : struct, IComponent
        {
            ValidateStructuralChange();
            return Actors.RemoveComponent<T>(actor);
        }

        /// <summary>
        ///   <para>设置行动者组件数据。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        /// <param name="component">组件数据</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent<T>(Actor actor, in T component)
            where T : struct, IComponent
        {
            ValidateStructuralChange(true);
            Actors.SetComponent(actor, component);
        }

        /// <summary>
        ///   <para>判断行动者是否拥有组件。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(Actor actor)
            where T : struct, IComponent
            => Actors.HasComponent<T>(actor);

        /// <summary>
        ///   <para>按运行时类型查询行动者是否拥有组件。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="componentType">组件类型。</param>
        internal bool HasComponent(Actor actor, Type componentType)
            => Actors.HasComponentByTypeId(actor, ComponentTypeRegistry.GetTypeId(componentType));

        /// <summary>
        ///   <para>判断行动者是否存活。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActorAlive(Actor actor) => !IsDisposed && Actors.IsActorAlive(actor);

        /// <summary>
        ///   <para>登记一个对应当前行动者的外部对象。</para>
        /// </summary>
        /// <param name="actor">对象对应的行动者。</param>
        /// <param name="actorObject">外部对象。</param>
        /// <param name="detach">由世界触发的解除回调。</param>
        internal void AttachActorObject(Actor actor, IActorObject actorObject, Action detach)
        {
            ValidateStructuralChange(true);
            if (actorObject == null) throw new ArgumentNullException(nameof(actorObject));
            if (detach == null) throw new ArgumentNullException(nameof(detach));
            if (!IsActorAlive(actor))
                throw new InvalidOperationException($"Actor {actor} is not alive.");
            m_Objects.Add(actor, actorObject, detach);
        }

        /// <summary>
        ///   <para>解除一个外部对象与行动者的对应关系。</para>
        /// </summary>
        /// <param name="actorObject">外部对象。</param>
        /// <returns>是否找到并解除。</returns>
        internal bool DetachActorObject(IActorObject actorObject)
        {
            ValidateStructuralChange();
            return m_Objects.Remove(actorObject);
        }

        /// <summary>
        ///   <para>添加能力。</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T AddCapability<T>(Actor actor)
            where T : Capability, new()
        {
            ValidateStructuralChange(true);
            return Capabilities.AddCapability<T>(actor);
        }

        /// <summary>
        ///   <para>移除指定能力。</para>
        /// </summary>
        /// <typeparam name="T">能力类型</typeparam>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveCapability<T>(Actor actor)
            where T : Capability
        {
            ValidateStructuralChange();
            return Capabilities.RemoveCapability<T>(actor);
        }

        /// <summary>
        ///   <para>阻塞标签。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="tagId">标签ID</param>
        /// <param name="instigator">触发者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BlockTag(Actor actor, TagId tagId, object instigator)
            => Capabilities.BlockTag(actor, tagId, instigator);

        /// <summary>
        ///   <para>解除阻塞标签。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="tagId">标签ID</param>
        /// <param name="instigator">触发者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UnblockTag(Actor actor, TagId tagId, object instigator)
            => Capabilities.UnblockTag(actor, tagId, instigator);

        /// <summary>
        ///   <para>查询标签是否被阻塞。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="tagId">标签ID</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTagBlocked(Actor actor, TagId tagId)
            => Capabilities.IsTagBlocked(actor, tagId);

        /// <summary>
        ///   <para>应用表单。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="sheet">表单</param>
        /// <param name="mode">应用模式</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SheetInstance ApplySheet(Actor actor, CapabilitySheet sheet, CapabilitySheetApplyMode mode = CapabilitySheetApplyMode.All)
            => Sheets.ApplySheet(actor, sheet, mode);

        /// <summary>
        ///   <para>移除指定表单实例。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        /// <param name="instance">表单实例</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveSheet(Actor actor, SheetInstance instance)
            => Sheets.RemoveSheet(actor, instance);

        /// <summary>
        ///   <para>移除全部表单。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAllSheets(Actor actor)
            => Sheets.RemoveAllSheets(actor);

        /// <summary>
        ///   <para>获取表单实例列表。</para>
        /// </summary>
        /// <param name="actor">目标行动者</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<SheetInstance> GetActorSheets(Actor actor)
            => Sheets.GetActorSheets(actor);

        /// <summary>
        ///   <para>查询拥有指定组件的全部行动者。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<Actor> QueryActorsWithComponent<T>()
            where T : struct, IComponent
            => Actors.QueryActorsWithComponent<T>();

        /// <summary>
        ///   <para>查询同时拥有指定多组件的全部行动者。</para>
        /// </summary>
        /// <param name="componentTypes">组件类型列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<Actor> QueryActorsWithComponents(params Type[] componentTypes)
            => Actors.QueryActorsWithComponents(componentTypes);

        /// <summary>
        ///   <para>查询拥有指定组件的全部行动者。</para>
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="result">结果列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueryActorsWithComponentNonAlloc<T>(List<Actor> result)
            where T : struct, IComponent
            => Actors.QueryActorsWithComponentNonAlloc<T>(result);

        /// <summary>
        ///   <para>查询同时拥有指定多组件的全部行动者。</para>
        /// </summary>
        /// <param name="result">结果列表</param>
        /// <param name="componentTypes">组件类型列表</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QueryActorsWithComponentsNonAlloc(List<Actor> result, params Type[] componentTypes)
            => Actors.QueryActorsWithComponentsNonAlloc(componentTypes, result);

        /// <summary>
        ///   <para>向世界线程派发一个任务（可从任意线程调用，在下一次 Tick 时执行）</para>
        /// </summary>
        /// <param name="action">任务逻辑</param>
        public bool Enqueue(Action<World> action)
        {
            if (action == null || IsDisposed) return false;

            bool lockTaken = false;
            try
            {
                m_DispatchLock.Enter(ref lockTaken);
                if (IsDisposed) return false;

                if (m_DispatchWriteCount >= m_DispatchWriteBuffer.Length)
                    Array.Resize(ref m_DispatchWriteBuffer, m_DispatchWriteBuffer.Length * 2);

                m_DispatchWriteBuffer[m_DispatchWriteCount++] = action;
                return true;
            }
            finally
            {
                if (lockTaken) m_DispatchLock.Exit();
            }
        }

        /// <summary>
        ///   <para>交换动作缓冲区并按登记顺序执行。</para>
        /// </summary>
        private void ExecutePendingActions()
        {
            if (IsDisposed) return;

            Action<World>[] executeBuffer;
            int executeCount;

            bool lockTaken = false;
            try
            {
                m_DispatchLock.Enter(ref lockTaken);
                executeCount = m_DispatchWriteCount;
                if (executeCount <= 0) return;

                executeBuffer = m_DispatchWriteBuffer;
                m_DispatchWriteBuffer = m_DispatchExecuteBuffer;
                m_DispatchExecuteBuffer = executeBuffer;
                m_DispatchWriteCount = 0;
            }
            finally
            {
                if (lockTaken) m_DispatchLock.Exit();
            }

            List<Exception> errors = null;
            for (int i = 0; i < executeCount; i++)
            {
                var action = executeBuffer[i];
                executeBuffer[i] = null;
                if (action == null) continue;
                try
                {
                    action(this);
                }
                catch (Exception ex)
                {
                    (errors ??= new List<Exception>()).Add(ex);
                }
            }
            if (errors != null) throw new AggregateException("World dispatch actions failed.", errors);
        }

        /// <summary>
        ///   <para>执行指定<see cref="TickGroup"/>分组世界逻辑帧。</para>
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        /// <param name="tickGroup">更新分组</param>
        internal void Tick(float deltaTime, TickGroup tickGroup)
        {
            ValidateStructuralChange();
            if (deltaTime < 0 || !Game.NumberUtility.IsFinite(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (m_IsTicking) throw new InvalidOperationException("World Tick cannot be reentered.");
            m_IsTicking = true;
            try
            {
                float scaledDelta = deltaTime * m_TimeScale;
                ExecutePendingActions();
                Replication?.BeforeTick(deltaTime, tickGroup);
                Capabilities.Update(scaledDelta, tickGroup);
                Replication?.AfterTick(deltaTime, tickGroup);
            }
            finally { m_IsTicking = false; }
        }

        /// <summary>
        ///   <para>清理世界。</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            ValidateStructuralChange();
            if (m_IsTicking || Capabilities.IsUpdating || m_IsClearing)
                throw new InvalidOperationException("The world cannot be cleared during Tick or cleanup.");
            m_IsClearing = true;
            List<Exception> errors = null;
            try
            {
                try { m_Objects.Clear(); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                try { Capabilities.Clear(); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                try { Replication?.Close(); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                try { DebugTrace.Clear(); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
#endif
                try
                {
                    Actors.Clear();
                }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                try
                {
                    ClearDispatchQueue();
                }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            }
            finally { m_IsClearing = false; }
            if (errors != null) throw new AggregateException("Clearing the world failed.", errors);
        }

        /// <summary>
        ///   <para>清除尚未执行的世界线程任务。</para>
        /// </summary>
        private void ClearDispatchQueue()
        {
            bool lockTaken = false;
            try
            {
                m_DispatchLock.Enter(ref lockTaken);
                Array.Clear(m_DispatchWriteBuffer, 0, m_DispatchWriteCount);
                m_DispatchWriteCount = 0;
                Array.Clear(m_DispatchExecuteBuffer, 0, m_DispatchExecuteBuffer.Length);
            }
            finally
            {
                if (lockTaken) m_DispatchLock.Exit();
            }
        }

        /// <summary>
        ///   <para>检查结构变更边界；跨线程操作请通过 <see cref="Enqueue"/> 提交。</para>
        /// </summary>
        /// <param name="adding">是否会创建新资源。</param>
        internal void ValidateStructuralChange(bool adding = false)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(World));
            if (Replication?.IsReading == true) throw new InvalidOperationException("Replication policies cannot change world structure.");
            if (Environment.CurrentManagedThreadId != m_OwnerThread)
                throw new InvalidOperationException("Submit structural changes to the world thread with Enqueue.");
            if (adding && m_IsClearing)
                throw new InvalidOperationException("Resources cannot be created while the world is clearing.");
        }
    }
}
