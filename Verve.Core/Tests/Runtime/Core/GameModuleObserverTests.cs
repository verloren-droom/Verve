namespace Verve.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [Category("Core")]
    internal class GameModuleObserverTests
    {
        [Test]
        public async Task FactoryConfiguration_AppliesAfterOwnershipBeforeEntryConfigurationAndInstall()
        {
            var factory = new ModuleFactory();
            using var modules = new GameModules(new GameModulesOptions { CreateModuleFactory = () => factory });
            factory.Configuration = module =>
            {
                Assert.Throws<InvalidOperationException>(() => module.Dispose());
                Assert.Catch<InvalidOperationException>(() => modules.Install<SecondModule>());
                ((ConfiguredModule)module).Value = 10;
            };
            modules.Install<ConfiguredModule>();
            Assert.That(modules.GetModule<ConfiguredModule>().InstalledValue, Is.EqualTo(10));
            modules.Uninstall<ConfiguredModule>();

            var manifest = new GameModuleManifest();
            manifest.Add(new GameModuleDescriptor(typeof(ConfiguredModule),
                () => new ConfiguredModule(), module =>
                {
                    Assert.That(((ConfiguredModule)module).Value, Is.EqualTo(10));
                    ((ConfiguredModule)module).Value = 20;
                }));
            await modules.InstallFromManifestAsync(manifest);
            Assert.That(modules.GetModule<ConfiguredModule>().InstalledValue, Is.EqualTo(20));
            Assert.That(factory.ConfigureCount, Is.EqualTo(2));
        }

        [Test]
        public async Task FactoryConfigurationFailure_DisposesInstanceAndSkipsEntryAndInstall()
        {
            foreach (var asynchronous in new[] { false, true })
            {
                var module = new ConfiguredModule();
                var factory = new ModuleFactory { Configuration = _ => throw new InvalidOperationException("configure failed") };
                using var modules = new GameModules(new GameModulesOptions { CreateModuleFactory = () => factory });
                var manifest = new GameModuleManifest();
                var entryCalled = false;
                manifest.Add(new GameModuleDescriptor(typeof(ConfiguredModule), () => module, _ => entryCalled = true));
                Exception failure = null;
                try
                {
                    if (asynchronous) await modules.InstallFromManifestAsync(manifest);
                    else modules.InstallFromManifest(manifest);
                }
                catch (Exception error) { failure = error; }
                Assert.That(failure?.Message, Is.EqualTo("configure failed"));
                Assert.That(entryCalled, Is.False);
                Assert.That(module.InstalledValue, Is.Zero);
                Assert.That(module.DisposeCount, Is.EqualTo(1));
                Assert.That(modules.InstalledModules, Is.Empty);
            }
        }

        [Test]
        public async Task Observer_ReportsCompletedOperationsAndCannotReenterContainerChanges()
        {
            var observer = new Observer();
            var modules = new GameModules(new GameModulesOptions { CreateObserver = () => observer });
            observer.Callback = result =>
            {
                Assert.That(result.Duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
                Assert.That(result.Failure, Is.Null);
                Assert.Catch<InvalidOperationException>(() => modules.Install<SecondModule>());
            };
            modules.Install<ConfiguredModule>();
            await modules.UninstallAsync<ConfiguredModule>();
            await modules.InstallAsync<ConfiguredModule>();
            await modules.DisposeAsync();
            Assert.That(observer.Results.Count, Is.EqualTo(4));
            Assert.That(observer.Results[0].Operation, Is.EqualTo(GameModuleOperation.Install));
            Assert.That(observer.Results[1].Operation, Is.EqualTo(GameModuleOperation.Uninstall));
            Assert.That(observer.Results[3].ModuleType, Is.EqualTo(typeof(ConfiguredModule)));
            Assert.That(observer.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ObserverFailure_DoesNotRollbackSuccessfulManifestOrContaminateNextOperation()
        {
            foreach (var asynchronous in new[] { false, true })
            {
                var observer = new Observer { Callback = _ => throw new InvalidOperationException("observer failed") };
                using var modules = new GameModules(new GameModulesOptions { CreateObserver = () => observer });
                var manifest = new GameModuleManifest(GameModuleInstallOrder.Declared);
                manifest.Add<ConfiguredModule>();
                manifest.Add<SecondModule>();
                Exception failure = null;
                try
                {
                    if (asynchronous) await modules.InstallFromManifestAsync(manifest);
                    else modules.InstallFromManifest(manifest);
                }
                catch (Exception error) { failure = error; }
                Assert.That(failure?.ToString(), Does.Contain("observer failed"));
                Assert.That(modules.InstalledModules.Count, Is.EqualTo(2));
                Assert.That(observer.Results.Count, Is.EqualTo(2));
                Assert.That(observer.Results.TrueForAll(result => result.Failure == null), Is.True);
                observer.Callback = null;
                await modules.UninstallAllAsync();
                Assert.That(modules.InstalledModules, Is.Empty);
            }
        }

        [Test]
        public async Task ManifestFailure_StillRollsBackAndPreservesOperationAndObserverErrors()
        {
            var observer = new Observer { Callback = _ => throw new InvalidOperationException("observer failed") };
            using var modules = new GameModules(new GameModulesOptions { CreateObserver = () => observer });
            var first = new ConfiguredModule();
            var second = new SecondModule();
            var manifest = new GameModuleManifest(GameModuleInstallOrder.Declared);
            manifest.Add(() => first);
            manifest.Add(new GameModuleDescriptor(typeof(SecondModule), () => second,
                _ => throw new InvalidOperationException("entry failed")));
            Exception failure = null;
            try { await modules.InstallFromManifestAsync(manifest); }
            catch (Exception error) { failure = error; }
            Assert.That(failure?.ToString(), Does.Contain("entry failed").And.Contain("observer failed"));
            Assert.That(modules.InstalledModules, Is.Empty);
            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(second.IsDisposed, Is.True);
            Assert.That(observer.Results.Count, Is.EqualTo(3));
            Assert.That(observer.Results[1].Failure?.Message, Is.EqualTo("entry failed"));
            Assert.That(observer.Results[2].Operation, Is.EqualTo(GameModuleOperation.Uninstall));
        }

        [Test]
        public async Task Disposal_CompletesAllCleanupAndReleasesObserverLastDespiteFailures()
        {
            foreach (var asynchronous in new[] { false, true })
            {
                var order = new List<string>();
                var observer = new Observer { Order = order, FailDispose = true };
                var factory = new ModuleFactory { Order = order, FailDispose = true };
                var modules = new GameModules(new GameModulesOptions
                {
                    CreateObserver = () => observer,
                    CreateModuleFactory = () => factory
                });
                var first = new ConfiguredModule { Order = order, FailDispose = true };
                modules.Install(() => first);
                modules.Install<SecondModule>();
                observer.Callback = _ => throw new InvalidOperationException("observer failed");
                Exception failure = null;
                try
                {
                    if (asynchronous) await modules.DisposeAsync();
                    else modules.Dispose();
                }
                catch (Exception error) { failure = error; }
                Assert.That(failure?.ToString(), Does.Contain("module disposal failed")
                    .And.Contain("factory disposal failed").And.Contain("observer failed").And.Contain("observer disposal failed"));
                Assert.That(order, Is.EqualTo(new[] { "module", "factory", "observer" }));
                Assert.That(observer.Results.Count, Is.EqualTo(4));
                Assert.That(observer.Results[2].Failure, Is.Null);
                Assert.That(observer.Results[3].Failure?.ToString(), Does.Contain("module disposal failed"));
                Assert.That(modules.IsDisposed, Is.True);
                modules.Dispose();
                Assert.That(observer.DisposeCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void ObserverCreation_RejectsSharingAndNullWithoutDisposingBorrowedObserver()
        {
            var observer = new Observer();
            using var owner = new GameModules(new GameModulesOptions { CreateObserver = () => observer });
            var factory = new ModuleFactory();
            Assert.Throws<InvalidOperationException>(() => new GameModules(new GameModulesOptions
            {
                CreateObserver = () => observer,
                CreateModuleFactory = () => factory
            }));
            Assert.That(observer.DisposeCount, Is.Zero);
            Assert.That(factory.DisposeCount, Is.EqualTo(1));
            var otherFactory = new ModuleFactory();
            Assert.Throws<InvalidOperationException>(() => new GameModules(new GameModulesOptions
            {
                CreateObserver = () => null,
                CreateModuleFactory = () => otherFactory
            }));
            Assert.That(otherFactory.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void FactoryFailure_ReportsKnownTypeOrExplicitlyUnknownTypeWithoutConfiguring()
        {
            var observer = new Observer();
            var factory = new ModuleFactory { Creation = _ => throw new InvalidOperationException("creation failed") };
            using var modules = new GameModules(new GameModulesOptions
            {
                CreateObserver = () => observer,
                CreateModuleFactory = () => factory
            });
            Assert.Throws<InvalidOperationException>(() => modules.Install<ConfiguredModule>());
            Assert.Throws<InvalidOperationException>(() => modules.Install(() => throw new InvalidOperationException("untyped failed")));
            Assert.That(observer.Results[0].ModuleType, Is.EqualTo(typeof(ConfiguredModule)));
            Assert.That(observer.Results[0].Failure?.Message, Is.EqualTo("creation failed"));
            Assert.That(observer.Results[1].ModuleType, Is.Null);
            Assert.That(factory.ConfigureCount, Is.Zero);
        }

        [Test]
        public async Task Cancellation_ReportsFailureAfterReleasingPartiallyInstalledModule()
        {
            var observer = new Observer();
            using var modules = new GameModules(new GameModulesOptions { CreateObserver = () => observer });
            using var cancellation = new CancellationTokenSource();
            var module = new WaitingModule();
            var install = modules.InstallAsync(() => module, cancellation.Token);
            await module.Started.Task;
            cancellation.Cancel();
            Exception failure = null;
            try { await install; }
            catch (Exception error) { failure = error; }
            Assert.That(failure, Is.InstanceOf<OperationCanceledException>());
            Assert.That(module.IsDisposed, Is.True);
            Assert.That(observer.Results.Count, Is.EqualTo(1));
            Assert.That(observer.Results[0].Failure, Is.SameAs(failure));
        }

        private sealed class ModuleFactory : IGameModuleFactory
        {
            public Action<GameModule> Configuration;
            public Func<Type, GameModule> Creation;
            public List<string> Order;
            public bool FailDispose;
            public int ConfigureCount;
            public int DisposeCount;
            public GameModule Create(Type moduleType) => Creation != null ? Creation(moduleType) : (GameModule)Activator.CreateInstance(moduleType);
            public void Configure(GameModule module) { ConfigureCount++; Configuration?.Invoke(module); }
            public void Dispose()
            {
                DisposeCount++;
                Order?.Add("factory");
                if (FailDispose) throw new InvalidOperationException("factory disposal failed");
            }
        }

        private sealed class Observer : IGameModuleObserver
        {
            public readonly List<GameModuleOperationResult> Results = new();
            public Action<GameModuleOperationResult> Callback;
            public List<string> Order;
            public int DisposeCount;
            public bool FailDispose;
            public void OnCompleted(GameModuleOperationResult result) { Results.Add(result); Callback?.Invoke(result); }
            public void Dispose()
            {
                DisposeCount++;
                Order?.Add("observer");
                if (FailDispose) throw new InvalidOperationException("observer disposal failed");
            }
        }

        private sealed class ConfiguredModule : GameModule
        {
            public int Value;
            public int InstalledValue;
            public int DisposeCount;
            public List<string> Order;
            public bool FailDispose;
            public ConfiguredModule() { }
            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                InstalledValue = Value;
                return default;
            }
            protected override void OnDispose()
            {
                DisposeCount++;
                Order?.Add("module");
                if (FailDispose) throw new InvalidOperationException("module disposal failed");
            }
        }

        private sealed class SecondModule : GameModule { public SecondModule() { } }

        private sealed class WaitingModule : GameModule
        {
            public readonly TaskCompletionSource<bool> Started = new();
            protected override async ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                Started.SetResult(true);
                await Task.Delay(Timeout.Infinite, ct);
            }
        }
    }
}
