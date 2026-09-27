namespace Verve.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [Category("Core")]
    internal class GameModuleCustomizationTests
    {
        [Test]
        public async Task CreationPolicy_AppliesToGenericRuntimeTypeAndManifestEntries()
        {
            var factory = new RecordingFactory();
            var options = new GameModulesOptions { CreateModuleFactory = () => factory };
            using var modules = new GameModules(options);
            modules.Install<InjectedModule>();
            Assert.That(modules.GetModule<InjectedModule>().Value, Is.EqualTo(42));
            modules.Uninstall<InjectedModule>();

            await modules.InstallAsync(typeof(InjectedModule));
            await modules.UninstallAsync<InjectedModule>();
            var manifest = new GameModuleManifest();
            manifest.Add<InjectedModule>();
            await modules.InstallFromManifestAsync(manifest);
            Assert.That(factory.Created, Has.Count.EqualTo(3));
            Assert.That(factory.Created[0].IsDisposed && factory.Created[1].IsDisposed, Is.True);
            Assert.That(factory.Created[2].IsInstalled, Is.True);
        }

        [Test]
        public void ExplicitFactory_OverridesContainerCreationWithoutChangingOwnership()
        {
            var factory = new RecordingFactory();
            using var modules = new GameModules(new GameModulesOptions { CreateModuleFactory = () => factory });
            var manifest = new GameModuleManifest();
            manifest.Add(() => new InjectedModule(99));
            modules.InstallFromManifest(manifest);
            var module = modules.GetModule<InjectedModule>();
            Assert.That(module.Value, Is.EqualTo(99));
            Assert.That(factory.Created, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => module.Dispose());
            modules.Dispose();
            Assert.That(module.DisposeCount, Is.EqualTo(1));
            Assert.That(factory.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Configure_RunsAfterOwnershipTransferAndFailureDisposesInstance()
        {
            var module = new InjectedModule(1);
            var manifest = new GameModuleManifest();
            var failure = new InvalidOperationException("configuration failed");
            manifest.Add(new GameModuleDescriptor(typeof(InjectedModule), () => module, instance =>
            {
                Assert.Throws<InvalidOperationException>(() => instance.Dispose());
                throw failure;
            }));
            using var modules = new GameModules();
            Assert.That(Assert.Throws<InvalidOperationException>(() => modules.InstallFromManifest(manifest)), Is.SameAs(failure));
            Assert.That(module.DisposeCount, Is.EqualTo(1));
            Assert.That(modules.InstalledModules, Is.Empty);
        }

        [Test]
        public void BorrowedModule_IsRejectedBeforeConfigurationOrCleanup()
        {
            using var owner = new GameModules();
            owner.Install(() => new InjectedModule(10));
            var borrowed = owner.GetModule<InjectedModule>();
            var factory = new RecordingFactory { CreateOverride = _ => borrowed };
            using var other = new GameModules(new GameModulesOptions { CreateModuleFactory = () => factory });
            var manifest = new GameModuleManifest();
            var configured = false;
            manifest.Add(new GameModuleDescriptor(typeof(InjectedModule), configure: _ => configured = true));
            Assert.Throws<InvalidOperationException>(() => other.InstallFromManifest(manifest));
            Assert.That(configured, Is.False);
            Assert.That(borrowed.IsInstalled, Is.True);
            Assert.That(borrowed.DisposeCount, Is.Zero);
        }

        [Test]
        public void InvalidFactoryResult_FailsWithoutDefaultCreationAndReleasesWrongInstance()
        {
            var wrong = new InjectedModule(7);
            var factory = new RecordingFactory { CreateOverride = _ => wrong };
            using var modules = new GameModules(new GameModulesOptions { CreateModuleFactory = () => factory });
            Assert.Throws<InvalidOperationException>(() => modules.Install<OtherModule>());
            Assert.That(wrong.DisposeCount, Is.EqualTo(1));
            factory.CreateOverride = _ => null;
            Assert.Throws<InvalidOperationException>(() => modules.Install<OtherModule>());
            Assert.That(modules.InstalledModules, Is.Empty);
        }

        [Test]
        public void SchedulerCreationFailure_ReleasesAlreadyCreatedFactory()
        {
            var factory = new RecordingFactory();
            var failure = new InvalidOperationException("scheduler creation failed");
            Assert.That(Assert.Throws<InvalidOperationException>(() => new GameModules(new GameModulesOptions
            {
                CreateModuleFactory = () => factory,
                CreateScheduler = () => throw failure
            })), Is.SameAs(failure));
            Assert.That(factory.DisposeCount, Is.EqualTo(1));

            var second = new RecordingFactory();
            Assert.Throws<InvalidOperationException>(() => new GameModules(new GameModulesOptions
            {
                CreateModuleFactory = () => second,
                CreateScheduler = () => null
            }));
            Assert.That(second.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void ExtensionInstances_CannotBeSharedOrReusedAfterDisposal()
        {
            var factory = new RecordingFactory();
            var scheduler = new RecordingScheduler();
            var options = new GameModulesOptions
            {
                CreateModuleFactory = () => factory,
                CreateScheduler = () => scheduler
            };
            using var owner = new GameModules(options);
            Assert.Throws<InvalidOperationException>(() => new GameModules(options));
            Assert.That(factory.DisposeCount, Is.Zero);
            var rejectedFactory = new RecordingFactory();
            Assert.Throws<InvalidOperationException>(() => new GameModules(new GameModulesOptions
            {
                CreateModuleFactory = () => rejectedFactory,
                CreateScheduler = () => scheduler
            }));
            Assert.That(scheduler.DisposeCount, Is.Zero);
            Assert.That(rejectedFactory.DisposeCount, Is.EqualTo(1));
            owner.Dispose();
            Assert.Throws<InvalidOperationException>(() => new GameModules(options));
            Assert.That(factory.DisposeCount, Is.EqualTo(1));
            Assert.That(scheduler.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ContainerDisposal_ReleasesModulesThenSchedulerThenFactoryDespiteFailures()
        {
            var order = new List<string>();
            var factory = new RecordingFactory { Order = order, FailDispose = true };
            var scheduler = new RecordingScheduler { Order = order, FailDispose = true };
            var modules = new GameModules(new GameModulesOptions
            {
                CreateModuleFactory = () => factory,
                CreateScheduler = () => scheduler
            });
            modules.Install(() => new InjectedModule(3) { Order = order });
            Exception failure = null;
            try { await modules.DisposeAsync(); }
            catch (Exception error) { failure = error; }
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure.ToString(), Does.Contain("scheduler disposal failed").And.Contain("factory disposal failed"));
            Assert.That(order, Is.EqualTo(new[] { "module", "scheduler", "factory" }));
            Assert.That(modules.IsDisposed, Is.True);
            await modules.DisposeAsync();
            Assert.That(factory.DisposeCount, Is.EqualTo(1));
            Assert.That(scheduler.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void CustomScheduler_UsesFrameworkTickGuardAndMayComposeDefaultScheduler()
        {
            var scheduler = new RecordingScheduler();
            using var modules = new GameModules(new GameModulesOptions { CreateScheduler = () => scheduler });
            var calls = 0;
            modules.Install(() => new TickModule(() => calls++));
            scheduler.BeforeTick = () => Assert.Throws<InvalidOperationException>(() => modules.Uninstall<TickModule>());
            modules.Tick(1, TickGroup.Gameplay);
            Assert.That(calls, Is.EqualTo(1));
            scheduler.BeforeTick = null;
            modules.Uninstall<TickModule>();
            modules.Tick(1, TickGroup.Gameplay);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void ReusingOptions_CreatesIndependentExtensionsAndExistingContainersIgnoreEdits()
        {
            var factories = new List<RecordingFactory>();
            var options = new GameModulesOptions
            {
                CreateModuleFactory = () => { var factory = new RecordingFactory(); factories.Add(factory); return factory; }
            };
            using var first = new GameModules(options);
            using var second = new GameModules(options);
            options.CreateModuleFactory = () => throw new InvalidOperationException("new configuration");
            first.Install<InjectedModule>();
            second.Install<InjectedModule>();
            Assert.That(factories, Has.Count.EqualTo(2));
            first.Dispose();
            Assert.That(factories[0].DisposeCount, Is.EqualTo(1));
            Assert.That(factories[1].DisposeCount, Is.Zero);
        }

        private sealed class InjectedModule : GameModule
        {
            public readonly int Value;
            public int DisposeCount;
            public List<string> Order;
            public InjectedModule(int value) => Value = value;
            protected override void OnDispose() { DisposeCount++; Order?.Add("module"); }
        }

        private sealed class OtherModule : GameModule { public OtherModule() { } }

        private sealed class RecordingFactory : IGameModuleFactory
        {
            public readonly List<GameModule> Created = new();
            public Func<Type, GameModule> CreateOverride;
            public List<string> Order;
            public bool FailDispose;
            public int DisposeCount;
            public GameModule Create(Type type)
            {
                var module = CreateOverride != null ? CreateOverride(type) : new InjectedModule(42);
                Created.Add(module);
                return module;
            }
            public void Configure(GameModule module) { }
            public void Dispose()
            {
                DisposeCount++;
                Order?.Add("factory");
                if (FailDispose) throw new InvalidOperationException("factory disposal failed");
            }
        }

        private sealed class RecordingScheduler : IGameLoopTickSystemScheduler
        {
            private readonly IGameLoopTickSystemScheduler m_Default = new GameModulesOptions().CreateScheduler();
            public Action BeforeTick;
            public List<string> Order;
            public bool FailDispose;
            public int DisposeCount;
            public void AddSystem(object system) => m_Default.AddSystem(system);
            public bool RemoveSystem(object system) => m_Default.RemoveSystem(system);
            public void Tick(float deltaTime, TickGroup group) { BeforeTick?.Invoke(); m_Default.Tick(deltaTime, group); }
            public void WaitForIdle() => m_Default.WaitForIdle();
            public void Dispose()
            {
                DisposeCount++;
                Order?.Add("scheduler");
                m_Default.Dispose();
                if (FailDispose) throw new InvalidOperationException("scheduler disposal failed");
            }
        }

        private sealed class TickModule : GameModule
        {
            private readonly Action m_Callback;
            public TickModule(Action callback) => m_Callback = callback;
            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                context.AddTickSystem(new CallbackTick(m_Callback));
                return default;
            }
        }

        private sealed class CallbackTick : IGameplayTick
        {
            private readonly Action m_Callback;
            public CallbackTick(Action callback) => m_Callback = callback;
            public void GameplayTick(float deltaTime) => m_Callback();
        }
    }
}
