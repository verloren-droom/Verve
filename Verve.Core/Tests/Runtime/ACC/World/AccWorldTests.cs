using System.Collections.Generic;

namespace Verve.Tests.ACC
{
    using Verve;
    using System;
    using NUnit.Framework;

    /// <summary>
    ///   <para>ACC 世界集成测试；覆盖资源所有权、Tick、表单和高压场景。</para>
    /// </summary>
    [Category("ACC")]
    internal class AccWorldTests
    {

        /// <summary>
        ///   <para>测试计数组件。</para>
        /// </summary>
        private struct CounterComponent : IComponent
        {
            /// <summary>
            ///   <para>计数值。</para>
            /// </summary>
            public int Value;
        }

        /// <summary>
        ///   <para>无行为测试能力。</para>
        /// </summary>
        private sealed class EmptyCapability : Capability
        {
            /// <summary>
            ///   <para>创建无行为能力。</para>
            /// </summary>
            public EmptyCapability() { }
        }

        /// <summary>
        ///   <para>记录 Tick 次数和时间的测试能力。</para>
        /// </summary>
        private sealed class CountingCapability : Capability
        {
            /// <summary>
            ///   <para>已执行 Tick 次数。</para>
            /// </summary>
            public int TickCount { get; private set; }

            /// <summary>
            ///   <para>最近一次 Tick 的时间间隔。</para>
            /// </summary>
            public float LastDelta { get; private set; }

            /// <inheritdoc />
            protected internal override void TickActive(in float deltaTime)
            {
                TickCount++;
                LastDelta = deltaTime;
            }
        }

        /// <summary>
        ///   <para>用于验证旧引用隔离的测试能力。</para>
        /// </summary>
        private sealed class StatefulCapability : Capability
        {
            /// <summary>
            ///   <para>可重置的测试值。</para>
            /// </summary>
            public int Value { get; set; }

            /// <inheritdoc />
            protected override void OnSetup() => Value = 0;
        }

        /// <summary>
        ///   <para>在初始化阶段抛出异常的测试能力。</para>
        /// </summary>
        private sealed class SetupFailureCapability : Capability
        {
            /// <inheritdoc />
            protected override void OnSetup() => throw new InvalidOperationException("Expected setup failure.");
        }

        /// <summary>
        ///   <para>验证能力移除时资源和标签阻塞均会释放的测试能力。</para>
        /// </summary>
        private sealed class OwnedResourceCapability : Capability
        {
            /// <summary>
            ///   <para>资源释放回调次数。</para>
            /// </summary>
            public int DisposeCount { get; private set; }

            /// <summary>
            ///   <para>登记标签阻塞。</para>
            /// </summary>
            /// <param name="tagId">要阻塞的标签。</param>
            public void Block(TagId tagId) => BlockCapabilitiesWithTag(tagId);

            /// <inheritdoc />
            protected override void Dispose(bool disposing)
            {
                if (disposing) DisposeCount++;
            }
        }

        /// <summary>
        ///   <para>在 Tick 中反复排队增删能力的测试能力。</para>
        /// </summary>
        private sealed class DeferredChurnCapability : Capability
        {
            /// <summary>
            ///   <para>待执行的增删次数。</para>
            /// </summary>
            private int m_Count;
            /// <summary>
            ///   <para>是否已经执行过压力操作。</para>
            /// </summary>
            private bool m_HasRun;

            /// <summary>
            ///   <para>设置增删次数。</para>
            /// </summary>
            /// <param name="count">增删次数。</param>
            public void Configure(int count) => m_Count = count;

            /// <inheritdoc />
            protected internal override void TickActive(in float deltaTime)
            {
                if (m_HasRun) return;
                m_HasRun = true;
                for (var i = 0; i < m_Count; i++)
                {
                    var added = OwnerWorld.AddCapability<EmptyCapability>(OwnerActor);
                    OwnerWorld.Capabilities.RemoveCapability(OwnerActor, added);
                    Assert.That(added, Is.Not.Null);
                }
            }
        }

        /// <summary>
        ///   <para>在停用阶段抛出异常的测试能力。</para>
        /// </summary>
        private sealed class ThrowingDeactivateCapability : Capability
        {
            /// <inheritdoc />
            protected internal override void OnDeactivated() => throw new InvalidOperationException("Expected deactivation failure.");
        }

        /// <summary>
        ///   <para>在释放时重入表单移除的测试能力。</para>
        /// </summary>
        private sealed class ReentrantSheetCleanupCapability : Capability
        {
            /// <summary>
            ///   <para>所属表单实例。</para>
            /// </summary>
            internal SheetInstance Sheet;
            /// <summary>
            ///   <para>释放次数。</para>
            /// </summary>
            internal int Releases;

            /// <inheritdoc />
            protected override void Dispose(bool disposing)
            {
                if (!disposing) return;
                Releases++;
                Sheet.RemoveFromActor();
            }
        }

        /// <summary>
        ///   <para>驱动多个能力延迟移除的测试能力。</para>
        /// </summary>
        private sealed class DeferredRemovalDriverCapability : Capability
        {
            /// <summary>
            ///   <para>第一个待移除能力。</para>
            /// </summary>
            private Capability m_First;
            /// <summary>
            ///   <para>第二个待移除能力。</para>
            /// </summary>
            private Capability m_Second;

            /// <summary>
            ///   <para>第一个移除请求是否已入队。</para>
            /// </summary>
            public bool FirstRemovalQueued { get; private set; }
            /// <summary>
            ///   <para>第二个移除请求是否已入队。</para>
            /// </summary>
            public bool SecondRemovalQueued { get; private set; }

            /// <summary>
            ///   <para>设置两个待移除能力。</para>
            /// </summary>
            /// <param name="first">第一个能力。</param>
            /// <param name="second">第二个能力。</param>
            public void Configure(Capability first, Capability second)
            {
                m_First = first;
                m_Second = second;
            }

            /// <inheritdoc />
            protected internal override void TickActive(in float deltaTime)
            {
                if (FirstRemovalQueued) return;
                FirstRemovalQueued = OwnerWorld.Capabilities.RemoveCapability(OwnerActor, m_First);
                SecondRemovalQueued = OwnerWorld.Capabilities.RemoveCapability(OwnerActor, m_Second);
            }
        }
        /// <summary>
        ///   <para>测试使用的模块容器句柄。</para>
        /// </summary>
        private GameModulesHandle m_ModulesHandle;
        /// <summary>
        ///   <para>测试使用的世界管理器。</para>
        /// </summary>
        private IWorldManager m_Worlds;

        /// <summary>
        ///   <para>创建并安装世界管理模块。</para>
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            m_ModulesHandle = Game.CreateModules();
            m_ModulesHandle.Modules.Install<WorldManagerModule>();
            m_Worlds = m_ModulesHandle.Modules.GetModule<IWorldManager>();
        }

        /// <summary>
        ///   <para>释放测试模块容器及其拥有的世界。</para>
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            m_ModulesHandle?.Dispose();
            m_ModulesHandle = null;
            m_Worlds = null;
        }

        /// <summary>
        ///   <para>验证行动者、组件和能力的基本生命周期。</para>
        /// </summary>
        [Test]
        public void World_ManagesActorComponentAndCapabilityLifetimes()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Lifetime");
            var actor = world.CreateActor();

            ref var component = ref world.AddComponent<CounterComponent>(actor);
            component.Value = 42;

            Assert.That(world.IsActorAlive(actor), Is.True);
            Assert.That(world.TryGetComponent(actor, out CounterComponent value), Is.True);
            Assert.That(value.Value, Is.EqualTo(42));

            var capability = world.AddCapability<EmptyCapability>(actor);
            Assert.That(capability, Is.Not.Null);
            Assert.That(world.RemoveCapability<EmptyCapability>(actor), Is.True);
            Assert.That(world.RemoveComponent<CounterComponent>(actor), Is.True);

            world.DestroyActor(actor);
            Assert.That(world.IsActorAlive(actor), Is.False);
            Assert.That(world.TryGetComponent(actor, out CounterComponent _), Is.False);
        }

        /// <summary>
        ///   <para>验证销毁行动者会释放数据并使旧标识失效。</para>
        /// </summary>
        [Test]
        public void Actor_DestroyReleasesDataAndInvalidatesIdentity()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Destroy");
            var actor = world.CreateActor();
            world.AddComponent<CounterComponent>(actor).Value = 1;
            world.AddCapability<EmptyCapability>(actor);

            world.DestroyActor(actor);

            Assert.That(world.IsActorAlive(actor), Is.False);
            Assert.That(world.TryGetComponent(actor, out CounterComponent _), Is.False);
            Assert.That(world.RemoveCapability<EmptyCapability>(actor), Is.False);

            var replacement = world.CreateActor();
            Assert.That(replacement.Index, Is.EqualTo(actor.Index));
            Assert.That(replacement.Version, Is.Not.EqualTo(actor.Version));
        }

        /// <summary>
        ///   <para>验证能力只在指定 Tick 组更新并应用时间缩放。</para>
        /// </summary>
        [Test]
        public void Capability_TicksOnlyInRequestedGroupAndUsesTimeScale()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Tick");
            var actor = world.CreateActor();
            var capability = world.AddCapability<CountingCapability>(actor);
            world.TimeScale = 0.5f;

            m_ModulesHandle.Modules.Tick(2f, TickGroup.Early);
            Assert.That(capability.TickCount, Is.Zero);
            m_ModulesHandle.Modules.Tick(2f, TickGroup.Gameplay);
            Assert.That(capability.TickCount, Is.EqualTo(1));
            Assert.That(capability.LastDelta, Is.EqualTo(1f));
        }

        /// <summary>
        ///   <para>验证世界动作按入队顺序在下一次 Tick 执行。</para>
        /// </summary>
        [Test]
        public void World_DispatchExecutesInOrderOnNextTick()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Dispatch");
            var calls = new List<int>();
            Assert.That(world.Enqueue(_ => calls.Add(1)), Is.True);
            Assert.That(world.Enqueue(_ => calls.Add(2)), Is.True);
            Assert.That(world.PendingActionCount, Is.EqualTo(2));

            m_ModulesHandle.Modules.Tick(0f, TickGroup.Gameplay);

            Assert.That(calls, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(world.PendingActionCount, Is.Zero);
        }

        /// <summary>
        ///   <para>验证表单能够移除本次应用所拥有的资源。</para>
        /// </summary>
        [Test]
        public void Sheet_AppliesAndRemovesOwnedMembers()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Sheet");
            var actor = world.CreateActor();
            var sheet = new CapabilitySheet()
                .AddComponent<CounterComponent>()
                .AddCapability<EmptyCapability>();

            var instance = world.ApplySheet(actor, sheet);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.True);
            Assert.That(world.GetActorSheets(actor), Has.Count.EqualTo(1));
            Assert.That(world.RemoveSheet(actor, instance), Is.True);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.False);
            Assert.That(world.GetActorSheets(actor), Is.Empty);
            Assert.That(world.RemoveSheet(actor, instance), Is.False);
        }

        /// <summary>
        ///   <para>验证组件模式不会创建能力。</para>
        /// </summary>
        [Test]
        public void Sheet_ComponentsOnlySkipsCapabilities()
        {
            var world = m_Worlds.Create("Tests.AccWorld.SheetComponents");
            var actor = world.CreateActor();
            var sheet = new CapabilitySheet()
                .AddComponent<CounterComponent>()
                .AddCapability<EmptyCapability>();

            var instance = world.ApplySheet(actor, sheet, CapabilitySheetApplyMode.ComponentsOnly);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.True);
            Assert.That(world.RemoveCapability<EmptyCapability>(actor), Is.False);
            instance.RemoveFromActor();
        }

        /// <summary>
        ///   <para>验证重叠表单不会误释放外部拥有的资源。</para>
        /// </summary>
        [Test]
        public void Sheet_OverlappingApplicationsRemoveOnlyResourcesTheyCreated()
        {
            var world = m_Worlds.Create("Tests.AccWorld.SheetOverlap");
            var actor = world.CreateActor();
            var componentSheet = new CapabilitySheet().AddComponent<CounterComponent>();
            var first = world.ApplySheet(actor, componentSheet);
            var second = world.ApplySheet(actor, componentSheet);

            Assert.That(world.RemoveSheet(actor, first), Is.True);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.True);
            Assert.That(world.RemoveSheet(actor, second), Is.True);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.False);

            var capabilitySheet = new CapabilitySheet().AddCapability<StatefulCapability>();
            var external = world.AddCapability<StatefulCapability>(actor);
            var owned = world.ApplySheet(actor, capabilitySheet);
            owned.RemoveFromActor();

            Assert.That(external.IsReleased, Is.False);
            var remaining = world.Actors.GetCapabilities(actor);
            Assert.That(remaining.Count, Is.EqualTo(1));
            Assert.That(remaining.Exists(item => item.capability == external), Is.True);
            Assert.That(world.RemoveCapability<StatefulCapability>(actor), Is.True);

            world.AddComponent<CounterComponent>(actor).Value = 7;
            var externalComponentSheet = world.ApplySheet(actor, componentSheet);
            externalComponentSheet.RemoveFromActor();
            Assert.That(world.GetComponent<CounterComponent>(actor).Value, Is.EqualTo(7));
            Assert.That(world.RemoveComponent<CounterComponent>(actor), Is.True);
        }

        /// <summary>
        ///   <para>拒绝负数、NaN 和无穷 Tick 间隔，避免污染世界时间。</para>
        /// </summary>
        [Test]
        public void World_RejectsInvalidTickDelta()
        {
            var world = m_Worlds.Create("Tests.AccWorld.InvalidDelta");
            Assert.Throws<ArgumentOutOfRangeException>(() => world.Tick(-1f, TickGroup.Gameplay));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.Tick(float.NaN, TickGroup.Gameplay));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.Tick(float.PositiveInfinity, TickGroup.Gameplay));
        }

        /// <summary>
        ///   <para>验证表单应用失败时会回滚已创建资源。</para>
        /// </summary>
        [Test]
        public void Sheet_FailedApplyRollsBackPreviouslyAddedResources()
        {
            var world = m_Worlds.Create("Tests.AccWorld.SheetRollback");
            var actor = world.CreateActor();
            var sheet = new CapabilitySheet()
                .AddComponent<CounterComponent>()
                .AddCapability<SetupFailureCapability>();

            Assert.Throws<InvalidOperationException>(() => world.ApplySheet(actor, sheet));
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.False);
            Assert.That(world.GetActorSheets(actor), Is.Empty);
        }

        /// <summary>
        ///   <para>验证跨线程释放被拒绝后仍可在世界线程正确释放。</para>
        /// </summary>
        [Test]
        public void Sheet_RejectedDisposalDoesNotMarkResourcesReleased()
        {
            var world = m_Worlds.Create("Tests.AccWorld.SheetThread");
            var actor = world.CreateActor();
            var instance = world.ApplySheet(actor, new CapabilitySheet().AddComponent<CounterComponent>());
            Exception failure = null;
            var thread = new System.Threading.Thread(() =>
            {
                try { instance.Dispose(); }
                catch (Exception error) { failure = error; }
            });
            thread.Start();
            Assert.That(thread.Join(TimeSpan.FromSeconds(5)), Is.True);

            Assert.That(failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(instance.IsDisposed, Is.False);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.True);
            instance.Dispose();
            Assert.That(instance.IsDisposed, Is.True);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.False);
            Assert.That(world.GetActorSheets(actor), Is.Empty);
        }

        /// <summary>
        ///   <para>验证世界自动清理与显式释放使用相同完成状态。</para>
        /// </summary>
        /// <param name="destroyActor">是否通过行动者销毁触发清理。</param>
        [TestCase(false)]
        [TestCase(true)]
        public void Sheet_AutomaticCleanupMarksInstanceReleased(bool destroyActor)
        {
            var world = m_Worlds.Create("Tests.AccWorld.SheetAutomatic");
            var actor = world.CreateActor();
            var instance = world.ApplySheet(actor, new CapabilitySheet().AddComponent<CounterComponent>());
            if (destroyActor) world.DestroyActor(actor);
            else world.Clear();

            Assert.That(instance.IsDisposed, Is.True);
            Assert.That(world.GetActorSheets(actor), Is.Empty);
            world.Dispose();
            Assert.DoesNotThrow(instance.Dispose);
            Assert.DoesNotThrow(instance.RemoveFromActor);
        }

        /// <summary>
        ///   <para>验证子表单回调失败仍注销已释放的嵌套资源。</para>
        /// </summary>
        [Test]
        public void Sheet_NestedCleanupFailureUnregistersReleasedResources()
        {
            var world = m_Worlds.Create("Tests.AccWorld.SheetNestedFailure");
            var actor = world.CreateActor();
            var sheet = new CapabilitySheet().AddComponent<CounterComponent>()
                .AddSubSheet(new CapabilitySheet().AddCapability<ThrowingDeactivateCapability>())
                .AddCapability<EmptyCapability>();
            var instance = world.ApplySheet(actor, sheet);
            world.Capabilities.Update(0.016f, TickGroup.Gameplay);

            Assert.Throws<AggregateException>(instance.Dispose);
            Assert.That(instance.IsDisposed, Is.True);
            Assert.That(world.GetActorSheets(actor), Is.Empty);
            Assert.That(world.Actors.GetCapabilities(actor), Is.Empty);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.False);
            Assert.DoesNotThrow(instance.Dispose);
        }

        /// <summary>
        ///   <para>验证重入释放被明确拒绝，能力只释放一次。</para>
        /// </summary>
        [Test]
        public void Sheet_ReentrantRemovalDoesNotRepeatCleanup()
        {
            var world = m_Worlds.Create("Tests.AccWorld.SheetReentry");
            var actor = world.CreateActor();
            var instance = world.ApplySheet(actor, new CapabilitySheet()
                .AddComponent<CounterComponent>().AddCapability<ReentrantSheetCleanupCapability>());
            var capability = (ReentrantSheetCleanupCapability)world.Actors.GetCapabilities(actor)[0].capability;
            capability.Sheet = instance;

            var failure = Assert.Throws<AggregateException>(instance.Dispose).Flatten();
            Assert.That(failure.InnerExceptions, Has.Count.EqualTo(1));
            Assert.That(failure.InnerExceptions[0], Is.TypeOf<InvalidOperationException>());
            Assert.That(capability.Releases, Is.EqualTo(1));
            Assert.That(instance.IsDisposed, Is.True);
            Assert.That(world.GetActorSheets(actor), Is.Empty);
            Assert.That(world.HasComponent<CounterComponent>(actor), Is.False);
        }

        /// <summary>
        ///   <para>验证标签注册表在重复查询和清理后保持双向映射正确。</para>
        /// </summary>
        [Test]
        public void TagRegistry_AssignsStableBidirectionalIds()
        {
            var name = "Tests.Acc.Tag." + Guid.NewGuid().ToString("N");
            TagRegistry.Reset();
            try
            {
                var first = TagRegistry.GetTagId(name);
                Assert.That(first, Is.Not.EqualTo(TagId.none));
                Assert.That(TagRegistry.GetTagId(name), Is.EqualTo(first));
                Assert.That(TagRegistry.GetTagName(first), Is.EqualTo(name));
            }
            finally
            {
                TagRegistry.Reset();
            }
        }

        /// <summary>
        ///   <para>验证更新期间大量延迟增删不会泄露能力实例。</para>
        /// </summary>
        [Test]
        public void Capability_DeferredAddRemoveChurnDoesNotLeakInstances()
        {
            var world = m_Worlds.Create("Tests.AccWorld.DeferredStress");
            var actor = world.CreateActor();
            var driver = world.AddCapability<DeferredChurnCapability>(actor);
            driver.Configure(128);

            m_ModulesHandle.Modules.Tick(0.016f, TickGroup.Gameplay);

            Assert.That(world.TryGetComponent<CounterComponent>(actor, out _), Is.False);
            Assert.That(world.RemoveCapability<DeferredChurnCapability>(actor), Is.True);
            Assert.That(world.Actors.GetCapabilities(actor), Is.Empty);
        }

        /// <summary>
        ///   <para>验证一个能力停用失败时后续延迟移除仍会继续。</para>
        /// </summary>
        [Test]
        public void Capability_DeferredRemovalContinuesAfterLifecycleFailure()
        {
            var world = m_Worlds.Create("Tests.AccWorld.DeferredRemovalFailure");
            var actor = world.CreateActor();
            var throwing = world.AddCapability<ThrowingDeactivateCapability>(actor);
            var retained = world.AddCapability<StatefulCapability>(actor);
            var driver = world.AddCapability<DeferredRemovalDriverCapability>(actor);
            driver.Configure(throwing, retained);

            Assert.Throws<AggregateException>(() => world.Capabilities.Update(0.016f, TickGroup.Gameplay));

            Assert.That(driver.FirstRemovalQueued, Is.True);
            Assert.That(driver.SecondRemovalQueued, Is.True);
            Assert.That(throwing.IsReleased, Is.True);
            Assert.That(world.Actors.GetCapabilities(actor).Exists(item => item.capability == retained), Is.False);
            Assert.That(world.Actors.GetCapabilities(actor).Exists(item => item.capability == driver), Is.True);
        }

        /// <summary>
        ///   <para>验证清理世界会释放能力并使行动者失效。</para>
        /// </summary>
        [Test]
        public void World_ClearReleasesCapabilitiesAndInvalidatesActors()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Clear");
            var actor = world.CreateActor();
            world.AddCapability<StatefulCapability>(actor);
            world.ApplySheet(actor, new CapabilitySheet().AddComponent<CounterComponent>());

            world.Clear();

            Assert.That(world.Actors.AliveActorCount, Is.Zero);
            Assert.That(world.IsActorAlive(actor), Is.False);
            Assert.That(world.GetActorSheets(actor), Is.Empty);
        }

        /// <summary>
        ///   <para>验证大量行动者和能力的创建、Tick、销毁流程。</para>
        /// </summary>
        [Test]
        public void World_HandlesHighVolumeActorCapabilityChurn()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Stress");
            const int count = 2000;
            var actors = world.CreateActors(count);
            for (var i = 0; i < actors.Length; i++)
                world.AddCapability<CountingCapability>(actors[i]);

            m_ModulesHandle.Modules.Tick(0.016f, TickGroup.Gameplay);
            Assert.That(world.Actors.AliveActorCount, Is.EqualTo(count));
            for (var i = 0; i < actors.Length; i++)
                world.DestroyActor(actors[i]);
            Assert.That(world.Actors.AliveActorCount, Is.Zero);
        }

        /// <summary>
        ///   <para>验证已移除能力不会被重新分配，旧引用不能影响新实例。</para>
        /// </summary>
        [Test]
        public void World_RemovedCapabilitiesCannotMutateReplacement()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Pool");
            var firstActor = world.CreateActor();
            var first = world.AddCapability<StatefulCapability>(firstActor);
            first.Value = 7;
            Assert.That(world.RemoveCapability<StatefulCapability>(firstActor), Is.True);

            var secondActor = world.CreateActor();
            var second = world.AddCapability<StatefulCapability>(secondActor);

            Assert.That(second, Is.Not.SameAs(first));
            first.Value = 99;
            Assert.That(first.IsReleased, Is.True);
            Assert.That(first.OwnerWorld, Is.Null);
            Assert.That(second.Value, Is.Zero);
            Assert.That(second.OwnerActor, Is.EqualTo(secondActor));
        }

        /// <summary>
        ///   <para>验证能力移除会清理资源、阻塞标签和所属引用。</para>
        /// </summary>
        [Test]
        public void Capability_RemovalReleasesOwnedResourcesAndTagBlocks()
        {
            var world = m_Worlds.Create("Tests.AccWorld.CapabilityOwnership");
            var actor = world.CreateActor();
            var tag = TagRegistry.GetTagId("Tests.Acc.OwnedTag." + Guid.NewGuid().ToString("N"));
            var capability = world.AddCapability<OwnedResourceCapability>(actor);
            capability.Block(tag);

            Assert.That(world.IsTagBlocked(actor, tag), Is.True);
            Assert.That(world.RemoveCapability<OwnedResourceCapability>(actor), Is.True);
            Assert.That(world.IsTagBlocked(actor, tag), Is.False);
            Assert.That(capability.OwnerWorld, Is.Null);
            Assert.That(capability.OwnerActor, Is.EqualTo(Actor.none));
            Assert.That(capability.DisposeCount, Is.EqualTo(1));

            var reused = world.AddCapability<OwnedResourceCapability>(world.CreateActor());
            Assert.That(reused, Is.Not.SameAs(capability));
            capability.Release();
            Assert.That(capability.DisposeCount, Is.EqualTo(1));
        }

        /// <summary>
        ///   <para>验证行动者索引和版本共同构成身份。</para>
        /// </summary>
        [Test]
        public void Actor_UsesIndexAndVersionForIdentity()
        {
            var first = new Actor(3, 1);
            var same = new Actor(3, 1);
            var nextVersion = new Actor(3, 2);

            Assert.That(first, Is.EqualTo(same));
            Assert.That(first, Is.Not.EqualTo(nextVersion));
            Assert.That(first.Index, Is.EqualTo(3));
            Assert.That(first.Version, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Actor(-1, 1));
        }

        /// <summary>
        ///   <para>验证世界管理器跟踪世界生命周期和活跃世界。</para>
        /// </summary>
        [Test]
        public void WorldManagerModule_TracksWorldLifetimeAndActiveWorld()
        {
            const string firstName = "Tests.AccWorld.First";
            const string secondName = "Tests.AccWorld.Second";

            var first = m_Worlds.Create(firstName);
            var second = m_Worlds.Create(secondName);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(m_Worlds.Count, Is.EqualTo(2));
            Assert.That(m_Worlds.Worlds, Is.EquivalentTo(new[] { first, second }));
            Assert.That(m_Worlds.Active, Is.SameAs(first));

            Assert.That(m_Worlds.SetActive(secondName), Is.True);
            Assert.That(m_Worlds.Active, Is.SameAs(second));

            m_Worlds.Destroy(secondName);
            Assert.That(m_Worlds.Active, Is.SameAs(first));
            Assert.That(m_Worlds.Contains(secondName), Is.False);

            m_Worlds.Destroy(firstName);
            Assert.That(m_Worlds.Count, Is.Zero);
            Assert.That(m_Worlds.Active, Is.Null);
        }

        /// <summary>
        ///   <para>验证模块 Tick 只驱动当前活跃世界。</para>
        /// </summary>
        [Test]
        public void WorldManagerModule_DrivesOnlyTheActiveWorldThroughItsOwnedTickSystem()
        {
            var first = m_Worlds.Create("Tests.AccWorld.First");
            var firstCounter = first.AddCapability<CountingCapability>(first.CreateActor());
            var second = m_Worlds.Create("Tests.AccWorld.Second");
            var secondCounter = second.AddCapability<CountingCapability>(second.CreateActor());

            m_ModulesHandle.Modules.Tick(0.25f, TickGroup.Gameplay);

            Assert.That(firstCounter.TickCount, Is.EqualTo(1));
            Assert.That(secondCounter.TickCount, Is.Zero);

            Assert.That(m_Worlds.SetActive(second.Name), Is.True);
            m_ModulesHandle.Modules.Tick(0.25f, TickGroup.Gameplay);

            Assert.That(firstCounter.TickCount, Is.EqualTo(1));
            Assert.That(secondCounter.TickCount, Is.EqualTo(1));
        }

        /// <summary>
        ///   <para>验证卸载世界管理模块会释放其拥有的世界。</para>
        /// </summary>
        [Test]
        public void WorldManagerModule_DisposesOwnedWorldsWhenUninstalled()
        {
            var world = m_Worlds.Create("Tests.AccWorld.Ownership");

            Assert.That(m_ModulesHandle.Modules.Uninstall<WorldManagerModule>(), Is.True);
            Assert.That(world.IsDisposed, Is.True);
            Assert.That(m_ModulesHandle.Modules.TryGetModule<IWorldManager>(out _), Is.False);
        }
    }
}
