// Copyright (c) 2025-2026 Benfach <hong125841@gmail.com>

namespace Verve.Tests.ACC
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;

    /// <summary>
    ///   <para>ACC 边界回归；覆盖调度、资源释放、并发和网络故障。</para>
    /// </summary>
    [Category("ACC")]
    public class AccBoundaryTests
    {
        /// <summary>
        ///   <para>测试数据。</para>
        /// </summary>
        public struct Value : IComponent
        {
            /// <summary>
            ///   <para>数值。</para>
            /// </summary>
            public int Number;
        }

        /// <summary>
        ///   <para>数学压力数据；保存独立计算状态。</para>
        /// </summary>
        public struct MathState : IComponent
        {
            /// <summary>
            ///   <para>计算值。</para>
            /// </summary>
            public float Value;
            /// <summary>
            ///   <para>相位值。</para>
            /// </summary>
            public float Phase;
            /// <summary>
            ///   <para>已完成步数。</para>
            /// </summary>
            public int Steps;
        }

        /// <summary>
        ///   <para>可观察能力；测试自定义条件和清理失败。</para>
        /// </summary>
        public class Probe : Capability
        {
            /// <summary>
            ///   <para>更新次数。</para>
            /// </summary>
            public int Ticks;
            /// <summary>
            ///   <para>释放次数。</para>
            /// </summary>
            public int Releases;
            /// <summary>
            ///   <para>可选更新行为。</para>
            /// </summary>
            public Action TickAction;
            /// <summary>
            ///   <para>可选释放行为。</para>
            /// </summary>
            public Action ReleaseAction;
            /// <summary>
            ///   <para>激活开关。</para>
            /// </summary>
            public bool Enabled = true;
            /// <inheritdoc />
            protected internal override bool ShouldActivate() => Enabled;
            /// <inheritdoc />
            protected internal override bool ShouldDeactivate() => !Enabled;
            /// <inheritdoc />
            protected internal override void TickActive(in float deltaTime) { Ticks++; TickAction?.Invoke(); }
            /// <inheritdoc />
            protected override void Dispose(bool disposing) { Releases++; ReleaseAction?.Invoke(); }
            /// <summary>
            ///   <para>验证注册后修改调度参数被拒绝。</para>
            /// </summary>
            public void ChangeOrder() => SetTick(TickGroup.Late, 1);
        }

        /// <summary>
        ///   <para>从组件值读取条件。</para>
        /// </summary>
        public sealed class PollingProbe : Probe
        {
            /// <inheritdoc />
            protected internal override bool ShouldActivate() => this.GetComponent<Value>().Number > 0;
            /// <inheritdoc />
            protected internal override bool ShouldDeactivate() => this.GetComponent<Value>().Number <= 0;
        }

        /// <summary>
        ///   <para>数学能力；执行非线性计算并只写入所属行动者。</para>
        /// </summary>
        public sealed class ComplexMathCapability : Capability
        {
            /// <inheritdoc />
            protected override void OnSetup() => SetTick(TickGroup.Gameplay, 0);

            /// <inheritdoc />
            protected internal override void TickActive(in float deltaTime)
            {
                ref var state = ref this.GetComponent<MathState>();
                Compute(ref state);
            }

            /// <summary>
            ///   <para>执行一轮复杂数学计算。</para>
            /// </summary>
            /// <param name="state">待更新状态。</param>
            public static void Compute(ref MathState state)
            {
                double value = state.Value;
                double phase = state.Phase;
                for (int i = 0; i < 12; i++)
                {
                    double angle = value * 0.173 + phase;
                    double wave = Math.Sin(angle) + Math.Cos(angle * 0.371);
                    value = value * 0.907 + wave * 0.131 + Math.Sqrt(Math.Abs(value) + 1.0) * 0.017;
                    phase += 0.013 + wave * 0.0007;
                }
                state.Value = (float)value;
                state.Phase = (float)phase;
                state.Steps++;
            }
        }

        /// <summary>
        ///   <para>生命周期探针；记录回调顺序和执行线程。</para>
        /// </summary>
        public sealed class LifecycleProbe : Probe
        {
            /// <summary>
            ///   <para>回调记录。</para>
            /// </summary>
            public List<(Actor actor, string phase, int thread)> Calls;
            /// <summary>
            ///   <para>激活操作；验证回调中的移除请求。</para>
            /// </summary>
            public Action ActivateAction;

            /// <inheritdoc />
            protected internal override bool ShouldActivate() { Record("check activate"); return base.ShouldActivate(); }
            /// <inheritdoc />
            protected internal override void OnActivated() { Record("activate"); ActivateAction?.Invoke(); }
            /// <inheritdoc />
            protected internal override void TickActive(in float deltaTime) { Record("tick"); base.TickActive(deltaTime); }
            /// <inheritdoc />
            protected internal override bool ShouldDeactivate() { Record("check deactivate"); return base.ShouldDeactivate(); }
            /// <inheritdoc />
            protected internal override void OnDeactivated() => Record("deactivate");
            /// <inheritdoc />
            protected override void Dispose(bool disposing) { Record("release"); base.Dispose(disposing); }

            /// <summary>
            ///   <para>记录一次回调。</para>
            /// </summary>
            /// <param name="phase">回调名称。</param>
            private void Record(string phase) => Calls.Add((OwnerActor, phase, Environment.CurrentManagedThreadId));
        }

        /// <summary>
        ///   <para>靠后执行的能力。</para>
        /// </summary>
        public sealed class LateProbe : Probe
        {
            /// <inheritdoc />
            protected override void OnSetup() => SetTick(TickGroup.Gameplay, 20);
        }

        /// <summary>
        ///   <para>重复清空、跨世界和高压复用均不使旧句柄复活。</para>
        /// </summary>
        /// <param name="storageThreadSafety">是否启用存储访问锁。</param>
        [TestCase(false), TestCase(true)]
        public void Identity_RemainsUniqueAcrossWorldsAndClear(bool storageThreadSafety)
        {
            using var first = new World("first", new ActorManagerOptions(64, storageThreadSafety, 2));
            using var second = new World("second", new ActorManagerOptions(64, storageThreadSafety, 2));
            var previous = new HashSet<Actor>();
            for (int round = 0; round < 32; round++)
            {
                var actors = first.CreateActors(2048);
                var foreign = second.CreateActor();
                Assert.That(first.IsActorAlive(foreign), Is.False);
                foreach (var actor in actors)
                {
                    Assert.That(previous.Add(actor), Is.True);
                    Assert.That(second.IsActorAlive(actor), Is.False);
                }
                first.Clear();
                second.Clear();
            }
            foreach (var actor in previous) Assert.That(first.IsActorAlive(actor), Is.False);
        }

        /// <summary>
        ///   <para>直接通过组件引用修改值也会触发下一帧条件判断。</para>
        /// </summary>
        [Test]
        public void Scheduler_PollsWithoutDirtyNotifications()
        {
            using var world = new World("poll");
            var actor = world.CreateActor();
            world.AddComponent<Value>(actor);
            var probe = world.AddCapability<PollingProbe>(actor);
            world.Tick(1, TickGroup.Gameplay);
            Assert.That(probe.Ticks, Is.Zero);
            world.GetComponent<Value>(actor).Number = 1;
            world.Tick(1, TickGroup.Late);
            Assert.That(probe.IsActive, Is.False);
            world.Tick(1, TickGroup.Gameplay);
            Assert.That(probe.Ticks, Is.EqualTo(1));
            world.GetComponent<Value>(actor).Number = 0;
            world.Tick(1, TickGroup.Gameplay);
            Assert.That(probe.IsActive, Is.False);
            Assert.That(probe.Ticks, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(probe.ChangeOrder);
        }

        /// <summary>
        ///   <para>随机移除后，同序注册顺序和跨序屏障仍保持稳定。</para>
        /// </summary>
        [Test]
        public void Scheduler_PreservesOrderAfterRemovalAndReactivation()
        {
            using var world = new World("order");
            var actor = world.CreateActor();
            var calls = new List<int>();
            var probes = new List<Probe>();
            for (int i = 0; i < 1024; i++)
            {
                int id = i;
                var probe = world.AddCapability<Probe>(actor);
                probe.TickAction = () => calls.Add(id);
                probes.Add(probe);
            }
            world.AddCapability<LateProbe>(actor).TickAction = () => calls.Add(9999);
            for (int i = 0; i < probes.Count; i += 3) world.Capabilities.RemoveCapability(actor, probes[i]);
            for (int round = 0; round < 3; round++)
            {
                calls.Clear();
                world.Tick(1, TickGroup.Gameplay);
                Assert.That(calls, Is.EqualTo(Enumerable.Range(0, 1024).Where(i => i % 3 != 0).Append(9999)));
                foreach (var probe in probes) probe.Enabled = !probe.Enabled;
                world.Tick(1, TickGroup.Gameplay);
                foreach (var probe in probes) probe.Enabled = true;
            }
        }

        /// <summary>
        ///   <para>更新和清理同时失败时仍保留两种异常并完成释放。</para>
        /// </summary>
        [Test]
        public void Failure_PreservesTickAndCleanupErrors()
        {
            using var world = new World("failure");
            var actor = world.CreateActor();
            var probe = world.AddCapability<Probe>(actor);
            probe.TickAction = () => throw new IOException("tick failure");
            probe.ReleaseAction = () => throw new FormatException("cleanup failure");
            var error = Assert.Throws<AggregateException>(() => world.Tick(1, TickGroup.Gameplay));
            Assert.That(error.Flatten().InnerExceptions.Select(e => e.Message), Is.EquivalentTo(new[] { "tick failure", "cleanup failure" }));
            Assert.That(probe.Releases, Is.EqualTo(1));
            Assert.That(probe.IsReleased, Is.True);
            Assert.That(world.Actors.GetCapabilities(actor), Is.Empty);
        }

        /// <summary>
        ///   <para>Tick 中销毁行动者会停止后续能力，释放时组件仍有效。</para>
        /// </summary>
        [Test]
        public void DestroyDuringTick_DefersStorageReleaseAndStopsCallbacks()
        {
            using var world = new World("destroy");
            var actor = world.CreateActor();
            world.AddComponent<Value>(actor).Number = 42;
            var first = world.AddCapability<Probe>(actor);
            var second = world.AddCapability<Probe>(actor);
            int observed = 0;
            first.TickAction = () => { world.DestroyActor(actor); world.DestroyActor(actor); };
            second.ReleaseAction = () => observed = world.GetComponent<Value>(actor).Number;
            world.Tick(1, TickGroup.Gameplay);
            Assert.That(observed, Is.EqualTo(42));
            Assert.That(second.Ticks, Is.Zero);
            Assert.That(world.IsActorAlive(actor), Is.False);
            Assert.That(first.Releases, Is.EqualTo(1));
            Assert.That(second.Releases, Is.EqualTo(1));
        }

        /// <summary>
        ///   <para>生命周期回调中的释放重入不会破坏世界。</para>
        /// </summary>
        [Test]
        public void DisposeDuringTick_IsRejectedWithoutPoisoningWorld()
        {
            using var world = new World("dispose");
            var probe = world.AddCapability<Probe>(world.CreateActor());
            probe.TickAction = () => Assert.Throws<InvalidOperationException>(world.Dispose);
            world.Tick(1, TickGroup.Gameplay);
            Assert.That(world.IsDisposed, Is.False);
            Assert.That(world.Actors.AliveActorCount, Is.EqualTo(1));
            world.Clear();
            Assert.That(probe.Releases, Is.EqualTo(1));
        }

        /// <summary>
        ///   <para>能力按稳定顺序更新，派发队列在下一帧提交状态变化。</para>
        /// </summary>
        [Test]
        public void Tick_UpdatesIndependentDataAndQueuesStructuralChanges()
        {
            using var world = new World("sequential-tick");
            var actors = world.CreateActors(1024);
            foreach (var actor in actors)
            {
                world.AddComponent<Value>(actor);
                var probe = world.AddCapability<Probe>(actor);
                probe.TickAction = () =>
                {
                    Assert.That(world.Enqueue(w => w.GetComponent<Value>(actor).Number++), Is.True);
                };
            }
            for (int i = 0; i < 16; i++) world.Tick(1, TickGroup.Gameplay);
            foreach (var actor in actors) Assert.That(world.GetComponent<Value>(actor).Number, Is.EqualTo(15));
            Assert.That(world.PendingActionCount, Is.EqualTo(1024));
        }

        /// <summary>
        ///   <para>能力数量和存储锁不改变生命周期顺序及执行线程。</para>
        /// </summary>
        /// <param name="count">能力数量。</param>
        /// <param name="storageThreadSafety">是否启用存储访问锁。</param>
        [Test, Combinatorial]
        public void Scheduler_LifecycleRunsInRegistrationOrderOnWorldThread(
            [Values(0, 1, 15, 16, 17, 1024)] int count,
            [Values(false, true)] bool storageThreadSafety)
        {
            using var world = new World("lifecycle", new ActorManagerOptions(64, storageThreadSafety, 2));
            var actors = world.CreateActors(count);
            var calls = new List<(Actor actor, string phase, int thread)>();
            var probes = actors.Select(actor => world.AddCapability<LifecycleProbe>(actor)).ToArray();
            foreach (var probe in probes) probe.Calls = calls;
            int thread = Environment.CurrentManagedThreadId;

            world.Tick(1, TickGroup.Gameplay);
            Assert.That(calls, Is.EqualTo(actors.SelectMany(actor => new[]
                { (actor, "check activate", thread), (actor, "activate", thread), (actor, "tick", thread) })));
            calls.Clear();
            world.Tick(1, TickGroup.Gameplay);
            Assert.That(calls, Is.EqualTo(actors.SelectMany(actor => new[]
                { (actor, "check deactivate", thread), (actor, "tick", thread) })));
            foreach (var probe in probes) probe.Enabled = false;
            calls.Clear();
            world.Tick(1, TickGroup.Gameplay);
            Assert.That(calls, Is.EqualTo(actors.SelectMany(actor => new[]
                { (actor, "check deactivate", thread), (actor, "deactivate", thread) })));
            calls.Clear();
            world.Clear();
            Assert.That(calls, Is.EquivalentTo(actors.Select(actor => (actor, "release", thread))));
            foreach (var probe in probes) Assert.That(probe.Releases, Is.EqualTo(1));
        }

        /// <summary>
        ///   <para>激活时移除自身或销毁行动者会跳过 Tick，并且仅释放一次。</para>
        /// </summary>
        /// <param name="destroyActor">是否销毁行动者。</param>
        [TestCase(false), TestCase(true)]
        public void Scheduler_RemovalDuringActivationSkipsTick(bool destroyActor)
        {
            using var world = new World("activation-removal");
            var actor = world.CreateActor();
            var probe = world.AddCapability<LifecycleProbe>(actor);
            probe.Calls = new List<(Actor actor, string phase, int thread)>();
            probe.ActivateAction = () =>
            {
                if (destroyActor) world.DestroyActor(actor);
                else world.RemoveCapability<LifecycleProbe>(actor);
            };
            world.Tick(1, TickGroup.Gameplay);
            world.Clear();
            Assert.That(probe.Ticks, Is.Zero);
            Assert.That(probe.Releases, Is.EqualTo(1));
            Assert.That(probe.Calls.Select(call => call.phase), Is.EqualTo(new[]
                { "check activate", "activate", "deactivate", "release" }));
        }

        /// <summary>
        ///   <para>高压数学计算保持数值有限、结果独立且步数完整。</para>
        /// </summary>
        [Test]
        public void Tick_ComplexMathRemainsFiniteAndDeterministic()
        {
            const int actorCount = 8192;
            const int tickCount = 24;
            using var world = new World("complex-math", new ActorManagerOptions(actorCount, true, 2));
            var actors = world.CreateActors(actorCount);
            var expected = new MathState[actorCount];
            for (int i = 0; i < actorCount; i++)
            {
                expected[i] = new MathState
                {
                    Value = (i - actorCount / 2) * 0.001f,
                    Phase = i * 0.0031f,
                };
                world.AddComponent<MathState>(actors[i]) = expected[i];
                world.AddCapability<ComplexMathCapability>(actors[i]);
            }

            for (int tick = 0; tick < tickCount; tick++)
            {
                world.Tick(1f / 60f, TickGroup.Gameplay);
                for (int i = 0; i < actorCount; i++)
                    ComplexMathCapability.Compute(ref expected[i]);
            }

            for (int i = 0; i < actorCount; i++)
            {
                var actual = world.GetComponent<MathState>(actors[i]);
                Assert.That(actual.Steps, Is.EqualTo(tickCount));
                Assert.That(Game.NumberUtility.IsFinite(actual.Value), Is.True);
                Assert.That(Game.NumberUtility.IsFinite(actual.Phase), Is.True);
                Assert.That(actual.Value, Is.EqualTo(expected[i].Value).Within(1e-5f));
                Assert.That(actual.Phase, Is.EqualTo(expected[i].Phase).Within(1e-5f));
            }
        }

        /// <summary>
        ///   <para>多个外部线程提交任务，世界线程按各提交者的顺序执行。</para>
        /// </summary>
        [Test]
        public void ExternalThread_UsesDispatchForStructuralChanges()
        {
            using var world = new World("dispatch");
            const int producers = 8, countPerProducer = 512;
            int thread = Environment.CurrentManagedThreadId;
            var executed = new int[producers];
            var tasks = Enumerable.Range(0, producers).Select(producer => Task.Run(() =>
            {
                Assert.Throws<InvalidOperationException>(() => world.CreateActor());
                Assert.Throws<InvalidOperationException>(() => world.Capabilities.Update(1, TickGroup.Gameplay));
                for (int i = 0; i < countPerProducer; i++)
                {
                    int sequence = i;
                    Assert.That(world.Enqueue(w =>
                    {
                        Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(thread));
                        Assert.That(executed[producer]++, Is.EqualTo(sequence));
                        w.CreateActor();
                    }), Is.True);
                }
            })).ToArray();
            Task.WhenAll(tasks).GetAwaiter().GetResult();
            Assert.That(world.Actors.AliveActorCount, Is.Zero);
            Assert.That(world.PendingActionCount, Is.EqualTo(producers * countPerProducer));
            world.Tick(1, TickGroup.Gameplay);
            Assert.That(world.Actors.AliveActorCount, Is.EqualTo(producers * countPerProducer));
            Assert.That(executed, Is.All.EqualTo(countPerProducer));
            Assert.That(world.PendingActionCount, Is.Zero);
            world.Dispose();
            Assert.That(world.Enqueue(_ => Assert.Fail("A disposed world must not run work.")), Is.False);
        }

        /// <summary>
        ///   <para>稀疏查询和交换移除不会漏项或返回旧句柄。</para>
        /// </summary>
        [Test]
        public void SparseQuery_TracksSwappedEntries()
        {
            using var world = new World("sparse", new ActorManagerOptions(100000, false, 2));
            var expected = new HashSet<Actor>();
            for (int i = 0; i < 100000; i++)
            {
                var actor = world.CreateActor();
                if (i % 997 != 0) continue;
                world.AddComponent<Value>(actor);
                expected.Add(actor);
            }
            foreach (var actor in expected.ToArray().Where((_, i) => i % 2 == 0))
            {
                world.DestroyActor(actor);
                expected.Remove(actor);
            }
            var result = new List<Actor>();
            world.QueryActorsWithComponentNonAlloc<Value>(result);
            Assert.That(result, Is.EquivalentTo(expected));
        }

        /// <summary>
        ///   <para>复制测试传输；连接关闭前由复制层独占释放。</para>
        /// </summary>
        private sealed class Transport : IReplicationTransport
        {
            public readonly Queue<byte[]> Incoming = new();
            public readonly List<byte[]> Sent = new();
            public bool BlockSend;
            public int Releases;

            public bool TrySend(ReadOnlySpan<byte> packet)
            {
                if (BlockSend) return false;
                Sent.Add(packet.ToArray());
                return true;
            }

            public int Receive(Span<byte> buffer)
            {
                if (Incoming.Count == 0) return 0;
                var packet = Incoming.Peek();
                if (packet.Length > buffer.Length) throw new InvalidDataException("test buffer too small");
                Incoming.Dequeue();
                packet.AsSpan().CopyTo(buffer);
                return packet.Length;
            }

            public void Dispose() => Releases++;
        }


        private static ReplicationSchema Schema()
            => new ReplicationSchema(1).Register<Value>(1);

        private static ReplicationOptions Options(ReplicationRole role)
            => new ReplicationOptions(Schema(), role, sendInterval: 0, maxActors: 1024);

        private static void Transfer(World authority, World replica, Transport output, Transport input)
        {
            authority.Tick(1, TickGroup.Late);
            foreach (var packet in output.Sent) input.Incoming.Enqueue(packet);
            output.Sent.Clear();
            replica.Tick(1, TickGroup.Early);
        }

        /// <summary>
        ///   <para>默认编码器按组件数值字段自动注册并完成首批复制。</para>
        /// </summary>
        [Test]
        public void Replication_DefaultCodecSynchronizesValueWithoutManualCodec()
        {
            using var authority = new World("authority", replicationOptions: Options(ReplicationRole.Authority));
            using var replica = new World("replica", replicationOptions: Options(ReplicationRole.Replica));
            var output = new Transport();
            var input = new Transport();
            authority.Replication.Attach(output);
            var peer = replica.Replication.Attach(input);
            var actor = authority.CreateActor();
            authority.AddComponent<Replicated>(actor);
            authority.AddComponent<Value>(actor).Number = 37;

            Transfer(authority, replica, output, input);

            Assert.That(peer.RemoteActorCount, Is.EqualTo(1));
            Assert.That(peer.TryGetActor(actor.id, out var remote), Is.True);
            Assert.That(replica.GetComponent<Value>(remote).Number, Is.EqualTo(37));
        }

        /// <summary>
        ///   <para>直接通过组件引用修改也会被状态差异捕获。</para>
        /// </summary>
        [Test]
        public void Replication_RefMutationIsCapturedWithoutDirtyCall()
        {
            using var authority = new World("authority", replicationOptions: Options(ReplicationRole.Authority));
            using var replica = new World("replica", replicationOptions: Options(ReplicationRole.Replica));
            var output = new Transport();
            var input = new Transport();
            authority.Replication.Attach(output);
            var peer = replica.Replication.Attach(input);
            var actor = authority.CreateActor();
            authority.AddComponent<Replicated>(actor);
            authority.AddComponent<Value>(actor).Number = 1;
            Transfer(authority, replica, output, input);

            ref var value = ref authority.GetComponent<Value>(actor);
            value.Number = 99;
            Transfer(authority, replica, output, input);

            Assert.That(peer.TryGetActor(actor.id, out var remote), Is.True);
            Assert.That(replica.GetComponent<Value>(remote).Number, Is.EqualTo(99));
        }

        /// <summary>
        ///   <para>实体和组件增删在同一批次中原子提交。</para>
        /// </summary>
        [Test]
        public void Replication_EntityAndComponentChangesCommitAsOneBatch()
        {
            using var authority = new World("authority", replicationOptions: Options(ReplicationRole.Authority));
            using var replica = new World("replica", replicationOptions: Options(ReplicationRole.Replica));
            var output = new Transport();
            var input = new Transport();
            authority.Replication.Attach(output);
            var peer = replica.Replication.Attach(input);
            var first = authority.CreateActor();
            authority.AddComponent<Replicated>(first);
            authority.AddComponent<Value>(first).Number = 10;
            Transfer(authority, replica, output, input);

            var second = authority.CreateActor();
            authority.AddComponent<Replicated>(second);
            authority.AddComponent<Value>(second).Number = 20;
            authority.RemoveComponent<Value>(first);
            Transfer(authority, replica, output, input);

            Assert.That(peer.TryGetActor(first.id, out var remoteFirst), Is.True);
            Assert.That(replica.HasComponent<Value>(remoteFirst), Is.False);
            Assert.That(peer.TryGetActor(second.id, out var remoteSecond), Is.True);
            Assert.That(replica.GetComponent<Value>(remoteSecond).Number, Is.EqualTo(20));

            authority.DestroyActor(first);
            Transfer(authority, replica, output, input);
            Assert.That(peer.TryGetActor(first.id, out _), Is.False);
        }

        /// <summary>
        ///   <para>传输背压时保持状态和基线，解除后只提交一次增量。</para>
        /// </summary>
        [Test]
        public void Replication_BackpressureDoesNotAdvanceBaseline()
        {
            using var authority = new World("authority", replicationOptions: Options(ReplicationRole.Authority));
            var output = new Transport { BlockSend = true };
            authority.Replication.Attach(output);
            var actor = authority.CreateActor();
            authority.AddComponent<Replicated>(actor);
            authority.AddComponent<Value>(actor).Number = 4;
            authority.Tick(1, TickGroup.Late);
            Assert.That(output.Sent, Is.Empty);

            output.BlockSend = false;
            authority.Tick(1, TickGroup.Late);
            Assert.That(output.Sent, Has.Count.EqualTo(1));
            authority.Tick(1, TickGroup.Late);
            Assert.That(output.Sent, Has.Count.EqualTo(1));
        }

        /// <summary>
        ///   <para>传输接管唯一；已释放对象不能被重新接管。</para>
        /// </summary>
        [Test]
        public void Replication_TransportOwnershipIsExclusiveAndReleased()
        {
            using var first = new World("first", replicationOptions: Options(ReplicationRole.Authority));
            using var second = new World("second", replicationOptions: Options(ReplicationRole.Authority));
            var transport = new Transport();
            first.Replication.Attach(transport);
            Assert.Throws<InvalidOperationException>(() => second.Replication.Attach(transport));
            first.Clear();
            Assert.That(transport.Releases, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => second.Replication.Attach(transport));
            Assert.That(transport.Releases, Is.EqualTo(1));
        }

    }
}
