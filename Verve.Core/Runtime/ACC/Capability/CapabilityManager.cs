// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve
{
    using System;
    using System.Buffers;
    using System.Threading;
    using System.Runtime.ExceptionServices;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///   <para>能力管理器；集中拥有能力实例和 Tick 队列。</para>
    /// </summary>
    [Serializable]
    public sealed class CapabilityManager
    {
        /// <summary>
        ///   <para>延迟命令类型；描述更新期间要执行的能力操作。</para>
        /// </summary>
        private enum DeferredCommandKind : byte
        {
            /// <summary>
            ///   <para>延迟添加能力。</para>
            /// </summary>
            Add = 1,
            /// <summary>
            ///   <para>延迟移除能力。</para>
            /// </summary>
            Remove = 2,
            /// <summary>
            ///   <para>延迟销毁行动者；保留组件直到能力回调结束。</para>
            /// </summary>
            DestroyActor = 3,
        }

        /// <summary>
        ///   <para>延迟命令；保存目标行动者和能力。</para>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct DeferredCommand
        {
            /// <summary>
            ///   <para>命令类型。</para>
            /// </summary>
            public DeferredCommandKind kind;
            /// <summary>
            ///   <para>命令目标行动者。</para>
            /// </summary>
            public Actor actor;
            /// <summary>
            ///   <para>命令涉及的能力。</para>
            /// </summary>
            public Capability capability;
        }

        /// <summary>
        ///   <para>所属世界。</para>
        /// </summary>
        private readonly World m_World;
        /// <summary>
        ///   <para>能力 Tick 调度队列。</para>
        /// </summary>
        private readonly ActionQueue m_ActionQueue;
        /// <summary>
        ///   <para>表单实例管理器。</para>
        /// </summary>
        private readonly SheetManager m_SheetManager;
        /// <summary>
        ///   <para>正在销毁的行动者。</para>
        /// </summary>
        private readonly HashSet<Actor> m_DestroyingActors = new(16);
        /// <summary>
        ///   <para>能力激活判断委托。</para>
        /// </summary>
        private readonly Func<Capability, bool> m_ShouldActivate;
        /// <summary>
        ///   <para>能力停用判断委托。</para>
        /// </summary>
        private readonly Func<Capability, bool> m_ShouldDeactivate;
        /// <summary>
        ///   <para>已加入延迟移除集合的能力。</para>
        /// </summary>
        private readonly HashSet<Capability> m_DeferredRemovals = new(ReferenceEqualityComparer<Capability>.Instance);
        /// <summary>
        ///   <para>复用的失败移除委托。</para>
        /// </summary>
        private readonly Action<Capability> m_OnFailure;
        /// <summary>
        ///   <para>保护延迟命令和销毁集合。</para>
        /// </summary>
        private SpinLock m_Lock;
        /// <summary>
        ///   <para>延迟命令写入缓冲区。</para>
        /// </summary>
        private DeferredCommand[] m_DeferredCommands;
        /// <summary>
        ///   <para>延迟命令执行缓冲区。</para>
        /// </summary>
        private DeferredCommand[] m_DeferredExecutionBuffer;
        /// <summary>
        ///   <para>延迟命令数量。</para>
        /// </summary>
        private int m_DeferredCommandCount;
        /// <summary>
        ///   <para>是否正在更新。</para>
        /// </summary>
        private int m_IsUpdating;
        /// <summary>
        ///   <para>是否正在清理。</para>
        /// </summary>
        private int m_IsClearing;
        /// <summary>
        ///   <para>是否已释放。</para>
        /// </summary>
        private bool m_IsDisposed;

        /// <summary>
        ///   <para>是否正在能力更新。</para>
        /// </summary>
        internal bool IsUpdating => Volatile.Read(ref m_IsUpdating) != 0;
        /// <summary>
        ///   <para>表单管理器；批量应用和移除能力、组件。</para>
        /// </summary>
        public SheetManager Sheets { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => m_SheetManager; }

        /// <summary>
        ///   <para>构造能力管理器。</para>
        /// </summary>
        /// <param name="world">所属世界。</param>
        internal CapabilityManager(World world)
        {
            m_World = world ?? throw new ArgumentNullException(nameof(world));
            m_ActionQueue = new ActionQueue();
            m_SheetManager = new SheetManager(world);
            m_Lock = new SpinLock(false);
            m_ShouldActivate = CheckCapabilityActivation;
            m_ShouldDeactivate = CheckCapabilityDeactivation;
            m_OnFailure = OnCapabilityFailure;
        }

        /// <summary>
        ///   <para>释放能力管理器。</para>
        /// </summary>
        internal void Dispose()
        {
            if (m_IsDisposed) return;
            if (Volatile.Read(ref m_IsUpdating) != 0)
                throw new InvalidOperationException("The capability manager cannot be disposed while updating.");

            List<Exception> errors = null;
            try { Clear(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }

            if (HasOwnedCapabilities())
            {
                var ownershipException = new InvalidOperationException("The capability manager could not be disposed because owned capabilities remain.");
                if (errors == null) throw ownershipException;
                errors.Add(ownershipException);
                throw new AggregateException("The capability manager could not release all owned capabilities.", errors);
            }

            m_IsDisposed = true;
            if (m_DeferredCommands != null)
            {
                ArrayPool<DeferredCommand>.Shared.Return(m_DeferredCommands, true);
                m_DeferredCommands = null;
            }
            if (m_DeferredExecutionBuffer != null)
            {
                ArrayPool<DeferredCommand>.Shared.Return(m_DeferredExecutionBuffer, true);
                m_DeferredExecutionBuffer = null;
            }

            if (errors != null)
                throw new AggregateException("Disposing the capability manager failed.", errors);
        }

        /// <summary>
        ///   <para>为行动者添加能力；实例由当前管理器拥有。</para>
        /// </summary>
        /// <typeparam name="T">能力类型。</typeparam>
        /// <param name="actor">行动者。</param>
        internal T AddCapability<T>(Actor actor)
            where T : Capability, new()
        {
            m_World.ValidateStructuralChange(true);
            if (m_IsDisposed) throw new ObjectDisposedException(nameof(CapabilityManager));
            if (Volatile.Read(ref m_IsClearing) != 0)
                throw new InvalidOperationException("Capabilities cannot be added while the manager is clearing.");
            if (!m_World.IsActorAlive(actor))
                throw new InvalidOperationException("Cannot add a capability to a dead actor.");
            if (IsActorDestroying(actor))
                throw new InvalidOperationException("Cannot add a capability while its actor is being destroyed.");

            var capability = new T();
            try
            {
                capability.Setup(actor, m_World);

                if (Volatile.Read(ref m_IsClearing) != 0)
                    throw new InvalidOperationException("Capabilities cannot be added while the manager is clearing.");
                if (IsActorDestroying(actor))
                    throw new InvalidOperationException("Cannot add a capability while its actor is being destroyed.");

                if (Volatile.Read(ref m_IsUpdating) != 0)
                {
                    EnqueueDeferred(DeferredCommandKind.Add, actor, capability);
                    return capability;
                }

                if (!AttachCapability(actor, capability))
                    throw new InvalidOperationException("The actor was destroyed while adding a capability.");
                return capability;
            }
            catch (Exception addException)
            {
                try
                {
                    capability.Release();
                }
                catch (Exception releaseException)
                {
                    throw new AggregateException("Adding the capability failed and its cleanup also failed.", addException, releaseException);
                }
                throw;
            }
        }

        /// <summary>
        ///   <para>移除行动者上的指定能力并释放其资源。</para>
        /// </summary>
        /// <typeparam name="T">能力类型。</typeparam>
        /// <param name="actor">行动者。</param>
        internal bool RemoveCapability<T>(Actor actor)
            where T : Capability
        {
            if (m_IsDisposed) return false;

            var capabilities = m_World.Actors.GetCapabilities(actor);
            if (capabilities == null) return false;

            for (int i = 0; i < capabilities.Count; i++)
            {
                var instance = capabilities[i];
                if (instance.capability is T)
                    return RemoveCapability(actor, instance.capability);
            }

            return false;
        }
        /// <summary>
        ///   <para>移除行动者上的指定能力实例。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="capability">待移除能力。</param>
        internal bool RemoveCapability(Actor actor, Capability capability)
        {
            m_World.ValidateStructuralChange();
            if (m_IsDisposed || capability == null) return false;
            if (capability.OwnerWorld != m_World || capability.OwnerActor != actor) return false;
            if (!m_World.IsActorAlive(actor)) return false;
            if (Volatile.Read(ref m_IsUpdating) != 0)
            {
                return EnqueueDeferred(DeferredCommandKind.Remove, actor, capability);
            }

            if (!m_World.Actors.RemoveCapabilityInstance(actor, capability))
                return false;

            DetachCapability(actor, capability);
            return true;
        }

        /// <summary>
        ///   <para>移除行动者上的全部能力。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        internal void RemoveAllCapabilities(Actor actor)
        {
            if (m_IsDisposed) return;

            var capabilities = m_World.Actors.GetCapabilities(actor);
            if (capabilities == null || capabilities.Count == 0) return;

            if (Volatile.Read(ref m_IsUpdating) != 0)
            {
                for (int i = 0; i < capabilities.Count; i++)
                {
                    var capability = capabilities[i].capability;
                    if (capability == null) continue;
                    RemoveCapability(actor, capability);
                }
                return;
            }

            RemoveAllCapabilitiesNow(actor);
        }

        /// <summary>
        ///   <para>立即释放行动者能力；仅在 Tick 之外或提交阶段调用。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        internal void RemoveAllCapabilitiesNow(Actor actor)
        {
            var capabilities = m_World.Actors.GetCapabilities(actor);
            if (capabilities == null) return;
            List<Exception> errors = null;
            for (int i = capabilities.Count - 1; i >= 0; i--)
            {
                var capability = capabilities[i].capability;
                if (capability == null) continue;

                if (!m_World.Actors.RemoveCapabilityInstance(actor, capability))
                    continue;
                try { DetachCapability(actor, capability); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            }

            if (errors != null)
                throw new AggregateException("One or more capabilities could not be removed.", errors);
        }
        /// <summary>
        ///   <para>登记延迟销毁并停止该行动者余下的能力回调。</para>
        /// </summary>
        /// <param name="actor">待销毁行动者。</param>
        internal void DeferActorDestruction(Actor actor)
        {
            if (IsActorDestroying(actor)) return;
            BeginActorDestruction(actor);
            try { EnqueueDeferred(DeferredCommandKind.DestroyActor, actor, null); }
            catch { EndActorDestruction(actor); throw; }
            var capabilities = m_World.Actors.GetCapabilities(actor);
            if (capabilities == null) return;
            foreach (var instance in capabilities) instance.capability.IsRemovalPending = true;
        }

        /// <summary>
        ///   <para>登记行动者进入销毁阶段。</para>
        /// </summary>
        /// <param name="actor">正在销毁的行动者。</param>
        internal void BeginActorDestruction(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                if (!m_DestroyingActors.Add(actor))
                    throw new InvalidOperationException("The actor is already being destroyed.");
            }
            finally { if (lockTaken) m_Lock.Exit(); }
        }

        /// <summary>
        ///   <para>结束行动者销毁阶段登记。</para>
        /// </summary>
        /// <param name="actor">已结束销毁的行动者。</param>
        internal void EndActorDestruction(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                m_DestroyingActors.Remove(actor);
            }
            finally { if (lockTaken) m_Lock.Exit(); }
        }

        /// <summary>
        ///   <para>阻塞行动者上指定标签（由发起者记录）</para>
        /// </summary>
        internal void BlockTag(Actor actor, TagId tagId, object instigator)
        {
            m_World.ValidateStructuralChange(true);
            m_World.Actors.BlockTag(actor, tagId, instigator);
        }

        /// <summary>
        ///   <para>解除行动者上指定标签的阻塞。</para>
        /// </summary>
        internal void UnblockTag(Actor actor, TagId tagId, object instigator)
        {
            m_World.ValidateStructuralChange();
            m_World.Actors.UnblockTag(actor, tagId, instigator);
        }

        /// <summary>
        ///   <para>查询行动者上的标签是否被阻塞。</para>
        /// </summary>
        internal bool IsTagBlocked(Actor actor, TagId tagId)
        {
            return m_World.Actors.IsTagBlocked(actor, tagId);
        }

        /// <summary>
        ///   <para>更新指定分组的能力系统。</para>
        /// </summary>
        internal void Update(float deltaTime, TickGroup tickGroup)
        {
            if (m_IsDisposed) return;
            BeginUpdate();
            Exception updateError = null;
            Exception cleanupError = null;
            try
            {
                m_ActionQueue.Update(deltaTime,
                    m_ShouldActivate,
                    m_ShouldDeactivate,
                    (int)tickGroup, m_OnFailure);
            }
            catch (Exception exception) { updateError = exception; }
            finally
            {
                try { ExecuteDeferredCommands(); }
                catch (Exception exception) { cleanupError = exception; }
                Volatile.Write(ref m_IsUpdating, 0);
            }
            ThrowUpdateErrors(updateError, cleanupError);
        }

        /// <summary>
        ///   <para>清理能力系统；释放表单与能力。</para>
        /// </summary>
        internal void Clear()
        {
            if (Interlocked.CompareExchange(ref m_IsClearing, 1, 0) != 0)
                throw new InvalidOperationException("The capability manager is already clearing.");
            try
            {
                if (Volatile.Read(ref m_IsUpdating) != 0)
                    throw new InvalidOperationException("Capabilities cannot be cleared while the manager is updating.");
                ClearCore();
            }
            finally { Volatile.Write(ref m_IsClearing, 0); }
        }

        /// <summary>
        ///   <para>执行不带并发状态切换的清理流程。</para>
        /// </summary>
        private void ClearCore()
        {
            List<Exception> errors = null;
            try { m_SheetManager.Clear(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }

            var actors = new List<Actor>();
            m_World.Actors.GetAliveActors(actors);
            for (int i = 0; i < actors.Count; i++)
            {
                try { RemoveAllCapabilities(actors[i]); }
                catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
            }

            List<Capability> pendingReleases = null;
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                for (int i = 0; i < m_DeferredCommandCount; i++)
                {
                    ref var command = ref m_DeferredCommands[i];
                    if (command.kind == DeferredCommandKind.Add && command.capability != null)
                        (pendingReleases ??= new List<Capability>()).Add(command.capability);
                    else if (command.kind == DeferredCommandKind.Remove && command.capability != null &&
                             !m_World.IsActorAlive(command.actor) && command.capability.OwnerActor == command.actor)
                        (pendingReleases ??= new List<Capability>()).Add(command.capability);
                }
                m_DeferredCommandCount = 0;
                m_DeferredRemovals.Clear();
                if (m_DeferredCommands != null) Array.Clear(m_DeferredCommands, 0, m_DeferredCommands.Length);
                if (m_DeferredExecutionBuffer != null) Array.Clear(m_DeferredExecutionBuffer, 0, m_DeferredExecutionBuffer.Length);
                m_DestroyingActors.Clear();
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }

            if (pendingReleases != null)
                for (int i = 0; i < pendingReleases.Count; i++)
                    try { pendingReleases[i].Release(); }
                    catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }

            m_ActionQueue.Clear();
            if (errors != null)
                throw new AggregateException("One or more capabilities could not be cleared.", errors);
        }

        /// <summary>
        ///   <para>检查世界中是否仍存在管理器拥有的能力。</para>
        /// </summary>
        private bool HasOwnedCapabilities()
        {
            var actors = new List<Actor>();
            m_World.Actors.GetAliveActors(actors);
            for (int i = 0; i < actors.Count; i++)
            {
                var capabilities = m_World.Actors.GetCapabilities(actors[i]);
                if (capabilities != null && capabilities.Count != 0) return true;
            }

            return false;
        }

        /// <summary>
        ///   <para>判断行动者是否正在销毁。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        private bool IsActorDestroying(Actor actor)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                return m_DestroyingActors.Contains(actor);
            }
            finally { if (lockTaken) m_Lock.Exit(); }
        }

        /// <summary>
        ///   <para>进入单线程更新区间并拒绝并发更新。</para>
        /// </summary>
        private void BeginUpdate()
        {
            m_World.ValidateStructuralChange();
            if (Volatile.Read(ref m_IsClearing) != 0 ||
                Interlocked.CompareExchange(ref m_IsUpdating, 1, 0) != 0)
                throw new InvalidOperationException("The capability manager cannot run concurrent updates or update while clearing.");

            if (Volatile.Read(ref m_IsClearing) == 0) return;
            Volatile.Write(ref m_IsUpdating, 0);
            throw new InvalidOperationException("The capability manager cannot update while clearing.");
        }

        /// <summary>
        ///   <para>将回调失败的能力移出调度；释放在当前 Tick 结束后执行。</para>
        /// </summary>
        /// <param name="capability">发生异常的能力。</param>
        private void OnCapabilityFailure(Capability capability)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_World.DebugTrace.Record(capability.OwnerActor, capability, CapabilityDebugEventKind.Failed);
#endif
            EnqueueDeferred(DeferredCommandKind.Remove, capability.OwnerActor, capability);
        }

        /// <summary>
        ///   <para>保留更新异常并报告清理异常。</para>
        /// </summary>
        /// <param name="updateError">更新异常。</param>
        /// <param name="cleanupError">清理异常。</param>
        private static void ThrowUpdateErrors(Exception updateError, Exception cleanupError)
        {
            if (updateError != null && cleanupError != null)
                throw new AggregateException("Capability update and deferred cleanup failed.", updateError, cleanupError);
            if (updateError != null) ExceptionDispatchInfo.Capture(updateError).Throw();
            if (cleanupError != null) ExceptionDispatchInfo.Capture(cleanupError).Throw();
        }

        /// <summary>
        ///   <para>判断能力是否满足激活条件。</para>
        /// </summary>
        /// <param name="capability">待检查能力。</param>
        private bool CheckCapabilityActivation(Capability capability)
        {
            var owner = capability.OwnerActor;
            foreach (var tag in capability.Tags)
            {
                if (m_World.Actors.IsTagBlocked(owner, tag))
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (m_World.DebugTrace.IsEnabled)
                    {
                        if (capability.SetDebugActivationBlocked(true))
                        {
                            var tagName = TagRegistry.GetTagName(tag);
                            m_World.DebugTrace.Record(
                                owner,
                                capability,
                                CapabilityDebugEventKind.Blocked,
                                $"标签 {(string.IsNullOrEmpty(tagName) ? tag.ToString() : tagName)} 被阻塞");
                        }
                    }
                    else
                    {
                        capability.SetDebugActivationBlocked(false);
                    }
#endif
                    return false;
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string blockedReason = null;
            if (!capability.RequiredComponents.IsEmpty || !capability.BlockedComponents.IsEmpty)
            {
                var actorMask = m_World.Actors.GetComponentMask(owner);
                if (!actorMask.ContainsAll(capability.RequiredComponents))
                    blockedReason = "缺少所需组件";
                else if (actorMask.ContainsAny(capability.BlockedComponents))
                    blockedReason = "存在阻止组件";
            }

            var canActivate = blockedReason == null && capability.ShouldActivate();
            if (!canActivate && m_World.DebugTrace.IsEnabled)
            {
                if (capability.SetDebugActivationBlocked(true))
                    m_World.DebugTrace.Record(
                        owner,
                        capability,
                        CapabilityDebugEventKind.Blocked,
                        blockedReason ?? "能力自身条件未满足");
            }
            else if (canActivate || !m_World.DebugTrace.IsEnabled)
            {
                capability.SetDebugActivationBlocked(false);
            }
            return canActivate;
#else
            return capability.ShouldActivate();
#endif
        }
        /// <summary>
        ///   <para>判断能力是否满足停用条件。</para>
        /// </summary>
        /// <param name="capability">待检查能力。</param>
        private bool CheckCapabilityDeactivation(Capability capability)
        {
            var owner = capability.OwnerActor;
            foreach (var tag in capability.Tags)
            {
                if (m_World.Actors.IsTagBlocked(owner, tag))
                    return true;
            }

            return capability.ShouldDeactivate();
        }

        /// <summary>
        ///   <para>加入更新期间的延迟命令队列。</para>
        /// </summary>
        /// <param name="kind">命令类型。</param>
        /// <param name="actor">命令目标行动者。</param>
        /// <param name="capability">命令涉及能力。</param>
        private bool EnqueueDeferred(DeferredCommandKind kind, Actor actor, Capability capability)
        {
            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                if (kind == DeferredCommandKind.Remove && !m_DeferredRemovals.Add(capability))
                    return false;

                if (m_DeferredCommands == null)
                    m_DeferredCommands = ArrayPool<DeferredCommand>.Shared.Rent(32);
                else if (m_DeferredCommandCount >= m_DeferredCommands.Length)
                    GrowDeferredBufferUnsafe();

                if (kind == DeferredCommandKind.Remove) capability.IsRemovalPending = true;
                m_DeferredCommands[m_DeferredCommandCount++] = new DeferredCommand
                {
                    kind = kind,
                    actor = actor,
                    capability = capability
                };
                return true;
            }
            catch
            {
                if (kind == DeferredCommandKind.Remove)
                    m_DeferredRemovals.Remove(capability);
                throw;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }
        }

        /// <summary>
        ///   <para>在已持有锁时扩展延迟命令缓冲区。</para>
        /// </summary>
        private void GrowDeferredBufferUnsafe()
        {
            var old = m_DeferredCommands;
            var next = ArrayPool<DeferredCommand>.Shared.Rent(old.Length * 2);
            Array.Copy(old, 0, next, 0, m_DeferredCommandCount);
            ArrayPool<DeferredCommand>.Shared.Return(old, true);
            m_DeferredCommands = next;
        }

        /// <summary>
        ///   <para>执行本次更新产生的延迟命令。</para>
        /// </summary>
        private void ExecuteDeferredCommands()
        {
            DeferredCommand[] buffer;
            int count;

            bool lockTaken = false;
            try
            {
                m_Lock.Enter(ref lockTaken);
                count = m_DeferredCommandCount;
                if (count <= 0) return;
                if (m_DeferredExecutionBuffer == null || m_DeferredExecutionBuffer.Length < count)
                {
                    if (m_DeferredExecutionBuffer != null)
                        ArrayPool<DeferredCommand>.Shared.Return(m_DeferredExecutionBuffer, false);
                    m_DeferredExecutionBuffer = ArrayPool<DeferredCommand>.Shared.Rent(count);
                }
                buffer = m_DeferredCommands;
                m_DeferredCommands = m_DeferredExecutionBuffer;
                m_DeferredExecutionBuffer = buffer;
                m_DeferredCommandCount = 0;
            }
            finally
            {
                if (lockTaken) m_Lock.Exit();
            }

            try
            {
                List<Exception> errors = null;
                for (int i = 0; i < count; i++)
                {
                    ref var cmd = ref buffer[i];
                    try
                    {
                        switch (cmd.kind)
                        {
                            case DeferredCommandKind.Add:
                                if (cmd.capability != null && !cmd.capability.IsReleased && m_World.IsActorAlive(cmd.actor) && !IsActorDestroying(cmd.actor))
                                    AttachCapability(cmd.actor, cmd.capability);
                                else
                                    cmd.capability?.Release();
                                break;
                            case DeferredCommandKind.Remove:
                                ExecuteDeferredRemoval(cmd.actor, cmd.capability);
                                break;
                            case DeferredCommandKind.DestroyActor:
                                m_World.DestroyActorNow(cmd.actor);
                                break;
                            default:
                                throw new InvalidOperationException($"Unknown deferred capability command: {cmd.kind}.");
                        }
                    }
                    catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
                    finally
                    {
                        if (cmd.kind == DeferredCommandKind.Remove)
                        {
                            bool gateTaken = false;
                            try
                            {
                                m_Lock.Enter(ref gateTaken);
                                m_DeferredRemovals.Remove(cmd.capability);
                            }
                            finally { if (gateTaken) m_Lock.Exit(); }
                        }
                    }
                }

                if (errors != null)
                    throw new AggregateException("One or more deferred capability commands failed.", errors);
            }
            finally
            {
                Array.Clear(buffer, 0, count);
            }
        }

        /// <summary>
        ///   <para>将能力挂接到行动者并加入 Tick 队列。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="capability">待挂接能力。</param>
        private bool AttachCapability(Actor actor, Capability capability)
        {
            var instance = new CapabilityInstance
            {
                capability = capability,
                tickGroup = (int)capability.TickGroup,
                tickOrder = capability.TickOrder
            };
            if (!m_World.Actors.AddCapabilityInstance(actor, instance))
            {
                capability.Release();
                return false;
            }

            if (Volatile.Read(ref m_IsClearing) != 0 || IsActorDestroying(actor))
            {
                m_World.Actors.RemoveCapabilityInstance(actor, capability);
                capability.Release();
                return false;
            }

            try
            {
                m_ActionQueue.AddCapability(capability, (int)capability.TickGroup, capability.TickOrder);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                m_World.DebugTrace.Record(actor, capability, CapabilityDebugEventKind.Added);
#endif
                return true;
            }
            catch (Exception addException)
            {
                m_World.Actors.RemoveCapabilityInstance(actor, capability);
                m_ActionQueue.RemoveCapability(capability);
                try { capability.Release(); }
                catch (Exception releaseException)
                {
                    throw new AggregateException("Attaching the capability failed and cleanup also failed.", addException, releaseException);
                }
                throw;
            }
        }

        /// <summary>
        ///   <para>执行一条延迟移除命令。</para>
        /// </summary>
        /// <param name="actor">目标行动者。</param>
        /// <param name="capability">待移除能力。</param>
        private void ExecuteDeferredRemoval(Actor actor, Capability capability)
        {
            if (capability == null) return;
            if (!m_World.IsActorAlive(actor))
            {
                m_ActionQueue.RemoveCapability(capability);
                capability.Release();
                return;
            }
            if (!m_World.Actors.RemoveCapabilityInstance(actor, capability)) return;
            DetachCapability(actor, capability);
        }

        /// <summary>
        ///   <para>从 Tick 队列解除能力并释放实例。</para>
        /// </summary>
        /// <param name="actor">能力所属行动者。</param>
        /// <param name="capability">待解除能力。</param>
        private void DetachCapability(Actor actor, Capability capability)
        {
            List<Exception> errors = null;
            try { m_ActionQueue.RemoveCapability(capability); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }

            try { capability.Release(); }
            catch (Exception exception) { (errors ??= new List<Exception>()).Add(exception); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_World.DebugTrace.Record(actor, capability, CapabilityDebugEventKind.Removed);
#endif

            if (errors != null)
                throw new AggregateException($"Removing {capability.GetType().Name} failed.", errors);
        }
    }
}
