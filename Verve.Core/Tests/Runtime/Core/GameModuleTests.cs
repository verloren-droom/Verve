namespace Verve.Tests.Core
{
    using Verve;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [Category("Core")]
    internal class GameModuleTests
    {
        private static readonly List<string> Lifecycle = new();

        [SetUp]
        public void SetUp()
        {
            Lifecycle.Clear();
        }

        [Test]
        public void ManifestInstall_OrdersDependenciesAndDisposesInReverseOrder()
        {
            var modules = new GameModules();
            try
            {
                var manifest = new GameModuleManifest();
                manifest.Add<DependentModule>();
                manifest.Add<DependencyModule>();

                modules.InstallFromManifest(manifest);

                CollectionAssert.AreEqual(new[] { "dependency.install", "dependent.install" }, Lifecycle);
                Assert.That(modules.GetModule<DependencyModule>(), Is.Not.Null);
                Assert.That(modules.GetModule<DependentModule>(), Is.Not.Null);

                modules.Dispose();

                CollectionAssert.AreEqual(
                    new[] { "dependency.install", "dependent.install", "dependent.uninstall", "dependency.uninstall" },
                    Lifecycle.GetRange(0, 4));
            }
            finally
            {
                modules.Dispose();
            }
        }

        [Test]
        public void FailedInstall_DoesNotLeaveModuleRegistered()
        {
            using var modules = new GameModules();

            Assert.Throws<InvalidOperationException>(() => modules.Install<FailingModule>());

            Assert.That(modules.TryGetModule<FailingModule>(out _), Is.False);
            Assert.That(modules.InstalledModules, Is.Empty);
        }

        [Test]
        public void InstalledModules_ReturnsStableSnapshotsAcrossChanges()
        {
            using var modules = new GameModules();
            var empty = modules.InstalledModules;
            modules.Install<DependencyModule>();
            var first = modules.InstalledModules;
            modules.Install<DependentModule>();
            var both = modules.InstalledModules;
            modules.Uninstall<DependentModule>();

            Assert.That(empty, Is.Empty);
            Assert.That(first, Has.Count.EqualTo(1));
            Assert.That(both, Has.Count.EqualTo(2));
            Assert.That(modules.InstalledModules, Has.Count.EqualTo(1));
        }

        [Test]
        public void OwnedInstance_CannotBeInstalledAgainOrDisposedByAnotherContainer()
        {
            using var owner = new GameModules();
            using var other = new GameModules();
            owner.Install<DependencyModule>();
            var instance = owner.GetModule<DependencyModule>();
            Assert.Throws<InvalidOperationException>(() => owner.Install(() => instance));
            Assert.Throws<InvalidOperationException>(() => other.Install(() => instance));
            Assert.Throws<InvalidOperationException>(() => instance.Dispose());
            Assert.That(instance.IsInstalled, Is.True);
            Assert.That(instance.IsDisposed, Is.False);
            Assert.That(other.InstalledModules, Is.Empty);
        }

        [Test]
        public void DuplicateInstall_PreservesExistingInstanceAndDependents()
        {
            using var modules = new GameModules();
            modules.Install<DependencyModule>();
            modules.Install<DependentModule>();
            var original = modules.GetModule<DependencyModule>();
            var duplicate = new DependencyModule();

            Assert.Throws<InvalidOperationException>(() => modules.Install(() => duplicate));

            Assert.That(modules.GetModule<DependencyModule>(), Is.SameAs(original));
            Assert.That(original.IsInstalled, Is.True);
            Assert.That(original.IsDisposed, Is.False);
            Assert.That(duplicate.IsDisposed, Is.True);
            Assert.That(Lifecycle, Is.EqualTo(new[] { "dependency.install", "dependent.install" }));
            Assert.Throws<InvalidOperationException>(() => modules.Uninstall<DependencyModule>());
        }

        [Test]
        public void ManifestDuplicate_RollsBackNewModulesAndPreservesExistingInstance()
        {
            using var modules = new GameModules();
            modules.Install<DependencyModule>();
            var original = modules.GetModule<DependencyModule>();
            var added = new TickModule();
            var duplicate = new DependencyModule();
            var unprocessedCreated = false;
            var manifest = new GameModuleManifest(GameModuleInstallOrder.Declared);
            manifest.Add(() => added);
            manifest.Add(() => duplicate);
            manifest.Add(() => { unprocessedCreated = true; return new FailingModule(); });

            Assert.Throws<InvalidOperationException>(() => modules.InstallFromManifest(manifest));

            Assert.That(modules.InstalledModules, Has.Count.EqualTo(1));
            Assert.That(modules.GetModule<DependencyModule>(), Is.SameAs(original));
            Assert.That(added.IsDisposed && duplicate.IsDisposed, Is.True);
            Assert.That(unprocessedCreated, Is.False);
            modules.Tick(1f, TickGroup.Gameplay);
            Assert.That(added.TickCount, Is.Zero);
        }

        [Test]
        public async Task ManifestFailureAsync_RollsBackInReverseDependencyOrder()
        {
            var modules = new GameModules();
            var dependency = new DependencyModule();
            var dependent = new DependentModule();
            var failed = new AsyncFailingModule();
            var manifest = new GameModuleManifest();
            manifest.Add(() => dependent);
            manifest.Add(() => dependency);
            manifest.Add(() => failed);
            try
            {
                try { await modules.InstallFromManifestAsync(manifest); Assert.Fail("Expected installation failure."); }
                catch (InvalidOperationException) { }
                Assert.That(modules.InstalledModules, Is.Empty);
                Assert.That(dependency.IsDisposed && dependent.IsDisposed && failed.IsDisposed, Is.True);
                Assert.That(Lifecycle, Is.EqualTo(new[]
                {
                    "dependency.install", "dependent.install", "dependent.uninstall", "dependency.uninstall"
                }));
            }
            finally { await modules.DisposeAsync(); }
        }

        [Test]
        public async Task ManifestCancellation_DisposesOwnedModulesAndDoesNotCreateLaterItems()
        {
            var modules = new GameModules();
            using var cancellation = new CancellationTokenSource();
            var added = new TickModule();
            var cancelled = new CancellingModule { Cancellation = cancellation };
            var unprocessedCreated = false;
            var manifest = new GameModuleManifest(GameModuleInstallOrder.Declared);
            manifest.Add(() => added);
            manifest.Add(() => cancelled);
            manifest.Add(() => { unprocessedCreated = true; return new FailingModule(); });
            try
            {
                try { await modules.InstallFromManifestAsync(manifest, cancellation.Token); Assert.Fail("Expected cancellation."); }
                catch (OperationCanceledException) { }
                Assert.That(modules.InstalledModules, Is.Empty);
                Assert.That(added.IsDisposed && cancelled.IsDisposed, Is.True);
                Assert.That(unprocessedCreated, Is.False);
            }
            finally { await modules.DisposeAsync(); }
        }

        [Test]
        public void UninstallFailure_StillDisposesModuleAndRemovesTicks()
        {
            using var modules = new GameModules();
            var module = new FailedTeardownModule();
            modules.Install(() => module);
            Assert.Throws<InvalidOperationException>(() => modules.Uninstall<FailedTeardownModule>());
            Assert.That(module.IsDisposed, Is.True);
            Assert.That(module.DisposeCount, Is.EqualTo(1));
            Assert.That(modules.InstalledModules, Is.Empty);
            modules.Tick(1, TickGroup.Gameplay);
            Assert.That(module.TickCount, Is.Zero);
            module.Dispose();
            Assert.That(module.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task DisposeAsync_FailedUninstallAndReleaseStillCleanRemainingModules()
        {
            var modules = new GameModules();
            modules.Install<DependencyModule>();
            var dependency = modules.GetModule<DependencyModule>();
            var module = new FailedTeardownModule { FailDispose = true, AsyncUninstall = true };
            modules.Install(() => module);
            try { await modules.DisposeAsync(); Assert.Fail("Expected teardown failures."); }
            catch (AggregateException error)
            {
                Assert.That(error.Flatten().InnerExceptions, Has.Count.EqualTo(2));
            }
            Assert.That(modules.IsDisposed, Is.True);
            Assert.That(module.IsDisposed && dependency.IsDisposed, Is.True);
            Assert.That(module.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void ManifestFactoryFailure_RollsBackAlreadyOwnedModules()
        {
            using var modules = new GameModules();
            var added = new TickModule();
            var manifest = new GameModuleManifest(GameModuleInstallOrder.Declared);
            manifest.Add(() => added);
            manifest.Add<FailingModule>(() => throw new InvalidOperationException("factory failed"));
            Assert.Throws<InvalidOperationException>(() => modules.InstallFromManifest(manifest));
            Assert.That(added.IsDisposed, Is.True);
            Assert.That(modules.InstalledModules, Is.Empty);
        }

        [Test]
        public void TickRegistration_IsOwnedAndRemovedWithModule()
        {
            using var modules = new GameModules();
            var module = new TickModule();
            modules.Install(() => module);

            modules.Tick(0.25f, TickGroup.Gameplay);
            Assert.That(module.TickCount, Is.EqualTo(1));

            Assert.That(modules.Uninstall<TickModule>(), Is.True);
            modules.Tick(0.25f, TickGroup.Gameplay);
            Assert.That(module.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void TickScheduler_UsesDeclaredOrderAndHonorsRemoval()
        {
            var calls = new List<int>();
            using var scheduler = new GameLoopTickSystemScheduler();
            var late = new OrderedTick(10, calls);
            var early = new OrderedTick(-10, calls);
            scheduler.AddSystem(late);
            scheduler.AddSystem(early);

            scheduler.Tick(0.1f, TickGroup.Gameplay);
            Assert.That(calls, Is.EqualTo(new[] { -10, 10 }));

            Assert.That(scheduler.RemoveSystem(early), Is.True);
            calls.Clear();
            scheduler.Tick(0.1f, TickGroup.Gameplay);
            Assert.That(calls, Is.EqualTo(new[] { 10 }));
        }

        private sealed class DependencyModule : GameModule
        {
            public DependencyModule() { }

            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                Lifecycle.Add("dependency.install");
                return default;
            }

            protected override ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
            {
                Lifecycle.Add("dependency.uninstall");
                return default;
            }
        }

        [GameModuleDependency(typeof(DependencyModule))]
        private sealed class DependentModule : GameModule
        {
            public DependentModule() { }

            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                Assert.That(context.GetDependency<DependencyModule>(), Is.Not.Null);
                Lifecycle.Add("dependent.install");
                return default;
            }

            protected override ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
            {
                Lifecycle.Add("dependent.uninstall");
                return default;
            }
        }

        private sealed class FailingModule : GameModule
        {
            public FailingModule() { }

            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                throw new InvalidOperationException("Expected test failure.");
            }
        }

        private sealed class AsyncFailingModule : GameModule
        {
            protected override async ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                await Task.Yield();
                throw new InvalidOperationException("Expected asynchronous failure.");
            }
        }

        private sealed class FailedTeardownModule : GameModule
        {
            public int DisposeCount, TickCount;
            public bool FailDispose, AsyncUninstall;
            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                context.AddTickSystem(new FailedTeardownTick(this));
                return default;
            }
            protected override async ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
            {
                if (AsyncUninstall) await Task.Yield();
                throw new InvalidOperationException("uninstall failed");
            }
            protected override void OnDispose()
            {
                DisposeCount++;
                if (FailDispose) throw new InvalidOperationException("release failed");
            }
            private sealed class FailedTeardownTick : IGameplayTick
            {
                private readonly FailedTeardownModule m_Owner;
                public FailedTeardownTick(FailedTeardownModule owner) => m_Owner = owner;
                public void GameplayTick(float deltaTime) => m_Owner.TickCount++;
            }
        }

        private sealed class CancellingModule : GameModule
        {
            public CancellationTokenSource Cancellation;
            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                Cancellation.Cancel();
                return default;
            }
        }

        private sealed class TickModule : GameModule
        {
            private readonly TickSystem m_TickSystem;

            public int TickCount { get; private set; }

            public TickModule()
            {
                m_TickSystem = new TickSystem(this);
            }

            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                context.AddTickSystem(m_TickSystem);
                return default;
            }

            private sealed class TickSystem : IGameplayTick
            {
                private readonly TickModule m_Owner;

                public TickSystem(TickModule owner)
                {
                    m_Owner = owner;
                }

                public void GameplayTick(float deltaTime)
                {
                    m_Owner.TickCount++;
                }
            }
        }

        private sealed class OrderedTick : IGameplayTick, ITickOrder
        {
            private readonly List<int> m_Calls;

            public int TickOrder { get; }

            public OrderedTick(int tickOrder, List<int> calls)
            {
                TickOrder = tickOrder;
                m_Calls = calls;
            }

            public void GameplayTick(float deltaTime)
            {
                m_Calls.Add(TickOrder);
            }
        }
    }
}
