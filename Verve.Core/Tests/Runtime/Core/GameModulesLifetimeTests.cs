namespace Verve.Tests.Core
{
    using System;
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    [Category("Core")]
    internal class GameModulesLifetimeTests
    {
        [UnityTest, Timeout(10000)]
        public IEnumerator LifetimeDestruction_AwaitsModuleCleanupAutomatically()
        {
            var owner = new GameObject("modules owner");
            var handle = Game.CreateModules(owner);
            var modules = handle.Modules;
            var module = new OwnedModule();
            modules.Install(() => module);
            UnityEngine.Object.DestroyImmediate(owner);
            while (!modules.IsDisposed) yield return null;
            Assert.That(handle.IsDisposed && module.IsDisposed, Is.True);
            Assert.That(module.Uninstalled, Is.True);
            Assert.That(module.DisposeCount, Is.EqualTo(1));
        }

        [UnityTest, Timeout(10000)]
        public IEnumerator LifetimeDestruction_CancelsPendingInstallAndReleasesItsResources()
        {
            var owner = new GameObject("modules owner");
            var handle = Game.CreateModules(owner);
            var modules = handle.Modules;
            var module = new OwnedModule { WaitDuringInstall = true };
            var install = modules.InstallAsync(() => module).AsTask();
            Assert.That(install.IsCompleted, Is.False);
            UnityEngine.Object.DestroyImmediate(owner);
            while (!modules.IsDisposed || !install.IsCompleted) yield return null;
            Assert.That(install.IsCanceled, Is.True);
            Assert.That(module.IsDisposed, Is.True);
            Assert.That(module.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void LifetimeBinding_UsesCustomCreationAndReleasesExtensionsAutomatically()
        {
            var owner = new GameObject("custom modules owner");
            var factory = new CustomFactory();
            var scheduler = new CustomScheduler();
            var manifest = ScriptableObject.CreateInstance<GameModuleManifestAsset>();
            ConfiguredModule module = null;
            try
            {
                manifest.SetManifestData(new GameModuleManifestAsset.GameModuleManifestData
                {
                    modules = new[]
                    {
                        new GameModuleManifestAsset.GameModuleEntry
                        {
                            type = typeof(ConfiguredModule).AssemblyQualifiedName,
                            fields = "{\"Value\":73}"
                        }
                    }
                });
                var handle = Game.CreateModules(owner.transform, new GameModulesOptions
                {
                    CreateModuleFactory = () => factory,
                    CreateScheduler = () => scheduler
                });
                handle.Modules.InstallFromManifest(manifest.ToManifest());
                module = handle.Modules.GetModule<ConfiguredModule>();
                Assert.That(module.Value, Is.EqualTo(73));
                Assert.That(module.CreatedByCustomFactory, Is.True);
                Assert.That(module.ConfiguredByCustomFactory, Is.True);
                Assert.That(module.ValueAtInstall, Is.EqualTo(73));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(manifest);
            }
            Assert.That(module.IsDisposed, Is.True);
            Assert.That(factory.DisposeCount, Is.EqualTo(1));
            Assert.That(scheduler.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Binding_RejectsDuplicateContainersAndInactiveLifetimeObjects()
        {
            var owner = new GameObject("modules owner");
            try
            {
                owner.SetActive(false);
                Assert.Throws<InvalidOperationException>(() => Game.CreateModules(owner));
                owner.SetActive(true);
                Game.CreateModules(owner);
                Assert.Throws<InvalidOperationException>(() => Game.CreateModules(owner));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        private sealed class OwnedModule : GameModule
        {
            public bool WaitDuringInstall, Uninstalled;
            public int DisposeCount;
            protected override async ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                if (WaitDuringInstall) await Task.Delay(Timeout.Infinite, ct);
            }
            protected override async ValueTask OnUninstall(GameModuleContext context, CancellationToken ct)
            {
                await Task.Yield();
                Uninstalled = true;
            }
            protected override void OnDispose() => DisposeCount++;
        }

        [Serializable]
        private sealed class ConfiguredModule : GameModule
        {
            public int Value;
            [NonSerialized] public bool CreatedByCustomFactory;
            [NonSerialized] public bool ConfiguredByCustomFactory;
            [NonSerialized] public int ValueAtInstall;
            public ConfiguredModule() { }
            protected override ValueTask OnInstall(GameModuleContext context, CancellationToken ct)
            {
                ValueAtInstall = Value;
                return default;
            }
        }

        private sealed class CustomFactory : IGameModuleFactory
        {
            public int DisposeCount;
            public GameModule Create(Type type) => new ConfiguredModule { CreatedByCustomFactory = true };
            public void Configure(GameModule module)
            {
                var configured = (ConfiguredModule)module;
                configured.ConfiguredByCustomFactory = true;
                configured.Value = 19;
            }
            public void Dispose() => DisposeCount++;
        }

        private sealed class CustomScheduler : IGameLoopTickSystemScheduler
        {
            public int DisposeCount;
            public void AddSystem(object system) { }
            public bool RemoveSystem(object system) => true;
            public void Tick(float deltaTime, TickGroup group) { }
            public void WaitForIdle() { }
            public void Dispose() => DisposeCount++;
        }
    }
}
